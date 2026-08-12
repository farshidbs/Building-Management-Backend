using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed partial class AssetManagementService
{
    public Task<AssetGalleryResponse> UploadGallery(string assetCode, IncomingFile file,
        GalleryMetadataRequest metadata, CancellationToken ct) => UploadGalleryCore(assetCode, file, metadata, ct);
    public async Task<IReadOnlyList<AssetGalleryResponse>> Gallery(string assetCode, CancellationToken ct)
    { var id = (await Entity(assetCode, ct)).Id; return await GalleryProjection(db.AssetGalleryFiles.AsNoTracking().Where(x => x.AssetId == id && x.IsActive).OrderBy(x => x.SortOrder)).ToListAsync(ct); }
    public async Task SetCover(string assetCode, string fileCode, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct); var storedId = await StoredId(fileCode, ct);
        await db.ExecuteInTransaction(async token =>
        {
            var target = await db.AssetGalleryFiles.SingleOrDefaultAsync(x => x.AssetId == asset.Id && x.StoredFileId == storedId && x.IsActive, token) ?? throw AppException.NotFound("asset_gallery");
            var covers = await db.AssetGalleryFiles.Where(x => x.AssetId == asset.Id && x.IsActive && x.IsCover && x.Id != target.Id).ToListAsync(token);
            foreach (var cover in covers) cover.RemoveCover(Now);
            target.Update(target.Title, target.Description, target.AltText, target.SortOrder, true, Now); await Save(token); return true;
        }, ct);
    }

    public async Task<AssetDocumentResponse> UploadDocument(string assetCode, IncomingFile file,
        DocumentMetadataRequest metadata, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct); var typeId = await RefId(db.DocumentTypes, metadata.DocumentTypeKey, "document_type", ct);
        var stored = await StageFile(asset.Code, "documents", null, file, false, ct);
        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                await Save(token);
                var entity = new AssetDocument(asset.Id, stored.Id, typeId, metadata.Title,
                    metadata.DocumentNumber, metadata.DocumentDate, metadata.EffectiveFrom,
                    metadata.ExpiresAt, metadata.Description, metadata.IsConfidential, Now);
                db.AssetDocuments.Add(entity);
                await Save(token);
                return await DocumentProjection(db.AssetDocuments.Where(x => x.Id == entity.Id)).SingleAsync(token);
            }, ct);
        }
        catch (Exception exception)
        {
            await CleanupFailedUpload(stored, exception, ct);
            throw;
        }
    }
    public async Task<IReadOnlyList<AssetDocumentResponse>> Documents(string assetCode, CancellationToken ct)
    { var id = (await Entity(assetCode, ct)).Id; return await DocumentProjection(db.AssetDocuments.AsNoTracking().Where(x => x.AssetId == id && x.IsActive).OrderByDescending(x => x.CreatedAtUtc)).ToListAsync(ct); }
    public async Task<AssetDocumentResponse> Document(string assetCode, string fileCode, CancellationToken ct)
    { var id = (await Entity(assetCode, ct)).Id; var storedId = await StoredId(fileCode, ct); return await DocumentProjection(db.AssetDocuments.AsNoTracking().Where(x => x.AssetId == id && x.StoredFileId == storedId && x.IsActive)).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("asset_document"); }

    public async Task<AssetFileResponse> UploadEventFile(string assetCode, long eventId, IncomingFile file,
        AssetFileMetadataRequest metadata, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct);
        var assetEvent = await EventEntity(asset.Id, eventId, ct);
        var stored = await StageFile(asset.Code, "events", eventId.ToString(System.Globalization.CultureInfo.InvariantCulture), file, false, ct);
        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                await Save(token);
                var entity = new AssetEventFile(assetEvent.Id, stored.Id, metadata.Title,
                    metadata.Description, Now);
                db.AssetEventFiles.Add(entity);
                await Save(token);
                return await EventFileProjection(db.AssetEventFiles.Where(x => x.Id == entity.Id)).SingleAsync(token);
            }, ct);
        }
        catch (Exception exception)
        {
            await CleanupFailedUpload(stored, exception, ct);
            throw;
        }
    }
    public async Task<IReadOnlyList<AssetFileResponse>> EventFiles(string assetCode, long eventId, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct);
        var assetEvent = await EventEntity(asset.Id, eventId, ct);
        return await EventFileProjection(db.AssetEventFiles.AsNoTracking().Where(f =>
            f.IsActive && f.AssetEventId == assetEvent.Id)).ToListAsync(ct);
    }

    public async Task RemoveFile(string assetCode, string kind, string fileCode, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct); var storedId = await StoredId(fileCode, ct);
        AssetFileRelation relation = kind switch
        {
            "gallery" => await db.AssetGalleryFiles.SingleOrDefaultAsync(x => x.AssetId == asset.Id && x.StoredFileId == storedId && x.IsActive, ct) ?? throw AppException.NotFound("asset_gallery"),
            "document" => await db.AssetDocuments.SingleOrDefaultAsync(x => x.AssetId == asset.Id && x.StoredFileId == storedId && x.IsActive, ct) ?? throw AppException.NotFound("asset_document"),
            _ => throw Validation("kind", "File relation kind is invalid.")
        };
        relation.Deactivate(Now); var stored = await db.StoredFiles.SingleAsync(x => x.Id == storedId, ct); stored.SetActivation(false, Now);
        await Save(ct); try { await storage.DeleteAsync(stored.StorageKey, ct); } catch (Exception exception) when (exception is not OperationCanceledException) { throw new AppException(500, "file.cleanup_failed", "File metadata was deactivated, but physical cleanup failed."); }
    }

    public async Task RemoveEventFile(string assetCode, long eventId, string fileCode, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct);
        var assetEvent = await EventEntity(asset.Id, eventId, ct);
        var storedId = await StoredId(fileCode, ct);
        var relation = await db.AssetEventFiles.SingleOrDefaultAsync(x =>
            x.AssetEventId == assetEvent.Id && x.StoredFileId == storedId && x.IsActive, ct)
            ?? throw AppException.NotFound("asset_event_file");
        relation.Deactivate(Now);
        var stored = await db.StoredFiles.SingleAsync(x => x.Id == storedId, ct);
        stored.SetActivation(false, Now);
        await Save(ct);
        try { await storage.DeleteAsync(stored.StorageKey, ct); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            PhysicalCleanupFailed(logger, fileCode, exception);
            throw new AppException(500, "file.cleanup_failed", "File metadata was deactivated, but physical cleanup failed.");
        }
    }

}
