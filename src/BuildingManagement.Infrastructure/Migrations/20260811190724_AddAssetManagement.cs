using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // EF Core generates repeated array arguments in migrations.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetEventTypes",
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
                    table.PrimaryKey("PK_AssetEventTypes", x => x.Id);
                    table.CheckConstraint("CK_AssetEventTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "AssetTypes",
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
                    table.PrimaryKey("PK_AssetTypes", x => x.Id);
                    table.CheckConstraint("CK_AssetTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetTypeId = table.Column<long>(type: "bigint", nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InstallationDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    PurchaseDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    SuggestedReviewIntervalDays = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.CheckConstraint("CK_Assets_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_Assets_ReviewInterval", "[SuggestedReviewIntervalDays] IS NULL OR [SuggestedReviewIntervalDays] > 0");
                    table.CheckConstraint("CK_Assets_Scope", "([ComplexId] IS NOT NULL AND [BuildingId] IS NULL) OR ([ComplexId] IS NULL AND [BuildingId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Assets_AssetTypes_AssetTypeId",
                        column: x => x.AssetTypeId,
                        principalSchema: "bms",
                        principalTable: "AssetTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Assets_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetDocuments",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDocuments", x => x.Id);
                    table.CheckConstraint("CK_AssetDocuments_Dates", "[ExpiresAt] IS NULL OR [EffectiveFrom] IS NULL OR [ExpiresAt] >= [EffectiveFrom]");
                    table.ForeignKey(
                        name: "FK_AssetDocuments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "bms",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "bms",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetEvents",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<long>(type: "bigint", nullable: false),
                    AssetEventTypeId = table.Column<long>(type: "bigint", nullable: false),
                    EventDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SuggestedNextDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ServiceProviderPartyId = table.Column<long>(type: "bigint", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetEvents", x => x.Id);
                    table.CheckConstraint("CK_AssetEvents_Cost", "[Cost] IS NULL OR [Cost] >= 0");
                    table.CheckConstraint("CK_AssetEvents_Dates", "[SuggestedNextDate] IS NULL OR [SuggestedNextDate] >= [EventDate]");
                    table.ForeignKey(
                        name: "FK_AssetEvents_AssetEventTypes_AssetEventTypeId",
                        column: x => x.AssetEventTypeId,
                        principalSchema: "bms",
                        principalTable: "AssetEventTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetEvents_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "bms",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetEvents_Parties_ServiceProviderPartyId",
                        column: x => x.ServiceProviderPartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetGalleryFiles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetGalleryFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetGalleryFiles_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "bms",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetGalleryFiles_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetEventFiles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetEventId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetEventFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetEventFiles_AssetEvents_AssetEventId",
                        column: x => x.AssetEventId,
                        principalSchema: "bms",
                        principalTable: "AssetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssetEventFiles_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "AssetEventTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "inspection", 10, "بازرسی", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "maintenance", 20, "سرویس و نگهداری", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "repair", 30, "تعمیر", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "replacement", 40, "تعویض", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "installation", 50, "نصب", null },
                    { 6L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "incident", 60, "خرابی یا حادثه", null },
                    { 7L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "other", 70, "سایر", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "AssetTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "elevator", 10, "آسانسور", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "water_pump", 20, "پمپ آب", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "boiler", 30, "دیگ و موتورخانه", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "generator", 40, "ژنراتور", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "parking_door", 50, "درب پارکینگ", null },
                    { 6L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "fire_alarm", 60, "سامانه اعلام حریق", null },
                    { 7L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "fire_extinguisher", 70, "کپسول آتش‌نشانی", null },
                    { 8L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "camera", 80, "دوربین مداربسته", null },
                    { 9L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "other", 90, "سایر", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_AssetId",
                schema: "bms",
                table: "AssetDocuments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_DocumentTypeId",
                schema: "bms",
                table: "AssetDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_IsActive",
                schema: "bms",
                table: "AssetDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_StoredFileId",
                schema: "bms",
                table: "AssetDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEventFiles_AssetEventId",
                schema: "bms",
                table: "AssetEventFiles",
                column: "AssetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEventFiles_IsActive",
                schema: "bms",
                table: "AssetEventFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEventFiles_StoredFileId",
                schema: "bms",
                table: "AssetEventFiles",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_AssetEventTypeId",
                schema: "bms",
                table: "AssetEvents",
                column: "AssetEventTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_AssetId_EventDate",
                schema: "bms",
                table: "AssetEvents",
                columns: new[] { "AssetId", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_IsActive",
                schema: "bms",
                table: "AssetEvents",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEvents_ServiceProviderPartyId",
                schema: "bms",
                table: "AssetEvents",
                column: "ServiceProviderPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetEventTypes_IsActive_SortOrder",
                schema: "bms",
                table: "AssetEventTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetEventTypes_Key",
                schema: "bms",
                table: "AssetEventTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AssetGalleryFiles_ActiveCover",
                schema: "bms",
                table: "AssetGalleryFiles",
                column: "AssetId",
                unique: true,
                filter: "[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_AssetGalleryFiles_AssetId_SortOrder",
                schema: "bms",
                table: "AssetGalleryFiles",
                columns: new[] { "AssetId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetGalleryFiles_IsActive",
                schema: "bms",
                table: "AssetGalleryFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetGalleryFiles_StoredFileId",
                schema: "bms",
                table: "AssetGalleryFiles",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_AssetTypeId",
                schema: "bms",
                table: "Assets",
                column: "AssetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_BuildingId",
                schema: "bms",
                table: "Assets",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_Code",
                schema: "bms",
                table: "Assets",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Assets_ComplexId",
                schema: "bms",
                table: "Assets",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_IsActive",
                schema: "bms",
                table: "Assets",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_IsActive_SortOrder",
                schema: "bms",
                table: "AssetTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_Key",
                schema: "bms",
                table: "AssetTypes",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetDocuments",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AssetEventFiles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AssetGalleryFiles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AssetEvents",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AssetEventTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AssetTypes",
                schema: "bms");
        }
    }
}
