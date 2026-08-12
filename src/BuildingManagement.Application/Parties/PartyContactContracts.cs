namespace BuildingManagement.Application;

public sealed record PartyContactRequest(string ContactTypeKey, string Value, string? Label = null,
    bool IsPrimary = false);

public sealed record PartyContactSelectorRequest(string ContactTypeKey, string Value);

public sealed record PartyContactUpdateRequest(string ContactTypeKey, string CurrentValue,
    string Value, string? Label = null);

public sealed record PartyContactResponse(ReferenceValueResponse ContactType, string Value,
    string? Label, bool IsPrimary, bool IsVerified, bool IsActive);
