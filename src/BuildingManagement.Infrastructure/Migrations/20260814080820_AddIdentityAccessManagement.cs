using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // EF Core generated multidimensional seed arrays.
#pragma warning disable CA1861 // EF Core generated repeated column-name arrays.

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAccessManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDate",
                schema: "bms",
                table: "Parties",
                type: "date",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccessGrants",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    GrantedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    UnitId = table.Column<long>(type: "bigint", nullable: true),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessGrants", x => x.Id);
                    table.CheckConstraint("CK_AccessGrants_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_AccessGrants_OneScope", "(CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END) = 1");
                });

            migrationBuilder.CreateTable(
                name: "AccessRoles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    Key = table.Column<string>(type: "varchar(50)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRoles", x => x.Id);
                    table.CheckConstraint("CK_AccessRoles_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "AccountRecoveryCases",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    RecoveryTypeKey = table.Column<string>(type: "varchar(40)", nullable: false),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    NewNormalizedIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByPlatformUserId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRecoveryCases", x => x.Id);
                    table.CheckConstraint("CK_AccountRecoveryCases_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "BuildingAccessSettings",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false),
                    UnitVisibilityKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    FinancialVisibilityKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingAccessSettings", x => x.Id);
                    table.CheckConstraint("CK_BuildingAccessSettings_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "BuildingRolePermissionOverrides",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    EffectKey = table.Column<string>(type: "varchar(10)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingRolePermissionOverrides", x => x.Id);
                    table.CheckConstraint("CK_BuildingRolePermissionOverrides_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "IdentityConflictReviews",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    PartyId = table.Column<long>(type: "bigint", nullable: true),
                    ConflictTypeKey = table.Column<string>(type: "varchar(50)", nullable: false),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityConflictReviews", x => x.Id);
                    table.CheckConstraint("CK_IdentityConflictReviews_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "Invitations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NormalizedIdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    UnitId = table.Column<long>(type: "bigint", nullable: true),
                    InvitedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AcceptedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitations", x => x.Id);
                    table.CheckConstraint("CK_Invitations_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_Invitations_OneScope", "(CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END) = 1");
                });

            migrationBuilder.CreateTable(
                name: "MembershipExitRequests",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MembershipId = table.Column<long>(type: "bigint", nullable: false),
                    RequestedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecidedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembershipExitRequests", x => x.Id);
                    table.CheckConstraint("CK_MembershipExitRequests_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "OtpChallenges",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    IdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedIdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PurposeKey = table.Column<string>(type: "varchar(40)", nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SentAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PartyAffiliations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PersonPartyId = table.Column<long>(type: "bigint", nullable: false),
                    OrganizationPartyId = table.Column<long>(type: "bigint", nullable: false),
                    AffiliationTypeKey = table.Column<string>(type: "varchar(50)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyAffiliations", x => x.Id);
                    table.CheckConstraint("CK_PartyAffiliations_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_PartyAffiliations_Parties_OrganizationPartyId",
                        column: x => x.OrganizationPartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyAffiliations_Parties_PersonPartyId",
                        column: x => x.PersonPartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryKey = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Key = table.Column<string>(type: "varchar(50)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                    table.CheckConstraint("CK_Permissions_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "PlatformPermissions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "varchar(50)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformPermissions", x => x.Id);
                    table.CheckConstraint("CK_PlatformPermissions_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "PlatformRolePermissions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRolePermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformRoles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "varchar(50)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformRoles", x => x.Id);
                    table.CheckConstraint("CK_PlatformRoles_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "PlatformUserRoles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlatformUserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUserRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformUsers",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NormalizedUsername = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    FailedLoginCount = table.Column<int>(type: "int", nullable: false),
                    LockedUntilUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformUsers", x => x.Id);
                    table.CheckConstraint("CK_PlatformUsers_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "RoleAllowedScopes",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ScopeKindKey = table.Column<string>(type: "varchar(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleAllowedScopes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditEvents",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    PlatformUserId = table.Column<long>(type: "bigint", nullable: true),
                    AuthSessionId = table.Column<long>(type: "bigint", nullable: true),
                    EventTypeKey = table.Column<string>(type: "varchar(80)", nullable: false),
                    ResourceKindKey = table.Column<string>(type: "varchar(50)", nullable: true),
                    ResourceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SupportActingSessions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlatformUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportActingSessions", x => x.Id);
                    table.CheckConstraint("CK_SupportActingSessions_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StatusKey = table.Column<string>(type: "varchar(40)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    PermissionId = table.Column<long>(type: "bigint", nullable: false),
                    EffectKey = table.Column<string>(type: "varchar(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_AccessRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "bms",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccessMemberships",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    UnitId = table.Column<long>(type: "bigint", nullable: true),
                    SourceUnitPartyRelationId = table.Column<long>(type: "bigint", nullable: true),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessMemberships", x => x.Id);
                    table.CheckConstraint("CK_AccessMemberships_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_AccessMemberships_OneScope", "(CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_AccessMemberships_AccessRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessMemberships_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthSessions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: true),
                    PlatformUserId = table.Column<long>(type: "bigint", nullable: true),
                    AuthenticatedViaLoginMethodId = table.Column<long>(type: "bigint", nullable: true),
                    ClientTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    ActiveComplexId = table.Column<long>(type: "bigint", nullable: true),
                    ActiveBuildingId = table.Column<long>(type: "bigint", nullable: true),
                    ActiveRoleId = table.Column<long>(type: "bigint", nullable: true),
                    AccessTokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AbsoluteExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IdleExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokeReasonKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeviceIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthSessions", x => x.Id);
                    table.CheckConstraint("CK_AuthSessions_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_AuthSessions_OneActor", "(CASE WHEN [UserId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [PlatformUserId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_AuthSessions_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "bms",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginMethods",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    IdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedIdentifierValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProviderSubject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleasedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReleaseReasonKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginMethods", x => x.Id);
                    table.CheckConstraint("CK_UserLoginMethods_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_UserLoginMethods_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserPartyLinks",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    LinkedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UnlinkedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPartyLinks", x => x.Id);
                    table.CheckConstraint("CK_UserPartyLinks_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_UserPartyLinks_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserPartyLinks_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuthRefreshTokens",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthSessionId = table.Column<long>(type: "bigint", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplacedByRefreshTokenId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthRefreshTokens_AuthSessions_AuthSessionId",
                        column: x => x.AuthSessionId,
                        principalSchema: "bms",
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "AccessRoles",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsSystemRole", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "building_manager", 10, "مدیر ساختمان", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "accountant", 20, "حسابدار", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "owner", 30, "مالک", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "tenant", 40, "مستأجر", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "resident", 50, "ساکن", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "Permissions",
                columns: new[] { "Id", "CategoryKey", "CreatedAtUtc", "Description", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "building_read", 10, "مشاهده ساختمان", null },
                    { 2L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "building_manage", 20, "مدیریت ساختمان", null },
                    { 3L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "unit_read", 30, "مشاهده واحد", null },
                    { 4L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "unit_manage", 40, "مدیریت واحد", null },
                    { 5L, "party", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "party_read", 50, "مشاهده اشخاص", null },
                    { 6L, "party", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "party_manage", 60, "مدیریت اشخاص", null },
                    { 7L, "files", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "file_read", 70, "مشاهده فایل", null },
                    { 8L, "files", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "file_manage", 80, "مدیریت فایل", null },
                    { 9L, "assets", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "asset_read", 90, "مشاهده دارایی", null },
                    { 10L, "assets", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "asset_manage", 100, "مدیریت دارایی", null },
                    { 11L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "finance_read", 110, "مشاهده مالی", null },
                    { 12L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "finance_manage", 120, "مدیریت مالی", null },
                    { 13L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "membership_manage", 130, "مدیریت دسترسی‌ها", null },
                    { 14L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "invitation_manage", 140, "مدیریت دعوت‌ها", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_Code",
                schema: "bms",
                table: "AccessGrants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_IsActive",
                schema: "bms",
                table: "AccessGrants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UserId_PermissionId_IsActive",
                schema: "bms",
                table: "AccessGrants",
                columns: new[] { "UserId", "PermissionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_Code",
                schema: "bms",
                table: "AccessMemberships",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_IsActive",
                schema: "bms",
                table: "AccessMemberships",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_RoleId",
                schema: "bms",
                table: "AccessMemberships",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_UserId_RoleId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessMemberships",
                columns: new[] { "UserId", "RoleId", "ComplexId", "BuildingId", "UnitId" },
                unique: true,
                filter: "[IsActive] = 1 AND [EndsAtUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccessRoles_IsActive_SortOrder",
                schema: "bms",
                table: "AccessRoles",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessRoles_Key",
                schema: "bms",
                table: "AccessRoles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCases_Code",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCases_IsActive",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshTokens_AuthSessionId",
                schema: "bms",
                table: "AuthRefreshTokens",
                column: "AuthSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshTokens_TokenHash",
                schema: "bms",
                table: "AuthRefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_AccessTokenHash",
                schema: "bms",
                table: "AuthSessions",
                column: "AccessTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_Code",
                schema: "bms",
                table: "AuthSessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_IsActive",
                schema: "bms",
                table: "AuthSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_PlatformUserId",
                schema: "bms",
                table: "AuthSessions",
                column: "PlatformUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_UserId_StatusKey",
                schema: "bms",
                table: "AuthSessions",
                columns: new[] { "UserId", "StatusKey" });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessSettings_BuildingId",
                schema: "bms",
                table: "BuildingAccessSettings",
                column: "BuildingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessSettings_Code",
                schema: "bms",
                table: "BuildingAccessSettings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingAccessSettings_IsActive",
                schema: "bms",
                table: "BuildingAccessSettings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingRolePermissionOverrides_BuildingId_RoleId_PermissionId",
                schema: "bms",
                table: "BuildingRolePermissionOverrides",
                columns: new[] { "BuildingId", "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingRolePermissionOverrides_Code",
                schema: "bms",
                table: "BuildingRolePermissionOverrides",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingRolePermissionOverrides_IsActive",
                schema: "bms",
                table: "BuildingRolePermissionOverrides",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityConflictReviews_Code",
                schema: "bms",
                table: "IdentityConflictReviews",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IdentityConflictReviews_IsActive",
                schema: "bms",
                table: "IdentityConflictReviews",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Code",
                schema: "bms",
                table: "Invitations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_IsActive",
                schema: "bms",
                table: "Invitations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TokenHash",
                schema: "bms",
                table: "Invitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_Code",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_IsActive",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_NormalizedIdentifierValue_PurposeKey_StatusKey",
                schema: "bms",
                table: "OtpChallenges",
                columns: new[] { "NormalizedIdentifierValue", "PurposeKey", "StatusKey" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyAffiliations_Code",
                schema: "bms",
                table: "PartyAffiliations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyAffiliations_IsActive",
                schema: "bms",
                table: "PartyAffiliations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAffiliations_OrganizationPartyId",
                schema: "bms",
                table: "PartyAffiliations",
                column: "OrganizationPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyAffiliations_PersonPartyId_OrganizationPartyId_AffiliationTypeKey",
                schema: "bms",
                table: "PartyAffiliations",
                columns: new[] { "PersonPartyId", "OrganizationPartyId", "AffiliationTypeKey" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsActive_SortOrder",
                schema: "bms",
                table: "Permissions",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Key",
                schema: "bms",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPermissions_IsActive_SortOrder",
                schema: "bms",
                table: "PlatformPermissions",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformPermissions_Key",
                schema: "bms",
                table: "PlatformPermissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRolePermissions_RoleId_PermissionId",
                schema: "bms",
                table: "PlatformRolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_IsActive_SortOrder",
                schema: "bms",
                table: "PlatformRoles",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformRoles_Key",
                schema: "bms",
                table: "PlatformRoles",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUserRoles_PlatformUserId_RoleId",
                schema: "bms",
                table: "PlatformUserRoles",
                columns: new[] { "PlatformUserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_Code",
                schema: "bms",
                table: "PlatformUsers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_IsActive",
                schema: "bms",
                table: "PlatformUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PlatformUsers_NormalizedUsername",
                schema: "bms",
                table: "PlatformUsers",
                column: "NormalizedUsername",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAllowedScopes_RoleId_ScopeKindKey",
                schema: "bms",
                table: "RoleAllowedScopes",
                columns: new[] { "RoleId", "ScopeKindKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "bms",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "bms",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_UserId_OccurredAtUtc",
                schema: "bms",
                table: "SecurityAuditEvents",
                columns: new[] { "UserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportActingSessions_Code",
                schema: "bms",
                table: "SupportActingSessions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportActingSessions_IsActive",
                schema: "bms",
                table: "SupportActingSessions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginMethods_Code",
                schema: "bms",
                table: "UserLoginMethods",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginMethods_IsActive",
                schema: "bms",
                table: "UserLoginMethods",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginMethods_LoginTypeKey_NormalizedIdentifierValue",
                schema: "bms",
                table: "UserLoginMethods",
                columns: new[] { "LoginTypeKey", "NormalizedIdentifierValue" },
                unique: true,
                filter: "[IsActive] = 1 AND [StatusKey] = 'active'");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginMethods_UserId_IsPrimary",
                schema: "bms",
                table: "UserLoginMethods",
                columns: new[] { "UserId", "IsPrimary" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserPartyLinks_Code",
                schema: "bms",
                table: "UserPartyLinks",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPartyLinks_IsActive",
                schema: "bms",
                table: "UserPartyLinks",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserPartyLinks_PartyId",
                schema: "bms",
                table: "UserPartyLinks",
                column: "PartyId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_UserPartyLinks_UserId_IsPrimary",
                schema: "bms",
                table: "UserPartyLinks",
                columns: new[] { "UserId", "IsPrimary" },
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Code",
                schema: "bms",
                table: "Users",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_IsActive",
                schema: "bms",
                table: "Users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Users_StatusKey",
                schema: "bms",
                table: "Users",
                column: "StatusKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessGrants",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AccessMemberships",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AccountRecoveryCases",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AuthRefreshTokens",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "BuildingAccessSettings",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "BuildingRolePermissionOverrides",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "IdentityConflictReviews",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Invitations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "MembershipExitRequests",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "OtpChallenges",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PartyAffiliations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformPermissions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformRolePermissions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformRoles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformUserRoles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "RoleAllowedScopes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "SecurityAuditEvents",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "SupportActingSessions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UserLoginMethods",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UserPartyLinks",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AuthSessions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AccessRoles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformUsers",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "bms");

            migrationBuilder.DropColumn(
                name: "BirthDate",
                schema: "bms",
                table: "Parties");
        }
    }
}
