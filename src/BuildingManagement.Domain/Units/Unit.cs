using System.Security.Cryptography;

namespace BuildingManagement.Domain;

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
    public int CurrentOccupantsCount { get; private set; }
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

    public void ChangeOccupancy(int occupantsCount, DateTimeOffset now)
    {
        if (occupantsCount < 0)
            throw new DomainValidationException("occupantsCount", "Must not be negative.");
        CurrentOccupantsCount = occupantsCount;
        Touch(now);
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
