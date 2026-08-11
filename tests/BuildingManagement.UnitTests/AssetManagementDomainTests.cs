using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class AssetManagementDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Asset_requires_exactly_one_scope()
    {
        Assert.Throws<DomainValidationException>(() => Create(null, null));
        Assert.Throws<DomainValidationException>(() => Create(1, 1));
        Assert.Null(Create(null, 1).ComplexId);
        Assert.Null(Create(1, null).BuildingId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Asset_rejects_non_positive_review_interval(int days) =>
        Assert.Throws<DomainValidationException>(() => Create(null, 1, days));

    [Fact]
    public void Asset_accepts_null_and_positive_review_interval()
    {
        Assert.Null(Create(null, 1).SuggestedReviewIntervalDays);
        Assert.Equal(30, Create(null, 1, 30).SuggestedReviewIntervalDays);
    }

    [Fact]
    public void Event_dates_and_optional_provider_are_validated()
    {
        var valid = new AssetEvent(1, 1, Now, "سرویس", null, Now.AddDays(30), null, null, Now);
        Assert.Null(valid.ServiceProviderPartyId);
        Assert.Throws<DomainValidationException>(() =>
            new AssetEvent(1, 1, Now, "سرویس", null, Now.AddDays(-1), null, null, Now));
    }

    [Fact]
    public void Gallery_cover_can_be_removed()
    {
        var gallery = new AssetGalleryFile(1, 1, null, null, null, 0, true, Now);
        gallery.RemoveCover(Now.AddMinutes(1));
        Assert.False(gallery.IsCover);
    }

    private static Asset Create(long? complexId, long? buildingId, int? days = null) =>
        new("A1B2C", 1, complexId, buildingId, "آسانسور", null, null, null, null, null, days, null, Now);
}
