namespace BuildingManagement.Domain;

public static class AssetReferenceKeys
{
    public static class Types
    {
        public const string Elevator = "elevator";
        public const string WaterPump = "water_pump";
        public const string Boiler = "boiler";
        public const string Generator = "generator";
        public const string ParkingDoor = "parking_door";
        public const string FireAlarm = "fire_alarm";
        public const string FireExtinguisher = "fire_extinguisher";
        public const string Camera = "camera";
        public const string Other = "other";
    }

    public static class EventTypes
    {
        public const string Inspection = "inspection";
        public const string Maintenance = "maintenance";
        public const string Repair = "repair";
        public const string Replacement = "replacement";
        public const string Installation = "installation";
        public const string Incident = "incident";
        public const string Other = "other";
    }
}

public sealed class AssetType : ReferenceDataItem
{
    private AssetType() { }
    public AssetType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class AssetEventType : ReferenceDataItem
{
    private AssetEventType() { }
    public AssetEventType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class Asset : Entity
{
    private Asset() { }
    public long AssetTypeId { get; private set; }
    public long? ComplexId { get; private set; }
    public long? BuildingId { get; private set; }
    public string Name { get; private set; } = "";
    public string? Brand { get; private set; }
    public string? Model { get; private set; }
    public string? SerialNumber { get; private set; }
    public DateTimeOffset? InstallationDate { get; private set; }
    public DateTimeOffset? PurchaseDate { get; private set; }
    public int? SuggestedReviewIntervalDays { get; private set; }
    public string? Description { get; private set; }

    public Asset(string code, long assetTypeId, long? complexId, long? buildingId, string name,
        string? brand, string? model, string? serialNumber, DateTimeOffset? installationDate,
        DateTimeOffset? purchaseDate, int? suggestedReviewIntervalDays, string? description,
        DateTimeOffset now)
    {
        Initialize(code, now);
        Update(assetTypeId, complexId, buildingId, name, brand, model, serialNumber,
            installationDate, purchaseDate, suggestedReviewIntervalDays, description, now);
        UpdatedAtUtc = null;
    }

    public void Update(long assetTypeId, long? complexId, long? buildingId, string name,
        string? brand, string? model, string? serialNumber, DateTimeOffset? installationDate,
        DateTimeOffset? purchaseDate, int? suggestedReviewIntervalDays, string? description,
        DateTimeOffset now)
    {
        if (assetTypeId <= 0) throw new DomainValidationException("assetTypeKey", "Asset type is required.");
        if (complexId.HasValue == buildingId.HasValue)
            throw new DomainValidationException("scope", "Exactly one of complexCode or buildingCode is required.");
        if (suggestedReviewIntervalDays <= 0)
            throw new DomainValidationException("suggestedReviewIntervalDays", "Must be greater than zero.");
        AssetTypeId = assetTypeId;
        ComplexId = complexId;
        BuildingId = buildingId;
        Name = Required(name, "name");
        Brand = Optional(brand);
        Model = Optional(model);
        SerialNumber = Optional(serialNumber);
        InstallationDate = installationDate?.ToUniversalTime();
        PurchaseDate = purchaseDate?.ToUniversalTime();
        SuggestedReviewIntervalDays = suggestedReviewIntervalDays;
        Description = Optional(description);
        Touch(now);
    }
}

public sealed class AssetEvent
{
    private AssetEvent() { }
    public long Id { get; private set; }
    public long AssetId { get; private set; }
    public long AssetEventTypeId { get; private set; }
    public DateTimeOffset EventDate { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public DateTimeOffset? SuggestedNextDate { get; private set; }
    public long? ServiceProviderPartyId { get; private set; }
    public decimal? Cost { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public AssetEvent(long assetId, long eventTypeId, DateTimeOffset eventDate, string title,
        string? description, DateTimeOffset? suggestedNextDate, long? serviceProviderPartyId,
        decimal? cost, DateTimeOffset now)
    {
        if (assetId <= 0) throw new DomainValidationException("assetCode", "Asset is required.");
        AssetId = assetId;
        CreatedAtUtc = now;
        Update(eventTypeId, eventDate, title, description, suggestedNextDate, serviceProviderPartyId, cost, now);
        UpdatedAtUtc = null;
    }

    public void Update(long eventTypeId, DateTimeOffset eventDate, string title, string? description,
        DateTimeOffset? suggestedNextDate, long? serviceProviderPartyId, decimal? cost, DateTimeOffset now)
    {
        if (eventTypeId <= 0) throw new DomainValidationException("eventTypeKey", "Event type is required.");
        if (suggestedNextDate < eventDate)
            throw new DomainValidationException("suggestedNextDate", "Cannot be earlier than event date.");
        if (cost < 0) throw new DomainValidationException("cost", "Must not be negative.");
        AssetEventTypeId = eventTypeId;
        EventDate = eventDate.ToUniversalTime();
        Title = RequiredValue(title, "title");
        Description = OptionalValue(description);
        SuggestedNextDate = suggestedNextDate?.ToUniversalTime();
        ServiceProviderPartyId = serviceProviderPartyId;
        Cost = cost;
        UpdatedAtUtc = now;
    }

    public void SetActivation(bool active, DateTimeOffset now) { IsActive = active; UpdatedAtUtc = now; }
    private static string RequiredValue(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new DomainValidationException(field, "Must not be blank.") : value.Trim();
    private static string? OptionalValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public abstract class AssetFileRelation
{
    public long Id { get; protected set; }
    public long StoredFileId { get; protected set; }
    public bool IsActive { get; protected set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];
    public void Deactivate(DateTimeOffset now) { IsActive = false; UpdatedAtUtc = now; }
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class AssetGalleryFile : AssetFileRelation
{
    private AssetGalleryFile() { }
    public long AssetId { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }
    public AssetGalleryFile(long assetId, long storedFileId, string? title, string? description,
        string? altText, int sortOrder, bool isCover, DateTimeOffset now)
    {
        AssetId = assetId; StoredFileId = storedFileId; CreatedAtUtc = now;
        Update(title, description, altText, sortOrder, isCover, now); UpdatedAtUtc = null;
    }
    public void Update(string? title, string? description, string? altText, int sortOrder, bool cover, DateTimeOffset now)
    {
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        Title = Optional(title); Description = Optional(description); AltText = Optional(altText);
        SortOrder = sortOrder; IsCover = cover; UpdatedAtUtc = now;
    }
    public void RemoveCover(DateTimeOffset now) { if (IsCover) { IsCover = false; UpdatedAtUtc = now; } }
}

public sealed class AssetDocument : AssetFileRelation
{
    private AssetDocument() { }
    public long AssetId { get; private set; }
    public long DocumentTypeId { get; private set; }
    public string Title { get; private set; } = "";
    public string? DocumentNumber { get; private set; }
    public DateTimeOffset? DocumentDate { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? Description { get; private set; }
    public bool IsConfidential { get; private set; }
    public AssetDocument(long assetId, long storedFileId, long documentTypeId, string title,
        string? documentNumber, DateTimeOffset? documentDate, DateTimeOffset? effectiveFrom,
        DateTimeOffset? expiresAt, string? description, bool confidential, DateTimeOffset now)
    {
        AssetId = assetId; StoredFileId = storedFileId; CreatedAtUtc = now;
        Update(documentTypeId, title, documentNumber, documentDate, effectiveFrom, expiresAt, description, confidential, now);
        UpdatedAtUtc = null;
    }
    public void Update(long typeId, string title, string? number, DateTimeOffset? date,
        DateTimeOffset? from, DateTimeOffset? expires, string? description, bool confidential, DateTimeOffset now)
    {
        if (typeId <= 0) throw new DomainValidationException("documentTypeKey", "Document type is required.");
        if (expires < from) throw new DomainValidationException("expiresAt", "Cannot be earlier than effective date.");
        DocumentTypeId = typeId; Title = string.IsNullOrWhiteSpace(title) ? throw new DomainValidationException("title", "Must not be blank.") : title.Trim();
        DocumentNumber = Optional(number); DocumentDate = date?.ToUniversalTime(); EffectiveFrom = from?.ToUniversalTime();
        ExpiresAt = expires?.ToUniversalTime(); Description = Optional(description); IsConfidential = confidential; UpdatedAtUtc = now;
    }
}

public sealed class AssetEventFile : AssetFileRelation
{
    private AssetEventFile() { }
    public long AssetEventId { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public AssetEventFile(long eventId, long storedFileId, string? title, string? description, DateTimeOffset now)
    { AssetEventId = eventId; StoredFileId = storedFileId; Title = Optional(title); Description = Optional(description); CreatedAtUtc = now; }
}
