using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed partial class PhysicalStructureService
{
    public async Task<BuildingResponse> CreateBuilding(BuildingRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var parents = await BuildingParents(request.LocationCode, request.ComplexCode, ct);
        var typeId = await ReferenceId(db.BuildingTypes, request.BuildingTypeKey, "building_type", ct);
        var entity = new Building(await UniqueCode(db.Buildings, ct), parents.ComplexId, parents.LocationId,
            typeId, request.Name, request.Address, request.PostalCode, request.Latitude, request.Longitude,
            request.FloorsCount, request.ConstructionYear, request.Description, Now);
        db.Buildings.Add(entity);
        await Save(ct);
        return await GetBuilding(entity.Code, ct);
    }

    public async Task<BuildingResponse> GetBuilding(string code, CancellationToken ct) =>
        await BuildingProjection(db.Buildings.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("building");

    public async Task<Page<BuildingResponse>> GetBuildings(PageQuery page, string? complexCode,
        string? locationCode, string? buildingTypeKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var complexId = await ComplexId(complexCode, ct);
        var locationId = await LocationId(locationCode, false, ct);
        long? typeId = string.IsNullOrWhiteSpace(buildingTypeKey)
            ? null
            : await ReferenceId(db.BuildingTypes, buildingTypeKey, "building_type", ct);
        var query = db.Buildings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(complexCode)) query = query.Where(x => x.ComplexId == complexId);
        if (!string.IsNullOrWhiteSpace(locationCode)) query = query.Where(x => x.LocationId == locationId);
        if (typeId.HasValue) query = query.Where(x => x.BuildingTypeId == typeId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.Name.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(BuildingProjection(Order(query, page, x => x.Name)), number, size, ct);
    }

    public async Task<BuildingResponse> UpdateBuilding(string code, BuildingRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await db.Buildings.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("building");
        var parents = await BuildingParents(request.LocationCode, request.ComplexCode, ct);
        var typeId = await ReferenceId(db.BuildingTypes, request.BuildingTypeKey, "building_type", ct);
        entity.Update(parents.ComplexId, parents.LocationId, typeId, request.Name, request.Address,
            request.PostalCode, request.Latitude, request.Longitude, request.FloorsCount,
            request.ConstructionYear, request.Description, Now);
        await Save(ct);
        return await GetBuilding(entity.Code, ct);
    }

}
