namespace BuildingManagement.Application;

public sealed record FinancialAccountRequest(string? UnitCode, string? BuildingCode, string? ComplexCode, string AccountKindKey);
public sealed record FinancialAccountResponse(string OwnerCode, string AccountKindKey, decimal CurrentBalance, bool IsActive);
public sealed record FinancialEntryResponse(string TransactionTypeKey, string EffectKey, decimal Amount, decimal BalanceAfter, DateTimeOffset OccurredAtUtc, string? Description);
public sealed record AccountAdjustmentRequest(string? AccountUnitCode, string? AccountBuildingCode,
    string? AccountComplexCode, string AccountKindKey, string? FundBuildingCode,
    string? FundComplexCode, string? FundAccountKindKey, string AdjustmentTypeKey, decimal Amount,
    DateTimeOffset EffectiveDate, string Reason, string? ResponsiblePartyTypeKey = null,
    string? ResponsiblePartyCode = null);
public sealed record AccountAdjustmentResponse(string Code, string Status, decimal Amount, string AdjustmentTypeKey);

public sealed record AssetEventLinkRequest(string AssetCode, long EventId);
public sealed record ExpenseRequest(string? BuildingCode, string? ComplexCode, long ExpenseTypeId,
    string? VendorPartyCode, string Title, decimal Amount, DateTimeOffset ExpenseDate,
    DateTimeOffset? DueDate, string? Description, IReadOnlyList<string>? AssetCodes = null,
    IReadOnlyList<AssetEventLinkRequest>? AssetEvents = null,
    IReadOnlyList<string>? RelatedBuildingCodes = null,
    IReadOnlyList<string>? RelatedComplexCodes = null);
public sealed record ExpenseTypeRequest(string? BuildingCode, string? ComplexCode, string Title, int SortOrder = 100);
public sealed record ExpenseTypeResponse(long Id, string? Key, string Title, string? BuildingCode,
    string? ComplexCode, bool IsActive);
public sealed record ExpenseResponse(string Code, string Title, decimal Amount, decimal PaidAmount, decimal RemainingAmount, string Status, DateTimeOffset ExpenseDate, DateTimeOffset? DueDate);
public sealed record ExpenseDisbursementRequest(string? FundBuildingCode, string? FundComplexCode,
    string FundAccountKindKey, string? PayeePartyCode, decimal Amount, string PaymentMethodKey, string? Notes);
public sealed record ExpenseDisbursementResponse(string Code, string ExpenseCode, decimal Amount, string Status, DateTimeOffset? PaidAtUtc);

public sealed record DemandExpenseLinkRequest(string ExpenseCode, decimal? RelatedAmount = null);
public sealed record DemandRequest(string? FundBuildingCode, string? FundComplexCode,
    string FundAccountKindKey, string DemandTypeKey,
    string Title, string? Description, DateTimeOffset DemandDate, DateTimeOffset? DueDate,
    DemandRuleRequest Rule, IReadOnlyList<DemandExpenseLinkRequest>? Expenses = null,
    IReadOnlyList<string>? AssetCodes = null, IReadOnlyList<AssetEventLinkRequest>? AssetEvents = null,
    IReadOnlyList<string>? RelatedBuildingCodes = null,
    IReadOnlyList<string>? RelatedComplexCodes = null);
public sealed record DemandRuleRequest(string AllocationMethodKey, string AmountModeKey, decimal? TotalAmount, decimal? RateAmount, bool? IncludeVacantUnits, string ResponsiblePartyTypeKey, string RedistributionPolicyKey, string? Notes);
public sealed record UpdateDemandDraftRequest(string Title, string? Description, DateTimeOffset DemandDate, DateTimeOffset? DueDate, DemandRuleRequest Rule);
public sealed record DemandOverrideRequest(string UnitCode, bool IsIncluded, decimal? FinalAmount, string? AdjustmentReason);
public sealed record DemandPreviewRequest(IReadOnlyList<DemandOverrideRequest>? Overrides = null);
public sealed record DemandAllocationResponse(string UnitCode, decimal? BasisValue, decimal CalculatedAmount, decimal FinalAmount, bool IsIncluded, string ResponsiblePartyTypeKey, string? ResponsiblePartyCode, string? AdjustmentReason);
public sealed record DemandPreviewResponse(IReadOnlyList<DemandAllocationResponse> Items, decimal CalculatedTotal, decimal FinalTotal, decimal Difference);
public sealed record DemandResponse(string Code, string Title, string Status, DateTimeOffset DemandDate, DateTimeOffset? DueDate, DemandPreviewResponse? Allocations);

public sealed record PaymentAllocationRequest(string ReceivableCode, decimal Amount);
public sealed record PaymentRequest(string UnitCode, string? FundBuildingCode, string? FundComplexCode,
    string FundAccountKindKey, string? PayerPartyCode, string PaymentMethodKey, decimal Amount,
    string? BankTrackingCode, string? Notes, IReadOnlyList<PaymentAllocationRequest> Allocations);
public sealed record PaymentResponse(string Code, decimal Amount, string Status, string? PayerPartyCode, DateTimeOffset? ConfirmedAtUtc);
public sealed record ReceivableResponse(string Code, string? DemandCode, string? AccountAdjustmentCode,
    string UnitCode, decimal OriginalAmount,
    decimal OutstandingAmount, string Status, string ResponsiblePartyTypeKey,
    string? ResponsiblePartyCode, DateTimeOffset? DueDate);
public sealed record FinancialFileResponse(StoredFileResponse File, string? Title, string? Description);
