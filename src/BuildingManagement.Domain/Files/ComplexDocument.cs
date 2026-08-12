namespace BuildingManagement.Domain;

public sealed class ComplexDocument : Entity
{
    private ComplexDocument() { }

    public long ComplexId { get; private set; }
    public long StoredFileId { get; private set; }
    public long DocumentTypeId { get; private set; }
    public string Title { get; private set; } = "";
    public string? DocumentNumber { get; private set; }
    public DateTimeOffset? DocumentDate { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? Description { get; private set; }
    public bool IsConfidential { get; private set; }

    public ComplexDocument(string code, long complexId, long storedFileId, long documentTypeId,
        string title, string? documentNumber, DateTimeOffset? documentDate,
        DateTimeOffset? effectiveFrom, DateTimeOffset? expiresAt, string? description,
        bool isConfidential, bool requiresDocumentDate, bool supportsExpiration, DateTimeOffset now)
    {
        Initialize(code, now);
        ComplexId = complexId;
        StoredFileId = storedFileId;
        Update(documentTypeId, title, documentNumber, documentDate, effectiveFrom, expiresAt,
            description, isConfidential, requiresDocumentDate, supportsExpiration, now);
        UpdatedAtUtc = null;
    }

    public void Update(long documentTypeId, string title, string? documentNumber,
        DateTimeOffset? documentDate, DateTimeOffset? effectiveFrom, DateTimeOffset? expiresAt,
        string? description, bool isConfidential, bool requiresDocumentDate,
        bool supportsExpiration, DateTimeOffset now)
    {
        DocumentRules.Validate(documentDate, effectiveFrom, expiresAt, requiresDocumentDate,
            supportsExpiration);
        DocumentTypeId = documentTypeId;
        Title = Required(title, "title");
        DocumentNumber = Optional(documentNumber);
        DocumentDate = documentDate;
        EffectiveFrom = effectiveFrom;
        ExpiresAt = expiresAt;
        Description = Optional(description);
        IsConfidential = isConfidential;
        Touch(now);
    }
}
