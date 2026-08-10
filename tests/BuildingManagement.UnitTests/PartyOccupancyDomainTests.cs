using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class PartyOccupancyDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PartyAllowsOnlyDisplayNameAndType()
    {
        var party = new Party("PTY01", 1, "ساکن واحد 17", null, null, null, null, null, Now);

        Assert.Equal("ساکن واحد 17", party.DisplayName);
        Assert.Null(party.FirstName);
        Assert.Null(party.IdentityNumber);
    }

    [Fact]
    public void PartyRequiresDisplayName() =>
        Assert.Throws<DomainValidationException>(() =>
            new Party("PTY01", 1, " ", null, null, null, null, null, Now));

    [Fact]
    public void PartyAllowsOptionalIdentityNumber()
    {
        var party = new Party("PTY01", 1, "علی رضایی", null, null, null, "0012345678", null, Now);

        Assert.Equal("0012345678", party.IdentityNumber);
    }

    [Fact]
    public void PartyIdentifierAbstractionDoesNotExist()
    {
        var assembly = typeof(Party).Assembly;

        Assert.Null(assembly.GetType("BuildingManagement.Domain.PartyIdentifier"));
        Assert.Null(assembly.GetType("BuildingManagement.Domain.PartyIdentifierType"));
    }

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
    public void RelationAllowsUnknownDates()
    {
        var relation = new UnitPartyRelation(1, 2, 3, null, null, null, Now);

        Assert.Null(relation.StartDate);
        Assert.Null(relation.EndDate);
        Assert.True(relation.IsActive);
    }

    [Fact]
    public void UnitPartyRelationHasNoPrimaryContactProperty() =>
        Assert.Null(typeof(UnitPartyRelation).GetProperty("IsPrimaryContact"));

    [Fact]
    public void PartyContactCanBePrimary()
    {
        var contact = new PartyContact(1, 1, "09120000000", "09120000000", null, true, Now);

        Assert.True(contact.IsPrimary);
    }

    [Fact]
    public void UnverifiedPartyContactCanBecomePrimaryIdempotently()
    {
        var contact = new PartyContact(1, 1, "09120000000", "09120000000", null, false, Now);

        contact.SetPrimary(true, Now.AddMinutes(1));
        var updatedAt = contact.UpdatedAtUtc;
        contact.SetPrimary(true, Now.AddMinutes(2));

        Assert.True(contact.IsPrimary);
        Assert.False(contact.IsVerified);
        Assert.Equal(updatedAt, contact.UpdatedAtUtc);
    }

    [Fact]
    public void EndingRelationWithoutKnownDateUsesCurrentTimeAndKeepsRecordActive()
    {
        var relation = new UnitPartyRelation(1, 2, 3, null, null, null, Now);

        relation.End(null, Now.AddDays(1));

        Assert.Equal(Now.AddDays(1), relation.EndDate);
        Assert.True(relation.IsActive);
    }

    [Fact]
    public void SoftDeletingRelationOnlyChangesActivation()
    {
        var relation = new UnitPartyRelation(1, 2, 3, null, null, null, Now);

        relation.SoftDelete(Now.AddDays(1));

        Assert.False(relation.IsActive);
        Assert.Null(relation.EndDate);
    }

    [Fact]
    public void OccupancyHistoryAllowsUnknownEffectiveFrom()
    {
        var history = new UnitOccupancyHistory(1, 3, null, null, Now);

        Assert.Null(history.EffectiveFrom);
    }
}
