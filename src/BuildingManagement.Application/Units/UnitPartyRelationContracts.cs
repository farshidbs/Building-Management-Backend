namespace BuildingManagement.Application;

public sealed record UnitOnboardingRelationRequest(string RelationTypeKey, PartySelectionRequest Party,
    DateTimeOffset? StartDate = null, string? Notes = null);

public sealed record UnitPartyRelationResponse(string PartyCode, string DisplayName,
    ReferenceValueResponse RelationType, DateTimeOffset? StartDate, DateTimeOffset? EndDate,
    string? Notes, bool IsActive);
