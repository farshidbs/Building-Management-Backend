using System.Security.Cryptography;

namespace BuildingManagement.Domain;

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
