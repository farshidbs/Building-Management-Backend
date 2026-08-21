using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed record AccessContextResponse(string ScopeKind, string ScopeCode, string ScopeName,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);
public sealed record AccessibleResourceIds(IReadOnlyList<long> ComplexIds,
    IReadOnlyList<long> BuildingIds, IReadOnlyList<long> UnitIds);

public sealed class AccessAuthorizationService(IApplicationDbContext db, TimeProvider clock)
{
    private sealed record ResourceScope(long? ComplexId, long? BuildingId, long? UnitId, bool Confidential = false);

    public async Task<bool> CanReadStoredFile(long userId, long storedFileId, CancellationToken ct)
    {
        var scopes = new List<ResourceScope>();
        scopes.AddRange(await db.BuildingGalleryFiles.AsNoTracking().Where(x => x.StoredFileId == storedFileId && x.IsActive).Select(x => new ResourceScope(null, x.BuildingId, null)).ToListAsync(ct));
        scopes.AddRange(await db.BuildingDocuments.AsNoTracking().Where(x => x.StoredFileId == storedFileId && x.IsActive).Select(x => new ResourceScope(null, x.BuildingId, null, x.IsConfidential)).ToListAsync(ct));
        scopes.AddRange(await db.ComplexGalleryFiles.AsNoTracking().Where(x => x.StoredFileId == storedFileId && x.IsActive).Select(x => new ResourceScope(x.ComplexId, null, null)).ToListAsync(ct));
        scopes.AddRange(await db.ComplexDocuments.AsNoTracking().Where(x => x.StoredFileId == storedFileId && x.IsActive).Select(x => new ResourceScope(x.ComplexId, null, null, x.IsConfidential)).ToListAsync(ct));
        scopes.AddRange(await (from relation in db.AssetGalleryFiles.AsNoTracking() join asset in db.Assets on relation.AssetId equals asset.Id where relation.StoredFileId == storedFileId && relation.IsActive select new ResourceScope(asset.ComplexId, asset.BuildingId, null)).ToListAsync(ct));
        scopes.AddRange(await (from relation in db.AssetDocuments.AsNoTracking() join asset in db.Assets on relation.AssetId equals asset.Id where relation.StoredFileId == storedFileId && relation.IsActive select new ResourceScope(asset.ComplexId, asset.BuildingId, null, relation.IsConfidential)).ToListAsync(ct));
        scopes.AddRange(await (from relation in db.AssetEventFiles.AsNoTracking() join assetEvent in db.AssetEvents on relation.AssetEventId equals assetEvent.Id join asset in db.Assets on assetEvent.AssetId equals asset.Id where relation.StoredFileId == storedFileId && relation.IsActive select new ResourceScope(asset.ComplexId, asset.BuildingId, null)).ToListAsync(ct));
        scopes.AddRange(await (from relation in db.ExpenseDocuments.AsNoTracking() join expense in db.Expenses on relation.ExpenseId equals expense.Id where relation.StoredFileId == storedFileId && relation.IsActive select new ResourceScope(expense.ComplexId, expense.BuildingId, null)).ToListAsync(ct));
        scopes.AddRange(await (from relation in db.ExpenseDisbursementFiles.AsNoTracking() join disbursement in db.ExpenseDisbursements on relation.ExpenseDisbursementId equals disbursement.Id join expense in db.Expenses on disbursement.ExpenseId equals expense.Id where relation.StoredFileId == storedFileId && relation.IsActive select new ResourceScope(expense.ComplexId, expense.BuildingId, null)).ToListAsync(ct));
        scopes.AddRange(await (from relation in db.PaymentEvidenceFiles.AsNoTracking() join payment in db.Payments on relation.PaymentId equals payment.Id join account in db.FinancialAccounts on payment.UnitAccountId equals account.Id where relation.StoredFileId == storedFileId && relation.IsActive select new ResourceScope(account.ComplexId, account.BuildingId, account.UnitId)).ToListAsync(ct));
        foreach (var scope in scopes.Distinct())
        {
            try { await Ensure(userId, scope.Confidential ? "file_read_confidential" : "file_read", scope.ComplexId, scope.BuildingId, scope.UnitId, ct); return true; }
            catch (AppException exception) when (exception.Status is 403 or 404) { }
        }
        return false;
    }

    public async Task EnsureParty(long userId, string permissionKey, long partyId, CancellationToken ct)
    {
        var unitScopes = await (from relation in db.UnitPartyRelations.AsNoTracking()
                                join unit in db.Units.AsNoTracking() on relation.UnitId equals unit.Id
                                where relation.PartyId == partyId && relation.IsActive
                                select new { UnitId = unit.Id, unit.BuildingId }).Distinct().ToListAsync(ct);
        foreach (var scope in unitScopes)
        {
            try
            {
                await Ensure(userId, permissionKey, null, scope.BuildingId, scope.UnitId, ct);
                return;
            }
            catch (AppException exception) when (exception.Status == 403) { }
        }
        throw new AppException(403, "authorization.denied", "You are not allowed to access this resource.");
    }

    public async Task<long> ResolvePartyReference(long userId, string partyCode, CancellationToken ct)
    {
        var normalized = PublicCode.Normalize(partyCode);
        var partyId = await db.Parties.AsNoTracking()
            .Where(x => x.Code == normalized && x.IsActive)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("party");
        var isOwnIdentityParty = await db.UserPartyLinks.AsNoTracking().AnyAsync(x =>
            x.UserId == userId && x.PartyId == partyId && x.IsActive && x.UnlinkedAtUtc == null, ct);
        if (!isOwnIdentityParty)
            await EnsureParty(userId, "party_view", partyId, ct);
        return partyId;
    }

    public async Task<bool> HasActiveMembership(long userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return await db.AccessMemberships.AsNoTracking().AnyAsync(x =>
            x.UserId == userId && x.IsActive && x.EndsAtUtc == null && x.StartsAtUtc <= now &&
            (!x.SourceUnitPartyRelationId.HasValue || db.UnitPartyRelations.Any(relation =>
                relation.Id == x.SourceUnitPartyRelationId && relation.IsActive &&
                (!relation.StartDate.HasValue || relation.StartDate <= now) && relation.EndDate == null)), ct);
    }
    public async Task<IReadOnlyList<AccessContextResponse>> GetContexts(long userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var memberships = await db.AccessMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EndsAtUtc == null && x.StartsAtUtc <= now &&
                (!x.SourceUnitPartyRelationId.HasValue || db.UnitPartyRelations.Any(relation =>
                    relation.Id == x.SourceUnitPartyRelationId && relation.IsActive &&
                    (!relation.StartDate.HasValue || relation.StartDate <= now) && relation.EndDate == null)))
            .ToListAsync(ct);
        var result = new List<AccessContextResponse>();
        foreach (var scopeGroup in memberships.GroupBy(x => new { x.ComplexId, x.BuildingId, x.UnitId }))
        {
            var roleIds = scopeGroup.Select(m => m.RoleId).ToArray();
            var roles = await db.AccessRoles.AsNoTracking().Where(x => roleIds.Contains(x.Id)).Select(x => x.Key).ToListAsync(ct);
            var permissions = await (from rp in db.AccessRolePermissions.AsNoTracking()
                                     join permission in db.AccessPermissions.AsNoTracking() on rp.PermissionId equals permission.Id
                                     where roleIds.Contains(rp.RoleId) && rp.EffectKey == IamKeys.Effects.Allow
                                     select permission.Key).Distinct().ToListAsync(ct);
            string kind; string code; string name;
            if (scopeGroup.Key.UnitId.HasValue) { var resource = await db.Units.AsNoTracking().Where(x => x.Id == scopeGroup.Key.UnitId).Select(x => new { x.Code, x.UnitNumber }).SingleAsync(ct); kind = IamKeys.Scopes.Unit; code = resource.Code; name = resource.UnitNumber; }
            else if (scopeGroup.Key.BuildingId.HasValue) { var resource = await db.Buildings.AsNoTracking().Where(x => x.Id == scopeGroup.Key.BuildingId).Select(x => new { x.Code, x.Name }).SingleAsync(ct); kind = IamKeys.Scopes.Building; code = resource.Code; name = resource.Name; }
            else { var resource = await db.Complexes.AsNoTracking().Where(x => x.Id == scopeGroup.Key.ComplexId).Select(x => new { x.Code, x.Name }).SingleAsync(ct); kind = IamKeys.Scopes.Complex; code = resource.Code; name = resource.Name; }
            result.Add(new(kind, code, name, roles, permissions));
        }
        return result;
    }

    public async Task EnsureAny(long userId, string permissionKey, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var allowed = await (from membership in db.AccessMemberships.AsNoTracking()
                             join user in db.Users.AsNoTracking() on membership.UserId equals user.Id
                             join rolePermission in db.AccessRolePermissions.AsNoTracking() on membership.RoleId equals rolePermission.RoleId
                             join permission in db.AccessPermissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                             where membership.UserId == userId && user.IsActive &&
                                   user.StatusKey == IamKeys.UserStatuses.Active && membership.IsActive &&
                                   membership.EndsAtUtc == null && membership.StartsAtUtc <= now &&
                                   rolePermission.EffectKey == IamKeys.Effects.Allow && permission.Key == permissionKey &&
                                   (!membership.SourceUnitPartyRelationId.HasValue || db.UnitPartyRelations.Any(relation =>
                                       relation.Id == membership.SourceUnitPartyRelationId && relation.IsActive &&
                                       (!relation.StartDate.HasValue || relation.StartDate <= now) && relation.EndDate == null))
                             select membership.Id).AnyAsync(ct);
        if (!allowed)
            throw new AppException(403, "authorization.denied", "You are not allowed to access this resource.");
    }

    public async Task EnsureSession(long userId, long sessionId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var active = await db.AuthSessions.AsNoTracking().AnyAsync(x => x.Id == sessionId &&
            x.UserId == userId && x.IsActive && x.RevokedAtUtc == null && x.AbsoluteExpiresAtUtc > now &&
            x.AccessTokenExpiresAtUtc > now && (!x.IdleExpiresAtUtc.HasValue || x.IdleExpiresAtUtc > now), ct);
        if (!active)
            throw new AppException(401, "authentication.invalid_session", "The authenticated session is no longer valid.");
    }

    public async Task EnsureActingSession(long userId, long platformUserId, long actingSessionId,
        CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var active = await db.SupportActingSessions.AsNoTracking().AnyAsync(x =>
            x.Id == actingSessionId && x.TargetUserId == userId && x.PlatformUserId == platformUserId &&
            x.IsActive && x.EndedAtUtc == null && x.ExpiresAtUtc > now, ct);
        if (!active) throw new AppException(401, "authentication.invalid_acting_session",
            "The support acting session is no longer valid.");
    }

    public async Task<AccessibleResourceIds> Accessible(long userId, string permissionKey, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive &&
            x.StatusKey == IamKeys.UserStatuses.Active, ct))
            return new([], [], []);

        var memberships = await db.AccessMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EndsAtUtc == null && x.StartsAtUtc <= now &&
                (!x.SourceUnitPartyRelationId.HasValue || db.UnitPartyRelations.Any(relation =>
                    relation.Id == x.SourceUnitPartyRelationId && relation.IsActive &&
                    (!relation.StartDate.HasValue || relation.StartDate <= now) && relation.EndDate == null)))
            .Select(x => new { x.RoleId, x.ComplexId, x.BuildingId, x.UnitId }).ToListAsync(ct);
        var roleIds = memberships.Select(x => x.RoleId).Distinct().ToArray();
        var baseAllowedRoleIds = await (from rolePermission in db.AccessRolePermissions.AsNoTracking()
                                        join permission in db.AccessPermissions.AsNoTracking()
                                            on rolePermission.PermissionId equals permission.Id
                                        where roleIds.Contains(rolePermission.RoleId) &&
                                              permission.Key == permissionKey &&
                                              rolePermission.EffectKey == IamKeys.Effects.Allow
                                        select rolePermission.RoleId).Distinct().ToListAsync(ct);
        var membershipBuildingIds = memberships.Where(x => x.BuildingId.HasValue)
            .Select(x => x.BuildingId!.Value).Distinct().ToArray();
        var membershipComplexIds = memberships.Where(x => x.ComplexId.HasValue)
            .Select(x => x.ComplexId!.Value).Distinct().ToArray();
        var membershipUnitIds = memberships.Where(x => x.UnitId.HasValue)
            .Select(x => x.UnitId!.Value).Distinct().ToArray();
        var unitParentBuildingIds = await db.Units.AsNoTracking()
            .Where(x => membershipUnitIds.Contains(x.Id)).Select(x => x.BuildingId).Distinct().ToListAsync(ct);
        var buildingIds = await db.Buildings.AsNoTracking().Where(building =>
            membershipBuildingIds.Contains(building.Id) ||
            unitParentBuildingIds.Contains(building.Id) ||
            building.ComplexId.HasValue && membershipComplexIds.Contains(building.ComplexId.Value))
            .Select(x => x.Id).ToListAsync(ct);
        var overrides = IamPermissionPolicy.CanOverrideAtBuilding(permissionKey)
            ? await (from item in db.BuildingRolePermissionOverrides.AsNoTracking()
                     join permission in db.AccessPermissions.AsNoTracking() on item.PermissionId equals permission.Id
                     where buildingIds.Contains(item.BuildingId) && item.IsActive &&
                           roleIds.Contains(item.RoleId) && permission.Key == permissionKey
                     select new { item.BuildingId, item.RoleId, item.EffectKey }).ToListAsync(ct)
            : [];

        bool RoleAllows(long roleId, long? buildingId)
        {
            var roleOverride = buildingId.HasValue
                ? overrides.SingleOrDefault(x => x.BuildingId == buildingId && x.RoleId == roleId)
                : null;
            return roleOverride?.EffectKey == IamKeys.Effects.Allow ||
                   roleOverride is null && baseAllowedRoleIds.Contains(roleId);
        }

        var complexIds = memberships.Where(x => x.ComplexId.HasValue && RoleAllows(x.RoleId, null))
            .Select(x => x.ComplexId!.Value).Distinct().ToList();
        // Complex coverage is expanded once through a focused query; list consumers use these explicit IDs.
        var buildingParents = await db.Buildings.AsNoTracking().Where(x => buildingIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ComplexId }).ToListAsync(ct);
        var effectiveBuildingIds = buildingParents.Where(building => memberships.Any(membership =>
            (membership.BuildingId == building.Id || membership.ComplexId == building.ComplexId) &&
            RoleAllows(membership.RoleId, building.Id))).Select(x => x.Id).Distinct().ToList();
        var units = await db.Units.AsNoTracking().Where(unit =>
            effectiveBuildingIds.Contains(unit.BuildingId) || membershipUnitIds.Contains(unit.Id))
            .Select(x => new { x.Id, x.BuildingId }).ToListAsync(ct);
        var unitIds = units.Where(unit => memberships.Any(membership =>
            (membership.UnitId == unit.Id || membership.BuildingId == unit.BuildingId ||
             buildingParents.Any(building => building.Id == unit.BuildingId && membership.ComplexId == building.ComplexId)) &&
            RoleAllows(membership.RoleId, unit.BuildingId))).Select(x => x.Id).Distinct().ToList();

        if (IamPermissionPolicy.CanGrantToIndividual(permissionKey))
        {
            var grants = await (from grant in db.AccessGrants.AsNoTracking()
                                join permission in db.AccessPermissions.AsNoTracking()
                                    on grant.PermissionId equals permission.Id
                                where grant.UserId == userId && grant.IsActive && grant.RevokedAtUtc == null &&
                                      (!grant.ExpiresAtUtc.HasValue || grant.ExpiresAtUtc > now) &&
                                      permission.Key == permissionKey
                                select new { grant.ComplexId, grant.BuildingId, grant.UnitId }).ToListAsync(ct);
            complexIds.AddRange(grants.Where(x => x.ComplexId.HasValue).Select(x => x.ComplexId!.Value));
            effectiveBuildingIds.AddRange(grants.Where(x => x.BuildingId.HasValue).Select(x => x.BuildingId!.Value));
            unitIds.AddRange(grants.Where(x => x.UnitId.HasValue).Select(x => x.UnitId!.Value));
        }
        return new(complexIds.Distinct().ToArray(), effectiveBuildingIds.Distinct().ToArray(),
            unitIds.Distinct().ToArray());
    }

    public async Task Ensure(long userId, string permissionKey, long? complexId, long? buildingId,
        long? unitId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var userIsActive = await db.Users.AsNoTracking().AnyAsync(x => x.Id == userId && x.IsActive &&
            x.StatusKey == IamKeys.UserStatuses.Active, ct);
        if (!userIsActive)
            throw new AppException(403, "authorization.denied", "You are not allowed to access this resource.");
        var targetBuildingId = buildingId;
        if (unitId.HasValue)
            targetBuildingId = await db.Units.AsNoTracking().Where(x => x.Id == unitId).Select(x => (long?)x.BuildingId).SingleOrDefaultAsync(ct);
        var targetComplexId = complexId;
        if (targetBuildingId.HasValue)
            targetComplexId = await db.Buildings.AsNoTracking().Where(x => x.Id == targetBuildingId).Select(x => x.ComplexId).SingleOrDefaultAsync(ct);

        var memberships = await db.AccessMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EndsAtUtc == null && x.StartsAtUtc <= now &&
                (!x.SourceUnitPartyRelationId.HasValue || db.UnitPartyRelations.Any(relation =>
                    relation.Id == x.SourceUnitPartyRelationId && relation.IsActive &&
                    (!relation.StartDate.HasValue || relation.StartDate <= now) && relation.EndDate == null)) &&
                ((unitId.HasValue && x.UnitId == unitId) ||
                 (targetBuildingId.HasValue && x.BuildingId == targetBuildingId) ||
                 (targetComplexId.HasValue && x.ComplexId == targetComplexId)))
            .Select(x => new { x.RoleId })
            .ToListAsync(ct);
        var roleIds = memberships.Select(x => x.RoleId).Distinct().ToArray();
        var baseAllows = await (from rolePermission in db.AccessRolePermissions.AsNoTracking()
                                join permission in db.AccessPermissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                                where roleIds.Contains(rolePermission.RoleId) && permission.Key == permissionKey
                                select new { rolePermission.RoleId, rolePermission.EffectKey }).ToListAsync(ct);
        var overrides = targetBuildingId.HasValue && IamPermissionPolicy.CanOverrideAtBuilding(permissionKey)
            ? await (from item in db.BuildingRolePermissionOverrides.AsNoTracking()
                     join permission in db.AccessPermissions.AsNoTracking() on item.PermissionId equals permission.Id
                     where item.BuildingId == targetBuildingId && item.IsActive && roleIds.Contains(item.RoleId) && permission.Key == permissionKey
                     select new { item.RoleId, item.EffectKey }).ToListAsync(ct)
            : [];
        var roleAllowed = roleIds.Any(roleId =>
        {
            var roleOverride = overrides.SingleOrDefault(x => x.RoleId == roleId);
            return roleOverride?.EffectKey == IamKeys.Effects.Allow ||
                   (roleOverride is null && baseAllows.Any(x => x.RoleId == roleId && x.EffectKey == IamKeys.Effects.Allow));
        });
        var grantAllowed = IamPermissionPolicy.CanGrantToIndividual(permissionKey) &&
            await (from grant in db.AccessGrants.AsNoTracking()
                   join permission in db.AccessPermissions.AsNoTracking() on grant.PermissionId equals permission.Id
                   where grant.UserId == userId && grant.IsActive && grant.RevokedAtUtc == null &&
                         (!grant.ExpiresAtUtc.HasValue || grant.ExpiresAtUtc > now) && permission.Key == permissionKey &&
                         ((complexId != null && grant.ComplexId == complexId) || (buildingId != null && grant.BuildingId == buildingId) || (unitId != null && grant.UnitId == unitId))
                   select grant.Id).AnyAsync(ct);
        if (!roleAllowed && !grantAllowed) throw new AppException(403, "authorization.denied", "You are not allowed to access this resource.");
    }
}
