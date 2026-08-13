using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

#pragma warning disable CA1861 // EF Core generates constant arrays for migration index definitions.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandTypes",
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
                    table.PrimaryKey("PK_DemandTypes", x => x.Id);
                    table.CheckConstraint("CK_DemandTypes_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "ExpenseTypes",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "varchar(50)", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTypes", x => x.Id);
                    table.CheckConstraint("CK_ExpenseTypes_GlobalKey", "[BuildingId] IS NOT NULL OR [ComplexId] IS NOT NULL OR [Key] IS NOT NULL");
                    table.CheckConstraint("CK_ExpenseTypes_Scope", "[BuildingId] IS NULL OR [ComplexId] IS NULL");
                    table.ForeignKey(
                        name: "FK_ExpenseTypes_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseTypes_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    AccountKindKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                    table.CheckConstraint("CK_FinancialAccounts_KindOwner", "([UnitId] IS NOT NULL AND [AccountKindKey] = 'unit_account') OR ([UnitId] IS NULL AND [AccountKindKey] IN ('current_fund','reserve_fund'))");
                    table.CheckConstraint("CK_FinancialAccounts_OneOwner", "(CASE WHEN [UnitId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialAccounts_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethods",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequiresManagerApproval = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PaymentMethods", x => x.Id);
                    table.CheckConstraint("CK_PaymentMethods_KeyFormat", "[Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    ComplexId = table.Column<long>(type: "bigint", nullable: true),
                    ExpenseTypeId = table.Column<long>(type: "bigint", nullable: false),
                    VendorPartyId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.CheckConstraint("CK_Expenses_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Expenses_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_Expenses_OneScope", "(CASE WHEN [BuildingId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ComplexId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_Expenses_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalSchema: "bms",
                        principalTable: "ExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_Parties_VendorPartyId",
                        column: x => x.VendorPartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountAdjustments",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialAccountId = table.Column<long>(type: "bigint", nullable: false),
                    FundAccountId = table.Column<long>(type: "bigint", nullable: true),
                    ResponsiblePartyTypeKey = table.Column<string>(type: "varchar(30)", nullable: true),
                    ResponsiblePartyId = table.Column<long>(type: "bigint", nullable: true),
                    AdjustmentTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountAdjustments", x => x.Id);
                    table.CheckConstraint("CK_AccountAdjustments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_AccountAdjustments_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_AccountAdjustments_Fund", "([AdjustmentTypeKey] = 'opening_debt' AND [FundAccountId] IS NOT NULL) OR ([AdjustmentTypeKey] <> 'opening_debt' AND [FundAccountId] IS NULL)");
                    table.ForeignKey(
                        name: "FK_AccountAdjustments_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountAdjustments_FinancialAccounts_FundAccountId",
                        column: x => x.FundAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AccountAdjustments_Parties_ResponsiblePartyId",
                        column: x => x.ResponsiblePartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Demands",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundAccountId = table.Column<long>(type: "bigint", nullable: false),
                    DemandTypeId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DemandDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Demands", x => x.Id);
                    table.CheckConstraint("CK_Demands_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_Demands_DemandTypes_DemandTypeId",
                        column: x => x.DemandTypeId,
                        principalSchema: "bms",
                        principalTable: "DemandTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Demands_FinancialAccounts_FundAccountId",
                        column: x => x.FundAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitAccountId = table.Column<long>(type: "bigint", nullable: false),
                    ReceivingFundAccountId = table.Column<long>(type: "bigint", nullable: false),
                    PayerPartyId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentMethodId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RejectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    GatewayReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankTrackingCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Payments_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_Payments_FinancialAccounts_ReceivingFundAccountId",
                        column: x => x.ReceivingFundAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_FinancialAccounts_UnitAccountId",
                        column: x => x.UnitAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Parties_PayerPartyId",
                        column: x => x.PayerPartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_PaymentMethods_PaymentMethodId",
                        column: x => x.PaymentMethodId,
                        principalSchema: "bms",
                        principalTable: "PaymentMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseAssetEvents",
                schema: "bms",
                columns: table => new
                {
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    AssetEventId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseAssetEvents", x => new { x.ExpenseId, x.AssetEventId });
                    table.ForeignKey(
                        name: "FK_ExpenseAssetEvents_AssetEvents_AssetEventId",
                        column: x => x.AssetEventId,
                        principalSchema: "bms",
                        principalTable: "AssetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseAssetEvents_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseAssets",
                schema: "bms",
                columns: table => new
                {
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    AssetId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseAssets", x => new { x.ExpenseId, x.AssetId });
                    table.ForeignKey(
                        name: "FK_ExpenseAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "bms",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseAssets_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseBuildings",
                schema: "bms",
                columns: table => new
                {
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseBuildings", x => new { x.ExpenseId, x.BuildingId });
                    table.ForeignKey(
                        name: "FK_ExpenseBuildings_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseBuildings_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseComplexes",
                schema: "bms",
                columns: table => new
                {
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseComplexes", x => new { x.ExpenseId, x.ComplexId });
                    table.ForeignKey(
                        name: "FK_ExpenseComplexes_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseComplexes_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseDisbursements",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    FundAccountId = table.Column<long>(type: "bigint", nullable: false),
                    PayeePartyId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethodKey = table.Column<string>(type: "varchar(50)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDisbursements", x => x.Id);
                    table.CheckConstraint("CK_ExpenseDisbursements_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_ExpenseDisbursements_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_ExpenseDisbursements_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDisbursements_FinancialAccounts_FundAccountId",
                        column: x => x.FundAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDisbursements_Parties_PayeePartyId",
                        column: x => x.PayeePartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseDocuments",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDocuments_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandAllocationRules",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    AllocationMethodKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    AmountModeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RateAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IncludeVacantUnits = table.Column<bool>(type: "bit", nullable: true),
                    ResponsiblePartyTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    RedistributionPolicyKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAllocationRules", x => x.Id);
                    table.CheckConstraint("CK_DemandAllocationRules_Amounts", "([TotalAmount] IS NULL OR [TotalAmount] > 0) AND ([RateAmount] IS NULL OR [RateAmount] > 0)");
                    table.ForeignKey(
                        name: "FK_DemandAllocationRules_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandAllocations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    UnitId = table.Column<long>(type: "bigint", nullable: false),
                    ResponsiblePartyTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    ResponsiblePartyId = table.Column<long>(type: "bigint", nullable: true),
                    BasisValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CalculatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FinalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsIncluded = table.Column<bool>(type: "bit", nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAllocations", x => x.Id);
                    table.CheckConstraint("CK_DemandAllocations_Amounts", "[CalculatedAmount] >= 0 AND [FinalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_DemandAllocations_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandAllocations_Parties_ResponsiblePartyId",
                        column: x => x.ResponsiblePartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandAllocations_Units_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "bms",
                        principalTable: "Units",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandAssetEvents",
                schema: "bms",
                columns: table => new
                {
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    AssetEventId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAssetEvents", x => new { x.DemandId, x.AssetEventId });
                    table.ForeignKey(
                        name: "FK_DemandAssetEvents_AssetEvents_AssetEventId",
                        column: x => x.AssetEventId,
                        principalSchema: "bms",
                        principalTable: "AssetEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandAssetEvents_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandAssets",
                schema: "bms",
                columns: table => new
                {
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    AssetId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandAssets", x => new { x.DemandId, x.AssetId });
                    table.ForeignKey(
                        name: "FK_DemandAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "bms",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandAssets_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandBuildings",
                schema: "bms",
                columns: table => new
                {
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    BuildingId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandBuildings", x => new { x.DemandId, x.BuildingId });
                    table.ForeignKey(
                        name: "FK_DemandBuildings_Buildings_BuildingId",
                        column: x => x.BuildingId,
                        principalSchema: "bms",
                        principalTable: "Buildings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandBuildings_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandComplexes",
                schema: "bms",
                columns: table => new
                {
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    ComplexId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandComplexes", x => new { x.DemandId, x.ComplexId });
                    table.ForeignKey(
                        name: "FK_DemandComplexes_Complexes_ComplexId",
                        column: x => x.ComplexId,
                        principalSchema: "bms",
                        principalTable: "Complexes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandComplexes_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DemandExpenses",
                schema: "bms",
                columns: table => new
                {
                    DemandId = table.Column<long>(type: "bigint", nullable: false),
                    ExpenseId = table.Column<long>(type: "bigint", nullable: false),
                    RelatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandExpenses", x => new { x.DemandId, x.ExpenseId });
                    table.ForeignKey(
                        name: "FK_DemandExpenses_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DemandExpenses_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalSchema: "bms",
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentEvidenceFiles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEvidenceFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentEvidenceFiles_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "bms",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentEvidenceFiles_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseDisbursementFiles",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpenseDisbursementId = table.Column<long>(type: "bigint", nullable: false),
                    StoredFileId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseDisbursementFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseDisbursementFiles_ExpenseDisbursements_ExpenseDisbursementId",
                        column: x => x.ExpenseDisbursementId,
                        principalSchema: "bms",
                        principalTable: "ExpenseDisbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExpenseDisbursementFiles_StoredFiles_StoredFileId",
                        column: x => x.StoredFileId,
                        principalSchema: "base",
                        principalTable: "StoredFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionTypeKey = table.Column<string>(type: "varchar(40)", nullable: false),
                    DemandId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    ExpenseDisbursementId = table.Column<long>(type: "bigint", nullable: true),
                    AccountAdjustmentId = table.Column<long>(type: "bigint", nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.CheckConstraint("CK_FinancialTransactions_OneSource", "(CASE WHEN [DemandId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [PaymentId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [ExpenseDisbursementId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [AccountAdjustmentId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("CK_FinancialTransactions_TypeSource", "([TransactionTypeKey] = 'demand' AND [DemandId] IS NOT NULL) OR ([TransactionTypeKey] = 'payment' AND [PaymentId] IS NOT NULL) OR ([TransactionTypeKey] = 'expense_disbursement' AND [ExpenseDisbursementId] IS NOT NULL) OR ([TransactionTypeKey] = 'account_adjustment' AND [AccountAdjustmentId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_AccountAdjustments_AccountAdjustmentId",
                        column: x => x.AccountAdjustmentId,
                        principalSchema: "bms",
                        principalTable: "AccountAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_Demands_DemandId",
                        column: x => x.DemandId,
                        principalSchema: "bms",
                        principalTable: "Demands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_ExpenseDisbursements_ExpenseDisbursementId",
                        column: x => x.ExpenseDisbursementId,
                        principalSchema: "bms",
                        principalTable: "ExpenseDisbursements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "bms",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitReceivables",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitAccountId = table.Column<long>(type: "bigint", nullable: false),
                    FundAccountId = table.Column<long>(type: "bigint", nullable: false),
                    DemandAllocationId = table.Column<long>(type: "bigint", nullable: true),
                    AccountAdjustmentId = table.Column<long>(type: "bigint", nullable: true),
                    OriginalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ResponsiblePartyTypeKey = table.Column<string>(type: "varchar(30)", nullable: false),
                    ResponsiblePartyId = table.Column<long>(type: "bigint", nullable: true),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitReceivables", x => x.Id);
                    table.CheckConstraint("CK_UnitReceivables_Amounts", "[OriginalAmount] > 0 AND [OutstandingAmount] >= 0 AND [OutstandingAmount] <= [OriginalAmount]");
                    table.CheckConstraint("CK_UnitReceivables_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.CheckConstraint("CK_UnitReceivables_Origin", "(CASE WHEN [DemandAllocationId] IS NULL THEN 0 ELSE 1 END + CASE WHEN [AccountAdjustmentId] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "FK_UnitReceivables_AccountAdjustments_AccountAdjustmentId",
                        column: x => x.AccountAdjustmentId,
                        principalSchema: "bms",
                        principalTable: "AccountAdjustments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitReceivables_DemandAllocations_DemandAllocationId",
                        column: x => x.DemandAllocationId,
                        principalSchema: "bms",
                        principalTable: "DemandAllocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitReceivables_FinancialAccounts_FundAccountId",
                        column: x => x.FundAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitReceivables_FinancialAccounts_UnitAccountId",
                        column: x => x.UnitAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitReceivables_Parties_ResponsiblePartyId",
                        column: x => x.ResponsiblePartyId,
                        principalSchema: "bms",
                        principalTable: "Parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactionEntries",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FinancialTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    FinancialAccountId = table.Column<long>(type: "bigint", nullable: false),
                    EffectKey = table.Column<string>(type: "varchar(10)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactionEntries", x => x.Id);
                    table.CheckConstraint("CK_FinancialTransactionEntries_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_FinancialTransactionEntries_Effect", "[EffectKey] IN ('increase','decrease')");
                    table.ForeignKey(
                        name: "FK_FinancialTransactionEntries_FinancialAccounts_FinancialAccountId",
                        column: x => x.FinancialAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialTransactionEntries_FinancialTransactions_FinancialTransactionId",
                        column: x => x.FinancialTransactionId,
                        principalSchema: "bms",
                        principalTable: "FinancialTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAllocations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<long>(type: "bigint", nullable: false),
                    UnitReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAllocations", x => x.Id);
                    table.CheckConstraint("CK_PaymentAllocations_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalSchema: "bms",
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentAllocations_UnitReceivables_UnitReceivableId",
                        column: x => x.UnitReceivableId,
                        principalSchema: "bms",
                        principalTable: "UnitReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "DemandTypes",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "monthly_charge", 10, "شارژ ماهانه", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "utility_contribution", 20, "سهم قبوض", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "special_assessment", 30, "مشارکت ویژه", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "maintenance_contribution", 40, "مشارکت نگهداری", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "fund_shortage", 50, "جبران کسری صندوق", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "ExpenseTypes",
                columns: new[] { "Id", "BuildingId", "ComplexId", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, null, null, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "water", 10, "آب", null },
                    { 2L, null, null, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "electricity", 20, "برق", null },
                    { 3L, null, null, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "cleaning", 30, "نظافت", null },
                    { 4L, null, null, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "caretaker", 40, "حقوق سرایدار", null },
                    { 5L, null, null, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "equipment_repair", 50, "تعمیر تجهیزات", null },
                    { 6L, null, null, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "insurance", 60, "بیمه", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "PaymentMethods",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "RequiresManagerApproval", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "online_gateway", false, 10, "درگاه آنلاین", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "card_to_card", true, 20, "کارت به کارت", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "bank_transfer", true, 30, "انتقال بانکی", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "cash", true, 40, "نقدی", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "manual", true, 50, "ثبت دستی", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_Code",
                schema: "bms",
                table: "AccountAdjustments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_FinancialAccountId_Status",
                schema: "bms",
                table: "AccountAdjustments",
                columns: new[] { "FinancialAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_FundAccountId",
                schema: "bms",
                table: "AccountAdjustments",
                column: "FundAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_IsActive",
                schema: "bms",
                table: "AccountAdjustments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AccountAdjustments_ResponsiblePartyId",
                schema: "bms",
                table: "AccountAdjustments",
                column: "ResponsiblePartyId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandAllocationRules_DemandId",
                schema: "bms",
                table: "DemandAllocationRules",
                column: "DemandId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandAllocations_DemandId_UnitId",
                schema: "bms",
                table: "DemandAllocations",
                columns: new[] { "DemandId", "UnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandAllocations_ResponsiblePartyId",
                schema: "bms",
                table: "DemandAllocations",
                column: "ResponsiblePartyId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandAllocations_UnitId",
                schema: "bms",
                table: "DemandAllocations",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandAssetEvents_AssetEventId",
                schema: "bms",
                table: "DemandAssetEvents",
                column: "AssetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandAssets_AssetId",
                schema: "bms",
                table: "DemandAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandBuildings_BuildingId",
                schema: "bms",
                table: "DemandBuildings",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandComplexes_ComplexId",
                schema: "bms",
                table: "DemandComplexes",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandExpenses_ExpenseId",
                schema: "bms",
                table: "DemandExpenses",
                column: "ExpenseId");

            migrationBuilder.CreateIndex(
                name: "IX_Demands_Code",
                schema: "bms",
                table: "Demands",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Demands_DemandTypeId",
                schema: "bms",
                table: "Demands",
                column: "DemandTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Demands_FundAccountId_Status_DemandDate_DueDate",
                schema: "bms",
                table: "Demands",
                columns: new[] { "FundAccountId", "Status", "DemandDate", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Demands_IsActive",
                schema: "bms",
                table: "Demands",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DemandTypes_IsActive_SortOrder",
                schema: "bms",
                table: "DemandTypes",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandTypes_Key",
                schema: "bms",
                table: "DemandTypes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAssetEvents_AssetEventId",
                schema: "bms",
                table: "ExpenseAssetEvents",
                column: "AssetEventId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseAssets_AssetId",
                schema: "bms",
                table: "ExpenseAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseBuildings_BuildingId",
                schema: "bms",
                table: "ExpenseBuildings",
                column: "BuildingId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseComplexes_ComplexId",
                schema: "bms",
                table: "ExpenseComplexes",
                column: "ComplexId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursementFiles_ExpenseDisbursementId_StoredFileId",
                schema: "bms",
                table: "ExpenseDisbursementFiles",
                columns: new[] { "ExpenseDisbursementId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursementFiles_IsActive",
                schema: "bms",
                table: "ExpenseDisbursementFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursementFiles_StoredFileId",
                schema: "bms",
                table: "ExpenseDisbursementFiles",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursements_Code",
                schema: "bms",
                table: "ExpenseDisbursements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursements_ExpenseId_Status",
                schema: "bms",
                table: "ExpenseDisbursements",
                columns: new[] { "ExpenseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursements_FundAccountId_Status",
                schema: "bms",
                table: "ExpenseDisbursements",
                columns: new[] { "FundAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursements_IsActive",
                schema: "bms",
                table: "ExpenseDisbursements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDisbursements_PayeePartyId",
                schema: "bms",
                table: "ExpenseDisbursements",
                column: "PayeePartyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_ExpenseId_StoredFileId",
                schema: "bms",
                table: "ExpenseDocuments",
                columns: new[] { "ExpenseId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_IsActive",
                schema: "bms",
                table: "ExpenseDocuments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseDocuments_StoredFileId",
                schema: "bms",
                table: "ExpenseDocuments",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BuildingId_Status_ExpenseDate",
                schema: "bms",
                table: "Expenses",
                columns: new[] { "BuildingId", "Status", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Code",
                schema: "bms",
                table: "Expenses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ComplexId_Status_ExpenseDate",
                schema: "bms",
                table: "Expenses",
                columns: new[] { "ComplexId", "Status", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseTypeId",
                schema: "bms",
                table: "Expenses",
                column: "ExpenseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_IsActive",
                schema: "bms",
                table: "Expenses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_VendorPartyId",
                schema: "bms",
                table: "Expenses",
                column: "VendorPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_BuildingId_Title",
                schema: "bms",
                table: "ExpenseTypes",
                columns: new[] { "BuildingId", "Title" },
                unique: true,
                filter: "[BuildingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_ComplexId_Title",
                schema: "bms",
                table: "ExpenseTypes",
                columns: new[] { "ComplexId", "Title" },
                unique: true,
                filter: "[ComplexId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseTypes_Key",
                schema: "bms",
                table: "ExpenseTypes",
                column: "Key",
                unique: true,
                filter: "[Key] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_BuildingId_AccountKindKey",
                schema: "bms",
                table: "FinancialAccounts",
                columns: new[] { "BuildingId", "AccountKindKey" },
                unique: true,
                filter: "[BuildingId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_ComplexId_AccountKindKey",
                schema: "bms",
                table: "FinancialAccounts",
                columns: new[] { "ComplexId", "AccountKindKey" },
                unique: true,
                filter: "[ComplexId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_IsActive",
                schema: "bms",
                table: "FinancialAccounts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_UnitId_AccountKindKey",
                schema: "bms",
                table: "FinancialAccounts",
                columns: new[] { "UnitId", "AccountKindKey" },
                unique: true,
                filter: "[UnitId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactionEntries_FinancialAccountId_CreatedAtUtc",
                schema: "bms",
                table: "FinancialTransactionEntries",
                columns: new[] { "FinancialAccountId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactionEntries_FinancialTransactionId",
                schema: "bms",
                table: "FinancialTransactionEntries",
                column: "FinancialTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_AccountAdjustmentId",
                schema: "bms",
                table: "FinancialTransactions",
                column: "AccountAdjustmentId",
                unique: true,
                filter: "[AccountAdjustmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_DemandId",
                schema: "bms",
                table: "FinancialTransactions",
                column: "DemandId",
                unique: true,
                filter: "[DemandId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_ExpenseDisbursementId",
                schema: "bms",
                table: "FinancialTransactions",
                column: "ExpenseDisbursementId",
                unique: true,
                filter: "[ExpenseDisbursementId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_PaymentId",
                schema: "bms",
                table: "FinancialTransactions",
                column: "PaymentId",
                unique: true,
                filter: "[PaymentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_UnitReceivableId",
                schema: "bms",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "UnitReceivableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_UnitReceivableId",
                schema: "bms",
                table: "PaymentAllocations",
                column: "UnitReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvidenceFiles_IsActive",
                schema: "bms",
                table: "PaymentEvidenceFiles",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvidenceFiles_PaymentId_StoredFileId",
                schema: "bms",
                table: "PaymentEvidenceFiles",
                columns: new[] { "PaymentId", "StoredFileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentEvidenceFiles_StoredFileId",
                schema: "bms",
                table: "PaymentEvidenceFiles",
                column: "StoredFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_IsActive_SortOrder",
                schema: "bms",
                table: "PaymentMethods",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethods_Key",
                schema: "bms",
                table: "PaymentMethods",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Code",
                schema: "bms",
                table: "Payments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_GatewayReference",
                schema: "bms",
                table: "Payments",
                column: "GatewayReference",
                unique: true,
                filter: "[GatewayReference] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_IsActive",
                schema: "bms",
                table: "Payments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayerPartyId",
                schema: "bms",
                table: "Payments",
                column: "PayerPartyId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaymentMethodId",
                schema: "bms",
                table: "Payments",
                column: "PaymentMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivingFundAccountId_Status",
                schema: "bms",
                table: "Payments",
                columns: new[] { "ReceivingFundAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UnitAccountId_Status",
                schema: "bms",
                table: "Payments",
                columns: new[] { "UnitAccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_AccountAdjustmentId",
                schema: "bms",
                table: "UnitReceivables",
                column: "AccountAdjustmentId",
                unique: true,
                filter: "[AccountAdjustmentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_Code",
                schema: "bms",
                table: "UnitReceivables",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_DemandAllocationId",
                schema: "bms",
                table: "UnitReceivables",
                column: "DemandAllocationId",
                unique: true,
                filter: "[DemandAllocationId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_FundAccountId",
                schema: "bms",
                table: "UnitReceivables",
                column: "FundAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_IsActive",
                schema: "bms",
                table: "UnitReceivables",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_ResponsiblePartyId",
                schema: "bms",
                table: "UnitReceivables",
                column: "ResponsiblePartyId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitReceivables_UnitAccountId_Status_DueDate",
                schema: "bms",
                table: "UnitReceivables",
                columns: new[] { "UnitAccountId", "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandAllocationRules",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandAssetEvents",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandAssets",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandBuildings",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandComplexes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandExpenses",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseAssetEvents",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseAssets",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseBuildings",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseComplexes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseDisbursementFiles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseDocuments",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "FinancialTransactionEntries",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PaymentAllocations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PaymentEvidenceFiles",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "FinancialTransactions",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitReceivables",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseDisbursements",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Payments",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "AccountAdjustments",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandAllocations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Expenses",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "PaymentMethods",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "Demands",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "ExpenseTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "DemandTypes",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "FinancialAccounts",
                schema: "bms");
        }
    }
}
