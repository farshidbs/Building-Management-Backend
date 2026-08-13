namespace BuildingManagement.Domain;

public sealed class UnitCreditSettlement : Entity
{
    private UnitCreditSettlement() { }
    public long UnitAccountId { get; private set; }
    public UnitCreditSettlement(string code, long unitAccountId, DateTimeOffset now) { if (unitAccountId <= 0) throw new DomainValidationException("unitAccount", "Required."); Initialize(code, now); UnitAccountId = unitAccountId; }
}

public sealed class UnitCreditSettlementAllocation
{
    private UnitCreditSettlementAllocation() { }
    public long Id { get; private set; }
    public long UnitCreditSettlementId { get; private set; }
    public long UnitReceivableId { get; private set; }
    public decimal Amount { get; private set; }
    public UnitCreditSettlementAllocation(long settlementId, long receivableId, decimal amount) { if (settlementId <= 0 || receivableId <= 0 || amount <= 0) throw new DomainValidationException("allocation", "Settlement, Receivable and positive amount are required."); UnitCreditSettlementId = settlementId; UnitReceivableId = receivableId; Amount = amount; }
}
