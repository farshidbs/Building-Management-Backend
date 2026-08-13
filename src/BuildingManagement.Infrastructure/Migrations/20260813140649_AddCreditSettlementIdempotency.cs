using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditSettlementIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitCreditSettlements_UnitAccountId",
                schema: "bms",
                table: "UnitCreditSettlements");

            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                schema: "bms",
                table: "UnitCreditSettlements",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlements_UnitAccountId_RequestId",
                schema: "bms",
                table: "UnitCreditSettlements",
                columns: new[] { "UnitAccountId", "RequestId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UnitCreditSettlements_UnitAccountId_RequestId",
                schema: "bms",
                table: "UnitCreditSettlements");

            migrationBuilder.DropColumn(
                name: "RequestId",
                schema: "bms",
                table: "UnitCreditSettlements");

            migrationBuilder.CreateIndex(
                name: "IX_UnitCreditSettlements_UnitAccountId",
                schema: "bms",
                table: "UnitCreditSettlements",
                column: "UnitAccountId");
        }
    }
}
