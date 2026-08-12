using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed partial class PhysicalStructureService
{
    public async Task<LocationResponse> CreateLocation(LocationRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var parentId = await LocationId(request.ParentCode, false, ct);
        var typeId = await ReferenceId(db.LocationTypes, request.LocationTypeKey, "location_type", ct);
        var entity = new Location(await UniqueCode(db.Locations, ct), parentId, typeId, request.Name, Now);
        await EnsureSiblingName(entity, ct);
        db.Locations.Add(entity);
        await Save(ct);
        return await GetLocation(entity.Code, ct);
    }

    public async Task<LocationResponse> GetLocation(string code, CancellationToken ct) =>
        await LocationProjection(db.Locations.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("location");

    public async Task<Page<LocationResponse>> GetLocations(PageQuery page, string? parentCode,
        string? locationTypeKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var parentId = await LocationId(parentCode, false, ct);
        long? typeId = string.IsNullOrWhiteSpace(locationTypeKey)
            ? null
            : await ReferenceId(db.LocationTypes, locationTypeKey, "location_type", ct);
        var query = db.Locations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(parentCode)) query = query.Where(x => x.ParentId == parentId);
        if (typeId.HasValue) query = query.Where(x => x.LocationTypeId == typeId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.Name.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(LocationProjection(Order(query, page, x => x.Name)), number, size, ct);
    }

    public async Task<LocationResponse> UpdateLocation(string code, LocationRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await db.Locations.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("location");
        var parentId = await LocationId(request.ParentCode, false, ct);
        if (parentId == entity.Id)
            throw new AppException(400, "validation.failed", "A location cannot be its own parent.",
                new Dictionary<string, string[]> { ["parentCode"] = ["A location cannot be its own parent."] });
        var typeId = await ReferenceId(db.LocationTypes, request.LocationTypeKey, "location_type", ct);
        entity.Update(parentId, typeId, request.Name, Now);
        await EnsureSiblingName(entity, ct);
        await Save(ct);
        return await GetLocation(entity.Code, ct);
    }

}
