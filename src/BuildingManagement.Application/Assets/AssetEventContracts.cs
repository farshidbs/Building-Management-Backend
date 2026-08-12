namespace BuildingManagement.Application;

public sealed record AssetEventRequest(string EventTypeKey, DateTimeOffset EventDate, string Title,
    string? Description, DateTimeOffset? SuggestedNextDate, string? ServiceProviderPartyCode, decimal? Cost);

public sealed record AssetEventResponse(long Id, DateTimeOffset EventDate, ReferenceValueResponse EventType,
    string Title, string? Description, DateTimeOffset? SuggestedNextDate,
    ResourceReferenceResponse? ServiceProvider, decimal? Cost, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
