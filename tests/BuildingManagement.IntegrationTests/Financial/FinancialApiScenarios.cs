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
    public async Task FinancialExpenseDisbursementAndAdjustmentPreserveTransactionIntegrity()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>(); var building = await db.Buildings.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
        var fund = await Post<FinancialAccountResponse>("/api/v1/financial/accounts", new FinancialAccountRequest(null, building.Code, null, FinancialKeys.AccountKinds.CurrentFund));
        Assert.Equal(0, fund.CurrentBalance);
        var adjustment = await Post<AccountAdjustmentResponse>("/api/v1/financial/accounts/adjustments", new AccountAdjustmentRequest(building.Code, FinancialKeys.AccountKinds.CurrentFund, FinancialKeys.Adjustments.OpeningCredit, 12_000_000m, DateTimeOffset.UtcNow, "مانده اولیه صندوق"));
        using var finalizedAdjustment = await client!.PostAsync($"/api/v1/financial/accounts/adjustments/{adjustment.Code}/finalize", null); finalizedAdjustment.EnsureSuccessStatusCode();
        var expense = await Post<ExpenseResponse>("/api/v1/financial/expenses", new ExpenseRequest(building.Code, null, "electricity", null, "قبض برق", 20_000_000m, DateTimeOffset.UtcNow, null, null));
        using var finalizeExpense = await client.PostAsync($"/api/v1/financial/expenses/{expense.Code}/finalize", null); finalizeExpense.EnsureSuccessStatusCode();
        var before = await db.FinancialAccounts.Where(x => x.BuildingId == building.Id && x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund).Select(x => x.CurrentBalance).SingleAsync(); Assert.Equal(12_000_000m, before);
        var disbursement = await Post<ExpenseDisbursementResponse>($"/api/v1/financial/expenses/{expense.Code}/disbursements", new ExpenseDisbursementRequest(building.Code, FinancialKeys.AccountKinds.CurrentFund, null, 20_000_000m, "bank_transfer", null));
        using var finalizeDisbursement = await client.PostAsync($"/api/v1/financial/expenses/{expense.Code}/disbursements/{disbursement.Code}/finalize", null); finalizeDisbursement.EnsureSuccessStatusCode();
        db.ChangeTracker.Clear(); var account = await db.FinancialAccounts.SingleAsync(x => x.BuildingId == building.Id && x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund); Assert.Equal(-8_000_000m, account.CurrentBalance); var transaction = await db.FinancialTransactions.SingleAsync(x => x.ExpenseDisbursementId != null); var entry = await db.FinancialTransactionEntries.SingleAsync(x => x.FinancialTransactionId == transaction.Id); Assert.Equal(account.CurrentBalance, entry.BalanceAfter); Assert.Equal(FinancialKeys.Effects.Decrease, entry.EffectKey); Assert.False(await db.Demands.AnyAsync());
        using var retry = await client.PostAsync($"/api/v1/financial/expenses/{expense.Code}/disbursements/{disbursement.Code}/finalize", null); Assert.Equal(HttpStatusCode.OK, retry.StatusCode); Assert.Equal(1, await db.FinancialTransactions.CountAsync(x => x.ExpenseDisbursementId != null));
    }
}
