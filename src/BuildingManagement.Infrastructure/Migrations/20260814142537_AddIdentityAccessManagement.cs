using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814, CA1861 // Generated EF Core seed arrays.

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
                    table.ForeignKey(
                        name: "FK_BuildingAccessSettings_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OtpChallenges",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                    table.ForeignKey(
                        name: "FK_RoleAllowedScopes_AccessRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_BuildingRolePermissionOverrides_AccessRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildingRolePermissionOverrides_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildingRolePermissionOverrides_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "bms",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "bms",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_PlatformRolePermissions_PlatformPermissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "bms",
                        principalTable: "PlatformPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformRolePermissions_PlatformRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_PlatformUserRoles_PlatformRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "PlatformRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlatformUserRoles_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "bms",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                    table.ForeignKey(
                        name: "FK_AccessGrants_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "bms",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessGrants_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                        name: "FK_AccessMemberships_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessMemberships_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessMemberships_UnitPartyRelations_SourceUnitPartyRelationId",
                        column: x => x.SourceUnitPartyRelationId,
                        principalSchema: "bms",
                        principalTable: "UnitPartyRelations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccessMemberships_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
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
                    table.ForeignKey(
                        name: "FK_AccountRecoveryCases_PlatformUsers_ResolvedByPlatformUserId",
                        column: x => x.ResolvedByPlatformUserId,
                        principalSchema: "bms",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountRecoveryCases_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_IdentityConflictReviews_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IdentityConflictReviews_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_Invitations_AccessRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "bms",
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitations_Users_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_SupportActingSessions_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "bms",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupportActingSessions_Users_TargetUserId",
                        column: x => x.TargetUserId,
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
                    table.ForeignKey(
                        name: "FK_MembershipExitRequests_AccessMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalSchema: "bms",
                        principalTable: "AccessMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipExitRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MembershipExitRequests_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
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
                    AccessTokenExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
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
                        name: "FK_AuthSessions_AccessRoles_ActiveRoleId",
                        column: x => x.ActiveRoleId,
                        principalSchema: "bms",
                        principalTable: "AccessRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Buildings_ActiveBuildingId",
                        column: x => x.ActiveBuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthSessions_Complexes_ActiveComplexId",
                        column: x => x.ActiveComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthSessions_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "bms",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthSessions_UserLoginMethods_AuthenticatedViaLoginMethodId",
                        column: x => x.AuthenticatedViaLoginMethodId,
                        principalSchema: "bms",
                        principalTable: "UserLoginMethods",
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
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthRefreshTokens_AuthRefreshTokens_ReplacedByRefreshTokenId",
                        column: x => x.ReplacedByRefreshTokenId,
                        principalSchema: "bms",
                        principalTable: "AuthRefreshTokens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuthRefreshTokens_AuthSessions_AuthSessionId",
                        column: x => x.AuthSessionId,
                        principalSchema: "bms",
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    table.ForeignKey(
                        name: "FK_SecurityAuditEvents_AuthSessions_AuthSessionId",
                        column: x => x.AuthSessionId,
                        principalSchema: "bms",
                        principalTable: "AuthSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEvents_PlatformUsers_PlatformUserId",
                        column: x => x.PlatformUserId,
                        principalSchema: "bms",
                        principalTable: "PlatformUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEvents_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "bms",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "AccessRoles",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsSystemRole", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "building_manager", 10, "مدیر ساختمان", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "accountant", 20, "حسابدار", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "unit_owner", 30, "مالک واحد", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "unit_tenant", 40, "مستأجر واحد", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "unit_resident", 50, "ساکن واحد", null },
                    { 6L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "complex_manager", 5, "مدیر مجتمع", null },
                    { 7L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "manager_assistant", 15, "دستیار مدیر", null },
                    { 8L, new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, "unit_representative", 60, "نماینده واحد", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "Permissions",
                columns: new[] { "Id", "CategoryKey", "CreatedAtUtc", "Description", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 15L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "complex_view", 150, "مشاهده مجتمع", null },
                    { 16L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "complex_manage", 160, "مدیریت مجتمع", null },
                    { 17L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "building_view", 170, "مشاهده ساختمان", null },
                    { 18L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "building_manage", 180, "مدیریت ساختمان", null },
                    { 19L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "unit_view", 190, "مشاهده واحد", null },
                    { 20L, "physical", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "unit_manage", 200, "مدیریت واحد", null },
                    { 21L, "party", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "party_view", 210, "مشاهده اشخاص", null },
                    { 22L, "party", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "party_manage", 220, "مدیریت اشخاص", null },
                    { 23L, "party", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "occupancy_view", 230, "مشاهده سکونت", null },
                    { 24L, "party", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "occupancy_manage", 240, "مدیریت سکونت", null },
                    { 25L, "assets", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "asset_view", 250, "مشاهده دارایی", null },
                    { 26L, "assets", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "asset_manage", 260, "مدیریت دارایی", null },
                    { 27L, "assets", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "asset_event_view", 270, "مشاهده رویداد دارایی", null },
                    { 28L, "assets", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "asset_event_manage", 280, "مدیریت رویداد دارایی", null },
                    { 29L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "expense_view", 290, "مشاهده هزینه", null },
                    { 30L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "expense_create", 300, "ایجاد هزینه", null },
                    { 31L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "expense_update", 310, "ویرایش هزینه", null },
                    { 32L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "expense_finalize", 320, "نهایی‌سازی هزینه", null },
                    { 33L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "demand_view", 330, "مشاهده مطالبه", null },
                    { 34L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "demand_create", 340, "ایجاد مطالبه", null },
                    { 35L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "demand_update", 350, "ویرایش مطالبه", null },
                    { 36L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "demand_finalize", 360, "نهایی‌سازی مطالبه", null },
                    { 37L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "payment_view", 370, "مشاهده پرداخت", null },
                    { 38L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "payment_submit", 380, "ثبت پرداخت", null },
                    { 39L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "payment_confirm", 390, "تأیید پرداخت", null },
                    { 40L, "files", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "file_read", 400, "مشاهده فایل", null },
                    { 41L, "files", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "file_read_confidential", 410, "مشاهده فایل محرمانه", null },
                    { 42L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "membership_view", 420, "مشاهده عضویت", null },
                    { 43L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "membership_manage_scoped", 430, "مدیریت عضویت", null },
                    { 44L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "invitation_send", 440, "ارسال دعوت", null },
                    { 45L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "invitation_revoke", 450, "لغو دعوت", null },
                    { 46L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "access_grant_view", 460, "مشاهده دسترسی تفویضی", null },
                    { 47L, "iam", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "access_grant_manage", 470, "مدیریت دسترسی تفویضی", null },
                    { 48L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "financial_unit_view_own", 480, "مشاهده مالی واحد خود", null },
                    { 49L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "financial_unit_view_other_summary", 490, "مشاهده خلاصه مالی سایر واحدها", null },
                    { 50L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "financial_unit_view_other_detail", 500, "مشاهده جزئیات مالی سایر واحدها", null },
                    { 51L, "finance", new DateTimeOffset(new DateTime(2026, 8, 14, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "financial_unit_pay", 510, "پرداخت برای واحد", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "RoleAllowedScopes",
                columns: new[] { "Id", "RoleId", "ScopeKindKey" },
                values: new object[,]
                {
                    { 1L, 6L, "complex" },
                    { 2L, 1L, "building" },
                    { 3L, 7L, "building" },
                    { 4L, 2L, "building" },
                    { 5L, 3L, "unit" },
                    { 6L, 4L, "unit" },
                    { 7L, 5L, "unit" },
                    { 8L, 8L, "unit" }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "RolePermissions",
                columns: new[] { "Id", "EffectKey", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1L, "allow", 15L, 6L },
                    { 2L, "allow", 16L, 6L },
                    { 3L, "allow", 17L, 6L },
                    { 4L, "allow", 18L, 6L },
                    { 5L, "allow", 19L, 6L },
                    { 6L, "allow", 20L, 6L },
                    { 7L, "allow", 21L, 6L },
                    { 8L, "allow", 22L, 6L },
                    { 9L, "allow", 23L, 6L },
                    { 10L, "allow", 24L, 6L },
                    { 11L, "allow", 25L, 6L },
                    { 12L, "allow", 26L, 6L },
                    { 13L, "allow", 27L, 6L },
                    { 14L, "allow", 28L, 6L },
                    { 15L, "allow", 29L, 6L },
                    { 16L, "allow", 30L, 6L },
                    { 17L, "allow", 31L, 6L },
                    { 18L, "allow", 32L, 6L },
                    { 19L, "allow", 33L, 6L },
                    { 20L, "allow", 34L, 6L },
                    { 21L, "allow", 35L, 6L },
                    { 22L, "allow", 36L, 6L },
                    { 23L, "allow", 37L, 6L },
                    { 24L, "allow", 38L, 6L },
                    { 25L, "allow", 39L, 6L },
                    { 26L, "allow", 40L, 6L },
                    { 27L, "allow", 41L, 6L },
                    { 28L, "allow", 42L, 6L },
                    { 29L, "allow", 43L, 6L },
                    { 30L, "allow", 44L, 6L },
                    { 31L, "allow", 45L, 6L },
                    { 32L, "allow", 46L, 6L },
                    { 33L, "allow", 47L, 6L },
                    { 34L, "allow", 48L, 6L },
                    { 35L, "allow", 49L, 6L },
                    { 36L, "allow", 50L, 6L },
                    { 37L, "allow", 51L, 6L },
                    { 38L, "allow", 17L, 1L },
                    { 39L, "allow", 18L, 1L },
                    { 40L, "allow", 19L, 1L },
                    { 41L, "allow", 20L, 1L },
                    { 42L, "allow", 21L, 1L },
                    { 43L, "allow", 22L, 1L },
                    { 44L, "allow", 23L, 1L },
                    { 45L, "allow", 24L, 1L },
                    { 46L, "allow", 25L, 1L },
                    { 47L, "allow", 26L, 1L },
                    { 48L, "allow", 27L, 1L },
                    { 49L, "allow", 28L, 1L },
                    { 50L, "allow", 29L, 1L },
                    { 51L, "allow", 30L, 1L },
                    { 52L, "allow", 31L, 1L },
                    { 53L, "allow", 32L, 1L },
                    { 54L, "allow", 33L, 1L },
                    { 55L, "allow", 34L, 1L },
                    { 56L, "allow", 35L, 1L },
                    { 57L, "allow", 36L, 1L },
                    { 58L, "allow", 37L, 1L },
                    { 59L, "allow", 38L, 1L },
                    { 60L, "allow", 39L, 1L },
                    { 61L, "allow", 40L, 1L },
                    { 62L, "allow", 41L, 1L },
                    { 63L, "allow", 42L, 1L },
                    { 64L, "allow", 43L, 1L },
                    { 65L, "allow", 44L, 1L },
                    { 66L, "allow", 45L, 1L },
                    { 67L, "allow", 46L, 1L },
                    { 68L, "allow", 47L, 1L },
                    { 69L, "allow", 48L, 1L },
                    { 70L, "allow", 49L, 1L },
                    { 71L, "allow", 50L, 1L },
                    { 72L, "allow", 51L, 1L },
                    { 73L, "allow", 17L, 7L },
                    { 74L, "allow", 19L, 7L },
                    { 75L, "allow", 21L, 7L },
                    { 76L, "allow", 23L, 7L },
                    { 77L, "allow", 25L, 7L },
                    { 78L, "allow", 27L, 7L },
                    { 79L, "allow", 29L, 7L },
                    { 80L, "allow", 30L, 7L },
                    { 81L, "allow", 31L, 7L },
                    { 82L, "allow", 33L, 7L },
                    { 83L, "allow", 34L, 7L },
                    { 84L, "allow", 35L, 7L },
                    { 85L, "allow", 37L, 7L },
                    { 86L, "allow", 38L, 7L },
                    { 87L, "allow", 40L, 7L },
                    { 88L, "allow", 42L, 7L },
                    { 89L, "allow", 44L, 7L },
                    { 90L, "allow", 46L, 7L },
                    { 91L, "allow", 48L, 7L },
                    { 92L, "allow", 17L, 2L },
                    { 93L, "allow", 19L, 2L },
                    { 94L, "allow", 21L, 2L },
                    { 95L, "allow", 23L, 2L },
                    { 96L, "allow", 25L, 2L },
                    { 97L, "allow", 27L, 2L },
                    { 98L, "allow", 29L, 2L },
                    { 99L, "allow", 30L, 2L },
                    { 100L, "allow", 31L, 2L },
                    { 101L, "allow", 32L, 2L },
                    { 102L, "allow", 33L, 2L },
                    { 103L, "allow", 34L, 2L },
                    { 104L, "allow", 35L, 2L },
                    { 105L, "allow", 36L, 2L },
                    { 106L, "allow", 37L, 2L },
                    { 107L, "allow", 38L, 2L },
                    { 108L, "allow", 39L, 2L },
                    { 109L, "allow", 40L, 2L },
                    { 110L, "allow", 48L, 2L },
                    { 111L, "allow", 49L, 2L },
                    { 112L, "allow", 50L, 2L },
                    { 113L, "allow", 51L, 2L },
                    { 114L, "allow", 17L, 3L },
                    { 115L, "allow", 19L, 3L },
                    { 116L, "allow", 21L, 3L },
                    { 117L, "allow", 23L, 3L },
                    { 118L, "allow", 25L, 3L },
                    { 119L, "allow", 27L, 3L },
                    { 120L, "allow", 29L, 3L },
                    { 121L, "allow", 33L, 3L },
                    { 122L, "allow", 37L, 3L },
                    { 123L, "allow", 38L, 3L },
                    { 124L, "allow", 40L, 3L },
                    { 125L, "allow", 48L, 3L },
                    { 126L, "allow", 51L, 3L },
                    { 127L, "allow", 17L, 4L },
                    { 128L, "allow", 19L, 4L },
                    { 129L, "allow", 23L, 4L },
                    { 130L, "allow", 25L, 4L },
                    { 131L, "allow", 27L, 4L },
                    { 132L, "allow", 29L, 4L },
                    { 133L, "allow", 33L, 4L },
                    { 134L, "allow", 37L, 4L },
                    { 135L, "allow", 38L, 4L },
                    { 136L, "allow", 40L, 4L },
                    { 137L, "allow", 48L, 4L },
                    { 138L, "allow", 51L, 4L },
                    { 139L, "allow", 17L, 5L },
                    { 140L, "allow", 19L, 5L },
                    { 141L, "allow", 23L, 5L },
                    { 142L, "allow", 25L, 5L },
                    { 143L, "allow", 27L, 5L },
                    { 144L, "allow", 29L, 5L },
                    { 145L, "allow", 33L, 5L },
                    { 146L, "allow", 37L, 5L },
                    { 147L, "allow", 40L, 5L },
                    { 148L, "allow", 48L, 5L },
                    { 149L, "allow", 17L, 8L },
                    { 150L, "allow", 19L, 8L },
                    { 151L, "allow", 23L, 8L },
                    { 152L, "allow", 25L, 8L },
                    { 153L, "allow", 27L, 8L },
                    { 154L, "allow", 29L, 8L },
                    { 155L, "allow", 33L, 8L },
                    { 156L, "allow", 37L, 8L },
                    { 157L, "allow", 38L, 8L },
                    { 158L, "allow", 40L, 8L },
                    { 159L, "allow", 48L, 8L },
                    { 160L, "allow", 51L, 8L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_BuildingId",
                schema: "bms",
                table: "AccessGrants",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_Code",
                schema: "bms",
                table: "AccessGrants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_ComplexId",
                schema: "bms",
                table: "AccessGrants",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_GrantedByUserId",
                schema: "bms",
                table: "AccessGrants",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_IsActive",
                schema: "bms",
                table: "AccessGrants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_PermissionId",
                schema: "bms",
                table: "AccessGrants",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UnitId",
                schema: "bms",
                table: "AccessGrants",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UserId_PermissionId_IsActive",
                schema: "bms",
                table: "AccessGrants",
                columns: new[] { "UserId", "PermissionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_BuildingId",
                schema: "bms",
                table: "AccessMemberships",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_Code",
                schema: "bms",
                table: "AccessMemberships",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_ComplexId",
                schema: "bms",
                table: "AccessMemberships",
                column: "ComplexId");

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
                name: "IX_AccessMemberships_SourceUnitPartyRelationId",
                schema: "bms",
                table: "AccessMemberships",
                column: "SourceUnitPartyRelationId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessMemberships_UnitId",
                schema: "bms",
                table: "AccessMemberships",
                column: "UnitId");

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
                name: "IX_AccountRecoveryCases_ResolvedByPlatformUserId",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "ResolvedByPlatformUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCases_UserId",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshTokens_AuthSessionId",
                schema: "bms",
                table: "AuthRefreshTokens",
                column: "AuthSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthRefreshTokens_ReplacedByRefreshTokenId",
                schema: "bms",
                table: "AuthRefreshTokens",
                column: "ReplacedByRefreshTokenId");

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
                name: "IX_AuthSessions_ActiveBuildingId",
                schema: "bms",
                table: "AuthSessions",
                column: "ActiveBuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_ActiveComplexId",
                schema: "bms",
                table: "AuthSessions",
                column: "ActiveComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_ActiveRoleId",
                schema: "bms",
                table: "AuthSessions",
                column: "ActiveRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AuthSessions_AuthenticatedViaLoginMethodId",
                schema: "bms",
                table: "AuthSessions",
                column: "AuthenticatedViaLoginMethodId");

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
                name: "IX_BuildingRolePermissionOverrides_PermissionId",
                schema: "bms",
                table: "BuildingRolePermissionOverrides",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingRolePermissionOverrides_RoleId",
                schema: "bms",
                table: "BuildingRolePermissionOverrides",
                column: "RoleId");

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
                name: "IX_IdentityConflictReviews_PartyId",
                schema: "bms",
                table: "IdentityConflictReviews",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityConflictReviews_UserId",
                schema: "bms",
                table: "IdentityConflictReviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_BuildingId",
                schema: "bms",
                table: "Invitations",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_Code",
                schema: "bms",
                table: "Invitations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_ComplexId",
                schema: "bms",
                table: "Invitations",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_InvitedByUserId",
                schema: "bms",
                table: "Invitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_IsActive",
                schema: "bms",
                table: "Invitations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_RoleId",
                schema: "bms",
                table: "Invitations",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TokenHash",
                schema: "bms",
                table: "Invitations",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UnitId",
                schema: "bms",
                table: "Invitations",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_Code",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_DecidedByUserId",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_IsActive",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_MembershipId",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_RequestedByUserId",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_NormalizedIdentifierValue_PurposeKey_StatusKey",
                schema: "bms",
                table: "OtpChallenges",
                columns: new[] { "NormalizedIdentifierValue", "PurposeKey", "StatusKey" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpChallenges_PublicReference",
                schema: "bms",
                table: "OtpChallenges",
                column: "PublicReference",
                unique: true);

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
                name: "IX_PlatformRolePermissions_PermissionId",
                schema: "bms",
                table: "PlatformRolePermissions",
                column: "PermissionId");

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
                name: "IX_PlatformUserRoles_RoleId",
                schema: "bms",
                table: "PlatformUserRoles",
                column: "RoleId");

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
                name: "IX_SecurityAuditEvents_AuthSessionId",
                schema: "bms",
                table: "SecurityAuditEvents",
                column: "AuthSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEvents_PlatformUserId",
                schema: "bms",
                table: "SecurityAuditEvents",
                column: "PlatformUserId");

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
                name: "IX_SupportActingSessions_PlatformUserId",
                schema: "bms",
                table: "SupportActingSessions",
                column: "PlatformUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportActingSessions_TargetUserId",
                schema: "bms",
                table: "SupportActingSessions",
                column: "TargetUserId");

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
                name: "PlatformRolePermissions",
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
                name: "UserPartyLinks",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AccessMemberships",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformPermissions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformRoles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AuthSessions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AccessRoles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PlatformUsers",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UserLoginMethods",
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
