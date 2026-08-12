using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Infrastructure;

public static class AssetDevelopmentSeeder
{
    public static async Task SeedAsync(BuildingManagementDbContext db, CancellationToken ct)
    {
        if (await db.Assets.AnyAsync(ct)) return;
        var complex = await db.Complexes.Where(x => x.IsActive).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
        var buildings = await db.Buildings.Where(x => x.IsActive).OrderBy(x => x.Id).Take(2).ToListAsync(ct);
        if (complex is null || buildings.Count == 0) return;
        var now = DateTimeOffset.UtcNow;
        async Task<long> Type(string key) => await db.AssetTypes.Where(x => x.Key == key).Select(x => x.Id).SingleAsync(ct);
        async Task<long> EventType(string key) => await db.AssetEventTypes.Where(x => x.Key == key).Select(x => x.Id).SingleAsync(ct);
        async Task<string> Code() { for (var i = 0; i < 20; i++) { var value = PublicCode.Create(); if (!await db.Assets.AnyAsync(x => x.Code == value, ct)) return value; } throw new InvalidOperationException("Asset sample code generation failed."); }

        var elevator = new Asset(await Code(), await Type(AssetReferenceKeys.Types.Elevator), null, buildings[0].Id,
            "آسانسور مسافربر شماره یک", "بهفر", "BH-8P", "ELV-1402-001", new(2023, 4, 10, 0, 0, 0, TimeSpan.Zero),
            new(2023, 3, 20, 0, 0, 0, TimeSpan.Zero), 30, "آسانسور هشت‌نفره ورودی اصلی ساختمان", now);
        var pump = new Asset(await Code(), await Type(AssetReferenceKeys.Types.WaterPump), null, buildings[0].Id,
            "بوستر پمپ آب ساختمان", "پنتاکس", "CBT 200", "PMP-98341", new(2022, 9, 1, 0, 0, 0, TimeSpan.Zero), null, 90,
            "مجموعه دو پمپه تأمین فشار آب واحدها", now);
        var generator = new Asset(await Code(), await Type(AssetReferenceKeys.Types.Generator), complex.Id, null,
            "دیزل ژنراتور برق اضطراری مجتمع", "پرکینز", "1106A", "GEN-77420", new(2021, 6, 15, 0, 0, 0, TimeSpan.Zero),
            new(2021, 5, 20, 0, 0, 0, TimeSpan.Zero), 60, "تأمین برق اضطراری مشاعات و آسانسورها", now);
        var fireAlarm = new Asset(await Code(), await Type(AssetReferenceKeys.Types.FireAlarm), null, buildings[0].Id,
            "مرکز کنترل اعلام حریق", "زیتکس", "ZX-1800", "FA-22019", new(2023, 1, 12, 0, 0, 0, TimeSpan.Zero), null, 120,
            "پنل مرکزی متصل به آشکارسازهای طبقات", now);
        var parkingDoor = new Asset(await Code(), await Type(AssetReferenceKeys.Types.ParkingDoor), complex.Id, null,
            "درب اتوماتیک پارکینگ شرقی", "سیماران", "فرازمحور", null, new(2024, 2, 5, 0, 0, 0, TimeSpan.Zero), null, 60,
            "ورودی خودرو سمت شرقی مجتمع", now);
        db.Assets.AddRange(elevator, pump, generator, fireAlarm, parkingDoor); await db.SaveChangesAsync(ct);

        db.AssetEvents.AddRange(
            new AssetEvent(elevator.Id, await EventType(AssetReferenceKeys.EventTypes.Installation), elevator.InstallationDate!.Value,
                "نصب و راه‌اندازی آسانسور", "تحویل اولیه و تست ایمنی انجام شد.", null, null, 185_000_000m, now),
            new AssetEvent(elevator.Id, await EventType(AssetReferenceKeys.EventTypes.Maintenance), now.AddDays(-12),
                "سرویس دوره‌ای آسانسور", "روغن‌کاری ریل‌ها، تنظیم درب و کنترل تابلو فرمان انجام شد.", now.AddDays(18), null, 3_500_000m, now),
            new AssetEvent(pump.Id, await EventType(AssetReferenceKeys.EventTypes.Repair), now.AddDays(-25),
                "تعویض سیل مکانیکی پمپ دوم", "نشتی برطرف و فشار خروجی آزمایش شد.", null, null, 7_800_000m, now),
            new AssetEvent(generator.Id, await EventType(AssetReferenceKeys.EventTypes.Inspection), now.AddDays(-40),
                "بازرسی و تست زیر بار", "سطح روغن، باتری و عملکرد تابلو چنج‌اور بررسی شد.", now.AddDays(20), null, null, now),
            new AssetEvent(fireAlarm.Id, await EventType(AssetReferenceKeys.EventTypes.Inspection), now.AddDays(-8),
                "آزمایش سامانه اعلام حریق", "آژیرها، شستی‌ها و حسگرهای راهروها تست شدند.", now.AddDays(112), null, 2_200_000m, now),
            new AssetEvent(parkingDoor.Id, await EventType(AssetReferenceKeys.EventTypes.Repair), now.AddDays(-4),
                "تنظیم جک و تعویض چشمی", "چشمی ایمنی معیوب تعویض و زمان بسته‌شدن تنظیم شد.", now.AddDays(56), null, 4_600_000m, now));
        await db.SaveChangesAsync(ct);
    }
}
