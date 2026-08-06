using BuildingManagement.Application;
using BuildingManagement.Infrastructure;
using Xunit;

namespace BuildingManagement.IntegrationTests;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task SavesReadsAndDeletesFileWithinConfiguredRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), $"bms-files-{Guid.NewGuid():N}");
        try
        {
            var storage = new LocalFileStorage(new FileStorageOptions { LocalRootPath = root }, AppContext.BaseDirectory);
            const string key = "buildings/1/gallery/file.png";
            var bytes = new byte[] { 1, 2, 3, 4 };
            await using (var input = new MemoryStream(bytes))
                await storage.SaveAsync(new StorageWriteRequest(key, input), TestContext.Current.CancellationToken);
            Assert.True(await storage.ExistsAsync(key, TestContext.Current.CancellationToken));
            await using var output = await storage.OpenReadAsync(key, TestContext.Current.CancellationToken);
            Assert.NotNull(output);
            using var copy = new MemoryStream();
            await output.CopyToAsync(copy, TestContext.Current.CancellationToken);
            Assert.Equal(bytes, copy.ToArray());
            await output.DisposeAsync();
            await storage.DeleteAsync(key, TestContext.Current.CancellationToken);
            Assert.False(await storage.ExistsAsync(key, TestContext.Current.CancellationToken));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("/absolute.txt")]
    public async Task RejectsStorageKeysOutsideRoot(string key)
    {
        var root = Path.Combine(Path.GetTempPath(), $"bms-files-{Guid.NewGuid():N}");
        var storage = new LocalFileStorage(new FileStorageOptions { LocalRootPath = root }, AppContext.BaseDirectory);
        await using var input = new MemoryStream([1]);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.SaveAsync(new StorageWriteRequest(key, input), TestContext.Current.CancellationToken));
    }
}
