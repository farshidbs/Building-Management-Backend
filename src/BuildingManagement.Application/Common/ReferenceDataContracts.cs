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

public sealed record ActivationRequest(bool IsActive);
