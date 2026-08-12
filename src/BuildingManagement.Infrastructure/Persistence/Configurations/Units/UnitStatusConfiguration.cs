using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal sealed class UnitStatusConfiguration : IEntityTypeConfiguration<UnitStatus>
{
    public void Configure(EntityTypeBuilder<UnitStatus> builder) =>
        ConfigurationHelpers.Reference(builder, "UnitStatuses");
}
