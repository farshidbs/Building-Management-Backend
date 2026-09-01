using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class AccessWorkflowService(IApplicationDbContext db, TimeProvider clock,
    ResourceAuthorization authorization, ICurrentActor actor)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<MembershipExitResponse> RequestExit(string membershipCode,
        MembershipExitRequestDto request, CancellationToken ct)
    {
        var userId = authorization.UserId;
        membershipCode = PublicCode.Normalize(membershipCode);
        var membership = await db.AccessMemberships.SingleOrDefaultAsync(x => x.Code == membershipCode &&
            x.UserId == userId && x.IsActive && x.EndsAtUtc == null && x.StatusKey == "active" &&
            x.StartsAtUtc <= Now && (!x.SourceUnitPartyRelationId.HasValue ||
                db.UnitPartyRelations.Any(relation => relation.Id == x.SourceUnitPartyRelationId &&
                    relation.IsActive && (!relation.StartDate.HasValue || relation.StartDate <= Now) &&
                    relation.EndDate == null)), ct)
            ?? throw AppException.NotFound("membership");
        if (await db.MembershipExitRequests.AnyAsync(x => x.MembershipId == membership.Id &&
                x.IsActive && x.StatusKey == "pending", ct))
            throw AppException.Conflict("membership_exit.already_pending", "A pending exit request already exists.");
        var entity = new MembershipExitRequest(await Unique(db.MembershipExitRequests, ct),
            membership.Id, userId, request.Reason, Now);
        db.MembershipExitRequests.Add(entity);
        Audit("membership_exit_requested", userId, "membership", membership.Code, request.Reason);
        try { await db.SaveChangesAsync(ct); }
        catch (Exception exception) when (db.IsUniqueViolation(exception))
        { throw AppException.Conflict("membership_exit.already_pending", "A pending exit request already exists."); }
        return await ExitResponse(entity.Id, ct);
    }

    public async Task CancelExit(string code, CancellationToken ct)
    {
        var userId = authorization.UserId;
        code = PublicCode.Normalize(code);
        var id = await db.MembershipExitRequests.AsNoTracking().Where(x => x.Code == code &&
                x.RequestedByUserId == userId)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("membership_exit_request");
        await db.ExecuteInTransaction(async token =>
        {
            await db.LockMembershipExitRequest(id, token);
            var entity = await db.MembershipExitRequests.SingleAsync(x => x.Id == id, token);
            try { entity.Cancel(userId, Now); }
            catch (DomainValidationException exception) { throw Validation(exception); }
            Audit("membership_exit_cancelled", userId, "membership_exit_request", entity.Code, null);
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    public async Task<IReadOnlyList<MembershipExitResponse>> ListExitRequests(string scopeKind,
        string scopeCode, CancellationToken ct)
    {
        var scope = await ResolveScope(scopeKind, scopeCode, ct);
        await authorization.Ensure("membership_manage_scoped", scope.ComplexId, scope.BuildingId, scope.UnitId, ct);
        return await (from exit in db.MembershipExitRequests.AsNoTracking()
                      join membership in db.AccessMemberships.AsNoTracking() on exit.MembershipId equals membership.Id
                      join user in db.Users.AsNoTracking() on exit.RequestedByUserId equals user.Id
                      join role in db.AccessRoles.AsNoTracking() on membership.RoleId equals role.Id
                      where membership.ComplexId == scope.ComplexId && membership.BuildingId == scope.BuildingId &&
                            membership.UnitId == scope.UnitId
                      orderby exit.CreatedAtUtc descending
                      select new MembershipExitResponse(exit.Code, membership.Code, user.Code, role.Key,
                          scope.Kind, scope.Code, exit.StatusKey, exit.Reason, exit.DecisionReason,
                          exit.CreatedAtUtc, exit.DecidedAtUtc)).ToListAsync(ct);
    }

    public Task DecideExit(string code, MembershipExitDecisionRequest request, bool approve,
        CancellationToken ct) => DecideExitCore(code, request, approve, ct);

    private async Task DecideExitCore(string code, MembershipExitDecisionRequest request, bool approve,
        CancellationToken ct)
    {
        code = PublicCode.Normalize(code);
        var item = await (from exit in db.MembershipExitRequests.AsNoTracking()
                          join membership in db.AccessMemberships.AsNoTracking() on exit.MembershipId equals membership.Id
                          where exit.Code == code
                          select new { ExitId = exit.Id, exit.RequestedByUserId, Membership = membership })
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("membership_exit_request");
        await authorization.Ensure("membership_manage_scoped", item.Membership.ComplexId,
            item.Membership.BuildingId, item.Membership.UnitId, ct);
        var managerId = authorization.UserId;
        if (managerId == item.RequestedByUserId)
            throw new AppException(403, "membership_exit.self_decision_denied", "The requester cannot decide their own exit request.");
        await db.ExecuteInTransaction(async token =>
        {
            await db.LockMembershipExitRequest(item.ExitId, token);
            await db.LockMembershipForSecurityMutation(item.Membership.Id, token);
            var exit = await db.MembershipExitRequests.SingleAsync(x => x.Id == item.ExitId, token);
            var membership = await db.AccessMemberships.SingleAsync(x => x.Id == item.Membership.Id, token);
            try { exit.Decide(approve, managerId, request.Reason, Now); }
            catch (DomainValidationException exception) { throw Validation(exception); }
            if (approve)
            {
                if (!membership.IsActive || membership.EndsAtUtc.HasValue)
                    throw AppException.Conflict("membership.already_ended", "The membership has already ended.");
                membership.End(Now);
            }
            Audit(approve ? "membership_exit_approved" : "membership_exit_rejected", managerId,
                "membership_exit_request", exit.Code, request.Reason);
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    public async Task<AccessGrantResponse> CreateGrant(CreateAccessGrantRequest request, CancellationToken ct)
    {
        var scope = await ResolveScope(request.ScopeKind, request.ScopeCode, ct);
        await authorization.Ensure("access_grant_manage", scope.ComplexId, scope.BuildingId, scope.UnitId, ct);
        var permissionKey = Required(request.PermissionKey, "permissionKey").ToLowerInvariant();
        IamPermissionPolicy.RequireGrantable(permissionKey);
        var userCode = PublicCode.Normalize(request.UserCode);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Code == userCode && x.IsActive &&
            x.StatusKey == IamKeys.UserStatuses.Active, ct) ?? throw AppException.NotFound("user");
        var permission = await db.AccessPermissions.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Key == permissionKey && x.IsActive, ct) ?? throw AppException.NotFound("permission");
        var now = Now;
        if (request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc <= now)
            throw FieldValidation("expiresAtUtc", "Must be in the future.");
        if (request.StartsAtUtc.HasValue && request.ExpiresAtUtc.HasValue && request.ExpiresAtUtc <= request.StartsAtUtc)
            throw FieldValidation("expiresAtUtc", "Must be later than startsAtUtc.");
        return await db.ExecuteInTransaction(async token =>
        {
            await db.LockUserForSecurityMutation(user.Id, token);
            if (!await db.Users.AnyAsync(x => x.Id == user.Id && x.IsActive &&
                    x.StatusKey == IamKeys.UserStatuses.Active, token)) throw AppException.NotFound("user");
            var existing = await db.AccessGrants.AsNoTracking().Where(x => x.UserId == user.Id &&
                x.PermissionId == permission.Id && x.ComplexId == scope.ComplexId &&
                x.BuildingId == scope.BuildingId && x.UnitId == scope.UnitId &&
                x.RevokedAtUtc == null).ToListAsync(token);
            var requestedStart = request.StartsAtUtc ?? now;
            if (existing.Any(x => WindowsOverlap(x.StartsAtUtc ?? x.CreatedAtUtc, x.ExpiresAtUtc,
                    requestedStart, request.ExpiresAtUtc)))
                throw AppException.Conflict("access_grant.duplicate", "An equivalent grant overlaps this time window.");
            var grant = new AccessGrant(await Unique(db.AccessGrants, token), user.Id, authorization.UserId,
                permission.Id, scope.ComplexId, scope.BuildingId, scope.UnitId, request.StartsAtUtc,
                request.ExpiresAtUtc, request.Reason, now);
            db.AccessGrants.Add(grant);
            Audit("access_grant_created", authorization.UserId, "access_grant", grant.Code, request.Reason);
            await db.SaveChangesAsync(token);
            return new AccessGrantResponse(grant.Code, user.Code, permission.Key, scope.Kind, scope.Code,
                grant.StartsAtUtc, grant.ExpiresAtUtc, grant.RevokedAtUtc, grant.IsActive, grant.Reason);
        }, ct);
    }

    private static bool WindowsOverlap(DateTimeOffset firstStart, DateTimeOffset? firstEnd,
        DateTimeOffset secondStart, DateTimeOffset? secondEnd) =>
        (!firstEnd.HasValue || secondStart < firstEnd.Value) &&
        (!secondEnd.HasValue || firstStart < secondEnd.Value);

    public async Task<IReadOnlyList<AccessGrantResponse>> ListGrants(string scopeKind, string scopeCode,
        CancellationToken ct)
    {
        var scope = await ResolveScope(scopeKind, scopeCode, ct);
        await authorization.Ensure("access_grant_view", scope.ComplexId, scope.BuildingId, scope.UnitId, ct);
        return await (from grant in db.AccessGrants.AsNoTracking()
                      join user in db.Users.AsNoTracking() on grant.UserId equals user.Id
                      join permission in db.AccessPermissions.AsNoTracking() on grant.PermissionId equals permission.Id
                      where grant.ComplexId == scope.ComplexId && grant.BuildingId == scope.BuildingId &&
                            grant.UnitId == scope.UnitId
                      orderby grant.CreatedAtUtc descending
                      select new AccessGrantResponse(grant.Code, user.Code, permission.Key, scope.Kind,
                          scope.Code, grant.StartsAtUtc, grant.ExpiresAtUtc, grant.RevokedAtUtc,
                          grant.IsActive, grant.Reason)).ToListAsync(ct);
    }

    public async Task RevokeGrant(string code, CancellationToken ct)
    {
        code = PublicCode.Normalize(code);
        var item = await db.AccessGrants.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code, ct)
            ?? throw AppException.NotFound("access_grant");
        await authorization.Ensure("access_grant_manage", item.ComplexId, item.BuildingId, item.UnitId, ct);
        await db.ExecuteInTransaction(async token =>
        {
            await db.LockAccessGrant(item.Id, token);
            var grant = await db.AccessGrants.SingleAsync(x => x.Id == item.Id, token);
            if (!grant.IsActive || grant.RevokedAtUtc.HasValue)
                throw AppException.Conflict("access_grant.already_revoked", "The access grant is already revoked.");
            grant.Revoke(Now);
            Audit("access_grant_revoked", authorization.UserId, "access_grant", grant.Code, null);
            await db.SaveChangesAsync(token);
            return true;
        }, ct);
    }

    private async Task<Scope> ResolveScope(string kind, string code, CancellationToken ct)
    {
        kind = Required(kind, "scopeKind").ToLowerInvariant();
        code = PublicCode.Normalize(code);
        if (kind == IamKeys.Scopes.Complex)
        {
            var id = await db.Complexes.AsNoTracking().Where(x => x.Code == code && x.IsActive)
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
            return new(kind, code, id, null, null);
        }
        if (kind == IamKeys.Scopes.Building)
        {
            var value = await db.Buildings.AsNoTracking().Where(x => x.Code == code && x.IsActive)
                .Select(x => new { x.Id, x.ComplexId }).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
            return new(kind, code, null, value.Id, null);
        }
        if (kind == IamKeys.Scopes.Unit)
        {
            var value = await (from unit in db.Units.AsNoTracking()
                               join building in db.Buildings.AsNoTracking() on unit.BuildingId equals building.Id
                               where unit.Code == code && unit.IsActive
                               select new { UnitId = unit.Id, BuildingId = building.Id, building.ComplexId })
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("unit");
            return new(kind, code, null, null, value.UnitId);
        }
        throw FieldValidation("scopeKind", "Must be complex, building, or unit.");
    }

    private async Task<MembershipExitResponse> ExitResponse(long id, CancellationToken ct)
    {
        var row = await (from exit in db.MembershipExitRequests.AsNoTracking()
                         join membership in db.AccessMemberships.AsNoTracking() on exit.MembershipId equals membership.Id
                         join user in db.Users.AsNoTracking() on exit.RequestedByUserId equals user.Id
                         join role in db.AccessRoles.AsNoTracking() on membership.RoleId equals role.Id
                         where exit.Id == id
                         select new { Exit = exit, Membership = membership, UserCode = user.Code, RoleKey = role.Key })
            .SingleAsync(ct);
        var kind = row.Membership.ComplexId.HasValue ? "complex" : row.Membership.BuildingId.HasValue ? "building" : "unit";
        var scopeCode = row.Membership.ComplexId.HasValue
            ? await db.Complexes.Where(x => x.Id == row.Membership.ComplexId).Select(x => x.Code).SingleAsync(ct)
            : row.Membership.BuildingId.HasValue
                ? await db.Buildings.Where(x => x.Id == row.Membership.BuildingId).Select(x => x.Code).SingleAsync(ct)
                : await db.Units.Where(x => x.Id == row.Membership.UnitId).Select(x => x.Code).SingleAsync(ct);
        return new(row.Exit.Code, row.Membership.Code, row.UserCode, row.RoleKey, kind, scopeCode,
            row.Exit.StatusKey, row.Exit.Reason, row.Exit.DecisionReason, row.Exit.CreatedAtUtc, row.Exit.DecidedAtUtc);
    }

    private void Audit(string type, long userId, string resourceKind, string code, string? reason) =>
        db.SecurityAuditEvents.Add(new SecurityAuditEvent(userId, actor.PlatformUserId, actor.SessionId,
            type, resourceKind, code, reason, null, Now));

    private static async Task<string> Unique<T>(IQueryable<T> set, CancellationToken ct) where T : Entity
    { for (var i = 0; i < 20; i++) { var code = PublicCode.Create(); if (!await set.AnyAsync(x => x.Code == code, ct)) return code; } throw new AppException(500, "code.generation_failed", "A unique code could not be generated."); }
    private static string Required(string? value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw FieldValidation(field, "Must not be blank.") : value.Trim();
    private static AppException FieldValidation(string field, string message) => new(400,
        "validation.failed", "The request is invalid.", new Dictionary<string, string[]> { [field] = [message] });
    private static AppException Validation(DomainValidationException exception) =>
        FieldValidation(exception.Field, exception.Message);
    private sealed record Scope(string Kind, string Code, long? ComplexId, long? BuildingId, long? UnitId);
}

public sealed record MembershipExitRequestDto(string? Reason);
public sealed record MembershipExitDecisionRequest(string? Reason);
public sealed record MembershipExitResponse(string Code, string MembershipCode, string UserCode,
    string RoleKey, string ScopeKind, string ScopeCode, string Status, string? Reason,
    string? DecisionReason, DateTimeOffset RequestedAtUtc, DateTimeOffset? DecidedAtUtc);
public sealed record CreateAccessGrantRequest(string UserCode, string PermissionKey, string ScopeKind,
    string ScopeCode, DateTimeOffset? StartsAtUtc, DateTimeOffset? ExpiresAtUtc, string Reason);
public sealed record AccessGrantResponse(string Code, string UserCode, string PermissionKey,
    string ScopeKind, string ScopeCode, DateTimeOffset? StartsAtUtc, DateTimeOffset? ExpiresAtUtc,
    DateTimeOffset? RevokedAtUtc, bool IsActive, string Reason);
