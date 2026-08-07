using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace BuildingManagement.IntegrationTests;

public sealed class SwaggerSmokeTests
{
    [Fact]
    public async Task SwaggerIncludesFileManagementRoutes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"bms-swagger-files-{Guid.NewGuid():N}");
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureLogging(logging => logging.ClearProviders());
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:BuildingManagement"] =
                            "Server=127.0.0.1,1;Database=smoke;User Id=smoke;Password=Smoke_only_123!;TrustServerCertificate=True;Connect Timeout=1",
                        ["FileStorage:LocalRootPath"] = root,
                        ["SeedDevelopmentData"] = "false"
                    }));
            });
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/swagger/v1/swagger.json",
                TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            var document = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("/api/v1/files/{fileCode}/content", document, StringComparison.Ordinal);
            Assert.Contains("/api/v1/buildings/{buildingCode}/gallery/", document, StringComparison.Ordinal);
            Assert.Contains("multipart/form-data", document, StringComparison.Ordinal);
            Assert.Contains("/api/v1/parties/", document, StringComparison.Ordinal);
            Assert.Contains("/api/v1/units/{unitCode}/occupancy-history", document, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
