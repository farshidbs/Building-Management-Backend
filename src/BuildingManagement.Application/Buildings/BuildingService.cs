using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class BuildingService(IApplicationDbContext db, TimeProvider clock, ResourceAuthorization authorization) : PhysicalStructureServiceBase(db, clock, authorization)
{
    public Task Activate(string code, bool active, CancellationToken ct) => Activate("building", code, active, ct);
    public Task Delete(string code, CancellationToken ct) => Delete("building", code, ct);

    public async Task<BuildingResponse> CreateBuilding(BuildingRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var parents = await BuildingParents(request.LocationCode, request.ComplexCode, ct);
        if (parents.ComplexId.HasValue) await Authorization.Ensure("building_manage", parents.ComplexId, null, null, ct);
        else await Authorization.EnsureAny("building_manage", ct);
        var typeId = await ReferenceId(Db.BuildingTypes, request.BuildingTypeKey, "building_type", ct);
        var entity = new Building(await UniqueCode(Db.Buildings, ct), parents.ComplexId, parents.LocationId,
            typeId, request.Name, request.Address, request.PostalCode, request.Latitude, request.Longitude,
            request.FloorsCount, request.ConstructionYear, request.Description, Now);
        Db.Buildings.Add(entity);
        await Save(ct);
        return await GetBuilding(entity.Code, ct);
    }

    public async Task<BuildingResponse> GetBuilding(string code, CancellationToken ct)
    {
        var entity = await Db.Buildings.AsNoTracking().SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("building");
        await Authorization.Ensure("building_view", entity.ComplexId, entity.Id, null, ct);
        return await BuildingProjection(Db.Buildings.AsNoTracking().Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

    public async Task<Page<BuildingResponse>> GetBuildings(PageQuery page, string? complexCode,
        string? locationCode, string? buildingTypeKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var complexId = await ComplexId(complexCode, ct);
        if (complexId.HasValue) await Authorization.Ensure("building_view", complexId, null, null, ct);
        var accessible = await Authorization.Accessible("building_view", ct);
        var locationId = await LocationId(locationCode, false, ct);
        long? typeId = string.IsNullOrWhiteSpace(buildingTypeKey)
            ? null
            : await ReferenceId(Db.BuildingTypes, buildingTypeKey, "building_type", ct);
        var query = Db.Buildings.AsNoTracking().Where(x => accessible.BuildingIds.Contains(x.Id) ||
            x.ComplexId.HasValue && accessible.ComplexIds.Contains(x.ComplexId.Value));
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
        var entity = await Db.Buildings.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("building");
        await Authorization.Ensure("building_manage", entity.ComplexId, entity.Id, null, ct);
        var parents = await BuildingParents(request.LocationCode, request.ComplexCode, ct);
        if (parents.ComplexId != entity.ComplexId && parents.ComplexId.HasValue)
            await Authorization.Ensure("building_manage", parents.ComplexId, null, null, ct);
        var typeId = await ReferenceId(Db.BuildingTypes, request.BuildingTypeKey, "building_type", ct);
        entity.Update(parents.ComplexId, parents.LocationId, typeId, request.Name, request.Address,
            request.PostalCode, request.Latitude, request.Longitude, request.FloorsCount,
            request.ConstructionYear, request.Description, Now);
        await Save(ct);
        return await GetBuilding(entity.Code, ct);
    }

}
