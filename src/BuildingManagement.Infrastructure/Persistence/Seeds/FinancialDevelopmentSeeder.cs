using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Infrastructure;

public static class FinancialDevelopmentSeeder
{
    public static async Task SeedAsync(BuildingManagementDbContext db, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var buildings = await db.Buildings.Where(x => x.IsActive).OrderBy(x => x.Id).Take(2).ToListAsync(ct);
        var units = await db.Units.Where(x => x.IsActive).OrderBy(x => x.Id).Take(8).ToListAsync(ct);
        if (buildings.Count == 0 || units.Count == 0) return;

        foreach (var unit in units)
            if (!await db.FinancialAccounts.AnyAsync(x => x.UnitId == unit.Id, ct))
                db.FinancialAccounts.Add(new FinancialAccount(unit.Id, null, null,
                    FinancialKeys.AccountKinds.Unit, now));

        foreach (var building in buildings)
            foreach (var kind in new[] { FinancialKeys.AccountKinds.CurrentFund, FinancialKeys.AccountKinds.ReserveFund })
                if (!await db.FinancialAccounts.AnyAsync(x => x.BuildingId == building.Id && x.AccountKindKey == kind, ct))
                    db.FinancialAccounts.Add(new FinancialAccount(null, building.Id, null, kind, now));

        var complexIds = buildings.Where(x => x.ComplexId.HasValue).Select(x => x.ComplexId!.Value).Distinct();
        foreach (var complexId in complexIds)
            foreach (var kind in new[] { FinancialKeys.AccountKinds.CurrentFund, FinancialKeys.AccountKinds.ReserveFund })
                if (!await db.FinancialAccounts.AnyAsync(x => x.ComplexId == complexId && x.AccountKindKey == kind, ct))
                    db.FinancialAccounts.Add(new FinancialAccount(null, null, complexId, kind, now));
        await db.SaveChangesAsync(ct);

        var firstBuilding = buildings[0];
        if (!await db.ExpenseTypes.AnyAsync(x => x.BuildingId == firstBuilding.Id && x.Title == "سرویس آسانسور", ct))
            db.ExpenseTypes.Add(new ExpenseType(null, "سرویس آسانسور", null, firstBuilding.Id, 100, now));
        if (firstBuilding.ComplexId is long parentComplexId &&
            !await db.ExpenseTypes.AnyAsync(x => x.ComplexId == parentComplexId && x.Title == "نگهداری فضای سبز", ct))
            db.ExpenseTypes.Add(new ExpenseType(null, "نگهداری فضای سبز", parentComplexId, null, 110, now));
        await db.SaveChangesAsync(ct);

        if (!await db.Expenses.AnyAsync(x => x.Title == "قبض برق مشاعات - داده نمایشی", ct))
        {
            var electricityId = await db.ExpenseTypes.Where(x => x.Key == "electricity").Select(x => x.Id).SingleAsync(ct);
            db.Expenses.Add(new Expense(await UniqueCode(db.Expenses, ct), firstBuilding.Id, null,
                electricityId, null, "قبض برق مشاعات - داده نمایشی", 12_500_000m, now.AddDays(-5),
                now.AddDays(10), "نمونه هزینه ثبت‌شده برای نمایش بخش مالی", now));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Demands.AnyAsync(x => x.Title == "شارژ ماهانه - داده نمایشی", ct))
        {
            var fundId = await db.FinancialAccounts.Where(x => x.BuildingId == firstBuilding.Id &&
                x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund).Select(x => x.Id).SingleAsync(ct);
            var demandTypeId = await db.DemandTypes.Where(x => x.Key == "monthly_charge").Select(x => x.Id).SingleAsync(ct);
            var demand = new Demand(await UniqueCode(db.Demands, ct), fundId, demandTypeId,
                "شارژ ماهانه - داده نمایشی", "پیش‌نویس قابل ویرایش برای دموی تخصیص شارژ",
                now, now.AddDays(15), now);
            db.Demands.Add(demand);
            await db.SaveChangesAsync(ct);
            db.DemandAllocationRules.Add(new DemandAllocationRule(demand.Id,
                FinancialKeys.AllocationMethods.Equal, FinancialKeys.AmountModes.PerUnit, null,
                2_000_000m, true, FinancialKeys.ResponsibleParties.Owner,
                FinancialKeys.Redistribution.None, "تقسیم مساوی بین واحدها"));
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task<string> UniqueCode<TEntity>(IQueryable<TEntity> entities, CancellationToken ct)
        where TEntity : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await entities.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new InvalidOperationException("Financial demo code generation failed.");
    }
}
