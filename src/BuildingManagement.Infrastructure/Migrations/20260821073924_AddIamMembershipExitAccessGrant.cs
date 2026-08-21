using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generates inline index column arrays in migrations.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIamMembershipExitAccessGrant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembershipExitRequests_MembershipId",
                schema: "bms",
                table: "MembershipExitRequests");

            migrationBuilder.DropIndex(
                name: "IX_AccessGrants_UserId_PermissionId_IsActive",
                schema: "bms",
                table: "AccessGrants");

            migrationBuilder.AddColumn<string>(
                name: "DecisionReason",
                schema: "bms",
                table: "MembershipExitRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartsAtUtc",
                schema: "bms",
                table: "AccessGrants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_MembershipId",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "MembershipId",
                unique: true,
                filter: "[IsActive] = 1 AND [StatusKey] = 'pending'");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UserId_PermissionId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessGrants",
                columns: new[] { "UserId", "PermissionId", "ComplexId", "BuildingId", "UnitId" },
                unique: true,
                filter: "[IsActive] = 1 AND [RevokedAtUtc] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MembershipExitRequests_MembershipId",
                schema: "bms",
                table: "MembershipExitRequests");

            migrationBuilder.DropIndex(
                name: "IX_AccessGrants_UserId_PermissionId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "AccessGrants");

            migrationBuilder.DropColumn(
                name: "DecisionReason",
                schema: "bms",
                table: "MembershipExitRequests");

            migrationBuilder.DropColumn(
                name: "StartsAtUtc",
                schema: "bms",
                table: "AccessGrants");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipExitRequests_MembershipId",
                schema: "bms",
                table: "MembershipExitRequests",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_AccessGrants_UserId_PermissionId_IsActive",
                schema: "bms",
                table: "AccessGrants",
                columns: new[] { "UserId", "PermissionId", "IsActive" });
        }
    }
}
