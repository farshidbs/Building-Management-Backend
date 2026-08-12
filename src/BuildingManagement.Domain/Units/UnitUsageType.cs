using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public sealed class UnitUsageType : ReferenceDataItem
{
    private UnitUsageType() { }
    public UnitUsageType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
