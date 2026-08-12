using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed record PartyRequest(string PartyTypeKey, string DisplayName, string? FirstName = null,
    string? LastName = null, string? OrganizationName = null, string? IdentityNumber = null,
    string? Description = null);

public sealed record PartyContactRequest(string ContactTypeKey, string Value, string? Label = null,
    bool IsPrimary = false);

public sealed record PartyContactSelectorRequest(string ContactTypeKey, string Value);

public sealed record PartyContactUpdateRequest(string ContactTypeKey, string CurrentValue,
    string Value, string? Label = null);

public sealed record NewPartyInput(PartyRequest Party,
    IReadOnlyList<PartyContactRequest>? Contacts = null);

public sealed record PartySelectionRequest(string? ExistingPartyCode, NewPartyInput? NewParty);

public sealed record UnitOnboardingRelationRequest(string RelationTypeKey, PartySelectionRequest Party,
    DateTimeOffset? StartDate = null, string? Notes = null);

public sealed record UnitOccupancyRequest(string Status, int OccupantsCount, DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<UnitOnboardingRelationRequest>? Relations = null, string? Notes = null);

public sealed record OccupancyChangeRequest(int OccupantsCount, DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<UnitOnboardingRelationRequest>? OccupancyRelations = null, string? Notes = null);

public sealed record PartyResponse(string Code, ReferenceValueResponse PartyType, string DisplayName,
    string? FirstName, string? LastName, string? OrganizationName, string? IdentityNumber,
    string? Description,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record PartySummaryResponse(string Code, ReferenceValueResponse PartyType,
    string DisplayName, string? FirstName, string? LastName, string? OrganizationName,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record PartyContactResponse(ReferenceValueResponse ContactType, string Value,
    string? Label, bool IsPrimary, bool IsVerified, bool IsActive);

public sealed record UnitPartyRelationResponse(string PartyCode, string DisplayName,
    ReferenceValueResponse RelationType, DateTimeOffset? StartDate, DateTimeOffset? EndDate,
    string? Notes, bool IsActive);

public sealed record UnitOccupancyHistoryResponse(int OccupantsCount, DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo, string? Notes, bool IsActive);

public sealed record CurrentOccupancyResponse(string Status, int OccupantsCount);
