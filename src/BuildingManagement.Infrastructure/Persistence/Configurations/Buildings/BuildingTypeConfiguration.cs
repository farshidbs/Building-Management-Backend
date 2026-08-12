using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class BuildingTypeConfiguration : IEntityTypeConfiguration<BuildingType>
{
    public void Configure(EntityTypeBuilder<BuildingType> builder) =>
        ConfigurationHelpers.Reference(builder, "BuildingTypes");
}
