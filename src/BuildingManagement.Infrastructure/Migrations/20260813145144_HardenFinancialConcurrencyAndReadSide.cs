using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenFinancialConcurrencyAndReadSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AvailableCreditAfter",
                schema: "bms",
                table: "UnitCreditSettlements",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "BankTrackingCode",
                schema: "bms",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UnitCreditSettlements_AvailableCreditAfter",
                schema: "bms",
                table: "UnitCreditSettlements",
                sql: "[AvailableCreditAfter] >= 0");

            migrationBuilder.CreateIndex(
                name: "UX_Payments_Fund_BankTrackingCode",
                schema: "bms",
                table: "Payments",
                columns: new[] { "ReceivingFundAccountId", "BankTrackingCode" },
                unique: true,
                filter: "[BankTrackingCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UnitCreditSettlements_AvailableCreditAfter",
                schema: "bms",
                table: "UnitCreditSettlements");

            migrationBuilder.DropIndex(
                name: "UX_Payments_Fund_BankTrackingCode",
                schema: "bms",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "AvailableCreditAfter",
                schema: "bms",
                table: "UnitCreditSettlements");

            migrationBuilder.AlterColumn<string>(
                name: "BankTrackingCode",
                schema: "bms",
                table: "Payments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");
        }
    }
}
