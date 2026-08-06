namespace BuildingManagement.Domain;

public static class StorageProviders
{
    public const string Local = "local";
}

public static class DocumentTypeKeys
{
    public const string BoardMeetingMinutes = "board_meeting_minutes";
    public const string ManagementApproval = "management_approval";
    public const string Contract = "contract";
    public const string BuildingPlan = "building_plan";
    public const string OwnershipDocument = "ownership_document";
    public const string Permit = "permit";
    public const string Invoice = "invoice";
    public const string InsurancePolicy = "insurance_policy";
    public const string OfficialLetter = "official_letter";
    public const string Other = "other";
}

public sealed class StoredFile : Entity
{
    private StoredFile() { }

    public string OriginalFileName { get; private set; } = "";
    public string StoredFileName { get; private set; } = "";
    public string StorageKey { get; private set; } = "";
    public string ContentType { get; private set; } = "";
    public string FileExtension { get; private set; } = "";
    public long FileSizeBytes { get; private set; }
    public string? ChecksumSha256 { get; private set; }
    public string StorageProvider { get; private set; } = "";

    public StoredFile(string code, string originalFileName, string storedFileName, string storageKey,
        string contentType, string fileExtension, long fileSizeBytes, string? checksumSha256,
        string storageProvider, DateTimeOffset now)
    {
        Initialize(code, now);
        OriginalFileName = Required(originalFileName, "originalFileName");
        StoredFileName = Required(storedFileName, "storedFileName");
        StorageKey = Required(storageKey, "storageKey");
        ContentType = Required(contentType, "contentType").ToLowerInvariant();
        FileExtension = NormalizeExtension(fileExtension);
        if (fileSizeBytes <= 0)
            throw new DomainValidationException("file", "File size must be greater than zero.");
        FileSizeBytes = fileSizeBytes;
        ChecksumSha256 = string.IsNullOrWhiteSpace(checksumSha256)
            ? null
            : checksumSha256.Trim().ToLowerInvariant();
        StorageProvider = Required(storageProvider, "storageProvider").ToLowerInvariant();
    }

    private static string NormalizeExtension(string value)
    {
        var extension = Required(value, "fileExtension").ToLowerInvariant();
        if (!extension.StartsWith('.') ||
            extension.Length is < 2 or > 10 ||
            extension.Skip(1).Any(character => !char.IsAsciiLetterOrDigit(character)))
            throw new DomainValidationException("fileExtension", "File extension is invalid.");
        return extension;
    }
}

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

public sealed class ComplexGalleryFile : Entity
{
    private ComplexGalleryFile() { }

    public long ComplexId { get; private set; }
    public long StoredFileId { get; private set; }
    public string? Title { get; private set; }
    public string? Description { get; private set; }
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }

    public ComplexGalleryFile(string code, long complexId, long storedFileId, string? title,
        string? description, string? altText, int sortOrder, bool isCover, DateTimeOffset now)
    {
        Initialize(code, now);
        ComplexId = complexId;
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

public sealed class BuildingDocument : Entity
{
    private BuildingDocument() { }

    public long BuildingId { get; private set; }
    public long StoredFileId { get; private set; }
    public long DocumentTypeId { get; private set; }
    public string Title { get; private set; } = "";
    public string? DocumentNumber { get; private set; }
    public DateTimeOffset? DocumentDate { get; private set; }
    public DateTimeOffset? EffectiveFrom { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public string? Description { get; private set; }
    public bool IsConfidential { get; private set; }

    public BuildingDocument(string code, long buildingId, long storedFileId, long documentTypeId,
        string title, string? documentNumber, DateTimeOffset? documentDate,
        DateTimeOffset? effectiveFrom, DateTimeOffset? expiresAt, string? description,
        bool isConfidential, bool requiresDocumentDate, bool supportsExpiration, DateTimeOffset now)
    {
        Initialize(code, now);
        BuildingId = buildingId;
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

internal static class DocumentRules
{
    internal static void Validate(DateTimeOffset? documentDate, DateTimeOffset? effectiveFrom,
        DateTimeOffset? expiresAt, bool requiresDocumentDate, bool supportsExpiration)
    {
        if (requiresDocumentDate && !documentDate.HasValue)
            throw new DomainValidationException("documentDate", "Document date is required for this document type.");
        if (expiresAt.HasValue && !supportsExpiration)
            throw new DomainValidationException("expiresAt", "This document type does not support expiration.");
        if (effectiveFrom.HasValue && expiresAt < effectiveFrom)
            throw new DomainValidationException("expiresAt", "Expiration cannot be earlier than effective date.");
    }
}
