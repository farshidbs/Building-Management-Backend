using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed record AccessContextResponse(string ScopeKind, string ScopeCode, string ScopeName,
    IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions);

public sealed class AccessAuthorizationService(IApplicationDbContext db, TimeProvider clock)
{
    public async Task<IReadOnlyList<AccessContextResponse>> GetContexts(long userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var memberships = await db.AccessMemberships.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive && x.EndsAtUtc == null && x.StartsAtUtc <= now)
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

    public async Task Ensure(long userId, string permissionKey, long? complexId, long? buildingId,
        long? unitId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var roleAllowed = await (from membership in db.AccessMemberships.AsNoTracking()
                                 join rolePermission in db.AccessRolePermissions.AsNoTracking() on membership.RoleId equals rolePermission.RoleId
                                 join permission in db.AccessPermissions.AsNoTracking() on rolePermission.PermissionId equals permission.Id
                                 where membership.UserId == userId && membership.IsActive && membership.EndsAtUtc == null && membership.StartsAtUtc <= now &&
                                       permission.Key == permissionKey && rolePermission.EffectKey == IamKeys.Effects.Allow &&
                                       ((complexId != null && membership.ComplexId == complexId) || (buildingId != null && membership.BuildingId == buildingId) || (unitId != null && membership.UnitId == unitId))
                                 select membership.Id).AnyAsync(ct);
        var grantAllowed = await (from grant in db.AccessGrants.AsNoTracking()
                                  join permission in db.AccessPermissions.AsNoTracking() on grant.PermissionId equals permission.Id
                                  where grant.UserId == userId && grant.IsActive && grant.RevokedAtUtc == null &&
                                        (!grant.ExpiresAtUtc.HasValue || grant.ExpiresAtUtc > now) && permission.Key == permissionKey &&
                                        ((complexId != null && grant.ComplexId == complexId) || (buildingId != null && grant.BuildingId == buildingId) || (unitId != null && grant.UnitId == unitId))
                                  select grant.Id).AnyAsync(ct);
        if (!roleAllowed && !grantAllowed) throw new AppException(403, "authorization.denied", "You are not allowed to access this resource.");
    }
}
