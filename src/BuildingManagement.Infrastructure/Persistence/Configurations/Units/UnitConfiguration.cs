using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        ConfigurationHelpers.Entity(builder, "Units");
        builder.Property(x => x.UnitNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NormalizedUnitNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Area).HasPrecision(12, 2);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Building>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitUsageType>().WithMany().HasForeignKey(x => x.UsageTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitStatus>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BuildingId, x.NormalizedUnitNumber }).IsUnique();
        builder.HasIndex(x => new { x.BuildingId, x.FloorNumber });
        builder.HasIndex(x => x.UsageTypeId);
        builder.HasIndex(x => x.StatusId);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Units_Area", "[Area] IS NULL OR [Area] >= 0");
            table.HasCheckConstraint("CK_Units_Counts",
                "([RoomsCount] IS NULL OR [RoomsCount] >= 0) AND [ParkingCount] >= 0 AND [StorageCount] >= 0 AND [CurrentOccupantsCount] >= 0");
        });
    }
}
