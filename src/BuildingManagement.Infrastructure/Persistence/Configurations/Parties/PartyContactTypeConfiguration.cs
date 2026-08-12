using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyContactTypeConfiguration : IEntityTypeConfiguration<PartyContactType>
{
    public void Configure(EntityTypeBuilder<PartyContactType> builder) =>
        ConfigurationHelpers.Reference(builder, "PartyContactTypes");
}
