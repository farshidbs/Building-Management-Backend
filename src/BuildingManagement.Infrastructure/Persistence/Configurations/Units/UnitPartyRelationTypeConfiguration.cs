using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class UnitPartyRelationTypeConfiguration : IEntityTypeConfiguration<UnitPartyRelationType>
{
    public void Configure(EntityTypeBuilder<UnitPartyRelationType> builder)
    {
        ConfigurationHelpers.Reference(builder, "UnitPartyRelationTypes");
        builder.Property(x => x.Description).HasMaxLength(500);
    }
}
