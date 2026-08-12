using BuildingManagement.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Api;

public sealed record AssetEventFileUploadForm(IFormFile File, string? Title = null, string? Description = null);

public static class AssetEndpoints
{
    public static void MapAssetEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        var assets = api.MapGroup("/assets").WithTags("Assets");
        api.MapGet("/asset-reference-data", async (IApplicationDbContext db, CancellationToken ct) => new
        {
            AssetTypes = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                db.AssetTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).Select(x => new ReferenceValueResponse(x.Key, x.Title)), ct),
            AssetEventTypes = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                db.AssetEventTypes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder).Select(x => new ReferenceValueResponse(x.Key, x.Title)), ct)
        }).WithTags("Reference Data");
        assets.MapPost("/", async (AssetRequest request, AssetService service, CancellationToken ct) =>
        { var result = await service.Create(request, ct); return Results.Created($"/api/v1/assets/{result.Code}", result); });
        assets.MapGet("/{assetCode}", (string assetCode, AssetService service, CancellationToken ct) => service.Get(assetCode, ct));
        assets.MapGet("/", (string? complexCode, string? buildingCode, string? assetTypeKey, string? search,
            bool? isActive, int? pageNumber, int? pageSize, string? sortDirection,
            AssetService service, CancellationToken ct) => service.List(new(pageNumber ?? 1,
                pageSize ?? 20, search, isActive, "name", sortDirection ?? "asc"), complexCode, buildingCode, assetTypeKey, ct));
        assets.MapPut("/{assetCode}", (string assetCode, AssetRequest request, AssetService service, CancellationToken ct) => service.Update(assetCode, request, ct));
        assets.MapPatch("/{assetCode}/activation", async (string assetCode, ActivationRequest request, AssetService service, CancellationToken ct) => { await service.Activate(assetCode, request.IsActive, ct); return Results.NoContent(); });

        assets.MapPost("/{assetCode}/events", async (string assetCode, AssetEventRequest request, AssetEventService service, CancellationToken ct) =>
        { var result = await service.CreateEvent(assetCode, request, ct); return Results.Created($"/api/v1/assets/{assetCode}/events/{result.Id}", result); });
        assets.MapGet("/{assetCode}/events", (string assetCode, AssetEventService service, CancellationToken ct) => service.Events(assetCode, ct));
        assets.MapGet("/{assetCode}/events/{eventId:long}", (string assetCode, long eventId,
            AssetEventService service, CancellationToken ct) => service.GetEvent(assetCode, eventId, ct));
        assets.MapPut("/{assetCode}/events/{eventId:long}", (string assetCode, long eventId,
            AssetEventRequest request, AssetEventService service, CancellationToken ct) =>
            service.UpdateEvent(assetCode, eventId, request, ct));

        assets.MapPost("/{assetCode}/gallery", async ([FromRoute] string assetCode, [FromForm] GalleryUploadForm form, AssetFileService service, CancellationToken ct) =>
        { var (incoming, stream) = Open(form.File); await using (stream) return Results.Created($"/api/v1/assets/{assetCode}/gallery", await service.UploadGallery(assetCode, incoming, new(form.Title, form.Description, form.AltText, form.SortOrder, form.IsCover), ct)); }).DisableAntiforgery();
        assets.MapGet("/{assetCode}/gallery", (string assetCode, AssetFileService service, CancellationToken ct) => service.Gallery(assetCode, ct));
        assets.MapPut("/{assetCode}/gallery/{fileCode}/cover", async (string assetCode, string fileCode, AssetFileService service, CancellationToken ct) => { await service.SetCover(assetCode, fileCode, ct); return Results.NoContent(); });
        assets.MapDelete("/{assetCode}/gallery/{fileCode}", async (string assetCode, string fileCode, AssetFileService service, CancellationToken ct) => { await service.RemoveFile(assetCode, "gallery", fileCode, ct); return Results.NoContent(); });

        assets.MapPost("/{assetCode}/documents", async ([FromRoute] string assetCode, [FromForm] DocumentUploadForm form, AssetFileService service, CancellationToken ct) =>
        { var (incoming, stream) = Open(form.File); await using (stream) return Results.Created($"/api/v1/assets/{assetCode}/documents", await service.UploadDocument(assetCode, incoming, new(form.DocumentTypeKey, form.Title, form.DocumentNumber, form.DocumentDate, form.EffectiveFrom, form.ExpiresAt, form.Description, form.IsConfidential), ct)); }).DisableAntiforgery();
        assets.MapGet("/{assetCode}/documents", (string assetCode, AssetFileService service, CancellationToken ct) => service.Documents(assetCode, ct));
        assets.MapGet("/{assetCode}/documents/{fileCode}", (string assetCode, string fileCode, AssetFileService service, CancellationToken ct) => service.Document(assetCode, fileCode, ct));
        assets.MapDelete("/{assetCode}/documents/{fileCode}", async (string assetCode, string fileCode, AssetFileService service, CancellationToken ct) => { await service.RemoveFile(assetCode, "document", fileCode, ct); return Results.NoContent(); });

        assets.MapPost("/{assetCode}/events/{eventId:long}/files", async ([FromRoute] string assetCode,
            [FromRoute] long eventId, [FromForm] AssetEventFileUploadForm form,
            AssetFileService service, CancellationToken ct) =>
        { var (incoming, stream) = Open(form.File); await using (stream) return Results.Created($"/api/v1/assets/{assetCode}/events/{eventId}/files", await service.UploadEventFile(assetCode, eventId, incoming, new(form.Title, form.Description), ct)); }).DisableAntiforgery();
        assets.MapGet("/{assetCode}/events/{eventId:long}/files", (string assetCode, long eventId,
            AssetFileService service, CancellationToken ct) => service.EventFiles(assetCode, eventId, ct));
        assets.MapDelete("/{assetCode}/events/{eventId:long}/files/{fileCode}", async (string assetCode,
            long eventId, string fileCode, AssetFileService service, CancellationToken ct) =>
        { await service.RemoveEventFile(assetCode, eventId, fileCode, ct); return Results.NoContent(); });
    }

    private static (IncomingFile Incoming, Stream Stream) Open(IFormFile? file)
    {
        if (file is null) throw new AppException(400, "validation.failed", "One or more validation errors occurred.", new Dictionary<string, string[]> { ["file"] = ["A file is required."] });
        var stream = file.OpenReadStream(); return (new(stream, file.FileName, file.ContentType, file.Length), stream);
    }
}
