using System.Security.Cryptography;

namespace BuildingManagement.Domain;

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
