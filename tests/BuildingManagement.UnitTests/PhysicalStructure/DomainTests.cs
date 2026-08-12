using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class DomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void PublicCodeHasRequiredFormat() =>
        Assert.Matches("^[A-Z0-9]{5}$", PublicCode.Create());

    [Theory]
    [InlineData("ABCD")]
    [InlineData("ABCDEF")]
    [InlineData("AB-12")]
    [InlineData("")]
    public void InvalidPublicCodeIsRejected(string code) =>
        Assert.Throws<DomainValidationException>(() => new Location(code, null, 1, "تهران", Now));

    [Fact]
    public void ReferenceKeyIsNormalized()
    {
        var type = new BuildingType(" Residential ", "مسکونی", 10, Now);
        Assert.Equal("residential", type.Key);
    }

    [Fact]
    public void InvalidReferenceKeyIsRejected() =>
        Assert.Throws<DomainValidationException>(() => new BuildingType("residential-type", "مسکونی", 10, Now));

    [Theory]
    [InlineData(-91, 0)]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    [InlineData(0, 181)]
    public void InvalidCoordinatesAreRejected(decimal latitude, decimal longitude) =>
        Assert.Throws<DomainValidationException>(() => new Complex("CMP01", 1, "C", "Address", "123",
            latitude, longitude, null, Now));

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 0, -1)]
    public void NegativeUnitValuesAreRejected(decimal area, int parking, int storage) =>
        Assert.Throws<DomainValidationException>(() => new Unit("UNT01", 1, 1, 1, "1", 1, area, 1,
            parking, storage, null, Now));

    [Fact]
    public void UnitNumberMustNotBeBlank() =>
        Assert.Throws<DomainValidationException>(() => new Unit("UNT01", 1, 1, 1, " ", null, null, null,
            0, 0, null, Now));

    [Fact]
    public void ActivationCanBeChanged()
    {
        var unit = new Unit("UNT01", 1, 1, 1, "A-1", null, null, null, 0, 0, null, Now);
        unit.SetActivation(false, Now.AddMinutes(1));
        Assert.False(unit.IsActive);
        Assert.Equal(Now.AddMinutes(1), unit.UpdatedAtUtc);
    }
}
