using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuildingManagement.Infrastructure.Migrations;

[DbContext(typeof(BuildingManagementDbContext))]
[Migration("202608060001_NormalizeReferenceData")]
public sealed class NormalizeReferenceData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE [LocationTypes] (
              [Id] bigint IDENTITY(1,1) NOT NULL, [Key] varchar(50) NOT NULL,
              [Title] nvarchar(100) NOT NULL, [ParentId] bigint NULL, [SortOrder] int NOT NULL,
              [IsActive] bit NOT NULL, [CreatedAtUtc] datetimeoffset(0) NOT NULL,
              [UpdatedAtUtc] datetimeoffset(0) NULL, [RowVersion] rowversion NOT NULL,
              CONSTRAINT [PK_LocationTypes] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_LocationTypes_KeyFormat] CHECK ([Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0),
              CONSTRAINT [FK_LocationTypes_LocationTypes_ParentId] FOREIGN KEY ([ParentId])
                REFERENCES [LocationTypes] ([Id]) ON DELETE NO ACTION);
            CREATE UNIQUE INDEX [IX_LocationTypes_Key] ON [LocationTypes] ([Key]);
            CREATE INDEX [IX_LocationTypes_ParentId] ON [LocationTypes] ([ParentId]);
            CREATE INDEX [IX_LocationTypes_IsActive_SortOrder] ON [LocationTypes] ([IsActive], [SortOrder]);

            DECLARE @Now datetimeoffset(0) = SYSUTCDATETIME();
            INSERT INTO [LocationTypes] ([Key],[Title],[ParentId],[SortOrder],[IsActive],[CreatedAtUtc])
              VALUES ('country',N'کشور',NULL,10,1,@Now);
            DECLARE @Country bigint = SCOPE_IDENTITY();
            INSERT INTO [LocationTypes] ([Key],[Title],[ParentId],[SortOrder],[IsActive],[CreatedAtUtc])
              VALUES ('state_or_province',N'استان یا ایالت',@Country,20,1,@Now);
            DECLARE @Province bigint = SCOPE_IDENTITY();
            INSERT INTO [LocationTypes] ([Key],[Title],[ParentId],[SortOrder],[IsActive],[CreatedAtUtc])
              VALUES ('city',N'شهر',@Province,30,1,@Now);
            DECLARE @City bigint = SCOPE_IDENTITY();
            INSERT INTO [LocationTypes] ([Key],[Title],[ParentId],[SortOrder],[IsActive],[CreatedAtUtc])
              VALUES ('district',N'منطقه',@City,40,1,@Now);
            DECLARE @District bigint = SCOPE_IDENTITY();
            INSERT INTO [LocationTypes] ([Key],[Title],[ParentId],[SortOrder],[IsActive],[CreatedAtUtc])
              VALUES ('neighborhood',N'محله',@District,50,1,@Now);

            CREATE TABLE [BuildingTypes] (
              [Id] bigint IDENTITY(1,1) NOT NULL, [Key] varchar(50) NOT NULL,
              [Title] nvarchar(100) NOT NULL, [SortOrder] int NOT NULL, [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL, [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL, CONSTRAINT [PK_BuildingTypes] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_BuildingTypes_KeyFormat] CHECK ([Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0));
            CREATE UNIQUE INDEX [IX_BuildingTypes_Key] ON [BuildingTypes] ([Key]);
            CREATE INDEX [IX_BuildingTypes_IsActive_SortOrder] ON [BuildingTypes] ([IsActive],[SortOrder]);
            INSERT INTO [BuildingTypes] ([Key],[Title],[SortOrder],[IsActive],[CreatedAtUtc]) VALUES
              ('residential',N'مسکونی',10,1,@Now),('commercial',N'تجاری',20,1,@Now),
              ('office',N'اداری',30,1,@Now),('mixed',N'مختلط',40,1,@Now),('other',N'سایر',50,1,@Now);

            CREATE TABLE [UnitUsageTypes] (
              [Id] bigint IDENTITY(1,1) NOT NULL, [Key] varchar(50) NOT NULL,
              [Title] nvarchar(100) NOT NULL, [SortOrder] int NOT NULL, [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL, [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL, CONSTRAINT [PK_UnitUsageTypes] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_UnitUsageTypes_KeyFormat] CHECK ([Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0));
            CREATE UNIQUE INDEX [IX_UnitUsageTypes_Key] ON [UnitUsageTypes] ([Key]);
            CREATE INDEX [IX_UnitUsageTypes_IsActive_SortOrder] ON [UnitUsageTypes] ([IsActive],[SortOrder]);
            INSERT INTO [UnitUsageTypes] ([Key],[Title],[SortOrder],[IsActive],[CreatedAtUtc]) VALUES
              ('residential',N'مسکونی',10,1,@Now),('commercial',N'تجاری',20,1,@Now),
              ('office',N'اداری',30,1,@Now),('storage',N'انباری',40,1,@Now),('other',N'سایر',50,1,@Now);

            CREATE TABLE [UnitStatuses] (
              [Id] bigint IDENTITY(1,1) NOT NULL, [Key] varchar(50) NOT NULL,
              [Title] nvarchar(100) NOT NULL, [SortOrder] int NOT NULL, [IsActive] bit NOT NULL,
              [CreatedAtUtc] datetimeoffset(0) NOT NULL, [UpdatedAtUtc] datetimeoffset(0) NULL,
              [RowVersion] rowversion NOT NULL, CONSTRAINT [PK_UnitStatuses] PRIMARY KEY ([Id]),
              CONSTRAINT [CK_UnitStatuses_KeyFormat] CHECK ([Key] NOT LIKE '%[^a-z0-9_]%' AND LEN([Key]) > 0));
            CREATE UNIQUE INDEX [IX_UnitStatuses_Key] ON [UnitStatuses] ([Key]);
            CREATE INDEX [IX_UnitStatuses_IsActive_SortOrder] ON [UnitStatuses] ([IsActive],[SortOrder]);
            INSERT INTO [UnitStatuses] ([Key],[Title],[SortOrder],[IsActive],[CreatedAtUtc]) VALUES
              ('available',N'آماده واگذاری',10,1,@Now),('occupied',N'در حال استفاده',20,1,@Now),
              ('vacant',N'خالی',30,1,@Now),('under_renovation',N'در حال بازسازی',40,1,@Now),
              ('inactive',N'غیرفعال',50,1,@Now);
            """);

        migrationBuilder.Sql("""
            ALTER TABLE [Locations] ADD [LocationTypeId] bigint NULL;
            ALTER TABLE [Buildings] ADD [BuildingTypeId] bigint NULL;
            ALTER TABLE [Units] ADD [UsageTypeId] bigint NULL, [StatusId] bigint NULL;
            """);

        migrationBuilder.Sql("""
            UPDATE [Locations] SET [LocationTypeId] = CASE [Type]
              WHEN 'Country' THEN (SELECT [Id] FROM [LocationTypes] WHERE [Key]='country')
              WHEN 'StateOrProvince' THEN (SELECT [Id] FROM [LocationTypes] WHERE [Key]='state_or_province')
              WHEN 'City' THEN (SELECT [Id] FROM [LocationTypes] WHERE [Key]='city')
              WHEN 'District' THEN (SELECT [Id] FROM [LocationTypes] WHERE [Key]='district')
              WHEN 'Neighborhood' THEN (SELECT [Id] FROM [LocationTypes] WHERE [Key]='neighborhood') END;
            ALTER TABLE [Locations] ALTER COLUMN [LocationTypeId] bigint NOT NULL;
            DROP INDEX [IX_Locations_ParentId_Type_NormalizedName] ON [Locations];
            ALTER TABLE [Locations] DROP COLUMN [Type];
            ALTER TABLE [Locations] ADD CONSTRAINT [FK_Locations_LocationTypes_LocationTypeId]
              FOREIGN KEY ([LocationTypeId]) REFERENCES [LocationTypes]([Id]) ON DELETE NO ACTION;
            CREATE INDEX [IX_Locations_LocationTypeId] ON [Locations]([LocationTypeId]);
            CREATE UNIQUE INDEX [IX_Locations_ParentId_LocationTypeId_NormalizedName]
              ON [Locations]([ParentId],[LocationTypeId],[NormalizedName]);

            UPDATE [Buildings] SET [BuildingTypeId] = CASE [BuildingType]
              WHEN 'Residential' THEN (SELECT [Id] FROM [BuildingTypes] WHERE [Key]='residential')
              WHEN 'Commercial' THEN (SELECT [Id] FROM [BuildingTypes] WHERE [Key]='commercial')
              WHEN 'Office' THEN (SELECT [Id] FROM [BuildingTypes] WHERE [Key]='office')
              WHEN 'Mixed' THEN (SELECT [Id] FROM [BuildingTypes] WHERE [Key]='mixed')
              WHEN 'Other' THEN (SELECT [Id] FROM [BuildingTypes] WHERE [Key]='other') END;
            ALTER TABLE [Buildings] ALTER COLUMN [BuildingTypeId] bigint NOT NULL;
            ALTER TABLE [Buildings] DROP COLUMN [BuildingType];
            ALTER TABLE [Buildings] ADD CONSTRAINT [FK_Buildings_BuildingTypes_BuildingTypeId]
              FOREIGN KEY ([BuildingTypeId]) REFERENCES [BuildingTypes]([Id]) ON DELETE NO ACTION;
            CREATE INDEX [IX_Buildings_BuildingTypeId] ON [Buildings]([BuildingTypeId]);

            UPDATE [Units] SET
              [UsageTypeId] = CASE [UsageType]
                WHEN 'Residential' THEN (SELECT [Id] FROM [UnitUsageTypes] WHERE [Key]='residential')
                WHEN 'Commercial' THEN (SELECT [Id] FROM [UnitUsageTypes] WHERE [Key]='commercial')
                WHEN 'Office' THEN (SELECT [Id] FROM [UnitUsageTypes] WHERE [Key]='office')
                WHEN 'Storage' THEN (SELECT [Id] FROM [UnitUsageTypes] WHERE [Key]='storage')
                WHEN 'Other' THEN (SELECT [Id] FROM [UnitUsageTypes] WHERE [Key]='other') END,
              [StatusId] = CASE [Status]
                WHEN 'Available' THEN (SELECT [Id] FROM [UnitStatuses] WHERE [Key]='available')
                WHEN 'Occupied' THEN (SELECT [Id] FROM [UnitStatuses] WHERE [Key]='occupied')
                WHEN 'Vacant' THEN (SELECT [Id] FROM [UnitStatuses] WHERE [Key]='vacant')
                WHEN 'UnderRenovation' THEN (SELECT [Id] FROM [UnitStatuses] WHERE [Key]='under_renovation')
                WHEN 'Inactive' THEN (SELECT [Id] FROM [UnitStatuses] WHERE [Key]='inactive') END;
            ALTER TABLE [Units] ALTER COLUMN [UsageTypeId] bigint NOT NULL;
            ALTER TABLE [Units] ALTER COLUMN [StatusId] bigint NOT NULL;
            ALTER TABLE [Units] DROP COLUMN [UsageType], [Status];
            ALTER TABLE [Units] ADD CONSTRAINT [FK_Units_UnitUsageTypes_UsageTypeId]
              FOREIGN KEY ([UsageTypeId]) REFERENCES [UnitUsageTypes]([Id]) ON DELETE NO ACTION;
            ALTER TABLE [Units] ADD CONSTRAINT [FK_Units_UnitStatuses_StatusId]
              FOREIGN KEY ([StatusId]) REFERENCES [UnitStatuses]([Id]) ON DELETE NO ACTION;
            CREATE INDEX [IX_Units_UsageTypeId] ON [Units]([UsageTypeId]);
            CREATE INDEX [IX_Units_StatusId] ON [Units]([StatusId]);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        throw new NotSupportedException(
            "Reference-data normalization preserves semantic keys and requires an explicit reviewed rollback migration.");
}
