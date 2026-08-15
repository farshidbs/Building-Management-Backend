using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIamAccountRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NewNormalizedIdentifier",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BirthDateEvidence",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "IdentityNumberEvidence",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MobileVerifiedAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OldLoginMethodId",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OldNormalizedIdentifier",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceHash",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReviewedAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE [bms].[AccountRecoveryCases]
                SET [ReferenceHash] = CONCAT('legacy-', [Id])
                WHERE [ReferenceHash] = '';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCases_OldLoginMethodId",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "OldLoginMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRecoveryCases_ReferenceHash",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "ReferenceHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccountRecoveryCases_UserLoginMethods_OldLoginMethodId",
                schema: "bms",
                table: "AccountRecoveryCases",
                column: "OldLoginMethodId",
                principalSchema: "bms",
                principalTable: "UserLoginMethods",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccountRecoveryCases_UserLoginMethods_OldLoginMethodId",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropIndex(
                name: "IX_AccountRecoveryCases_OldLoginMethodId",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropIndex(
                name: "IX_AccountRecoveryCases_ReferenceHash",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "BirthDateEvidence",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "CompletedAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "IdentityNumberEvidence",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "MobileVerifiedAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "OldLoginMethodId",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "OldNormalizedIdentifier",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "ReferenceHash",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.DropColumn(
                name: "ReviewedAtUtc",
                schema: "bms",
                table: "AccountRecoveryCases");

            migrationBuilder.AlterColumn<string>(
                name: "NewNormalizedIdentifier",
                schema: "bms",
                table: "AccountRecoveryCases",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
