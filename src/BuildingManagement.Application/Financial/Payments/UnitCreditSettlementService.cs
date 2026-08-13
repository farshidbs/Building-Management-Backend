using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class UnitCreditSettlementService(IApplicationDbContext db, TimeProvider clock) : FinancialServiceBase(db, clock)
{
    public async Task<UnitCreditSettlementResponse> Create(string unitCode, UnitCreditSettlementRequest request, CancellationToken ct)
    {
        Validate(request); var unit = Code(unitCode); var account = await UnitAccount(unit, ct);
        var prior = await Find(account.Id, request.RequestId, ct);
        if (prior is not null) return await Replay(prior, unit, request, ct);
        try
        {
            return await Db.ExecuteInTransaction<UnitCreditSettlementResponse>(async token =>
            {
                var total = request.Allocations.Sum(x => x.Amount);
                if (total > account.AvailableCredit) throw Validation("allocations", "Settlement exceeds available Unit credit.");
                var targets = new List<(UnitReceivable Receivable, decimal Amount)>();
                foreach (var item in request.Allocations)
                {
                    var receivable = await Db.UnitReceivables.SingleOrDefaultAsync(x => x.Code == Code(item.ReceivableCode) && x.IsActive, token) ?? throw AppException.NotFound("unit_receivable");
                    if (receivable.UnitAccountId != account.Id) throw Validation("allocations", "Credit cannot settle another Unit's Receivable.");
                    if (item.Amount > receivable.OutstandingAmount) throw Validation("allocations", "Allocation exceeds outstanding amount.");
                    targets.Add((receivable, item.Amount));
                }
                var settlement = new UnitCreditSettlement(await Unique(Db.UnitCreditSettlements, token), account.Id, request.RequestId, Now);
                Db.UnitCreditSettlements.Add(settlement); await Save(token);
                foreach (var target in targets) { target.Receivable.ApplyUnitCredit(target.Amount, Now); Db.UnitCreditSettlementAllocations.Add(new(settlement.Id, target.Receivable.Id, target.Amount)); }
                account.ConsumeAvailableCredit(total, Now); await Save(token);
                return await Response(settlement, unit, account.AvailableCredit, token);
            }, ct);
        }
        catch (AppException ex) when (ex.Code == "persistence.conflict")
        { var raced = await Find(account.Id, request.RequestId, ct); if (raced is null) throw; return await Replay(raced, unit, request, ct); }
    }

    public async Task<Page<UnitCreditSettlementResponse>> History(string unitCode, PageQuery query, CancellationToken ct)
    {
        var (number, size) = query.Validated(); var unit = Code(unitCode); var account = await UnitAccount(unit, ct);
        var source = Db.UnitCreditSettlements.AsNoTracking().Where(x => x.UnitAccountId == account.Id);
        var total = await source.CountAsync(ct); var rows = await source.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id).Skip((number - 1) * size).Take(size).ToListAsync(ct);
        var items = new List<UnitCreditSettlementResponse>(); foreach (var row in rows) items.Add(await Response(row, unit, account.AvailableCredit, ct));
        return new(items, number, size, total);
    }

    private Task<UnitCreditSettlement?> Find(long accountId, Guid requestId, CancellationToken ct) => Db.UnitCreditSettlements.AsNoTracking().SingleOrDefaultAsync(x => x.UnitAccountId == accountId && x.RequestId == requestId, ct);
    private async Task<UnitCreditSettlementResponse> Replay(UnitCreditSettlement settlement, string unit, UnitCreditSettlementRequest request, CancellationToken ct)
    {
        var stored = await Allocations(settlement.Id, ct); var requested = request.Allocations.Select(x => new UnitCreditSettlementAllocationResponse(Code(x.ReceivableCode), x.Amount)).OrderBy(x => x.ReceivableCode).ThenBy(x => x.Amount).ToList();
        if (!requested.SequenceEqual(stored.OrderBy(x => x.ReceivableCode).ThenBy(x => x.Amount))) throw AppException.Conflict("credit_settlement.idempotency_mismatch", "RequestId was already used for a different settlement command.");
        var credit = await Db.FinancialAccounts.Where(x => x.Id == settlement.UnitAccountId).Select(x => x.AvailableCredit).SingleAsync(ct);
        return new(settlement.Code, unit, settlement.RequestId, settlement.CreatedAtUtc, stored.Sum(x => x.Amount), credit, stored);
    }
    private async Task<UnitCreditSettlementResponse> Response(UnitCreditSettlement settlement, string unit, decimal credit, CancellationToken ct) { var allocations = await Allocations(settlement.Id, ct); return new(settlement.Code, unit, settlement.RequestId, settlement.CreatedAtUtc, allocations.Sum(x => x.Amount), credit, allocations); }
    private Task<List<UnitCreditSettlementAllocationResponse>> Allocations(long id, CancellationToken ct) => Db.UnitCreditSettlementAllocations.AsNoTracking().Where(x => x.UnitCreditSettlementId == id).OrderBy(x => x.Id).Select(x => new UnitCreditSettlementAllocationResponse(Db.UnitReceivables.Where(r => r.Id == x.UnitReceivableId).Select(r => r.Code).Single(), x.Amount)).ToListAsync(ct);
    private static void Validate(UnitCreditSettlementRequest request) { if (request is null) throw Validation("request", "Required."); if (request.RequestId == Guid.Empty) throw Validation("requestId", "Required."); if (request.Allocations is null || request.Allocations.Count == 0) throw Validation("allocations", "At least one allocation is required."); if (request.Allocations.Any(x => x.Amount <= 0) || request.Allocations.GroupBy(x => x.ReceivableCode, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) throw Validation("allocations", "Receivables must be unique and amounts positive."); }
}
