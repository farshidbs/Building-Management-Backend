using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed record ReferenceDataResponse(
    IReadOnlyList<ReferenceValueResponse> LocationTypes,
    IReadOnlyList<ReferenceValueResponse> BuildingTypes,
    IReadOnlyList<ReferenceValueResponse> UnitUsageTypes,
    IReadOnlyList<ReferenceValueResponse> UnitStatuses,
    IReadOnlyList<ReferenceValueResponse> DocumentTypes,
    IReadOnlyList<ReferenceValueResponse> PartyTypes,
    IReadOnlyList<ReferenceValueResponse> PartyContactTypes,
    IReadOnlyList<ReferenceValueResponse> UnitPartyRelationTypes);
public sealed record LocationRequest(string? ParentCode, string Name, string LocationTypeKey);
public sealed record LocationResponse(string Code, ResourceReferenceResponse? Parent, string Name, ReferenceValueResponse LocationType,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record ComplexRequest(string LocationCode, string Name, string Address, string PostalCode,
    decimal? Latitude, decimal? Longitude, string? Description);
public sealed record ComplexResponse(string Code, ResourceReferenceResponse Location, string Name, string Address,
    string PostalCode, decimal? Latitude, decimal? Longitude, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record BuildingRequest(string? ComplexCode, string LocationCode, string BuildingTypeKey,
    string Name, string Address, string PostalCode, decimal? Latitude, decimal? Longitude,
    int? FloorsCount, int? ConstructionYear, string? Description);
public sealed record BuildingResponse(string Code, ResourceReferenceResponse? Complex, ResourceReferenceResponse Location,
    ReferenceValueResponse BuildingType, string Name, string Address, string PostalCode,
    decimal? Latitude, decimal? Longitude, int? FloorsCount, int? ConstructionYear,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record UnitRequest(string UsageTypeKey, string StatusKey, string UnitNumber, int? FloorNumber,
    decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount, string? Description,
    UnitOccupancyRequest? Occupancy = null);
public sealed record UnitUpdateRequest(string UsageTypeKey, string StatusKey, string UnitNumber, int? FloorNumber,
    decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount, string? Description);
public sealed record UnitResponse(string Code, ResourceReferenceResponse Building, ResourceReferenceResponse? Complex,
    ReferenceValueResponse UsageType,
    ReferenceValueResponse Status, string UnitNumber, int? FloorNumber, decimal? Area, int? RoomsCount,
    int ParkingCount, int StorageCount, string? Description, CurrentOccupancyResponse CurrentOccupancy,
    bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record ActivationRequest(bool IsActive);
