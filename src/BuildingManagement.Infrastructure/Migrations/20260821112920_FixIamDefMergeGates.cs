using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generates inline index column arrays.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixIamDefMergeGates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccessGrants_UserId_PermissionId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessGrants");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UserId_PermissionId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessGrants",
                columns: new[] { "UserId", "PermissionId", "ComplexId", "BuildingId", "UnitId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AccessGrants_UserId_PermissionId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessGrants");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UserId_PermissionId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessGrants",
                columns: new[] { "UserId", "PermissionId", "ComplexId", "BuildingId", "UnitId" },
                unique: true,
                filter: "[IsActive] = 1 AND [RevokedAtUtc] IS NULL");
        }
    }
}
