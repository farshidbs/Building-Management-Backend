using BuildingManagement.Application;
using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class IdentityAccessDomainTests
{
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
            "web", "hash", Now, Now.AddHours(1), null, null, null));
        Assert.Throws<DomainValidationException>(() => new AuthSession("SES01", 1, 2, null,
            "web", "hash", Now, Now.AddHours(1), null, null, null));
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
}
