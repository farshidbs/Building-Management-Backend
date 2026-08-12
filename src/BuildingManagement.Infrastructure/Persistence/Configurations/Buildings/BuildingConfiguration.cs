using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        ConfigurationHelpers.Entity(builder, "Buildings");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Complex>().WithMany().HasForeignKey(x => x.ComplexId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BuildingType>().WithMany().HasForeignKey(x => x.BuildingTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.ComplexId);
        builder.HasIndex(x => x.BuildingTypeId);
        builder.ToTable(table => table.HasCheckConstraint("CK_Buildings_Floors", "[FloorsCount] IS NULL OR [FloorsCount] >= 0"));
    }
}
