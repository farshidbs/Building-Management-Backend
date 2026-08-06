using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuildingManagement.Infrastructure.Migrations;

[DbContext(typeof(BuildingManagementDbContext))]
[Migration("202608060002_UseBmsSchema")]
public sealed class UseBmsSchema : Migration
{
    private static readonly string[] Tables =
    [
        "LocationTypes",
        "BuildingTypes",
        "UnitUsageTypes",
        "UnitStatuses",
        "Locations",
        "Complexes",
        "Buildings",
        "Units"
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "bms");

        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"""
                DECLARE @sourceSchema sysname = (
                    SELECT SCHEMA_NAME([schema_id])
                    FROM sys.tables
                    WHERE [name] = N'{table}'
                );

                IF @sourceSchema IS NULL
                    THROW 50001, 'Required table {table} was not found.', 1;

                IF @sourceSchema <> N'bms'
                BEGIN
                    DECLARE @sql nvarchar(max) = N'ALTER SCHEMA [bms] TRANSFER ' + QUOTENAME(@sourceSchema) + N'.[{table}]';
                    EXEC sys.sp_executesql @sql;
                END;
                """);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"""
                IF OBJECT_ID(N'[bms].[{table}]') IS NOT NULL
                    ALTER SCHEMA [dbo] TRANSFER [bms].[{table}];
                """);
        }
    }
}
