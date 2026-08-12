using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

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
