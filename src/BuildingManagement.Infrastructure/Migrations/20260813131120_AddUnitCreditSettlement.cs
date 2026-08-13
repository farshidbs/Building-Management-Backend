using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitCreditSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableCredit",
                schema: "bms",
                table: "FinancialAccounts",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "UnitCreditSettlements",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitAccountId = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "varchar(5)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitCreditSettlements", x => x.Id);
                    table.CheckConstraint("CK_UnitCreditSettlements_CodeFormat", "[Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5");
                    table.ForeignKey(
                        name: "FK_UnitCreditSettlements_FinancialAccounts_UnitAccountId",
                        column: x => x.UnitAccountId,
                        principalSchema: "bms",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitCreditSettlementAllocations",
                schema: "bms",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitCreditSettlementId = table.Column<long>(type: "bigint", nullable: false),
                    UnitReceivableId = table.Column<long>(type: "bigint", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnitCreditSettlementAllocations", x => x.Id);
                    table.CheckConstraint("CK_UnitCreditSettlementAllocations_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_UnitCreditSettlementAllocations_UnitCreditSettlements_UnitCreditSettlementId",
                        column: x => x.UnitCreditSettlementId,
                        principalSchema: "bms",
                        principalTable: "UnitCreditSettlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitCreditSettlementAllocations_UnitReceivables_UnitReceivableId",
                        column: x => x.UnitReceivableId,
                        principalSchema: "bms",
                        principalTable: "UnitReceivables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_FinancialAccounts_AvailableCredit",
                schema: "bms",
                table: "FinancialAccounts",
                sql: "[AvailableCredit] >= 0 AND ([UnitId] IS NOT NULL OR [AvailableCredit] = 0)");

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlementAllocations_UnitCreditSettlementId_UnitReceivableId",
                schema: "bms",
                table: "UnitCreditSettlementAllocations",
                columns: new[] { "UnitCreditSettlementId", "UnitReceivableId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlementAllocations_UnitReceivableId",
                schema: "bms",
                table: "UnitCreditSettlementAllocations",
                column: "UnitReceivableId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlements_Code",
                schema: "bms",
                table: "UnitCreditSettlements",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlements_IsActive",
                schema: "bms",
                table: "UnitCreditSettlements",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlements_UnitAccountId",
                schema: "bms",
                table: "UnitCreditSettlements",
                column: "UnitAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnitCreditSettlementAllocations",
                schema: "bms");

            migrationBuilder.DropTable(
                name: "UnitCreditSettlements",
                schema: "bms");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FinancialAccounts_AvailableCredit",
                schema: "bms",
                table: "FinancialAccounts");

            migrationBuilder.DropColumn(
                name: "AvailableCredit",
                schema: "bms",
                table: "FinancialAccounts");
        }
    }
}
