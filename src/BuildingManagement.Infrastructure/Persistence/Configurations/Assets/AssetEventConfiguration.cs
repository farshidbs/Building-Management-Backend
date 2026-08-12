using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

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
