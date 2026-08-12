using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class PartyTypeConfiguration : IEntityTypeConfiguration<PartyType>
{
    public void Configure(EntityTypeBuilder<PartyType> builder)
    {
        ConfigurationHelpers.Reference(builder, "PartyTypes");
    }
}
