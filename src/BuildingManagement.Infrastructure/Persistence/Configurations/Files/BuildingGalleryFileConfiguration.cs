using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class BuildingGalleryFileConfiguration : IEntityTypeConfiguration<BuildingGalleryFile>
{
    public void Configure(EntityTypeBuilder<BuildingGalleryFile> builder)
    {
        ConfigurationHelpers.Entity(builder, "BuildingGalleryFiles");
        ConfigureGallery(builder);
        builder.HasOne<Building>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BuildingId, x.StoredFileId }).IsUnique();
        builder.HasIndex(x => x.BuildingId).IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");
    }

    private static void ConfigureGallery(EntityTypeBuilder<BuildingGalleryFile> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.HasIndex(x => new { x.BuildingId, x.IsActive, x.SortOrder });
    }
}
