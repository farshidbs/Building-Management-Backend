namespace BuildingManagement.Domain;

public sealed class AssetDocument : AssetFileRelation
{
    private AssetDocument() { }
    public long AssetId { get; private set; }
    public long DocumentTypeId { get; private set; }
    public string Title { get; private set; } = "";
    public string? DocumentNumber { get; private set; }
    public DateTimeOffset? DocumentDate { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? Description { get; private set; }
    public bool IsConfidential { get; private set; }
    public AssetDocument(long assetId, long storedFileId, long documentTypeId, string title,
        string? documentNumber, DateTimeOffset? documentDate, DateTimeOffset? effectiveFrom,
        DateTimeOffset? expiresAt, string? description, bool confidential, DateTimeOffset now)
    {
        AssetId = assetId; StoredFileId = storedFileId; CreatedAtUtc = now;
        Update(documentTypeId, title, documentNumber, documentDate, effectiveFrom, expiresAt, description, confidential, now);
        UpdatedAtUtc = null;
    }
    public void Update(long typeId, string title, string? number, DateTimeOffset? date,
        DateTimeOffset? from, DateTimeOffset? expires, string? description, bool confidential, DateTimeOffset now)
    {
        if (typeId <= 0) throw new DomainValidationException("documentTypeKey", "Document type is required.");
        if (expires < from) throw new DomainValidationException("expiresAt", "Cannot be earlier than effective date.");
        DocumentTypeId = typeId; Title = string.IsNullOrWhiteSpace(title) ? throw new DomainValidationException("title", "Must not be blank.") : title.Trim();
        DocumentNumber = Optional(number); DocumentDate = date?.ToUniversalTime(); EffectiveFrom = from?.ToUniversalTime();
        ExpiresAt = expires?.ToUniversalTime(); Description = Optional(description); IsConfidential = confidential; UpdatedAtUtc = now;
    }
}
