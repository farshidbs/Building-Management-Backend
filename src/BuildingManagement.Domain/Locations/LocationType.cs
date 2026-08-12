using System.Security.Cryptography;

namespace BuildingManagement.Domain;

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
