namespace BuildingManagement.Domain;

public sealed class BuildingGalleryFile : Entity
{
    private BuildingGalleryFile() { }

    public long BuildingId { get; private set; }
    public long StoredFileId { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }

    public BuildingGalleryFile(string code, long buildingId, long storedFileId, string? title,
        string? description, string? altText, int sortOrder, bool isCover, DateTimeOffset now)
    {
        Initialize(code, now);
        BuildingId = buildingId;
        StoredFileId = storedFileId;
        Update(title, description, altText, sortOrder, isCover, now);
        UpdatedAtUtc = null;
    }

    public void Update(string? title, string? description, string? altText, int sortOrder,
        bool isCover, DateTimeOffset now)
    {
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        Title = Optional(title);
        Description = Optional(description);
        AltText = Optional(altText);
        SortOrder = sortOrder;
        IsCover = isCover;
        Touch(now);
    }

    public void RemoveCover(DateTimeOffset now)
    {
        if (!IsCover) return;
        IsCover = false;
        Touch(now);
    }
}
