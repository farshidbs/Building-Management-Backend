using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using BuildingManagement.Application;
using BuildingManagement.Domain;
using BuildingManagement.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace BuildingManagement.IntegrationTests;

public sealed partial class ApiScenarios : IAsyncLifetime
{
    private MsSqlContainer? database;
    private WebApplicationFactory<Program>? factory;
    private HttpClient? client;
    private bool enabled;
    private string? skipReason;
    private string? fileRoot;

    public async ValueTask InitializeAsync()
    {
        enabled = string.Equals(Environment.GetEnvironmentVariable("RUN_SQLSERVER_INTEGRATION_TESTS"),
            "true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
        {
            skipReason = "Set RUN_SQLSERVER_INTEGRATION_TESTS=true to run SQL Server integration tests.";
            return;
        }

        try
        {
            database = new MsSqlBuilder().Build();
            fileRoot = Path.Combine(Path.GetTempPath(), $"bms-api-files-{Guid.NewGuid():N}");
            await database.StartAsync();
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:BuildingManagement"] = database.GetConnectionString(),
                        ["SeedDevelopmentData"] = "true",
                        ["FileStorage:LocalRootPath"] = fileRoot
                    })));
            client = factory.CreateClient();
        }
#pragma warning disable CA1031 // Infrastructure failures should skip the opt-in suite, not fail normal development runs.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            enabled = false;
            skipReason = $"SQL Server integration infrastructure is unavailable ({exception.GetType().Name}).";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        if (database is not null) await database.DisposeAsync();
        if (fileRoot is not null && Directory.Exists(fileRoot)) Directory.Delete(fileRoot, true);
    }

    private async Task<T> Post<T>(string uri, object value)
    {
        var response = await client!.PostAsJsonAsync(uri, value);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<T> Put<T>(string uri, object value)
    {
        var response = await client!.PutAsJsonAsync(uri, value);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
