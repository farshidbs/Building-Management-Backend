namespace BuildingManagement.Domain;

public static class AssetReferenceKeys
{
    public static class Types
    {
        public const string Elevator = "elevator";
        public const string WaterPump = "water_pump";
        public const string Boiler = "boiler";
        public const string Generator = "generator";
        public const string ParkingDoor = "parking_door";
        public const string FireAlarm = "fire_alarm";
        public const string FireExtinguisher = "fire_extinguisher";
        public const string Camera = "camera";
        public const string Other = "other";
    }

    public static class EventTypes
    {
        public const string Inspection = "inspection";
        public const string Maintenance = "maintenance";
        public const string Repair = "repair";
        public const string Replacement = "replacement";
        public const string Installation = "installation";
        public const string Incident = "incident";
        public const string Other = "other";
    }
}
