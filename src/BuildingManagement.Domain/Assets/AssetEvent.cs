namespace BuildingManagement.Domain;

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
