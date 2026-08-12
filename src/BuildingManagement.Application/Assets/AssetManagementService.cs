using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed partial class AssetManagementService(IApplicationDbContext db, IFileStorage storage,
    FileStorageOptions options, TimeProvider clock, ILogger<AssetManagementService> logger)
{
    private static readonly Action<ILogger, string, Exception?> PhysicalCleanupFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "AssetPhysicalCleanupFailed"),
            "Physical cleanup failed for Asset file {FileReference}.");
    private static readonly Action<ILogger, string, string, Exception?> UploadCleanupFailed =
        LoggerMessage.Define<string, string>(LogLevel.Error, new EventId(2, "AssetUploadCleanupFailed"),
            "Cleanup failed for file {StorageKey} after upload persistence error {ErrorType}.");
    private DateTimeOffset Now => clock.GetUtcNow();

    private async Task<AssetGalleryResponse> UploadGalleryCore(string code, IncomingFile file, GalleryMetadataRequest metadata, CancellationToken ct)
    {
        var asset = await Entity(code, ct);
        var stored = await StageFile(asset.Code, "gallery", null, file, true, ct);
        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                await Save(token);
                if (metadata.IsCover) { var covers = await db.AssetGalleryFiles.Where(x => x.AssetId == asset.Id && x.IsActive && x.IsCover).ToListAsync(token); foreach (var cover in covers) cover.RemoveCover(Now); }
                var entity = new AssetGalleryFile(asset.Id, stored.Id, metadata.Title, metadata.Description, metadata.AltText, metadata.SortOrder, metadata.IsCover, Now); db.AssetGalleryFiles.Add(entity); await Save(token); return await GalleryProjection(db.AssetGalleryFiles.Where(x => x.Id == entity.Id)).SingleAsync(token);
            }, ct);
        }
        catch (Exception exception)
        {
            await CleanupFailedUpload(stored, exception, ct);
            throw;
        }
    }

    private async Task<StoredFile> StageFile(string assetCode, string category, string? childFolder,
        IncomingFile incoming, bool image, CancellationToken ct)
    {
        var validated = image ? FileStoragePolicy.ValidateImage(incoming, options) : FileStoragePolicy.ValidateDocument(incoming, options);
        var name = $"{Guid.NewGuid():N}{validated.Extension}";
        var key = childFolder is null
            ? FileStoragePolicy.CreateStorageKey("assets", assetCode, category, name)
            : FileStoragePolicy.CreateStorageKey("assets", assetCode, category, childFolder, name);
        await storage.SaveAsync(new(key, incoming.Content), ct); var stored = new StoredFile(await UniqueCode(db.StoredFiles, ct), validated.OriginalFileName, name, key, validated.ContentType, validated.Extension, validated.Length, null, StorageProviders.Local, Now);
        db.StoredFiles.Add(stored);
        return stored;
    }

    private async Task CleanupFailedUpload(StoredFile file, Exception operationException, CancellationToken ct)
    {
        foreach (var entry in db.AssetGalleryFiles.Local.Where(x => x.StoredFileId == file.Id).ToList())
            db.Detach(entry);
        foreach (var entry in db.AssetDocuments.Local.Where(x => x.StoredFileId == file.Id).ToList())
            db.Detach(entry);
        foreach (var entry in db.AssetEventFiles.Local.Where(x => x.StoredFileId == file.Id).ToList())
            db.Detach(entry);
        db.Detach(file);
        try { await storage.DeleteAsync(file.StorageKey, ct); }
        catch (Exception cleanupException) when (cleanupException is not OperationCanceledException)
        {
            UploadCleanupFailed(logger, file.StorageKey, operationException.GetType().Name,
                cleanupException);
        }
    }
    private async Task<Asset> Entity(string code, CancellationToken ct) => await db.Assets.SingleOrDefaultAsync(x => x.Code == Normalize(code), ct) ?? throw AppException.NotFound("asset");
    private async Task<AssetEvent> EventEntity(long assetId, long eventId, CancellationToken ct) =>
        await db.AssetEvents.SingleOrDefaultAsync(x => x.Id == eventId && x.AssetId == assetId, ct)
        ?? throw AppException.NotFound("asset_event");
    private async Task<long?> ComplexId(string? code, bool active, CancellationToken ct) => string.IsNullOrWhiteSpace(code) ? null : await db.Complexes.Where(x => x.Code == Normalize(code) && (!active || x.IsActive)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
    private async Task<long?> BuildingId(string? code, bool active, CancellationToken ct) => string.IsNullOrWhiteSpace(code) ? null : await db.Buildings.Where(x => x.Code == Normalize(code) && (!active || x.IsActive)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
    private async Task<long?> PartyId(string? code, CancellationToken ct) => string.IsNullOrWhiteSpace(code) ? null : await db.Parties.Where(x => x.Code == Normalize(code) && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");
    private async Task<long> StoredId(string code, CancellationToken ct) => await db.StoredFiles.Where(x => x.Code == Normalize(code) && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("file");
    private static async Task<long> RefId<T>(IQueryable<T> set, string key, string resource, CancellationToken ct)
        where T : ReferenceDataItem
    {
        if (string.IsNullOrWhiteSpace(key)) throw Validation("key", "Reference key is required.");
        var normalized = key.Trim().ToLowerInvariant();
        return await set.Where(x => x.Key == normalized && x.IsActive).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound(resource);
    }
    private static string Normalize(string code) => PublicCode.Normalize(code);
    private static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct) where T : Entity { for (var i = 0; i < 20; i++) { var code = PublicCode.Create(); if (!await set.AnyAsync(x => x.Code == code, ct)) return code; } throw new AppException(500, "code.generation_failed", "A unique public code could not be generated."); }
    private async Task Save(CancellationToken ct) { try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw AppException.Conflict("concurrency.conflict", "The resource changed since it was read."); } catch (DbUpdateException) { throw AppException.Conflict("persistence.conflict", "The change conflicts with existing data."); } }
    private static void Validate(AssetRequest? r) { if (r is null) throw Validation("request", "A request body is required."); if (string.IsNullOrWhiteSpace(r.AssetTypeKey)) throw Validation("assetTypeKey", "Asset type is required."); if (string.IsNullOrWhiteSpace(r.Name)) throw Validation("name", "Name is required."); if (string.IsNullOrWhiteSpace(r.ComplexCode) == string.IsNullOrWhiteSpace(r.BuildingCode)) throw Validation("scope", "Exactly one of complexCode or buildingCode is required."); }
    private static void Validate(AssetEventRequest? r)
    {
        if (r is null) throw Validation("request", "A request body is required.");
        if (string.IsNullOrWhiteSpace(r.EventTypeKey))
            throw Validation("eventTypeKey", "Event type is required.");
        if (r.EventDate == default)
            throw Validation("eventDate", "Event date is required.");
        if (string.IsNullOrWhiteSpace(r.Title)) throw Validation("title", "Title is required.");
    }
    private static AppException Validation(string field, string message) => new(400, "validation.failed", "One or more validation errors occurred.", new Dictionary<string, string[]> { [field] = [message] });

    private IQueryable<AssetResponse> Projection(IQueryable<Asset> q) => q.Select(x => new AssetResponse(x.Code,
        db.AssetTypes.Where(t => t.Id == x.AssetTypeId).Select(t => new ReferenceValueResponse(t.Key, t.Title)).Single(),
        x.ComplexId == null ? null : db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => new ResourceReferenceResponse(c.Code, c.Name)).Single(),
        x.BuildingId == null ? null : db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => new ResourceReferenceResponse(b.Code, b.Name)).Single(),
        x.Name, x.Brand, x.Model, x.SerialNumber, x.InstallationDate, x.PurchaseDate, x.SuggestedReviewIntervalDays, x.Description,
        db.AssetEvents.Where(e => e.AssetId == x.Id && e.IsActive).OrderByDescending(e => e.EventDate)
            .ThenByDescending(e => e.Id).Select(e => (DateTimeOffset?)e.EventDate).FirstOrDefault(),
        db.AssetEvents.Where(e => e.AssetId == x.Id && e.IsActive).OrderByDescending(e => e.EventDate)
            .ThenByDescending(e => e.Id).Select(e => e.SuggestedNextDate ??
                (x.SuggestedReviewIntervalDays == null ? null :
                    e.EventDate.AddDays(x.SuggestedReviewIntervalDays.Value))).FirstOrDefault(),
        x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private IQueryable<AssetEventResponse> EventProjection(IQueryable<AssetEvent> q) => q.Select(x => new AssetEventResponse(x.Id, x.EventDate, db.AssetEventTypes.Where(t => t.Id == x.AssetEventTypeId).Select(t => new ReferenceValueResponse(t.Key, t.Title)).Single(), x.Title, x.Description, x.SuggestedNextDate,
        x.ServiceProviderPartyId == null ? null : db.Parties.Where(p => p.Id == x.ServiceProviderPartyId).Select(p => new ResourceReferenceResponse(p.Code, p.DisplayName)).Single(), x.Cost, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private IQueryable<AssetGalleryResponse> GalleryProjection(IQueryable<AssetGalleryFile> q) =>
        q.Select(x => new AssetGalleryResponse(
            db.StoredFiles.Where(file => file.Id == x.StoredFileId).Select(file =>
                new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content",
                    file.OriginalFileName, file.ContentType, file.FileExtension, file.FileSizeBytes)).Single(),
            x.Title, x.Description, x.AltText, x.SortOrder, x.IsCover, x.IsActive,
            x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<AssetDocumentResponse> DocumentProjection(IQueryable<AssetDocument> q) =>
        q.Select(x => new AssetDocumentResponse(
            db.StoredFiles.Where(file => file.Id == x.StoredFileId).Select(file =>
                new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content",
                    file.OriginalFileName, file.ContentType, file.FileExtension, file.FileSizeBytes)).Single(),
            db.DocumentTypes.Where(t => t.Id == x.DocumentTypeId).Select(t =>
                new ReferenceValueResponse(t.Key, t.Title)).Single(), x.Title, x.DocumentNumber,
            x.DocumentDate, x.EffectiveFrom, x.ExpiresAt, x.Description, x.IsConfidential,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<AssetFileResponse> EventFileProjection(IQueryable<AssetEventFile> q) =>
        q.Select(x => new AssetFileResponse(
            db.StoredFiles.Where(file => file.Id == x.StoredFileId).Select(file =>
                new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content",
                    file.OriginalFileName, file.ContentType, file.FileExtension, file.FileSizeBytes)).Single(),
            x.Title, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
}
