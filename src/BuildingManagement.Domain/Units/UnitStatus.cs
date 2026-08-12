using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public sealed class UnitStatus : ReferenceDataItem
{
    private UnitStatus() { }
    public UnitStatus(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
