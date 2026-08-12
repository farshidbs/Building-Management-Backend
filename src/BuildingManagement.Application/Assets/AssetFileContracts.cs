namespace BuildingManagement.Application;

public sealed record AssetFileMetadataRequest(string? Title, string? Description);

public sealed record AssetFileResponse(StoredFileResponse File, string? Title, string? Description,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record AssetGalleryResponse(StoredFileResponse File, string? Title, string? Description,
    string? AltText, int SortOrder, bool IsCover, bool IsActive, DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record AssetDocumentResponse(StoredFileResponse File, ReferenceValueResponse DocumentType,
    string Title, string? DocumentNumber, DateTimeOffset? DocumentDate, DateTimeOffset? EffectiveFrom,
    DateTimeOffset? ExpiresAt, string? Description, bool IsConfidential, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
