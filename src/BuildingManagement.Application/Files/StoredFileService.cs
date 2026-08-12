using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed partial class FileManagementService
{
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

}
