using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class LocationTypeConfiguration : IEntityTypeConfiguration<LocationType>
{
    public void Configure(EntityTypeBuilder<LocationType> builder)
    {
        ConfigurationHelpers.Reference(builder, "LocationTypes");
        builder.HasOne<LocationType>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ParentId);
    }
}
