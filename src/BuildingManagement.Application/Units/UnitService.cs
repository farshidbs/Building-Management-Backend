using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed partial class PhysicalStructureService
{
    public async Task<UnitResponse> CreateUnit(string buildingCode, UnitRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var buildingId = await db.Buildings.Where(x => x.Code == NormalizeCode(buildingCode))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        var usageTypeId = await ReferenceId(db.UnitUsageTypes, request.UsageTypeKey, "unit_usage_type", ct);
        var statusId = await ReferenceId(db.UnitStatuses, request.StatusKey, "unit_status", ct);
        RejectOccupancyStatus(request.StatusKey);
        var entity = new Unit(await UniqueCode(db.Units, ct), buildingId, usageTypeId, statusId,
            request.UnitNumber, request.FloorNumber, request.Area, request.RoomsCount, request.ParkingCount,
            request.StorageCount, request.Description, Now);
        await EnsureUnitNumber(entity, null, ct);
        await db.ExecuteInTransaction(async token =>
        {
            db.Units.Add(entity);
            await partyOccupancy.OnboardUnit(entity, request.Occupancy!, token);
            return entity.Code;
        }, ct);
        return await GetUnit(entity.Code, ct);
    }

    public async Task<UnitResponse> GetUnit(string code, CancellationToken ct) =>
        await UnitProjection(db.Units.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("unit");

    public async Task<Page<UnitResponse>> GetUnits(string buildingCode, PageQuery page, int? floor,
        string? usageTypeKey, string? statusKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var buildingId = await db.Buildings.Where(x => x.Code == NormalizeCode(buildingCode))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        long? usageTypeId = string.IsNullOrWhiteSpace(usageTypeKey)
            ? null
            : await ReferenceId(db.UnitUsageTypes, usageTypeKey, "unit_usage_type", ct);
        long? statusId = string.IsNullOrWhiteSpace(statusKey)
            ? null
            : await ReferenceId(db.UnitStatuses, statusKey, "unit_status", ct);
        var query = db.Units.AsNoTracking().Where(x => x.BuildingId == buildingId);
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
        var entity = await db.Units.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("unit");
        var usageTypeId = await ReferenceId(db.UnitUsageTypes, request.UsageTypeKey, "unit_usage_type", ct);
        var statusId = await ReferenceId(db.UnitStatuses, request.StatusKey, "unit_status", ct);
        RejectOccupancyStatus(request.StatusKey);
        entity.Update(usageTypeId, statusId, request.UnitNumber, request.FloorNumber, request.Area,
            request.RoomsCount, request.ParkingCount, request.StorageCount, request.Description, Now);
        await EnsureUnitNumber(entity, entity.Id, ct);
        await Save(ct);
        return await GetUnit(entity.Code, ct);
    }

}
