using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed partial class PartyOccupancyService
{
    public async Task<PartyResponse> CreateParty(PartyRequest request, CancellationToken ct)
    {
        ValidateParty(request);
        var typeId = await ReferenceId(db.PartyTypes, request.PartyTypeKey, "party_type", ct);
        var party = new Party(await UniqueCode(db.Parties, ct), typeId, request.DisplayName,
            request.FirstName, request.LastName, request.OrganizationName, request.IdentityNumber,
            request.Description, Now);
        db.Parties.Add(party);
        await Save(ct);
        return await GetParty(party.Code, ct);
    }

    public async Task<PartyResponse> GetParty(string code, CancellationToken ct) =>
        await PartyProjection(db.Parties.AsNoTracking().Where(x => x.Code == NormalizeCode(code)))
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");

    public async Task<Page<PartySummaryResponse>> GetParties(PageQuery page, string? partyTypeKey,
        string? contact, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        long? typeId = string.IsNullOrWhiteSpace(partyTypeKey)
            ? null
            : await ReferenceId(db.PartyTypes, partyTypeKey, "party_type", ct);
        var query = db.Parties.AsNoTracking();
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
            query = query.Where(party => db.PartyContacts.Any(x =>
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
        var party = await db.Parties.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("party");
        var typeId = await ReferenceId(db.PartyTypes, request.PartyTypeKey, "party_type", ct);
        party.Update(typeId, request.DisplayName, request.FirstName, request.LastName,
            request.OrganizationName, request.IdentityNumber, request.Description, Now);
        await Save(ct);
        return await GetParty(party.Code, ct);
    }

    public async Task ActivateParty(string code, bool active, CancellationToken ct)
    {
        var party = await db.Parties.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("party");
        party.SetActivation(active, Now);
        await Save(ct);
    }

}
