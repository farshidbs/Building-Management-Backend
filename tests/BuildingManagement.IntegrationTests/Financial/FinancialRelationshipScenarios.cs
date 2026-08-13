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
    public async Task CrossUnitCreditSettlementIsRejectedWithoutFinancialEffects()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unitA, unitB, building) = await TwoUnitsInOneBuilding(db);
        var accountA = await EnsureUnitAccount(unitA.Code, db);
        var accountB = await EnsureUnitAccount(unitB.Code, db);
        var fund = await EnsureFund(building.Code, db);
        await FinalizeOpeningCredit(unitA.Code, null, 700m);
        var receivable = await CreateOpeningDebt(unitB.Code, building.Code, 500m, db);
        db.ChangeTracker.Clear();
        var fundBefore = await Balance(db, fund.Id);

        using var response = await client!.PostAsJsonAsync(
            $"/api/v1/financial/units/{unitA.Code}/credit-settlements",
            new UnitCreditSettlementRequest(Guid.NewGuid(), [new(receivable.Code, 500m)]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(700m, await Credit(db, accountA.Id));
        Assert.Equal(0m, await Credit(db, accountB.Id));
        Assert.Equal(700m, await Balance(db, accountA.Id));
        Assert.Equal(-500m, await Balance(db, accountB.Id));
        Assert.Equal(500m, await Outstanding(db, receivable.Id));
        Assert.Equal(fundBefore, await Balance(db, fund.Id));
        Assert.False(await db.UnitCreditSettlements.AnyAsync());
    }

    [Fact]
    public async Task OneCreditSettlementAllocatesAcrossMultipleReceivables()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unit, building) = await FirstUnit(db);
        var account = await EnsureUnitAccount(unit.Code, db);
        var fund = await EnsureFund(building.Code, db);
        await FinalizeOpeningCredit(unit.Code, null, 1_000m);
        var a = await CreateOpeningDebt(unit.Code, building.Code, 300m, db);
        var b = await CreateOpeningDebt(unit.Code, building.Code, 500m, db);
        var c = await CreateOpeningDebt(unit.Code, building.Code, 600m, db);
        var balanceBefore = await Balance(db, account.Id);
        var fundBefore = await Balance(db, fund.Id);

        var result = await Post<UnitCreditSettlementResponse>(
            $"/api/v1/financial/units/{unit.Code}/credit-settlements",
            new UnitCreditSettlementRequest(Guid.NewGuid(),
                [new(a.Code, 300m), new(b.Code, 500m), new(c.Code, 200m)]));
        db.ChangeTracker.Clear();

        Assert.Equal(0m, result.AvailableCreditAfter);
        Assert.Equal(0m, await Credit(db, account.Id));
        Assert.Equal(balanceBefore, await Balance(db, account.Id));
        Assert.Equal(fundBefore, await Balance(db, fund.Id));
        await AssertReceivable(db, a.Id, 0m, FinancialKeys.ReceivableStatuses.Paid);
        await AssertReceivable(db, b.Id, 0m, FinancialKeys.ReceivableStatuses.Paid);
        await AssertReceivable(db, c.Id, 400m, FinancialKeys.ReceivableStatuses.PartiallyPaid);
        var settlementId = await db.UnitCreditSettlements.Where(x => x.Code == result.Code)
            .Select(x => x.Id).SingleAsync();
        Assert.Equal(3, await db.UnitCreditSettlementAllocations.CountAsync(x =>
            x.UnitCreditSettlementId == settlementId));
    }

    [Fact]
    public async Task AdvancePaymentCreatesAvailableCreditWithoutReceivable()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unit, building) = await FirstUnit(db);
        var account = await EnsureUnitAccount(unit.Code, db);
        var fund = await EnsureFund(building.Code, db);

        var payment = await CreateAndConfirmPayment(unit.Code, building.Code, 1_400m, []);
        db.ChangeTracker.Clear();

        Assert.Equal(1_400m, await Balance(db, account.Id));
        Assert.Equal(1_400m, await Credit(db, account.Id));
        Assert.Equal(1_400m, await Balance(db, fund.Id));
        Assert.False(await db.PaymentAllocations.AnyAsync(x => x.PaymentId ==
            db.Payments.Where(p => p.Code == payment.Code).Select(p => p.Id).Single()));
        Assert.Equal(2, await db.FinancialTransactionEntries.CountAsync(x =>
            x.FinancialTransactionId == db.FinancialTransactions.Where(t =>
                t.PaymentId == db.Payments.Where(p => p.Code == payment.Code).Select(p => p.Id).Single())
                .Select(t => t.Id).Single()));
    }

    [Fact]
    public async Task FullyAllocatedPaymentCreatesNoAvailableCredit()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unit, building) = await FirstUnit(db);
        var account = await EnsureUnitAccount(unit.Code, db);
        var fund = await EnsureFund(building.Code, db);
        var receivable = await CreateOpeningDebt(unit.Code, building.Code, 700m, db);

        var payment = await CreateAndConfirmPayment(unit.Code, building.Code, 700m,
            [new(receivable.Code, 700m)]);
        db.ChangeTracker.Clear();

        Assert.Equal(0m, await Balance(db, account.Id));
        Assert.Equal(0m, await Credit(db, account.Id));
        Assert.Equal(700m, await Balance(db, fund.Id));
        await AssertReceivable(db, receivable.Id, 0m, FinancialKeys.ReceivableStatuses.Paid);
        Assert.Single(await db.PaymentAllocations.Where(x => x.PaymentId ==
            db.Payments.Where(p => p.Code == payment.Code).Select(p => p.Id).Single()).ToListAsync());
    }

    [Fact]
    public async Task OpeningCreditAffectsAvailableCreditOnlyForUnitAccount()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unit, building) = await FirstUnit(db);
        var account = await EnsureUnitAccount(unit.Code, db);
        var fund = await EnsureFund(building.Code, db);

        await FinalizeOpeningCredit(unit.Code, null, 900m);
        await FinalizeOpeningCredit(null, building.Code, 1_100m);
        db.ChangeTracker.Clear();

        Assert.Equal(900m, await Balance(db, account.Id));
        Assert.Equal(900m, await Credit(db, account.Id));
        Assert.Equal(1_100m, await Balance(db, fund.Id));
        Assert.Equal(0m, await Credit(db, fund.Id));
        Assert.Equal(2, await db.FinancialTransactions.CountAsync(x => x.AccountAdjustmentId != null));
    }

    [Fact]
    public async Task OnePaymentAllocatesAcrossMultipleReceivables()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unit, building) = await FirstUnit(db);
        var account = await EnsureUnitAccount(unit.Code, db);
        var fund = await EnsureFund(building.Code, db);
        var a = await CreateOpeningDebt(unit.Code, building.Code, 300m, db);
        var b = await CreateOpeningDebt(unit.Code, building.Code, 500m, db);

        var payment = await CreateAndConfirmPayment(unit.Code, building.Code, 800m,
            [new(a.Code, 300m), new(b.Code, 500m)]);
        db.ChangeTracker.Clear();

        Assert.Equal(0m, await Balance(db, account.Id));
        Assert.Equal(0m, await Credit(db, account.Id));
        Assert.Equal(800m, await Balance(db, fund.Id));
        await AssertReceivable(db, a.Id, 0m, FinancialKeys.ReceivableStatuses.Paid);
        await AssertReceivable(db, b.Id, 0m, FinancialKeys.ReceivableStatuses.Paid);
        var paymentId = await db.Payments.Where(x => x.Code == payment.Code).Select(x => x.Id).SingleAsync();
        Assert.Equal(2, await db.PaymentAllocations.CountAsync(x => x.PaymentId == paymentId));
        Assert.Equal(2, await db.FinancialTransactionEntries.CountAsync(x =>
            x.FinancialTransactionId == db.FinancialTransactions.Where(t => t.PaymentId == paymentId)
                .Select(t => t.Id).Single()));
    }

    [Fact]
    public async Task MultiplePaymentsSettleOneReceivableProgressively()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var (unit, building) = await FirstUnit(db);
        var account = await EnsureUnitAccount(unit.Code, db);
        var fund = await EnsureFund(building.Code, db);
        var receivable = await CreateOpeningDebt(unit.Code, building.Code, 1_000m, db);

        await CreateAndConfirmPayment(unit.Code, building.Code, 400m, [new(receivable.Code, 400m)]);
        db.ChangeTracker.Clear();
        await AssertReceivable(db, receivable.Id, 600m, FinancialKeys.ReceivableStatuses.PartiallyPaid);
        Assert.Equal(-600m, await Balance(db, account.Id));
        Assert.Equal(400m, await Balance(db, fund.Id));

        await CreateAndConfirmPayment(unit.Code, building.Code, 600m, [new(receivable.Code, 600m)]);
        db.ChangeTracker.Clear();
        await AssertReceivable(db, receivable.Id, 0m, FinancialKeys.ReceivableStatuses.Paid);
        Assert.Equal(0m, await Balance(db, account.Id));
        Assert.Equal(0m, await Credit(db, account.Id));
        Assert.Equal(1_000m, await Balance(db, fund.Id));
        Assert.Equal(2, await db.PaymentAllocations.CountAsync(x => x.UnitReceivableId == receivable.Id));
        Assert.Equal(2, await db.FinancialTransactions.CountAsync(x => x.PaymentId != null));
    }

    private async Task<FinancialAccount> EnsureUnitAccount(string unitCode, BuildingManagementDbContext db)
    {
        await Post<FinancialAccountResponse>("/api/v1/financial/accounts",
            new FinancialAccountRequest(unitCode, null, null, FinancialKeys.AccountKinds.Unit));
        return await db.FinancialAccounts.SingleAsync(x => x.UnitId ==
            db.Units.Where(u => u.Code == unitCode).Select(u => u.Id).Single());
    }

    private async Task<FinancialAccount> EnsureFund(string buildingCode, BuildingManagementDbContext db)
    {
        await Post<FinancialAccountResponse>("/api/v1/financial/accounts",
            new FinancialAccountRequest(null, buildingCode, null, FinancialKeys.AccountKinds.ReserveFund));
        return await db.FinancialAccounts.SingleAsync(x => x.BuildingId ==
            db.Buildings.Where(b => b.Code == buildingCode).Select(b => b.Id).Single() &&
            x.AccountKindKey == FinancialKeys.AccountKinds.ReserveFund);
    }

    private async Task<UnitReceivable> CreateOpeningDebt(string unitCode, string buildingCode,
        decimal amount, BuildingManagementDbContext db)
    {
        var adjustment = await Post<AccountAdjustmentResponse>("/api/v1/financial/accounts/adjustments",
            new AccountAdjustmentRequest(unitCode, null, null, FinancialKeys.AccountKinds.Unit,
                buildingCode, null, FinancialKeys.AccountKinds.ReserveFund,
                FinancialKeys.Adjustments.OpeningDebt, amount, DateTimeOffset.UtcNow,
                "بدهی تست", FinancialKeys.ResponsibleParties.Owner));
        using var response = await client!.PostAsync(
            $"/api/v1/financial/accounts/adjustments/{adjustment.Code}/finalize", null);
        response.EnsureSuccessStatusCode();
        return await db.UnitReceivables.SingleAsync(x => x.AccountAdjustmentId ==
            db.AccountAdjustments.Where(a => a.Code == adjustment.Code).Select(a => a.Id).Single());
    }

    private async Task FinalizeOpeningCredit(string? unitCode, string? buildingCode, decimal amount)
    {
        var adjustment = await Post<AccountAdjustmentResponse>("/api/v1/financial/accounts/adjustments",
            new AccountAdjustmentRequest(unitCode, buildingCode, null,
                unitCode is null ? FinancialKeys.AccountKinds.ReserveFund : FinancialKeys.AccountKinds.Unit,
                null, null, null, FinancialKeys.Adjustments.OpeningCredit, amount,
                DateTimeOffset.UtcNow, "اعتبار تست"));
        using var response = await client!.PostAsync(
            $"/api/v1/financial/accounts/adjustments/{adjustment.Code}/finalize", null);
        response.EnsureSuccessStatusCode();
    }

    private async Task<PaymentResponse> CreateAndConfirmPayment(string unitCode, string buildingCode,
        decimal amount, IReadOnlyList<PaymentAllocationRequest> allocations)
    {
        var payment = await Post<PaymentResponse>("/api/v1/financial/payments",
            new PaymentRequest(unitCode, buildingCode, null, FinancialKeys.AccountKinds.ReserveFund,
                null, "cash", amount, null, null, allocations));
        using var response = await client!.PostAsync(
            $"/api/v1/financial/payments/{payment.Code}/manager-confirm", null);
        response.EnsureSuccessStatusCode();
        return payment;
    }

    private static async Task<(Unit Unit, Building Building)> FirstUnit(BuildingManagementDbContext db)
    {
        var unit = await db.Units.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        var building = await db.Buildings.AsNoTracking().SingleAsync(x => x.Id == unit.BuildingId);
        return (unit, building);
    }

    private static async Task<(Unit A, Unit B, Building Building)> TwoUnitsInOneBuilding(
        BuildingManagementDbContext db)
    {
        var buildingId = await db.Buildings.AsNoTracking().Where(x =>
            db.Units.Count(u => u.BuildingId == x.Id) >= 2).OrderBy(x => x.Id).Select(x => x.Id).FirstAsync();
        var units = await db.Units.AsNoTracking().Where(x => x.BuildingId == buildingId)
            .OrderBy(x => x.Id).Take(2).ToListAsync();
        var building = await db.Buildings.AsNoTracking().SingleAsync(x => x.Id == buildingId);
        return (units[0], units[1], building);
    }

    private static Task<decimal> Balance(BuildingManagementDbContext db, long id) =>
        db.FinancialAccounts.Where(x => x.Id == id).Select(x => x.CurrentBalance).SingleAsync();

    private static Task<decimal> Credit(BuildingManagementDbContext db, long id) =>
        db.FinancialAccounts.Where(x => x.Id == id).Select(x => x.AvailableCredit).SingleAsync();

    private static Task<decimal> Outstanding(BuildingManagementDbContext db, long id) =>
        db.UnitReceivables.Where(x => x.Id == id).Select(x => x.OutstandingAmount).SingleAsync();

    private static async Task AssertReceivable(BuildingManagementDbContext db, long id,
        decimal outstanding, string status)
    {
        var row = await db.UnitReceivables.Where(x => x.Id == id)
            .Select(x => new { x.OutstandingAmount, x.Status }).SingleAsync();
        Assert.Equal(outstanding, row.OutstandingAmount);
        Assert.Equal(status, row.Status);
    }
}
