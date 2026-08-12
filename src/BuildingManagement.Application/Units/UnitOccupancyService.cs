using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class UnitOccupancyService(IApplicationDbContext db, TimeProvider clock) : PartyOccupancyServiceBase(db, clock)
{
    public async Task<IReadOnlyList<UnitOccupancyHistoryResponse>> GetOccupancyHistory(
        string unitCode, CancellationToken ct)
    {
        var unitId = await UnitId(unitCode, ct);
        return await Db.UnitOccupancyHistories.AsNoTracking().Where(x => x.UnitId == unitId)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new UnitOccupancyHistoryResponse(x.OccupantsCount, x.EffectiveFrom,
                x.EffectiveTo, x.Notes, x.IsActive)).ToListAsync(ct);
    }

    public async Task OnboardUnit(Unit unit, UnitOccupancyRequest request, CancellationToken ct)
    {
        ValidateOccupancy(request);
        await Save(ct);
        unit.ChangeOccupancy(request.OccupantsCount, Now);
        Db.UnitOccupancyHistories.Add(new UnitOccupancyHistory(unit.Id, request.OccupantsCount,
            request.EffectiveFrom, request.Notes, Now));
        var relations = request.Relations ?? [];
        foreach (var input in relations)
            await AddRelation(unit.Id, input, ct);
        await Save(ct);
        if (request.OccupantsCount > 0 && !await HasOccupancyRelation(unit.Id, ct))
            throw Validation("relations", "An occupied unit requires at least one tenant or resident relation.");
        if (request.OccupantsCount == 0 && await HasOccupancyRelation(unit.Id, ct))
            throw Validation("relations", "A vacant unit cannot have an active tenant or resident relation.");
        await Save(ct);
    }

    public async Task<CurrentOccupancyResponse> ChangeOccupancy(string unitCode,
        OccupancyChangeRequest request, CancellationToken ct)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        if (request.OccupantsCount < 0)
            throw Validation("occupantsCount", "Must not be negative.");
        return await Db.ExecuteInTransaction(async token =>
        {
            var unit = await Db.Units.SingleOrDefaultAsync(x => x.Code == NormalizeCode(unitCode), token)
                ?? throw AppException.NotFound("unit");
            var current = await Db.UnitOccupancyHistories.SingleOrDefaultAsync(x =>
                x.UnitId == unit.Id && x.IsActive && x.EffectiveTo == null, token)
                ?? throw AppException.Conflict("occupancy.history_missing",
                    "The unit has no current occupancy history.");
            if (current.OccupantsCount != unit.CurrentOccupantsCount)
                throw AppException.Conflict("occupancy.inconsistent",
                    "Current occupancy does not match the open history record.");
            current.Close(request.EffectiveFrom, Now);
            if (request.OccupantsCount == 0)
                await CloseOccupancyRelations(unit.Id, request.EffectiveFrom, token);
            else if (request.OccupancyRelations is not null)
                await ReplaceOccupancyRelations(unit.Id, request.OccupancyRelations,
                    request.EffectiveFrom, token);
            await Save(token);
            if (request.OccupantsCount > 0 && !await HasOccupancyRelation(unit.Id, token))
                throw Validation("occupancyRelations",
                    "An occupied unit requires at least one active tenant or resident relation.");
            unit.ChangeOccupancy(request.OccupantsCount, Now);
            Db.UnitOccupancyHistories.Add(new UnitOccupancyHistory(unit.Id, request.OccupantsCount,
                request.EffectiveFrom, request.Notes, Now));
            await Save(token);
            return Current(unit.CurrentOccupantsCount);
        }, ct);
    }

}
