using BuildingManagement.Application;

namespace BuildingManagement.Infrastructure;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string rootPath;

    public LocalFileStorage(FileStorageOptions options, string contentRootPath)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!string.Equals(options.Provider, "Local", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Unsupported file storage provider '{options.Provider}'.");

        rootPath = Path.GetFullPath(Path.IsPathRooted(options.LocalRootPath)
            ? options.LocalRootPath
            : Path.Combine(contentRootPath, options.LocalRootPath));
        Directory.CreateDirectory(rootPath);
    }

    public async Task SaveAsync(StorageWriteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var destinationPath = ResolvePath(request.StorageKey);
        var directory = Path.GetDirectoryName(destinationPath)
            ?? throw new InvalidOperationException("The storage key has no parent directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = destinationPath + ".uploading";
        try
        {
            await using (var output = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await request.Content.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, false);
        }
        catch
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
            throw;
        }
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan)
            : null;
        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(ResolvePath(storageKey)));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
            File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
            throw new InvalidOperationException("The storage key must be a non-empty relative path.");

        var normalizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var resolved = Path.GetFullPath(Path.Combine(rootPath, normalizedKey));
        var rootPrefix = rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? rootPath
            : rootPath + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The storage key resolves outside the configured storage root.");
        return resolved;
    }
}
