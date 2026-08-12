namespace BuildingManagement.Application;

public sealed record ComplexRequest(string LocationCode, string Name, string Address, string PostalCode,
    decimal? Latitude, decimal? Longitude, string? Description);

public sealed record ComplexResponse(string Code, ResourceReferenceResponse Location, string Name, string Address,
    string PostalCode, decimal? Latitude, decimal? Longitude, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
