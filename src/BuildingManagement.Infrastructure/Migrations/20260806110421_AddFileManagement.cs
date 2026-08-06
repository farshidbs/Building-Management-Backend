using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration index column arrays.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFileManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "base");

            migrationBuilder.CreateTable(
                name: "DocumentTypes",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequiresDocumentDate = table.Column<bool>(type: "bit", nullable: false),
                    SupportsExpiration = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_DocumentTypes", x => x.Id);
                    table.CheckConstraint("CK_DocumentTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "StoredFiles",
                schema: "base",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    FileExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ChecksumSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    StorageProvider = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredFiles", x => x.Id);
                    table.CheckConstraint("CK_StoredFiles_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                });

            migrationBuilder.CreateTable(
                name: "BuildingDocuments",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingDocuments", x => x.Id);
                    table.CheckConstraint("CK_BuildingDocuments_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_BuildingDocuments_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildingDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "bms",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildingDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuildingGalleryFiles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuildingGalleryFiles", x => x.Id);
                    table.CheckConstraint("CK_BuildingGalleryFiles_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_BuildingGalleryFiles_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuildingGalleryFiles_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplexDocuments",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplexId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DocumentDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    EffectiveFrom = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplexDocuments", x => x.Id);
                    table.CheckConstraint("CK_ComplexDocuments_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_ComplexDocuments_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplexDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalSchema: "bms",
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplexDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplexGalleryFiles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplexId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AltText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsCover = table.Column<bool>(type: "bit", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplexGalleryFiles", x => x.Id);
                    table.CheckConstraint("CK_ComplexGalleryFiles_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_ComplexGalleryFiles_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplexGalleryFiles_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "DocumentTypes",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "IsActive", "Key", "RequiresDocumentDate", "SortOrder", "SupportsExpiration", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "ownership_document", true, 10, false, "سند مالکیت", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "contract", true, 20, false, "قرارداد", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "insurance_policy", true, 30, true, "بیمه‌نامه", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "permit", true, 40, true, "مجوز", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "building_plan", false, 50, false, "نقشه", null },
                    { 6L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "invoice", true, 60, false, "صورتحساب", null },
                    { 7L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "management_approval", true, 70, false, "رسید", null },
                    { 8L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "board_meeting_minutes", true, 80, false, "گزارش", null },
                    { 9L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "official_letter", true, 90, true, "گواهی", null },
                    { 10L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, true, "other", false, 100, false, "سایر", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDocuments_BuildingId_IsActive_DocumentTypeId",
                schema: "bms",
                table: "BuildingDocuments",
                columns: new[] { "BuildingId", "IsActive", "DocumentTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDocuments_BuildingId_StoredFileId",
                schema: "bms",
                table: "BuildingDocuments",
                columns: new[] { "BuildingId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDocuments_Code",
                schema: "bms",
                table: "BuildingDocuments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDocuments_DocumentTypeId",
                schema: "bms",
                table: "BuildingDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDocuments_IsActive",
                schema: "bms",
                table: "BuildingDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingDocuments_StoredFileId",
                schema: "bms",
                table: "BuildingDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingGalleryFiles_BuildingId",
                schema: "bms",
                table: "BuildingGalleryFiles",
                column: "BuildingId",
                unique: true,
                filter: "[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingGalleryFiles_BuildingId_IsActive_SortOrder",
                schema: "bms",
                table: "BuildingGalleryFiles",
                columns: new[] { "BuildingId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BuildingGalleryFiles_BuildingId_StoredFileId",
                schema: "bms",
                table: "BuildingGalleryFiles",
                columns: new[] { "BuildingId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingGalleryFiles_Code",
                schema: "bms",
                table: "BuildingGalleryFiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BuildingGalleryFiles_IsActive",
                schema: "bms",
                table: "BuildingGalleryFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BuildingGalleryFiles_StoredFileId",
                schema: "bms",
                table: "BuildingGalleryFiles",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexDocuments_Code",
                schema: "bms",
                table: "ComplexDocuments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplexDocuments_ComplexId_IsActive_DocumentTypeId",
                schema: "bms",
                table: "ComplexDocuments",
                columns: new[] { "ComplexId", "IsActive", "DocumentTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplexDocuments_ComplexId_StoredFileId",
                schema: "bms",
                table: "ComplexDocuments",
                columns: new[] { "ComplexId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplexDocuments_DocumentTypeId",
                schema: "bms",
                table: "ComplexDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexDocuments_IsActive",
                schema: "bms",
                table: "ComplexDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexDocuments_StoredFileId",
                schema: "bms",
                table: "ComplexDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexGalleryFiles_Code",
                schema: "bms",
                table: "ComplexGalleryFiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplexGalleryFiles_ComplexId",
                schema: "bms",
                table: "ComplexGalleryFiles",
                column: "ComplexId",
                unique: true,
                filter: "[IsActive] = CAST(1 AS bit) AND [IsCover] = CAST(1 AS bit)");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexGalleryFiles_ComplexId_IsActive_SortOrder",
                schema: "bms",
                table: "ComplexGalleryFiles",
                columns: new[] { "ComplexId", "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplexGalleryFiles_ComplexId_StoredFileId",
                schema: "bms",
                table: "ComplexGalleryFiles",
                columns: new[] { "ComplexId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplexGalleryFiles_IsActive",
                schema: "bms",
                table: "ComplexGalleryFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ComplexGalleryFiles_StoredFileId",
                schema: "bms",
                table: "ComplexGalleryFiles",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_IsActive_SortOrder",
                schema: "bms",
                table: "DocumentTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTypes_Key",
                schema: "bms",
                table: "DocumentTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_Code",
                schema: "base",
                table: "StoredFiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_IsActive",
                schema: "base",
                table: "StoredFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StoredFiles_StorageProvider_StorageKey",
                schema: "base",
                table: "StoredFiles",
                columns: new[] { "StorageProvider", "StorageKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuildingDocuments",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "BuildingGalleryFiles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ComplexDocuments",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ComplexGalleryFiles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DocumentTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "StoredFiles",
                schema: "base");
        }
    }
}
