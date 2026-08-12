using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed partial class PhysicalStructureService
{
    public async Task<ReferenceDataResponse> GetReferenceData(CancellationToken ct) =>
        new(
            await ReferenceList(db.LocationTypes, ct),
            await ReferenceList(db.BuildingTypes, ct),
            await ReferenceList(db.UnitUsageTypes, ct),
            await ReferenceList(db.UnitStatuses, ct),
            await ReferenceList(db.DocumentTypes, ct),
            await ReferenceList(db.PartyTypes, ct),
            await ReferenceList(db.PartyContactTypes, ct),
            await ReferenceList(db.UnitPartyRelationTypes, ct));

}
