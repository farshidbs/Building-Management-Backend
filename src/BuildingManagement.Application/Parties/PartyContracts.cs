namespace BuildingManagement.Application;

public sealed record PartyRequest(string PartyTypeKey, string DisplayName, string? FirstName = null,
    string? LastName = null, string? OrganizationName = null, string? IdentityNumber = null,
    string? Description = null);

public sealed record NewPartyInput(PartyRequest Party,
    IReadOnlyList<PartyContactRequest>? Contacts = null);

public sealed record PartySelectionRequest(string? ExistingPartyCode, NewPartyInput? NewParty);

public sealed record PartyResponse(string Code, ReferenceValueResponse PartyType, string DisplayName,
    string? FirstName, string? LastName, string? OrganizationName, string? IdentityNumber,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record PartySummaryResponse(string Code, ReferenceValueResponse PartyType,
    string DisplayName, string? FirstName, string? LastName, string? OrganizationName,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
