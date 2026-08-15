using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class IamService(IApplicationDbContext db, TimeProvider clock, IIamSecretProtector protector,
    IOtpDelivery otpDelivery, IamOptions options)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<RequestOtpResponse> RequestOtp(RequestOtpRequest request, CancellationToken ct)
    {
        var purpose = Required(request.PurposeKey, "purposeKey").ToLowerInvariant();
        if (purpose is not ("login" or "register"))
            throw Validation("purposeKey", "Public OTP purpose must be login or register.");
        var mobile = IranianMobileNormalizer.Normalize(request.Mobile);
        if (await db.OtpChallenges.AnyAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.PurposeKey == purpose && x.StatusKey == IamKeys.OtpStatuses.Pending &&
            x.SentAtUtc > Now.AddMinutes(-1), ct))
            throw new AppException(429, "otp.cooldown", "Wait before requesting another OTP.");
        var recent = await db.OtpChallenges.CountAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.CreatedAtUtc > Now.AddMinutes(-10), ct);
        if (recent >= 5) throw new AppException(429, "otp.rate_limited", "Too many OTP requests.");
        var code = protector.CreateOtp();
        var challenge = new OtpChallenge(protector.CreateToken(18), IamKeys.LoginTypes.Mobile, mobile, mobile, purpose,
            protector.Hash(code), Now, Now.AddMinutes(options.OtpLifetimeMinutes), options.OtpMaxAttempts);
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        await otpDelivery.Send(mobile, code, ct);
        return new(challenge.PublicReference, challenge.ExpiresAtUtc);
    }

    public async Task<TokenResponse> VerifyOtp(VerifyOtpRequest request, CancellationToken ct)
    {
        var reference = Required(request.ChallengeReference, "challengeReference");
        var code = Required(request.Code, "code");
        if (!await db.TryReserveOtpAttempt(reference, Now, ct))
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        var challenge = await db.OtpChallenges.SingleOrDefaultAsync(x => x.PublicReference == reference, ct)
            ?? throw AppException.NotFound("otp_challenge");
        if (!protector.Verify(code, challenge.CodeHash))
        {
            db.Detach(challenge);
            await db.MarkOtpAttemptFailed(reference, Now, ct);
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        }

        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                challenge.Verify(Now);
                challenge.Consume(Now);
                await db.SaveChangesAsync(token);
                var methodState = await UsableLoginMethods().AsNoTracking()
                    .Where(x => x.LoginTypeKey == challenge.LoginTypeKey &&
                        x.NormalizedIdentifierValue == challenge.NormalizedIdentifierValue)
                    .Select(x => new { x.Id, x.UserId }).SingleOrDefaultAsync(token);
                UserLoginMethod? method = null;
                User user;
                string? partyCode;
                if (methodState is null)
                {
                    if (challenge.PurposeKey == "login") throw new AppException(401, "authentication.failed", "Authentication failed.");
                    user = new User(await UniqueCode(db.Users, token), Now); db.Users.Add(user); await db.SaveChangesAsync(token);
                    method = new UserLoginMethod(await UniqueCode(db.UserLoginMethods, token), user.Id,
                        IamKeys.LoginTypes.Mobile, challenge.IdentifierValue, challenge.NormalizedIdentifierValue, true, Now);
                    method.Verify(Now); db.UserLoginMethods.Add(method);
                    var personType = await db.PartyTypes.SingleAsync(x =>
                        x.Key == PartyReferenceKeys.PartyTypes.IranianPerson, token);
                    var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "کاربر جدید" : request.DisplayName.Trim();
                    var party = new Party(await UniqueCode(db.Parties, token), personType.Id, displayName,
                        null, null, null, null, null, Now); db.Parties.Add(party); await db.SaveChangesAsync(token);
                    db.UserPartyLinks.Add(new UserPartyLink(await UniqueCode(db.UserPartyLinks, token), user.Id, party.Id, true, Now));
                    partyCode = party.Code;
                }
                else
                {
                    await db.LockUserForSecurityMutation(methodState.UserId, token);
                    method = await UsableLoginMethods().SingleOrDefaultAsync(x => x.Id == methodState.Id &&
                        x.UserId == methodState.UserId && x.LoginTypeKey == challenge.LoginTypeKey &&
                        x.NormalizedIdentifierValue == challenge.NormalizedIdentifierValue, token)
                        ?? throw new AppException(401, "authentication.failed", "Authentication failed.");
                    user = await db.Users.SingleAsync(x => x.Id == method.UserId && x.IsActive && x.StatusKey == IamKeys.UserStatuses.Active, token);
                    partyCode = await (from link in db.UserPartyLinks
                                       where link.UserId == user.Id && link.IsActive && link.IsPrimary
                                       join party in db.Parties on link.PartyId equals party.Id
                                       select party.Code).SingleOrDefaultAsync(token);
                }
                var result = await CreateSession(user, method.Id, request.ClientTypeKey, request.DeviceIdentifier, token);
                await db.SaveChangesAsync(token);
                return result with { PartyCode = partyCode };
            }, ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries) db.Detach(entry.Entity);
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        }
    }

    public async Task<TokenResponse> Refresh(RefreshTokenRequest request, CancellationToken ct)
    {
        var hash = protector.Hash(Required(request.RefreshToken, "refreshToken"));
        var state = await db.AuthRefreshTokens.AsNoTracking()
            .Where(x => x.TokenHash == hash)
            .Select(x => new { x.AuthSessionId })
            .SingleOrDefaultAsync(ct)
            ?? throw new AppException(401, "refresh_token.invalid", "Refresh token is invalid.");

        try
        {
            var result = await db.ExecuteInTransaction<TokenResponse?>(async token =>
            {
                await db.LockSessionForSecurityMutation(state.AuthSessionId, token);
                var old = await db.AuthRefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, token)
                    ?? throw new AppException(401, "refresh_token.invalid", "Refresh token is invalid.");
                var session = await db.AuthSessions.SingleAsync(x => x.Id == old.AuthSessionId, token);
                if (old.ConsumedAtUtc.HasValue || old.RevokedAtUtc.HasValue)
                {
                    await RevokeLockedSessions([session], "refresh_reuse", token);
                    await db.SaveChangesAsync(token);
                    return null;
                }
                if (!session.IsUsable(Now) || old.ExpiresAtUtc <= Now)
                    throw new AppException(401, "refresh_token.invalid", "Refresh token is invalid.");
                old.Consume(Now);
                var plainAccess = protector.CreateToken(); var plainRefresh = protector.CreateToken();
                var accessExpiry = Now.AddMinutes(options.AccessTokenMinutes);
                session.RotateAccessToken(protector.Hash(plainAccess), accessExpiry, Now);
                var replacement = new AuthRefreshToken(session.Id, protector.Hash(plainRefresh), Now, Now.AddDays(options.RefreshTokenDays));
                db.AuthRefreshTokens.Add(replacement); await db.SaveChangesAsync(token); old.ReplaceWith(replacement.Id);
                var user = await db.Users.SingleAsync(x => x.Id == session.UserId, token);
                var partyCode = await (from link in db.UserPartyLinks where link.UserId == user.Id && link.IsActive && link.IsPrimary join p in db.Parties on link.PartyId equals p.Id select p.Code).SingleOrDefaultAsync(token);
                // Access tokens are session-bound; refresh rotates the refresh secret while the current access token remains bounded by session expiry.
                await db.SaveChangesAsync(token);
                return new TokenResponse(plainAccess, plainRefresh, accessExpiry, user.Code, partyCode);
            }, ct);
            return result ?? throw new AppException(401, "refresh_token.reused", "Refresh token reuse was detected.");
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries) db.Detach(entry.Entity);
            await RevokeReplaySession(state.AuthSessionId, ct);
            throw new AppException(401, "refresh_token.reused", "Refresh token reuse was detected.");
        }
    }

    public async Task<CurrentUserResponse> Current(long userId, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, ct) ?? throw AppException.NotFound("user");
        var profile = await (from link in db.UserPartyLinks.AsNoTracking() where link.UserId == userId && link.IsActive && link.IsPrimary join p in db.Parties on link.PartyId equals p.Id select new { p.Code, p.DisplayName }).SingleOrDefaultAsync(ct);
        var methods = await db.UserLoginMethods.AsNoTracking().Where(x => x.UserId == userId).OrderByDescending(x => x.IsPrimary).Select(x => new LoginMethodResponse(x.Code, x.LoginTypeKey, x.IdentifierValue, x.IsPrimary, x.IsVerified, x.StatusKey, x.ReleasedAtUtc)).ToListAsync(ct);
        return new(user.Code, user.StatusKey, profile?.Code, profile?.DisplayName, methods);
    }

    public Task<List<LoginMethodResponse>> LoginMethods(long userId, CancellationToken ct) =>
        db.UserLoginMethods.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.IsPrimary)
            .ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new LoginMethodResponse(x.Code, x.LoginTypeKey, x.IdentifierValue,
                x.IsPrimary, x.IsVerified, x.StatusKey, x.ReleasedAtUtc)).ToListAsync(ct);

    public async Task<RequestOtpResponse> RequestLoginMethodOtp(long userId, LoginMethodOtpRequest request,
        CancellationToken ct)
    {
        _ = await ActiveUser(userId, ct);
        var mobile = IranianMobileNormalizer.Normalize(request.Mobile);
        if (await UsableLoginMethods().AnyAsync(x => x.NormalizedIdentifierValue == mobile, ct))
            throw AppException.Conflict("login_method.identifier_unavailable", "The identifier is unavailable.");
        if (await db.OtpChallenges.AnyAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.PurposeKey == "login_method_add" && x.StatusKey == IamKeys.OtpStatuses.Pending &&
            x.SentAtUtc > Now.AddMinutes(-1), ct))
            throw new AppException(429, "otp.cooldown", "Wait before requesting another OTP.");
        if (await db.OtpChallenges.CountAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.CreatedAtUtc > Now.AddMinutes(-10), ct) >= 5)
            throw new AppException(429, "otp.rate_limited", "Too many OTP requests.");
        var code = protector.CreateOtp();
        var challenge = new OtpChallenge(protector.CreateToken(18), IamKeys.LoginTypes.Mobile,
            mobile, mobile, "login_method_add", protector.Hash(code), Now,
            Now.AddMinutes(options.OtpLifetimeMinutes), options.OtpMaxAttempts);
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        await otpDelivery.Send(mobile, code, ct);
        return new(challenge.PublicReference, challenge.ExpiresAtUtc);
    }

    public async Task<LoginMethodResponse> AddLoginMethod(long userId, AddLoginMethodRequest request,
        CancellationToken ct)
    {
        var mobile = IranianMobileNormalizer.Normalize(request.Mobile);
        var reference = Required(request.ChallengeReference, "challengeReference");
        if (!await db.TryReserveOtpAttempt(reference, Now, ct))
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        var proof = await db.OtpChallenges.AsNoTracking().SingleOrDefaultAsync(x =>
            x.PublicReference == reference && x.PurposeKey == "login_method_add" &&
            x.NormalizedIdentifierValue == mobile, ct);
        if (proof is null || !protector.Verify(Required(request.OtpCode, "otpCode"), proof.CodeHash))
        {
            await db.MarkOtpAttemptFailed(reference, Now, ct);
            throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
        }
        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                await db.LockUserForSecurityMutation(userId, token);
                _ = await ActiveUser(userId, token);
                var challenge = await db.OtpChallenges.SingleOrDefaultAsync(x =>
                    x.PublicReference == reference && x.PurposeKey == "login_method_add" &&
                    x.NormalizedIdentifierValue == mobile, token)
                    ?? throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
                if (!protector.Verify(request.OtpCode, challenge.CodeHash))
                    throw new AppException(400, "otp.invalid", "OTP is invalid or expired.");
                if (await UsableLoginMethods().AnyAsync(x => x.NormalizedIdentifierValue == mobile, token))
                    throw AppException.Conflict("login_method.identifier_unavailable", "The identifier is unavailable.");
                var owned = await UsableLoginMethods().Where(x => x.UserId == userId).ToListAsync(token);
                var makePrimary = request.MakePrimary || owned.Count == 0;
                if (makePrimary)
                {
                    foreach (var method in owned.Where(x => x.IsPrimary)) method.SetPrimary(false, Now);
                    if (owned.Any(x => x.IsPrimary == false)) await db.SaveChangesAsync(token);
                }
                var created = new UserLoginMethod(await UniqueCode(db.UserLoginMethods, token), userId,
                    IamKeys.LoginTypes.Mobile, mobile, mobile, makePrimary, Now);
                created.Verify(Now);
                db.UserLoginMethods.Add(created);
                challenge.Verify(Now);
                challenge.Consume(Now);
                await db.SaveChangesAsync(token);
                return LoginMethod(created);
            }, ct);
        }
        catch (DbUpdateException exception) when (db.IsUniqueViolation(exception))
        {
            throw AppException.Conflict("login_method.identifier_unavailable", "The identifier is unavailable.");
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries) db.Detach(entry.Entity);
            throw AppException.Conflict("login_method.concurrent_change", "The LoginMethod changed concurrently.");
        }
    }

    public Task SetPrimaryLoginMethod(long userId, string code, CancellationToken ct) =>
        db.ExecuteInTransaction(async token =>
        {
            await db.LockUserForSecurityMutation(userId, token);
            code = PublicCode.Normalize(code);
            var target = await UsableLoginMethods().SingleOrDefaultAsync(x =>
                x.UserId == userId && x.Code == code, token) ?? throw AppException.NotFound("login_method");
            var methods = await UsableLoginMethods().Where(x => x.UserId == userId).ToListAsync(token);
            foreach (var method in methods.Where(x => x.Id != target.Id && x.IsPrimary))
                method.SetPrimary(false, Now);
            await db.SaveChangesAsync(token);
            target.SetPrimary(true, Now);
            await db.SaveChangesAsync(token);
            return target.Id;
        }, ct);

    public async Task ReleaseLoginMethod(long userId, string code, ReleaseLoginMethodRequest request,
        CancellationToken ct)
    {
        var reasons = new[] { "user_requested", "number_changed", "lost_access" };
        var reason = Required(request.ReasonKey, "reasonKey").ToLowerInvariant();
        if (!reasons.Contains(reason, StringComparer.Ordinal))
            throw Validation("reasonKey", "Release reason is not allowed.");
        await db.ExecuteInTransaction(async token =>
        {
            await db.LockUserForSecurityMutation(userId, token);
            var targetCode = PublicCode.Normalize(code);
            var target = await UsableLoginMethods().SingleOrDefaultAsync(x =>
                x.UserId == userId && x.Code == targetCode, token) ?? throw AppException.NotFound("login_method");
            var usable = await UsableLoginMethods().Where(x => x.UserId == userId).ToListAsync(token);
            if (usable.Count <= 1)
                throw AppException.Conflict("login_method.last_usable", "The last usable LoginMethod cannot be released.");
            if (target.IsPrimary)
            {
                if (string.IsNullOrWhiteSpace(request.ReplacementPrimaryLoginMethodCode))
                    throw Validation("replacementPrimaryLoginMethodCode", "A replacement primary LoginMethod is required.");
                var replacementCode = PublicCode.Normalize(request.ReplacementPrimaryLoginMethodCode);
                var replacement = usable.SingleOrDefault(x => x.Code == replacementCode && x.Id != target.Id)
                    ?? throw AppException.NotFound("replacement_login_method");
                target.Release(reason, Now);
                await db.SaveChangesAsync(token);
                replacement.SetPrimary(true, Now);
            }
            else target.Release(reason, Now);
            await RevokeSessions(usableSessionQuery: db.AuthSessions.Where(x => x.IsActive &&
                x.AuthenticatedViaLoginMethodId == target.Id), "login_method_released", token);
            await db.SaveChangesAsync(token);
            return target.Id;
        }, ct);
    }

    public Task<List<SessionResponse>> Sessions(long userId, long currentSessionId, CancellationToken ct) =>
        db.AuthSessions.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsActive).ThenByDescending(x => x.CreatedAtUtc)
            .Select(x => new SessionResponse(x.Code, x.ClientTypeKey, x.AbsoluteExpiresAtUtc,
                x.LastSeenAtUtc, x.Id == currentSessionId, x.DeviceIdentifier)).ToListAsync(ct);

    public async Task RevokeSession(long userId, string code, CancellationToken ct)
    {
        code = PublicCode.Normalize(code);
        var sessionId = await db.AuthSessions.AsNoTracking().Where(x => x.UserId == userId && x.Code == code)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("session");
        await db.ExecuteInTransaction(async token =>
        {
            await db.LockSessionForSecurityMutation(sessionId, token);
            var session = await db.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId &&
                x.UserId == userId && x.Code == code, token) ?? throw AppException.NotFound("session");
            await RevokeLockedSessions([session], "user_revoked_session", token);
            await db.SaveChangesAsync(token);
            return session.Id;
        }, ct);
    }

    public async Task Logout(long sessionId, bool all, CancellationToken ct)
    {
        await db.ExecuteInTransaction(async token =>
        {
            var state = await db.AuthSessions.AsNoTracking().Where(x => x.Id == sessionId)
                .Select(x => new { x.UserId }).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("session");
            if (all)
            {
                var userId = state.UserId ?? throw AppException.NotFound("session");
                // Global lock order: User first, then that User's Sessions in ascending Id order.
                await db.LockUserForSecurityMutation(userId, token);
                await RevokeSessions(db.AuthSessions.Where(x => x.UserId == userId && x.IsActive),
                    "logout_all", token);
            }
            else
            {
                await db.LockSessionForSecurityMutation(sessionId, token);
                var session = await db.AuthSessions.SingleAsync(x => x.Id == sessionId, token);
                await RevokeLockedSessions([session], "logout", token);
            }
            await db.SaveChangesAsync(token);
            return sessionId;
        }, ct);
    }

    public async Task<(long UserId, TokenResponse Tokens)> ProvisionInvitationIdentity(string normalizedMobile,
        string? displayName, string clientTypeKey, string? deviceIdentifier, CancellationToken ct)
    {
        var methodState = await UsableLoginMethods().AsNoTracking().Where(x =>
                x.LoginTypeKey == IamKeys.LoginTypes.Mobile &&
                x.NormalizedIdentifierValue == normalizedMobile)
            .Select(x => new { x.Id, x.UserId }).SingleOrDefaultAsync(ct);
        UserLoginMethod? method = null;
        User user;
        string? partyCode;
        if (methodState is null)
        {
            user = new User(await UniqueCode(db.Users, ct), Now);
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            method = new UserLoginMethod(await UniqueCode(db.UserLoginMethods, ct), user.Id,
                IamKeys.LoginTypes.Mobile, normalizedMobile, normalizedMobile, true, Now);
            method.Verify(Now);
            db.UserLoginMethods.Add(method);
            var personType = await db.PartyTypes.SingleAsync(x =>
                x.Key == PartyReferenceKeys.PartyTypes.IranianPerson, ct);
            var party = new Party(await UniqueCode(db.Parties, ct), personType.Id,
                string.IsNullOrWhiteSpace(displayName) ? "کاربر دعوت‌شده" : displayName.Trim(),
                null, null, null, null, null, Now);
            db.Parties.Add(party);
            await db.SaveChangesAsync(ct);
            db.UserPartyLinks.Add(new UserPartyLink(await UniqueCode(db.UserPartyLinks, ct), user.Id,
                party.Id, true, Now));
            partyCode = party.Code;
        }
        else
        {
            await db.LockUserForSecurityMutation(methodState.UserId, ct);
            method = await UsableLoginMethods().SingleOrDefaultAsync(x => x.Id == methodState.Id &&
                x.UserId == methodState.UserId && x.LoginTypeKey == IamKeys.LoginTypes.Mobile &&
                x.NormalizedIdentifierValue == normalizedMobile, ct)
                ?? throw new AppException(401, "authentication.failed", "Authentication failed.");
            user = await db.Users.SingleAsync(x => x.Id == method.UserId && x.IsActive &&
                x.StatusKey == IamKeys.UserStatuses.Active, ct);
            partyCode = await (from link in db.UserPartyLinks
                               where link.UserId == user.Id && link.IsActive && link.IsPrimary
                               join party in db.Parties on link.PartyId equals party.Id
                               select party.Code)
                .SingleOrDefaultAsync(ct);
        }
        var tokens = await CreateSession(user, method.Id, clientTypeKey, deviceIdentifier, ct);
        await db.SaveChangesAsync(ct);
        return (user.Id, tokens with { PartyCode = partyCode });
    }

    private async Task<TokenResponse> CreateSession(User user, long methodId, string client, string? device, CancellationToken ct)
    {
        client = Required(client, "clientTypeKey").ToLowerInvariant();
        if (client is not ("web" or "mobile")) throw Validation("clientTypeKey", "Client type must be web or mobile.");
        var access = protector.CreateToken(); var refresh = protector.CreateToken();
        var expiry = client == "mobile" ? Now.AddDays(options.MobileSessionDays) : Now.AddHours(options.WebSessionHours);
        var accessExpiry = Now.AddMinutes(options.AccessTokenMinutes);
        var session = new AuthSession(await UniqueCode(db.AuthSessions, ct), user.Id, null, methodId, client,
            protector.Hash(access), accessExpiry, Now, expiry, client == "web" ? Now.AddHours(2) : null, device, null);
        db.AuthSessions.Add(session); await db.SaveChangesAsync(ct);
        db.AuthRefreshTokens.Add(new AuthRefreshToken(session.Id, protector.Hash(refresh), Now, Now.AddDays(options.RefreshTokenDays)));
        return new(access, refresh, accessExpiry, user.Code, null);
    }

    private async Task RevokeReplaySession(long sessionId, CancellationToken ct)
    {
        await db.ExecuteInTransaction(async token =>
        {
            await db.LockSessionForSecurityMutation(sessionId, token);
            var session = await db.AuthSessions.SingleAsync(x => x.Id == sessionId, token);
            await RevokeLockedSessions([session], "refresh_reuse", token);
            await db.SaveChangesAsync(token);
            return session.Id;
        }, ct);
    }

    private IQueryable<UserLoginMethod> UsableLoginMethods() => db.UserLoginMethods.Where(x =>
        x.IsActive && x.IsVerified && x.ReleasedAtUtc == null && x.StatusKey == IamKeys.LoginStatuses.Active);

    private async Task<User> ActiveUser(long userId, CancellationToken ct) =>
        await db.Users.SingleOrDefaultAsync(x => x.Id == userId && x.IsActive &&
            x.StatusKey == IamKeys.UserStatuses.Active, ct) ?? throw AppException.NotFound("user");

    private async Task RevokeSessions(IQueryable<AuthSession> usableSessionQuery, string reason,
        CancellationToken ct)
    {
        var ids = await usableSessionQuery.AsNoTracking().Select(x => x.Id).OrderBy(x => x).ToListAsync(ct);
        foreach (var id in ids) await db.LockSessionForSecurityMutation(id, ct);
        var sessions = await db.AuthSessions.Where(x => ids.Contains(x.Id)).OrderBy(x => x.Id).ToListAsync(ct);
        await RevokeLockedSessions(sessions, reason, ct);
    }

    private async Task RevokeLockedSessions(IReadOnlyCollection<AuthSession> sessions, string reason,
        CancellationToken ct)
    {
        foreach (var session in sessions.Where(x => x.IsActive)) session.Revoke(reason, Now);
        var ids = sessions.Select(x => x.Id).ToList();
        var refreshTokens = await db.AuthRefreshTokens.Where(x => ids.Contains(x.AuthSessionId) &&
            x.RevokedAtUtc == null && x.ConsumedAtUtc == null && x.ExpiresAtUtc > Now).ToListAsync(ct);
        foreach (var refreshToken in refreshTokens) refreshToken.Revoke(Now);
    }

    private static LoginMethodResponse LoginMethod(UserLoginMethod method) => new(method.Code,
        method.LoginTypeKey, method.IdentifierValue, method.IsPrimary, method.IsVerified,
        method.StatusKey, method.ReleasedAtUtc);

    private static async Task<string> UniqueCode<TEntity>(DbSet<TEntity> set, CancellationToken ct) where TEntity : Entity
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var value = string.Create(5, alphabet, static (span, chars) => { for (var i = 0; i < span.Length; i++) span[i] = chars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(chars.Length)]; });
            if (!await set.AnyAsync(x => x.Code == value, ct)) return value;
        }
        throw new InvalidOperationException("Could not allocate a public code.");
    }
    private static string Required(string? value, string field) => string.IsNullOrWhiteSpace(value) ? throw Validation(field, $"{field} is required.") : value.Trim();
    private static AppException Validation(string field, string message) => new(400, "validation.failed", "Request validation failed.", new Dictionary<string, string[]> { [field] = [message] });
}
