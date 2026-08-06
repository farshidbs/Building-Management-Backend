using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public static class PublicCode
{
    public const int Length = 5;
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Create()
    {
        Span<char> code = stackalloc char[Length];
        for (var index = 0; index < code.Length; index++)
            code[index] = Characters[RandomNumberGenerator.GetInt32(Characters.Length)];
        return new string(code);
    }

    public static string Normalize(string value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToUpperInvariant();
        if (normalized.Length != Length || normalized.Any(character => !Characters.Contains(character)))
            throw new DomainValidationException("code", "Code must contain exactly five uppercase English letters or digits.");
        return normalized;
    }
}

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

public abstract class ReferenceDataItem
{
    public long Id { get; protected set; }
    public string Key { get; private set; } = "";
    public string Title { get; private set; } = "";
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    protected void Initialize(string key, string title, int sortOrder, DateTimeOffset now)
    {
        Key = NormalizeKey(key);
        Title = Required(title, "title");
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        SortOrder = sortOrder;
        CreatedAtUtc = now;
    }

    public void Update(string title, int sortOrder, bool isActive, DateTimeOffset now)
    {
        Title = Required(title, "title");
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    private static string NormalizeKey(string value)
    {
        var key = Required(value, "key").ToLowerInvariant();
        if (key.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            throw new DomainValidationException("key", "Key may contain lowercase English letters, digits, and underscores only.");
        return key;
    }

    protected static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException(field, "Must not be blank.") : value.Trim();
}

public sealed class LocationType : ReferenceDataItem
{
    private LocationType() { }
    public long? ParentId { get; private set; }
    public LocationType(string key, string title, long? parentId, int sortOrder, DateTimeOffset now)
    {
        Initialize(key, title, sortOrder, now);
        ParentId = parentId;
    }
}

public sealed class BuildingType : ReferenceDataItem
{
    private BuildingType() { }
    public BuildingType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class UnitUsageType : ReferenceDataItem
{
    private UnitUsageType() { }
    public UnitUsageType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public sealed class UnitStatus : ReferenceDataItem
{
    private UnitStatus() { }
    public UnitStatus(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}

public abstract class Entity
{
    public long Id { get; protected set; }
    public string Code { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];

    protected void Initialize(string code, DateTimeOffset now)
    {
        Code = PublicCode.Normalize(code);
        CreatedAtUtc = now;
    }

    public void SetActivation(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    protected void Touch(DateTimeOffset now) => UpdatedAtUtc = now;
    protected static string Required(string value, string name) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException(name, "Must not be blank.") : value.Trim();
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    protected static void Coordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude is < -90 or > 90) throw new DomainValidationException("latitude", "Must be between -90 and 90.");
        if (longitude is < -180 or > 180) throw new DomainValidationException("longitude", "Must be between -180 and 180.");
    }
}

public sealed class Location : Entity
{
    private Location() { }
    public long? ParentId { get; private set; }
    public long LocationTypeId { get; private set; }
    public string Name { get; private set; } = "";
    public string NormalizedName { get; private set; } = "";

    public Location(string code, long? parentId, long locationTypeId, string name, DateTimeOffset now)
    {
        Initialize(code, now);
        Update(parentId, locationTypeId, name, now);
        UpdatedAtUtc = null;
    }

    public void Update(long? parentId, long locationTypeId, string name, DateTimeOffset now)
    {
        if (Id != 0 && parentId == Id) throw new DomainValidationException("parentCode", "A location cannot be its own parent.");
        ParentId = parentId;
        LocationTypeId = locationTypeId;
        Name = Required(name, "name");
        NormalizedName = Name.ToUpperInvariant();
        Touch(now);
    }
}

public sealed class Complex : Entity
{
    private Complex() { }
    public long LocationId { get; private set; }
    public string Name { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? Description { get; private set; }

    public Complex(string code, long locationId, string name, string address, string postalCode,
        decimal? latitude, decimal? longitude, string? description, DateTimeOffset now)
    {
        Initialize(code, now);
        Update(locationId, name, address, postalCode, latitude, longitude, description, now);
        UpdatedAtUtc = null;
    }

    public void Update(long locationId, string name, string address, string postalCode,
        decimal? latitude, decimal? longitude, string? description, DateTimeOffset now)
    {
        Coordinates(latitude, longitude);
        LocationId = locationId;
        Name = Required(name, "name");
        Address = Required(address, "address");
        PostalCode = Required(postalCode, "postalCode");
        Latitude = latitude;
        Longitude = longitude;
        Description = Optional(description);
        Touch(now);
    }
}

public sealed class Building : Entity
{
    private Building() { }
    public long? ComplexId { get; private set; }
    public long LocationId { get; private set; }
    public long BuildingTypeId { get; private set; }
    public string Name { get; private set; } = "";
    public string Address { get; private set; } = "";
    public string PostalCode { get; private set; } = "";
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public int? FloorsCount { get; private set; }
    public int? ConstructionYear { get; private set; }
    public string? Description { get; private set; }

    public Building(string code, long? complexId, long locationId, long buildingTypeId, string name,
        string address, string postalCode, decimal? latitude, decimal? longitude, int? floorsCount,
        int? constructionYear, string? description, DateTimeOffset now)
    {
        Initialize(code, now);
        Update(complexId, locationId, buildingTypeId, name, address, postalCode, latitude, longitude,
            floorsCount, constructionYear, description, now);
        UpdatedAtUtc = null;
    }

    public void Update(long? complexId, long locationId, long buildingTypeId, string name, string address,
        string postalCode, decimal? latitude, decimal? longitude, int? floorsCount, int? constructionYear,
        string? description, DateTimeOffset now)
    {
        Coordinates(latitude, longitude);
        if (floorsCount < 0) throw new DomainValidationException("floorsCount", "Must not be negative.");
        if (constructionYear is < 1000 or > 9999) throw new DomainValidationException("constructionYear", "Must be a four-digit year.");
        ComplexId = complexId;
        LocationId = locationId;
        BuildingTypeId = buildingTypeId;
        Name = Required(name, "name");
        Address = Required(address, "address");
        PostalCode = Required(postalCode, "postalCode");
        Latitude = latitude;
        Longitude = longitude;
        FloorsCount = floorsCount;
        ConstructionYear = constructionYear;
        Description = Optional(description);
        Touch(now);
    }
}

public sealed class Unit : Entity
{
    private Unit() { }
    public long BuildingId { get; private set; }
    public long UsageTypeId { get; private set; }
    public long StatusId { get; private set; }
    public string UnitNumber { get; private set; } = "";
    public string NormalizedUnitNumber { get; private set; } = "";
    public int? FloorNumber { get; private set; }
    public decimal? Area { get; private set; }
    public int? RoomsCount { get; private set; }
    public int ParkingCount { get; private set; }
    public int StorageCount { get; private set; }
    public string? Description { get; private set; }

    public Unit(string code, long buildingId, long usageTypeId, long statusId, string unitNumber,
        int? floorNumber, decimal? area, int? roomsCount, int parkingCount, int storageCount,
        string? description, DateTimeOffset now)
    {
        Initialize(code, now);
        BuildingId = buildingId;
        Update(usageTypeId, statusId, unitNumber, floorNumber, area, roomsCount, parkingCount, storageCount, description, now);
        UpdatedAtUtc = null;
    }

    public void Update(long usageTypeId, long statusId, string unitNumber, int? floorNumber, decimal? area,
        int? roomsCount, int parkingCount, int storageCount, string? description, DateTimeOffset now)
    {
        if (area < 0) throw new DomainValidationException("area", "Must not be negative.");
        if (roomsCount < 0) throw new DomainValidationException("roomsCount", "Must not be negative.");
        if (parkingCount < 0) throw new DomainValidationException("parkingCount", "Must not be negative.");
        if (storageCount < 0) throw new DomainValidationException("storageCount", "Must not be negative.");
        UsageTypeId = usageTypeId;
        StatusId = statusId;
        UnitNumber = Required(unitNumber, "unitNumber");
        NormalizedUnitNumber = UnitNumber.ToUpperInvariant();
        FloorNumber = floorNumber;
        Area = area;
        RoomsCount = roomsCount;
        ParkingCount = parkingCount;
        StorageCount = storageCount;
        Description = Optional(description);
        Touch(now);
    }
}

public sealed class DomainValidationException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
