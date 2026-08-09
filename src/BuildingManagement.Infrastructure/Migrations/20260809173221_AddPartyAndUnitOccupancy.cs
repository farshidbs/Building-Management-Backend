using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration uses constant array arguments

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
                    table.PrimaryKey("PK_PartyContactTypes", x => x.Id);
                    table.CheckConstraint("CK_PartyContactTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "PartyTypes",
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
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
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
                    table.CheckConstraint("CK_UnitOccupancyHistories_Dates", "[EffectiveTo] IS NULL OR [EffectiveFrom] IS NULL OR [EffectiveTo] >= [EffectiveFrom]");
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
                schema: "bms",
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
                    IdentityNumber = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                        principalSchema: "bms",
                        principalTable: "PartyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PartyContacts",
                schema: "bms",
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
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartyContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartyContacts_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PartyContacts_PartyContactTypes_PartyContactTypeId",
                        column: x => x.PartyContactTypeId,
                        principalSchema: "bms",
                        principalTable: "PartyContactTypes",
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
                    IsPrimaryContact = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitPartyRelations", x => x.Id);
                    table.CheckConstraint("CK_UnitPartyRelations_Dates", "[EndDate] IS NULL OR [StartDate] IS NULL OR [EndDate] >= [StartDate]");
                    table.ForeignKey(
                        name: "FK_UnitPartyRelations_Parties_PartyId",
                        column: x => x.PartyId,
                        principalSchema: "bms",
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
                schema: "bms",
                table: "PartyContactTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "mobile", 10, "موبایل", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "phone", 20, "تلفن", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "email", 30, "ایمیل", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "PartyTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "iranian_person", 10, "شخص حقیقی ایرانی", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "iranian_organization", 20, "شخص حقوقی ایرانی", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "foreign_person", 30, "شخص حقیقی خارجی", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "foreign_organization", 40, "شخص حقوقی خارجی", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "UnitPartyRelationTypes",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "IsOccupancyRelation", "IsOwnershipRelation", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, true, "owner", 10, "مالک", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, false, "tenant", 20, "مستأجر", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, true, false, "resident", 30, "ساکن", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, false, "legal_representative", 40, "نماینده قانونی", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, false, "contact_person", 50, "شخص رابط", null },
                    { 6L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, false, false, "other", 60, "سایر", null }
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

            // Existing units have no reliable occupancy effective date. Preserve that fact as unknown
            // while establishing the required single current history row.
            migrationBuilder.Sql("""
                INSERT INTO [bms].[UnitOccupancyHistories]
                    ([UnitId], [OccupantsCount], [EffectiveFrom], [EffectiveTo], [Notes],
                     [IsActive], [CreatedAtUtc], [UpdatedAtUtc])
                SELECT [Id], [CurrentOccupantsCount], NULL, NULL, NULL, 1, SYSUTCDATETIME(), NULL
                FROM [bms].[Units];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_Code",
                schema: "bms",
                table: "Parties",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Parties_IsActive",
                schema: "bms",
                table: "Parties",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_NormalizedDisplayName",
                schema: "bms",
                table: "Parties",
                column: "NormalizedDisplayName");

            migrationBuilder.CreateIndex(
                name: "IX_Parties_PartyTypeId",
                schema: "bms",
                table: "Parties",
                column: "PartyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_NormalizedValue",
                schema: "bms",
                table: "PartyContacts",
                column: "NormalizedValue");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyContactTypeId",
                schema: "bms",
                table: "PartyContacts",
                column: "PartyContactTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId",
                schema: "bms",
                table: "PartyContacts",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContacts_PartyId_PartyContactTypeId",
                schema: "bms",
                table: "PartyContacts",
                columns: new[] { "PartyId", "PartyContactTypeId" },
                unique: true,
                filter: "[IsActive] = CAST(1 AS bit) AND [IsPrimary] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_PartyContactTypes_IsActive_SortOrder",
                schema: "bms",
                table: "PartyContactTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyContactTypes_Key",
                schema: "bms",
                table: "PartyContactTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PartyTypes_IsActive_SortOrder",
                schema: "bms",
                table: "PartyTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PartyTypes_Key",
                schema: "bms",
                table: "PartyTypes",
                column: "Key",
                unique: true);

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
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitOccupancyHistories",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitPartyRelations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PartyContactTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Parties",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitPartyRelationTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PartyTypes",
                schema: "bms");

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
