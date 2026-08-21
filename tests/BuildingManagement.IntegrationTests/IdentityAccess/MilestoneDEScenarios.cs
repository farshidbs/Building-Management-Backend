using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BuildingManagement.Application;
using BuildingManagement.Domain;
using BuildingManagement.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BuildingManagement.IntegrationTests;

public sealed partial class ApiScenarios
{
    [Fact]
    public async Task PlatformAndActingRealmsAreSeparatedAndActingRevocationIsImmediate()
    {
        RequireMilestoneDESql();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client!.GetAsync("/api/v1/platform/recovery-cases")).StatusCode);
        var target = await CreateAuthenticatedClientWithIdentity();
        using var targetClient = target.Client;
        string username; const string password = "Support-Test-Password-42!";
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPlatformPasswordHasher>();
            var now = DateTimeOffset.UtcNow;
            username = $"support-{Guid.NewGuid():N}";
            var template = new PlatformUser(PublicCode.Create(), username, "placeholder", now);
            var platform = new PlatformUser(template.Code, username, hasher.Hash(template, password), now);
            db.PlatformUsers.Add(platform); await db.SaveChangesAsync();
            var roleId = await db.PlatformRoles.Where(x => x.Key == "support_manager")
                .Select(x => x.Id).SingleAsync();
            db.PlatformUserRoles.Add(new PlatformUserRoleLink(platform.Id, roleId));
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/v1/platform/auth/login",
            new PlatformLoginRequest(username, password, "support-test"));
        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<PlatformTokenResponse>())!;
        using var platformClient = factory.CreateClient();
        platformClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            login.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await platformClient.GetAsync("/api/v1/me/login-methods")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await platformClient.PostAsJsonAsync(
            "/api/v1/platform/support/acting-sessions",
            new StartActingSessionRequest(await UserCode(target.UserId), " "))).StatusCode);
        var actingResponse = await platformClient.PostAsJsonAsync(
            "/api/v1/platform/support/acting-sessions",
            new StartActingSessionRequest(await UserCode(target.UserId), "بررسی درخواست مشتری", "T-42", 120));
        Assert.Equal(HttpStatusCode.Created, actingResponse.StatusCode);
        var acting = (await actingResponse.Content.ReadFromJsonAsync<ActingSessionTokenResponse>())!;
        Assert.True(acting.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(61));

        using var actingClient = factory.CreateClient();
        actingClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            acting.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await actingClient.GetAsync("/api/v1/me/login-methods")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await actingClient.GetAsync("/api/v1/platform/recovery-cases")).StatusCode);
        using var revoke = await platformClient.PostAsync(
            $"/api/v1/platform/support/acting-sessions/{acting.Code}/revoke", null);
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await actingClient.GetAsync("/api/v1/buildings?pageNumber=1&pageSize=1")).StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var row = await verifyDb.SupportActingSessions.SingleAsync(x => x.Code == acting.Code);
        Assert.False(row.IsActive); Assert.NotNull(row.EndedAtUtc);
        Assert.True(await verifyDb.SecurityAuditEvents.AnyAsync(x => x.EventTypeKey == "acting_started" &&
            x.ResourceCode == acting.Code));
        Assert.True(await verifyDb.SecurityAuditEvents.AnyAsync(x => x.EventTypeKey == "acting_revoked" &&
            x.ResourceCode == acting.Code));
    }

    private async Task<string> UserCode(long userId)
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        return await db.Users.Where(x => x.Id == userId).Select(x => x.Code).SingleAsync();
    }

    private void RequireMilestoneDESql()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
    }
}
