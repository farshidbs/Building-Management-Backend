using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class PlatformSupportService(IApplicationDbContext db, TimeProvider clock,
    IIamSecretProtector protector, IPlatformPasswordHasher passwordHasher, ICurrentActor actor,
    IamOptions options)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<PlatformTokenResponse> Login(PlatformLoginRequest request, CancellationToken ct)
    {
        var username = request.Username?.Trim().ToUpperInvariant() ?? "";
        var password = request.Password ?? "";
        var userId = await db.PlatformUsers.AsNoTracking().Where(x => x.NormalizedUsername == username)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct);
        if (!userId.HasValue) throw AuthenticationFailed();
        var result = await db.ExecuteInTransaction<PlatformTokenResponse?>(async token =>
        {
            await db.LockPlatformUserForSecurityMutation(userId.Value, token);
            var user = await db.PlatformUsers.SingleAsync(x => x.Id == userId.Value, token);
            var locked = user.LockedUntilUtc.HasValue && user.LockedUntilUtc > Now;
            var valid = !string.IsNullOrWhiteSpace(password) && user.IsActive &&
                user.StatusKey == "active" && !locked && passwordHasher.Verify(user, user.PasswordHash, password);
            if (!valid)
            {
                if (user.IsActive && user.StatusKey == "active" && !locked)
                {
                    user.RecordFailedLogin(options.PlatformMaxFailedAttempts,
                        options.PlatformLockoutMinutes, Now);
                    await db.SaveChangesAsync(token);
                }
                return null;
            }
            user.RecordSuccessfulLogin(Now);
            var access = protector.CreateToken(); var refresh = protector.CreateToken();
            var accessExpiry = Now.AddMinutes(options.AccessTokenMinutes);
            var session = new AuthSession(await UniqueCode(db.AuthSessions, token), null, user.Id, null, "web",
                protector.Hash(access), accessExpiry, Now, Now.AddHours(options.WebSessionHours),
                Now.AddHours(2), request.DeviceIdentifier, null);
            db.AuthSessions.Add(session); await db.SaveChangesAsync(token);
            db.AuthRefreshTokens.Add(new AuthRefreshToken(session.Id, protector.Hash(refresh), Now,
                Now.AddDays(options.RefreshTokenDays)));
            await db.SaveChangesAsync(token);
            return new PlatformTokenResponse(access, refresh, accessExpiry, user.Code);
        }, ct);
        return result ?? throw AuthenticationFailed();
    }

    public async Task<PlatformTokenResponse> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = protector.Hash(Required(request.RefreshToken, "refreshToken"));
        var sessionId = await db.AuthRefreshTokens.AsNoTracking().Where(x => x.TokenHash == hash)
            .Select(x => (long?)x.AuthSessionId).SingleOrDefaultAsync(ct)
            ?? throw AuthenticationFailed();
        var result = await db.ExecuteInTransaction<PlatformTokenResponse?>(async token =>
        {
            await db.LockSessionForSecurityMutation(sessionId, token);
            var old = await db.AuthRefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, token)
                ?? throw AuthenticationFailed();
            var session = await db.AuthSessions.SingleAsync(x => x.Id == sessionId, token);
            if (!session.PlatformUserId.HasValue || session.UserId.HasValue ||
                !session.IsUsable(Now) || old.ExpiresAtUtc <= Now)
                throw AuthenticationFailed();
            if (old.ConsumedAtUtc.HasValue || old.RevokedAtUtc.HasValue)
            {
                session.Revoke("platform_refresh_reuse", Now);
                var active = await db.AuthRefreshTokens.Where(x => x.AuthSessionId == session.Id &&
                    x.RevokedAtUtc == null && x.ConsumedAtUtc == null).ToListAsync(token);
                foreach (var item in active) item.Revoke(Now);
                await db.SaveChangesAsync(token);
                return null;
            }
            var platform = await db.PlatformUsers.SingleOrDefaultAsync(x => x.Id == session.PlatformUserId &&
                x.IsActive && x.StatusKey == "active", token) ?? throw AuthenticationFailed();
            old.Consume(Now);
            var access = protector.CreateToken(); var refresh = protector.CreateToken();
            var accessExpiry = Now.AddMinutes(options.AccessTokenMinutes);
            session.RotateAccessToken(protector.Hash(access), accessExpiry, Now);
            var replacement = new AuthRefreshToken(session.Id, protector.Hash(refresh), Now,
                Now.AddDays(options.RefreshTokenDays));
            db.AuthRefreshTokens.Add(replacement); await db.SaveChangesAsync(token);
            old.ReplaceWith(replacement.Id); await db.SaveChangesAsync(token);
            return new(access, refresh, accessExpiry, platform.Code);
        }, ct);
        return result ?? throw new AppException(401, "platform_refresh.reused",
            "Refresh token reuse was detected.");
    }

    public async Task<List<RecoveryReviewResponse>> RecoveryCases(CancellationToken ct)
    {
        await EnsurePermission("recovery_case_view", ct);
        return await db.AccountRecoveryCases.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => Review(x)).ToListAsync(ct);
    }

    public async Task<RecoveryReviewResponse> RecoveryCase(string code, CancellationToken ct)
    {
        await EnsurePermission("recovery_case_view", ct); code = PublicCode.Normalize(code);
        var item = await db.AccountRecoveryCases.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code, ct)
            ?? throw AppException.NotFound("recovery");
        return Review(item);
    }

    public Task DecideRecovery(string code, RecoveryDecisionRequest request, bool approve,
        CancellationToken ct) => db.ExecuteInTransaction(async token =>
    {
        await EnsurePermission("recovery_case_review", token);
        code = PublicCode.Normalize(code);
        var state = await db.AccountRecoveryCases.AsNoTracking().Where(x => x.Code == code)
            .Select(x => new { x.ReferenceHash }).SingleOrDefaultAsync(token)
            ?? throw AppException.NotFound("recovery");
        await db.LockRecoveryCase(state.ReferenceHash, token);
        var recovery = await db.AccountRecoveryCases.SingleAsync(x => x.Code == code, token);
        var platformId = PlatformUserId(); var reason = Required(request.Reason, "reason");
        if (approve)
        {
            if (!recovery.UserId.HasValue)
                throw AppException.Conflict("recovery.candidate_unresolved", "Recovery candidate is unresolved.");
            recovery.Approve(platformId, reason, Now);
        }
        else recovery.Reject(platformId, reason, Now);
        db.SecurityAuditEvents.Add(new SecurityAuditEvent(recovery.UserId, platformId, actor.SessionId,
            approve ? "recovery_approved" : "recovery_rejected", "account_recovery", recovery.Code,
            AuditReason(reason, request.TicketReference), null, Now));
        await db.SaveChangesAsync(token); return recovery.Id;
    }, ct);

    public async Task<ActingSessionTokenResponse> StartActing(StartActingSessionRequest request,
        CancellationToken ct)
    {
        await EnsurePermission("support_act", ct);
        var targetCode = PublicCode.Normalize(request.TargetUserCode);
        var target = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Code == targetCode &&
            x.IsActive && x.StatusKey == IamKeys.UserStatuses.Active, ct) ?? throw AppException.NotFound("user");
        var reason = Required(request.Reason, "reason");
        var minutes = Math.Clamp(request.RequestedMinutes, 1, 60);
        var plain = protector.CreateToken(); var platformId = PlatformUserId();
        var acting = new SupportActingSession(await UniqueCode(db.SupportActingSessions, ct), platformId,
            target.Id, actor.SessionId ?? throw AuthenticationFailed(), protector.Hash(plain), reason,
            request.TicketReference, Now.AddMinutes(minutes), Now);
        db.SupportActingSessions.Add(acting);
        db.SecurityAuditEvents.Add(new SecurityAuditEvent(target.Id, platformId, actor.SessionId,
            "acting_started", "support_acting", acting.Code,
            AuditReason(reason, request.TicketReference), null, Now));
        await db.SaveChangesAsync(ct);
        return new(acting.Code, plain, acting.ExpiresAtUtc, target.Code);
    }

    public Task RevokeActing(string code, CancellationToken ct) => db.ExecuteInTransaction(async token =>
    {
        await EnsurePermission("support_act", token); code = PublicCode.Normalize(code);
        var id = await db.SupportActingSessions.AsNoTracking().Where(x => x.Code == code)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(token)
            ?? throw AppException.NotFound("support_acting_session");
        await db.LockSupportActingSession(id, token);
        var acting = await db.SupportActingSessions.SingleAsync(x => x.Id == id, token);
        acting.End("platform_revoked", Now);
        db.SecurityAuditEvents.Add(new SecurityAuditEvent(acting.TargetUserId, PlatformUserId(),
            actor.SessionId, "acting_revoked", "support_acting", acting.Code, acting.Reason, null, Now));
        await db.SaveChangesAsync(token); return acting.Id;
    }, ct);

    public async Task<List<ActingSessionResponse>> ActingSessions(CancellationToken ct)
    {
        await EnsurePermission("support_act_view", ct);
        await ExpireActingSessions(ct);
        return await (from item in db.SupportActingSessions.AsNoTracking()
                      join user in db.Users.AsNoTracking() on item.TargetUserId equals user.Id
                      orderby item.CreatedAtUtc descending
                      select new ActingSessionResponse(item.Code, user.Code, item.Reason,
                          item.TicketReference, item.ExpiresAtUtc, item.EndedAtUtc,
                          item.IsActive && item.EndedAtUtc == null && item.ExpiresAtUtc > Now
                              ? "active" : item.EndReasonKey ?? "expired")).ToListAsync(ct);
    }

    private async Task ExpireActingSessions(CancellationToken ct)
    {
        var ids = await db.SupportActingSessions.AsNoTracking().Where(x => x.IsActive &&
            x.EndedAtUtc == null && x.ExpiresAtUtc <= Now).Select(x => x.Id).OrderBy(x => x).ToListAsync(ct);
        foreach (var id in ids)
        {
            await db.ExecuteInTransaction(async token =>
            {
                await db.LockSupportActingSession(id, token);
                var item = await db.SupportActingSessions.SingleAsync(x => x.Id == id, token);
                if (item.IsActive && item.EndedAtUtc == null && item.ExpiresAtUtc <= Now)
                {
                    item.End("expired", Now);
                    db.SecurityAuditEvents.Add(new SecurityAuditEvent(item.TargetUserId,
                        item.PlatformUserId, item.PlatformAuthSessionId, "acting_expired",
                        "support_acting", item.Code, item.Reason, null, Now));
                    await db.SaveChangesAsync(token);
                }
                return item.Id;
            }, ct);
        }
    }

    public async Task EnsurePermission(string permission, CancellationToken ct)
    {
        var platformId = PlatformUserId();
        var allowed = await (from assignment in db.PlatformUserRoles.AsNoTracking()
                             join role in db.PlatformRoles.AsNoTracking() on assignment.RoleId equals role.Id
                             join link in db.PlatformRolePermissions.AsNoTracking() on role.Id equals link.RoleId
                             join capability in db.PlatformPermissions.AsNoTracking() on link.PermissionId equals capability.Id
                             where assignment.PlatformUserId == platformId && role.IsActive && capability.IsActive &&
                                   capability.Key == permission
                             select link.Id).AnyAsync(ct);
        if (!allowed) throw new AppException(403, "platform_authorization.denied", "Platform permission is required.");
    }

    private long PlatformUserId() => actor.ActorType == "platform" && actor.PlatformUserId.HasValue
        ? actor.PlatformUserId.Value : throw AuthenticationFailed();
    private static RecoveryReviewResponse Review(AccountRecoveryCase x) => new(x.Code, x.StatusKey,
        x.OldNormalizedIdentifier, x.NewNormalizedIdentifier, x.IdentityNumberEvidence,
        x.BirthDateEvidence, x.ExpiresAtUtc, x.MobileVerifiedAtUtc, x.Reason);
    private static string AuditReason(string reason, string? ticket) => string.IsNullOrWhiteSpace(ticket)
        ? reason : $"{reason} | ticket:{ticket.Trim()}";
    private static AppException AuthenticationFailed() => new(401, "platform_authentication.failed",
        "Authentication failed.");
    private static string Required(string? value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new AppException(400, "validation.failed", "Request validation failed.",
            new Dictionary<string, string[]> { [field] = [$"{field} is required."] }) : value.Trim();
    private static async Task<string> UniqueCode<TEntity>(DbSet<TEntity> set, CancellationToken ct)
        where TEntity : Entity
    { for (var i = 0; i < 20; i++) { var value = PublicCode.Create(); if (!await set.AnyAsync(x => x.Code == value, ct)) return value; } throw new InvalidOperationException("Could not allocate a public code."); }
}
