namespace BuildingManagement.Domain;

public static class PartyReferenceKeys
{
    public static class PartyTypes
    {
        public const string IranianPerson = "iranian_person";
        public const string IranianOrganization = "iranian_organization";
        public const string ForeignPerson = "foreign_person";
        public const string ForeignOrganization = "foreign_organization";
    }

    public static class ContactTypes
    {
        public const string Mobile = "mobile";
        public const string Phone = "phone";
        public const string Email = "email";
    }

    public static class RelationTypes
    {
        public const string Owner = "owner";
        public const string Tenant = "tenant";
        public const string Resident = "resident";
        public const string LegalRepresentative = "legal_representative";
        public const string ContactPerson = "contact_person";
        public const string Other = "other";
    }
}

public sealed class PartyType : ReferenceDataItem
{
    private PartyType() { }
    public PartyType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class PartyContactType : ReferenceDataItem
{
    private PartyContactType() { }
    public PartyContactType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class UnitPartyRelationType : ReferenceDataItem
{
    private UnitPartyRelationType() { }
    public string? Description { get; private set; }
    public bool IsOwnershipRelation { get; private set; }
    public bool IsOccupancyRelation { get; private set; }

    public UnitPartyRelationType(string key, string title, string? description, bool isOwnershipRelation,
        bool isOccupancyRelation, int sortOrder, DateTimeOffset now)
    {
        Initialize(key, title, sortOrder, now);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsOwnershipRelation = isOwnershipRelation;
        IsOccupancyRelation = isOccupancyRelation;
    }
}

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

    private static string RequiredValue(string value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainValidationException(field, "Must not be blank.")
            : value.Trim();

    private static string? OptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnitPartyRelation
{
    private UnitPartyRelation() { }
    public long Id { get; private set; }
    public long UnitId { get; private set; }
    public long PartyId { get; private set; }
    public long UnitPartyRelationTypeId { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public UnitPartyRelation(long unitId, long partyId, long relationTypeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate, string? notes, DateTimeOffset now)
    {
        if (unitId <= 0) throw new DomainValidationException("unit", "Unit is required.");
        if (partyId <= 0) throw new DomainValidationException("party", "Party is required.");
        if (relationTypeId <= 0)
            throw new DomainValidationException("relationType", "Relation type is required.");
        UnitId = unitId;
        PartyId = partyId;
        UnitPartyRelationTypeId = relationTypeId;
        ValidateDates(startDate, endDate);
        StartDate = startDate?.ToUniversalTime();
        EndDate = endDate?.ToUniversalTime();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = now;
    }

    public void End(DateTimeOffset? endDate, DateTimeOffset now)
    {
        var terminationDate = (endDate ?? now).ToUniversalTime();
        ValidateDates(StartDate, terminationDate);
        EndDate = terminationDate;
        UpdatedAtUtc = now;
    }

    public void SoftDelete(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now;
    }

    private static void ValidateDates(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (startDate.HasValue && endDate < startDate)
            throw new DomainValidationException("endDate", "Cannot be earlier than start date.");
    }

}

public sealed class UnitOccupancyHistory
{
    private UnitOccupancyHistory() { }
    public long Id { get; private set; }
    public long UnitId { get; private set; }
    public int OccupantsCount { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public UnitOccupancyHistory(long unitId, int occupantsCount, DateTimeOffset? effectiveFrom,
        string? notes, DateTimeOffset now)
    {
        if (unitId <= 0) throw new DomainValidationException("unit", "Unit is required.");
        if (occupantsCount < 0) throw new DomainValidationException("occupantsCount", "Must not be negative.");
        UnitId = unitId;
        OccupantsCount = occupantsCount;
        EffectiveFrom = effectiveFrom?.ToUniversalTime();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = now;
    }

    public void Close(DateTimeOffset? effectiveTo, DateTimeOffset now)
    {
        if (effectiveTo.HasValue && EffectiveFrom.HasValue && effectiveTo < EffectiveFrom)
            throw new DomainValidationException("effectiveFrom", "Cannot be earlier than current occupancy effective date.");
        EffectiveTo = effectiveTo?.ToUniversalTime();
        IsActive = false;
        UpdatedAtUtc = now;
    }
}
