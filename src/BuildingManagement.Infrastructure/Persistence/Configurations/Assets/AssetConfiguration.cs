using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

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
