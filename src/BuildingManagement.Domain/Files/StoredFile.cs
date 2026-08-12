namespace BuildingManagement.Domain;

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
