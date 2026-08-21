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
    public async Task RecoveryOtpUsesCooldownAndCrossPurposeRollingLimitWithoutIdentityDisclosure()
    {
        RequireMilestoneDESql();
        var cooldownReference = $"cooldown-{Guid.NewGuid():N}";
        var limitedReference = $"limited-{Guid.NewGuid():N}";
        const string cooldownMobile = "+989121234501";
        const string limitedMobile = "+989121234502";
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            var now = DateTimeOffset.UtcNow;
            db.AccountRecoveryCases.AddRange(
                new AccountRecoveryCase(PublicCode.Create(), protector.Hash(cooldownReference), null, null,
                    "+989120000001", cooldownMobile, null, null, now.AddHours(1), now),
                new AccountRecoveryCase(PublicCode.Create(), protector.Hash(limitedReference), null, null,
                    "+989120000002", limitedMobile, null, null, now.AddHours(1), now));
            db.OtpChallenges.Add(new OtpChallenge(protector.CreateToken(18), "mobile", cooldownMobile,
                cooldownMobile, "login", protector.Hash("123456"), now.AddSeconds(-30), now.AddMinutes(3), 5));
            for (var index = 0; index < 5; index++)
                db.OtpChallenges.Add(new OtpChallenge(protector.CreateToken(18), "mobile", limitedMobile,
                    limitedMobile, index % 2 == 0 ? "login" : "register", protector.Hash("123456"),
                    now.AddMinutes(-2), now.AddMinutes(1), 5));
            await db.SaveChangesAsync();
        }
        var cooldown = await client!.PostAsync($"/api/v1/auth/recovery/{cooldownReference}/otp", null);
        Assert.Equal((HttpStatusCode)429, cooldown.StatusCode);
        var limited = await client.PostAsync($"/api/v1/auth/recovery/{limitedReference}/otp", null);
        Assert.Equal((HttpStatusCode)429, limited.StatusCode);
        var body = await limited.Content.ReadAsStringAsync();
        Assert.Contains("otp.rate_limited", body, StringComparison.Ordinal);
        Assert.DoesNotContain(limitedMobile, body, StringComparison.Ordinal);
        Assert.DoesNotContain("User", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Party", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RecoveryCancellationIsTerminalAndHasNoCredentialOrSessionSideEffects()
    {
        RequireMilestoneDESql();
        var reference = $"cancel-{Guid.NewGuid():N}";
        long userId; int methodsBefore; int sessionsBefore;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            var user = await db.Users.OrderBy(x => x.Id).FirstAsync(); userId = user.Id;
            methodsBefore = await db.UserLoginMethods.CountAsync(x => x.UserId == userId);
            sessionsBefore = await db.AuthSessions.CountAsync(x => x.UserId == userId);
            db.AccountRecoveryCases.Add(new AccountRecoveryCase(PublicCode.Create(), protector.Hash(reference),
                userId, null, "+989120000000", "+989121111111", null, null,
                DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.NoContent,
            (await client!.PostAsync($"/api/v1/auth/recovery/{reference}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsync($"/api/v1/auth/recovery/{reference}/cancel", null)).StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var recovery = await verifyDb.AccountRecoveryCases.SingleAsync(x => x.UserId == userId &&
            x.NewNormalizedIdentifier == "+989121111111");
        Assert.Equal("cancelled", recovery.StatusKey); Assert.False(recovery.IsActive);
        Assert.Equal(methodsBefore, await verifyDb.UserLoginMethods.CountAsync(x => x.UserId == userId));
        Assert.Equal(sessionsBefore, await verifyDb.AuthSessions.CountAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task RefreshTokensCannotCrossCustomerAndPlatformRealmsOrMutateTheOtherChain()
    {
        RequireMilestoneDESql();
        string customerRefresh; string platformRefresh;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPlatformPasswordHasher>();
            var now = DateTimeOffset.UtcNow;
            var user = new User(PublicCode.Create(), now); db.Users.Add(user);
            var template = new PlatformUser(PublicCode.Create(), $"realm-{Guid.NewGuid():N}", "x", now);
            var platform = new PlatformUser(template.Code, template.Username,
                hasher.Hash(template, "Realm-Test-Password-42!"), now); db.PlatformUsers.Add(platform);
            await db.SaveChangesAsync();
            customerRefresh = protector.CreateToken(); platformRefresh = protector.CreateToken();
            var customerSession = new AuthSession(PublicCode.Create(), user.Id, null, null, "web",
                protector.Hash(protector.CreateToken()), now.AddMinutes(15), now, now.AddHours(2),
                now.AddHours(1), null, null);
            var platformSession = new AuthSession(PublicCode.Create(), null, platform.Id, null, "web",
                protector.Hash(protector.CreateToken()), now.AddMinutes(15), now, now.AddHours(2),
                now.AddHours(1), null, null);
            db.AuthSessions.AddRange(customerSession, platformSession); await db.SaveChangesAsync();
            db.AuthRefreshTokens.AddRange(
                new AuthRefreshToken(customerSession.Id, protector.Hash(customerRefresh), now, now.AddDays(1)),
                new AuthRefreshToken(platformSession.Id, protector.Hash(platformRefresh), now, now.AddDays(1)));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Unauthorized, (await client!.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(platformRefresh))).StatusCode);
        Assert.True((await client!.PostAsJsonAsync("/api/v1/platform/auth/refresh",
            new RefreshTokenRequest(platformRefresh))).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client!.PostAsJsonAsync("/api/v1/platform/auth/refresh",
            new RefreshTokenRequest(customerRefresh))).StatusCode);
        Assert.True((await client!.PostAsJsonAsync("/api/v1/auth/refresh",
            new RefreshTokenRequest(customerRefresh))).IsSuccessStatusCode);
    }

    [Fact]
    public async Task PlatformCredentialFailuresAreGenericDurableAndLockTheAccount()
    {
        RequireMilestoneDESql();
        string username; const string password = "Platform-Lockout-Test-42!";
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPlatformPasswordHasher>();
            var now = DateTimeOffset.UtcNow; username = $"lock-{Guid.NewGuid():N}";
            var template = new PlatformUser(PublicCode.Create(), username, "x", now);
            db.PlatformUsers.Add(new PlatformUser(template.Code, username,
                hasher.Hash(template, password), now)); await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await client!.PostAsJsonAsync("/api/v1/platform/auth/login",
            new PlatformLoginRequest("missing-user", "", null))).StatusCode);
        for (var attempt = 0; attempt < 5; attempt++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await client!.PostAsJsonAsync("/api/v1/platform/auth/login",
                new PlatformLoginRequest(username, attempt == 0 ? "" : "wrong", null))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client!.PostAsJsonAsync("/api/v1/platform/auth/login",
            new PlatformLoginRequest(username, password, null))).StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var row = await verifyDb.PlatformUsers.SingleAsync(x => x.Username == username);
        Assert.Equal(5, row.FailedLoginCount); Assert.True(row.LockedUntilUtc > DateTimeOffset.UtcNow);
        Assert.False(await verifyDb.AuthSessions.AnyAsync(x => x.PlatformUserId == row.Id));
    }

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
        await using (var expiryScope = factory.Services.CreateAsyncScope())
        {
            var expiryDb = expiryScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var sourceSessionId = await expiryDb.SupportActingSessions.Where(x => x.Code == acting.Code)
                .Select(x => x.PlatformAuthSessionId).SingleAsync();
            await expiryDb.AuthSessions.Where(x => x.Id == sourceSessionId).ExecuteUpdateAsync(setters =>
                setters.SetProperty(x => x.AccessTokenExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(-1)));
        }
        Assert.NotEqual(HttpStatusCode.Unauthorized,
            (await actingClient.GetAsync("/api/v1/buildings?pageNumber=1&pageSize=1")).StatusCode);
        await using (var restoreScope = factory.Services.CreateAsyncScope())
        {
            var restoreDb = restoreScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var sourceSessionId = await restoreDb.SupportActingSessions.Where(x => x.Code == acting.Code)
                .Select(x => x.PlatformAuthSessionId).SingleAsync();
            await restoreDb.AuthSessions.Where(x => x.Id == sourceSessionId).ExecuteUpdateAsync(setters =>
                setters.SetProperty(x => x.AccessTokenExpiresAtUtc, DateTimeOffset.UtcNow.AddMinutes(15)));
        }
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
