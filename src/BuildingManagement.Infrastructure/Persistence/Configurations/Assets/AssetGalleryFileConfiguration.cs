using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class AssetGalleryFileConfiguration : IEntityTypeConfiguration<AssetGalleryFile>
{
    public void Configure(EntityTypeBuilder<AssetGalleryFile> builder)
    {
        AssetFileConfiguration.Base(builder, "AssetGalleryFiles");
        builder.Property(x => x.Title).HasMaxLength(200); builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AssetId, x.SortOrder });
        builder.HasIndex(x => x.AssetId)
            .HasDatabaseName("UX_AssetGalleryFiles_ActiveCover")
            .IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");
    }
}
