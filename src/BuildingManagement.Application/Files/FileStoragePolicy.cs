using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public static class FileStoragePolicy
{
    private static readonly Dictionary<string, string[]> ContentTypes =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".png"] = ["image/png"],
            [".webp"] = ["image/webp"],
            [".pdf"] = ["application/pdf"],
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xls"] = ["application/vnd.ms-excel"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
        };

    public static string CreateStorageKey(string ownerFolder, string ownerCode, string category,
        string storedFileName)
    {
        ownerCode = PublicCode.Normalize(ownerCode);
        ValidateSegment(ownerFolder, nameof(ownerFolder));
        ValidateSegment(category, nameof(category));
        ValidateSegment(storedFileName, nameof(storedFileName));
        return $"{ownerFolder}/{ownerCode}/{category}/{storedFileName}";
    }

    public static string CreateStorageKey(string ownerFolder, string ownerCode, string category,
        string childFolder, string storedFileName)
    {
        ownerCode = PublicCode.Normalize(ownerCode);
        ValidateSegment(ownerFolder, nameof(ownerFolder));
        ValidateSegment(category, nameof(category));
        ValidateSegment(childFolder, nameof(childFolder));
        ValidateSegment(storedFileName, nameof(storedFileName));
        return $"{ownerFolder}/{ownerCode}/{category}/{childFolder}/{storedFileName}";
    }

    public static ValidatedFile ValidateImage(IncomingFile file, FileStorageOptions options) =>
        Validate(file, options.MaximumImageFileSizeBytes, options.AllowedImageExtensions, true);

    public static ValidatedFile ValidateDocument(IncomingFile file, FileStorageOptions options) =>
        Validate(file, options.MaximumDocumentFileSizeBytes, options.AllowedDocumentExtensions, false);

    public static string SanitizeDownloadName(string value, string extension)
    {
        var name = value.Replace('\\', '/').Split('/').LastOrDefault() ?? "";
        name = new string(name.Where(character => !char.IsControl(character) && character is not '"' and not ';').ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(name)) name = $"download{extension}";
        return name.Length <= 255 ? name : name[..255];
    }

    private static ValidatedFile Validate(IncomingFile file, long maximumSize,
        IEnumerable<string> allowedExtensions, bool image)
    {
        if (file.Content is null)
            throw Validation("file", "A file is required.");
        if (file.Length <= 0)
            throw Validation("file", "File must not be empty.");
        if (maximumSize <= 0)
            throw new InvalidOperationException("Configured maximum file size must be positive.");
        if (file.Length > maximumSize)
            throw new AppException(413, "file.too_large", "The uploaded file exceeds the configured size limit.",
                new Dictionary<string, string[]> { ["file"] = [$"Maximum size is {maximumSize} bytes."] });

        var originalName = SanitizeDownloadName(file.OriginalFileName, "");
        var dotIndex = originalName.LastIndexOf('.');
        var extension = dotIndex < 0 ? "" : originalName[dotIndex..].ToLowerInvariant();
        var configured = allowedExtensions.Select(NormalizeExtension).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!configured.Contains(extension) || !ContentTypes.TryGetValue(extension, out var compatibleContentTypes) ||
            image && !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".png", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            throw Validation("file", $"The '{extension}' file extension is not supported.");

        var contentType = (file.ContentType ?? "").Split(';', 2)[0].Trim().ToLowerInvariant();
        if (!compatibleContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw Validation("file", "The declared content type is not compatible with the file extension.");

        return new(originalName, extension, contentType, file.Length);
    }

    private static string NormalizeExtension(string value)
    {
        var extension = value.Trim().ToLowerInvariant();
        return extension.StartsWith('.') ? extension : $".{extension}";
    }

    private static void ValidateSegment(string value, string parameter)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or ".." ||
            value.Contains('/') || value.Contains('\\') || value.Contains(':'))
            throw new ArgumentException("Storage-key segment is invalid.", parameter);
    }

    private static AppException Validation(string field, string message) =>
        new(400, "validation.failed", "One or more validation errors occurred.",
            new Dictionary<string, string[]> { [field] = [message] });
}

public sealed record ValidatedFile(string OriginalFileName, string Extension, string ContentType, long Length);
