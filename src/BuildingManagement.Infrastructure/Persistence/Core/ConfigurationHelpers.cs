using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

internal static class ConfigurationHelpers
{
    internal const string Schema = "bms";

    internal static void Entity<T>(EntityTypeBuilder<T> builder, string table, string schema = Schema) where T : Entity
    {
        builder.ToTable(table, schema, tableBuilder =>
            tableBuilder.HasCheckConstraint($"CK_{table}_CodeFormat",
                "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.Code).HasColumnType("varchar(5)").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.IsActive);
    }

    internal static void Reference<T>(EntityTypeBuilder<T> builder, string table, string schema = Schema)
        where T : ReferenceDataItem
    {
        builder.ToTable(table, schema, tableBuilder =>
            tableBuilder.HasCheckConstraint($"CK_{table}_KeyFormat",
                "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.Key).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Title).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SortOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.SortOrder });
    }

}
