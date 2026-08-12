namespace BuildingManagement.Domain;

public sealed class PartyContact
{
    private PartyContact() { }
    public long Id { get; private set; }
    public long PartyId { get; private set; }
    public long PartyContactTypeId { get; private set; }
    public string Value { get; private set; } = "";
    public string NormalizedValue { get; private set; } = "";
    public string? Label { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public PartyContact(long partyId, long contactTypeId, string value, string normalizedValue,
        string? label, bool isPrimary, DateTimeOffset now)
    {
        if (partyId <= 0) throw new DomainValidationException("party", "Party is required.");
        PartyId = partyId;
        Update(contactTypeId, value, normalizedValue, label, isPrimary, now);
        CreatedAtUtc = now;
        UpdatedAtUtc = null;
    }

    public void Update(long contactTypeId, string value, string normalizedValue, string? label,
        bool isPrimary, DateTimeOffset now)
    {
        if (contactTypeId <= 0)
            throw new DomainValidationException("contactType", "Contact type is required.");
        PartyContactTypeId = contactTypeId;
        Value = RequiredValue(value, "value");
        NormalizedValue = RequiredValue(normalizedValue, "value");
        Label = OptionalValue(label);
        IsPrimary = isPrimary;
        UpdatedAtUtc = now;
    }

    public void Verify(DateTimeOffset now)
    {
        IsVerified = true;
        VerifiedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void SetPrimary(bool isPrimary, DateTimeOffset now)
    {
        if (IsPrimary == isPrimary) return;
        IsPrimary = isPrimary;
        UpdatedAtUtc = now;
    }

    public void SetActivation(bool isActive, DateTimeOffset now)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    private static string RequiredValue(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainValidationException(field, "Must not be blank.")
            : value.Trim();

    private static string? OptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
