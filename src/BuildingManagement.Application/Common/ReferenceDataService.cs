using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class ReferenceDataService(IApplicationDbContext db, TimeProvider clock) : PhysicalStructureServiceBase(db, clock)
{
    public async Task<ReferenceDataResponse> GetReferenceData(CancellationToken ct) =>
        new(
            await ReferenceList(Db.LocationTypes, ct),
            await ReferenceList(Db.BuildingTypes, ct),
            await ReferenceList(Db.UnitUsageTypes, ct),
            await ReferenceList(Db.UnitStatuses, ct),
            await ReferenceList(Db.DocumentTypes, ct),
            await ReferenceList(Db.PartyTypes, ct),
            await ReferenceList(Db.PartyContactTypes, ct),
            await ReferenceList(Db.UnitPartyRelationTypes, ct));

}
