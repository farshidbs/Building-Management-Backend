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
                var method = await db.UserLoginMethods.SingleOrDefaultAsync(x => x.IsActive && x.IsVerified &&
                    x.NormalizedIdentifierValue == challenge.NormalizedIdentifierValue, token);
                User user;
                string? partyCode;
                if (method is null)
                {
                    if (challenge.PurposeKey == "login") throw new AppException(401, "authentication.failed", "Authentication failed.");
                    user = new User(await UniqueCode(db.Users, token), Now); db.Users.Add(user); await db.SaveChangesAsync(token);
                    method = new UserLoginMethod(await UniqueCode(db.UserLoginMethods, token), user.Id,
                        IamKeys.LoginTypes.Mobile, challenge.IdentifierValue, challenge.NormalizedIdentifierValue, true, Now);
                    method.Verify(Now); db.UserLoginMethods.Add(method);
                    var personType = await db.PartyTypes.SingleAsync(x => x.Key == "person", token);
                    var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? "کاربر جدید" : request.DisplayName.Trim();
                    var party = new Party(await UniqueCode(db.Parties, token), personType.Id, displayName,
                        null, null, null, null, null, Now); db.Parties.Add(party); await db.SaveChangesAsync(token);
                    db.UserPartyLinks.Add(new UserPartyLink(await UniqueCode(db.UserPartyLinks, token), user.Id, party.Id, true, Now));
                    partyCode = party.Code;
                }
                else
                {
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
            .Select(x => new { x.AuthSessionId, x.ConsumedAtUtc, x.RevokedAtUtc, x.ExpiresAtUtc })
            .SingleOrDefaultAsync(ct)
            ?? throw new AppException(401, "refresh_token.invalid", "Refresh token is invalid.");
        if (state.ConsumedAtUtc.HasValue || state.RevokedAtUtc.HasValue)
        {
            await RevokeReplaySession(state.AuthSessionId, ct);
            throw new AppException(401, "refresh_token.reused", "Refresh token reuse was detected.");
        }

        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                var old = await db.AuthRefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == hash, token)
                    ?? throw new AppException(401, "refresh_token.invalid", "Refresh token is invalid.");
                var session = await db.AuthSessions.SingleAsync(x => x.Id == old.AuthSessionId, token);
                if (!session.IsUsable(Now) || old.ExpiresAtUtc <= Now || old.ConsumedAtUtc.HasValue || old.RevokedAtUtc.HasValue)
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

    public async Task Logout(long sessionId, bool all, CancellationToken ct)
    {
        var session = await db.AuthSessions.SingleAsync(x => x.Id == sessionId, ct);
        var sessions = all && session.UserId.HasValue ? await db.AuthSessions.Where(x => x.UserId == session.UserId && x.IsActive).ToListAsync(ct) : [session];
        foreach (var item in sessions) item.Revoke(all ? "logout_all" : "logout", Now);
        await db.SaveChangesAsync(ct);
    }

    public async Task<(long UserId, TokenResponse Tokens)> ProvisionInvitationIdentity(string normalizedMobile,
        string? displayName, string clientTypeKey, string? deviceIdentifier, long? identityPartyId,
        CancellationToken ct)
    {
        var method = await db.UserLoginMethods.SingleOrDefaultAsync(x => x.IsActive && x.IsVerified &&
            x.LoginTypeKey == IamKeys.LoginTypes.Mobile && x.NormalizedIdentifierValue == normalizedMobile, ct);
        User user;
        string? partyCode;
        if (method is null)
        {
            user = new User(await UniqueCode(db.Users, ct), Now);
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            method = new UserLoginMethod(await UniqueCode(db.UserLoginMethods, ct), user.Id,
                IamKeys.LoginTypes.Mobile, normalizedMobile, normalizedMobile, true, Now);
            method.Verify(Now);
            db.UserLoginMethods.Add(method);
            Party party;
            if (identityPartyId.HasValue)
                party = await db.Parties.SingleAsync(x => x.Id == identityPartyId && x.IsActive, ct);
            else
            {
                var personType = await db.PartyTypes.SingleAsync(x => x.Key == "person", ct);
                party = new Party(await UniqueCode(db.Parties, ct), personType.Id,
                    string.IsNullOrWhiteSpace(displayName) ? "کاربر دعوت‌شده" : displayName.Trim(),
                    null, null, null, null, null, Now);
                db.Parties.Add(party);
                await db.SaveChangesAsync(ct);
            }
            db.UserPartyLinks.Add(new UserPartyLink(await UniqueCode(db.UserPartyLinks, ct), user.Id,
                party.Id, true, Now));
            partyCode = party.Code;
        }
        else
        {
            user = await db.Users.SingleAsync(x => x.Id == method.UserId && x.IsActive &&
                x.StatusKey == IamKeys.UserStatuses.Active, ct);
            partyCode = await (from link in db.UserPartyLinks where link.UserId == user.Id && link.IsActive && link.IsPrimary
                               join party in db.Parties on link.PartyId equals party.Id select party.Code)
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
        var session = await db.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, ct);
        if (session is not null && session.IsActive)
        {
            session.Revoke("refresh_reuse", Now);
            await db.SaveChangesAsync(ct);
        }
    }

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
