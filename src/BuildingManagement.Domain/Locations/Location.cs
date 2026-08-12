using System.Security.Cryptography;

namespace BuildingManagement.Domain;

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
