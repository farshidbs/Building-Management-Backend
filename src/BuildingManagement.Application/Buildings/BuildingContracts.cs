namespace BuildingManagement.Application;

public sealed record BuildingRequest(string? ComplexCode, string LocationCode, string BuildingTypeKey,
    string Name, string Address, string PostalCode, decimal? Latitude, decimal? Longitude,
    int? FloorsCount, int? ConstructionYear, string? Description);

public sealed record BuildingResponse(string Code, ResourceReferenceResponse? Complex,
    ResourceReferenceResponse Location, ReferenceValueResponse BuildingType, string Name, string Address,
    string PostalCode, decimal? Latitude, decimal? Longitude, int? FloorsCount, int? ConstructionYear,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
