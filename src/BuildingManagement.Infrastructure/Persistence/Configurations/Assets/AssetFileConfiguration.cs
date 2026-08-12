using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal static class AssetFileConfiguration
{
    internal static void Base<T>(EntityTypeBuilder<T> builder, string table) where T : AssetFileRelation
    {
        builder.ToTable(table, ConfigurationHelpers.Schema); builder.HasKey(x => x.Id); builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired(); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken(); builder.HasIndex(x => x.IsActive);
        builder.HasOne<StoredFile>().WithMany().HasForeignKey(x => x.StoredFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.StoredFileId);
    }
}
