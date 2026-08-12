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

    public void Detach(object entity) => Entry(entity).State = EntityState.Detached;

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
