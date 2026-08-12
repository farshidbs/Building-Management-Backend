namespace BuildingManagement.Domain;

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
