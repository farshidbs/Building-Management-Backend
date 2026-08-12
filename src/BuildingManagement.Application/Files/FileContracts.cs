using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class FileStorageOptions
{
    public string Provider { get; init; } = StorageProviders.Local;
    public string LocalRootPath { get; init; } = "fileuploads";
    public long MaximumImageFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public long MaximumDocumentFileSizeBytes { get; init; } = 25 * 1024 * 1024;
    public string[] AllowedImageExtensions { get; init; } = [".jpg", ".jpeg", ".png", ".webp"];
    public string[] AllowedDocumentExtensions { get; init; } =
        [".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx"];
}

public sealed record StorageWriteRequest(string StorageKey, Stream Content);

public interface IFileStorage
{
    Task SaveAsync(StorageWriteRequest request, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed record IncomingFile(Stream Content, string OriginalFileName, string ContentType, long Length);

public sealed record GalleryMetadataRequest(
    string? Title,
    string? Description,
    string? AltText,
    int SortOrder = 0,
    bool IsCover = false);

public sealed record DocumentMetadataRequest(
    string DocumentTypeKey,
    string Title,
    string? DocumentNumber,
    DateTimeOffset? DocumentDate,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? ExpiresAt,
    string? Description,
    bool IsConfidential = false);

public sealed record StoredFileResponse(
    string Code,
    string ContentUrl,
    string OriginalFileName,
    string ContentType,
    string FileExtension,
    long FileSizeBytes);

public sealed record GalleryFileResponse(
    string Code,
    StoredFileResponse File,
    string? Title,
    string? Description,
    string? AltText,
    int SortOrder,
    bool IsCover,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record DocumentFileResponse(
    string Code,
    StoredFileResponse File,
    ReferenceValueResponse DocumentType,
    string Title,
    string? DocumentNumber,
    DateTimeOffset? DocumentDate,
    DateTimeOffset? EffectiveFrom,
    DateTimeOffset? ExpiresAt,
    string? Description,
    bool IsConfidential,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record FileContentResponse(
    Stream Content,
    string ContentType,
    string OriginalFileName,
    bool Inline);
