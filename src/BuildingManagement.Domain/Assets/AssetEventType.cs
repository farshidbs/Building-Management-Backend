namespace BuildingManagement.Domain;

public sealed class AssetEventType : ReferenceDataItem
{
    private AssetEventType() { }
    public AssetEventType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
