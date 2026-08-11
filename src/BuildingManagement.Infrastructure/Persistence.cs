using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManagement.Infrastructure;

public sealed class BuildingManagementDbContext(DbContextOptions<BuildingManagementDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<LocationType> LocationTypes => Set<LocationType>();
    public DbSet<BuildingType> BuildingTypes => Set<BuildingType>();
    public DbSet<UnitUsageType> UnitUsageTypes => Set<UnitUsageType>();
    public DbSet<UnitStatus> UnitStatuses => Set<UnitStatus>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<PartyType> PartyTypes => Set<PartyType>();
    public DbSet<PartyContactType> PartyContactTypes => Set<PartyContactType>();
    public DbSet<UnitPartyRelationType> UnitPartyRelationTypes => Set<UnitPartyRelationType>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Complex> Complexes => Set<Complex>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();
    public DbSet<BuildingGalleryFile> BuildingGalleryFiles => Set<BuildingGalleryFile>();
    public DbSet<ComplexGalleryFile> ComplexGalleryFiles => Set<ComplexGalleryFile>();
    public DbSet<BuildingDocument> BuildingDocuments => Set<BuildingDocument>();
    public DbSet<ComplexDocument> ComplexDocuments => Set<ComplexDocument>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<PartyContact> PartyContacts => Set<PartyContact>();
    public DbSet<UnitPartyRelation> UnitPartyRelations => Set<UnitPartyRelation>();
    public DbSet<UnitOccupancyHistory> UnitOccupancyHistories => Set<UnitOccupancyHistory>();
    public DbSet<AssetType> AssetTypes => Set<AssetType>();
    public DbSet<AssetEventType> AssetEventTypes => Set<AssetEventType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();
    public DbSet<AssetGalleryFile> AssetGalleryFiles => Set<AssetGalleryFile>();
    public DbSet<AssetDocument> AssetDocuments => Set<AssetDocument>();
    public DbSet<AssetEventFile> AssetEventFiles => Set<AssetEventFile>();

    public async Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (Database.CurrentTransaction is not null)
            return await operation(cancellationToken);
        var strategy = Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BuildingManagementDbContext).Assembly);
}

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BuildingManagementDbContext>(options => options.UseSqlServer(connectionString,
            sql => sql.MigrationsAssembly(typeof(BuildingManagementDbContext).Assembly.FullName).EnableRetryOnFailure()));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<BuildingManagementDbContext>());
        return services;
    }
}

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

internal sealed class LocationTypeConfiguration : IEntityTypeConfiguration<LocationType>
{
    public void Configure(EntityTypeBuilder<LocationType> builder)
    {
        ConfigurationHelpers.Reference(builder, "LocationTypes");
        builder.HasOne<LocationType>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ParentId);
    }
}

internal sealed class BuildingTypeConfiguration : IEntityTypeConfiguration<BuildingType>
{
    public void Configure(EntityTypeBuilder<BuildingType> builder) =>
        ConfigurationHelpers.Reference(builder, "BuildingTypes");
}

internal sealed class UnitUsageTypeConfiguration : IEntityTypeConfiguration<UnitUsageType>
{
    public void Configure(EntityTypeBuilder<UnitUsageType> builder) =>
        ConfigurationHelpers.Reference(builder, "UnitUsageTypes");
}

internal sealed class UnitStatusConfiguration : IEntityTypeConfiguration<UnitStatus>
{
    public void Configure(EntityTypeBuilder<UnitStatus> builder) =>
        ConfigurationHelpers.Reference(builder, "UnitStatuses");
}

internal sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        ConfigurationHelpers.Entity(builder, "Locations");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LocationType>().WithMany().HasForeignKey(x => x.LocationTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ParentId, x.LocationTypeId, x.NormalizedName }).IsUnique();
        builder.HasIndex(x => x.ParentId);
        builder.HasIndex(x => x.LocationTypeId);
        builder.ToTable(table => table.HasCheckConstraint("CK_Locations_NotSelfParent", "[ParentId] IS NULL OR [ParentId] <> [Id]"));
    }
}

internal sealed class ComplexConfiguration : IEntityTypeConfiguration<Complex>
{
    public void Configure(EntityTypeBuilder<Complex> builder)
    {
        ConfigurationHelpers.Entity(builder, "Complexes");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.LocationId);
    }
}

internal sealed class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        ConfigurationHelpers.Entity(builder, "Buildings");
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Complex>().WithMany().HasForeignKey(x => x.ComplexId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BuildingType>().WithMany().HasForeignKey(x => x.BuildingTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.LocationId);
        builder.HasIndex(x => x.ComplexId);
        builder.HasIndex(x => x.BuildingTypeId);
        builder.ToTable(table => table.HasCheckConstraint("CK_Buildings_Floors", "[FloorsCount] IS NULL OR [FloorsCount] >= 0"));
    }
}

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        ConfigurationHelpers.Entity(builder, "Units");
        builder.Property(x => x.UnitNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NormalizedUnitNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Area).HasPrecision(12, 2);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasOne<Building>().WithMany().HasForeignKey(x => x.BuildingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitUsageType>().WithMany().HasForeignKey(x => x.UsageTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitStatus>().WithMany().HasForeignKey(x => x.StatusId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.BuildingId, x.NormalizedUnitNumber }).IsUnique();
        builder.HasIndex(x => new { x.BuildingId, x.FloorNumber });
        builder.HasIndex(x => x.UsageTypeId);
        builder.HasIndex(x => x.StatusId);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Units_Area", "[Area] IS NULL OR [Area] >= 0");
            table.HasCheckConstraint("CK_Units_Counts",
                "([RoomsCount] IS NULL OR [RoomsCount] >= 0) AND [ParkingCount] >= 0 AND [StorageCount] >= 0 AND [CurrentOccupantsCount] >= 0");
        });
    }
}
