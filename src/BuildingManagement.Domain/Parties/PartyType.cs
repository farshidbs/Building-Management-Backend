namespace BuildingManagement.Domain;

public sealed class PartyType : ReferenceDataItem
{
    private PartyType() { }
    public PartyType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
