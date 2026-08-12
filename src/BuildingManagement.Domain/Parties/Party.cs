namespace BuildingManagement.Domain;

public sealed class Party : Entity
{
    private Party() { }
    public long PartyTypeId { get; private set; }
    public string DisplayName { get; private set; } = "";
    public string NormalizedDisplayName { get; private set; } = "";
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? OrganizationName { get; private set; }
    public string? IdentityNumber { get; private set; }
    public string? Description { get; private set; }

    public Party(string code, long partyTypeId, string displayName, string? firstName, string? lastName,
        string? organizationName, string? identityNumber, string? description, DateTimeOffset now)
    {
        Initialize(code, now);
        Update(partyTypeId, displayName, firstName, lastName, organizationName, identityNumber,
            description, now);
        UpdatedAtUtc = null;
    }

    public void Update(long partyTypeId, string displayName, string? firstName, string? lastName,
        string? organizationName, string? identityNumber, string? description, DateTimeOffset now)
    {
        if (partyTypeId <= 0) throw new DomainValidationException("partyType", "Party type is required.");
        PartyTypeId = partyTypeId;
        DisplayName = Required(displayName, "displayName");
        NormalizedDisplayName = DisplayName.ToUpperInvariant();
        FirstName = Optional(firstName);
        LastName = Optional(lastName);
        OrganizationName = Optional(organizationName);
        IdentityNumber = Optional(identityNumber);
        Description = Optional(description);
        Touch(now);
    }
}
