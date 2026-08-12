using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class ComplexGalleryFileConfiguration : IEntityTypeConfiguration<ComplexGalleryFile>
{
    public void Configure(EntityTypeBuilder<ComplexGalleryFile> builder)
    {
        ConfigurationHelpers.Entity(builder, "ComplexGalleryFiles");
        builder.Property(x => x.Title).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.HasOne<Complex>().WithMany().HasForeignKey(x => x.ComplexId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ComplexId, x.StoredFileId }).IsUnique();
        builder.HasIndex(x => new { x.ComplexId, x.IsActive, x.SortOrder });
        builder.HasIndex(x => x.ComplexId).IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");
    }
}
