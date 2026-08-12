namespace BuildingManagement.Domain;

public sealed class AssetEventFile : AssetFileRelation
{
    private AssetEventFile() { }
    public long AssetEventId { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public AssetEventFile(long eventId, long storedFileId, string? title, string? description, DateTimeOffset now)
    { AssetEventId = eventId; StoredFileId = storedFileId; Title = Optional(title); Description = Optional(description); CreatedAtUtc = now; }
}
