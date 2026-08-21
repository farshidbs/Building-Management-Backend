using BuildingManagement.Application;
using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class IdentityAccessDomainTests
{
    [Fact]
    public void RecoveryRequiresMobileProofAndSupportApprovalBeforeSingleCompletion()
    {
        var recovery = new AccountRecoveryCase("RCV01", "reference-hash", 10, 20,
            "+989121111111", "+989122222222", "0012345678", new DateOnly(1990, 1, 1),
            Now.AddHours(1), Now);
        Assert.Equal("pending_mobile_verification", recovery.StatusKey);
        Assert.Throws<DomainValidationException>(() => recovery.Approve(5, "ticket", Now));
        recovery.MarkMobileVerified(Now.AddMinutes(1));
        recovery.Approve(5, "ticket-123", Now.AddMinutes(2));
        recovery.Complete(Now.AddMinutes(3));
        Assert.Equal("completed", recovery.StatusKey);
        Assert.False(recovery.IsActive);
        Assert.Throws<DomainValidationException>(() => recovery.Complete(Now.AddMinutes(4)));
    }

    [Fact]
    public void RejectedOrExpiredRecoveryCannotComplete()
    {
        var rejected = new AccountRecoveryCase("RCV02", "hash-2", 10, 20, "+989121111111",
            "+989122222222", null, null, Now.AddHours(1), Now);
        rejected.MarkMobileVerified(Now); rejected.Reject(5, "evidence mismatch", Now);
        Assert.Throws<DomainValidationException>(() => rejected.Complete(Now));
        var expired = new AccountRecoveryCase("RCV03", "hash-3", 10, 20, "+989121111111",
            "+989122222222", null, null, Now, Now.AddHours(-1));
        expired.Expire(Now);
        Assert.Equal("expired", expired.StatusKey);
    }

    [Fact]
    public void SupportActingSessionRetainsPlatformTargetSourceAndRevocationHistory()
    {
        var acting = new SupportActingSession("ACT01", 7, 11, 13, "token-hash",
            "بررسی تیکت", "T-100", Now.AddMinutes(30), Now);
        Assert.Equal(7, acting.PlatformUserId);
        Assert.Equal(11, acting.TargetUserId);
        Assert.Equal(13, acting.PlatformAuthSessionId);
        acting.End("platform_revoked", Now.AddMinutes(2));
        acting.End("duplicate", Now.AddMinutes(3));
        Assert.False(acting.IsActive);
        Assert.Equal("platform_revoked", acting.EndReasonKey);
        Assert.Equal(Now.AddMinutes(2), acting.EndedAtUtc);
    }

    [Fact]
    public void PlatformAndActingDtosDoNotExposeHashesOrInternalIds()
    {
        var types = new[] { typeof(PlatformTokenResponse), typeof(ActingSessionTokenResponse),
            typeof(RecoveryReviewResponse) };
        Assert.All(types, type => Assert.DoesNotContain(type.GetProperties(), property =>
            property.Name.Contains("Hash", StringComparison.OrdinalIgnoreCase) || property.Name == "Id"));
    }

    [Fact]
    public void LoginMethodVerificationPrimarySwitchAndReleasePreserveHistory()
    {
        var now = DateTimeOffset.UtcNow;
        var method = new UserLoginMethod("LM001", 10, IamKeys.LoginTypes.Mobile,
            "+989121234567", "+989121234567", false, now);
        method.Verify(now.AddMinutes(1));
        method.SetPrimary(true, now.AddMinutes(2));
        Assert.True(method.IsActive);
        Assert.True(method.IsVerified);
        Assert.True(method.IsPrimary);
        Assert.Equal(IamKeys.LoginStatuses.Active, method.StatusKey);

        method.Release("number_changed", now.AddMinutes(3));
        Assert.False(method.IsActive);
        Assert.False(method.IsPrimary);
        Assert.Equal(IamKeys.LoginStatuses.Released, method.StatusKey);
        Assert.Equal("+989121234567", method.NormalizedIdentifierValue);
        Assert.Equal("number_changed", method.ReleaseReasonKey);
        Assert.Equal(now.AddMinutes(3), method.ReleasedAtUtc);
    }

    [Fact]
    public void SessionAndRefreshTokenRevocationAreHistoricalAndIdempotent()
    {
        var now = DateTimeOffset.UtcNow;
        var session = new AuthSession("SES01", 10, null, 20, "web", "access-hash",
            now.AddMinutes(15), now, now.AddHours(12), now.AddHours(2), "browser", null);
        var refresh = new AuthRefreshToken(30, "refresh-hash", now, now.AddDays(30));
        session.Revoke("login_method_released", now.AddMinutes(1));
        refresh.Revoke(now.AddMinutes(1));
        refresh.Revoke(now.AddMinutes(2));
        Assert.False(session.IsUsable(now.AddMinutes(2)));
        Assert.Equal(IamKeys.SessionStatuses.Revoked, session.StatusKey);
        Assert.Equal("login_method_released", session.RevokeReasonKey);
        Assert.NotNull(refresh.RevokedAtUtc);
    }

    [Fact]
    public void LoginMethodAndSessionDtosContainNoInternalIdsOrHashes()
    {
        var loginProperties = typeof(LoginMethodResponse).GetProperties().Select(x => x.Name).ToArray();
        var sessionProperties = typeof(SessionResponse).GetProperties().Select(x => x.Name).ToArray();
        Assert.DoesNotContain(loginProperties, x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase) ||
            x is "Id" or "UserId");
        Assert.DoesNotContain(sessionProperties, x => x.Contains("Hash", StringComparison.OrdinalIgnoreCase) ||
            x is "Id" or "UserId");
    }

    [Fact]
    public void InvitationLifecycleRejectsExpiredAndRevokedAcceptanceAndIsIdempotentForSameUser()
    {
        var now = DateTimeOffset.UtcNow;
        var accepted = new Invitation("ABCDE", "hash", "989121234567", "building_collaborator",
            1, null, 1, null, 10, null, now.AddDays(1), now);
        accepted.Accept(20, now);
        accepted.Accept(20, now.AddMinutes(1));
        Assert.Equal(20, accepted.AcceptedByUserId);
        Assert.Throws<DomainValidationException>(() => accepted.Accept(21, now.AddMinutes(2)));
        Assert.Throws<DomainValidationException>(() => accepted.Revoke(now.AddMinutes(2)));

        var revoked = new Invitation("FGHIJ", "hash2", "989121234568", "unit_person",
            2, null, null, 2, 10, 30, now.AddDays(1), now);
        revoked.Revoke(now);
        Assert.Throws<DomainValidationException>(() => revoked.Accept(20, now));
        var expired = new Invitation("KLMNO", "hash3", "989121234569", "unit_person",
            2, null, null, 2, 10, 30, now, now.AddDays(-1));
        Assert.Throws<DomainValidationException>(() => expired.Accept(20, now));
        expired.Expire(now);
        Assert.False(expired.IsActive);
        Assert.Equal(now, expired.ExpiredAtUtc);
        Assert.Throws<DomainValidationException>(() => expired.Revoke(now.AddMinutes(1)));
        Assert.Null(expired.RevokedAtUtc);
        expired.Expire(now.AddMinutes(1));

        var future = new Invitation("PQRST", "hash4", "989121234570", "unit_person",
            2, null, null, 2, 10, 30, now.AddDays(1), now);
        Assert.Throws<DomainValidationException>(() => future.Expire(now));
    }

    [Theory]
    [InlineData("09121234567", "+989121234567")]
    [InlineData("+98 912 123 4567", "+989121234567")]
    public void InvitationMobileBindingUsesCanonicalIranianMobile(string input, string expected) =>
        Assert.Equal(expected, IranianMobileNormalizer.Normalize(input));
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("09121234567", "+989121234567")]
    [InlineData("+98 912 123 4567", "+989121234567")]
    [InlineData("00989121234567", "+989121234567")]
    [InlineData("۰۹۱۲۱۲۳۴۵۶۷", "+989121234567")]
    public void IranianMobileNormalizationIsCanonical(string input, string expected) =>
        Assert.Equal(expected, IranianMobileNormalizer.Normalize(input));

    [Theory]
    [InlineData("")]
    [InlineData("02112345678")]
    [InlineData("0912123456")]
    public void InvalidMobileIsRejected(string input) =>
        Assert.Throws<AppException>(() => IranianMobileNormalizer.Normalize(input));

    [Fact]
    public void SessionRequiresExactlyOneActor()
    {
        Assert.Throws<DomainValidationException>(() => new AuthSession("SES01", null, null, null,
            "web", "hash", Now.AddMinutes(15), Now, Now.AddHours(1), null, null, null));
        Assert.Throws<DomainValidationException>(() => new AuthSession("SES01", 1, 2, null,
            "web", "hash", Now.AddMinutes(15), Now, Now.AddHours(1), null, null, null));
    }

    [Fact]
    public void MembershipRequiresExactlyOneScope()
    {
        Assert.Throws<DomainValidationException>(() =>
            new AccessMembership("MEM01", 1, 1, null, null, null, null, Now));
        Assert.Throws<DomainValidationException>(() =>
            new AccessMembership("MEM01", 1, 1, 1, 2, null, null, Now));
    }

    [Fact]
    public void ReleasedLoginMethodKeepsHistoryButStopsBeingUsable()
    {
        var method = new UserLoginMethod("LOG01", 1, "mobile", "09121234567", "+989121234567", true, Now);
        method.Verify(Now); method.Release("changed_number", Now.AddDays(1));
        Assert.False(method.IsActive); Assert.False(method.IsPrimary);
        Assert.Equal(IamKeys.LoginStatuses.Released, method.StatusKey);
        Assert.NotNull(method.ReleasedAtUtc);
    }

    [Fact]
    public void RefreshTokenCannotBeConsumedTwice()
    {
        var token = new AuthRefreshToken(1, "hash", Now, Now.AddDays(1));
        token.Consume(Now);
        Assert.Throws<DomainValidationException>(() => token.Consume(Now));
    }

    [Fact]
    public void PartyBirthDateIsOptionalAndExplicitlyMutable()
    {
        var party = new Party("PTY01", 1, "علی رضایی", null, null, null, null, null, Now);
        Assert.Null(party.BirthDate);
        party.SetBirthDate(new DateOnly(1990, 1, 2), Now);
        Assert.Equal(new DateOnly(1990, 1, 2), party.BirthDate);
    }

    [Fact]
    public void SensitivePermissionsCannotBeBuildingOverriddenOrIndividuallyGranted()
    {
        Assert.False(IamPermissionPolicy.CanOverrideAtBuilding("file_read_confidential"));
        Assert.False(IamPermissionPolicy.CanOverrideAtBuilding("membership_manage_scoped"));
        Assert.False(IamPermissionPolicy.CanGrantToIndividual("building_manage"));
        Assert.True(IamPermissionPolicy.CanGrantToIndividual("financial_unit_view_own"));
        Assert.True(IamPermissionPolicy.CanOverrideAtBuilding("unit_view"));
        Assert.Throws<AppException>(() => IamPermissionPolicy.RequireOverrideable("file_read_confidential"));
        Assert.Throws<AppException>(() => IamPermissionPolicy.RequireGrantable("building_manage"));
    }

    [Fact]
    public void MembershipExitRetainsHistoryAcrossApprovalRejectionAndCancellation()
    {
        var approved = new MembershipExitRequest("EXT01", 10, 20, "خروج", Now);
        approved.Decide(true, 30, "تأیید مدیر", Now.AddMinutes(1));
        Assert.Equal("approved", approved.StatusKey);
        Assert.False(approved.IsActive);
        Assert.Equal(30, approved.DecidedByUserId);

        var rejected = new MembershipExitRequest("EXT02", 11, 21, null, Now);
        rejected.Decide(false, 31, "نیاز به بررسی", Now.AddMinutes(1));
        Assert.Equal("rejected", rejected.StatusKey);
        Assert.False(rejected.IsActive);

        var cancelled = new MembershipExitRequest("EXT03", 12, 22, null, Now);
        cancelled.Cancel(22, Now.AddMinutes(1));
        Assert.Equal("cancelled", cancelled.StatusKey);
        Assert.False(cancelled.IsActive);
        Assert.Throws<DomainValidationException>(() => cancelled.Cancel(22, Now.AddMinutes(2)));
    }

    [Fact]
    public void AccessGrantSupportsScheduledActivationAndPreservesRevocationHistory()
    {
        var grant = new AccessGrant("GRT01", 1, 2, 3, null, null, 4,
            Now.AddHours(1), Now.AddHours(2), "دسترسی موقت", Now);
        Assert.Equal(Now.AddHours(1), grant.StartsAtUtc);
        grant.Revoke(Now.AddMinutes(10));
        Assert.False(grant.IsActive);
        Assert.Equal(Now.AddMinutes(10), grant.RevokedAtUtc);
        Assert.Throws<DomainValidationException>(() => new AccessGrant("GRT02", 1, 2, 3,
            null, null, 4, Now.AddHours(2), Now.AddHours(1), "نامعتبر", Now));
    }
}
