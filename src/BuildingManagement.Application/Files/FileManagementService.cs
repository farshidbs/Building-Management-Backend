using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class FileManagementService(
    IApplicationDbContext db,
    IFileStorage storage,
    FileStorageOptions options,
    TimeProvider clock)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<GalleryFileResponse> UploadBuildingGallery(
        string buildingCode, IncomingFile incoming, GalleryMetadataRequest metadata, CancellationToken ct)
    {
        var buildingId = await ActiveBuildingId(buildingCode, ct);
        var file = FileStoragePolicy.ValidateImage(incoming, options);
        var stored = await Store(incoming, file, "buildings", NormalizeCode(buildingCode), "gallery", ct);
        try
        {
            if (metadata.IsCover) await ClearBuildingCover(buildingId, null, ct);
            var relation = new BuildingGalleryFile(await UniqueCode(db.BuildingGalleryFiles, ct),
                buildingId, stored.Id, metadata.Title, metadata.Description, metadata.AltText,
                metadata.SortOrder, metadata.IsCover, Now);
            db.BuildingGalleryFiles.Add(relation);
            await Save(ct);
            return await BuildingGalleryProjection(db.BuildingGalleryFiles.Where(x => x.Id == relation.Id))
                .SingleAsync(ct);
        }
        catch
        {
            await RollbackStoredFile(stored, ct);
            throw;
        }
    }

    public async Task<GalleryFileResponse> UploadComplexGallery(
        string complexCode, IncomingFile incoming, GalleryMetadataRequest metadata, CancellationToken ct)
    {
        var complexId = await ActiveComplexId(complexCode, ct);
        var file = FileStoragePolicy.ValidateImage(incoming, options);
        var stored = await Store(incoming, file, "complexes", NormalizeCode(complexCode), "gallery", ct);
        try
        {
            if (metadata.IsCover) await ClearComplexCover(complexId, null, ct);
            var relation = new ComplexGalleryFile(await UniqueCode(db.ComplexGalleryFiles, ct),
                complexId, stored.Id, metadata.Title, metadata.Description, metadata.AltText,
                metadata.SortOrder, metadata.IsCover, Now);
            db.ComplexGalleryFiles.Add(relation);
            await Save(ct);
            return await ComplexGalleryProjection(db.ComplexGalleryFiles.Where(x => x.Id == relation.Id))
                .SingleAsync(ct);
        }
        catch
        {
            await RollbackStoredFile(stored, ct);
            throw;
        }
    }

    public async Task<IReadOnlyList<GalleryFileResponse>> GetBuildingGallery(
        string buildingCode, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct);
        return await BuildingGalleryProjection(db.BuildingGalleryFiles.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.IsActive)
            .OrderByDescending(x => x.IsCover).ThenBy(x => x.SortOrder).ThenBy(x => x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GalleryFileResponse>> GetComplexGallery(
        string complexCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        return await ComplexGalleryProjection(db.ComplexGalleryFiles.AsNoTracking()
            .Where(x => x.ComplexId == complexId && x.IsActive)
            .OrderByDescending(x => x.IsCover).ThenBy(x => x.SortOrder).ThenBy(x => x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<GalleryFileResponse> UpdateBuildingGallery(string buildingCode, string galleryCode,
        GalleryMetadataRequest request, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct);
        var relation = await db.BuildingGalleryFiles.SingleOrDefaultAsync(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("building_gallery");
        if (request.IsCover) await ClearBuildingCover(buildingId, relation.Id, ct);
        relation.Update(request.Title, request.Description, request.AltText, request.SortOrder,
            request.IsCover, Now);
        await Save(ct);
        return await BuildingGalleryProjection(db.BuildingGalleryFiles.Where(x => x.Id == relation.Id))
            .SingleAsync(ct);
    }

    public async Task<GalleryFileResponse> UpdateComplexGallery(string complexCode, string galleryCode,
        GalleryMetadataRequest request, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        var relation = await db.ComplexGalleryFiles.SingleOrDefaultAsync(x =>
            x.ComplexId == complexId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("complex_gallery");
        if (request.IsCover) await ClearComplexCover(complexId, relation.Id, ct);
        relation.Update(request.Title, request.Description, request.AltText, request.SortOrder,
            request.IsCover, Now);
        await Save(ct);
        return await ComplexGalleryProjection(db.ComplexGalleryFiles.Where(x => x.Id == relation.Id))
            .SingleAsync(ct);
    }

    public async Task DeleteBuildingGallery(string buildingCode, string galleryCode, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct);
        var relation = await db.BuildingGalleryFiles.SingleOrDefaultAsync(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("building_gallery");
        var storedFileId = relation.StoredFileId;
        db.BuildingGalleryFiles.Remove(relation);
        await Save(ct);
        await CleanupOrphan(storedFileId, ct);
    }

    public async Task DeleteComplexGallery(string complexCode, string galleryCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        var relation = await db.ComplexGalleryFiles.SingleOrDefaultAsync(x =>
            x.ComplexId == complexId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("complex_gallery");
        var storedFileId = relation.StoredFileId;
        db.ComplexGalleryFiles.Remove(relation);
        await Save(ct);
        await CleanupOrphan(storedFileId, ct);
    }

    public Task<DocumentFileResponse> UploadBuildingDocument(
        string buildingCode, IncomingFile incoming, DocumentMetadataRequest metadata, CancellationToken ct) =>
        UploadDocument(buildingCode, incoming, metadata, true, ct);

    public Task<DocumentFileResponse> UploadComplexDocument(
        string complexCode, IncomingFile incoming, DocumentMetadataRequest metadata, CancellationToken ct) =>
        UploadDocument(complexCode, incoming, metadata, false, ct);

    public async Task<IReadOnlyList<DocumentFileResponse>> GetBuildingDocuments(
        string buildingCode, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct);
        return await BuildingDocumentProjection(db.BuildingDocuments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.IsActive)
            .OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.CreatedAtUtc)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentFileResponse>> GetComplexDocuments(
        string complexCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        return await ComplexDocumentProjection(db.ComplexDocuments.AsNoTracking()
            .Where(x => x.ComplexId == complexId && x.IsActive)
            .OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.CreatedAtUtc)).ToListAsync(ct);
    }

    public async Task<DocumentFileResponse> GetBuildingDocument(
        string buildingCode, string documentCode, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct);
        return await BuildingDocumentProjection(db.BuildingDocuments.AsNoTracking().Where(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(documentCode) && x.IsActive))
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building_document");
    }

    public async Task<DocumentFileResponse> GetComplexDocument(
        string complexCode, string documentCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        return await ComplexDocumentProjection(db.ComplexDocuments.AsNoTracking().Where(x =>
            x.ComplexId == complexId && x.Code == NormalizeCode(documentCode) && x.IsActive))
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex_document");
    }

    public Task<DocumentFileResponse> UpdateBuildingDocument(string buildingCode, string documentCode,
        DocumentMetadataRequest request, CancellationToken ct) =>
        UpdateDocument(buildingCode, documentCode, request, true, ct);

    public Task<DocumentFileResponse> UpdateComplexDocument(string complexCode, string documentCode,
        DocumentMetadataRequest request, CancellationToken ct) =>
        UpdateDocument(complexCode, documentCode, request, false, ct);

    public Task DeleteBuildingDocument(string buildingCode, string documentCode, CancellationToken ct) =>
        DeleteDocument(buildingCode, documentCode, true, ct);

    public Task DeleteComplexDocument(string complexCode, string documentCode, CancellationToken ct) =>
        DeleteDocument(complexCode, documentCode, false, ct);

    public async Task<FileContentResponse> GetContent(string fileCode, CancellationToken ct)
    {
        var code = NormalizeCode(fileCode);
        var stored = await db.StoredFiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code && x.IsActive, ct)
            ?? throw AppException.NotFound("file");
        if (!await storage.ExistsAsync(stored.StorageKey, ct))
            throw AppException.NotFound("file");
        var content = await storage.OpenReadAsync(stored.StorageKey, ct)
            ?? throw AppException.NotFound("file");
        var inline = await db.BuildingGalleryFiles.AnyAsync(x => x.StoredFileId == stored.Id && x.IsActive, ct) ||
                     await db.ComplexGalleryFiles.AnyAsync(x => x.StoredFileId == stored.Id && x.IsActive, ct) ||
                     await db.AssetGalleryFiles.AnyAsync(x => x.StoredFileId == stored.Id && x.IsActive, ct);
        return new(content, stored.ContentType,
            FileStoragePolicy.SanitizeDownloadName(stored.OriginalFileName, stored.FileExtension), inline);
    }

    private async Task<DocumentFileResponse> UploadDocument(string ownerCode, IncomingFile incoming,
        DocumentMetadataRequest metadata, bool building, CancellationToken ct)
    {
        ValidateDocumentMetadata(metadata);
        var ownerId = building
            ? await ActiveBuildingId(ownerCode, ct)
            : await ActiveComplexId(ownerCode, ct);
        var type = await DocumentType(metadata.DocumentTypeKey, ct);
        var file = FileStoragePolicy.ValidateDocument(incoming, options);
        var stored = await Store(incoming, file, building ? "buildings" : "complexes", NormalizeCode(ownerCode),
            "documents", ct);
        try
        {
            if (building)
            {
                var document = new BuildingDocument(await UniqueCode(db.BuildingDocuments, ct),
                    ownerId, stored.Id, type.Id, metadata.Title, metadata.DocumentNumber,
                    metadata.DocumentDate, metadata.EffectiveFrom, metadata.ExpiresAt,
                    metadata.Description, metadata.IsConfidential, type.RequiresDocumentDate,
                    type.SupportsExpiration, Now);
                db.BuildingDocuments.Add(document);
                await Save(ct);
                return await BuildingDocumentProjection(db.BuildingDocuments.Where(x => x.Id == document.Id))
                    .SingleAsync(ct);
            }
            else
            {
                var document = new ComplexDocument(await UniqueCode(db.ComplexDocuments, ct),
                    ownerId, stored.Id, type.Id, metadata.Title, metadata.DocumentNumber,
                    metadata.DocumentDate, metadata.EffectiveFrom, metadata.ExpiresAt,
                    metadata.Description, metadata.IsConfidential, type.RequiresDocumentDate,
                    type.SupportsExpiration, Now);
                db.ComplexDocuments.Add(document);
                await Save(ct);
                return await ComplexDocumentProjection(db.ComplexDocuments.Where(x => x.Id == document.Id))
                    .SingleAsync(ct);
            }
        }
        catch
        {
            await RollbackStoredFile(stored, ct);
            throw;
        }
    }

    private async Task<DocumentFileResponse> UpdateDocument(string ownerCode, string documentCode,
        DocumentMetadataRequest request, bool building, CancellationToken ct)
    {
        ValidateDocumentMetadata(request);
        var type = await DocumentType(request.DocumentTypeKey, ct);
        if (building)
        {
            var ownerId = await BuildingId(ownerCode, false, ct);
            var document = await db.BuildingDocuments.SingleOrDefaultAsync(x =>
                x.BuildingId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("building_document");
            document.Update(type.Id, request.Title, request.DocumentNumber, request.DocumentDate,
                request.EffectiveFrom, request.ExpiresAt, request.Description, request.IsConfidential,
                type.RequiresDocumentDate, type.SupportsExpiration, Now);
            await Save(ct);
            return await BuildingDocumentProjection(db.BuildingDocuments.Where(x => x.Id == document.Id))
                .SingleAsync(ct);
        }
        else
        {
            var ownerId = await ComplexId(ownerCode, false, ct);
            var document = await db.ComplexDocuments.SingleOrDefaultAsync(x =>
                x.ComplexId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("complex_document");
            document.Update(type.Id, request.Title, request.DocumentNumber, request.DocumentDate,
                request.EffectiveFrom, request.ExpiresAt, request.Description, request.IsConfidential,
                type.RequiresDocumentDate, type.SupportsExpiration, Now);
            await Save(ct);
            return await ComplexDocumentProjection(db.ComplexDocuments.Where(x => x.Id == document.Id))
                .SingleAsync(ct);
        }
    }

    private async Task DeleteDocument(string ownerCode, string documentCode, bool building, CancellationToken ct)
    {
        long storedFileId;
        if (building)
        {
            var ownerId = await BuildingId(ownerCode, false, ct);
            var document = await db.BuildingDocuments.SingleOrDefaultAsync(x =>
                x.BuildingId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("building_document");
            storedFileId = document.StoredFileId;
            db.BuildingDocuments.Remove(document);
        }
        else
        {
            var ownerId = await ComplexId(ownerCode, false, ct);
            var document = await db.ComplexDocuments.SingleOrDefaultAsync(x =>
                x.ComplexId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("complex_document");
            storedFileId = document.StoredFileId;
            db.ComplexDocuments.Remove(document);
        }
        await Save(ct);
        await CleanupOrphan(storedFileId, ct);
    }

    private async Task<StoredFile> Store(IncomingFile incoming, ValidatedFile file,
        string ownerFolder, string ownerCode, string category, CancellationToken ct)
    {
        var storedFileName = $"{Guid.NewGuid():N}{file.Extension}";
        var storageKey = FileStoragePolicy.CreateStorageKey(ownerFolder, ownerCode, category, storedFileName);
        await storage.SaveAsync(new(storageKey, incoming.Content), ct);
        var stored = new StoredFile(await UniqueCode(db.StoredFiles, ct), file.OriginalFileName,
            storedFileName, storageKey, file.ContentType, file.Extension, file.Length, null,
            StorageProviders.Local, Now);
        db.StoredFiles.Add(stored);
        try
        {
            await Save(ct);
            return stored;
        }
        catch
        {
            await BestEffortDelete(storageKey, ct);
            throw;
        }
    }

    private async Task RollbackStoredFile(StoredFile stored, CancellationToken ct)
    {
        db.StoredFiles.Remove(stored);
        try { await db.SaveChangesAsync(ct); }
        catch { /* Preserve the original operation failure. The orphan remains inactive/diagnosable. */ }
        await BestEffortDelete(stored.StorageKey, ct);
    }

    private async Task CleanupOrphan(long storedFileId, CancellationToken ct)
    {
        var referenced =
            await db.BuildingGalleryFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await db.ComplexGalleryFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await db.BuildingDocuments.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await db.ComplexDocuments.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct);
        referenced = referenced ||
            await db.AssetGalleryFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await db.AssetDocuments.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await db.AssetEventFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct);
        if (referenced) return;

        var stored = await db.StoredFiles.SingleOrDefaultAsync(x => x.Id == storedFileId, ct);
        if (stored is null) return;
        stored.SetActivation(false, Now);
        await Save(ct);
        try
        {
            await storage.DeleteAsync(stored.StorageKey, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AppException(500, "file.cleanup_failed",
                "File metadata was deactivated, but physical cleanup failed.");
        }
    }

    private async Task BestEffortDelete(string storageKey, CancellationToken ct)
    {
        try { await storage.DeleteAsync(storageKey, ct); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Cleanup must not replace the original persistence failure.
        }
    }

    private async Task ClearBuildingCover(long buildingId, long? exceptId, CancellationToken ct)
    {
        var covers = await db.BuildingGalleryFiles.Where(x =>
            x.BuildingId == buildingId && x.IsActive && x.IsCover && x.Id != exceptId).ToListAsync(ct);
        foreach (var cover in covers) cover.RemoveCover(Now);
    }

    private async Task ClearComplexCover(long complexId, long? exceptId, CancellationToken ct)
    {
        var covers = await db.ComplexGalleryFiles.Where(x =>
            x.ComplexId == complexId && x.IsActive && x.IsCover && x.Id != exceptId).ToListAsync(ct);
        foreach (var cover in covers) cover.RemoveCover(Now);
    }

    private async Task<long> ActiveBuildingId(string code, CancellationToken ct) =>
        await BuildingId(code, true, ct);

    private async Task<long> ActiveComplexId(string code, CancellationToken ct) =>
        await ComplexId(code, true, ct);

    private async Task<long> BuildingId(string code, bool activeOnly, CancellationToken ct) =>
        await db.Buildings.Where(x => x.Code == NormalizeCode(code) && (!activeOnly || x.IsActive))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");

    private async Task<long> ComplexId(string code, bool activeOnly, CancellationToken ct) =>
        await db.Complexes.Where(x => x.Code == NormalizeCode(code) && (!activeOnly || x.IsActive))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");

    private async Task<DocumentType> DocumentType(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw Validation("documentTypeKey", "Document type is required.");
        var normalizedKey = key.Trim().ToLowerInvariant();
        return await db.DocumentTypes.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Key == normalizedKey && x.IsActive, ct)
            ?? throw AppException.NotFound("document_type");
    }

    private static void ValidateDocumentMetadata(DocumentMetadataRequest request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        if (string.IsNullOrWhiteSpace(request.DocumentTypeKey))
            throw Validation("documentTypeKey", "Document type is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw Validation("title", "Title is required.");
    }

    private static string NormalizeCode(string code) => PublicCode.Normalize(code);

    private static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct)
        where T : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new AppException(500, "code.generation_failed", "A unique public code could not be generated.");
    }

    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw AppException.Conflict("concurrency.conflict", "The resource changed since it was read.");
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("persistence.conflict", "The change conflicts with existing data.");
        }
    }

    private static AppException Validation(string field, string message) =>
        new(400, "validation.failed", "One or more validation errors occurred.",
            new Dictionary<string, string[]> { [field] = [message] });

    private IQueryable<GalleryFileResponse> BuildingGalleryProjection(IQueryable<BuildingGalleryFile> query) =>
        query.Select(x => new GalleryFileResponse(
            x.Code,
            db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            x.Title, x.Description, x.AltText, x.SortOrder, x.IsCover, x.IsActive,
            x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<GalleryFileResponse> ComplexGalleryProjection(IQueryable<ComplexGalleryFile> query) =>
        query.Select(x => new GalleryFileResponse(
            x.Code,
            db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            x.Title, x.Description, x.AltText, x.SortOrder, x.IsCover, x.IsActive,
            x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<DocumentFileResponse> BuildingDocumentProjection(IQueryable<BuildingDocument> query) =>
        query.Select(x => new DocumentFileResponse(
            x.Code,
            db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            db.DocumentTypes.Where(type => type.Id == x.DocumentTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Title, x.DocumentNumber, x.DocumentDate, x.EffectiveFrom, x.ExpiresAt, x.Description,
            x.IsConfidential, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<DocumentFileResponse> ComplexDocumentProjection(IQueryable<ComplexDocument> query) =>
        query.Select(x => new DocumentFileResponse(
            x.Code,
            db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            db.DocumentTypes.Where(type => type.Id == x.DocumentTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Title, x.DocumentNumber, x.DocumentDate, x.EffectiveFrom, x.ExpiresAt, x.Description,
            x.IsConfidential, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
}
