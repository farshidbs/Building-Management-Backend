using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed record PartyRequest(string PartyTypeKey, string DisplayName, string? FirstName = null,
    string? LastName = null, string? OrganizationName = null, string? IdentityNumber = null,
    string? Description = null);

public sealed record PartyContactRequest(string ContactTypeKey, string Value, string? Label = null,
    bool IsPrimary = false);

public sealed record NewPartyInput(PartyRequest Party,
    IReadOnlyList<PartyContactRequest>? Contacts = null);

public sealed record PartySelectionRequest(string? ExistingPartyCode, NewPartyInput? NewParty);

public sealed record UnitOnboardingRelationRequest(string RelationTypeKey, PartySelectionRequest Party,
    DateTimeOffset? StartDate = null, string? Notes = null);

public sealed record UnitOccupancyRequest(string Status, int OccupantsCount, DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<UnitOnboardingRelationRequest>? Relations = null, string? Notes = null);

public sealed record OccupancyChangeRequest(int OccupantsCount, DateTimeOffset? EffectiveFrom = null,
    IReadOnlyList<UnitOnboardingRelationRequest>? OccupancyRelations = null, string? Notes = null);

public sealed record PartyResponse(string Code, ReferenceValueResponse PartyType, string DisplayName,
    string? FirstName, string? LastName, string? OrganizationName, string? IdentityNumber,
    string? Description,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record PartySummaryResponse(string Code, ReferenceValueResponse PartyType,
    string DisplayName, string? FirstName, string? LastName, string? OrganizationName,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);

public sealed record PartyContactResponse(ReferenceValueResponse ContactType, string Value,
    string? Label, bool IsPrimary, bool IsVerified, bool IsActive);

public sealed record UnitPartyRelationResponse(string PartyCode, string DisplayName,
    ReferenceValueResponse RelationType, DateTimeOffset? StartDate, DateTimeOffset? EndDate,
    string? Notes, bool IsActive);

public sealed record UnitOccupancyHistoryResponse(int OccupantsCount, DateTimeOffset? EffectiveFrom,
    DateTimeOffset? EffectiveTo, string? Notes, bool IsActive);

public sealed record CurrentOccupancyResponse(string Status, int OccupantsCount);

public sealed class PartyOccupancyService(IApplicationDbContext db, TimeProvider clock)
{
    private DateTimeOffset Now => clock.GetUtcNow();

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

    public async Task<PartyContactResponse> AddContact(string partyCode, PartyContactRequest request,
        CancellationToken ct)
    {
        ValidateContact(request);
        var partyId = await ActivePartyId(partyCode, ct);
        var type = await Reference(db.PartyContactTypes, request.ContactTypeKey, "party_contact_type", ct);
        if (request.IsPrimary)
            await ClearPrimaryContact(partyId, type.Id, ct);
        var contact = new PartyContact(partyId, type.Id,
            request.Value, NormalizeContact(type.Key, request.Value), request.Label, request.IsPrimary, Now);
        db.PartyContacts.Add(contact);
        await Save(ct);
        return await ContactProjection(db.PartyContacts.Where(x => x.Id == contact.Id)).SingleAsync(ct);
    }

    public async Task<IReadOnlyList<PartyContactResponse>> GetContacts(string partyCode, CancellationToken ct)
    {
        var partyId = await PartyId(partyCode, ct);
        return await ContactProjection(db.PartyContacts.AsNoTracking().Where(x => x.PartyId == partyId))
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.IsPrimary).ToListAsync(ct);
    }

    public async Task OnboardUnit(Unit unit, UnitOccupancyRequest request, CancellationToken ct)
    {
        ValidateOccupancy(request);
        await Save(ct);
        unit.ChangeOccupancy(request.OccupantsCount, Now);
        db.UnitOccupancyHistories.Add(new UnitOccupancyHistory(unit.Id, request.OccupantsCount,
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
        return await db.ExecuteInTransaction(async token =>
        {
            var unit = await db.Units.SingleOrDefaultAsync(x => x.Code == NormalizeCode(unitCode), token)
                ?? throw AppException.NotFound("unit");
            var current = await db.UnitOccupancyHistories.SingleOrDefaultAsync(x =>
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
            db.UnitOccupancyHistories.Add(new UnitOccupancyHistory(unit.Id, request.OccupantsCount,
                request.EffectiveFrom, request.Notes, Now));
            await Save(token);
            return Current(unit.CurrentOccupantsCount);
        }, ct);
    }

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

    private async Task<UnitPartyRelation> AddRelation(long unitId, UnitOnboardingRelationRequest request,
        CancellationToken ct)
    {
        if (request is null) throw Validation("relation", "Relation is required.");
        var type = await Reference(db.UnitPartyRelationTypes, request.RelationTypeKey,
            "unit_party_relation_type", ct);
        var partyId = await ResolveParty(request.Party, ct);
        var relation = new UnitPartyRelation(unitId, partyId, type.Id,
            request.StartDate, null, request.Notes, Now);
        db.UnitPartyRelations.Add(relation);
        return relation;
    }

    private async Task<long> ResolveParty(PartySelectionRequest selection, CancellationToken ct)
    {
        if (selection is null)
            throw Validation("party", "Party selection is required.");
        var hasExisting = !string.IsNullOrWhiteSpace(selection.ExistingPartyCode);
        var hasNew = selection.NewParty is not null;
        if (hasExisting == hasNew)
            throw Validation("party", "Specify either an existing party code or one new party.");
        if (hasExisting)
            return await ActivePartyId(selection.ExistingPartyCode!, ct);

        var input = selection.NewParty!;
        ValidateParty(input.Party);
        var typeId = await ReferenceId(db.PartyTypes, input.Party.PartyTypeKey, "party_type", ct);
        var party = new Party(await UniqueCode(db.Parties, ct), typeId, input.Party.DisplayName,
            input.Party.FirstName, input.Party.LastName, input.Party.OrganizationName,
            input.Party.IdentityNumber, input.Party.Description, Now);
        db.Parties.Add(party);
        await Save(ct);
        foreach (var contact in input.Contacts ?? [])
        {
            ValidateContact(contact);
            var contactType = await Reference(db.PartyContactTypes, contact.ContactTypeKey,
                "party_contact_type", ct);
            db.PartyContacts.Add(new PartyContact(party.Id,
                contactType.Id, contact.Value, NormalizeContact(contactType.Key, contact.Value),
                contact.Label, contact.IsPrimary, Now));
        }
        return party.Id;
    }

    private async Task ReplaceOccupancyRelations(long unitId,
        IReadOnlyList<UnitOnboardingRelationRequest> requested, DateTimeOffset? effectiveFrom,
        CancellationToken ct)
    {
        var desired = new List<(long PartyId, long TypeId, UnitOnboardingRelationRequest Input)>();
        foreach (var input in requested)
        {
            var type = await Reference(db.UnitPartyRelationTypes, input.RelationTypeKey,
                "unit_party_relation_type", ct);
            if (!type.IsOccupancyRelation)
                throw Validation("occupancyRelations", "Only tenant or resident relations are allowed here.");
            desired.Add((await ResolveParty(input.Party, ct), type.Id, input));
        }
        var current = await db.UnitPartyRelations.Where(x => x.UnitId == unitId && x.IsActive &&
            x.EndDate == null && db.UnitPartyRelationTypes.Any(type =>
                type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation)).ToListAsync(ct);
        foreach (var relation in current.Where(existing =>
            !desired.Any(x => x.PartyId == existing.PartyId && x.TypeId == existing.UnitPartyRelationTypeId)))
            relation.End(effectiveFrom, Now);
        foreach (var item in desired.Where(x =>
            !current.Any(existing => existing.PartyId == x.PartyId &&
                existing.UnitPartyRelationTypeId == x.TypeId)))
            db.UnitPartyRelations.Add(new UnitPartyRelation(unitId, item.PartyId, item.TypeId,
                item.Input.StartDate, null, item.Input.Notes, Now));
    }

    private async Task CloseOccupancyRelations(long unitId, DateTimeOffset? endDate, CancellationToken ct)
    {
        var relations = await db.UnitPartyRelations.Where(x => x.UnitId == unitId && x.IsActive &&
            x.EndDate == null && db.UnitPartyRelationTypes.Any(type =>
                type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation)).ToListAsync(ct);
        foreach (var relation in relations) relation.End(endDate, Now);
    }

    private Task<bool> HasOccupancyRelation(long unitId, CancellationToken ct) =>
        db.UnitPartyRelations.AnyAsync(x => x.UnitId == unitId && x.IsActive && x.EndDate == null &&
            db.UnitPartyRelationTypes.Any(type =>
                type.Id == x.UnitPartyRelationTypeId && type.IsOccupancyRelation), ct);

    private async Task ClearPrimaryContact(long partyId, long typeId, CancellationToken ct)
    {
        var contacts = await db.PartyContacts.Where(x => x.PartyId == partyId &&
            x.PartyContactTypeId == typeId && x.IsActive && x.IsPrimary).ToListAsync(ct);
        foreach (var contact in contacts)
            contact.Update(contact.PartyContactTypeId, contact.Value, contact.NormalizedValue,
                contact.Label, false, Now);
    }

    private async Task<long> UnitId(string code, CancellationToken ct) =>
        await db.Units.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("unit");

    private async Task<long> PartyId(string code, CancellationToken ct) =>
        await db.Parties.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");

    private async Task<long> ActivePartyId(string code, CancellationToken ct) =>
        await db.Parties.Where(x => x.Code == NormalizeCode(code) && x.IsActive).Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");

    private static void ValidateParty(PartyRequest request)
    {
        if (request is null) throw Validation("request", "A request body is required.");
        if (string.IsNullOrWhiteSpace(request.PartyTypeKey))
            throw Validation("partyTypeKey", "Party type is required.");
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            throw Validation("displayName", "Display name is required.");
    }

    private static void ValidateContact(PartyContactRequest request)
    {
        if (request is null) throw Validation("contact", "Contact is required.");
        if (string.IsNullOrWhiteSpace(request.ContactTypeKey))
            throw Validation("contactTypeKey", "Contact type is required.");
        if (string.IsNullOrWhiteSpace(request.Value))
            throw Validation("value", "Contact value is required.");
    }

    private static void ValidateOccupancy(UnitOccupancyRequest request)
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

    private static string NormalizeContact(string typeKey, string value) =>
        typeKey == PartyReferenceKeys.ContactTypes.Email
            ? value.Trim().ToLowerInvariant()
            : NormalizeLoose(value);

    private static string NormalizeLoose(string value) =>
        string.Concat(value.Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static string NormalizeCode(string code) => PublicCode.Normalize(code);
    private static string NormalizeKey(string key) => string.IsNullOrWhiteSpace(key)
        ? throw Validation("key", "Reference key is required.")
        : key.Trim().ToLowerInvariant();

    private static async Task<T> Reference<T>(IQueryable<T> set, string key, string resource,
        CancellationToken ct) where T : ReferenceDataItem =>
        await set.SingleOrDefaultAsync(x => x.Key == NormalizeKey(key) && x.IsActive, ct)
        ?? throw AppException.NotFound(resource);

    private static async Task<long> ReferenceId<T>(IQueryable<T> set, string key, string resource,
        CancellationToken ct) where T : ReferenceDataItem =>
        (await Reference(set, key, resource, ct)).Id;

    private static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct)
        where T : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new AppException(500, "code.generation_failed", "A unique public code could not be generated.");
    }

    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException)
        {
            throw AppException.Conflict("concurrency.conflict", "The resource changed since it was read.");
        }
        catch (DbUpdateException)
        {
            throw AppException.Conflict("persistence.conflict", "The change conflicts with existing data.");
        }
    }

    private static AppException Validation(string field, string message) =>
        new(400, "validation.failed", "One or more validation errors occurred.",
            new Dictionary<string, string[]> { [field] = [message] });

    private IQueryable<PartyResponse> PartyProjection(IQueryable<Party> query) =>
        query.Select(x => new PartyResponse(x.Code,
            db.PartyTypes.Where(type => type.Id == x.PartyTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.DisplayName, x.FirstName, x.LastName, x.OrganizationName, x.IdentityNumber, x.Description,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<PartySummaryResponse> PartySummaryProjection(IQueryable<Party> query) =>
        query.Select(x => new PartySummaryResponse(x.Code,
            db.PartyTypes.Where(type => type.Id == x.PartyTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.DisplayName, x.FirstName, x.LastName, x.OrganizationName, x.Description,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<PartyContactResponse> ContactProjection(IQueryable<PartyContact> query) =>
        query.Select(x => new PartyContactResponse(
            db.PartyContactTypes.Where(type => type.Id == x.PartyContactTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Value, x.Label, x.IsPrimary, x.IsVerified, x.IsActive));

    private IQueryable<UnitPartyRelationResponse> RelationProjection(IQueryable<UnitPartyRelation> query) =>
        query.Select(x => new UnitPartyRelationResponse(
            db.Parties.Where(party => party.Id == x.PartyId).Select(party => party.Code).Single(),
            db.Parties.Where(party => party.Id == x.PartyId).Select(party => party.DisplayName).Single(),
            db.UnitPartyRelationTypes.Where(type => type.Id == x.UnitPartyRelationTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.StartDate, x.EndDate, x.Notes, x.IsActive));
}
