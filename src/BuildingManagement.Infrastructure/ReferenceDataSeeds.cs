using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal static class ReferenceDataSeed
{
    internal static readonly DateTimeOffset CreatedAtUtc = new(2026, 8, 5, 0, 0, 0, TimeSpan.Zero);
}

internal sealed class LocationTypeSeedConfiguration : IEntityTypeConfiguration<LocationType>
{
    public void Configure(EntityTypeBuilder<LocationType> builder) =>
        builder.HasData(
            new { Id = 1L, Key = "country", Title = "کشور", ParentId = (long?)null, SortOrder = 10, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 2L, Key = "state_or_province", Title = "استان یا ایالت", ParentId = (long?)1, SortOrder = 20, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 3L, Key = "city", Title = "شهر", ParentId = (long?)2, SortOrder = 30, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 4L, Key = "district", Title = "منطقه", ParentId = (long?)3, SortOrder = 40, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 5L, Key = "neighborhood", Title = "محله", ParentId = (long?)4, SortOrder = 50, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc });
}

internal sealed class BuildingTypeSeedConfiguration : IEntityTypeConfiguration<BuildingType>
{
    public void Configure(EntityTypeBuilder<BuildingType> builder) =>
        builder.HasData(
            new { Id = 1L, Key = "residential", Title = "مسکونی", SortOrder = 10, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 2L, Key = "commercial", Title = "تجاری", SortOrder = 20, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 3L, Key = "office", Title = "اداری", SortOrder = 30, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 4L, Key = "mixed", Title = "مختلط", SortOrder = 40, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 5L, Key = "other", Title = "سایر", SortOrder = 50, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc });
}

internal sealed class UnitUsageTypeSeedConfiguration : IEntityTypeConfiguration<UnitUsageType>
{
    public void Configure(EntityTypeBuilder<UnitUsageType> builder) =>
        builder.HasData(
            new { Id = 1L, Key = "residential", Title = "مسکونی", SortOrder = 10, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 2L, Key = "commercial", Title = "تجاری", SortOrder = 20, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 3L, Key = "office", Title = "اداری", SortOrder = 30, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 4L, Key = "storage", Title = "انباری", SortOrder = 40, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 5L, Key = "other", Title = "سایر", SortOrder = 50, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc });
}

internal sealed class UnitStatusSeedConfiguration : IEntityTypeConfiguration<UnitStatus>
{
    public void Configure(EntityTypeBuilder<UnitStatus> builder) =>
        builder.HasData(
            new { Id = 1L, Key = "available", Title = "آماده واگذاری", SortOrder = 10, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 2L, Key = "occupied", Title = "در حال استفاده", SortOrder = 20, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 3L, Key = "vacant", Title = "خالی", SortOrder = 30, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 4L, Key = "under_renovation", Title = "در حال بازسازی", SortOrder = 40, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc },
            new { Id = 5L, Key = "inactive", Title = "غیرفعال", SortOrder = 50, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc });
}
