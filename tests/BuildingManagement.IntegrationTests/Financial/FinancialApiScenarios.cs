using System.Net;
using System.Net.Http.Json;
using BuildingManagement.Application;
using BuildingManagement.Domain;
using BuildingManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildingManagement.IntegrationTests;

public sealed partial class ApiScenarios
{
    [Fact]
    public async Task ExpenseDisbursementPreservesTransactionIntegrityAndAllowsNegativeFund()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var building = await db.Buildings.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        var expenseTypeId = await db.ExpenseTypes.Where(x => x.Key == "electricity").Select(x => x.Id).SingleAsync();
        await Post<FinancialAccountResponse>("/api/v1/financial/accounts",
            new FinancialAccountRequest(null, building.Code, null, FinancialKeys.AccountKinds.CurrentFund));
        var opening = await Post<AccountAdjustmentResponse>("/api/v1/financial/accounts/adjustments",
            new AccountAdjustmentRequest(null, building.Code, null, FinancialKeys.AccountKinds.CurrentFund,
                null, null, null, FinancialKeys.Adjustments.OpeningCredit, 12_000_000m,
                DateTimeOffset.UtcNow, "مانده اولیه صندوق"));
        using var finalizedOpening = await client!.PostAsync(
            $"/api/v1/financial/accounts/adjustments/{opening.Code}/finalize", null);
        finalizedOpening.EnsureSuccessStatusCode();

        var expense = await Post<ExpenseResponse>("/api/v1/financial/expenses",
            new ExpenseRequest(building.Code, null, expenseTypeId, null, "قبض برق", 20_000_000m,
                DateTimeOffset.UtcNow, null, null));
        using var finalizeExpense = await client.PostAsync(
            $"/api/v1/financial/expenses/{expense.Code}/finalize", null);
        finalizeExpense.EnsureSuccessStatusCode();
        Assert.Equal(12_000_000m, await db.FinancialAccounts.Where(x => x.BuildingId == building.Id &&
            x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund).Select(x => x.CurrentBalance).SingleAsync());

        var disbursement = await Post<ExpenseDisbursementResponse>(
            $"/api/v1/financial/expenses/{expense.Code}/disbursements",
            new ExpenseDisbursementRequest(building.Code, null, FinancialKeys.AccountKinds.CurrentFund,
                null, 20_000_000m, "bank_transfer", null));
        using var finalizeDisbursement = await client.PostAsync(
            $"/api/v1/financial/expenses/{expense.Code}/disbursements/{disbursement.Code}/finalize", null);
        finalizeDisbursement.EnsureSuccessStatusCode();
        var filesBeforeFailure = Directory.Exists(fileRoot!)
            ? Directory.GetFiles(fileRoot!, "*", SearchOption.AllDirectories).Length : 0;
        var metadataBeforeFailure = await db.StoredFiles.CountAsync();
        using (var invalid = Pdf("نامعتبر.pdf", new string('ط', 250)))
        using (var failedUpload = await client.PostAsync(
            $"/api/v1/financial/expenses/{expense.Code}/documents", invalid))
            Assert.Equal(HttpStatusCode.Conflict, failedUpload.StatusCode);
        db.ChangeTracker.Clear();
        Assert.Equal(metadataBeforeFailure, await db.StoredFiles.CountAsync());
        Assert.Equal(0, await db.ExpenseDocuments.CountAsync(x => x.ExpenseId ==
            db.Expenses.Where(e => e.Code == expense.Code).Select(e => e.Id).Single()));
        Assert.Equal(filesBeforeFailure, Directory.GetFiles(fileRoot!, "*", SearchOption.AllDirectories).Length);
        using (var form = Pdf("سند-هزینه.pdf"))
        using (var upload = await client.PostAsync($"/api/v1/financial/expenses/{expense.Code}/documents", form))
            upload.EnsureSuccessStatusCode();
        using (var form = Pdf("رسید-پرداخت.pdf"))
        using (var upload = await client.PostAsync(
            $"/api/v1/financial/expenses/{expense.Code}/disbursements/{disbursement.Code}/files", form))
            upload.EnsureSuccessStatusCode();

        db.ChangeTracker.Clear();
        var account = await db.FinancialAccounts.SingleAsync(x => x.BuildingId == building.Id &&
            x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund);
        Assert.Equal(-8_000_000m, account.CurrentBalance);
        var transaction = await db.FinancialTransactions.SingleAsync(x => x.ExpenseDisbursementId != null);
        var entry = await db.FinancialTransactionEntries.SingleAsync(x =>
            x.FinancialTransactionId == transaction.Id);
        Assert.Equal(account.CurrentBalance, entry.BalanceAfter);
        Assert.False(await db.Demands.AnyAsync());
        Assert.Equal(1, await db.ExpenseDocuments.CountAsync(x => x.ExpenseId ==
            db.Expenses.Where(e => e.Code == expense.Code).Select(e => e.Id).Single()));
        Assert.Equal(1, await db.ExpenseDisbursementFiles.CountAsync(x => x.ExpenseDisbursementId ==
            db.ExpenseDisbursements.Where(d => d.Code == disbursement.Code).Select(d => d.Id).Single()));
        using var retry = await client.PostAsync(
            $"/api/v1/financial/expenses/{expense.Code}/disbursements/{disbursement.Code}/finalize", null);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
        Assert.Equal(1, await db.FinancialTransactions.CountAsync(x => x.ExpenseDisbursementId != null));
    }

    [Fact]
    public async Task OpeningDebtCreatesPayableReceivableAndSupportsPartialAndExcessPayment()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var unit = await db.Units.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        var building = await db.Buildings.AsNoTracking().SingleAsync(x => x.Id == unit.BuildingId);
        await Post<FinancialAccountResponse>("/api/v1/financial/accounts",
            new FinancialAccountRequest(unit.Code, null, null, FinancialKeys.AccountKinds.Unit));
        await Post<FinancialAccountResponse>("/api/v1/financial/accounts",
            new FinancialAccountRequest(null, building.Code, null, FinancialKeys.AccountKinds.ReserveFund));
        var adjustment = await Post<AccountAdjustmentResponse>("/api/v1/financial/accounts/adjustments",
            new AccountAdjustmentRequest(unit.Code, null, null, FinancialKeys.AccountKinds.Unit,
                building.Code, null, FinancialKeys.AccountKinds.ReserveFund,
                FinancialKeys.Adjustments.OpeningDebt, 8_000_000m, DateTimeOffset.UtcNow,
                "بدهی ابتدای دوره", FinancialKeys.ResponsibleParties.Owner));
        using var finalize = await client!.PostAsync(
            $"/api/v1/financial/accounts/adjustments/{adjustment.Code}/finalize", null);
        finalize.EnsureSuccessStatusCode();
        using var finalizeAgain = await client.PostAsync(
            $"/api/v1/financial/accounts/adjustments/{adjustment.Code}/finalize", null);
        finalizeAgain.EnsureSuccessStatusCode();

        var receivable = await db.UnitReceivables.SingleAsync(x => x.AccountAdjustmentId != null);
        var unitAccount = await db.FinancialAccounts.SingleAsync(x => x.UnitId == unit.Id);
        Assert.Equal(8_000_000m, receivable.OriginalAmount);
        Assert.Equal(8_000_000m, receivable.OutstandingAmount);
        Assert.Equal(-8_000_000m, unitAccount.CurrentBalance);
        Assert.Equal(1, await db.UnitReceivables.CountAsync(x => x.AccountAdjustmentId != null));

        var first = await Post<PaymentResponse>("/api/v1/financial/payments", new PaymentRequest(unit.Code,
            building.Code, null, FinancialKeys.AccountKinds.ReserveFund, null, "card_to_card", 3_000_000m,
            null, null, [new(receivable.Code, 3_000_000m)]));
        using var confirmFirst = await client.PostAsync(
            $"/api/v1/financial/payments/{first.Code}/manager-confirm", null);
        confirmFirst.EnsureSuccessStatusCode();
        using (var form = Pdf("فیش-واریز.pdf"))
        using (var upload = await client.PostAsync($"/api/v1/financial/payments/{first.Code}/evidence", form))
            upload.EnsureSuccessStatusCode();
        db.ChangeTracker.Clear();
        Assert.Equal(5_000_000m, (await db.UnitReceivables.SingleAsync(x => x.Id == receivable.Id)).OutstandingAmount);

        var second = await Post<PaymentResponse>("/api/v1/financial/payments", new PaymentRequest(unit.Code,
            building.Code, null, FinancialKeys.AccountKinds.ReserveFund, null, "card_to_card", 7_000_000m,
            null, null, [new(receivable.Code, 5_000_000m)]));
        using var confirmSecond = await client.PostAsync(
            $"/api/v1/financial/payments/{second.Code}/manager-confirm", null);
        confirmSecond.EnsureSuccessStatusCode();
        db.ChangeTracker.Clear();
        var settled = await db.UnitReceivables.SingleAsync(x => x.Id == receivable.Id);
        unitAccount = await db.FinancialAccounts.SingleAsync(x => x.UnitId == unit.Id);
        var fund = await db.FinancialAccounts.SingleAsync(x => x.BuildingId == building.Id &&
            x.AccountKindKey == FinancialKeys.AccountKinds.ReserveFund);
        Assert.Equal(0, settled.OutstandingAmount);
        Assert.Equal(FinancialKeys.ReceivableStatuses.Paid, settled.Status);
        Assert.Equal(2_000_000m, unitAccount.CurrentBalance);
        Assert.Equal(2_000_000m, unitAccount.AvailableCredit);
        Assert.Equal(10_000_000m, fund.CurrentBalance);
        Assert.Equal(1, await db.PaymentEvidenceFiles.CountAsync(x => x.PaymentId ==
            db.Payments.Where(p => p.Code == first.Code).Select(p => p.Id).Single()));

        var online = await Post<PaymentResponse>("/api/v1/financial/payments", new PaymentRequest(unit.Code,
            building.Code, null, FinancialKeys.AccountKinds.ReserveFund, null, "online_gateway", 100_000m,
            null, null, []));
        using var forgedManual = await client.PostAsync(
            $"/api/v1/financial/payments/{online.Code}/manager-confirm", null);
        Assert.Equal(HttpStatusCode.BadRequest, forgedManual.StatusCode);
        var trusted = scope.ServiceProvider.GetRequiredService<ITrustedPaymentResultProcessor>();
        await trusted.ProcessSuccessfulPayment(online.Code, "provider-operation-1", DateTimeOffset.UtcNow,
            CancellationToken.None);
        await trusted.ProcessSuccessfulPayment(online.Code, "provider-operation-1", DateTimeOffset.UtcNow,
            CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.Equal(1, await db.FinancialTransactions.CountAsync(x => x.PaymentId ==
            db.Payments.Where(p => p.Code == online.Code).Select(p => p.Id).Single()));
        Assert.Equal(2_100_000m, await db.FinancialAccounts.Where(x => x.Id == unitAccount.Id)
            .Select(x => x.AvailableCredit).SingleAsync());
        var laterDebt = await Post<AccountAdjustmentResponse>("/api/v1/financial/accounts/adjustments",
            new AccountAdjustmentRequest(unit.Code, null, null, FinancialKeys.AccountKinds.Unit,
                building.Code, null, FinancialKeys.AccountKinds.ReserveFund,
                FinancialKeys.Adjustments.OpeningDebt, 1_000_000m, DateTimeOffset.UtcNow,
                "بدهی بعدی مالک", FinancialKeys.ResponsibleParties.Owner));
        using (var finalizeLaterDebt = await client.PostAsync(
            $"/api/v1/financial/accounts/adjustments/{laterDebt.Code}/finalize", null))
            finalizeLaterDebt.EnsureSuccessStatusCode();
        var laterReceivable = await db.UnitReceivables.SingleAsync(x => x.AccountAdjustmentId ==
            db.AccountAdjustments.Where(a => a.Code == laterDebt.Code).Select(a => a.Id).Single());
        var balanceBeforeCredit = await db.FinancialAccounts.Where(x => x.Id == unitAccount.Id)
            .Select(x => x.CurrentBalance).SingleAsync();
        var fundBeforeCredit = await db.FinancialAccounts.Where(x => x.Id == fund.Id)
            .Select(x => x.CurrentBalance).SingleAsync();
        var creditSettlement = await Post<UnitCreditSettlementResponse>(
            $"/api/v1/financial/units/{unit.Code}/credit-settlements",
            new UnitCreditSettlementRequest([new(laterReceivable.Code, 400_000m)]));
        db.ChangeTracker.Clear();
        Assert.Equal(1_700_000m, creditSettlement.AvailableCredit);
        Assert.Equal(600_000m, await db.UnitReceivables.Where(x => x.Id == laterReceivable.Id)
            .Select(x => x.OutstandingAmount).SingleAsync());
        Assert.Equal(balanceBeforeCredit, await db.FinancialAccounts.Where(x => x.Id == unitAccount.Id)
            .Select(x => x.CurrentBalance).SingleAsync());
        Assert.Equal(fundBeforeCredit, await db.FinancialAccounts.Where(x => x.Id == fund.Id)
            .Select(x => x.CurrentBalance).SingleAsync());
        Assert.Equal(1, await db.UnitCreditSettlements.CountAsync(x => x.Code == creditSettlement.Code));
        Assert.Equal(1, await db.UnitCreditSettlementAllocations.CountAsync(x =>
            x.UnitCreditSettlementId == db.UnitCreditSettlements.Where(s => s.Code == creditSettlement.Code)
                .Select(s => s.Id).Single()));
    }

    [Fact]
    public async Task ExpenseTypeIdentityAndScopeAreUnambiguous()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var building = await db.Buildings.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        var otherBuilding = await db.Buildings.AsNoTracking().Where(x => x.Id != building.Id)
            .OrderBy(x => x.Id).FirstAsync();
        var complexCode = await db.Complexes.Where(x => x.Id == building.ComplexId).Select(x => x.Code).SingleAsync();
        var complexType = await Post<ExpenseTypeResponse>("/api/v1/financial/expenses/types",
            new ExpenseTypeRequest(null, complexCode, "تعمیر ویژه"));
        var buildingType = await Post<ExpenseTypeResponse>("/api/v1/financial/expenses/types",
            new ExpenseTypeRequest(building.Code, null, "تعمیر ویژه"));
        var otherBuildingType = await Post<ExpenseTypeResponse>("/api/v1/financial/expenses/types",
            new ExpenseTypeRequest(otherBuilding.Code, null, "تعمیر ویژه"));
        Assert.NotEqual(complexType.Id, buildingType.Id);
        var globalOnly = await client!.GetFromJsonAsync<List<ExpenseTypeResponse>>(
            "/api/v1/financial/expenses/types");
        Assert.All(globalOnly!, x => Assert.True(x.BuildingCode is null && x.ComplexCode is null));
        var available = await client!.GetFromJsonAsync<List<ExpenseTypeResponse>>(
            $"/api/v1/financial/expenses/types?buildingCode={building.Code}");
        Assert.Contains(available!, x => x.Id == complexType.Id);
        Assert.Contains(available!, x => x.Id == buildingType.Id);
        Assert.DoesNotContain(available!, x => x.Id == otherBuildingType.Id);

        var expense = await Post<ExpenseResponse>("/api/v1/financial/expenses",
            new ExpenseRequest(building.Code, null, buildingType.Id, null, "تعمیر موتورخانه", 1_000_000m,
                DateTimeOffset.UtcNow, null, null));
        var expenseTypeId = await db.Expenses.Where(x => x.Code == expense.Code)
            .Select(x => x.ExpenseTypeId).SingleAsync();
        Assert.Equal(buildingType.Id, expenseTypeId);

        var parentTypeExpense = await Post<ExpenseResponse>("/api/v1/financial/expenses",
            new ExpenseRequest(building.Code, null, complexType.Id, null, "هزینه فضای مجتمع",
                2_000_000m, DateTimeOffset.UtcNow, null, null));
        Assert.Equal(complexType.Id, await db.Expenses.Where(x => x.Code == parentTypeExpense.Code)
            .Select(x => x.ExpenseTypeId).SingleAsync());
        using var unrelatedForBuilding = await client!.PostAsJsonAsync("/api/v1/financial/expenses",
            new ExpenseRequest(building.Code, null, otherBuildingType.Id, null, "نوع نامرتبط",
                1_000_000m, DateTimeOffset.UtcNow, null, null));
        Assert.Equal(HttpStatusCode.NotFound, unrelatedForBuilding.StatusCode);
        await Post<ExpenseResponse>("/api/v1/financial/expenses",
            new ExpenseRequest(null, complexCode, complexType.Id, null, "هزینه مجتمع",
                1_000_000m, DateTimeOffset.UtcNow, null, null));
        using var buildingTypeForComplex = await client!.PostAsJsonAsync("/api/v1/financial/expenses",
            new ExpenseRequest(null, complexCode, buildingType.Id, null, "نوع ساختمان برای مجتمع",
                1_000_000m, DateTimeOffset.UtcNow, null, null));
        Assert.Equal(HttpStatusCode.NotFound, buildingTypeForComplex.StatusCode);
    }

    [Fact]
    public async Task DraftDemandCanBeUpdatedButFinalizedSnapshotCannotBeRepreviewed()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var building = await db.Buildings.AsNoTracking().OrderByDescending(x => x.Id).FirstAsync();
        if (!await db.FinancialAccounts.AnyAsync(x => x.BuildingId == building.Id &&
            x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund))
            await Post<FinancialAccountResponse>("/api/v1/financial/accounts",
                new FinancialAccountRequest(null, building.Code, null, FinancialKeys.AccountKinds.CurrentFund));
        var unitIds = await db.Units.Where(x => x.BuildingId == building.Id).Select(x => x.Id).ToListAsync();
        foreach (var unitId in unitIds.Where(unitId => !db.FinancialAccounts.Any(x => x.UnitId == unitId)))
            db.FinancialAccounts.Add(new FinancialAccount(unitId, null, null, FinancialKeys.AccountKinds.Unit,
                DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        var draftRequest = new DemandRequest(building.Code, null, FinancialKeys.AccountKinds.CurrentFund,
            "monthly_charge", "شارژ آزمایشی", null, DateTimeOffset.UtcNow, null,
            new DemandRuleRequest(FinancialKeys.AllocationMethods.Equal, FinancialKeys.AmountModes.PerUnit,
                null, 100_000m, true, FinancialKeys.ResponsibleParties.Owner,
                FinancialKeys.Redistribution.None, null));
        var demand = await Post<DemandResponse>("/api/v1/financial/demands", draftRequest);
        var firstPreview = await Post<DemandPreviewResponse>(
            $"/api/v1/financial/demands/{demand.Code}/preview", new DemandPreviewRequest());
        var updatedRequest = new UpdateDemandDraftRequest("شارژ اصلاح‌شده", draftRequest.Description,
            draftRequest.DemandDate, draftRequest.DueDate,
            draftRequest.Rule with { RateAmount = 200_000m });
        await Put<DemandResponse>($"/api/v1/financial/demands/{demand.Code}", updatedRequest);
        var secondPreview = await Post<DemandPreviewResponse>(
            $"/api/v1/financial/demands/{demand.Code}/preview", new DemandPreviewRequest());
        Assert.Equal(firstPreview.FinalTotal * 2, secondPreview.FinalTotal);
        using var finalize = await client!.PostAsJsonAsync(
            $"/api/v1/financial/demands/{demand.Code}/finalize", new DemandPreviewRequest());
        finalize.EnsureSuccessStatusCode();
        using var updateFinalized = await client!.PutAsJsonAsync(
            $"/api/v1/financial/demands/{demand.Code}", updatedRequest);
        Assert.Equal(HttpStatusCode.Conflict, updateFinalized.StatusCode);
        using var repreview = await client!.PostAsJsonAsync(
            $"/api/v1/financial/demands/{demand.Code}/preview", new DemandPreviewRequest());
        Assert.Equal(HttpStatusCode.Conflict, repreview.StatusCode);
        Assert.Equal(unitIds.Count, await db.DemandAllocations.CountAsync(x =>
            x.DemandId == db.Demands.Where(d => d.Code == demand.Code).Select(d => d.Id).Single()));
    }

    private static MultipartFormDataContent Pdf(string fileName, string? title = null)
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent("%PDF-1.4 test"u8.ToArray());
        content.Headers.ContentType = new("application/pdf");
        form.Add(content, "File", fileName);
        if (title is not null) form.Add(new StringContent(title), "Title");
        return form;
    }
}
