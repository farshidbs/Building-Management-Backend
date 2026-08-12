namespace BuildingManagement.Domain;

public abstract class AssetFileRelation
{
    public long Id { get; protected set; }
    public long StoredFileId { get; protected set; }
    public bool IsActive { get; protected set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; protected set; }
    public byte[] RowVersion { get; private set; } = [];
    public void Deactivate(DateTimeOffset now) { IsActive = false; UpdatedAtUtc = now; }
    protected static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
