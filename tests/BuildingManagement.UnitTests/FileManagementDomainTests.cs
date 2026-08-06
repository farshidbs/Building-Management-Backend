using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class FileManagementDomainTests
{
    [Fact]
    public void StoredFileRejectsInvalidExtension()
    {
        var exception = Assert.Throws<DomainValidationException>(() =>
            new StoredFile("A1B2C", "photo.exe", "value.exe", "buildings/1/gallery/value.exe",
                "application/octet-stream", ".exe!", 10, null, StorageProviders.Local, DateTimeOffset.UtcNow));
        Assert.Equal("fileExtension", exception.Field);
    }

    [Fact]
    public void GalleryMetadataCanBeUpdatedWithoutReplacingStoredFile()
    {
        var now = DateTimeOffset.UtcNow;
        var gallery = new BuildingGalleryFile("A1B2C", 1, 2, null, null, null, 0, false, now);
        gallery.Update("نما", "تصویر اصلی", "نمای ساختمان", 2, true, now.AddMinutes(1));
        Assert.Equal(2, gallery.StoredFileId);
        Assert.True(gallery.IsCover);
        Assert.Equal("نما", gallery.Title);
    }

    [Fact]
    public void DocumentRejectsExpirationBeforeEffectiveDate()
    {
        var now = DateTimeOffset.UtcNow;
        var exception = Assert.Throws<DomainValidationException>(() =>
            new BuildingDocument("A1B2C", 1, 2, 3, "بیمه‌نامه", null, now, now.AddDays(2),
                now.AddDays(1), null, false, true, true, now));
        Assert.Equal("expiresAt", exception.Field);
    }
}
