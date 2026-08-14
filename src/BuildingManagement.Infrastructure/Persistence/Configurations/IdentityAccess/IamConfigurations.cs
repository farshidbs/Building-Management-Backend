using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b) { ConfigurationHelpers.Entity(b, "Users"); b.Property(x => x.StatusKey).HasColumnType("varchar(40)").IsRequired(); b.HasIndex(x => x.StatusKey); }
}
internal sealed class UserLoginMethodConfiguration : IEntityTypeConfiguration<UserLoginMethod>
{
    public void Configure(EntityTypeBuilder<UserLoginMethod> b) { ConfigurationHelpers.Entity(b, "UserLoginMethods"); b.Property(x => x.LoginTypeKey).HasColumnType("varchar(30)").IsRequired(); b.Property(x => x.IdentifierValue).HasMaxLength(200).IsRequired(); b.Property(x => x.NormalizedIdentifierValue).HasMaxLength(200).IsRequired(); b.Property(x => x.StatusKey).HasColumnType("varchar(30)").IsRequired(); b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.LoginTypeKey, x.NormalizedIdentifierValue }).IsUnique().HasFilter("[IsActive] = 1 AND [StatusKey] = 'active'"); b.HasIndex(x => new { x.UserId, x.IsPrimary }).IsUnique().HasFilter("[IsActive] = 1 AND [IsPrimary] = 1"); }
}
internal sealed class UserPartyLinkConfiguration : IEntityTypeConfiguration<UserPartyLink>
{
    public void Configure(EntityTypeBuilder<UserPartyLink> b) { ConfigurationHelpers.Entity(b, "UserPartyLinks"); b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Party>().WithMany().HasForeignKey(x => x.PartyId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.PartyId).IsUnique().HasFilter("[IsActive] = 1"); b.HasIndex(x => new { x.UserId, x.IsPrimary }).IsUnique().HasFilter("[IsActive] = 1 AND [IsPrimary] = 1"); }
}
internal sealed class PartyAffiliationConfiguration : IEntityTypeConfiguration<PartyAffiliation>
{
    public void Configure(EntityTypeBuilder<PartyAffiliation> b) { ConfigurationHelpers.Entity(b, "PartyAffiliations"); b.Property(x => x.AffiliationTypeKey).HasColumnType("varchar(50)").IsRequired(); b.Property(x => x.Title).HasMaxLength(150); b.HasOne<Party>().WithMany().HasForeignKey(x => x.PersonPartyId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Party>().WithMany().HasForeignKey(x => x.OrganizationPartyId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.PersonPartyId, x.OrganizationPartyId, x.AffiliationTypeKey }); }
}
internal sealed class OtpChallengeConfiguration : IEntityTypeConfiguration<OtpChallenge>
{
    public void Configure(EntityTypeBuilder<OtpChallenge> b) { b.ToTable("OtpChallenges", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.Property(x => x.LoginTypeKey).HasColumnType("varchar(30)"); b.Property(x => x.IdentifierValue).HasMaxLength(200); b.Property(x => x.NormalizedIdentifierValue).HasMaxLength(200); b.Property(x => x.PurposeKey).HasColumnType("varchar(40)"); b.Property(x => x.CodeHash).HasMaxLength(128); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.HasIndex(x => new { x.NormalizedIdentifierValue, x.PurposeKey, x.StatusKey }); }
}
internal sealed class AuthSessionConfiguration : IEntityTypeConfiguration<AuthSession>
{
    public void Configure(EntityTypeBuilder<AuthSession> b) { ConfigurationHelpers.Entity(b, "AuthSessions"); b.ToTable("AuthSessions", "bms", t => t.HasCheckConstraint("CK_AuthSessions_OneActor", "(CASE WHEN [UserId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [PlatformUserId] IS NULL THEN 0 ELSE 1 END) = 1")); b.Property(x => x.ClientTypeKey).HasColumnType("varchar(30)"); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.Property(x => x.AccessTokenHash).HasMaxLength(128).IsRequired(); b.HasIndex(x => x.AccessTokenHash).IsUnique(); b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.PlatformUserId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.UserId, x.StatusKey }); }
}
internal sealed class AuthRefreshTokenConfiguration : IEntityTypeConfiguration<AuthRefreshToken>
{
    public void Configure(EntityTypeBuilder<AuthRefreshToken> b) { b.ToTable("AuthRefreshTokens", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.Property(x => x.TokenHash).HasMaxLength(128).IsRequired(); b.HasIndex(x => x.TokenHash).IsUnique(); b.HasOne<AuthSession>().WithMany().HasForeignKey(x => x.AuthSessionId).OnDelete(DeleteBehavior.Cascade); }
}
internal sealed class AccessRoleConfiguration : IEntityTypeConfiguration<AccessRole>
{
    public void Configure(EntityTypeBuilder<AccessRole> b)
    {
        ConfigurationHelpers.Reference(b, "AccessRoles"); b.Property(x => x.Description).HasMaxLength(500); var at = new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero); b.HasData(
    new { Id = 1L, Key = "building_manager", Title = "مدیر ساختمان", SortOrder = 10, IsActive = true, CreatedAtUtc = at, IsSystemRole = true },
    new { Id = 2L, Key = "accountant", Title = "حسابدار", SortOrder = 20, IsActive = true, CreatedAtUtc = at, IsSystemRole = true },
    new { Id = 3L, Key = "owner", Title = "مالک", SortOrder = 30, IsActive = true, CreatedAtUtc = at, IsSystemRole = true },
    new { Id = 4L, Key = "tenant", Title = "مستأجر", SortOrder = 40, IsActive = true, CreatedAtUtc = at, IsSystemRole = true },
    new { Id = 5L, Key = "resident", Title = "ساکن", SortOrder = 50, IsActive = true, CreatedAtUtc = at, IsSystemRole = true });
    }
}
internal sealed class AccessPermissionConfiguration : IEntityTypeConfiguration<AccessCapability>
{
    public void Configure(EntityTypeBuilder<AccessCapability> b)
    {
        ConfigurationHelpers.Reference(b, "Permissions"); b.Property(x => x.CategoryKey).HasColumnType("varchar(50)"); b.Property(x => x.Description).HasMaxLength(500); var at = new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.Zero); b.HasData(
    Seed(1, "building_read", "مشاهده ساختمان", "physical", 10, at), Seed(2, "building_manage", "مدیریت ساختمان", "physical", 20, at),
    Seed(3, "unit_read", "مشاهده واحد", "physical", 30, at), Seed(4, "unit_manage", "مدیریت واحد", "physical", 40, at),
    Seed(5, "party_read", "مشاهده اشخاص", "party", 50, at), Seed(6, "party_manage", "مدیریت اشخاص", "party", 60, at),
    Seed(7, "file_read", "مشاهده فایل", "files", 70, at), Seed(8, "file_manage", "مدیریت فایل", "files", 80, at),
    Seed(9, "asset_read", "مشاهده دارایی", "assets", 90, at), Seed(10, "asset_manage", "مدیریت دارایی", "assets", 100, at),
    Seed(11, "finance_read", "مشاهده مالی", "finance", 110, at), Seed(12, "finance_manage", "مدیریت مالی", "finance", 120, at),
    Seed(13, "membership_manage", "مدیریت دسترسی‌ها", "iam", 130, at), Seed(14, "invitation_manage", "مدیریت دعوت‌ها", "iam", 140, at));
    }
    private static object Seed(long id, string key, string title, string category, int order, DateTimeOffset at) => new { Id = id, Key = key, Title = title, CategoryKey = category, SortOrder = order, IsActive = true, CreatedAtUtc = at };
}
internal sealed class AccessRolePermissionConfiguration : IEntityTypeConfiguration<AccessRoleCapability> { public void Configure(EntityTypeBuilder<AccessRoleCapability> b) { b.ToTable("RolePermissions", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.Property(x => x.EffectKey).HasColumnType("varchar(10)"); b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique(); b.HasOne<AccessRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade); b.HasOne<AccessCapability>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade); } }
internal sealed class RoleAllowedScopeConfiguration : IEntityTypeConfiguration<RoleAllowedScope> { public void Configure(EntityTypeBuilder<RoleAllowedScope> b) { b.ToTable("RoleAllowedScopes", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.Property(x => x.ScopeKindKey).HasColumnType("varchar(20)"); b.HasIndex(x => new { x.RoleId, x.ScopeKindKey }).IsUnique(); } }
internal sealed class AccessMembershipConfiguration : IEntityTypeConfiguration<AccessMembership>
{
    public void Configure(EntityTypeBuilder<AccessMembership> b) { ConfigurationHelpers.Entity(b, "AccessMemberships"); b.ToTable("AccessMemberships", "bms", t => t.HasCheckConstraint("CK_AccessMemberships_OneScope", "(CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END) = 1")); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<AccessRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.UserId, x.RoleId, x.ComplexId, x.BuildingId, x.UnitId }).IsUnique().HasFilter("[IsActive] = 1 AND [EndsAtUtc] IS NULL"); }
}
internal sealed class AccessGrantConfiguration : IEntityTypeConfiguration<AccessGrant> { public void Configure(EntityTypeBuilder<AccessGrant> b) { ConfigurationHelpers.Entity(b, "AccessGrants"); b.ToTable("AccessGrants", "bms", t => t.HasCheckConstraint("CK_AccessGrants_OneScope", "(CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END) = 1")); b.Property(x => x.Reason).HasMaxLength(500); b.HasIndex(x => new { x.UserId, x.PermissionId, x.IsActive }); } }
internal sealed class BuildingAccessSettingConfiguration : IEntityTypeConfiguration<BuildingAccessSetting> { public void Configure(EntityTypeBuilder<BuildingAccessSetting> b) { ConfigurationHelpers.Entity(b, "BuildingAccessSettings"); b.Property(x => x.UnitVisibilityKey).HasColumnType("varchar(30)"); b.Property(x => x.FinancialVisibilityKey).HasColumnType("varchar(30)"); b.HasIndex(x => x.BuildingId).IsUnique(); } }
internal sealed class InvitationConfiguration : IEntityTypeConfiguration<Invitation> { public void Configure(EntityTypeBuilder<Invitation> b) { ConfigurationHelpers.Entity(b, "Invitations"); b.ToTable("Invitations", "bms", t => t.HasCheckConstraint("CK_Invitations_OneScope", "(CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END) = 1")); b.Property(x => x.TokenHash).HasMaxLength(128); b.Property(x => x.NormalizedIdentifierValue).HasMaxLength(200); b.HasIndex(x => x.TokenHash).IsUnique(); } }
internal sealed class MembershipExitRequestConfiguration : IEntityTypeConfiguration<MembershipExitRequest> { public void Configure(EntityTypeBuilder<MembershipExitRequest> b) { ConfigurationHelpers.Entity(b, "MembershipExitRequests"); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.Property(x => x.Reason).HasMaxLength(500); } }
internal sealed class IdentityConflictReviewConfiguration : IEntityTypeConfiguration<IdentityConflictReview> { public void Configure(EntityTypeBuilder<IdentityConflictReview> b) { ConfigurationHelpers.Entity(b, "IdentityConflictReviews"); b.Property(x => x.ConflictTypeKey).HasColumnType("varchar(50)"); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.Property(x => x.Details).HasMaxLength(1000); } }
internal sealed class AccountRecoveryCaseConfiguration : IEntityTypeConfiguration<AccountRecoveryCase> { public void Configure(EntityTypeBuilder<AccountRecoveryCase> b) { ConfigurationHelpers.Entity(b, "AccountRecoveryCases"); b.Property(x => x.RecoveryTypeKey).HasColumnType("varchar(40)"); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.Property(x => x.Reason).HasMaxLength(1000); } }
internal sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser> { public void Configure(EntityTypeBuilder<PlatformUser> b) { ConfigurationHelpers.Entity(b, "PlatformUsers"); b.Property(x => x.Username).HasMaxLength(100); b.Property(x => x.NormalizedUsername).HasMaxLength(100); b.Property(x => x.PasswordHash).HasMaxLength(500); b.Property(x => x.StatusKey).HasColumnType("varchar(30)"); b.HasIndex(x => x.NormalizedUsername).IsUnique(); } }
internal sealed class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole> { public void Configure(EntityTypeBuilder<PlatformRole> b) => ConfigurationHelpers.Reference(b, "PlatformRoles"); }
internal sealed class PlatformPermissionConfiguration : IEntityTypeConfiguration<PlatformCapability> { public void Configure(EntityTypeBuilder<PlatformCapability> b) => ConfigurationHelpers.Reference(b, "PlatformPermissions"); }
internal sealed class PlatformRolePermissionConfiguration : IEntityTypeConfiguration<PlatformRolePermissionLink> { public void Configure(EntityTypeBuilder<PlatformRolePermissionLink> b) { b.ToTable("PlatformRolePermissions", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique(); } }
internal sealed class PlatformUserRoleConfiguration : IEntityTypeConfiguration<PlatformUserRoleLink> { public void Configure(EntityTypeBuilder<PlatformUserRoleLink> b) { b.ToTable("PlatformUserRoles", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.HasIndex(x => new { x.PlatformUserId, x.RoleId }).IsUnique(); } }
internal sealed class SupportActingSessionConfiguration : IEntityTypeConfiguration<SupportActingSession> { public void Configure(EntityTypeBuilder<SupportActingSession> b) { ConfigurationHelpers.Entity(b, "SupportActingSessions"); b.Property(x => x.Reason).HasMaxLength(1000); } }
internal sealed class SecurityAuditEventConfiguration : IEntityTypeConfiguration<SecurityAuditEvent> { public void Configure(EntityTypeBuilder<SecurityAuditEvent> b) { b.ToTable("SecurityAuditEvents", "bms"); b.HasKey(x => x.Id); b.Property(x => x.Id).UseIdentityColumn(); b.Property(x => x.EventTypeKey).HasColumnType("varchar(80)"); b.Property(x => x.ResourceKindKey).HasColumnType("varchar(50)"); b.Property(x => x.ResourceCode).HasMaxLength(100); b.Property(x => x.Reason).HasMaxLength(1000); b.Property(x => x.IpAddress).HasMaxLength(64); b.HasIndex(x => new { x.UserId, x.OccurredAtUtc }); } }
internal sealed class BuildingRolePermissionOverrideConfiguration : IEntityTypeConfiguration<BuildingRolePermissionOverride> { public void Configure(EntityTypeBuilder<BuildingRolePermissionOverride> b) { ConfigurationHelpers.Entity(b, "BuildingRolePermissionOverrides"); b.Property(x => x.EffectKey).HasColumnType("varchar(10)"); b.HasIndex(x => new { x.BuildingId, x.RoleId, x.PermissionId }).IsUnique(); } }
