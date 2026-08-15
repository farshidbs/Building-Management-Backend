using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Data.SqlClient;

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
    public DbSet<BuildingRolePermissionOverride> BuildingRolePermissionOverrides => Set<BuildingRolePermissionOverride>();

    public void Detach(object entity) => Entry(entity).State = EntityState.Detached;

    public async Task<bool> TryReserveOtpAttempt(string publicReference, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var reference = new SqlParameter("@reference", publicReference);
        var current = new SqlParameter("@now", now);
        var affected = await Database.ExecuteSqlRawAsync("""
            UPDATE [bms].[OtpChallenges] WITH (UPDLOCK, ROWLOCK)
            SET [AttemptCount] = [AttemptCount] + 1
            WHERE [PublicReference] = @reference
              AND [StatusKey] = 'pending'
              AND [ExpiresAtUtc] >= @now
              AND [AttemptCount] < [MaxAttempts]
            """, [reference, current], cancellationToken);
        return affected == 1;
    }

    public Task MarkOtpAttemptFailed(string publicReference, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var reference = new SqlParameter("@reference", publicReference);
        var current = new SqlParameter("@now", now);
        return Database.ExecuteSqlRawAsync("""
            UPDATE [bms].[OtpChallenges]
            SET [StatusKey] = CASE
                WHEN [ExpiresAtUtc] < @now THEN 'expired'
                WHEN [AttemptCount] >= [MaxAttempts] THEN 'blocked'
                ELSE [StatusKey]
            END
            WHERE [PublicReference] = @reference AND [StatusKey] = 'pending'
            """, [reference, current], cancellationToken);
    }

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
    public async Task LockUserForFirstRoot(long userId, CancellationToken cancellationToken)
        => await LockUserForSecurityMutation(userId, cancellationToken);

    public async Task LockUserForSecurityMutation(long userId, CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("The User security lock requires an active transaction.");
        _ = await Users.FromSqlInterpolated(
                $"SELECT * FROM [bms].[Users] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {userId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound("user");
    }

    public async Task LockSessionForSecurityMutation(long sessionId, CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("The Session security lock requires an active transaction.");
        _ = await AuthSessions.FromSqlInterpolated(
                $"SELECT * FROM [bms].[AuthSessions] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {sessionId}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound("session");
    }

    public async Task LockRecoveryCase(string referenceHash, CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("The Recovery Case lock requires an active transaction.");
        _ = await AccountRecoveryCases.FromSqlInterpolated(
                $"SELECT * FROM [bms].[AccountRecoveryCases] WITH (UPDLOCK, HOLDLOCK) WHERE [ReferenceHash] = {referenceHash}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound("recovery");
    }

    public async Task LockInvitation(string tokenHash, CancellationToken cancellationToken)
    {
        if (Database.CurrentTransaction is null)
            throw new InvalidOperationException("The Invitation lock requires an active transaction.");
        _ = await Invitations.FromSqlInterpolated(
                $"SELECT * FROM [bms].[Invitations] WITH (UPDLOCK, HOLDLOCK) WHERE [TokenHash] = {tokenHash}")
            .AsNoTracking().SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound("invitation");
    }

    public bool IsUniqueViolation(Exception exception) =>
        exception is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } };

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BuildingManagementDbContext).Assembly);
}
