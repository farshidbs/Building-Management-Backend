namespace BuildingManagement.Domain;

public sealed class ExpenseType
{
    private ExpenseType() { }
    public long Id { get; private set; }
    public string? Key { get; private set; }
    public string Title { get; private set; } = "";
    public long? ComplexId { get; private set; }
    public long? BuildingId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true; public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public ExpenseType(string? key, string title, long? complexId, long? buildingId, int sortOrder, DateTimeOffset now)
    { if (complexId.HasValue && buildingId.HasValue) throw new DomainValidationException("scope", "Only one custom scope is allowed."); if (!complexId.HasValue && !buildingId.HasValue && string.IsNullOrWhiteSpace(key)) throw new DomainValidationException("key", "Global expense type requires a key."); Key = string.IsNullOrWhiteSpace(key) ? null : key.Trim().ToLowerInvariant(); Title = string.IsNullOrWhiteSpace(title) ? throw new DomainValidationException("title", "Required.") : title.Trim(); ComplexId = complexId; BuildingId = buildingId; SortOrder = sortOrder; CreatedAtUtc = now; }
}

public sealed class Expense : Entity
{
    private Expense() { }
    public long? BuildingId { get; private set; }
    public long? ComplexId { get; private set; }
    public long ExpenseTypeId { get; private set; }
    public long? VendorPartyId { get; private set; }
    public string Title { get; private set; } = ""; public decimal Amount { get; private set; }
    public DateTimeOffset ExpenseDate { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public string? Description { get; private set; }
    public string Status { get; private set; } = FinancialKeys.Statuses.Draft; public DateTimeOffset? FinalizedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public Expense(string code, long? buildingId, long? complexId, long typeId, long? vendorId, string title, decimal amount, DateTimeOffset expenseDate, DateTimeOffset? dueDate, string? description, DateTimeOffset now)
    { Initialize(code, now); Update(buildingId, complexId, typeId, vendorId, title, amount, expenseDate, dueDate, description, now); UpdatedAtUtc = null; }
    public void Update(long? buildingId, long? complexId, long typeId, long? vendorId, string title, decimal amount, DateTimeOffset expenseDate, DateTimeOffset? dueDate, string? description, DateTimeOffset now)
    { if (Status != FinancialKeys.Statuses.Draft) throw new DomainValidationException("status", "Only draft expense can be edited."); if (buildingId.HasValue == complexId.HasValue) throw new DomainValidationException("scope", "Exactly one scope is required."); if (typeId <= 0 || amount <= 0) throw new DomainValidationException("amount", "Type and positive amount are required."); BuildingId = buildingId; ComplexId = complexId; ExpenseTypeId = typeId; VendorPartyId = vendorId; Title = Required(title, "title"); Amount = amount; ExpenseDate = expenseDate; DueDate = dueDate; Description = Optional(description); Touch(now); }
    public void Finalize(DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Finalized) return; if (Status != FinancialKeys.Statuses.Draft) throw new DomainValidationException("status", "Expense cannot be finalized."); Status = FinancialKeys.Statuses.Finalized; FinalizedAtUtc = now; Touch(now); }
    public void TouchFinancialState(DateTimeOffset now) { if (Status != FinancialKeys.Statuses.Finalized) throw new DomainValidationException("status", "Expense must be finalized."); Touch(now); }
    public void Cancel(DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Cancelled) return; Status = FinancialKeys.Statuses.Cancelled; CancelledAtUtc = now; Touch(now); }
}

public sealed class ExpenseDisbursement : Entity
{
    private ExpenseDisbursement() { }
    public long ExpenseId { get; private set; }
    public long FundAccountId { get; private set; }
    public long? PayeePartyId { get; private set; }
    public decimal Amount { get; private set; }
    public string PaymentMethodKey { get; private set; } = ""; public string Status { get; private set; } = FinancialKeys.Statuses.Draft; public DateTimeOffset? PaidAtUtc { get; private set; }
    public string? Notes { get; private set; }
    public ExpenseDisbursement(string code, long expenseId, long fundId, long? payeeId, decimal amount, string method, DateTimeOffset? paidAtUtc, string? notes, DateTimeOffset now)
    { if (expenseId <= 0 || fundId <= 0 || amount <= 0) throw new DomainValidationException("amount", "Expense, fund, and positive amount are required."); if (paidAtUtc > now) throw new DomainValidationException("paidAtUtc", "Payment time cannot be in the future."); Initialize(code, now); ExpenseId = expenseId; FundAccountId = fundId; PayeePartyId = payeeId; Amount = amount; PaymentMethodKey = Required(method, "paymentMethodKey"); PaidAtUtc = paidAtUtc?.ToUniversalTime(); Notes = Optional(notes); }
    public ExpenseDisbursement(string code, long expenseId, long fundId, long? payeeId, decimal amount, string method, string? notes, DateTimeOffset now) : this(code, expenseId, fundId, payeeId, amount, method, null, notes, now) { }
    public void Finalize(DateTimeOffset now) { if (Status == FinancialKeys.Statuses.Finalized) return; if (Status != FinancialKeys.Statuses.Draft) throw new DomainValidationException("status", "Disbursement cannot be finalized."); Status = FinancialKeys.Statuses.Finalized; PaidAtUtc ??= now; Touch(now); }
}

public sealed class ExpenseDocument : FinancialRecord { private ExpenseDocument() { } public long ExpenseId { get; private set; } public long StoredFileId { get; private set; } public string? Title { get; private set; } public string? Description { get; private set; } public ExpenseDocument(long expenseId, long fileId, string? title, string? description, DateTimeOffset now) { ExpenseId = expenseId; StoredFileId = fileId; Title = Optional(title); Description = Optional(description); CreatedAtUtc = now; } }
public sealed class ExpenseDisbursementFile : FinancialRecord { private ExpenseDisbursementFile() { } public long ExpenseDisbursementId { get; private set; } public long StoredFileId { get; private set; } public string? Title { get; private set; } public ExpenseDisbursementFile(long parentId, long fileId, string? title, DateTimeOffset now) { ExpenseDisbursementId = parentId; StoredFileId = fileId; Title = Optional(title); CreatedAtUtc = now; } }

public sealed class ExpenseAsset { public long ExpenseId { get; private set; } public long AssetId { get; private set; } private ExpenseAsset() { } public ExpenseAsset(long e, long a) { ExpenseId = e; AssetId = a; } }
public sealed class ExpenseAssetEvent { public long ExpenseId { get; private set; } public long AssetEventId { get; private set; } private ExpenseAssetEvent() { } public ExpenseAssetEvent(long e, long a) { ExpenseId = e; AssetEventId = a; } }
public sealed class ExpenseBuilding { public long ExpenseId { get; private set; } public long BuildingId { get; private set; } private ExpenseBuilding() { } public ExpenseBuilding(long e, long b) { ExpenseId = e; BuildingId = b; } }
public sealed class ExpenseComplex { public long ExpenseId { get; private set; } public long ComplexId { get; private set; } private ExpenseComplex() { } public ExpenseComplex(long e, long c) { ExpenseId = e; ComplexId = c; } }
