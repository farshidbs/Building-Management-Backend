namespace BuildingManagement.Domain;

public abstract class FinancialRecord
{
    public long Id { get; protected set; }
    public bool IsActive { get; protected set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];
    protected static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new DomainValidationException(field, "Must not be blank.") : value.Trim();
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    protected static void Positive(decimal amount, string field = "amount") { if (amount <= 0) throw new DomainValidationException(field, "Must be greater than zero."); }
    protected void Touch(DateTimeOffset now) => UpdatedAtUtc = now;
}
