using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using System.Net.Http.Headers;
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
    private long defaultUserId;
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

        fileRoot = Path.Combine(Path.GetTempPath(), $"bms-api-files-{Guid.NewGuid():N}");
        try
        {
            database = new MsSqlBuilder().Build();
            await database.StartAsync();
        }
#pragma warning disable CA1031 // Infrastructure failures should skip the opt-in suite, not fail normal development runs.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            enabled = false;
            skipReason = $"SQL Server integration infrastructure is unavailable ({exception.GetType().Name}).";
            return;
        }

        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:BuildingManagement"] = database.GetConnectionString(),
                    ["SeedDevelopmentData"] = "true",
                    ["FileStorage:LocalRootPath"] = fileRoot,
                    ["Iam:Secret"] = "integration-test-secret-at-least-thirty-two-characters"
                })));
        client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            await CreateAuthenticatedComplexManager());
    }

    private async Task<string> CreateAuthenticatedComplexManager()
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
        var now = DateTimeOffset.UtcNow;
        var user = new User(PublicCode.Create(), now);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        defaultUserId = user.Id;
        var method = new UserLoginMethod(PublicCode.Create(), user.Id, IamKeys.LoginTypes.Mobile,
            "09120000000", "989120000000", true, now);
        method.Verify(now);
        db.UserLoginMethods.Add(method);
        await db.SaveChangesAsync();
        var token = protector.CreateToken();
        db.AuthSessions.Add(new AuthSession(PublicCode.Create(), user.Id, null, method.Id, "web",
            protector.Hash(token), now.AddHours(1), now, now.AddHours(2), now.AddHours(1), "integration", null));
        var roleId = await db.AccessRoles.Where(x => x.Key == "complex_manager").Select(x => x.Id).SingleAsync();
        var complexIds = await db.Complexes.Select(x => x.Id).ToListAsync();
        foreach (var complexId in complexIds)
            db.AccessMemberships.Add(new AccessMembership(PublicCode.Create(), user.Id, roleId,
                complexId, null, null, null, now));
        await db.SaveChangesAsync();
        return token;
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
        var result = (await response.Content.ReadFromJsonAsync<T>())!;
        if (result is ComplexResponse complex)
            await GrantDefaultComplexManager(complex.Code);
        return result;
    }

    private async Task GrantDefaultComplexManager(string complexCode)
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var roleId = await db.AccessRoles.Where(x => x.Key == "complex_manager").Select(x => x.Id).SingleAsync();
        var complexId = await db.Complexes.Where(x => x.Code == complexCode).Select(x => x.Id).SingleAsync();
        if (!await db.AccessMemberships.AnyAsync(x => x.UserId == defaultUserId && x.RoleId == roleId && x.ComplexId == complexId))
        {
            db.AccessMemberships.Add(new AccessMembership(PublicCode.Create(), defaultUserId, roleId,
                complexId, null, null, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string roleKey, long? complexId = null,
        long? buildingId = null, long? unitId = null)
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
        var now = DateTimeOffset.UtcNow;
        var user = new User(PublicCode.Create(), now);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        var identifier = $"test-{Guid.NewGuid():N}";
        var method = new UserLoginMethod(PublicCode.Create(), user.Id, IamKeys.LoginTypes.Mobile,
            identifier, identifier, true, now);
        method.Verify(now);
        db.UserLoginMethods.Add(method);
        await db.SaveChangesAsync();
        var token = protector.CreateToken();
        db.AuthSessions.Add(new AuthSession(PublicCode.Create(), user.Id, null, method.Id, "web",
            protector.Hash(token), now.AddHours(1), now, now.AddHours(2), now.AddHours(1),
            "integration-security", null));
        var roleId = await db.AccessRoles.Where(x => x.Key == roleKey).Select(x => x.Id).SingleAsync();
        db.AccessMemberships.Add(new AccessMembership(PublicCode.Create(), user.Id, roleId,
            complexId, buildingId, unitId, null, now));
        await db.SaveChangesAsync();
        var authenticated = factory.CreateClient();
        authenticated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return authenticated;
    }

    private async Task<T> Put<T>(string uri, object value)
    {
        var response = await client!.PutAsJsonAsync(uri, value);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
