using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public abstract class FinancialServiceBase(IApplicationDbContext db, TimeProvider clock)
{
    protected IApplicationDbContext Db { get; } = db;
    protected DateTimeOffset Now => clock.GetUtcNow();
    protected static string Code(string value) => PublicCode.Normalize(value);
    protected static AppException Validation(string field, string message) => new(400, "validation.failed", "One or more validation errors occurred.", new Dictionary<string, string[]> { [field] = [message] });
    protected async Task Save(CancellationToken ct) { try { await Db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw AppException.Conflict("concurrency.conflict", "The financial resource changed concurrently."); } catch (DbUpdateException) { throw AppException.Conflict("persistence.conflict", "The financial change conflicts with existing data."); } }
    protected static async Task<string> Unique<T>(IQueryable<T> set, CancellationToken ct) where T : Entity { for (var i = 0; i < 20; i++) { var code = PublicCode.Create(); if (!await set.AnyAsync(x => x.Code == code, ct)) return code; } throw new AppException(500, "code.generation_failed", "A unique code could not be generated."); }
    protected async Task<FinancialTransactionEntry> Apply(FinancialAccount account, string effect, decimal amount, FinancialTransaction transaction, CancellationToken ct) { await Save(ct); var balance = account.Apply(effect, amount, Now); var entry = new FinancialTransactionEntry(transaction.Id, account.Id, effect, amount, balance, Now); Db.FinancialTransactionEntries.Add(entry); return entry; }
    protected async Task<FinancialAccount> AccountByOwner(string ownerCode, string kind, CancellationToken ct) { var normalized = Code(ownerCode); var account = await Db.FinancialAccounts.SingleOrDefaultAsync(x => x.AccountKindKey == kind && ((x.UnitId != null && Db.Units.Any(u => u.Id == x.UnitId && u.Code == normalized)) || (x.BuildingId != null && Db.Buildings.Any(b => b.Id == x.BuildingId && b.Code == normalized)) || (x.ComplexId != null && Db.Complexes.Any(c => c.Id == x.ComplexId && c.Code == normalized))), ct); return account ?? throw AppException.NotFound("financial_account"); }
}
