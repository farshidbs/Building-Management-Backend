using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class AssetTypeConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder) => ConfigurationHelpers.Reference(builder, "AssetTypes");
}

internal sealed class AssetEventTypeConfiguration : IEntityTypeConfiguration<AssetEventType>
{
    public void Configure(EntityTypeBuilder<AssetEventType> builder) => ConfigurationHelpers.Reference(builder, "AssetEventTypes");
}

internal sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        ConfigurationHelpers.Entity(builder, "Assets");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(100);
        builder.Property(x => x.Model).HasMaxLength(100);
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.InstallationDate).HasPrecision(0);
        builder.Property(x => x.PurchaseDate).HasPrecision(0);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<AssetType>().WithMany().HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Complex>().WithMany().HasForeignKey(x => x.ComplexId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Building>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AssetTypeId);
        builder.HasIndex(x => x.ComplexId);
        builder.HasIndex(x => x.BuildingId);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Assets_Scope", "([ComplexId] IS NOT NULL AND [BuildingId] IS NULL) OR ([ComplexId] IS NULL AND [BuildingId] IS NOT NULL)");
            table.HasCheckConstraint("CK_Assets_ReviewInterval", "[SuggestedReviewIntervalDays] IS NULL OR [SuggestedReviewIntervalDays] > 0");
        });
    }
}

internal sealed class AssetEventConfiguration : IEntityTypeConfiguration<AssetEvent>
{
    public void Configure(EntityTypeBuilder<AssetEvent> builder)
    {
        Child(builder, "AssetEvents");
        builder.Property(x => x.EventDate).HasPrecision(0).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.SuggestedNextDate).HasPrecision(0);
        builder.Property(x => x.Cost).HasPrecision(18, 2);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AssetEventType>().WithMany().HasForeignKey(x => x.AssetEventTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.ServiceProviderPartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AssetId, x.EventDate });
        builder.HasIndex(x => x.AssetEventTypeId);
        builder.HasIndex(x => x.ServiceProviderPartyId);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_AssetEvents_Dates", "[SuggestedNextDate] IS NULL OR [SuggestedNextDate] >= [EventDate]");
            table.HasCheckConstraint("CK_AssetEvents_Cost", "[Cost] IS NULL OR [Cost] >= 0");
        });
    }

    private static void Child(EntityTypeBuilder<AssetEvent> builder, string table)
    {
        builder.ToTable(table, ConfigurationHelpers.Schema); builder.HasKey(x => x.Id); builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired(); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => x.IsActive);
    }
}

internal static class AssetFileConfiguration
{
    internal static void Base<T>(EntityTypeBuilder<T> builder, string table) where T : AssetFileRelation
    {
        builder.ToTable(table, ConfigurationHelpers.Schema); builder.HasKey(x => x.Id); builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired(); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => x.IsActive);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StoredFileId);
    }
}

internal sealed class AssetGalleryFileConfiguration : IEntityTypeConfiguration<AssetGalleryFile>
{
    public void Configure(EntityTypeBuilder<AssetGalleryFile> builder)
    {
        AssetFileConfiguration.Base(builder, "AssetGalleryFiles");
        builder.Property(x => x.Title).HasMaxLength(200); builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.AltText).HasMaxLength(300);
        builder.HasOne<Asset>().WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AssetId, x.SortOrder });
        builder.HasIndex(x => x.AssetId).IsUnique().HasFilter("[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");
    }
}

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

internal sealed class AssetEventFileConfiguration : IEntityTypeConfiguration<AssetEventFile>
{
    public void Configure(EntityTypeBuilder<AssetEventFile> builder)
    {
        AssetFileConfiguration.Base(builder, "AssetEventFiles");
        builder.Property(x => x.Title).HasMaxLength(200); builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasOne<AssetEvent>().WithMany().HasForeignKey(x => x.AssetEventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AssetEventId);
    }
}
