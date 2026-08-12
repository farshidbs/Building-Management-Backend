using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        ConfigurationHelpers.Entity(builder, "StoredFiles", "base");
        builder.Property(x => x.StorageProvider).HasMaxLength(30).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150).IsRequired();
        builder.Property(x => x.FileExtension).HasMaxLength(20).IsRequired();
        builder.Property(x => x.StoredFileName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChecksumSha256).HasMaxLength(64);
        builder.HasIndex(x => new { x.StorageProvider, x.StorageKey }).IsUnique();
    }
}
