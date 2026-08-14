using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class UnitService(IApplicationDbContext db, TimeProvider clock, UnitOccupancyService occupancyService,
    ResourceAuthorization authorization) : PhysicalStructureServiceBase(db, clock, authorization)
{
    public Task Activate(string code, bool active, CancellationToken ct) => Activate("unit", code, active, ct);
    public Task Delete(string code, CancellationToken ct) => Delete("unit", code, ct);

    public async Task<UnitResponse> CreateUnit(string buildingCode, UnitRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var buildingId = await Db.Buildings.Where(x => x.Code == NormalizeCode(buildingCode))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        await Authorization.Ensure("unit_manage", null, buildingId, null, ct);
        var usageTypeId = await ReferenceId(Db.UnitUsageTypes, request.UsageTypeKey, "unit_usage_type", ct);
        var statusId = await ReferenceId(Db.UnitStatuses, request.StatusKey, "unit_status", ct);
        RejectOccupancyStatus(request.StatusKey);
        var entity = new Unit(await UniqueCode(Db.Units, ct), buildingId, usageTypeId, statusId,
            request.UnitNumber, request.FloorNumber, request.Area, request.RoomsCount, request.ParkingCount,
            request.StorageCount, request.Description, Now);
        await EnsureUnitNumber(entity, null, ct);
        await Db.ExecuteInTransaction(async token =>
        {
            Db.Units.Add(entity);
            await occupancyService.OnboardUnit(entity, request.Occupancy!, token);
            return entity.Code;
        }, ct);
        return await GetUnit(entity.Code, ct);
    }

    public async Task<UnitResponse> GetUnit(string code, CancellationToken ct)
    {
        var entity = await Db.Units.AsNoTracking().SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("unit");
        await Authorization.Ensure("unit_view", null, entity.BuildingId, entity.Id, ct);
        return await UnitProjection(Db.Units.AsNoTracking().Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

    public async Task<Page<UnitResponse>> GetUnits(string buildingCode, PageQuery page, int? floor,
        string? usageTypeKey, string? statusKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var buildingId = await Db.Buildings.Where(x => x.Code == NormalizeCode(buildingCode))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        await Authorization.Ensure("unit_view", null, buildingId, null, ct);
        var accessible = await Authorization.Accessible("unit_view", ct);
        long? usageTypeId = string.IsNullOrWhiteSpace(usageTypeKey)
            ? null
            : await ReferenceId(Db.UnitUsageTypes, usageTypeKey, "unit_usage_type", ct);
        long? statusId = string.IsNullOrWhiteSpace(statusKey)
            ? null
            : await ReferenceId(Db.UnitStatuses, statusKey, "unit_status", ct);
        var query = Db.Units.AsNoTracking().Where(x => x.BuildingId == buildingId &&
            accessible.UnitIds.Contains(x.Id));
        if (floor.HasValue) query = query.Where(x => x.FloorNumber == floor);
        if (usageTypeId.HasValue) query = query.Where(x => x.UsageTypeId == usageTypeId);
        if (statusId.HasValue) query = query.Where(x => x.StatusId == statusId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.UnitNumber.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(UnitProjection(Order(query, page, x => x.UnitNumber)), number, size, ct);
    }

    public async Task<UnitResponse> UpdateUnit(string code, UnitUpdateRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await Db.Units.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("unit");
        await Authorization.Ensure("unit_manage", null, entity.BuildingId, entity.Id, ct);
        var usageTypeId = await ReferenceId(Db.UnitUsageTypes, request.UsageTypeKey, "unit_usage_type", ct);
        var statusId = await ReferenceId(Db.UnitStatuses, request.StatusKey, "unit_status", ct);
        RejectOccupancyStatus(request.StatusKey);
        entity.Update(usageTypeId, statusId, request.UnitNumber, request.FloorNumber, request.Area,
            request.RoomsCount, request.ParkingCount, request.StorageCount, request.Description, Now);
        await EnsureUnitNumber(entity, entity.Id, ct);
        await Save(ct);
        return await GetUnit(entity.Code, ct);
    }

}
