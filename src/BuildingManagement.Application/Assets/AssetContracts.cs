namespace BuildingManagement.Application;

public sealed record AssetRequest(string AssetTypeKey, string? ComplexCode, string? BuildingCode,
    string Name, string? Brand, string? Model, string? SerialNumber, DateTimeOffset? InstallationDate,
    DateTimeOffset? PurchaseDate, int? SuggestedReviewIntervalDays, string? Description);

public sealed record AssetResponse(string Code, ReferenceValueResponse AssetType,
    ResourceReferenceResponse? Complex, ResourceReferenceResponse? Building, string Name, string? Brand,
    string? Model, string? SerialNumber, DateTimeOffset? InstallationDate, DateTimeOffset? PurchaseDate,
    int? SuggestedReviewIntervalDays, string? Description, DateTimeOffset? LastEventDate,
    DateTimeOffset? SuggestedNextReviewDate, bool IsActive, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
