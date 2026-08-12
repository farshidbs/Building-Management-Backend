using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public abstract class FileManagementServiceBase(
    IApplicationDbContext db,
    IFileStorage storage,
    FileStorageOptions options,
    TimeProvider clock)
{
    protected IApplicationDbContext Db { get; } = db;
    protected IFileStorage Storage { get; } = storage;
    protected FileStorageOptions Options { get; } = options;
    protected DateTimeOffset Now => clock.GetUtcNow();

    protected async Task<DocumentFileResponse> UploadDocument(string ownerCode, IncomingFile incoming,
        DocumentMetadataRequest metadata, bool building, CancellationToken ct)
    {
        ValidateDocumentMetadata(metadata);
        var ownerId = building
            ? await ActiveBuildingId(ownerCode, ct)
            : await ActiveComplexId(ownerCode, ct);
        var type = await DocumentType(metadata.DocumentTypeKey, ct);
        var file = FileStoragePolicy.ValidateDocument(incoming, Options);
        var stored = await Store(incoming, file, building ? "buildings" : "complexes", NormalizeCode(ownerCode),
            "documents", ct);
        try
        {
            if (building)
            {
                var document = new BuildingDocument(await UniqueCode(Db.BuildingDocuments, ct),
                    ownerId, stored.Id, type.Id, metadata.Title, metadata.DocumentNumber,
                    metadata.DocumentDate, metadata.EffectiveFrom, metadata.ExpiresAt,
                    metadata.Description, metadata.IsConfidential, type.RequiresDocumentDate,
                    type.SupportsExpiration, Now);
                Db.BuildingDocuments.Add(document);
                await Save(ct);
                return await BuildingDocumentProjection(Db.BuildingDocuments.Where(x => x.Id == document.Id))
                    .SingleAsync(ct);
            }
            else
            {
                var document = new ComplexDocument(await UniqueCode(Db.ComplexDocuments, ct),
                    ownerId, stored.Id, type.Id, metadata.Title, metadata.DocumentNumber,
                    metadata.DocumentDate, metadata.EffectiveFrom, metadata.ExpiresAt,
                    metadata.Description, metadata.IsConfidential, type.RequiresDocumentDate,
                    type.SupportsExpiration, Now);
                Db.ComplexDocuments.Add(document);
                await Save(ct);
                return await ComplexDocumentProjection(Db.ComplexDocuments.Where(x => x.Id == document.Id))
                    .SingleAsync(ct);
            }
        }
        catch
        {
            await RollbackStoredFile(stored, ct);
            throw;
        }
    }

    protected async Task<DocumentFileResponse> UpdateDocument(string ownerCode, string documentCode,
        DocumentMetadataRequest request, bool building, CancellationToken ct)
    {
        ValidateDocumentMetadata(request);
        var type = await DocumentType(request.DocumentTypeKey, ct);
        if (building)
        {
            var ownerId = await BuildingId(ownerCode, false, ct);
            var document = await Db.BuildingDocuments.SingleOrDefaultAsync(x =>
                x.BuildingId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("building_document");
            document.Update(type.Id, request.Title, request.DocumentNumber, request.DocumentDate,
                request.EffectiveFrom, request.ExpiresAt, request.Description, request.IsConfidential,
                type.RequiresDocumentDate, type.SupportsExpiration, Now);
            await Save(ct);
            return await BuildingDocumentProjection(Db.BuildingDocuments.Where(x => x.Id == document.Id))
                .SingleAsync(ct);
        }
        else
        {
            var ownerId = await ComplexId(ownerCode, false, ct);
            var document = await Db.ComplexDocuments.SingleOrDefaultAsync(x =>
                x.ComplexId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("complex_document");
            document.Update(type.Id, request.Title, request.DocumentNumber, request.DocumentDate,
                request.EffectiveFrom, request.ExpiresAt, request.Description, request.IsConfidential,
                type.RequiresDocumentDate, type.SupportsExpiration, Now);
            await Save(ct);
            return await ComplexDocumentProjection(Db.ComplexDocuments.Where(x => x.Id == document.Id))
                .SingleAsync(ct);
        }
    }

    protected async Task DeleteDocument(string ownerCode, string documentCode, bool building, CancellationToken ct)
    {
        long storedFileId;
        if (building)
        {
            var ownerId = await BuildingId(ownerCode, false, ct);
            var document = await Db.BuildingDocuments.SingleOrDefaultAsync(x =>
                x.BuildingId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("building_document");
            storedFileId = document.StoredFileId;
            Db.BuildingDocuments.Remove(document);
        }
        else
        {
            var ownerId = await ComplexId(ownerCode, false, ct);
            var document = await Db.ComplexDocuments.SingleOrDefaultAsync(x =>
                x.ComplexId == ownerId && x.Code == NormalizeCode(documentCode), ct)
                ?? throw AppException.NotFound("complex_document");
            storedFileId = document.StoredFileId;
            Db.ComplexDocuments.Remove(document);
        }
        await Save(ct);
        await CleanupOrphan(storedFileId, ct);
    }

    protected async Task<StoredFile> Store(IncomingFile incoming, ValidatedFile file,
        string ownerFolder, string ownerCode, string category, CancellationToken ct)
    {
        var storedFileName = $"{Guid.NewGuid():N}{file.Extension}";
        var storageKey = FileStoragePolicy.CreateStorageKey(ownerFolder, ownerCode, category, storedFileName);
        await Storage.SaveAsync(new(storageKey, incoming.Content), ct);
        var stored = new StoredFile(await UniqueCode(Db.StoredFiles, ct), file.OriginalFileName,
            storedFileName, storageKey, file.ContentType, file.Extension, file.Length, null,
            StorageProviders.Local, Now);
        Db.StoredFiles.Add(stored);
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

    protected async Task RollbackStoredFile(StoredFile stored, CancellationToken ct)
    {
        Db.StoredFiles.Remove(stored);
        try { await Db.SaveChangesAsync(ct); }
        catch { /* Preserve the original operation failure. The orphan remains inactive/diagnosable. */ }
        await BestEffortDelete(stored.StorageKey, ct);
    }

    protected async Task CleanupOrphan(long storedFileId, CancellationToken ct)
    {
        var referenced =
            await Db.BuildingGalleryFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await Db.ComplexGalleryFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await Db.BuildingDocuments.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await Db.ComplexDocuments.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct);
        referenced = referenced ||
            await Db.AssetGalleryFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await Db.AssetDocuments.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct) ||
            await Db.AssetEventFiles.AnyAsync(x => x.StoredFileId == storedFileId && x.IsActive, ct);
        if (referenced) return;

        var stored = await Db.StoredFiles.SingleOrDefaultAsync(x => x.Id == storedFileId, ct);
        if (stored is null) return;
        stored.SetActivation(false, Now);
        await Save(ct);
        try
        {
            await Storage.DeleteAsync(stored.StorageKey, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AppException(500, "file.cleanup_failed",
                "File metadata was deactivated, but physical cleanup failed.");
        }
    }

    protected async Task BestEffortDelete(string storageKey, CancellationToken ct)
    {
        try { await Storage.DeleteAsync(storageKey, ct); }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Cleanup must not replace the original persistence failure.
        }
    }

    protected async Task ClearBuildingCover(long buildingId, long? exceptId, CancellationToken ct)
    {
        var covers = await Db.BuildingGalleryFiles.Where(x =>
            x.BuildingId == buildingId && x.IsActive && x.IsCover && x.Id != exceptId).ToListAsync(ct);
        foreach (var cover in covers) cover.RemoveCover(Now);
    }

    protected async Task ClearComplexCover(long complexId, long? exceptId, CancellationToken ct)
    {
        var covers = await Db.ComplexGalleryFiles.Where(x =>
            x.ComplexId == complexId && x.IsActive && x.IsCover && x.Id != exceptId).ToListAsync(ct);
        foreach (var cover in covers) cover.RemoveCover(Now);
    }

    protected async Task<long> ActiveBuildingId(string code, CancellationToken ct) =>
        await BuildingId(code, true, ct);

    protected async Task<long> ActiveComplexId(string code, CancellationToken ct) =>
        await ComplexId(code, true, ct);

    protected async Task<long> BuildingId(string code, bool activeOnly, CancellationToken ct) =>
        await Db.Buildings.Where(x => x.Code == NormalizeCode(code) && (!activeOnly || x.IsActive))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");

    protected async Task<long> ComplexId(string code, bool activeOnly, CancellationToken ct) =>
        await Db.Complexes.Where(x => x.Code == NormalizeCode(code) && (!activeOnly || x.IsActive))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");

    protected async Task<DocumentType> DocumentType(string key, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw Validation("documentTypeKey", "Document type is required.");
        var normalizedKey = key.Trim().ToLowerInvariant();
        return await Db.DocumentTypes.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Key == normalizedKey && x.IsActive, ct)
            ?? throw AppException.NotFound("document_type");
    }

    protected static void ValidateDocumentMetadata(DocumentMetadataRequest request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        if (string.IsNullOrWhiteSpace(request.DocumentTypeKey))
            throw Validation("documentTypeKey", "Document type is required.");
        if (string.IsNullOrWhiteSpace(request.Title))
            throw Validation("title", "Title is required.");
    }

    protected static string NormalizeCode(string code) => PublicCode.Normalize(code);

    protected static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct)
        where T : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new AppException(500, "code.generation_failed", "A unique public code could not be generated.");
    }

    protected async Task Save(CancellationToken ct)
    {
        try { await Db.SaveChangesAsync(ct); }
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

    protected IQueryable<GalleryFileResponse> BuildingGalleryProjection(IQueryable<BuildingGalleryFile> query) =>
        query.Select(x => new GalleryFileResponse(
            x.Code,
            Db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            x.Title, x.Description, x.AltText, x.SortOrder, x.IsCover, x.IsActive,
            x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<GalleryFileResponse> ComplexGalleryProjection(IQueryable<ComplexGalleryFile> query) =>
        query.Select(x => new GalleryFileResponse(
            x.Code,
            Db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            x.Title, x.Description, x.AltText, x.SortOrder, x.IsCover, x.IsActive,
            x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<DocumentFileResponse> BuildingDocumentProjection(IQueryable<BuildingDocument> query) =>
        query.Select(x => new DocumentFileResponse(
            x.Code,
            Db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            Db.DocumentTypes.Where(type => type.Id == x.DocumentTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Title, x.DocumentNumber, x.DocumentDate, x.EffectiveFrom, x.ExpiresAt, x.Description,
            x.IsConfidential, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<DocumentFileResponse> ComplexDocumentProjection(IQueryable<ComplexDocument> query) =>
        query.Select(x => new DocumentFileResponse(
            x.Code,
            Db.StoredFiles.Where(file => file.Id == x.StoredFileId)
                .Select(file => new StoredFileResponse(file.Code, "/api/v1/files/" + file.Code + "/content", file.OriginalFileName, file.ContentType,
                    file.FileExtension, file.FileSizeBytes)).Single(),
            Db.DocumentTypes.Where(type => type.Id == x.DocumentTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Title, x.DocumentNumber, x.DocumentDate, x.EffectiveFrom, x.ExpiresAt, x.Description,
            x.IsConfidential, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
}

