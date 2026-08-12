using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class UnitOccupancyHistoryConfiguration : IEntityTypeConfiguration<UnitOccupancyHistory>
{
    public void Configure(EntityTypeBuilder<UnitOccupancyHistory> builder)
    {
        builder.ToTable("UnitOccupancyHistories", ConfigurationHelpers.Schema, table =>
        {
            table.HasCheckConstraint("CK_UnitOccupancyHistories_Count", "[OccupantsCount] >= 0");
            table.HasCheckConstraint("CK_UnitOccupancyHistories_Dates",
                "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.EffectiveFrom).HasPrecision(0);
        builder.Property(x => x.EffectiveTo).HasPrecision(0);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.UnitId, x.EffectiveFrom });
        builder.HasIndex(x => x.UnitId).IsUnique()
            .HasFilter("[IsActive] = CAST(1 AS bit) AND [EffectiveTo] IS NULL");
    }
}
