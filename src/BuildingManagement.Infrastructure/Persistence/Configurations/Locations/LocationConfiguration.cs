using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        ConfigurationHelpers.Entity(builder, "Locations");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LocationType>().WithMany().HasForeignKey(x => x.LocationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ParentId, x.LocationTypeId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => x.ParentId);
        builder.HasIndex(x => x.LocationTypeId);
        builder.ToTable(table => table.HasCheckConstraint("CK_Locations_NotSelfParent", "[ParentId] IS NULL OR [ParentId] <> [Id]"));
    }
}
