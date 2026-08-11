using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

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
public sealed record AssetEventResponse(DateTimeOffset EventDate, ReferenceValueResponse EventType, string Title,
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

public sealed class AssetManagementService(IApplicationDbContext db, IFileStorage storage,
    FileStorageOptions options, TimeProvider clock)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<AssetResponse> Create(AssetRequest request, CancellationToken ct)
    {
        Validate(request);
        var typeId = await RefId(db.AssetTypes, request.AssetTypeKey, "asset_type", ct);
        var complexId = await ComplexId(request.ComplexCode, true, ct);
        var buildingId = await BuildingId(request.BuildingCode, true, ct);
        var asset = new Asset(await UniqueCode(db.Assets, ct), typeId, complexId, buildingId, request.Name,
            request.Brand, request.Model, request.SerialNumber, request.InstallationDate, request.PurchaseDate,
            request.SuggestedReviewIntervalDays, request.Description, Now);
        db.Assets.Add(asset); await Save(ct); return await Get(asset.Code, ct);
    }

    public async Task<AssetResponse> Get(string code, CancellationToken ct) =>
        await Projection(db.Assets.AsNoTracking().Where(x => x.Code == Normalize(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("asset");

    public async Task<Page<AssetResponse>> List(PageQuery query, string? complexCode, string? buildingCode,
        string? assetTypeKey, CancellationToken ct)
    {
        var (number, size) = query.Validated();
        var complexId = await ComplexId(complexCode, false, ct); var buildingId = await BuildingId(buildingCode, false, ct);
        long? typeId = string.IsNullOrWhiteSpace(assetTypeKey) ? null : await RefId(db.AssetTypes, assetTypeKey, "asset_type", ct);
        var source = db.Assets.AsNoTracking().Where(x => (!query.IsActive.HasValue || x.IsActive == query.IsActive) &&
            (!complexId.HasValue || x.ComplexId == complexId) && (!buildingId.HasValue || x.BuildingId == buildingId) &&
            (!typeId.HasValue || x.AssetTypeId == typeId) && (string.IsNullOrWhiteSpace(query.Search) || x.Name.Contains(query.Search)));
        var total = await source.CountAsync(ct);
        source = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(x => x.Name).ThenByDescending(x => x.Id) : source.OrderBy(x => x.Name).ThenBy(x => x.Id);
        return new(await Projection(source.Skip((number - 1) * size).Take(size)).ToListAsync(ct), number, size, total);
    }

    public async Task<AssetResponse> Update(string code, AssetRequest request, CancellationToken ct)
    {
        Validate(request); var asset = await Entity(code, ct);
        var typeId = await RefId(db.AssetTypes, request.AssetTypeKey, "asset_type", ct);
        asset.Update(typeId, await ComplexId(request.ComplexCode, true, ct), await BuildingId(request.BuildingCode, true, ct),
            request.Name, request.Brand, request.Model, request.SerialNumber, request.InstallationDate, request.PurchaseDate,
            request.SuggestedReviewIntervalDays, request.Description, Now);
        await Save(ct); return await Get(asset.Code, ct);
    }

    public async Task Activate(string code, bool active, CancellationToken ct)
    { var asset = await Entity(code, ct); asset.SetActivation(active, Now); await Save(ct); }

    public async Task<AssetEventResponse> CreateEvent(string assetCode, AssetEventRequest request, CancellationToken ct)
    {
        Validate(request); var asset = await Entity(assetCode, ct);
        var typeId = await RefId(db.AssetEventTypes, request.EventTypeKey, "asset_event_type", ct);
        var partyId = await PartyId(request.ServiceProviderPartyCode, ct);
        var entity = new AssetEvent(asset.Id, typeId, request.EventDate, request.Title, request.Description,
            request.SuggestedNextDate, partyId, request.Cost, Now);
        db.AssetEvents.Add(entity); await Save(ct); return await EventProjection(db.AssetEvents.Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

    public async Task<IReadOnlyList<AssetEventResponse>> Events(string assetCode, CancellationToken ct)
    {
        var id = (await Entity(assetCode, ct)).Id;
        return await EventProjection(db.AssetEvents.AsNoTracking().Where(x => x.AssetId == id && x.IsActive)
            .OrderByDescending(x => x.EventDate).ThenByDescending(x => x.Id)).ToListAsync(ct);
    }

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
        var stored = await Store(asset.Code, "documents", file, false, ct);
        try
        {
            var entity = new AssetDocument(asset.Id, stored.Id, typeId, metadata.Title, metadata.DocumentNumber,
            metadata.DocumentDate, metadata.EffectiveFrom, metadata.ExpiresAt, metadata.Description, metadata.IsConfidential, Now);
            db.AssetDocuments.Add(entity); await Save(ct); return await DocumentProjection(db.AssetDocuments.Where(x => x.Id == entity.Id)).SingleAsync(ct);
        }
        catch { await Rollback(stored, ct); throw; }
    }
    public async Task<IReadOnlyList<AssetDocumentResponse>> Documents(string assetCode, CancellationToken ct)
    { var id = (await Entity(assetCode, ct)).Id; return await DocumentProjection(db.AssetDocuments.AsNoTracking().Where(x => x.AssetId == id && x.IsActive).OrderByDescending(x => x.CreatedAtUtc)).ToListAsync(ct); }
    public async Task<AssetDocumentResponse> Document(string assetCode, string fileCode, CancellationToken ct)
    { var id = (await Entity(assetCode, ct)).Id; var storedId = await StoredId(fileCode, ct); return await DocumentProjection(db.AssetDocuments.AsNoTracking().Where(x => x.AssetId == id && x.StoredFileId == storedId && x.IsActive)).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("asset_document"); }

    public async Task<AssetFileResponse> UploadEventFile(string assetCode, DateTimeOffset eventDate, IncomingFile file,
        AssetFileMetadataRequest metadata, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct); var events = await db.AssetEvents.Where(x => x.AssetId == asset.Id && x.EventDate == eventDate.ToUniversalTime() && x.IsActive).ToListAsync(ct);
        if (events.Count != 1) throw new AppException(409, "asset_event.ambiguous", "EventDate must identify exactly one active event for this asset.");
        var stored = await Store(asset.Code, "events", file, false, ct);
        try { var entity = new AssetEventFile(events[0].Id, stored.Id, metadata.Title, metadata.Description, Now); db.AssetEventFiles.Add(entity); await Save(ct); return await EventFileProjection(db.AssetEventFiles.Where(x => x.Id == entity.Id)).SingleAsync(ct); }
        catch { await Rollback(stored, ct); throw; }
    }
    public async Task<IReadOnlyList<AssetFileResponse>> EventFiles(string assetCode, DateTimeOffset eventDate, CancellationToken ct)
    { var asset = await Entity(assetCode, ct); return await EventFileProjection(db.AssetEventFiles.AsNoTracking().Where(f => f.IsActive && db.AssetEvents.Any(e => e.Id == f.AssetEventId && e.AssetId == asset.Id && e.EventDate == eventDate.ToUniversalTime() && e.IsActive))).ToListAsync(ct); }

    public async Task RemoveFile(string assetCode, string kind, string fileCode, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct); var storedId = await StoredId(fileCode, ct);
        AssetFileRelation relation = kind switch
        {
            "gallery" => await db.AssetGalleryFiles.SingleOrDefaultAsync(x => x.AssetId == asset.Id && x.StoredFileId == storedId && x.IsActive, ct) ?? throw AppException.NotFound("asset_gallery"),
            "document" => await db.AssetDocuments.SingleOrDefaultAsync(x => x.AssetId == asset.Id && x.StoredFileId == storedId && x.IsActive, ct) ?? throw AppException.NotFound("asset_document"),
            "event" => await db.AssetEventFiles.SingleOrDefaultAsync(x => x.StoredFileId == storedId && x.IsActive && db.AssetEvents.Any(e => e.Id == x.AssetEventId && e.AssetId == asset.Id), ct) ?? throw AppException.NotFound("asset_event_file"),
            _ => throw Validation("kind", "File relation kind is invalid.")
        };
        relation.Deactivate(Now); var stored = await db.StoredFiles.SingleAsync(x => x.Id == storedId, ct); stored.SetActivation(false, Now);
        await Save(ct); try { await storage.DeleteAsync(stored.StorageKey, ct); } catch (Exception exception) when (exception is not OperationCanceledException) { throw new AppException(500, "file.cleanup_failed", "File metadata was deactivated, but physical cleanup failed."); }
    }

    private async Task<AssetGalleryResponse> UploadGalleryCore(string code, IncomingFile file, GalleryMetadataRequest metadata, CancellationToken ct)
    {
        var asset = await Entity(code, ct); var stored = await Store(asset.Code, "gallery", file, true, ct);
        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                if (metadata.IsCover) { var covers = await db.AssetGalleryFiles.Where(x => x.AssetId == asset.Id && x.IsActive && x.IsCover).ToListAsync(token); foreach (var cover in covers) cover.RemoveCover(Now); }
                var entity = new AssetGalleryFile(asset.Id, stored.Id, metadata.Title, metadata.Description, metadata.AltText, metadata.SortOrder, metadata.IsCover, Now); db.AssetGalleryFiles.Add(entity); await Save(token); return await GalleryProjection(db.AssetGalleryFiles.Where(x => x.Id == entity.Id)).SingleAsync(token);
            }, ct);
        }
        catch { await Rollback(stored, ct); throw; }
    }

    private async Task<StoredFile> Store(string assetCode, string category, IncomingFile incoming, bool image, CancellationToken ct)
    {
        var validated = image ? FileStoragePolicy.ValidateImage(incoming, options) : FileStoragePolicy.ValidateDocument(incoming, options);
        var name = $"{Guid.NewGuid():N}{validated.Extension}"; var key = FileStoragePolicy.CreateStorageKey("assets", assetCode, category, name);
        await storage.SaveAsync(new(key, incoming.Content), ct); var stored = new StoredFile(await UniqueCode(db.StoredFiles, ct), validated.OriginalFileName, name, key, validated.ContentType, validated.Extension, validated.Length, null, StorageProviders.Local, Now);
        db.StoredFiles.Add(stored); try { await Save(ct); return stored; } catch { await storage.DeleteAsync(key, ct); throw; }
    }
    private async Task Rollback(StoredFile file, CancellationToken ct) { db.StoredFiles.Remove(file); try { await db.SaveChangesAsync(ct); } catch { } try { await storage.DeleteAsync(file.StorageKey, ct); } catch { } }
    private async Task<Asset> Entity(string code, CancellationToken ct) => await db.Assets.SingleOrDefaultAsync(x => x.Code == Normalize(code), ct) ?? throw AppException.NotFound("asset");
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
    private static void Validate(AssetEventRequest? r) { if (r is null) throw Validation("request", "A request body is required."); if (string.IsNullOrWhiteSpace(r.EventTypeKey)) throw Validation("eventTypeKey", "Event type is required."); if (string.IsNullOrWhiteSpace(r.Title)) throw Validation("title", "Title is required."); }
    private static AppException Validation(string field, string message) => new(400, "validation.failed", "One or more validation errors occurred.", new Dictionary<string, string[]> { [field] = [message] });

    private IQueryable<AssetResponse> Projection(IQueryable<Asset> q) => q.Select(x => new AssetResponse(x.Code,
        db.AssetTypes.Where(t => t.Id == x.AssetTypeId).Select(t => new ReferenceValueResponse(t.Key, t.Title)).Single(),
        x.ComplexId == null ? null : db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => new ResourceReferenceResponse(c.Code, c.Name)).Single(),
        x.BuildingId == null ? null : db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => new ResourceReferenceResponse(b.Code, b.Name)).Single(),
        x.Name, x.Brand, x.Model, x.SerialNumber, x.InstallationDate, x.PurchaseDate, x.SuggestedReviewIntervalDays, x.Description,
        db.AssetEvents.Where(e => e.AssetId == x.Id && e.IsActive).OrderByDescending(e => e.EventDate).Select(e => (DateTimeOffset?)e.EventDate).FirstOrDefault(),
        db.AssetEvents.Where(e => e.AssetId == x.Id && e.IsActive).OrderByDescending(e => e.EventDate).Select(e => e.SuggestedNextDate ?? (x.SuggestedReviewIntervalDays == null ? null : e.EventDate.AddDays(x.SuggestedReviewIntervalDays.Value))).FirstOrDefault(),
        x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private IQueryable<AssetEventResponse> EventProjection(IQueryable<AssetEvent> q) => q.Select(x => new AssetEventResponse(x.EventDate, db.AssetEventTypes.Where(t => t.Id == x.AssetEventTypeId).Select(t => new ReferenceValueResponse(t.Key, t.Title)).Single(), x.Title, x.Description, x.SuggestedNextDate,
        x.ServiceProviderPartyId == null ? null : db.Parties.Where(p => p.Id == x.ServiceProviderPartyId).Select(p => new ResourceReferenceResponse(p.Code, p.DisplayName)).Single(), x.Cost, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private IQueryable<AssetGalleryResponse> GalleryProjection(IQueryable<AssetGalleryFile> q) => q.Select(x => new AssetGalleryResponse(File(x.StoredFileId), x.Title, x.Description, x.AltText, x.SortOrder, x.IsCover, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private IQueryable<AssetDocumentResponse> DocumentProjection(IQueryable<AssetDocument> q) => q.Select(x => new AssetDocumentResponse(File(x.StoredFileId), db.DocumentTypes.Where(t => t.Id == x.DocumentTypeId).Select(t => new ReferenceValueResponse(t.Key, t.Title)).Single(), x.Title, x.DocumentNumber, x.DocumentDate, x.EffectiveFrom, x.ExpiresAt, x.Description, x.IsConfidential, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private IQueryable<AssetFileResponse> EventFileProjection(IQueryable<AssetEventFile> q) => q.Select(x => new AssetFileResponse(File(x.StoredFileId), x.Title, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
    private StoredFileResponse File(long id) => db.StoredFiles.Where(x => x.Id == id).Select(x => new StoredFileResponse(x.Code, "/api/v1/files/" + x.Code + "/content", x.OriginalFileName, x.ContentType, x.FileExtension, x.FileSizeBytes)).Single();
}
