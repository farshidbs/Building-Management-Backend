using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Infrastructure;

public static class DevelopmentSeeder
{
    public static async Task SeedAsync(BuildingManagementDbContext db, CancellationToken ct)
    {
        if (await db.Locations.AnyAsync(ct)) return;

        var now = DateTimeOffset.UtcNow;
        var countryTypeId = await ReferenceId(db.LocationTypes, ReferenceKeys.LocationTypes.Country, ct);
        var provinceTypeId = await ReferenceId(db.LocationTypes, ReferenceKeys.LocationTypes.StateOrProvince, ct);
        var cityTypeId = await ReferenceId(db.LocationTypes, ReferenceKeys.LocationTypes.City, ct);
        var residentialBuildingTypeId = await ReferenceId(db.BuildingTypes, ReferenceKeys.BuildingTypes.Residential, ct);
        var mixedBuildingTypeId = await ReferenceId(db.BuildingTypes, ReferenceKeys.BuildingTypes.Mixed, ct);
        var residentialUsageTypeId = await ReferenceId(db.UnitUsageTypes, ReferenceKeys.UnitUsageTypes.Residential, ct);
        var commercialUsageTypeId = await ReferenceId(db.UnitUsageTypes, ReferenceKeys.UnitUsageTypes.Commercial, ct);
        var occupiedStatusId = await ReferenceId(db.UnitStatuses, ReferenceKeys.UnitStatuses.Occupied, ct);
        var availableStatusId = await ReferenceId(db.UnitStatuses, ReferenceKeys.UnitStatuses.Available, ct);
        var vacantStatusId = await ReferenceId(db.UnitStatuses, ReferenceKeys.UnitStatuses.Vacant, ct);

        var locationCodes = new HashSet<string>(StringComparer.Ordinal);
        var country = new Location(await CreateUniqueCode(db.Locations, locationCodes, ct), null,
            countryTypeId, "ایران", now);
        db.Locations.Add(country);
        await db.SaveChangesAsync(ct);

        var province = new Location(await CreateUniqueCode(db.Locations, locationCodes, ct), country.Id,
            provinceTypeId, "تهران", now);
        db.Locations.Add(province);
        await db.SaveChangesAsync(ct);

        var city = new Location(await CreateUniqueCode(db.Locations, locationCodes, ct), province.Id,
            cityTypeId, "تهران", now);
        db.Locations.Add(city);
        await db.SaveChangesAsync(ct);

        var complex = new Complex(
            await CreateUniqueCode(db.Complexes, new HashSet<string>(StringComparer.Ordinal), ct),
            city.Id,
            "مجتمع مسکونی سرو",
            "تهران، سعادت‌آباد، بلوار دریا، خیابان صراف‌ها",
            "۱۹۹۸۸۱۲۳۴۵",
            35.785m,
            51.375m,
            "داده نمونه فارسی برای محیط توسعه",
            now);
        db.Complexes.Add(complex);
        await db.SaveChangesAsync(ct);

        var buildingCodes = new HashSet<string>(StringComparer.Ordinal);
        var tower = new Building(
            await CreateUniqueCode(db.Buildings, buildingCodes, ct),
            complex.Id,
            city.Id,
            residentialBuildingTypeId,
            "برج سرو",
            "تهران، سعادت‌آباد، بلوار دریا، مجتمع سرو",
            "۱۹۹۸۸۱۲۳۴۵",
            35.785m,
            51.375m,
            10,
            2020,
            "برج مسکونی نمونه",
            now);
        var independent = new Building(
            await CreateUniqueCode(db.Buildings, buildingCodes, ct),
            null,
            city.Id,
            mixedBuildingTypeId,
            "ساختمان مستقل بهار",
            "تهران، خیابان بهار، کوچه یاس",
            "۱۵۶۷۸۹۴۳۲۱",
            35.716m,
            51.426m,
            3,
            2018,
            "ساختمان مستقل مسکونی و تجاری",
            now);
        db.Buildings.AddRange(tower, independent);
        await db.SaveChangesAsync(ct);

        var unitCodes = new HashSet<string>(StringComparer.Ordinal);
        db.Units.AddRange(
            new Unit(await CreateUniqueCode(db.Units, unitCodes, ct), tower.Id,
                residentialUsageTypeId, occupiedStatusId, "۱۰۱", 1, 95, 2, 1, 0,
                "واحد مسکونی دوخوابه", now),
            new Unit(await CreateUniqueCode(db.Units, unitCodes, ct), tower.Id,
                residentialUsageTypeId, availableStatusId, "A-2", 1, 88, 2, 1, 1,
                "واحد مسکونی آماده واگذاری", now),
            new Unit(await CreateUniqueCode(db.Units, unitCodes, ct), independent.Id,
                commercialUsageTypeId, vacantStatusId, "مغازه-۰۴", 0, 40, null, 0, 0,
                "واحد تجاری طبقه همکف", now));
        await db.SaveChangesAsync(ct);
    }

    private static async Task<long> ReferenceId<TEntity>(IQueryable<TEntity> entities, string key, CancellationToken ct)
        where TEntity : ReferenceDataItem =>
        await entities.Where(entity => entity.Key == key).Select(entity => (long?)entity.Id).SingleOrDefaultAsync(ct)
        ?? throw new InvalidOperationException($"Required reference data '{key}' is missing.");

    private static async Task<string> CreateUniqueCode<TEntity>(
        IQueryable<TEntity> entities,
        HashSet<string> reserved,
        CancellationToken ct)
        where TEntity : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!reserved.Add(code)) continue;
            if (!await entities.AnyAsync(entity => entity.Code == code, ct)) return code;
            reserved.Remove(code);
        }
        throw new InvalidOperationException("A unique public code could not be generated for sample data.");
    }
}
