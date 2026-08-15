using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public interface IApplicationDbContext
{
    DbSet<LocationType> LocationTypes { get; }
    DbSet<BuildingType> BuildingTypes { get; }
    DbSet<UnitUsageType> UnitUsageTypes { get; }
    DbSet<UnitStatus> UnitStatuses { get; }
    DbSet<DocumentType> DocumentTypes { get; }
    DbSet<PartyType> PartyTypes { get; }
    DbSet<PartyContactType> PartyContactTypes { get; }
    DbSet<UnitPartyRelationType> UnitPartyRelationTypes { get; }
    DbSet<Location> Locations { get; }
    DbSet<Complex> Complexes { get; }
    DbSet<Building> Buildings { get; }
    DbSet<Unit> Units { get; }
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<BuildingGalleryFile> BuildingGalleryFiles { get; }
    DbSet<ComplexGalleryFile> ComplexGalleryFiles { get; }
    DbSet<BuildingDocument> BuildingDocuments { get; }
    DbSet<ComplexDocument> ComplexDocuments { get; }
    DbSet<Party> Parties { get; }
    DbSet<PartyContact> PartyContacts { get; }
    DbSet<UnitPartyRelation> UnitPartyRelations { get; }
    DbSet<UnitOccupancyHistory> UnitOccupancyHistories { get; }
    DbSet<AssetType> AssetTypes { get; }
    DbSet<AssetEventType> AssetEventTypes { get; }
    DbSet<Asset> Assets { get; }
    DbSet<AssetEvent> AssetEvents { get; }
    DbSet<AssetGalleryFile> AssetGalleryFiles { get; }
    DbSet<AssetDocument> AssetDocuments { get; }
    DbSet<AssetEventFile> AssetEventFiles { get; }
    DbSet<FinancialAccount> FinancialAccounts { get; }
    DbSet<FinancialTransaction> FinancialTransactions { get; }
    DbSet<FinancialTransactionEntry> FinancialTransactionEntries { get; }
    DbSet<ExpenseType> ExpenseTypes { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<ExpenseDocument> ExpenseDocuments { get; }
    DbSet<ExpenseDisbursement> ExpenseDisbursements { get; }
    DbSet<ExpenseDisbursementFile> ExpenseDisbursementFiles { get; }
    DbSet<ExpenseAsset> ExpenseAssets { get; }
    DbSet<ExpenseAssetEvent> ExpenseAssetEvents { get; }
    DbSet<ExpenseBuilding> ExpenseBuildings { get; }
    DbSet<ExpenseComplex> ExpenseComplexes { get; }
    DbSet<DemandType> DemandTypes { get; }
    DbSet<Demand> Demands { get; }
    DbSet<DemandAllocationRule> DemandAllocationRules { get; }
    DbSet<DemandAllocation> DemandAllocations { get; }
    DbSet<DemandExpense> DemandExpenses { get; }
    DbSet<DemandAsset> DemandAssets { get; }
    DbSet<DemandAssetEvent> DemandAssetEvents { get; }
    DbSet<DemandBuilding> DemandBuildings { get; }
    DbSet<DemandComplex> DemandComplexes { get; }
    DbSet<UnitReceivable> UnitReceivables { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentAllocation> PaymentAllocations { get; }
    DbSet<PaymentEvidenceFile> PaymentEvidenceFiles { get; }
    DbSet<AccountAdjustment> AccountAdjustments { get; }
    DbSet<UnitCreditSettlement> UnitCreditSettlements { get; }
    DbSet<UnitCreditSettlementAllocation> UnitCreditSettlementAllocations { get; }
    DbSet<User> Users { get; }
    DbSet<UserLoginMethod> UserLoginMethods { get; }
    DbSet<UserPartyLink> UserPartyLinks { get; }
    DbSet<PartyAffiliation> PartyAffiliations { get; }
    DbSet<OtpChallenge> OtpChallenges { get; }
    DbSet<AuthSession> AuthSessions { get; }
    DbSet<AuthRefreshToken> AuthRefreshTokens { get; }
    DbSet<AccessRole> AccessRoles { get; }
    DbSet<AccessCapability> AccessPermissions { get; }
    DbSet<AccessRoleCapability> AccessRolePermissions { get; }
    DbSet<RoleAllowedScope> RoleAllowedScopes { get; }
    DbSet<AccessMembership> AccessMemberships { get; }
    DbSet<AccessGrant> AccessGrants { get; }
    DbSet<BuildingAccessSetting> BuildingAccessSettings { get; }
    DbSet<Invitation> Invitations { get; }
    DbSet<MembershipExitRequest> MembershipExitRequests { get; }
    DbSet<IdentityConflictReview> IdentityConflictReviews { get; }
    DbSet<AccountRecoveryCase> AccountRecoveryCases { get; }
    DbSet<PlatformUser> PlatformUsers { get; }
    DbSet<PlatformRole> PlatformRoles { get; }
    DbSet<PlatformCapability> PlatformPermissions { get; }
    DbSet<PlatformRolePermissionLink> PlatformRolePermissions { get; }
    DbSet<PlatformUserRoleLink> PlatformUserRoles { get; }
    DbSet<SupportActingSession> SupportActingSessions { get; }
    DbSet<SecurityAuditEvent> SecurityAuditEvents { get; }
    DbSet<BuildingRolePermissionOverride> BuildingRolePermissionOverrides { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> TryReserveOtpAttempt(string publicReference, DateTimeOffset now,
        CancellationToken cancellationToken);
    Task MarkOtpAttemptFailed(string publicReference, DateTimeOffset now,
        CancellationToken cancellationToken);
    void Detach(object entity);
    Task LockUserForFirstRoot(long userId, CancellationToken cancellationToken);
    Task LockUserForSecurityMutation(long userId, CancellationToken cancellationToken);
    Task LockSessionForSecurityMutation(long sessionId, CancellationToken cancellationToken);
    Task LockInvitation(string tokenHash, CancellationToken cancellationToken);
    bool IsUniqueViolation(Exception exception);
    Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed class AppException(int status, string code, string message, IDictionary<string, string[]>? errors = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public IDictionary<string, string[]>? Errors { get; } = errors;
    public static AppException NotFound(string resource) => new(404, $"{resource}.not_found", $"{resource} was not found.");
    public static AppException Conflict(string code, string message) => new(409, code, message);
}

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record PageQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string SortBy = "name", string SortDirection = "asc")
{
    public (int Number, int Size) Validated() =>
        PageNumber < 1 || PageSize is < 1 or > 100
            ? throw new AppException(400, "validation.failed", "Pagination values are invalid.",
                new Dictionary<string, string[]> { ["pagination"] = ["pageNumber must be at least 1 and pageSize must be between 1 and 100."] })
            : (PageNumber, PageSize);
}

public sealed record ReferenceValueResponse(string Key, string Title);
public sealed record ResourceReferenceResponse(string Code, string Name);
