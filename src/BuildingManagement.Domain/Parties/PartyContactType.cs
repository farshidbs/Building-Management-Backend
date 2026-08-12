namespace BuildingManagement.Domain;

public sealed class PartyContactType : ReferenceDataItem
{
    private PartyContactType() { }
    public PartyContactType(string key, string title, int sortOrder, DateTimeOffset now) =>
        Initialize(key, title, sortOrder, now);
}
