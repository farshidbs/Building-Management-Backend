using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed partial class PartyOccupancyService
{
    public async Task<IReadOnlyList<UnitOccupancyHistoryResponse>> GetOccupancyHistory(
        string unitCode, CancellationToken ct)
    {
        var unitId = await UnitId(unitCode, ct);
        return await db.UnitOccupancyHistories.AsNoTracking().Where(x => x.UnitId == unitId)
            .OrderByDescending(x => x.EffectiveFrom)
            .Select(x => new UnitOccupancyHistoryResponse(x.OccupantsCount, x.EffectiveFrom,
                x.EffectiveTo, x.Notes, x.IsActive)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<UnitPartyRelationResponse>> GetUnitParties(string unitCode,
        bool currentOnly, string? relationTypeKey, CancellationToken ct)
    {
        var unitId = await UnitId(unitCode, ct);
        long? typeId = string.IsNullOrWhiteSpace(relationTypeKey)
            ? null
            : await ReferenceId(db.UnitPartyRelationTypes, relationTypeKey, "unit_party_relation_type", ct);
        var query = db.UnitPartyRelations.AsNoTracking().Where(x => x.UnitId == unitId && x.IsActive);
        if (currentOnly) query = query.Where(x => x.EndDate == null);
        if (typeId.HasValue) query = query.Where(x => x.UnitPartyRelationTypeId == typeId);
        return await RelationProjection(query.OrderByDescending(x => x.StartDate)).ToListAsync(ct);
    }

    public async Task<UnitPartyRelationResponse> AddUnitRelation(string unitCode,
        UnitOnboardingRelationRequest request, CancellationToken ct)
    {
        if (request is null) throw Validation("relation", "Relation is required.");
        return await db.ExecuteInTransaction(async token =>
        {
            var unitId = await UnitId(unitCode, token);
            var type = await Reference(db.UnitPartyRelationTypes, request.RelationTypeKey,
                "unit_party_relation_type", token);
            if (type.IsOccupancyRelation)
                throw Validation("relationTypeKey",
                    "Tenant and resident relations must be changed through the occupancy workflow.");
            var relation = await AddRelation(unitId, request, token);
            await Save(token);
            return await RelationProjection(db.UnitPartyRelations.Where(x => x.Id == relation.Id))
                .SingleAsync(token);
        }, ct);
    }

    public async Task EndUnitRelation(string unitCode, string partyCode, string relationTypeKey,
        DateTimeOffset? endDate, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(partyCode))
            throw Validation("partyCode", "Party code is required.");
        if (string.IsNullOrWhiteSpace(relationTypeKey))
            throw Validation("relationTypeKey", "Relation type is required.");
        var unitId = await UnitId(unitCode, ct);
        var partyId = await PartyId(partyCode, ct);
        var relationTypeId = await ReferenceId(db.UnitPartyRelationTypes, relationTypeKey,
            "unit_party_relation_type", ct);
        var relation = await db.UnitPartyRelations.SingleOrDefaultAsync(x =>
            x.UnitId == unitId && x.PartyId == partyId &&
            x.UnitPartyRelationTypeId == relationTypeId && x.IsActive && x.EndDate == null, ct)
            ?? throw AppException.NotFound("unit_party_relation");
        var isOccupancy = await db.UnitPartyRelationTypes.Where(x =>
            x.Id == relation.UnitPartyRelationTypeId).Select(x => x.IsOccupancyRelation).SingleAsync(ct);
        if (isOccupancy && relation.IsActive &&
            !await db.UnitPartyRelations.AnyAsync(x => x.UnitId == unitId && x.Id != relation.Id &&
                x.IsActive && x.EndDate == null && db.UnitPartyRelationTypes
                    .Any(type => type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation), ct) &&
            await db.Units.AnyAsync(x => x.Id == unitId && x.CurrentOccupantsCount > 0, ct))
            throw AppException.Conflict("occupancy.relation_required",
                "Change occupancy to vacant before ending the final occupancy relation.");
        relation.End(endDate, Now);
        await Save(ct);
    }

    public static CurrentOccupancyResponse Current(int count) =>
        new(count == 0 ? ReferenceKeys.UnitStatuses.Vacant : ReferenceKeys.UnitStatuses.Occupied, count);

}
