using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class StoredFileService(IApplicationDbContext db, IFileStorage storage, FileStorageOptions options, TimeProvider clock) : FileManagementServiceBase(db, storage, options, clock)
{
    public async Task<FileContentResponse> GetContent(string fileCode, CancellationToken ct)
    {
        var code = NormalizeCode(fileCode);
        var stored = await Db.StoredFiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == code && x.IsActive, ct)
            ?? throw AppException.NotFound("file");
        if (!await Storage.ExistsAsync(stored.StorageKey, ct))
            throw AppException.NotFound("file");
        var content = await Storage.OpenReadAsync(stored.StorageKey, ct)
            ?? throw AppException.NotFound("file");
        var inline = await Db.BuildingGalleryFiles.AnyAsync(x => x.StoredFileId == stored.Id && x.IsActive, ct) ||
                     await Db.ComplexGalleryFiles.AnyAsync(x => x.StoredFileId == stored.Id && x.IsActive, ct) ||
                     await Db.AssetGalleryFiles.AnyAsync(x => x.StoredFileId == stored.Id && x.IsActive, ct);
        return new(content, stored.ContentType,
            FileStoragePolicy.SanitizeDownloadName(stored.OriginalFileName, stored.FileExtension), inline);
    }

}
