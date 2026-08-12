namespace BuildingManagement.Application;

public sealed record UnitOccupancyRequest(string Status, int OccupantsCount, DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<UnitOnboardingRelationRequest>? Relations = null, string? Notes = null);

public sealed record OccupancyChangeRequest(int OccupantsCount, DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<UnitOnboardingRelationRequest>? OccupancyRelations = null, string? Notes = null);

public sealed record UnitOccupancyHistoryResponse(int OccupantsCount, DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo, string? Notes, bool IsActive);

public sealed record CurrentOccupancyResponse(string Status, int OccupantsCount);
