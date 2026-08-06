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

internal sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        ConfigurationHelpers.Reference(builder, "DocumentTypes");
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}

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

internal sealed class BuildingDocumentConfiguration : IEntityTypeConfiguration<BuildingDocument>
{
    public void Configure(EntityTypeBuilder<BuildingDocument> builder)
    {
        ConfigurationHelpers.Entity(builder, "BuildingDocuments");
        ConfigureDocument(builder);
        builder.HasOne<Building>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BuildingId, x.StoredFileId }).IsUnique();
        builder.HasIndex(x => new { x.BuildingId, x.IsActive, x.DocumentTypeId });
    }

    private static void ConfigureDocument(EntityTypeBuilder<BuildingDocument> builder)
    {
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DocumentDate).HasPrecision(0);
        builder.Property(x => x.EffectiveFrom).HasPrecision(0);
        builder.Property(x => x.ExpiresAt).HasPrecision(0);
    }
}

internal sealed class ComplexDocumentConfiguration : IEntityTypeConfiguration<ComplexDocument>
{
    public void Configure(EntityTypeBuilder<ComplexDocument> builder)
    {
        ConfigurationHelpers.Entity(builder, "ComplexDocuments");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DocumentNumber).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.DocumentDate).HasPrecision(0);
        builder.Property(x => x.EffectiveFrom).HasPrecision(0);
        builder.Property(x => x.ExpiresAt).HasPrecision(0);
        builder.HasOne<Complex>().WithMany().HasForeignKey(x => x.ComplexId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ComplexId, x.StoredFileId }).IsUnique();
        builder.HasIndex(x => new { x.ComplexId, x.IsActive, x.DocumentTypeId });
    }
}
