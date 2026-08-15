using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class RecoveryService(IApplicationDbContext db, TimeProvider clock,
    IIamSecretProtector protector, IOtpDelivery otpDelivery, IamOptions options)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<RecoveryReferenceResponse> Start(StartRecoveryRequest request, CancellationToken ct)
    {
        var oldMobile = IranianMobileNormalizer.Normalize(request.OldMobile);
        var newMobile = IranianMobileNormalizer.Normalize(request.NewMobile);
        var reference = protector.CreateToken(24);
        var candidates = await (from method in db.UserLoginMethods.AsNoTracking()
                                join link in db.UserPartyLinks.AsNoTracking() on method.UserId equals link.UserId
                                join party in db.Parties.AsNoTracking() on link.PartyId equals party.Id
                                where method.NormalizedIdentifierValue == oldMobile && link.IsActive && link.IsPrimary &&
                                      (request.IdentityNumber == null || party.IdentityNumber == request.IdentityNumber.Trim()) &&
                                      (!request.BirthDate.HasValue || party.BirthDate == request.BirthDate)
                                select new { method.UserId, MethodId = method.Id, party.Id })
            .Distinct().ToListAsync(ct);
        var users = candidates.Select(x => x.UserId).Distinct().ToList();
        var candidate = users.Count == 1
            ? candidates.OrderByDescending(x => x.MethodId).First(x => x.UserId == users[0]) : null;
        var recovery = new AccountRecoveryCase(await UniqueCode(db.AccountRecoveryCases, ct),
            protector.Hash(reference), candidate?.UserId, candidate?.MethodId, oldMobile, newMobile,
            request.IdentityNumber?.Trim(), request.BirthDate, Now.AddHours(24), Now);
        db.AccountRecoveryCases.Add(recovery);
        if (users.Count > 1)
            db.IdentityConflictReviews.Add(new IdentityConflictReview(
                await UniqueCode(db.IdentityConflictReviews, ct), null, null,
                "account_recovery_ambiguous", $"RecoveryCase:{recovery.Code}", Now));
        await db.SaveChangesAsync(ct);
        return new(reference, recovery.StatusKey, recovery.ExpiresAtUtc);
    }

    public async Task<RecoveryStatusResponse> Status(string reference, CancellationToken ct)
    {
        var recovery = await Find(reference, ct);
        if (recovery.ExpiresAtUtc <= Now && recovery.IsActive)
        {
            recovery.Expire(Now);
            await db.SaveChangesAsync(ct);
        }
        return SafeStatus(recovery);
    }

    public async Task<RecoveryOtpRequestResponse> RequestOtp(string reference, CancellationToken ct)
    {
        var recovery = await Find(reference, ct);
        if (!recovery.IsActive || recovery.ExpiresAtUtc <= Now ||
            recovery.StatusKey is not ("pending_mobile_verification" or "approved"))
            throw AppException.NotFound("recovery");
        var mobile = recovery.NewNormalizedIdentifier!;
        if (await db.OtpChallenges.AnyAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.PurposeKey == "account_recovery_new_mobile" && x.StatusKey == IamKeys.OtpStatuses.Pending &&
            x.SentAtUtc > Now.AddMinutes(-1), ct))
            throw new AppException(429, "otp.cooldown", "Wait before requesting another OTP.");
        var code = protector.CreateOtp();
        var challenge = new OtpChallenge(protector.CreateToken(18), IamKeys.LoginTypes.Mobile,
            RecoveryBinding(recovery, mobile), mobile, "account_recovery_new_mobile",
            protector.Hash(code), Now, Now.AddMinutes(options.OtpLifetimeMinutes), options.OtpMaxAttempts);
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync(ct);
        await otpDelivery.Send(mobile, code, ct);
        return new(challenge.PublicReference, challenge.ExpiresAtUtc);
    }

    public async Task<RecoveryStatusResponse> VerifyMobile(string reference,
        VerifyRecoveryMobileRequest request, CancellationToken ct)
    {
        var recoveryHash = protector.Hash(Required(reference, "recoveryReference"));
        var challengeReference = Required(request.ChallengeReference, "challengeReference");
        if (!await db.TryReserveOtpAttempt(challengeReference, Now, ct)) throw InvalidOtp();
        var challenge = await db.OtpChallenges.AsNoTracking().SingleOrDefaultAsync(x =>
            x.PublicReference == challengeReference && x.PurposeKey == "account_recovery_new_mobile", ct);
        if (challenge is null || !protector.Verify(Required(request.OtpCode, "otpCode"), challenge.CodeHash))
        {
            await db.MarkOtpAttemptFailed(challengeReference, Now, ct);
            throw InvalidOtp();
        }
        return await db.ExecuteInTransaction(async token =>
        {
            await db.LockRecoveryCase(recoveryHash, token);
            var recovery = await db.AccountRecoveryCases.SingleAsync(x => x.ReferenceHash == recoveryHash, token);
            var tracked = await BoundChallenge(recovery, challengeReference, token);
            if (!protector.Verify(request.OtpCode, tracked.CodeHash)) throw InvalidOtp();
            tracked.Verify(Now); tracked.Consume(Now); recovery.MarkMobileVerified(Now);
            await db.SaveChangesAsync(token);
            return SafeStatus(recovery);
        }, ct);
    }

    public async Task<TokenResponse> Complete(string reference, CompleteRecoveryRequest request,
        CancellationToken ct)
    {
        var recoveryHash = protector.Hash(Required(reference, "recoveryReference"));
        var challengeReference = Required(request.ChallengeReference, "challengeReference");
        if (!await db.TryReserveOtpAttempt(challengeReference, Now, ct)) throw InvalidOtp();
        var proof = await db.OtpChallenges.AsNoTracking().SingleOrDefaultAsync(x =>
            x.PublicReference == challengeReference && x.PurposeKey == "account_recovery_new_mobile", ct);
        if (proof is null || !protector.Verify(Required(request.OtpCode, "otpCode"), proof.CodeHash))
        {
            await db.MarkOtpAttemptFailed(challengeReference, Now, ct); throw InvalidOtp();
        }
        try
        {
            return await db.ExecuteInTransaction(async token =>
            {
                await db.LockRecoveryCase(recoveryHash, token);
                var recovery = await db.AccountRecoveryCases.SingleOrDefaultAsync(x =>
                    x.ReferenceHash == recoveryHash, token) ?? throw AppException.NotFound("recovery");
                if (recovery.StatusKey == "completed")
                    throw AppException.Conflict("recovery.already_completed", "Recovery was already completed.");
                if (recovery.StatusKey != "approved" || !recovery.UserId.HasValue || recovery.ExpiresAtUtc <= Now)
                    throw AppException.Conflict("recovery.not_completable", "Recovery is not completable.");
                await db.LockUserForSecurityMutation(recovery.UserId.Value, token);
                var challenge = await BoundChallenge(recovery, challengeReference, token);
                if (!protector.Verify(request.OtpCode, challenge.CodeHash)) throw InvalidOtp();
                var newMobile = recovery.NewNormalizedIdentifier!;
                if (await UsableMethods().AnyAsync(x => x.NormalizedIdentifierValue == newMobile, token))
                    throw AppException.Conflict("recovery.identifier_unavailable", "Recovery cannot be completed.");
                var methods = await UsableMethods().Where(x => x.UserId == recovery.UserId).ToListAsync(token);
                foreach (var method in methods.Where(x => x.IsPrimary)) method.SetPrimary(false, Now);
                await db.SaveChangesAsync(token);
                foreach (var method in methods.Where(x => x.NormalizedIdentifierValue ==
                    recovery.OldNormalizedIdentifier)) method.Release("account_recovery", Now);
                var created = new UserLoginMethod(await UniqueCode(db.UserLoginMethods, token),
                    recovery.UserId.Value, IamKeys.LoginTypes.Mobile, newMobile, newMobile, true, Now);
                created.Verify(Now); db.UserLoginMethods.Add(created);
                challenge.Verify(Now); challenge.Consume(Now);
                await RevokeAllSessions(recovery.UserId.Value, token);
                await db.SaveChangesAsync(token);
                var user = await db.Users.SingleAsync(x => x.Id == recovery.UserId && x.IsActive, token);
                var result = await CreateSession(user, created.Id, request.ClientTypeKey,
                    request.DeviceIdentifier, token);
                recovery.Complete(Now);
                await db.SaveChangesAsync(token);
                return result;
            }, ct);
        }
        catch (DbUpdateException exception) when (db.IsUniqueViolation(exception))
        {
            throw AppException.Conflict("recovery.identifier_unavailable", "Recovery cannot be completed.");
        }
    }

    private async Task<OtpChallenge> BoundChallenge(AccountRecoveryCase recovery, string reference,
        CancellationToken ct) => await db.OtpChallenges.SingleOrDefaultAsync(x =>
        x.PublicReference == reference && x.PurposeKey == "account_recovery_new_mobile" &&
        x.NormalizedIdentifierValue == recovery.NewNormalizedIdentifier &&
        x.IdentifierValue == RecoveryBinding(recovery, recovery.NewNormalizedIdentifier!), ct)
        ?? throw InvalidOtp();

    private async Task RevokeAllSessions(long userId, CancellationToken ct)
    {
        var ids = await db.AuthSessions.AsNoTracking().Where(x => x.UserId == userId && x.IsActive)
            .Select(x => x.Id).OrderBy(x => x).ToListAsync(ct);
        foreach (var id in ids) await db.LockSessionForSecurityMutation(id, ct);
        var sessions = await db.AuthSessions.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        foreach (var session in sessions.Where(x => x.IsActive)) session.Revoke("account_recovery", Now);
        var tokens = await db.AuthRefreshTokens.Where(x => ids.Contains(x.AuthSessionId) &&
            x.RevokedAtUtc == null && x.ConsumedAtUtc == null).ToListAsync(ct);
        foreach (var token in tokens) token.Revoke(Now);
    }

    private async Task<TokenResponse> CreateSession(User user, long methodId, string client,
        string? device, CancellationToken ct)
    {
        client = Required(client, "clientTypeKey").ToLowerInvariant();
        if (client is not ("web" or "mobile")) throw Validation("clientTypeKey", "Client type must be web or mobile.");
        var access = protector.CreateToken(); var refresh = protector.CreateToken();
        var expiry = client == "mobile" ? Now.AddDays(options.MobileSessionDays) : Now.AddHours(options.WebSessionHours);
        var accessExpiry = Now.AddMinutes(options.AccessTokenMinutes);
        var session = new AuthSession(await UniqueCode(db.AuthSessions, ct), user.Id, null, methodId,
            client, protector.Hash(access), accessExpiry, Now, expiry,
            client == "web" ? Now.AddHours(2) : null, device, null);
        db.AuthSessions.Add(session); await db.SaveChangesAsync(ct);
        db.AuthRefreshTokens.Add(new AuthRefreshToken(session.Id, protector.Hash(refresh), Now,
            Now.AddDays(options.RefreshTokenDays)));
        var partyCode = await (from link in db.UserPartyLinks where link.UserId == user.Id &&
            link.IsActive && link.IsPrimary join party in db.Parties on link.PartyId equals party.Id
            select party.Code).SingleOrDefaultAsync(ct);
        return new(access, refresh, accessExpiry, user.Code, partyCode);
    }

    private async Task<AccountRecoveryCase> Find(string reference, CancellationToken ct)
    {
        var hash = protector.Hash(Required(reference, "recoveryReference"));
        return await db.AccountRecoveryCases.SingleOrDefaultAsync(x => x.ReferenceHash == hash, ct)
            ?? throw AppException.NotFound("recovery");
    }
    private IQueryable<UserLoginMethod> UsableMethods() => db.UserLoginMethods.Where(x => x.IsActive &&
        x.IsVerified && x.ReleasedAtUtc == null && x.StatusKey == IamKeys.LoginStatuses.Active);
    private static string RecoveryBinding(AccountRecoveryCase recovery, string mobile) => $"{recovery.Code}:{mobile}";
    private static RecoveryStatusResponse SafeStatus(AccountRecoveryCase value) => new(value.StatusKey,
        value.ExpiresAtUtc, value.StatusKey == "approved");
    private static AppException InvalidOtp() => new(400, "otp.invalid", "OTP is invalid or expired.");
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
