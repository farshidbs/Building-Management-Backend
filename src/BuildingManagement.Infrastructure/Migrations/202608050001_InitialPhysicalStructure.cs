using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuildingManagement.Infrastructure.Migrations;

[DbContext(typeof(BuildingManagementDbContext))]
[Migration("202608050001_InitialPhysicalStructure")]
public sealed class InitialPhysicalStructure : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE [Locations] (
              [Id] bigint IDENTITY(1,1) NOT NULL,
              [Code] varchar(5) NOT NULL,
              [ParentId] bigint NULL,
              [Name] nvarchar(200) NOT NULL,
              [NormalizedName] nvarchar(200) NOT NULL,
              [Type] nvarchar(30) NOT NULL,
              [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL,
              [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL,
              CONSTRAINT [PK_Locations] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_Locations_CodeFormat] CHECK ([Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5),
              CONSTRAINT [CK_Locations_NotSelfParent] CHECK ([ParentId] IS NULL OR [ParentId] <> [Id]),
              CONSTRAINT [FK_Locations_Locations_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION);
            CREATE UNIQUE INDEX [IX_Locations_Code] ON [Locations] ([Code]);
            CREATE UNIQUE INDEX [IX_Locations_ParentId_Type_NormalizedName] ON [Locations] ([ParentId], [Type], [NormalizedName]);
            CREATE INDEX [IX_Locations_ParentId] ON [Locations] ([ParentId]);
            CREATE INDEX [IX_Locations_IsActive] ON [Locations] ([IsActive]);

            CREATE TABLE [Complexes] (
              [Id] bigint IDENTITY(1,1) NOT NULL,
              [Code] varchar(5) NOT NULL,
              [LocationId] bigint NOT NULL,
              [Name] nvarchar(200) NOT NULL,
              [Address] nvarchar(500) NOT NULL,
              [PostalCode] nvarchar(30) NOT NULL,
              [Latitude] decimal(9,6) NULL,
              [Longitude] decimal(9,6) NULL,
              [Description] nvarchar(2000) NULL,
              [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL,
              [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL,
              CONSTRAINT [PK_Complexes] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_Complexes_CodeFormat] CHECK ([Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5),
              CONSTRAINT [FK_Complexes_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION);
            CREATE UNIQUE INDEX [IX_Complexes_Code] ON [Complexes] ([Code]);
            CREATE INDEX [IX_Complexes_LocationId] ON [Complexes] ([LocationId]);
            CREATE INDEX [IX_Complexes_IsActive] ON [Complexes] ([IsActive]);

            CREATE TABLE [Buildings] (
              [Id] bigint IDENTITY(1,1) NOT NULL,
              [Code] varchar(5) NOT NULL,
              [ComplexId] bigint NULL,
              [LocationId] bigint NOT NULL,
              [Name] nvarchar(200) NOT NULL,
              [Address] nvarchar(500) NOT NULL,
              [PostalCode] nvarchar(30) NOT NULL,
              [Latitude] decimal(9,6) NULL,
              [Longitude] decimal(9,6) NULL,
              [FloorsCount] int NULL,
              [BuildingType] nvarchar(30) NOT NULL,
              [ConstructionYear] int NULL,
              [Description] nvarchar(2000) NULL,
              [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL,
              [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL,
              CONSTRAINT [PK_Buildings] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_Buildings_CodeFormat] CHECK ([Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5),
              CONSTRAINT [CK_Buildings_Floors] CHECK ([FloorsCount] IS NULL OR [FloorsCount] >= 0),
              CONSTRAINT [FK_Buildings_Complexes_ComplexId] FOREIGN KEY ([ComplexId]) REFERENCES [Complexes] ([Id]) ON DELETE NO ACTION,
              CONSTRAINT [FK_Buildings_Locations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [Locations] ([Id]) ON DELETE NO ACTION);
            CREATE UNIQUE INDEX [IX_Buildings_Code] ON [Buildings] ([Code]);
            CREATE INDEX [IX_Buildings_ComplexId] ON [Buildings] ([ComplexId]);
            CREATE INDEX [IX_Buildings_LocationId] ON [Buildings] ([LocationId]);
            CREATE INDEX [IX_Buildings_IsActive] ON [Buildings] ([IsActive]);

            CREATE TABLE [Units] (
              [Id] bigint IDENTITY(1,1) NOT NULL,
              [Code] varchar(5) NOT NULL,
              [BuildingId] bigint NOT NULL,
              [UnitNumber] nvarchar(50) NOT NULL,
              [NormalizedUnitNumber] nvarchar(50) NOT NULL,
              [FloorNumber] int NULL,
              [Area] decimal(12,2) NULL,
              [RoomsCount] int NULL,
              [ParkingCount] int NOT NULL,
              [StorageCount] int NOT NULL,
              [UsageType] nvarchar(30) NOT NULL,
              [Status] nvarchar(30) NOT NULL,
              [Description] nvarchar(2000) NULL,
              [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL,
              [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL,
              CONSTRAINT [PK_Units] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_Units_CodeFormat] CHECK ([Code] NOT LIKE '%[^A-Z0-9]%' AND LEN([Code]) = 5),
              CONSTRAINT [CK_Units_Area] CHECK ([Area] IS NULL OR [Area] >= 0),
              CONSTRAINT [CK_Units_Counts] CHECK (([RoomsCount] IS NULL OR [RoomsCount] >= 0) AND [ParkingCount] >= 0 AND [StorageCount] >= 0),
              CONSTRAINT [FK_Units_Buildings_BuildingId] FOREIGN KEY ([BuildingId]) REFERENCES [Buildings] ([Id]) ON DELETE NO ACTION);
            CREATE UNIQUE INDEX [IX_Units_Code] ON [Units] ([Code]);
            CREATE UNIQUE INDEX [IX_Units_BuildingId_NormalizedUnitNumber] ON [Units] ([BuildingId], [NormalizedUnitNumber]);
            CREATE INDEX [IX_Units_BuildingId_FloorNumber] ON [Units] ([BuildingId], [FloorNumber]);
            CREATE INDEX [IX_Units_IsActive] ON [Units] ([IsActive]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP TABLE [Units]; DROP TABLE [Buildings]; DROP TABLE [Complexes]; DROP TABLE [Locations];");
}
