using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration index column arrays.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartyAndUnitOccupancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_Counts",
                schema: "bms",
                table: "Units");

            migrationBuilder.AddColumn<int>(
                name: "CurrentOccupantsCount",
                schema: "bms",
                table: "Units",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PartyContactTypes",
                schema: "base",
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
                    table.PrimaryKey("PK_PartyContactTypes", x => x.Id);
                    table.CheckConstraint("CK_PartyContactTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "PartyIdentifierTypes",
                schema: "base",
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
                    table.PrimaryKey("PK_PartyIdentifierTypes", x => x.Id);
                    table.CheckConstraint("CK_PartyIdentifierTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "PartyTypes",
                schema: "base",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
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
                    table.PrimaryKey("PK_PartyTypes", x => x.Id);
                    table.CheckConstraint("CK_PartyTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "UnitOccupancyHistories",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<long>(type: "bigint", nullable: false),
                    OccupantsCount = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    EffectiveTo = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitOccupancyHistories", x => x.Id);
                    table.CheckConstraint("CK_UnitOccupancyHistories_Count", "[OccupantsCount] >= 0");
                    table.CheckConstraint("CK_UnitOccupancyHistories_Dates", "[EffectiveTo] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_UnitOccupancyHistories_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitPartyRelationTypes",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsOwnershipRelation = table.Column<bool>(type: "bit", nullable: false),
                    IsOccupancyRelation = table.Column<bool>(type: "bit", nullable: false),
                    CanBePaymentContact = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_UnitPartyRelationTypes", x => x.Id);
                    table.CheckConstraint("CK_UnitPartyRelationTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Parties",
                schema: "base",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartyTypeId = table.Column<long>(type: "bigint", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrganizationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parties", x => x.Id);
                    table.CheckConstraint("CK_Parties_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_Parties_PartyTypes_PartyTypeId",
                        column: x => x.PartyTypeId,
                        principalSchema: "base",
                        principalTable: "PartyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                schema: "base",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    PartyContactTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.Id);
                    table.CheckConstraint("CK_PartyContacts_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_PartyContacts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "base",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyContacts_PartyContactTypes_PartyContactTypeId",
                        column: x => x.PartyContactTypeId,
                        principalSchema: "base",
                        principalTable: "PartyContactTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyIdentifiers",
                schema: "base",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    PartyIdentifierTypeId = table.Column<long>(type: "bigint", nullable: false),
                    CountryCode = table.Column<string>(type: "char(2)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyIdentifiers", x => x.Id);
                    table.CheckConstraint("CK_PartyIdentifiers_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_PartyIdentifiers_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "base",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyIdentifiers_PartyIdentifierTypes_PartyIdentifierTypeId",
                        column: x => x.PartyIdentifierTypeId,
                        principalSchema: "base",
                        principalTable: "PartyIdentifierTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitPartyRelations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<long>(type: "bigint", nullable: false),
                    PartyId = table.Column<long>(type: "bigint", nullable: false),
                    UnitPartyRelationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    OwnershipShare = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IsPrimaryContact = table.Column<bool>(type: "bit", nullable: false),
                    IsPaymentContact = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitPartyRelations", x => x.Id);
                    table.CheckConstraint("CK_UnitPartyRelations_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_UnitPartyRelations_Dates", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.CheckConstraint("CK_UnitPartyRelations_OwnershipShare", "[OwnershipShare] IS NULL OR ([OwnershipShare] > 0 AND [OwnershipShare] <= 100)");
                    table.ForeignKey(
                        name: "FK_UnitPartyRelations_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "base",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitPartyRelations_UnitPartyRelationTypes_UnitPartyRelationTypeId",
                        column: x => x.UnitPartyRelationTypeId,
                        principalSchema: "bms",
                        principalTable: "UnitPartyRelationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitPartyRelations_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "base",
                table: "PartyContactTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "mobile", 10, "موبایل", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "phone", 20, "تلفن", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "email", 30, "ایمیل", null }
                });

            migrationBuilder.InsertData(
                schema: "base",
                table: "PartyIdentifierTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "national_id", 10, "کد ملی", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "legal_entity_national_id", 20, "شناسه ملی شخص حقوقی", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "passport_number", 30, "شماره گذرنامه", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "residence_identifier", 40, "شناسه اقامت", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "other", 50, "سایر", null }
                });

            migrationBuilder.InsertData(
                schema: "base",
                table: "PartyTypes",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "person", 10, "شخص", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "organization", 20, "سازمان", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "UnitPartyRelationTypes",
                columns: new[] { "Id", "CanBePaymentContact", "CreatedAtUtc", "Description", "IsActive", "IsOccupancyRelation", "IsOwnershipRelation", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, true, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, true, "owner", 10, "مالک", null },
                    { 2L, true, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, false, "tenant", 20, "مستأجر", null },
                    { 3L, false, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, false, "resident", 30, "ساکن", null },
                    { 4L, true, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, false, "legal_representative", 40, "نماینده قانونی", null },
                    { 5L, true, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, false, "contact_person", 50, "شخص رابط", null },
                    { 6L, false, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, false, "other", 60, "سایر", null }
                });

            migrationBuilder.UpdateData(
                schema: "bms",
                table: "UnitStatuses",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "IsActive", "Title" },
                values: new object[] { false, "در حال استفاده (قدیمی)" });

            migrationBuilder.UpdateData(
                schema: "bms",
                table: "UnitStatuses",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "IsActive", "Title" },
                values: new object[] { false, "خالی (قدیمی)" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_Counts",
                schema: "bms",
                table: "Units",
                sql: "([RoomsCount] IS NULL OR [RoomsCount] >= 0) AND [ParkingCount] >= 0 AND [StorageCount] >= 0 AND [CurrentOccupantsCount] >= 0");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Code",
                schema: "base",
                table: "Parties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_IsActive",
                schema: "base",
                table: "Parties",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_NormalizedDisplayName",
                schema: "base",
                table: "Parties",
                column: "NormalizedDisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_PartyTypeId",
                schema: "base",
                table: "Parties",
                column: "PartyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_Code",
                schema: "base",
                table: "PartyContacts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_IsActive",
                schema: "base",
                table: "PartyContacts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_NormalizedValue",
                schema: "base",
                table: "PartyContacts",
                column: "NormalizedValue");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyContactTypeId",
                schema: "base",
                table: "PartyContacts",
                column: "PartyContactTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId",
                schema: "base",
                table: "PartyContacts",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId_PartyContactTypeId",
                schema: "base",
                table: "PartyContacts",
                columns: new[] { "PartyId", "PartyContactTypeId" },
                unique: true,
                filter: "[IsActive] = CAST(1 AS bit) AND [IsPrimary] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContactTypes_IsActive_SortOrder",
                schema: "base",
                table: "PartyContactTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyContactTypes_Key",
                schema: "base",
                table: "PartyContactTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifiers_Code",
                schema: "base",
                table: "PartyIdentifiers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifiers_CountryCode_PartyIdentifierTypeId_NormalizedValue",
                schema: "base",
                table: "PartyIdentifiers",
                columns: new[] { "CountryCode", "PartyIdentifierTypeId", "NormalizedValue" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifiers_IsActive",
                schema: "base",
                table: "PartyIdentifiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifiers_PartyId",
                schema: "base",
                table: "PartyIdentifiers",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifiers_PartyIdentifierTypeId",
                schema: "base",
                table: "PartyIdentifiers",
                column: "PartyIdentifierTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifierTypes_IsActive_SortOrder",
                schema: "base",
                table: "PartyIdentifierTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyIdentifierTypes_Key",
                schema: "base",
                table: "PartyIdentifierTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyTypes_IsActive_SortOrder",
                schema: "base",
                table: "PartyTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyTypes_Key",
                schema: "base",
                table: "PartyTypes",
                column: "Key",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO [bms].[UnitOccupancyHistories]
                    ([UnitId], [OccupantsCount], [EffectiveFrom], [EffectiveTo], [Notes],
                     [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT [Id], 0, [CreatedAtUtc], NULL, N'Migration baseline',
                       CAST(1 AS bit), [CreatedAtUtc], NULL
                FROM [bms].[Units];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UnitOccupancyHistories_UnitId",
                schema: "bms",
                table: "UnitOccupancyHistories",
                column: "UnitId",
                unique: true,
                filter: "[IsActive] = CAST(1 AS bit) AND [EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnitOccupancyHistories_UnitId_EffectiveFrom",
                schema: "bms",
                table: "UnitOccupancyHistories",
                columns: new[] { "UnitId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelations_Code",
                schema: "bms",
                table: "UnitPartyRelations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelations_IsActive",
                schema: "bms",
                table: "UnitPartyRelations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelations_PartyId",
                schema: "bms",
                table: "UnitPartyRelations",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelations_UnitId_IsActive_EndDate",
                schema: "bms",
                table: "UnitPartyRelations",
                columns: new[] { "UnitId", "IsActive", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelations_UnitId_PartyId_UnitPartyRelationTypeId_EndDate",
                schema: "bms",
                table: "UnitPartyRelations",
                columns: new[] { "UnitId", "PartyId", "UnitPartyRelationTypeId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelations_UnitPartyRelationTypeId",
                schema: "bms",
                table: "UnitPartyRelations",
                column: "UnitPartyRelationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelationTypes_IsActive_SortOrder",
                schema: "bms",
                table: "UnitPartyRelationTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitPartyRelationTypes_Key",
                schema: "bms",
                table: "UnitPartyRelationTypes",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartyContacts",
                schema: "base");

            migrationBuilder.DropTable(
                name: "PartyIdentifiers",
                schema: "base");

            migrationBuilder.DropTable(
                name: "UnitOccupancyHistories",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitPartyRelations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PartyContactTypes",
                schema: "base");

            migrationBuilder.DropTable(
                name: "PartyIdentifierTypes",
                schema: "base");

            migrationBuilder.DropTable(
                name: "Parties",
                schema: "base");

            migrationBuilder.DropTable(
                name: "UnitPartyRelationTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PartyTypes",
                schema: "base");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Units_Counts",
                schema: "bms",
                table: "Units");

            migrationBuilder.DropColumn(
                name: "CurrentOccupantsCount",
                schema: "bms",
                table: "Units");

            migrationBuilder.UpdateData(
                schema: "bms",
                table: "UnitStatuses",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "IsActive", "Title" },
                values: new object[] { true, "در حال استفاده" });

            migrationBuilder.UpdateData(
                schema: "bms",
                table: "UnitStatuses",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "IsActive", "Title" },
                values: new object[] { true, "خالی" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Units_Counts",
                schema: "bms",
                table: "Units",
                sql: "([RoomsCount] IS NULL OR [RoomsCount] >= 0) AND [ParkingCount] >= 0 AND [StorageCount] >= 0");
        }
    }
}
