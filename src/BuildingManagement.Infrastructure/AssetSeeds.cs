using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildingManagement.Infrastructure;

internal sealed class AssetTypeSeedConfiguration : IEntityTypeConfiguration<AssetType>
{
    public void Configure(EntityTypeBuilder<AssetType> builder) => builder.HasData(
        Item(1, AssetReferenceKeys.Types.Elevator, "آسانسور", 10), Item(2, AssetReferenceKeys.Types.WaterPump, "پمپ آب", 20),
        Item(3, AssetReferenceKeys.Types.Boiler, "دیگ و موتورخانه", 30), Item(4, AssetReferenceKeys.Types.Generator, "ژنراتور", 40),
        Item(5, AssetReferenceKeys.Types.ParkingDoor, "درب پارکینگ", 50), Item(6, AssetReferenceKeys.Types.FireAlarm, "سامانه اعلام حریق", 60),
        Item(7, AssetReferenceKeys.Types.FireExtinguisher, "کپسول آتش‌نشانی", 70), Item(8, AssetReferenceKeys.Types.Camera, "دوربین مداربسته", 80),
        Item(9, AssetReferenceKeys.Types.Other, "سایر", 90));
    private static object Item(long id, string key, string title, int order) => new { Id = id, Key = key, Title = title, SortOrder = order, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc };
}

internal sealed class AssetEventTypeSeedConfiguration : IEntityTypeConfiguration<AssetEventType>
{
    public void Configure(EntityTypeBuilder<AssetEventType> builder) => builder.HasData(
        Item(1, AssetReferenceKeys.EventTypes.Inspection, "بازرسی", 10), Item(2, AssetReferenceKeys.EventTypes.Maintenance, "سرویس و نگهداری", 20),
        Item(3, AssetReferenceKeys.EventTypes.Repair, "تعمیر", 30), Item(4, AssetReferenceKeys.EventTypes.Replacement, "تعویض", 40),
        Item(5, AssetReferenceKeys.EventTypes.Installation, "نصب", 50), Item(6, AssetReferenceKeys.EventTypes.Incident, "خرابی یا حادثه", 60),
        Item(7, AssetReferenceKeys.EventTypes.Other, "سایر", 70));
    private static object Item(long id, string key, string title, int order) => new { Id = id, Key = key, Title = title, SortOrder = order, IsActive = true, CreatedAtUtc = ReferenceDataSeed.CreatedAtUtc };
}
