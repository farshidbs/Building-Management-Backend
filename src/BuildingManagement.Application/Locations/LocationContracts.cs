namespace BuildingManagement.Application;

public sealed record LocationRequest(string? ParentCode, string Name, string LocationTypeKey);

public sealed record LocationResponse(string Code, ResourceReferenceResponse? Parent, string Name,
    ReferenceValueResponse LocationType, bool IsActive, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
