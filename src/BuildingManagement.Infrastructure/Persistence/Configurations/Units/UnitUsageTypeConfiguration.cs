using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class UnitUsageTypeConfiguration : IEntityTypeConfiguration<UnitUsageType>
{
    public void Configure(EntityTypeBuilder<UnitUsageType> builder) =>
        ConfigurationHelpers.Reference(builder, "UnitUsageTypes");
}
