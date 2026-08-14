using BuildingManagement.Application;
using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class IdentityAccessDomainTests
{
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
}
