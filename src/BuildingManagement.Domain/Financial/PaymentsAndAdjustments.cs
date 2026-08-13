namespace BuildingManagement.Domain;

public sealed class PaymentMethod : ReferenceDataItem
{
    private PaymentMethod() { }
    public bool RequiresManagerApproval { get; private set; }
    public PaymentMethod(string key, string title, bool approval, int order, DateTimeOffset now) { Initialize(key, title, order, now); RequiresManagerApproval = approval; }
}

public sealed class Payment : Entity
{
    private Payment() { }
    public long UnitAccountId { get; private set; }
    public long ReceivingFundAccountId { get; private set; }
    public long? PayerPartyId { get; private set; }
    public long PaymentMethodId { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = FinancialKeys.Statuses.Draft; public DateTimeOffset? PaidAtUtc { get; private set; }
    public DateTimeOffset? SubmittedAtUtc { get; private set; }
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public DateTimeOffset? RejectedAtUtc { get; private set; }
    public string? GatewayReference { get; private set; }
    public string? BankTrackingCode { get; private set; }
    public string? Notes { get; private set; }
    public Payment(string code, long unitAccountId, long fundId, long? payerId, long methodId, decimal amount, string? gateway, string? tracking, string? notes, DateTimeOffset now) { if (unitAccountId <= 0 || fundId <= 0 || methodId <= 0 || amount <= 0) throw new DomainValidationException("amount", "Accounts, method and positive amount are required."); Initialize(code, now); UnitAccountId = unitAccountId; ReceivingFundAccountId = fundId; PayerPartyId = payerId; PaymentMethodId = methodId; Amount = amount; GatewayReference = Optional(gateway); BankTrackingCode = Optional(tracking); Notes = Optional(notes); }
    public void Submit(bool approvalRequired, DateTimeOffset now) { if (Status != FinancialKeys.Statuses.Draft) return; Status = approvalRequired ? FinancialKeys.Statuses.WaitingForApproval : FinancialKeys.Statuses.GatewayPending; SubmittedAtUtc = now; Touch(now); }
    public void Confirm(DateTimeOffset paidAt, DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Confirmed) return; if (Status is FinancialKeys.Statuses.Draft or FinancialKeys.Statuses.Rejected or FinancialKeys.Statuses.Failed) throw new DomainValidationException("status", "Payment cannot be confirmed."); Status = FinancialKeys.Statuses.Confirmed; PaidAtUtc = paidAt; ConfirmedAtUtc = now; Touch(now); }
    public void Reject(DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Confirmed) throw new DomainValidationException("status", "Confirmed payment cannot be rejected."); Status = FinancialKeys.Statuses.Rejected; RejectedAtUtc = now; Touch(now); }
}

public sealed class PaymentAllocation
{
    private PaymentAllocation() { }
    public long Id { get; private set; }
    public long PaymentId { get; private set; }
    public long UnitReceivableId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentAllocation(long paymentId, long receivableId, decimal amount) { if (amount <= 0) throw new DomainValidationException("amount", "Must be positive."); PaymentId = paymentId; UnitReceivableId = receivableId; Amount = amount; }
}
public sealed class PaymentEvidenceFile : FinancialRecord { private PaymentEvidenceFile() { } public long PaymentId { get; private set; } public long StoredFileId { get; private set; } public string? Title { get; private set; } public string? Description { get; private set; } public PaymentEvidenceFile(long p, long f, string? title, string? description, DateTimeOffset now) { PaymentId = p; StoredFileId = f; Title = Optional(title); Description = Optional(description); CreatedAtUtc = now; } }

public sealed class AccountAdjustment : Entity
{
    private AccountAdjustment() { }
    public long FinancialAccountId { get; private set; }
    public string AdjustmentTypeKey { get; private set; } = ""; public decimal Amount { get; private set; }
    public DateTimeOffset EffectiveDate { get; private set; }
    public string Reason { get; private set; } = ""; public string Status { get; private set; } = FinancialKeys.Statuses.Draft;
    public AccountAdjustment(string code, long accountId, string type, decimal amount, DateTimeOffset effective, string reason, DateTimeOffset now) { if (accountId <= 0 || amount <= 0) throw new DomainValidationException("amount", "Account and positive amount are required."); Initialize(code, now); FinancialAccountId = accountId; AdjustmentTypeKey = Required(type, "adjustmentTypeKey"); Amount = amount; EffectiveDate = effective; Reason = Required(reason, "reason"); }
    public void Finalize(DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Finalized) return; if (Status != FinancialKeys.Statuses.Draft) throw new DomainValidationException("status", "Adjustment cannot be finalized."); Status = FinancialKeys.Statuses.Finalized; Touch(now); }
}
