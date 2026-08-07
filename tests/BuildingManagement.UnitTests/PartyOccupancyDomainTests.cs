using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class PartyOccupancyDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PartyAllowsOnlyDisplayNameAndType()
    {
        var party = new Party("PTY01", 1, "ساکن واحد 17", null, null, null, null, Now);

        Assert.Equal("ساکن واحد 17", party.DisplayName);
        Assert.Null(party.FirstName);
    }

    [Fact]
    public void PartyRequiresDisplayName() =>
        Assert.Throws<DomainValidationException>(() =>
            new Party("PTY01", 1, " ", null, null, null, null, Now));

    [Fact]
    public void UnitOccupancyCannotBeNegative()
    {
        var unit = new Unit("UNT01", 1, 1, 1, "17", null, null, null, 0, 0, null, Now);

        Assert.Throws<DomainValidationException>(() => unit.ChangeOccupancy(-1, Now));
    }

    [Fact]
    public void OccupancyHistoryClosesAtEffectiveBoundary()
    {
        var history = new UnitOccupancyHistory(1, 3, Now, null, Now);

        history.Close(Now.AddDays(1), Now.AddDays(1));

        Assert.False(history.IsActive);
        Assert.Equal(Now.AddDays(1), history.EffectiveTo);
    }

    [Fact]
    public void RelationRejectsInvalidOwnershipShare() =>
        Assert.Throws<DomainValidationException>(() =>
            new UnitPartyRelation("REL01", 1, 2, 3, Now, null, 101, false, false, null, Now));
}
