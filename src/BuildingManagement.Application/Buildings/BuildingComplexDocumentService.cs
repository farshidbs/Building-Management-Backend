using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed partial class FileManagementService
{
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

}
