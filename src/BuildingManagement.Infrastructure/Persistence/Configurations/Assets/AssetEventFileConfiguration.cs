using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class AssetEventFileConfiguration : IEntityTypeConfiguration<AssetEventFile>
{
    public void Configure(EntityTypeBuilder<AssetEventFile> builder)
    {
        AssetFileConfiguration.Base(builder, "AssetEventFiles");
        builder.Property(x => x.Title).HasMaxLength(200); builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasOne<AssetEvent>().WithMany().HasForeignKey(x => x.AssetEventId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.AssetEventId);
    }
}
