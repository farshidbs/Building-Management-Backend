using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public sealed class BuildingType : ReferenceDataItem
{
    private BuildingType() { }
    public BuildingType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
