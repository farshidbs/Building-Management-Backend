using BuildingManagement.Application;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagement.Api;

public sealed record GalleryUploadForm(IFormFile File, string? Title, string? Description, string? AltText, int SortOrder, bool IsCover);
public sealed record DocumentUploadForm(IFormFile File, string DocumentTypeKey, string Title, string? DocumentNumber,
    DateTimeOffset? DocumentDate, DateTimeOffset? EffectiveFrom, DateTimeOffset? ExpiresAt, string? Description, bool IsConfidential);

public static class FileEndpoints
{
    public static void MapFileManagementEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        MapBuildingFiles(api);
        MapComplexFiles(api);
        api.MapGet("/files/{fileCode}/content", async (string fileCode, StoredFileService service, CancellationToken ct) =>
        {
            var file = await service.GetContent(fileCode, ct);
            return Results.File(file.Content, file.ContentType, file.Inline ? null : file.OriginalFileName, enableRangeProcessing: true);
        }).WithTags("Files");
    }

    private static void MapBuildingFiles(RouteGroupBuilder api)
    {
        var gallery = api.MapGroup("/buildings/{buildingCode}/gallery").WithTags("Building Gallery");
        gallery.MapPost("/", async ([FromRoute] string buildingCode, [FromForm] GalleryUploadForm form,
            BuildingComplexGalleryService service, CancellationToken ct) =>
        {
            var (incoming, stream) = Open(form.File);
            await using (stream)
            {
                var metadata = new GalleryMetadataRequest(form.Title, form.Description, form.AltText, form.SortOrder, form.IsCover);
                var response = await service.UploadBuildingGallery(buildingCode, incoming, metadata, ct);
                return Results.Created($"/api/v1/buildings/{buildingCode}/gallery/{response.Code}", response);
            }
        }).DisableAntiforgery();
        gallery.MapGet("/", (string buildingCode, BuildingComplexGalleryService service, CancellationToken ct) =>
            service.GetBuildingGallery(buildingCode, ct));
        gallery.MapPut("/{galleryCode}", (string buildingCode, string galleryCode, GalleryMetadataRequest request,
            BuildingComplexGalleryService service, CancellationToken ct) =>
            service.UpdateBuildingGallery(buildingCode, galleryCode, request, ct));
        gallery.MapDelete("/{galleryCode}", async (string buildingCode, string galleryCode,
            BuildingComplexGalleryService service, CancellationToken ct) =>
        {
            await service.DeleteBuildingGallery(buildingCode, galleryCode, ct);
            return Results.NoContent();
        });

        var documents = api.MapGroup("/buildings/{buildingCode}/documents").WithTags("Building Documents");
        documents.MapPost("/", async ([FromRoute] string buildingCode, [FromForm] DocumentUploadForm form,
            BuildingComplexDocumentService service, CancellationToken ct) =>
        {
            var (incoming, stream) = Open(form.File);
            await using (stream)
            {
                var metadata = DocumentMetadata(form);
                var response = await service.UploadBuildingDocument(buildingCode, incoming, metadata, ct);
                return Results.Created($"/api/v1/buildings/{buildingCode}/documents/{response.Code}", response);
            }
        }).DisableAntiforgery();
        documents.MapGet("/", (string buildingCode, BuildingComplexDocumentService service, CancellationToken ct) =>
            service.GetBuildingDocuments(buildingCode, ct));
        documents.MapGet("/{documentCode}", (string buildingCode, string documentCode,
            BuildingComplexDocumentService service, CancellationToken ct) =>
            service.GetBuildingDocument(buildingCode, documentCode, ct));
        documents.MapPut("/{documentCode}", (string buildingCode, string documentCode, DocumentMetadataRequest request,
            BuildingComplexDocumentService service, CancellationToken ct) =>
            service.UpdateBuildingDocument(buildingCode, documentCode, request, ct));
        documents.MapDelete("/{documentCode}", async (string buildingCode, string documentCode,
            BuildingComplexDocumentService service, CancellationToken ct) =>
        {
            await service.DeleteBuildingDocument(buildingCode, documentCode, ct);
            return Results.NoContent();
        });
    }

    private static void MapComplexFiles(RouteGroupBuilder api)
    {
        var gallery = api.MapGroup("/complexes/{complexCode}/gallery").WithTags("Complex Gallery");
        gallery.MapPost("/", async ([FromRoute] string complexCode, [FromForm] GalleryUploadForm form,
            BuildingComplexGalleryService service, CancellationToken ct) =>
        {
            var (incoming, stream) = Open(form.File);
            await using (stream)
            {
                var metadata = new GalleryMetadataRequest(form.Title, form.Description, form.AltText, form.SortOrder, form.IsCover);
                var response = await service.UploadComplexGallery(complexCode, incoming, metadata, ct);
                return Results.Created($"/api/v1/complexes/{complexCode}/gallery/{response.Code}", response);
            }
        }).DisableAntiforgery();
        gallery.MapGet("/", (string complexCode, BuildingComplexGalleryService service, CancellationToken ct) =>
            service.GetComplexGallery(complexCode, ct));
        gallery.MapPut("/{galleryCode}", (string complexCode, string galleryCode, GalleryMetadataRequest request,
            BuildingComplexGalleryService service, CancellationToken ct) =>
            service.UpdateComplexGallery(complexCode, galleryCode, request, ct));
        gallery.MapDelete("/{galleryCode}", async (string complexCode, string galleryCode,
            BuildingComplexGalleryService service, CancellationToken ct) =>
        {
            await service.DeleteComplexGallery(complexCode, galleryCode, ct);
            return Results.NoContent();
        });

        var documents = api.MapGroup("/complexes/{complexCode}/documents").WithTags("Complex Documents");
        documents.MapPost("/", async ([FromRoute] string complexCode, [FromForm] DocumentUploadForm form,
            BuildingComplexDocumentService service, CancellationToken ct) =>
        {
            var (incoming, stream) = Open(form.File);
            await using (stream)
            {
                var metadata = DocumentMetadata(form);
                var response = await service.UploadComplexDocument(complexCode, incoming, metadata, ct);
                return Results.Created($"/api/v1/complexes/{complexCode}/documents/{response.Code}", response);
            }
        }).DisableAntiforgery();
        documents.MapGet("/", (string complexCode, BuildingComplexDocumentService service, CancellationToken ct) =>
            service.GetComplexDocuments(complexCode, ct));
        documents.MapGet("/{documentCode}", (string complexCode, string documentCode,
            BuildingComplexDocumentService service, CancellationToken ct) =>
            service.GetComplexDocument(complexCode, documentCode, ct));
        documents.MapPut("/{documentCode}", (string complexCode, string documentCode, DocumentMetadataRequest request,
            BuildingComplexDocumentService service, CancellationToken ct) =>
            service.UpdateComplexDocument(complexCode, documentCode, request, ct));
        documents.MapDelete("/{documentCode}", async (string complexCode, string documentCode,
            BuildingComplexDocumentService service, CancellationToken ct) =>
        {
            await service.DeleteComplexDocument(complexCode, documentCode, ct);
            return Results.NoContent();
        });
    }

    private static (IncomingFile Incoming, Stream Stream) Open(IFormFile? file)
    {
        if (file is null)
            throw new AppException(400, "validation.failed", "One or more validation errors occurred.",
                new Dictionary<string, string[]> { ["file"] = ["A file is required."] });
        var stream = file.OpenReadStream();
        return (new IncomingFile(stream, file.FileName, file.ContentType, file.Length), stream);
    }

    private static DocumentMetadataRequest DocumentMetadata(DocumentUploadForm form) =>
        new(form.DocumentTypeKey, form.Title, form.DocumentNumber, form.DocumentDate,
            form.EffectiveFrom, form.ExpiresAt, form.Description, form.IsConfidential);
}
