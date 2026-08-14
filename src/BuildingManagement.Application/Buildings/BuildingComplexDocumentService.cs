using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class BuildingComplexDocumentService(IApplicationDbContext db, IFileStorage storage, FileStorageOptions options, TimeProvider clock, ResourceAuthorization authorization) : FileManagementServiceBase(db, storage, options, clock, authorization)
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
        var canReadConfidential = await Authorization.Can("file_read_confidential", null, buildingId, null, ct);
        return await BuildingDocumentProjection(Db.BuildingDocuments.AsNoTracking()
            .Where(x => x.BuildingId == buildingId && x.IsActive && (!x.IsConfidential || canReadConfidential))
            .OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.CreatedAtUtc)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DocumentFileResponse>> GetComplexDocuments(
        string complexCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        var canReadConfidential = await Authorization.Can("file_read_confidential", complexId, null, null, ct);
        return await ComplexDocumentProjection(Db.ComplexDocuments.AsNoTracking()
            .Where(x => x.ComplexId == complexId && x.IsActive && (!x.IsConfidential || canReadConfidential))
            .OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.CreatedAtUtc)).ToListAsync(ct);
    }

    public async Task<DocumentFileResponse> GetBuildingDocument(
        string buildingCode, string documentCode, CancellationToken ct)
    {
        var buildingId = await BuildingId(buildingCode, false, ct);
        var confidential = await Db.BuildingDocuments.AsNoTracking().Where(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(documentCode) && x.IsActive)
            .Select(x => (bool?)x.IsConfidential).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("building_document");
        if (confidential)
            await Authorization.Ensure("file_read_confidential", null, buildingId, null, ct);
        return await BuildingDocumentProjection(Db.BuildingDocuments.AsNoTracking().Where(x =>
            x.BuildingId == buildingId && x.Code == NormalizeCode(documentCode) && x.IsActive))
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building_document");
    }

    public async Task<DocumentFileResponse> GetComplexDocument(
        string complexCode, string documentCode, CancellationToken ct)
    {
        var complexId = await ComplexId(complexCode, false, ct);
        var confidential = await Db.ComplexDocuments.AsNoTracking().Where(x =>
            x.ComplexId == complexId && x.Code == NormalizeCode(documentCode) && x.IsActive)
            .Select(x => (bool?)x.IsConfidential).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("complex_document");
        if (confidential)
            await Authorization.Ensure("file_read_confidential", complexId, null, null, ct);
        return await ComplexDocumentProjection(Db.ComplexDocuments.AsNoTracking().Where(x =>
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
