using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class InvitationService(IApplicationDbContext db, TimeProvider clock,
    IIamSecretProtector protector, IOtpDelivery otpDelivery, IamOptions options,
    ResourceAuthorization authorization, IamService iam)
{
    private static readonly Dictionary<string, string> RelationRoles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [PartyReferenceKeys.RelationTypes.Owner] = "unit_owner",
            [PartyReferenceKeys.RelationTypes.Tenant] = "unit_tenant",
            [PartyReferenceKeys.RelationTypes.Resident] = "unit_resident",
            [PartyReferenceKeys.RelationTypes.LegalRepresentative] = "unit_representative"
        };
    private static readonly HashSet<string> BuildingRoles =
        ["building_manager", "manager_assistant", "accountant"];
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<InvitationResponse> CreateUnit(CreateUnitInvitationRequest request, CancellationToken ct)
    {
        var unitCode = PublicCode.Normalize(request.UnitCode);
        var partyCode = PublicCode.Normalize(request.PartyCode);
        var relationTypeKey = Required(request.RelationTypeKey, "relationTypeKey").ToLowerInvariant();
        var unit = await db.Units.AsNoTracking().SingleOrDefaultAsync(x => x.Code == unitCode && x.IsActive, ct)
            ?? throw AppException.NotFound("unit");
        await authorization.Ensure("invitation_send", null, unit.BuildingId, unit.Id, ct);
        var relation = await db.UnitPartyRelations.AsNoTracking().SingleOrDefaultAsync(x =>
            x.UnitId == unit.Id && x.IsActive && x.EndDate == null &&
            db.UnitPartyRelationTypes.Any(type => type.Id == x.UnitPartyRelationTypeId &&
                type.Key == relationTypeKey) &&
            db.Parties.Any(party => party.Id == x.PartyId && party.Code == partyCode && party.IsActive), ct)
            ?? throw AppException.NotFound("unit_party_relation");
        var relationKey = await db.UnitPartyRelationTypes.Where(x => x.Id == relation.UnitPartyRelationTypeId)
            .Select(x => x.Key).SingleAsync(ct);
        if (!RelationRoles.TryGetValue(relationKey, out var roleKey))
            throw Validation("partyCode", "The Unit relation is not eligible for invitation.");
        var mobile = await PrimaryMobile(relation.PartyId, ct);
        return await Create("unit_person", mobile, roleKey, null, null, unit.Id, relation.Id, ct);
    }

    public async Task<InvitationResponse> CreateBuilding(CreateBuildingInvitationRequest request,
        CancellationToken ct)
    {
        var buildingCode = PublicCode.Normalize(request.BuildingCode);
        var building = await db.Buildings.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Code == buildingCode && x.IsActive, ct) ?? throw AppException.NotFound("building");
        await authorization.Ensure("invitation_send", building.ComplexId, building.Id, null, ct);
        await authorization.Ensure("membership_manage_scoped", building.ComplexId, building.Id, null, ct);
        var roleKey = Required(request.RoleKey, "roleKey").ToLowerInvariant();
        if (!BuildingRoles.Contains(roleKey))
            throw Validation("roleKey", "Role is not allowed for a Building collaborator invitation.");
        return await Create("building_collaborator", IranianMobileNormalizer.Normalize(request.Mobile),
            roleKey, null, building.Id, null, null, ct);
    }

    public async Task<IReadOnlyList<BulkInvitationItemResponse>> CreateBulk(string buildingCode,
        CancellationToken ct)
    {
        buildingCode = PublicCode.Normalize(buildingCode);
        var building = await db.Buildings.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Code == buildingCode && x.IsActive, ct) ?? throw AppException.NotFound("building");
        await authorization.Ensure("invitation_send", building.ComplexId, building.Id, null, ct);
        var relations = await (from relation in db.UnitPartyRelations.AsNoTracking()
                               join unit in db.Units.AsNoTracking() on relation.UnitId equals unit.Id
                               join party in db.Parties.AsNoTracking() on relation.PartyId equals party.Id
                               join type in db.UnitPartyRelationTypes.AsNoTracking() on relation.UnitPartyRelationTypeId equals type.Id
                               where unit.BuildingId == building.Id && unit.IsActive && relation.IsActive &&
                                     relation.EndDate == null && party.IsActive
                               select new { Relation = relation, Unit = unit, Party = party, TypeKey = type.Key })
            .ToListAsync(ct);
        var result = new List<BulkInvitationItemResponse>();
        foreach (var item in relations)
        {
            if (!RelationRoles.TryGetValue(item.TypeKey, out var roleKey))
            {
                result.Add(new(item.Party.DisplayName, item.Unit.Code, "", "not_eligible", null, null));
                continue;
            }
            var mobile = await TryPrimaryMobile(item.Party.Id, ct);
            if (mobile is null)
            {
                result.Add(new(item.Party.DisplayName, item.Unit.Code, roleKey, "no_usable_mobile", null, null));
                continue;
            }
            var roleId = await RoleId(roleKey, IamKeys.Scopes.Unit, ct);
            var userId = await ExistingUserId(mobile, ct);
            if (userId.HasValue && await EquivalentMembership(userId.Value, roleId, null, null, item.Unit.Id, ct))
            {
                result.Add(new(item.Party.DisplayName, item.Unit.Code, roleKey, "already_member", null, null));
                continue;
            }
            var existing = await Pending(mobile, roleId, null, null, item.Unit.Id, ct);
            if (existing is not null)
            {
                result.Add(new(item.Party.DisplayName, item.Unit.Code, roleKey, "already_pending", existing.Code, null));
                continue;
            }
            var created = await Create("unit_person", mobile, roleKey, null, null, item.Unit.Id,
                item.Relation.Id, ct);
            result.Add(new(item.Party.DisplayName, item.Unit.Code, roleKey, "created", created.Code, created.Token));
        }
        return result;
    }

    public async Task<InvitationPreviewResponse> Preview(string token, CancellationToken ct)
    {
        var invitation = await ByToken(token, ct);
        if (invitation.RevokedAtUtc.HasValue) throw AppException.NotFound("invitation");
        var roleTitle = await db.AccessRoles.Where(x => x.Id == invitation.RoleId)
            .Select(x => x.Title).SingleAsync(ct);
        var (kind, name, unitLabel) = await ScopePreview(invitation, ct);
        return new(invitation.InvitationTypeKey, roleTitle, kind, name, unitLabel,
            Status(invitation), invitation.ExpiresAtUtc);
    }

    public async Task<RequestOtpResponse> RequestAcceptanceOtp(InvitationOtpRequest request,
        CancellationToken ct)
    {
        var invitation = await ByToken(request.Token, ct);
        if (invitation.RevokedAtUtc.HasValue || invitation.AcceptedAtUtc.HasValue || invitation.ExpiresAtUtc <= Now)
            throw AppException.NotFound("invitation");
        if (await db.OtpChallenges.AnyAsync(x => x.NormalizedIdentifierValue == invitation.NormalizedIdentifierValue &&
            x.PurposeKey == "invitation_accept" && x.StatusKey == IamKeys.OtpStatuses.Pending &&
            x.SentAtUtc > Now.AddMinutes(-1), ct))
            throw new AppException(429, "otp.cooldown", "Wait before requesting another OTP.");
        if (await db.OtpChallenges.CountAsync(x =>
            x.NormalizedIdentifierValue == invitation.NormalizedIdentifierValue &&
            x.CreatedAtUtc > Now.AddMinutes(-10), ct) >= 5)
            throw new AppException(429, "otp.rate_limited", "Too many OTP requests.");
        var code = protector.CreateOtp();
        var challenge = new OtpChallenge(protector.CreateToken(18), IamKeys.LoginTypes.Mobile,
            invitation.NormalizedIdentifierValue, invitation.NormalizedIdentifierValue, "invitation_accept",
            protector.Hash(code), Now, Now.AddMinutes(options.OtpLifetimeMinutes), options.OtpMaxAttempts);
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        await otpDelivery.Send(invitation.NormalizedIdentifierValue, code, ct);
        return new(challenge.PublicReference, challenge.ExpiresAtUtc);
    }

    public async Task<InvitationAcceptanceResponse> Accept(AcceptInvitationRequest request,
        CancellationToken ct)
    {
        var tokenHash = protector.Hash(Required(request.Token, "token"));
        var reference = Required(request.ChallengeReference, "challengeReference");
        if (!await db.TryReserveOtpAttempt(reference, Now, ct))
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        var reservedChallenge = await db.OtpChallenges.AsNoTracking().SingleOrDefaultAsync(x =>
            x.PublicReference == reference && x.PurposeKey == "invitation_accept", ct);
        if (reservedChallenge is null || !protector.Verify(Required(request.OtpCode, "otpCode"),
            reservedChallenge.CodeHash))
        {
            await db.MarkOtpAttemptFailed(reference, Now, ct);
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        }
        try
        {
            return await db.ExecuteInTransaction<InvitationAcceptanceResponse>(async transactionToken =>
            {
                await db.LockInvitation(tokenHash, transactionToken);
                var invitation = await db.Invitations.SingleOrDefaultAsync(x => x.TokenHash == tokenHash,
                    transactionToken) ?? throw AppException.NotFound("invitation");
                if (invitation.RevokedAtUtc.HasValue || invitation.ExpiresAtUtc <= Now)
                    throw AppException.NotFound("invitation");
                if (invitation.AcceptedAtUtc.HasValue)
                    throw AppException.Conflict("invitation.already_accepted", "Invitation was already accepted.");
                if (invitation.SourceUnitPartyRelationId.HasValue &&
                    !await db.UnitPartyRelations.AnyAsync(x => x.Id == invitation.SourceUnitPartyRelationId &&
                        x.IsActive && x.EndDate == null, transactionToken))
                    throw AppException.Conflict("invitation.source_relation_ended",
                        "The Unit relationship is no longer active.");
                var challenge = await db.OtpChallenges.SingleOrDefaultAsync(x =>
                    x.PublicReference == reference && x.PurposeKey == "invitation_accept" &&
                    x.NormalizedIdentifierValue == invitation.NormalizedIdentifierValue, transactionToken)
                    ?? throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
                if (!protector.Verify(request.OtpCode, challenge.CodeHash))
                    throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
                challenge.Verify(Now);
                challenge.Consume(Now);
                await db.SaveChangesAsync(transactionToken);
                var identity = await iam.ProvisionInvitationIdentity(invitation.NormalizedIdentifierValue,
                    request.DisplayName, request.ClientTypeKey, request.DeviceIdentifier, transactionToken);
                var roleKey = await db.AccessRoles.Where(x => x.Id == invitation.RoleId)
                    .Select(x => x.Key).SingleAsync(transactionToken);
                await EndStaleEquivalentMemberships(identity.UserId, invitation.RoleId,
                    invitation.ComplexId, invitation.BuildingId, invitation.UnitId, transactionToken);
                if (!await EquivalentMembership(identity.UserId, invitation.RoleId, invitation.ComplexId,
                    invitation.BuildingId, invitation.UnitId, transactionToken))
                    db.AccessMemberships.Add(new AccessMembership(await UniqueCode(db.AccessMemberships,
                        transactionToken), identity.UserId, invitation.RoleId, invitation.ComplexId,
                        invitation.BuildingId, invitation.UnitId, invitation.SourceUnitPartyRelationId, Now));
                invitation.Accept(identity.UserId, Now);
                await db.SaveChangesAsync(transactionToken);
                var (scopeKind, scopeCode) = await ScopeIdentity(invitation, transactionToken);
                return new("accepted", roleKey, scopeKind, scopeCode, identity.Tokens);
            }, ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries) db.Detach(entry.Entity);
            throw AppException.Conflict("invitation.concurrent_acceptance", "Invitation was already processed.");
        }
        catch (DbUpdateException exception) when (db.IsUniqueViolation(exception))
        {
            throw AppException.Conflict("invitation.concurrent_acceptance", "Invitation was already processed.");
        }
    }

    public async Task Revoke(string code, CancellationToken ct)
    {
        code = PublicCode.Normalize(code);
        var state = await db.Invitations.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code, ct)
            ?? throw AppException.NotFound("invitation");
        await EnsureScope(state, "invitation_revoke", ct);
        var expired = await db.ExecuteInTransaction(async token =>
        {
            await db.LockInvitation(state.TokenHash, token);
            var invitation = await db.Invitations.SingleAsync(x => x.Id == state.Id, token);
            if (invitation.ExpiredAtUtc.HasValue || invitation.ExpiresAtUtc <= Now)
            {
                invitation.Expire(Now);
                await db.SaveChangesAsync(token);
                return true;
            }
            invitation.Revoke(Now);
            await db.SaveChangesAsync(token);
            return false;
        }, ct);
        if (expired) throw AppException.Conflict("invitation.expired", "Expired invitation cannot be revoked.");
    }

    public async Task<IReadOnlyList<InvitationListItemResponse>> List(string? buildingCode, string? unitCode,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(buildingCode) == string.IsNullOrWhiteSpace(unitCode))
            throw Validation("scope", "Specify exactly one Building or Unit code.");
        IQueryable<Invitation> query = db.Invitations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buildingCode))
        {
            var code = PublicCode.Normalize(buildingCode);
            var building = await db.Buildings.SingleOrDefaultAsync(x => x.Code == code, ct)
                ?? throw AppException.NotFound("building");
            await authorization.Ensure("invitation_send", building.ComplexId, building.Id, null, ct);
            var unitIds = db.Units.Where(x => x.BuildingId == building.Id).Select(x => x.Id);
            query = query.Where(x => x.BuildingId == building.Id || x.UnitId.HasValue && unitIds.Contains(x.UnitId.Value));
        }
        else
        {
            var code = PublicCode.Normalize(unitCode!);
            var unit = await db.Units.SingleOrDefaultAsync(x => x.Code == code, ct)
                ?? throw AppException.NotFound("unit");
            await authorization.Ensure("invitation_send", null, unit.BuildingId, unit.Id, ct);
            query = query.Where(x => x.UnitId == unit.Id);
        }
        var invitations = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        var result = new List<InvitationListItemResponse>();
        foreach (var invitation in invitations)
        {
            var item = await Response(invitation, "", ct);
            result.Add(new(item.Code, item.TypeKey, item.RoleKey, item.ScopeKind, item.ScopeCode,
                item.ScopeName, item.Status, item.ExpiresAtUtc));
        }
        return result;
    }

    private async Task<InvitationResponse> Create(string type, string mobile, string roleKey,
        long? complexId, long? buildingId, long? unitId, long? sourceRelationId, CancellationToken ct)
    {
        var scopeKind = unitId.HasValue ? IamKeys.Scopes.Unit : buildingId.HasValue
            ? IamKeys.Scopes.Building : IamKeys.Scopes.Complex;
        var roleId = await RoleId(roleKey, scopeKind, ct);
        try
        {
            return await db.ExecuteInTransaction<InvitationResponse>(async transactionToken =>
            {
                var userId = await ExistingUserId(mobile, transactionToken);
                if (userId.HasValue && await EquivalentMembership(userId.Value, roleId, complexId,
                    buildingId, unitId, transactionToken))
                    throw AppException.Conflict("invitation.already_member",
                        "Target already has this Membership.");

                var equivalent = await db.Invitations.Where(x =>
                    x.NormalizedIdentifierValue == mobile && x.RoleId == roleId &&
                    x.ComplexId == complexId && x.BuildingId == buildingId && x.UnitId == unitId &&
                    x.IsActive && x.AcceptedAtUtc == null && x.RevokedAtUtc == null).ToListAsync(transactionToken);
                foreach (var stale in equivalent.Where(x => x.ExpiresAtUtc <= Now)) stale.Expire(Now);
                if (equivalent.Any(x => x.ExpiresAtUtc > Now))
                    throw AppException.Conflict("invitation.already_pending",
                        "An equivalent invitation is already pending.");
                if (equivalent.Count != 0) await db.SaveChangesAsync(transactionToken);

                var plainToken = protector.CreateToken(24);
                var invitation = new Invitation(await UniqueCode(db.Invitations, transactionToken),
                    protector.Hash(plainToken), mobile, type, roleId, complexId, buildingId, unitId,
                    authorization.UserId, sourceRelationId, Now.AddDays(7), Now);
                db.Invitations.Add(invitation);
                await db.SaveChangesAsync(transactionToken);
                return await Response(invitation, plainToken, transactionToken);
            }, ct);
        }
        catch (DbUpdateException exception) when (db.IsUniqueViolation(exception))
        {
            throw AppException.Conflict("invitation.already_pending",
                "An equivalent invitation is already pending.");
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries) db.Detach(entry.Entity);
            throw AppException.Conflict("invitation.already_pending",
                "An equivalent invitation is already pending.");
        }
    }

    private async Task<InvitationResponse> Response(Invitation invitation, string token, CancellationToken ct)
    {
        var roleKey = await db.AccessRoles.Where(x => x.Id == invitation.RoleId).Select(x => x.Key).SingleAsync(ct);
        var (kind, code) = await ScopeIdentity(invitation, ct);
        var (_, name, _) = await ScopePreview(invitation, ct);
        return new(invitation.Code, token, invitation.InvitationTypeKey, roleKey, kind, code, name,
            Status(invitation), invitation.ExpiresAtUtc);
    }

    private async Task EnsureScope(Invitation invitation, string permission, CancellationToken ct) =>
        await authorization.Ensure(permission, invitation.ComplexId, invitation.BuildingId,
            invitation.UnitId, ct);

    private async Task<long> RoleId(string roleKey, string scope, CancellationToken ct)
    {
        var role = await db.AccessRoles.SingleOrDefaultAsync(x => x.Key == roleKey && x.IsActive, ct)
            ?? throw AppException.NotFound("access_role");
        if (!await db.RoleAllowedScopes.AnyAsync(x => x.RoleId == role.Id && x.ScopeKindKey == scope, ct))
            throw Validation("roleKey", "Role is not allowed at the target scope.");
        return role.Id;
    }

    private async Task<string> PrimaryMobile(long partyId, CancellationToken ct) =>
        await TryPrimaryMobile(partyId, ct) ?? throw Validation("partyCode", "Party has no usable mobile.");

    private async Task<string?> TryPrimaryMobile(long partyId, CancellationToken ct)
    {
        var mobileTypeId = await db.PartyContactTypes.Where(x => x.Key == PartyReferenceKeys.ContactTypes.Mobile)
            .Select(x => x.Id).SingleAsync(ct);
        var value = await db.PartyContacts.AsNoTracking().Where(x => x.PartyId == partyId && x.IsActive &&
            x.PartyContactTypeId == mobileTypeId).OrderByDescending(x => x.IsPrimary)
            .Select(x => x.Value).FirstOrDefaultAsync(ct);
        if (value is null) return null;
        try { return IranianMobileNormalizer.Normalize(value); }
        catch (AppException) { return null; }
    }

    private async Task<long?> ExistingUserId(string mobile, CancellationToken ct) =>
        await db.UserLoginMethods.AsNoTracking().Where(x => x.IsActive && x.IsVerified &&
            x.LoginTypeKey == IamKeys.LoginTypes.Mobile && x.NormalizedIdentifierValue == mobile)
            .Select(x => (long?)x.UserId).SingleOrDefaultAsync(ct);

    private Task<bool> EquivalentMembership(long userId, long roleId, long? complexId,
        long? buildingId, long? unitId, CancellationToken ct) => db.AccessMemberships.AnyAsync(x =>
        x.UserId == userId && x.RoleId == roleId && x.ComplexId == complexId &&
        x.BuildingId == buildingId && x.UnitId == unitId && x.IsActive && x.EndsAtUtc == null &&
        (!x.SourceUnitPartyRelationId.HasValue || db.UnitPartyRelations.Any(relation =>
            relation.Id == x.SourceUnitPartyRelationId && relation.IsActive && relation.EndDate == null &&
            (!relation.StartDate.HasValue || relation.StartDate <= Now))), ct);

    private async Task EndStaleEquivalentMemberships(long userId, long roleId, long? complexId,
        long? buildingId, long? unitId, CancellationToken ct)
    {
        var candidates = await db.AccessMemberships.Where(x => x.UserId == userId && x.RoleId == roleId &&
            x.ComplexId == complexId && x.BuildingId == buildingId && x.UnitId == unitId &&
            x.IsActive && x.EndsAtUtc == null && x.SourceUnitPartyRelationId.HasValue).ToListAsync(ct);
        foreach (var membership in candidates)
            if (!await db.UnitPartyRelations.AnyAsync(relation =>
                relation.Id == membership.SourceUnitPartyRelationId && relation.IsActive &&
                relation.EndDate == null && (!relation.StartDate.HasValue || relation.StartDate <= Now), ct))
                membership.End(Now);
    }

    private Task<Invitation?> Pending(string mobile, long roleId, long? complexId, long? buildingId,
        long? unitId, CancellationToken ct) => db.Invitations.AsNoTracking().FirstOrDefaultAsync(x =>
        x.NormalizedIdentifierValue == mobile && x.RoleId == roleId && x.ComplexId == complexId &&
        x.BuildingId == buildingId && x.UnitId == unitId && x.IsActive && x.AcceptedAtUtc == null &&
        x.RevokedAtUtc == null && x.ExpiresAtUtc > Now, ct);

    private async Task<Invitation> ByToken(string token, CancellationToken ct) =>
        await db.Invitations.AsNoTracking().SingleOrDefaultAsync(x =>
            x.TokenHash == protector.Hash(Required(token, "token")), ct) ?? throw AppException.NotFound("invitation");

    private async Task<(string Kind, string Code)> ScopeIdentity(Invitation invitation, CancellationToken ct)
    {
        if (invitation.UnitId.HasValue) return (IamKeys.Scopes.Unit,
            await db.Units.Where(x => x.Id == invitation.UnitId).Select(x => x.Code).SingleAsync(ct));
        if (invitation.BuildingId.HasValue) return (IamKeys.Scopes.Building,
            await db.Buildings.Where(x => x.Id == invitation.BuildingId).Select(x => x.Code).SingleAsync(ct));
        return (IamKeys.Scopes.Complex,
            await db.Complexes.Where(x => x.Id == invitation.ComplexId).Select(x => x.Code).SingleAsync(ct));
    }

    private async Task<(string Kind, string Name, string? UnitLabel)> ScopePreview(Invitation invitation,
        CancellationToken ct)
    {
        if (invitation.UnitId.HasValue)
        {
            var value = await (from unit in db.Units
                               where unit.Id == invitation.UnitId
                               join building in db.Buildings on unit.BuildingId equals building.Id
                               select new { building.Name, unit.UnitNumber }).SingleAsync(ct);
            return (IamKeys.Scopes.Unit, value.Name, value.UnitNumber);
        }
        if (invitation.BuildingId.HasValue) return (IamKeys.Scopes.Building,
            await db.Buildings.Where(x => x.Id == invitation.BuildingId).Select(x => x.Name).SingleAsync(ct), null);
        return (IamKeys.Scopes.Complex,
            await db.Complexes.Where(x => x.Id == invitation.ComplexId).Select(x => x.Name).SingleAsync(ct), null);
    }

    private string Status(Invitation invitation) => invitation.AcceptedAtUtc.HasValue ? "accepted" :
        invitation.RevokedAtUtc.HasValue ? "revoked" : invitation.ExpiresAtUtc <= Now ? "expired" : "pending";
    private static string Required(string? value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw Validation(field, $"{field} is required.") : value.Trim();
    private static AppException Validation(string field, string message) => new(400, "validation.failed",
        "Request validation failed.", new Dictionary<string, string[]> { [field] = [message] });
    private static async Task<string> UniqueCode<TEntity>(DbSet<TEntity> set, CancellationToken ct)
        where TEntity : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new InvalidOperationException("Could not allocate a public code.");
    }
}
