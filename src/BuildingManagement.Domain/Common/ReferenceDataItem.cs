using System.Security.Cryptography;

namespace BuildingManagement.Domain;

public abstract class ReferenceDataItem
{
    public long Id { get; protected set; }
    public string Key { get; private set; } = "";
    public string Title { get; private set; } = "";
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; protected set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    protected void Initialize(string key, string title, int sortOrder, DateTimeOffset now)
    {
        Key = NormalizeKey(key);
        Title = Required(title, "title");
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        SortOrder = sortOrder;
        CreatedAtUtc = now;
    }

    public void Update(string title, int sortOrder, bool isActive, DateTimeOffset now)
    {
        Title = Required(title, "title");
        if (sortOrder < 0) throw new DomainValidationException("sortOrder", "Must not be negative.");
        SortOrder = sortOrder;
        IsActive = isActive;
        UpdatedAtUtc = now;
    }

    private static string NormalizeKey(string value)
    {
        var key = Required(value, "key").ToLowerInvariant();
        if (key.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            throw new DomainValidationException("key", "Key may contain lowercase English letters, digits, and underscores only.");
        return key;
    }

    protected static string Required(string value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new DomainValidationException(field, "Must not be blank.") : value.Trim();
}
