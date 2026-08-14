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
    public DbSet<FinancialAccount> FinancialAccounts => Set<FinancialAccount>();
    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();
    public DbSet<FinancialTransactionEntry> FinancialTransactionEntries => Set<FinancialTransactionEntry>();
    public DbSet<ExpenseType> ExpenseTypes => Set<ExpenseType>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ExpenseDocument> ExpenseDocuments => Set<ExpenseDocument>();
    public DbSet<ExpenseDisbursement> ExpenseDisbursements => Set<ExpenseDisbursement>();
    public DbSet<ExpenseDisbursementFile> ExpenseDisbursementFiles => Set<ExpenseDisbursementFile>();
    public DbSet<ExpenseAsset> ExpenseAssets => Set<ExpenseAsset>();
    public DbSet<ExpenseAssetEvent> ExpenseAssetEvents => Set<ExpenseAssetEvent>();
    public DbSet<ExpenseBuilding> ExpenseBuildings => Set<ExpenseBuilding>();
    public DbSet<ExpenseComplex> ExpenseComplexes => Set<ExpenseComplex>();
    public DbSet<DemandType> DemandTypes => Set<DemandType>();
    public DbSet<Demand> Demands => Set<Demand>();
    public DbSet<DemandAllocationRule> DemandAllocationRules => Set<DemandAllocationRule>();
    public DbSet<DemandAllocation> DemandAllocations => Set<DemandAllocation>();
    public DbSet<DemandExpense> DemandExpenses => Set<DemandExpense>();
    public DbSet<DemandAsset> DemandAssets => Set<DemandAsset>();
    public DbSet<DemandAssetEvent> DemandAssetEvents => Set<DemandAssetEvent>();
    public DbSet<DemandBuilding> DemandBuildings => Set<DemandBuilding>();
    public DbSet<DemandComplex> DemandComplexes => Set<DemandComplex>();
    public DbSet<UnitReceivable> UnitReceivables => Set<UnitReceivable>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<PaymentEvidenceFile> PaymentEvidenceFiles => Set<PaymentEvidenceFile>();
    public DbSet<AccountAdjustment> AccountAdjustments => Set<AccountAdjustment>();
    public DbSet<UnitCreditSettlement> UnitCreditSettlements => Set<UnitCreditSettlement>();
    public DbSet<UnitCreditSettlementAllocation> UnitCreditSettlementAllocations => Set<UnitCreditSettlementAllocation>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserLoginMethod> UserLoginMethods => Set<UserLoginMethod>();
    public DbSet<UserPartyLink> UserPartyLinks => Set<UserPartyLink>();
    public DbSet<PartyAffiliation> PartyAffiliations => Set<PartyAffiliation>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    public DbSet<AuthSession> AuthSessions => Set<AuthSession>();
    public DbSet<AuthRefreshToken> AuthRefreshTokens => Set<AuthRefreshToken>();
    public DbSet<AccessRole> AccessRoles => Set<AccessRole>();
    public DbSet<AccessCapability> AccessPermissions => Set<AccessCapability>();
    public DbSet<AccessRoleCapability> AccessRolePermissions => Set<AccessRoleCapability>();
    public DbSet<RoleAllowedScope> RoleAllowedScopes => Set<RoleAllowedScope>();
    public DbSet<AccessMembership> AccessMemberships => Set<AccessMembership>();
    public DbSet<AccessGrant> AccessGrants => Set<AccessGrant>();
    public DbSet<BuildingAccessSetting> BuildingAccessSettings => Set<BuildingAccessSetting>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<MembershipExitRequest> MembershipExitRequests => Set<MembershipExitRequest>();
    public DbSet<IdentityConflictReview> IdentityConflictReviews => Set<IdentityConflictReview>();
    public DbSet<AccountRecoveryCase> AccountRecoveryCases => Set<AccountRecoveryCase>();
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<PlatformCapability> PlatformPermissions => Set<PlatformCapability>();
    public DbSet<PlatformRolePermissionLink> PlatformRolePermissions => Set<PlatformRolePermissionLink>();
    public DbSet<PlatformUserRoleLink> PlatformUserRoles => Set<PlatformUserRoleLink>();
    public DbSet<SupportActingSession> SupportActingSessions => Set<SupportActingSession>();
    public DbSet<SecurityAuditEvent> SecurityAuditEvents => Set<SecurityAuditEvent>();

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
