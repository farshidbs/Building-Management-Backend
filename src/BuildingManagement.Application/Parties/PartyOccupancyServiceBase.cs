using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public abstract class PartyOccupancyServiceBase(IApplicationDbContext db, TimeProvider clock,
    ResourceAuthorization? resourceAuthorization = null)
{
    protected IApplicationDbContext Db { get; } = db;
    protected ResourceAuthorization Authorization { get; } = resourceAuthorization!;
    protected DateTimeOffset Now => clock.GetUtcNow();

    protected async Task<UnitPartyRelation> AddRelation(long unitId, UnitOnboardingRelationRequest request,
        CancellationToken ct)
    {
        if (request is null) throw Validation("relation", "Relation is required.");
        var type = await Reference(Db.UnitPartyRelationTypes, request.RelationTypeKey,
            "unit_party_relation_type", ct);
        var partyId = await ResolveParty(request.Party, ct);
        var relation = new UnitPartyRelation(unitId, partyId, type.Id,
            request.StartDate, null, request.Notes, Now);
        Db.UnitPartyRelations.Add(relation);
        return relation;
    }

    protected async Task<long> ResolveParty(PartySelectionRequest selection, CancellationToken ct)
    {
        if (selection is null)
            throw Validation("party", "Party selection is required.");
        var hasExisting = !string.IsNullOrWhiteSpace(selection.ExistingPartyCode);
        var hasNew = selection.NewParty is not null;
        if (hasExisting == hasNew)
            throw Validation("party", "Specify either an existing party code or one new party.");
        if (hasExisting)
            return await Authorization.ResolvePartyReference(selection.ExistingPartyCode!, ct);

        var input = selection.NewParty!;
        ValidateParty(input.Party);
        var typeId = await ReferenceId(Db.PartyTypes, input.Party.PartyTypeKey, "party_type", ct);
        var party = new Party(await UniqueCode(Db.Parties, ct), typeId, input.Party.DisplayName,
            input.Party.FirstName, input.Party.LastName, input.Party.OrganizationName,
            input.Party.IdentityNumber, input.Party.Description, Now);
        Db.Parties.Add(party);
        await Save(ct);
        foreach (var contact in input.Contacts ?? [])
        {
            ValidateContact(contact);
            var contactType = await Reference(Db.PartyContactTypes, contact.ContactTypeKey,
                "party_contact_type", ct);
            Db.PartyContacts.Add(new PartyContact(party.Id,
                contactType.Id, contact.Value, NormalizeContact(contactType.Key, contact.Value),
                contact.Label, contact.IsPrimary, Now));
        }
        return party.Id;
    }

    protected async Task ReplaceOccupancyRelations(long unitId,
        IReadOnlyList<UnitOnboardingRelationRequest> requested, DateTimeOffset? effectiveFrom,
        CancellationToken ct)
    {
        var desired = new List<(long PartyId, long TypeId, UnitOnboardingRelationRequest Input)>();
        foreach (var input in requested)
        {
            var type = await Reference(Db.UnitPartyRelationTypes, input.RelationTypeKey,
                "unit_party_relation_type", ct);
            if (!type.IsOccupancyRelation)
                throw Validation("occupancyRelations", "Only tenant or resident relations are allowed here.");
            desired.Add((await ResolveParty(input.Party, ct), type.Id, input));
        }
        var current = await Db.UnitPartyRelations.Where(x => x.UnitId == unitId && x.IsActive &&
            x.EndDate == null && Db.UnitPartyRelationTypes.Any(type =>
                type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation)).ToListAsync(ct);
        foreach (var relation in current.Where(existing =>
            !desired.Any(x => x.PartyId == existing.PartyId && x.TypeId == existing.UnitPartyRelationTypeId)))
            relation.End(effectiveFrom, Now);
        foreach (var item in desired.Where(x =>
            !current.Any(existing => existing.PartyId == x.PartyId &&
                existing.UnitPartyRelationTypeId == x.TypeId)))
            Db.UnitPartyRelations.Add(new UnitPartyRelation(unitId, item.PartyId, item.TypeId,
                item.Input.StartDate, null, item.Input.Notes, Now));
    }

    protected async Task CloseOccupancyRelations(long unitId, DateTimeOffset? endDate, CancellationToken ct)
    {
        var relations = await Db.UnitPartyRelations.Where(x => x.UnitId == unitId && x.IsActive &&
            x.EndDate == null && Db.UnitPartyRelationTypes.Any(type =>
                type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation)).ToListAsync(ct);
        foreach (var relation in relations) relation.End(endDate, Now);
    }

    protected Task<bool> HasOccupancyRelation(long unitId, CancellationToken ct) =>
        Db.UnitPartyRelations.AnyAsync(x => x.UnitId == unitId && x.IsActive && x.EndDate == null &&
            Db.UnitPartyRelationTypes.Any(type =>
                type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation), ct);

    protected async Task ClearPrimaryContact(long partyId, long typeId, CancellationToken ct)
    {
        var contacts = await Db.PartyContacts.Where(x => x.PartyId == partyId &&
            x.PartyContactTypeId == typeId && x.IsActive && x.IsPrimary).ToListAsync(ct);
        foreach (var contact in contacts)
            contact.SetPrimary(false, Now);
    }

    protected async Task<PartyContact> FindContact(long partyId, long typeId, string normalizedValue,
        bool activeOnly, CancellationToken ct)
    {
        var matches = await Db.PartyContacts.Where(x => x.PartyId == partyId &&
            x.PartyContactTypeId == typeId && x.NormalizedValue == normalizedValue &&
            (!activeOnly || x.IsActive)).Take(2).ToListAsync(ct);
        return matches.Count switch
        {
            0 => throw AppException.NotFound("party_contact"),
            1 => matches[0],
            _ => throw AppException.Conflict("party_contact.ambiguous",
                "More than one contact matches the supplied type and value.")
        };
    }

    protected async Task<long> UnitId(string code, CancellationToken ct) =>
        await Db.Units.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("unit");

    protected async Task<long> PartyId(string code, CancellationToken ct) =>
        await Db.Parties.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");

    protected async Task<long> ActivePartyId(string code, CancellationToken ct) =>
        await Db.Parties.Where(x => x.Code == NormalizeCode(code) && x.IsActive).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");

    protected static void ValidateParty(PartyRequest request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        if (string.IsNullOrWhiteSpace(request.PartyTypeKey))
            throw Validation("partyTypeKey", "Party type is required.");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw Validation("displayName", "Display name is required.");
    }

    protected static void ValidateContact(PartyContactRequest request)
    {
        if (request is null) throw Validation("contact", "Contact is required.");
        if (string.IsNullOrWhiteSpace(request.ContactTypeKey))
            throw Validation("contactTypeKey", "Contact type is required.");
        if (string.IsNullOrWhiteSpace(request.Value))
            throw Validation("value", "Contact value is required.");
    }

    protected static void ValidateContactSelector(PartyContactSelectorRequest request)
    {
        if (request is null) throw Validation("contact", "Contact selection is required.");
        if (string.IsNullOrWhiteSpace(request.ContactTypeKey))
            throw Validation("contactTypeKey", "Contact type is required.");
        if (string.IsNullOrWhiteSpace(request.Value))
            throw Validation("value", "Contact value is required.");
    }

    protected static void ValidateContactUpdate(PartyContactUpdateRequest request)
    {
        if (request is null) throw Validation("contact", "Contact update is required.");
        if (string.IsNullOrWhiteSpace(request.ContactTypeKey))
            throw Validation("contactTypeKey", "Contact type is required.");
        if (string.IsNullOrWhiteSpace(request.CurrentValue))
            throw Validation("currentValue", "Current contact value is required.");
        if (string.IsNullOrWhiteSpace(request.Value))
            throw Validation("value", "Contact value is required.");
    }

    protected static void ValidateOccupancy(UnitOccupancyRequest request)
    {
        if (request is null) throw Validation("occupancy", "Occupancy is required.");
        var status = request.Status?.Trim().ToLowerInvariant();
        if (status is not ReferenceKeys.UnitStatuses.Vacant and not ReferenceKeys.UnitStatuses.Occupied)
            throw Validation("occupancy.status", "Status must be vacant or occupied.");
        if (status == ReferenceKeys.UnitStatuses.Vacant && request.OccupantsCount != 0)
            throw Validation("occupancy.occupantsCount", "A vacant unit must have zero occupants.");
        if (status == ReferenceKeys.UnitStatuses.Occupied && request.OccupantsCount <= 0)
            throw Validation("occupancy.occupantsCount", "An occupied unit must have at least one occupant.");
    }

    protected static string NormalizeContact(string typeKey, string value) =>
        typeKey == PartyReferenceKeys.ContactTypes.Email
            ? value.Trim().ToLowerInvariant()
            : NormalizeLoose(value);

    protected static string NormalizeLoose(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    protected static string NormalizeCode(string code) => PublicCode.Normalize(code);
    protected static string NormalizeKey(string key) => string.IsNullOrWhiteSpace(key)
        ? throw Validation("key", "Reference key is required.")
        : key.Trim().ToLowerInvariant();

    protected static async Task<T> Reference<T>(IQueryable<T> set, string key, string resource,
        CancellationToken ct) where T : ReferenceDataItem =>
        await set.SingleOrDefaultAsync(x => x.Key == NormalizeKey(key) && x.IsActive, ct)
        ?? throw AppException.NotFound(resource);

    protected static async Task<long> ReferenceId<T>(IQueryable<T> set, string key, string resource,
        CancellationToken ct) where T : ReferenceDataItem =>
        (await Reference(set, key, resource, ct)).Id;

    protected static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct)
        where T : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new AppException(500, "code.generation_failed", "A unique public code could not be generated.");
    }

    protected async Task Save(CancellationToken ct)
    {
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw AppException.Conflict("concurrency.conflict", "The resource changed since it was read.");
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("persistence.conflict", "The change conflicts with existing data.");
        }
    }

    protected static AppException Validation(string field, string message) =>
        new(400, "validation.failed", "One or more validation errors occurred.",
            new Dictionary<string, string[]> { [field] = [message] });

    protected IQueryable<PartyResponse> PartyProjection(IQueryable<Party> query) =>
        query.Select(x => new PartyResponse(x.Code,
            Db.PartyTypes.Where(type => type.Id == x.PartyTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.DisplayName, x.FirstName, x.LastName, x.OrganizationName, x.IdentityNumber, x.Description,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<PartySummaryResponse> PartySummaryProjection(IQueryable<Party> query) =>
        query.Select(x => new PartySummaryResponse(x.Code,
            Db.PartyTypes.Where(type => type.Id == x.PartyTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.DisplayName, x.FirstName, x.LastName, x.OrganizationName, x.Description,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<PartyContactResponse> ContactProjection(IQueryable<PartyContact> query) =>
        query.Select(x => new PartyContactResponse(
            Db.PartyContactTypes.Where(type => type.Id == x.PartyContactTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Value, x.Label, x.IsPrimary, x.IsVerified, x.IsActive));

    protected IQueryable<UnitPartyRelationResponse> RelationProjection(IQueryable<UnitPartyRelation> query) =>
        query.Select(x => new UnitPartyRelationResponse(
            Db.Parties.Where(party => party.Id == x.PartyId).Select(party => party.Code).Single(),
            Db.Parties.Where(party => party.Id == x.PartyId).Select(party => party.DisplayName).Single(),
            Db.UnitPartyRelationTypes.Where(type => type.Id == x.UnitPartyRelationTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.StartDate, x.EndDate, x.Notes, x.IsActive));

    protected static CurrentOccupancyResponse Current(int count) =>
        new(count == 0 ? ReferenceKeys.UnitStatuses.Vacant : ReferenceKeys.UnitStatuses.Occupied, count);
}

