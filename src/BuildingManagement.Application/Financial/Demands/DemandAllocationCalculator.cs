namespace BuildingManagement.Application;

public sealed record AllocationInput(string UnitCode, decimal Basis, bool Eligible,
    string ResponsibleType, string? PartyCode);

public static class DemandAllocationCalculator
{
    public static DemandPreviewResponse Calculate(IReadOnlyList<AllocationInput> units,
        DemandRuleRequest rule, IReadOnlyList<DemandOverrideRequest>? overrides)
    {
        Validate(rule, overrides);
        if (units.Count == 0) return new([], 0, 0, 0);
        var unitCodes = units.Select(x => x.UnitCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if ((overrides ?? []).Any(x => !unitCodes.Contains(x.UnitCode)))
            throw Validation("overrides", "An override references a Unit outside the Demand scope.");

        var eligible = units.Where(x => x.Eligible).ToList();
        var calculated = CalculateAmounts(eligible, rule);
        var overrideMap = (overrides ?? []).ToDictionary(x => x.UnitCode,
            StringComparer.OrdinalIgnoreCase);
        var items = units.Select(unit =>
        {
            var amount = calculated.GetValueOrDefault(unit.UnitCode);
            var adjustment = overrideMap.GetValueOrDefault(unit.UnitCode);
            var included = unit.Eligible && (adjustment?.IsIncluded ?? true);
            var final = included ? adjustment?.FinalAmount ?? amount : 0;
            if (final < 0) throw Validation("overrides", "Final amount must not be negative.");
            return new DemandAllocationResponse(unit.UnitCode, unit.Basis, amount, final,
                included, unit.ResponsibleType, unit.PartyCode, adjustment?.AdjustmentReason);
        }).ToList();

        if (rule.RedistributionPolicyKey == BuildingManagement.Domain.FinancialKeys.Redistribution.ToOthers &&
            rule.TotalAmount is decimal target)
            Redistribute(items, overrideMap, rule, target);

        var calculatedTotal = items.Sum(x => x.CalculatedAmount);
        var finalTotal = items.Sum(x => x.FinalAmount);
        return new(items, calculatedTotal, finalTotal, finalTotal - calculatedTotal);
    }

    private static void Redistribute(List<DemandAllocationResponse> items,
        Dictionary<string, DemandOverrideRequest> overrides, DemandRuleRequest rule, decimal target)
    {
        var fixedItems = items.Where(x => x.IsIncluded && overrides.ContainsKey(x.UnitCode)).ToList();
        var remaining = items.Where(x => x.IsIncluded && !overrides.ContainsKey(x.UnitCode)).ToList();
        var remainingTarget = target - fixedItems.Sum(x => x.FinalAmount);
        if (remainingTarget < 0) throw Validation("overrides", "Overrides exceed demand total.");
        if (remaining.Count == 0 && remainingTarget != 0)
            throw Validation("overrides", "At least one eligible Unit is required for redistribution.");
        var weights = remaining.Select(x => rule.AllocationMethodKey ==
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Equal ? 1m : x.BasisValue ?? 0).ToList();
        var distributed = Distribute(remainingTarget, weights);
        for (var index = 0; index < remaining.Count; index++)
        {
            var itemIndex = items.FindIndex(x => x.UnitCode == remaining[index].UnitCode);
            items[itemIndex] = remaining[index] with { FinalAmount = distributed[index] };
        }
    }

    private static Dictionary<string, decimal> CalculateAmounts(List<AllocationInput> units,
        DemandRuleRequest rule)
    {
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (rule.AllocationMethodKey == BuildingManagement.Domain.FinancialKeys.AllocationMethods.Custom)
            return result;
        if (rule.AmountModeKey is BuildingManagement.Domain.FinancialKeys.AmountModes.PerPerson or
            BuildingManagement.Domain.FinancialKeys.AmountModes.PerArea)
        {
            foreach (var unit in units) result[unit.UnitCode] = unit.Basis * (rule.RateAmount ?? 0);
            return result;
        }
        if (rule.AmountModeKey == BuildingManagement.Domain.FinancialKeys.AmountModes.PerUnit)
        {
            foreach (var unit in units) result[unit.UnitCode] = rule.RateAmount ?? 0;
            return result;
        }
        var weights = units.Select(x => rule.AllocationMethodKey ==
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Equal ? 1m : x.Basis).ToList();
        var amounts = Distribute(rule.TotalAmount ?? 0, weights);
        for (var index = 0; index < units.Count; index++) result[units[index].UnitCode] = amounts[index];
        return result;
    }

    private static decimal[] Distribute(decimal total, List<decimal> weights)
    {
        if (weights.Count == 0) return [];
        var sum = weights.Sum();
        if (sum <= 0) throw Validation("basis", "Allocation basis must be positive.");
        var result = weights.Select(x => decimal.Floor(total * x / sum * 100) / 100).ToArray();
        var cents = (int)decimal.Round((total - result.Sum()) * 100, 0, MidpointRounding.AwayFromZero);
        for (var index = 0; index < cents; index++) result[index % result.Length] += .01m;
        return result;
    }

    private static void Validate(DemandRuleRequest rule, IReadOnlyList<DemandOverrideRequest>? overrides)
    {
        if (rule is null) throw Validation("rule", "Allocation rule is required.");
        if (rule.AllocationMethodKey is not (BuildingManagement.Domain.FinancialKeys.AllocationMethods.Equal or
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Occupants or
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Area or
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Custom))
            throw Validation("allocationMethodKey", "Allocation method is invalid.");
        if (rule.RedistributionPolicyKey is not (BuildingManagement.Domain.FinancialKeys.Redistribution.None or
            BuildingManagement.Domain.FinancialKeys.Redistribution.ToOthers))
            throw Validation("redistributionPolicyKey", "Redistribution policy is invalid.");
        var validCombination = rule.AllocationMethodKey switch
        {
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Equal =>
                rule.AmountModeKey is BuildingManagement.Domain.FinancialKeys.AmountModes.Total or BuildingManagement.Domain.FinancialKeys.AmountModes.PerUnit,
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Occupants =>
                rule.AmountModeKey is BuildingManagement.Domain.FinancialKeys.AmountModes.Total or BuildingManagement.Domain.FinancialKeys.AmountModes.PerPerson,
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Area =>
                rule.AmountModeKey is BuildingManagement.Domain.FinancialKeys.AmountModes.Total or BuildingManagement.Domain.FinancialKeys.AmountModes.PerArea,
            BuildingManagement.Domain.FinancialKeys.AllocationMethods.Custom =>
                rule.AmountModeKey == BuildingManagement.Domain.FinancialKeys.AmountModes.Total,
            _ => false
        };
        if (!validCombination) throw Validation("amountModeKey", "Amount mode is not supported for the selected allocation method.");
        if (rule.RedistributionPolicyKey == BuildingManagement.Domain.FinancialKeys.Redistribution.ToOthers &&
            rule.AmountModeKey != BuildingManagement.Domain.FinancialKeys.AmountModes.Total)
            throw Validation("redistributionPolicyKey", "Redistribution is only supported for fixed-total allocation.");
        if ((overrides ?? []).GroupBy(x => x.UnitCode, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
            throw Validation("overrides", "Each Unit may be overridden once.");
    }

    private static AppException Validation(string field, string message) => new(400,
        "validation.failed", "One or more validation errors occurred.",
        new Dictionary<string, string[]> { [field] = [message] });
}
