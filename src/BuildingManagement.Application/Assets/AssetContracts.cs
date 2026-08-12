using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed record AssetRequest(string AssetTypeKey, string? ComplexCode, string? BuildingCode,
    string Name, string? Brand, string? Model, string? SerialNumber, DateTimeOffset? InstallationDate,
    DateTimeOffset? PurchaseDate, int? SuggestedReviewIntervalDays, string? Description);
public sealed record AssetEventRequest(string EventTypeKey, DateTimeOffset EventDate, string Title,
    string? Description, DateTimeOffset? SuggestedNextDate, string? ServiceProviderPartyCode, decimal? Cost);
public sealed record AssetResponse(string Code, ReferenceValueResponse AssetType,
    ResourceReferenceResponse? Complex, ResourceReferenceResponse? Building, string Name, string? Brand,
    string? Model, string? SerialNumber, DateTimeOffset? InstallationDate, DateTimeOffset? PurchaseDate,
    int? SuggestedReviewIntervalDays, string? Description, DateTimeOffset? LastEventDate,
    DateTimeOffset? SuggestedNextReviewDate, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record AssetEventResponse(long Id, DateTimeOffset EventDate, ReferenceValueResponse EventType, string Title,
    string? Description, DateTimeOffset? SuggestedNextDate, ResourceReferenceResponse? ServiceProvider,
    decimal? Cost, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record AssetFileMetadataRequest(string? Title, string? Description);
public sealed record AssetFileResponse(StoredFileResponse File, string? Title, string? Description,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record AssetGalleryResponse(StoredFileResponse File, string? Title, string? Description,
    string? AltText, int SortOrder, bool IsCover, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record AssetDocumentResponse(StoredFileResponse File, ReferenceValueResponse DocumentType,
    string Title, string? DocumentNumber, DateTimeOffset? DocumentDate, DateTimeOffset? EffectiveFrom,
    DateTimeOffset? ExpiresAt, string? Description, bool IsConfidential, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
