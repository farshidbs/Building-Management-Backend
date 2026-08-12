using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public static class ReferenceKeys
{
    public static class LocationTypes
    {
        public const string Country = "country";
        public const string StateOrProvince = "state_or_province";
        public const string City = "city";
        public const string District = "district";
        public const string Neighborhood = "neighborhood";
    }

    public static class BuildingTypes
    {
        public const string Residential = "residential";
        public const string Commercial = "commercial";
        public const string Office = "office";
        public const string Mixed = "mixed";
        public const string Other = "other";
    }

    public static class UnitUsageTypes
    {
        public const string Residential = "residential";
        public const string Commercial = "commercial";
        public const string Office = "office";
        public const string Storage = "storage";
        public const string Other = "other";
    }

    public static class UnitStatuses
    {
        public const string Available = "available";
        public const string Occupied = "occupied";
        public const string Vacant = "vacant";
        public const string UnderRenovation = "under_renovation";
        public const string Inactive = "inactive";
    }
}
