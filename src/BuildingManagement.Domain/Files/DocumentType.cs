namespace BuildingManagement.Domain;

public sealed class DocumentType : ReferenceDataItem
{
    private DocumentType() { }

    public string? Description { get; private set; }
    public bool RequiresDocumentDate { get; private set; }
    public bool SupportsExpiration { get; private set; }

    public DocumentType(string key, string title, string? description, bool requiresDocumentDate,
        bool supportsExpiration, int sortOrder, DateTimeOffset now)
    {
        Initialize(key, title, sortOrder, now);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        RequiresDocumentDate = requiresDocumentDate;
        SupportsExpiration = supportsExpiration;
    }
}
