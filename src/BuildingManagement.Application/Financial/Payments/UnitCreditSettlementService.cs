using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class UnitCreditSettlementService(IApplicationDbContext db, TimeProvider clock)
    : FinancialServiceBase(db, clock)
{
    public Task<UnitCreditSettlementResponse> Create(string unitCode, UnitCreditSettlementRequest request,
        CancellationToken ct) => Db.ExecuteInTransaction<UnitCreditSettlementResponse>(async token =>
    {
        if (request?.Allocations is null || request.Allocations.Count == 0)
            throw Validation("allocations", "At least one allocation is required.");
        if (request.Allocations.Any(x => x.Amount <= 0) || request.Allocations
            .GroupBy(x => x.ReceivableCode, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw Validation("allocations", "Receivables must be unique and amounts positive.");

        var account = await UnitAccount(unitCode, token);
        var total = request.Allocations.Sum(x => x.Amount);
        if (total > account.AvailableCredit)
            throw Validation("allocations", "Settlement exceeds available Unit credit.");
        var targets = new List<(UnitReceivable Receivable, decimal Amount)>();
        foreach (var item in request.Allocations)
        {
            var code = Code(item.ReceivableCode);
            var receivable = await Db.UnitReceivables.SingleOrDefaultAsync(x => x.Code == code && x.IsActive, token)
                ?? throw AppException.NotFound("unit_receivable");
            if (receivable.UnitAccountId != account.Id)
                throw Validation("allocations", "Credit cannot settle another Unit's Receivable.");
            if (item.Amount > receivable.OutstandingAmount)
                throw Validation("allocations", "Allocation exceeds outstanding amount.");
            targets.Add((receivable, item.Amount));
        }

        var settlement = new UnitCreditSettlement(await Unique(Db.UnitCreditSettlements, token), account.Id, Now);
        Db.UnitCreditSettlements.Add(settlement);
        await Save(token);
        foreach (var target in targets)
        {
            target.Receivable.ApplyUnitCredit(target.Amount, Now);
            Db.UnitCreditSettlementAllocations.Add(new UnitCreditSettlementAllocation(
                settlement.Id, target.Receivable.Id, target.Amount));
        }
        account.ConsumeAvailableCredit(total, Now);
        await Save(token);
        return new(settlement.Code, Code(unitCode), total, account.AvailableCredit);
    }, ct);
}
