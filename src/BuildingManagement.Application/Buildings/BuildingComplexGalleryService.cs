using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class BuildingComplexGalleryService(IApplicationDbContext db, IFileStorage storage, FileStorageOptions options, TimeProvider clock, ResourceAuthorization authorization) : FileManagementServiceBase(db, storage, options, clock, authorization)
{
    public async Task<GalleryFileResponse> UploadBuildingGallery(
        string buildingCode, IncomingFile incoming, GalleryMetadataRequest metadata, CancellationToken ct)
    {
        var buildingId = await ActiveBuildingId(buildingCode, ct, true);
        var file = FileStoragePolicy.ValidateImage(incoming, Options);
        var stored = await Store(incoming, file, "buildings", NormalizeCode(buildingCode), "gallery", ct);
        try
        {
            if (metadata.IsCover) await ClearBuildingCover(buildingId, null, ct);
            var relation = new BuildingGalleryFile(await UniqueCode(Db.BuildingGalleryFiles, ct),
                buildingId, stored.Id, metadata.Title, metadata.Description, metadata.AltText,
                metadata.SortOrder, metadata.IsCover, Now);
            Db.BuildingGalleryFiles.Add(relation);
            await Save(ct);
            return await BuildingGalleryProjection(Db.BuildingGalleryFiles.Where(x => x.Id == relation.Id))
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
        var complexId = await ActiveComplexId(complexCode, ct, true);
        var file = FileStoragePolicy.ValidateImage(incoming, Options);
        var stored = await Store(incoming, file, "complexes", NormalizeCode(complexCode), "gallery", ct);
        try
        {
            if (metadata.IsCover) await ClearComplexCover(complexId, null, ct);
            var relation = new ComplexGalleryFile(await UniqueCode(Db.ComplexGalleryFiles, ct),
                complexId, stored.Id, metadata.Title, metadata.Description, metadata.AltText,
                metadata.SortOrder, metadata.IsCover, Now);
            Db.ComplexGalleryFiles.Add(relation);
            await Save(ct);
            return await ComplexGalleryProjection(Db.ComplexGalleryFiles.Where(x => x.Id == relation.Id))
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
        return await BuildingGalleryProjection(Db.BuildingGalleryFiles.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.IsActive)
            .OrderByDescending(x => x.IsCover).ThenBy(x => x.SortOrder).ThenBy(x => x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GalleryFileResponse>> GetComplexGallery(
        string complexCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        return await ComplexGalleryProjection(Db.ComplexGalleryFiles.AsNoTracking()
            .Where(x => x.ComplexId == complexId && x.IsActive)
            .OrderByDescending(x => x.IsCover).ThenBy(x => x.SortOrder).ThenBy(x => x.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<GalleryFileResponse> UpdateBuildingGallery(string buildingCode, string galleryCode,
        GalleryMetadataRequest request, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct, true);
        var relation = await Db.BuildingGalleryFiles.SingleOrDefaultAsync(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("building_gallery");
        if (request.IsCover) await ClearBuildingCover(buildingId, relation.Id, ct);
        relation.Update(request.Title, request.Description, request.AltText, request.SortOrder,
            request.IsCover, Now);
        await Save(ct);
        return await BuildingGalleryProjection(Db.BuildingGalleryFiles.Where(x => x.Id == relation.Id))
            .SingleAsync(ct);
    }

    public async Task<GalleryFileResponse> UpdateComplexGallery(string complexCode, string galleryCode,
        GalleryMetadataRequest request, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct, true);
        var relation = await Db.ComplexGalleryFiles.SingleOrDefaultAsync(x =>
            x.ComplexId == complexId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("complex_gallery");
        if (request.IsCover) await ClearComplexCover(complexId, relation.Id, ct);
        relation.Update(request.Title, request.Description, request.AltText, request.SortOrder,
            request.IsCover, Now);
        await Save(ct);
        return await ComplexGalleryProjection(Db.ComplexGalleryFiles.Where(x => x.Id == relation.Id))
            .SingleAsync(ct);
    }

    public async Task DeleteBuildingGallery(string buildingCode, string galleryCode, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct, true);
        var relation = await Db.BuildingGalleryFiles.SingleOrDefaultAsync(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("building_gallery");
        var storedFileId = relation.StoredFileId;
        Db.BuildingGalleryFiles.Remove(relation);
        await Save(ct);
        await CleanupOrphan(storedFileId, ct);
    }

    public async Task DeleteComplexGallery(string complexCode, string galleryCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct, true);
        var relation = await Db.ComplexGalleryFiles.SingleOrDefaultAsync(x =>
            x.ComplexId == complexId && x.Code == NormalizeCode(galleryCode), ct)
            ?? throw AppException.NotFound("complex_gallery");
        var storedFileId = relation.StoredFileId;
        Db.ComplexGalleryFiles.Remove(relation);
        await Save(ct);
        await CleanupOrphan(storedFileId, ct);
    }

}
