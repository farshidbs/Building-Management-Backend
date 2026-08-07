namespace BuildingManagement.Domain;

public static class PartyReferenceKeys
{
    public static class PartyTypes
    {
        public const string Person = "person";
        public const string Organization = "organization";
    }

    public static class ContactTypes
    {
        public const string Mobile = "mobile";
        public const string Phone = "phone";
        public const string Email = "email";
    }

    public static class IdentifierTypes
    {
        public const string NationalId = "national_id";
        public const string LegalEntityNationalId = "legal_entity_national_id";
        public const string PassportNumber = "passport_number";
        public const string ResidenceIdentifier = "residence_identifier";
        public const string Other = "other";
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
    public string? Description { get; private set; }

    public PartyType(string key, string title, string? description, int sortOrder, DateTimeOffset now)
    {
        Initialize(key, title, sortOrder, now);
        Description = OptionalValue(description);
    }

    private static string? OptionalValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class PartyContactType : ReferenceDataItem
{
    private PartyContactType() { }
    public PartyContactType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class PartyIdentifierType : ReferenceDataItem
{
    private PartyIdentifierType() { }
    public PartyIdentifierType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class UnitPartyRelationType : ReferenceDataItem
{
    private UnitPartyRelationType() { }
    public string? Description { get; private set; }
    public bool IsOwnershipRelation { get; private set; }
    public bool IsOccupancyRelation { get; private set; }
    public bool CanBePaymentContact { get; private set; }

    public UnitPartyRelationType(string key, string title, string? description, bool isOwnershipRelation,
        bool isOccupancyRelation, bool canBePaymentContact, int sortOrder, DateTimeOffset now)
    {
        Initialize(key, title, sortOrder, now);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsOwnershipRelation = isOwnershipRelation;
        IsOccupancyRelation = isOccupancyRelation;
        CanBePaymentContact = canBePaymentContact;
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
    public string? Description { get; private set; }

    public Party(string code, long partyTypeId, string displayName, string? firstName, string? lastName,
        string? organizationName, string? description, DateTimeOffset now)
    {
        Initialize(code, now);
        Update(partyTypeId, displayName, firstName, lastName, organizationName, description, now);
        UpdatedAtUtc = null;
    }

    public void Update(long partyTypeId, string displayName, string? firstName, string? lastName,
        string? organizationName, string? description, DateTimeOffset now)
    {
        if (partyTypeId <= 0) throw new DomainValidationException("partyType", "Party type is required.");
        PartyTypeId = partyTypeId;
        DisplayName = Required(displayName, "displayName");
        NormalizedDisplayName = DisplayName.ToUpperInvariant();
        FirstName = Optional(firstName);
        LastName = Optional(lastName);
        OrganizationName = Optional(organizationName);
        Description = Optional(description);
        Touch(now);
    }
}

public sealed class PartyContact : Entity
{
    private PartyContact() { }
    public long PartyId { get; private set; }
    public long PartyContactTypeId { get; private set; }
    public string Value { get; private set; } = "";
    public string NormalizedValue { get; private set; } = "";
    public string? Label { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public PartyContact(string code, long partyId, long contactTypeId, string value, string normalizedValue,
        string? label, bool isPrimary, DateTimeOffset now)
    {
        Initialize(code, now);
        PartyId = partyId;
        Update(contactTypeId, value, normalizedValue, label, isPrimary, now);
        UpdatedAtUtc = null;
    }

    public void Update(long contactTypeId, string value, string normalizedValue, string? label,
        bool isPrimary, DateTimeOffset now)
    {
        PartyContactTypeId = contactTypeId;
        Value = Required(value, "value");
        NormalizedValue = Required(normalizedValue, "value");
        Label = Optional(label);
        IsPrimary = isPrimary;
        Touch(now);
    }

    public void Verify(DateTimeOffset now)
    {
        IsVerified = true;
        VerifiedAtUtc = now;
        Touch(now);
    }
}

public sealed class PartyIdentifier : Entity
{
    private PartyIdentifier() { }
    public long PartyId { get; private set; }
    public long PartyIdentifierTypeId { get; private set; }
    public string CountryCode { get; private set; } = "";
    public string Value { get; private set; } = "";
    public string NormalizedValue { get; private set; } = "";
    public bool IsVerified { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }

    public PartyIdentifier(string code, long partyId, long identifierTypeId, string countryCode,
        string value, string normalizedValue, DateTimeOffset now)
    {
        Initialize(code, now);
        PartyId = partyId;
        PartyIdentifierTypeId = identifierTypeId;
        CountryCode = Required(countryCode, "countryCode").ToUpperInvariant();
        if (CountryCode.Length != 2)
            throw new DomainValidationException("countryCode", "Must be a two-letter country code.");
        Value = Required(value, "value");
        NormalizedValue = Required(normalizedValue, "value");
    }

    public void Verify(DateTimeOffset now)
    {
        IsVerified = true;
        VerifiedAtUtc = now;
        Touch(now);
    }
}

public sealed class UnitPartyRelation : Entity
{
    private UnitPartyRelation() { }
    public long UnitId { get; private set; }
    public long PartyId { get; private set; }
    public long UnitPartyRelationTypeId { get; private set; }
    public DateTimeOffset? StartDate { get; private set; }
    public DateTimeOffset? EndDate { get; private set; }
    public decimal? OwnershipShare { get; private set; }
    public bool IsPrimaryContact { get; private set; }
    public bool IsPaymentContact { get; private set; }
    public string? Notes { get; private set; }

    public UnitPartyRelation(string code, long unitId, long partyId, long relationTypeId,
        DateTimeOffset? startDate, DateTimeOffset? endDate, decimal? ownershipShare,
        bool isPrimaryContact, bool isPaymentContact, string? notes, DateTimeOffset now)
    {
        Initialize(code, now);
        UnitId = unitId;
        PartyId = partyId;
        UnitPartyRelationTypeId = relationTypeId;
        ValidateDates(startDate, endDate);
        ValidateShare(ownershipShare);
        StartDate = startDate?.ToUniversalTime();
        EndDate = endDate?.ToUniversalTime();
        OwnershipShare = ownershipShare;
        IsPrimaryContact = isPrimaryContact;
        IsPaymentContact = isPaymentContact;
        Notes = Optional(notes);
    }

    public void End(DateTimeOffset endDate, DateTimeOffset now)
    {
        ValidateDates(StartDate, endDate);
        EndDate = endDate.ToUniversalTime();
        SetActivation(false, now);
    }

    private static void ValidateDates(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {
        if (startDate.HasValue && endDate < startDate)
            throw new DomainValidationException("endDate", "Cannot be earlier than start date.");
    }

    private static void ValidateShare(decimal? ownershipShare)
    {
        if (ownershipShare is <= 0 or > 100)
            throw new DomainValidationException("ownershipShare", "Must be greater than zero and at most 100.");
    }
}

public sealed class UnitOccupancyHistory
{
    private UnitOccupancyHistory() { }
    public long Id { get; private set; }
    public long UnitId { get; private set; }
    public int OccupantsCount { get; private set; }
    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public UnitOccupancyHistory(long unitId, int occupantsCount, DateTimeOffset effectiveFrom,
        string? notes, DateTimeOffset now)
    {
        if (unitId <= 0) throw new DomainValidationException("unit", "Unit is required.");
        if (occupantsCount < 0) throw new DomainValidationException("occupantsCount", "Must not be negative.");
        UnitId = unitId;
        OccupantsCount = occupantsCount;
        EffectiveFrom = effectiveFrom.ToUniversalTime();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAtUtc = now;
    }

    public void Close(DateTimeOffset effectiveTo, DateTimeOffset now)
    {
        if (effectiveTo < EffectiveFrom)
            throw new DomainValidationException("effectiveFrom", "Cannot be earlier than current occupancy effective date.");
        EffectiveTo = effectiveTo.ToUniversalTime();
        IsActive = false;
        UpdatedAtUtc = now;
    }
}
