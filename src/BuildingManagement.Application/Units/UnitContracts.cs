namespace BuildingManagement.Application;

public sealed record UnitRequest(string UsageTypeKey, string StatusKey, string UnitNumber, int? FloorNumber,
    decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount, string? Description,
    UnitOccupancyRequest? Occupancy = null);

public sealed record UnitUpdateRequest(string UsageTypeKey, string StatusKey, string UnitNumber, int? FloorNumber,
    decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount, string? Description);

public sealed record UnitResponse(string Code, ResourceReferenceResponse Building,
    ResourceReferenceResponse? Complex, ReferenceValueResponse UsageType, ReferenceValueResponse Status,
    string UnitNumber, int? FloorNumber, decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount,
    string? Description, CurrentOccupancyResponse CurrentOccupancy, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
