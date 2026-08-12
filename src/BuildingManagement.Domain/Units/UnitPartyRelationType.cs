namespace BuildingManagement.Domain;

public sealed class UnitPartyRelationType : ReferenceDataItem
{
    private UnitPartyRelationType() { }
    public string? Description { get; private set; }
    public bool IsOwnershipRelation { get; private set; }
    public bool IsOccupancyRelation { get; private set; }

    public UnitPartyRelationType(string key, string title, string? description, bool isOwnershipRelation,
        bool isOccupancyRelation, int sortOrder, DateTimeOffset now)
    {
        Initialize(key, title, sortOrder, now);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsOwnershipRelation = isOwnershipRelation;
        IsOccupancyRelation = isOccupancyRelation;
    }
}
