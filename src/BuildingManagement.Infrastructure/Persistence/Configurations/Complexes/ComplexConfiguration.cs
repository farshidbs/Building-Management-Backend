using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class ComplexConfiguration : IEntityTypeConfiguration<Complex>
{
    public void Configure(EntityTypeBuilder<Complex> builder)
    {
        ConfigurationHelpers.Entity(builder, "Complexes");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.LocationId);
    }
}
