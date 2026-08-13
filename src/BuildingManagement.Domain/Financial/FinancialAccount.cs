namespace BuildingManagement.Domain;

public sealed class FinancialAccount : FinancialRecord
{
    private FinancialAccount() { }
    public long? UnitId { get; private set; }
    public long? BuildingId { get; private set; }
    public long? ComplexId { get; private set; }
    public string AccountKindKey { get; private set; } = "";
    public decimal CurrentBalance { get; private set; }
    public decimal AvailableCredit { get; private set; }

    public FinancialAccount(long? unitId, long? buildingId, long? complexId, string kind, DateTimeOffset now)
    {
        if ((unitId.HasValue ? 1 : 0) + (buildingId.HasValue ? 1 : 0) + (complexId.HasValue ? 1 : 0) != 1)
            throw new DomainValidationException("owner", "Exactly one account owner is required.");
        if (unitId.HasValue && kind != FinancialKeys.AccountKinds.Unit)
            throw new DomainValidationException("accountKindKey", "A Unit account must use unit_account.");
        if (!unitId.HasValue && kind is not (FinancialKeys.AccountKinds.CurrentFund or FinancialKeys.AccountKinds.ReserveFund))
            throw new DomainValidationException("accountKindKey", "A Building or Complex account must use a Fund kind.");
        UnitId = unitId; BuildingId = buildingId; ComplexId = complexId;
        AccountKindKey = Required(kind, "accountKindKey"); CreatedAtUtc = now;
    }

    public void AddAvailableCredit(decimal amount, DateTimeOffset now) { Positive(amount); if (UnitId is null) throw new DomainValidationException("availableCredit", "Only Unit accounts may hold available credit."); AvailableCredit += amount; Touch(now); }
    public void ConsumeAvailableCredit(decimal amount, DateTimeOffset now) { Positive(amount); if (UnitId is null || amount > AvailableCredit) throw new DomainValidationException("availableCredit", "Insufficient Unit available credit."); AvailableCredit -= amount; Touch(now); }

    public decimal Apply(string effect, decimal amount, DateTimeOffset now)
    {
        Positive(amount);
        CurrentBalance = effect switch
        {
            FinancialKeys.Effects.Increase => CurrentBalance + amount,
            FinancialKeys.Effects.Decrease => CurrentBalance - amount,
            _ => throw new DomainValidationException("effectKey", "Effect is invalid.")
        };
        Touch(now); return CurrentBalance;
    }
}

public sealed class FinancialTransaction
{
    private FinancialTransaction() { }
    public long Id { get; private set; }
    public string TransactionTypeKey { get; private set; } = "";
    public long? DemandId { get; private set; }
    public long? PaymentId { get; private set; }
    public long? ExpenseDisbursementId { get; private set; }
    public long? AccountAdjustmentId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? Description { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public FinancialTransaction(string type, long? demandId, long? paymentId, long? disbursementId,
        long? adjustmentId, DateTimeOffset occurredAtUtc, string? description, DateTimeOffset now)
    {
        if ((demandId.HasValue ? 1 : 0) + (paymentId.HasValue ? 1 : 0) + (disbursementId.HasValue ? 1 : 0) + (adjustmentId.HasValue ? 1 : 0) != 1)
            throw new DomainValidationException("source", "Exactly one transaction source is required.");
        var sourceMatchesType = type switch
        {
            FinancialKeys.TransactionTypes.Demand => demandId.HasValue,
            FinancialKeys.TransactionTypes.Payment => paymentId.HasValue,
            FinancialKeys.TransactionTypes.ExpenseDisbursement => disbursementId.HasValue,
            FinancialKeys.TransactionTypes.AccountAdjustment => adjustmentId.HasValue,
            _ => false
        };
        if (!sourceMatchesType) throw new DomainValidationException("transactionTypeKey", "Transaction type must match its source.");
        TransactionTypeKey = string.IsNullOrWhiteSpace(type) ? throw new DomainValidationException("transactionTypeKey", "Required.") : type;
        DemandId = demandId; PaymentId = paymentId; ExpenseDisbursementId = disbursementId; AccountAdjustmentId = adjustmentId;
        OccurredAtUtc = occurredAtUtc.ToUniversalTime(); Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(); CreatedAtUtc = now;
    }
}

public sealed class FinancialTransactionEntry
{
    private FinancialTransactionEntry() { }
    public long Id { get; private set; }
    public long FinancialTransactionId { get; private set; }
    public long FinancialAccountId { get; private set; }
    public string EffectKey { get; private set; } = "";
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public FinancialTransactionEntry(long transactionId, long accountId, string effect, decimal amount, decimal balanceAfter, DateTimeOffset now)
    { if (transactionId <= 0 || accountId <= 0) throw new DomainValidationException("entry", "Transaction and account are required."); if (amount <= 0) throw new DomainValidationException("amount", "Must be greater than zero."); if (effect is not (FinancialKeys.Effects.Increase or FinancialKeys.Effects.Decrease)) throw new DomainValidationException("effectKey", "Effect is invalid."); FinancialTransactionId = transactionId; FinancialAccountId = accountId; EffectKey = effect; Amount = amount; BalanceAfter = balanceAfter; CreatedAtUtc = now; }
}
