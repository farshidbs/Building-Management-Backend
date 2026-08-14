using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public abstract class FinancialServiceBase(IApplicationDbContext db, TimeProvider clock,
    ResourceAuthorization? resourceAuthorization = null)
{
    protected IApplicationDbContext Db { get; } = db;
    protected ResourceAuthorization Authorization { get; } = resourceAuthorization!;
    protected DateTimeOffset Now => clock.GetUtcNow();
    protected static string Code(string value) => PublicCode.Normalize(value);
    protected static AppException Validation(string field, string message) => new(400, "validation.failed", "One or more validation errors occurred.", new Dictionary<string, string[]> { [field] = [message] });
    protected async Task Save(CancellationToken ct) { try { await Db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw AppException.Conflict("concurrency.conflict", "The financial resource changed concurrently."); } catch (DbUpdateException) { throw AppException.Conflict("persistence.conflict", "The financial change conflicts with existing data."); } }
    protected static async Task<string> Unique<T>(IQueryable<T> set, CancellationToken ct) where T : Entity { for (var i = 0; i < 20; i++) { var code = PublicCode.Create(); if (!await set.AnyAsync(x => x.Code == code, ct)) return code; } throw new AppException(500, "code.generation_failed", "A unique code could not be generated."); }
    protected async Task<FinancialTransactionEntry> Apply(FinancialAccount account, string effect, decimal amount, FinancialTransaction transaction, CancellationToken ct) { await Save(ct); var balance = account.Apply(effect, amount, Now); var entry = new FinancialTransactionEntry(transaction.Id, account.Id, effect, amount, balance, Now); Db.FinancialTransactionEntries.Add(entry); return entry; }
    protected async Task<FinancialAccount> UnitAccount(string unitCode, CancellationToken ct)
    { var normalized = Code(unitCode); return await Db.FinancialAccounts.SingleOrDefaultAsync(x => x.UnitId != null && x.AccountKindKey == FinancialKeys.AccountKinds.Unit && Db.Units.Any(u => u.Id == x.UnitId && u.Code == normalized), ct) ?? throw AppException.NotFound("financial_account"); }
    protected async Task<FinancialAccount> FundAccount(string? buildingCode, string? complexCode, string kind, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(buildingCode) == string.IsNullOrWhiteSpace(complexCode)) throw Validation("fundScope", "Exactly one Fund buildingCode or complexCode is required.");
        if (kind is not (FinancialKeys.AccountKinds.CurrentFund or FinancialKeys.AccountKinds.ReserveFund)) throw Validation("fundAccountKindKey", "A Fund kind is required.");
        var building = string.IsNullOrWhiteSpace(buildingCode) ? null : Code(buildingCode);
        var complex = string.IsNullOrWhiteSpace(complexCode) ? null : Code(complexCode);
        return await Db.FinancialAccounts.SingleOrDefaultAsync(x => x.AccountKindKey == kind &&
            (building != null && x.BuildingId != null && Db.Buildings.Any(b => b.Id == x.BuildingId && b.Code == building) ||
             complex != null && x.ComplexId != null && Db.Complexes.Any(c => c.Id == x.ComplexId && c.Code == complex)), ct)
            ?? throw AppException.NotFound("financial_account");
    }

    protected Task Authorize(string permission, long? complexId, long? buildingId, long? unitId,
        CancellationToken ct) => Authorization.Ensure(permission, complexId, buildingId, unitId, ct);

    protected Task Authorize(FinancialAccount account, string permission, CancellationToken ct) =>
        Authorize(permission, account.ComplexId, account.BuildingId, account.UnitId, ct);

    protected Task Authorize(Expense expense, string permission, CancellationToken ct) =>
        Authorize(permission, expense.ComplexId, expense.BuildingId, null, ct);

    protected async Task Authorize(Demand demand, string permission, CancellationToken ct)
    {
        var account = await Db.FinancialAccounts.AsNoTracking().SingleAsync(x => x.Id == demand.FundAccountId, ct);
        await Authorize(account, permission, ct);
    }
}
