using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861 // EF Core generated migration metadata uses inline column arrays.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIamInvitationWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AcceptedByUserId",
                schema: "bms",
                table: "Invitations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvitationTypeKey",
                schema: "bms",
                table: "Invitations",
                type: "varchar(30)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "SourceUnitPartyRelationId",
                schema: "bms",
                table: "Invitations",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_AcceptedByUserId",
                schema: "bms",
                table: "Invitations",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_NormalizedIdentifierValue_RoleId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "Invitations",
                columns: new[] { "NormalizedIdentifierValue", "RoleId", "ComplexId", "BuildingId", "UnitId" },
                unique: true,
                filter: "[AcceptedAtUtc] IS NULL AND [RevokedAtUtc] IS NULL AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_SourceUnitPartyRelationId",
                schema: "bms",
                table: "Invitations",
                column: "SourceUnitPartyRelationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_UnitPartyRelations_SourceUnitPartyRelationId",
                schema: "bms",
                table: "Invitations",
                column: "SourceUnitPartyRelationId",
                principalSchema: "bms",
                principalTable: "UnitPartyRelations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Users_AcceptedByUserId",
                schema: "bms",
                table: "Invitations",
                column: "AcceptedByUserId",
                principalSchema: "bms",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_UnitPartyRelations_SourceUnitPartyRelationId",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Users_AcceptedByUserId",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_AcceptedByUserId",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_NormalizedIdentifierValue_RoleId_ComplexId_BuildingId_UnitId",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_SourceUnitPartyRelationId",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "AcceptedByUserId",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "InvitationTypeKey",
                schema: "bms",
                table: "Invitations");

            migrationBuilder.DropColumn(
                name: "SourceUnitPartyRelationId",
                schema: "bms",
                table: "Invitations");
        }
    }
}
