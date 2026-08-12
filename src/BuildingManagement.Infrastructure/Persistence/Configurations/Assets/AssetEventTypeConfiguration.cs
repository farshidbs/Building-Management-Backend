using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class AssetEventTypeConfiguration : IEntityTypeConfiguration<AssetEventType>
{
    public void Configure(EntityTypeBuilder<AssetEventType> builder) => ConfigurationHelpers.Reference(builder, "AssetEventTypes");
}
