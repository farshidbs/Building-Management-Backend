using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class PartyService(IApplicationDbContext db, TimeProvider clock, ResourceAuthorization authorization) : PartyOccupancyServiceBase(db, clock, authorization)
{
    public async Task<PartyResponse> CreateParty(PartyRequest request, CancellationToken ct)
    {
        ValidateParty(request);
        await Authorization.EnsureAny("party_manage", ct);
        var typeId = await ReferenceId(Db.PartyTypes, request.PartyTypeKey, "party_type", ct);
        var party = new Party(await UniqueCode(Db.Parties, ct), typeId, request.DisplayName,
            request.FirstName, request.LastName, request.OrganizationName, request.IdentityNumber,
            request.Description, Now);
        Db.Parties.Add(party);
        await Save(ct);
        return await GetParty(party.Code, ct);
    }

    public async Task<PartyResponse> GetParty(string code, CancellationToken ct)
    {
        var party = await Db.Parties.AsNoTracking().SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("party");
        await Authorization.EnsureParty("party_view", party.Id, ct);
        return await PartyProjection(Db.Parties.AsNoTracking().Where(x => x.Id == party.Id)).SingleAsync(ct);
    }

    public async Task<Page<PartySummaryResponse>> GetParties(PageQuery page, string? partyTypeKey,
        string? contact, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var accessible = await Authorization.Accessible("party_view", ct);
        long? typeId = string.IsNullOrWhiteSpace(partyTypeKey)
            ? null
            : await ReferenceId(Db.PartyTypes, partyTypeKey, "party_type", ct);
        var query = Db.Parties.AsNoTracking().Where(party => Db.UnitPartyRelations.Any(relation =>
            relation.PartyId == party.Id && relation.IsActive && Db.Units.Any(unit => unit.Id == relation.UnitId &&
                (accessible.UnitIds.Contains(unit.Id) || accessible.BuildingIds.Contains(unit.BuildingId) ||
                 Db.Buildings.Any(building => building.Id == unit.BuildingId && building.ComplexId.HasValue &&
                     accessible.ComplexIds.Contains(building.ComplexId.Value))))));
        if (typeId.HasValue) query = query.Where(x => x.PartyTypeId == typeId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search))
        {
            var search = page.Search.Trim();
            query = query.Where(x => x.DisplayName.Contains(search) || x.Code.Contains(search));
        }
        if (!string.IsNullOrWhiteSpace(contact))
        {
            var normalized = NormalizeLoose(contact);
            query = query.Where(party => Db.PartyContacts.Any(x =>
                x.PartyId == party.Id && x.IsActive && x.NormalizedValue.Contains(normalized)));
        }
        query = (page.SortBy.ToLowerInvariant(), page.SortDirection.ToLowerInvariant()) switch
        {
            ("createdatutc", "desc") => query.OrderByDescending(x => x.CreatedAtUtc),
            ("code", "desc") => query.OrderByDescending(x => x.Code),
            ("code", _) => query.OrderBy(x => x.Code),
            (_, "desc") => query.OrderByDescending(x => x.DisplayName),
            _ => query.OrderBy(x => x.DisplayName)
        };
        var count = await query.CountAsync(ct);
        var items = await PartySummaryProjection(query.Skip((number - 1) * size).Take(size)).ToListAsync(ct);
        return new(items, number, size, count);
    }

    public async Task<PartyResponse> UpdateParty(string code, PartyRequest request, CancellationToken ct)
    {
        ValidateParty(request);
        var party = await Db.Parties.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("party");
        await Authorization.EnsureParty("party_manage", party.Id, ct);
        var typeId = await ReferenceId(Db.PartyTypes, request.PartyTypeKey, "party_type", ct);
        party.Update(typeId, request.DisplayName, request.FirstName, request.LastName,
            request.OrganizationName, request.IdentityNumber, request.Description, Now);
        await Save(ct);
        return await GetParty(party.Code, ct);
    }

    public async Task ActivateParty(string code, bool active, CancellationToken ct)
    {
        var party = await Db.Parties.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("party");
        await Authorization.EnsureParty("party_manage", party.Id, ct);
        party.SetActivation(active, Now);
        await Save(ct);
    }

}
