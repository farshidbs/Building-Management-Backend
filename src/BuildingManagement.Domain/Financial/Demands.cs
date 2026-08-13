namespace BuildingManagement.Domain;

public sealed class DemandType : ReferenceDataItem { private DemandType() { } public DemandType(string key, string title, int sortOrder, DateTimeOffset now) => Initialize(key, title, sortOrder, now); }

public sealed class Demand : Entity
{
    private Demand() { }
    public long FundAccountId { get; private set; }
    public long DemandTypeId { get; private set; }
    public string Title { get; private set; } = ""; public string? Description { get; private set; }
    public DateTimeOffset DemandDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public string Status { get; private set; } = FinancialKeys.Statuses.Draft; public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Demand(string code, long fundId, long typeId, string title, string? description, DateTimeOffset demandDate, DateTimeOffset? dueDate, DateTimeOffset now) { Initialize(code, now); if (fundId <= 0 || typeId <= 0) throw new DomainValidationException("fundAccountCode", "Fund and type are required."); FundAccountId = fundId; DemandTypeId = typeId; Title = Required(title, "title"); Description = Optional(description); DemandDate = demandDate; DueDate = dueDate; }
    public void UpdateDraft(string title, string? description, DateTimeOffset demandDate,
        DateTimeOffset? dueDate, DateTimeOffset now)
    { if (Status != FinancialKeys.Statuses.Draft) throw new DomainValidationException("status", "Only a Draft Demand can be updated."); Title = Required(title, "title"); Description = Optional(description); DemandDate = demandDate; DueDate = dueDate; Touch(now); }
    public void Finalize(DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Finalized) return; if (Status != FinancialKeys.Statuses.Draft) throw new DomainValidationException("status", "Demand cannot be finalized."); Status = FinancialKeys.Statuses.Finalized; FinalizedAtUtc = now; Touch(now); }
}

public sealed class DemandAllocationRule
{
    private DemandAllocationRule() { }
    public long Id { get; private set; }
    public long DemandId { get; private set; }
    public string AllocationMethodKey { get; private set; } = ""; public string AmountModeKey { get; private set; } = ""; public decimal? TotalAmount { get; private set; }
    public decimal? RateAmount { get; private set; }
    public bool? IncludeVacantUnits { get; private set; }
    public string ResponsiblePartyTypeKey { get; private set; } = ""; public string RedistributionPolicyKey { get; private set; } = ""; public string? Notes { get; private set; }
    public DemandAllocationRule(long demandId, string method, string mode, decimal? total, decimal? rate, bool? includeVacant, string responsible, string redistribution, string? notes)
    {
        if (demandId <= 0) throw new DomainValidationException("demand", "Required.");
        if (mode == FinancialKeys.AmountModes.Total && (!total.HasValue || rate.HasValue))
            throw new DomainValidationException("amount", "Total mode requires totalAmount only.");
        if (mode != FinancialKeys.AmountModes.Total && (total.HasValue || !rate.HasValue))
            throw new DomainValidationException("rateAmount", "Rate mode requires rateAmount only.");
        if (total <= 0 || rate <= 0) throw new DomainValidationException("amount", "Must be positive.");
        var validCombination = (method, mode) switch
        {
            (FinancialKeys.AllocationMethods.Equal, FinancialKeys.AmountModes.Total or FinancialKeys.AmountModes.PerUnit) => true,
            (FinancialKeys.AllocationMethods.Occupants, FinancialKeys.AmountModes.Total or FinancialKeys.AmountModes.PerPerson) => true,
            (FinancialKeys.AllocationMethods.Area, FinancialKeys.AmountModes.Total or FinancialKeys.AmountModes.PerArea) => true,
            (FinancialKeys.AllocationMethods.Custom, FinancialKeys.AmountModes.Total) => true,
            _ => false
        };
        if (!validCombination)
            throw new DomainValidationException("allocation", "Allocation method and amount mode are incompatible.");
        if (redistribution == FinancialKeys.Redistribution.ToOthers && mode != FinancialKeys.AmountModes.Total)
            throw new DomainValidationException("redistributionPolicyKey", "Redistribution requires a fixed total amount.");

        DemandId = demandId;
        AllocationMethodKey = method;
        AmountModeKey = mode;
        TotalAmount = total;
        RateAmount = rate;
        IncludeVacantUnits = includeVacant;
        ResponsiblePartyTypeKey = FinancialKeys.ResponsibleParties.RequireValid(responsible);
        RedistributionPolicyKey = redistribution;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
    public void Update(string method, string mode, decimal? total, decimal? rate, bool? includeVacant,
        string responsible, string redistribution, string? notes)
    {
        var replacement = new DemandAllocationRule(DemandId, method, mode, total, rate, includeVacant,
            responsible, redistribution, notes);
        AllocationMethodKey = replacement.AllocationMethodKey; AmountModeKey = replacement.AmountModeKey;
        TotalAmount = replacement.TotalAmount; RateAmount = replacement.RateAmount;
        IncludeVacantUnits = replacement.IncludeVacantUnits;
        ResponsiblePartyTypeKey = replacement.ResponsiblePartyTypeKey;
        RedistributionPolicyKey = replacement.RedistributionPolicyKey; Notes = replacement.Notes;
    }
}

public sealed class DemandAllocation
{
    private DemandAllocation() { }
    public long Id { get; private set; }
    public long DemandId { get; private set; }
    public long UnitId { get; private set; }
    public string ResponsiblePartyTypeKey { get; private set; } = ""; public long? ResponsiblePartyId { get; private set; }
    public decimal? BasisValue { get; private set; }
    public decimal CalculatedAmount { get; private set; }
    public decimal FinalAmount { get; private set; }
    public bool IsIncluded { get; private set; }
    public string? AdjustmentReason { get; private set; }
    public DemandAllocation(long demandId, long unitId, string responsible, long? partyId, decimal? basis, decimal calculated, decimal final, bool included, string? reason) { if (calculated < 0 || final < 0) throw new DomainValidationException("amount", "Must not be negative."); DemandId = demandId; UnitId = unitId; ResponsiblePartyTypeKey = FinancialKeys.ResponsibleParties.RequireValid(responsible); ResponsiblePartyId = partyId; BasisValue = basis; CalculatedAmount = calculated; FinalAmount = final; IsIncluded = included; AdjustmentReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(); }
}

public sealed class UnitReceivable : Entity
{
    private UnitReceivable() { }
    public long UnitAccountId { get; private set; }
    public long FundAccountId { get; private set; }
    public long? DemandAllocationId { get; private set; }
    public long? AccountAdjustmentId { get; private set; }
    public decimal OriginalAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string ResponsiblePartyTypeKey { get; private set; } = ""; public long? ResponsiblePartyId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public string Status { get; private set; } = FinancialKeys.ReceivableStatuses.Open;
    public UnitReceivable(string code, long unitAccountId, long fundId, long? allocationId, long? adjustmentId, decimal amount, string responsible, long? partyId, DateTimeOffset? dueDate, DateTimeOffset now) { if (allocationId.HasValue == adjustmentId.HasValue) throw new DomainValidationException("origin", "Exactly one origin is required."); if (amount <= 0) throw new DomainValidationException("amount", "Must be positive."); Initialize(code, now); UnitAccountId = unitAccountId; FundAccountId = fundId; DemandAllocationId = allocationId; AccountAdjustmentId = adjustmentId; OriginalAmount = OutstandingAmount = amount; ResponsiblePartyTypeKey = FinancialKeys.ResponsibleParties.RequireValid(responsible); ResponsiblePartyId = partyId; DueDate = dueDate; }
    public void ApplyPayment(decimal amount, DateTimeOffset now) { if (amount <= 0) throw new DomainValidationException("amount", "Must be positive."); if (amount > OutstandingAmount) throw new DomainValidationException("amount", "Cannot exceed outstanding amount."); OutstandingAmount -= amount; Status = OutstandingAmount == 0 ? FinancialKeys.ReceivableStatuses.Paid : FinancialKeys.ReceivableStatuses.PartiallyPaid; Touch(now); }
}

public sealed class DemandExpense { public long DemandId { get; private set; } public long ExpenseId { get; private set; } public decimal? RelatedAmount { get; private set; } private DemandExpense() { } public DemandExpense(long d, long e, decimal? amount) { if (amount < 0) throw new DomainValidationException("relatedAmount", "Must not be negative."); DemandId = d; ExpenseId = e; RelatedAmount = amount; } }
public sealed class DemandAsset { public long DemandId { get; private set; } public long AssetId { get; private set; } private DemandAsset() { } public DemandAsset(long d, long a) { DemandId = d; AssetId = a; } }
public sealed class DemandAssetEvent { public long DemandId { get; private set; } public long AssetEventId { get; private set; } private DemandAssetEvent() { } public DemandAssetEvent(long d, long a) { DemandId = d; AssetEventId = a; } }
public sealed class DemandBuilding { public long DemandId { get; private set; } public long BuildingId { get; private set; } private DemandBuilding() { } public DemandBuilding(long d, long b) { DemandId = d; BuildingId = b; } }
public sealed class DemandComplex { public long DemandId { get; private set; } public long ComplexId { get; private set; } private DemandComplex() { } public DemandComplex(long d, long c) { DemandId = d; ComplexId = c; } }
