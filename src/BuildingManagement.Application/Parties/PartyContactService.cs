using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class PartyContactService(IApplicationDbContext db, TimeProvider clock) : PartyOccupancyServiceBase(db, clock)
{
    public async Task<PartyContactResponse> AddContact(string partyCode, PartyContactRequest request,
        CancellationToken ct)
    {
        ValidateContact(request);
        return await Db.ExecuteInTransaction(async token =>
        {
            var partyId = await ActivePartyId(partyCode, token);
            var type = await Reference(Db.PartyContactTypes, request.ContactTypeKey,
                "party_contact_type", token);
            if (request.IsPrimary)
            {
                await ClearPrimaryContact(partyId, type.Id, token);
                await Save(token);
            }
            var contact = new PartyContact(partyId, type.Id,
                request.Value, NormalizeContact(type.Key, request.Value), request.Label,
                request.IsPrimary, Now);
            Db.PartyContacts.Add(contact);
            await Save(token);
            return await ContactProjection(Db.PartyContacts.AsNoTracking()
                .Where(x => x.Id == contact.Id)).SingleAsync(token);
        }, ct);
    }

    public async Task<IReadOnlyList<PartyContactResponse>> GetContacts(string partyCode, CancellationToken ct)
    {
        var partyId = await PartyId(partyCode, ct);
        return await ContactProjection(Db.PartyContacts.AsNoTracking().Where(x => x.PartyId == partyId))
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.IsPrimary).ToListAsync(ct);
    }

    public async Task<PartyContactResponse> SetPrimaryContact(string partyCode,
        PartyContactSelectorRequest request, CancellationToken ct)
    {
        ValidateContactSelector(request);
        return await Db.ExecuteInTransaction(async token =>
        {
            var partyId = await ActivePartyId(partyCode, token);
            var type = await Reference(Db.PartyContactTypes, request.ContactTypeKey,
                "party_contact_type", token);
            var target = await FindContact(partyId, type.Id,
                NormalizeContact(type.Key, request.Value), activeOnly: true, token);
            if (target.IsPrimary)
                return await ContactProjection(Db.PartyContacts.AsNoTracking()
                    .Where(x => x.Id == target.Id)).SingleAsync(token);

            var previous = await Db.PartyContacts.Where(x => x.PartyId == partyId &&
                x.PartyContactTypeId == type.Id && x.IsActive && x.IsPrimary && x.Id != target.Id)
                .ToListAsync(token);
            foreach (var contact in previous) contact.SetPrimary(false, Now);
            if (previous.Count > 0) await Save(token);
            target.SetPrimary(true, Now);
            await Save(token);
            return await ContactProjection(Db.PartyContacts.AsNoTracking()
                .Where(x => x.Id == target.Id)).SingleAsync(token);
        }, ct);
    }

    public async Task<PartyContactResponse> UpdateContact(string partyCode,
        PartyContactUpdateRequest request, CancellationToken ct)
    {
        ValidateContactUpdate(request);
        return await Db.ExecuteInTransaction(async token =>
        {
            var partyId = await ActivePartyId(partyCode, token);
            var type = await Reference(Db.PartyContactTypes, request.ContactTypeKey,
                "party_contact_type", token);
            var contact = await FindContact(partyId, type.Id,
                NormalizeContact(type.Key, request.CurrentValue), activeOnly: true, token);
            contact.Update(type.Id, request.Value, NormalizeContact(type.Key, request.Value),
                request.Label, contact.IsPrimary, Now);
            await Save(token);
            return await ContactProjection(Db.PartyContacts.AsNoTracking()
                .Where(x => x.Id == contact.Id)).SingleAsync(token);
        }, ct);
    }

}
