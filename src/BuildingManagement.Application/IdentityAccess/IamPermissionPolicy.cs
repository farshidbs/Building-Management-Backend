namespace BuildingManagement.Application;

public static class IamPermissionPolicy
{
    private static readonly HashSet<string> Overrideable = new(StringComparer.Ordinal)
    {
        "unit_view", "party_view", "occupancy_view", "asset_view", "asset_event_view",
        "expense_view", "demand_view", "payment_view", "financial_unit_view_other_summary"
    };

    private static readonly HashSet<string> Grantable = new(StringComparer.Ordinal)
    {
        "financial_unit_view_own", "financial_unit_pay"
    };

    public static bool CanOverrideAtBuilding(string permissionKey) =>
        Overrideable.Contains(Normalize(permissionKey));

    public static bool CanGrantToIndividual(string permissionKey) =>
        Grantable.Contains(Normalize(permissionKey));

    public static void RequireOverrideable(string permissionKey)
    {
        if (!CanOverrideAtBuilding(permissionKey))
            throw new AppException(400, "permission.not_overrideable", "This permission cannot be overridden at Building scope.");
    }

    public static void RequireGrantable(string permissionKey)
    {
        if (!CanGrantToIndividual(permissionKey))
            throw new AppException(400, "permission.not_grantable", "This permission cannot be granted individually.");
    }

    private static string Normalize(string value) => value?.Trim().ToLowerInvariant() ?? "";
}
