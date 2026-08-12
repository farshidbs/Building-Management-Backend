namespace BuildingManagement.Domain;

public sealed class AssetType : ReferenceDataItem
{
    private AssetType() { }
    public AssetType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
