using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional
#pragma warning disable CA1861 // Generated migration column arrays are immutable call metadata.

namespace BuildingManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIamPlatformSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EndReasonKey",
                schema: "bms",
                table: "SupportActingSessions",
                type: "varchar(40)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PlatformAuthSessionId",
                schema: "bms",
                table: "SupportActingSessions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "TicketReference",
                schema: "bms",
                table: "SupportActingSessions",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                schema: "bms",
                table: "SupportActingSessions",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                schema: "bms",
                table: "PlatformPermissions",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "recovery_case_view", 10, "مشاهده پرونده بازیابی", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "recovery_case_review", 20, "بررسی پرونده بازیابی", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "support_act", 30, "نمایندگی پشتیبانی", null },
                    { 4L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "support_act_view", 40, "مشاهده نمایندگی پشتیبانی", null },
                    { 5L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "platform_user_manage", 50, "مدیریت کاربران پلتفرم", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "PlatformRoles",
                columns: new[] { "Id", "CreatedAtUtc", "IsActive", "Key", "SortOrder", "Title", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { 1L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "super_admin", 10, "مدیر کل پلتفرم", null },
                    { 2L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "support_manager", 20, "مدیر پشتیبانی", null },
                    { 3L, new DateTimeOffset(new DateTime(2026, 8, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "support_agent", 30, "کارشناس پشتیبانی", null }
                });

            migrationBuilder.InsertData(
                schema: "bms",
                table: "PlatformRolePermissions",
                columns: new[] { "Id", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1L, 1L, 1L },
                    { 2L, 2L, 1L },
                    { 3L, 3L, 1L },
                    { 4L, 4L, 1L },
                    { 5L, 5L, 1L },
                    { 6L, 1L, 2L },
                    { 7L, 2L, 2L },
                    { 8L, 3L, 2L },
                    { 9L, 4L, 2L },
                    { 10L, 1L, 3L }
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupportActingSessions_PlatformAuthSessionId",
                schema: "bms",
                table: "SupportActingSessions",
                column: "PlatformAuthSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportActingSessions_TokenHash",
                schema: "bms",
                table: "SupportActingSessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SupportActingSessions_AuthSessions_PlatformAuthSessionId",
                schema: "bms",
                table: "SupportActingSessions",
                column: "PlatformAuthSessionId",
                principalSchema: "bms",
                principalTable: "AuthSessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupportActingSessions_AuthSessions_PlatformAuthSessionId",
                schema: "bms",
                table: "SupportActingSessions");

            migrationBuilder.DropIndex(
                name: "IX_SupportActingSessions_PlatformAuthSessionId",
                schema: "bms",
                table: "SupportActingSessions");

            migrationBuilder.DropIndex(
                name: "IX_SupportActingSessions_TokenHash",
                schema: "bms",
                table: "SupportActingSessions");

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRolePermissions",
                keyColumn: "Id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformPermissions",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformPermissions",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformPermissions",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformPermissions",
                keyColumn: "Id",
                keyValue: 4L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformPermissions",
                keyColumn: "Id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRoles",
                keyColumn: "Id",
                keyValue: 1L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRoles",
                keyColumn: "Id",
                keyValue: 2L);

            migrationBuilder.DeleteData(
                schema: "bms",
                table: "PlatformRoles",
                keyColumn: "Id",
                keyValue: 3L);

            migrationBuilder.DropColumn(
                name: "EndReasonKey",
                schema: "bms",
                table: "SupportActingSessions");

            migrationBuilder.DropColumn(
                name: "PlatformAuthSessionId",
                schema: "bms",
                table: "SupportActingSessions");

            migrationBuilder.DropColumn(
                name: "TicketReference",
                schema: "bms",
                table: "SupportActingSessions");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                schema: "bms",
                table: "SupportActingSessions");
        }
    }
}
