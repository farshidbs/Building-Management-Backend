namespace BuildingManagement.Domain;

public sealed class AssetGalleryFile : AssetFileRelation
{
    private AssetGalleryFile() { }
    public long AssetId { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }
    public AssetGalleryFile(long assetId, long storedFileId, string? title, string? description,
        string? altText, int sortOrder, bool isCover, DateTimeOffset now)
    {
        AssetId = assetId; StoredFileId = storedFileId; CreatedAtUtc = now;
        Update(title, description, altText, sortOrder, isCover, now); UpdatedAtUtc = null;
    }
    public void Update(string? title, string? description, string? altText, int sortOrder, bool cover, DateTimeOffset now)
    {
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        Title = Optional(title); Description = Optional(description); AltText = Optional(altText);
        SortOrder = sortOrder; IsCover = cover; UpdatedAtUtc = now;
    }
    public void RemoveCover(DateTimeOffset now) { if (IsCover) { IsCover = false; UpdatedAtUtc = now; } }
}
