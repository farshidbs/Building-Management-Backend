using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class AssetDocumentConfiguration : IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetDocument> builder)
    {
        AssetFileConfiguration.Base(builder, "AssetDocuments");
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired(); builder.Property(x => x.DocumentNumber).HasMaxLength(100);
        builder.Property(x => x.DocumentDate).HasPrecision(0); builder.Property(x => x.EffectiveFrom).HasPrecision(0);
        builder.Property(x => x.ExpiresAt).HasPrecision(0); builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DocumentType>().WithMany().HasForeignKey(x => x.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AssetId); builder.HasIndex(x => x.DocumentTypeId);
        builder.ToTable(table => table.HasCheckConstraint("CK_AssetDocuments_Dates", "[ExpiresAt] IS NULL OR [EffectiveFrom] IS NULL OR [ExpiresAt] >= [EffectiveFrom]"));
    }
}
