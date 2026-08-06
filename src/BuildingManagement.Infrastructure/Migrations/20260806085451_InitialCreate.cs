using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF-generated migration uses inline arrays for columns and seed data

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bms");

            migrationBuilder.CreateTable(
                name: "BuildingTypes",
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
                    table.PrimaryKey("PK_BuildingTypes", x => x.Id);
                    table.CheckConstraint("CK_BuildingTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "LocationTypes",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_LocationTypes", x => x.Id);
                    table.CheckConstraint("CK_LocationTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                    table.ForeignKey(
                        name: "FK_LocationTypes_LocationTypes_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "bms",
                        principalTable: "LocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitStatuses",
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
                    table.PrimaryKey("PK_UnitStatuses", x => x.Id);
                    table.CheckConstraint("CK_UnitStatuses_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "UnitUsageTypes",
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
                    table.PrimaryKey("PK_UnitUsageTypes", x => x.Id);
                    table.CheckConstraint("CK_UnitUsageTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<long>(type: "bigint", nullable: true),
                    LocationTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.Id);
                    table.CheckConstraint("CK_Locations_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_Locations_NotSelfParent", "[ParentId] IS NULL OR [ParentId] <> [Id]");
                    table.ForeignKey(
                        name: "FK_Locations_LocationTypes_LocationTypeId",
                        column: x => x.LocationTypeId,
                        principalSchema: "bms",
                        principalTable: "LocationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Locations_Locations_ParentId",
                        column: x => x.ParentId,
                        principalSchema: "bms",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Complexes",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complexes", x => x.Id);
                    table.CheckConstraint("CK_Complexes_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_Complexes_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "bms",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Buildings",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    BuildingTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    FloorsCount = table.Column<int>(type: "int", nullable: true),
                    ConstructionYear = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Buildings", x => x.Id);
                    table.CheckConstraint("CK_Buildings_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_Buildings_Floors", "[FloorsCount] IS NULL OR [FloorsCount] >= 0");
                    table.ForeignKey(
                        name: "FK_Buildings_BuildingTypes_BuildingTypeId",
                        column: x => x.BuildingTypeId,
                        principalSchema: "bms",
                        principalTable: "BuildingTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Buildings_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Buildings_Locations_LocationId",
                        column: x => x.LocationId,
                        principalSchema: "bms",
                        principalTable: "Locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Units",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false),
                    UsageTypeId = table.Column<long>(type: "bigint", nullable: false),
                    StatusId = table.Column<long>(type: "bigint", nullable: false),
                    UnitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NormalizedUnitNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FloorNumber = table.Column<int>(type: "int", nullable: true),
                    Area = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: true),
                    RoomsCount = table.Column<int>(type: "int", nullable: true),
                    ParkingCount = table.Column<int>(type: "int", nullable: false),
                    StorageCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Units", x => x.Id);
                    table.CheckConstraint("CK_Units_Area", "[Area] IS NULL OR [Area] >= 0");
                    table.CheckConstraint("CK_Units_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_Units_Counts", "([RoomsCount] IS NULL OR [RoomsCount] >= 0) AND [ParkingCount] >= 0 AND [StorageCount] >= 0");
                    table.ForeignKey(
                        name: "FK_Units_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Units_UnitStatuses_StatusId",
                        column: x => x.StatusId,
                        principalSchema: "bms",
                        principalTable: "UnitStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Units_UnitUsageTypes_UsageTypeId",
                        column: x => x.UsageTypeId,
                        principalSchema: "bms",
                        principalTable: "UnitUsageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "BuildingTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "residential", 10, "مسکونی", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "commercial", 20, "تجاری", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "office", 30, "اداری", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "mixed", 40, "مختلط", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "other", 50, "سایر", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "LocationTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "ParentId", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[] { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "country", null, 10, "کشور", null });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "UnitStatuses",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "available", 10, "آماده واگذاری", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "occupied", 20, "در حال استفاده", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "vacant", 30, "خالی", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "under_renovation", 40, "در حال بازسازی", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "inactive", 50, "غیرفعال", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "UnitUsageTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "residential", 10, "مسکونی", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "commercial", 20, "تجاری", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "office", 30, "اداری", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "storage", 40, "انباری", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "other", 50, "سایر", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "LocationTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "ParentId", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "state_or_province", 1L, 20, "استان یا ایالت", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "city", 2L, 30, "شهر", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "district", 3L, 40, "منطقه", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "neighborhood", 4L, 50, "محله", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_BuildingTypeId",
                schema: "bms",
                table: "Buildings",
                column: "BuildingTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_Code",
                schema: "bms",
                table: "Buildings",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_ComplexId",
                schema: "bms",
                table: "Buildings",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_IsActive",
                schema: "bms",
                table: "Buildings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Buildings_LocationId",
                schema: "bms",
                table: "Buildings",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingTypes_IsActive_SortOrder",
                schema: "bms",
                table: "BuildingTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingTypes_Key",
                schema: "bms",
                table: "BuildingTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complexes_Code",
                schema: "bms",
                table: "Complexes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Complexes_IsActive",
                schema: "bms",
                table: "Complexes",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Complexes_LocationId",
                schema: "bms",
                table: "Complexes",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_Code",
                schema: "bms",
                table: "Locations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Locations_IsActive",
                schema: "bms",
                table: "Locations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_LocationTypeId",
                schema: "bms",
                table: "Locations",
                column: "LocationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ParentId",
                schema: "bms",
                table: "Locations",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ParentId_LocationTypeId_NormalizedName",
                schema: "bms",
                table: "Locations",
                columns: new[] { "ParentId", "LocationTypeId", "NormalizedName" },
                unique: true,
                filter: "[ParentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_IsActive_SortOrder",
                schema: "bms",
                table: "LocationTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_Key",
                schema: "bms",
                table: "LocationTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocationTypes_ParentId",
                schema: "bms",
                table: "LocationTypes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_BuildingId_FloorNumber",
                schema: "bms",
                table: "Units",
                columns: new[] { "BuildingId", "FloorNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Units_BuildingId_NormalizedUnitNumber",
                schema: "bms",
                table: "Units",
                columns: new[] { "BuildingId", "NormalizedUnitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_Code",
                schema: "bms",
                table: "Units",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Units_IsActive",
                schema: "bms",
                table: "Units",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Units_StatusId",
                schema: "bms",
                table: "Units",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Units_UsageTypeId",
                schema: "bms",
                table: "Units",
                column: "UsageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitStatuses_IsActive_SortOrder",
                schema: "bms",
                table: "UnitStatuses",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitStatuses_Key",
                schema: "bms",
                table: "UnitStatuses",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitUsageTypes_IsActive_SortOrder",
                schema: "bms",
                table: "UnitUsageTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitUsageTypes_Key",
                schema: "bms",
                table: "UnitUsageTypes",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Units",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Buildings",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitStatuses",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitUsageTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "BuildingTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Complexes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Locations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "LocationTypes",
                schema: "bms");
        }
    }
}
