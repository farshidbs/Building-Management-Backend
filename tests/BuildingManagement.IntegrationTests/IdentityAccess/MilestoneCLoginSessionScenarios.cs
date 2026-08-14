using System.Net;
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
    public async Task AuthenticatedUserAddsSecondMobileWithoutCreatingAnotherIdentityAndSwitchesPrimary()
    {
        RequireMilestoneCSql();
        var actor = await CreateAuthenticatedClientWithIdentity();
        using var authenticated = actor.Client;
        var before = await IdentityCounts(actor.UserId);
        var mobile = $"+989{Random.Shared.Next(100000000, 999999999)}";
        var challenge = await authenticated.PostAsJsonAsync("/api/v1/me/login-methods/mobile/otp",
            new LoginMethodOtpRequest(mobile));
        challenge.EnsureSuccessStatusCode();
        var reference = (await challenge.Content.ReadFromJsonAsync<RequestOtpResponse>())!;
        var code = testOtpDelivery!.CodeFor(mobile);
        var addedResponse = await authenticated.PostAsJsonAsync("/api/v1/me/login-methods/mobile",
            new AddLoginMethodRequest(mobile, reference.ChallengeReference, code, true));
        Assert.Equal(HttpStatusCode.Created, addedResponse.StatusCode);
        var added = (await addedResponse.Content.ReadFromJsonAsync<LoginMethodResponse>())!;
        Assert.True(added.IsPrimary);

        var methods = (await authenticated.GetFromJsonAsync<List<LoginMethodResponse>>(
            "/api/v1/me/login-methods"))!;
        Assert.Equal(2, methods.Count(x => x.IsActiveLoginMethod()));
        Assert.Single(methods, x => x.IsPrimary && x.IsActiveLoginMethod());
        var after = await IdentityCounts(actor.UserId);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Parties, after.Parties);
        Assert.Equal(before.Links, after.Links);
        Assert.Equal(before.Memberships, after.Memberships);
    }

    [Fact]
    public async Task LastMethodAndForeignMethodReleaseAreDeniedAndReleasedIdentifierCanBeReused()
    {
        RequireMilestoneCSql();
        var first = await CreateAuthenticatedClientWithIdentity();
        var second = await CreateAuthenticatedClientWithIdentity();
        using var firstClient = first.Client;
        using var secondClient = second.Client;
        var firstMethods = (await firstClient.GetFromJsonAsync<List<LoginMethodResponse>>(
            "/api/v1/me/login-methods"))!;
        var only = Assert.Single(firstMethods, x => x.IsActiveLoginMethod());
        using var lastDenied = await firstClient.PostAsJsonAsync(
            $"/api/v1/me/login-methods/{only.Code}/release", new ReleaseLoginMethodRequest("user_requested"));
        Assert.Equal(HttpStatusCode.Conflict, lastDenied.StatusCode);
        using var foreignDenied = await secondClient.PostAsJsonAsync(
            $"/api/v1/me/login-methods/{only.Code}/release", new ReleaseLoginMethodRequest("user_requested"));
        Assert.Equal(HttpStatusCode.NotFound, foreignDenied.StatusCode);

        var newMobile = $"+989{Random.Shared.Next(100000000, 999999999)}";
        var added = await AddMobile(firstClient, newMobile, false);
        using var released = await firstClient.PostAsJsonAsync(
            $"/api/v1/me/login-methods/{added.Code}/release", new ReleaseLoginMethodRequest("number_changed"));
        Assert.Equal(HttpStatusCode.NoContent, released.StatusCode);
        var reclaimed = await AddMobile(secondClient, newMobile, false);
        Assert.NotEqual(added.Code, reclaimed.Code);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(2, await db.UserLoginMethods.CountAsync(x => x.NormalizedIdentifierValue == newMobile));
        Assert.Equal(1, await db.UserLoginMethods.CountAsync(x => x.NormalizedIdentifierValue == newMobile &&
            x.IsActive && x.StatusKey == IamKeys.LoginStatuses.Active));
    }

    [Fact]
    public async Task SessionListAndRevocationAreOwnedAndRevokeRefreshTokens()
    {
        RequireMilestoneCSql();
        var actor = await CreateAuthenticatedClientWithIdentity();
        var foreign = await CreateAuthenticatedClientWithIdentity();
        using var authenticated = actor.Client;
        using var foreignClient = foreign.Client;
        AuthSession target;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            var methodId = await db.UserLoginMethods.Where(x => x.UserId == actor.UserId && x.IsActive)
                .Select(x => x.Id).SingleAsync();
            var now = DateTimeOffset.UtcNow;
            target = new AuthSession(PublicCode.Create(), actor.UserId, null, methodId, "web",
                protector.Hash(protector.CreateToken()), now.AddMinutes(15), now, now.AddHours(12),
                now.AddHours(2), "other-device", null);
            db.AuthSessions.Add(target);
            await db.SaveChangesAsync();
            db.AuthRefreshTokens.Add(new AuthRefreshToken(target.Id, protector.Hash(protector.CreateToken()),
                now, now.AddDays(30)));
            await db.SaveChangesAsync();
        }

        var sessions = (await authenticated.GetFromJsonAsync<List<SessionResponse>>("/api/v1/me/sessions"))!;
        Assert.Contains(sessions, x => x.Code == target.Code && !x.IsCurrent);
        Assert.Single(sessions, x => x.IsCurrent);
        using var foreignDenied = await foreignClient.PostAsync($"/api/v1/me/sessions/{target.Code}/revoke", null);
        Assert.Equal(HttpStatusCode.NotFound, foreignDenied.StatusCode);
        using var revoked = await authenticated.PostAsync($"/api/v1/me/sessions/{target.Code}/revoke", null);
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False((await verifyDb.AuthSessions.SingleAsync(x => x.Id == target.Id)).IsActive);
        Assert.All(await verifyDb.AuthRefreshTokens.Where(x => x.AuthSessionId == target.Id).ToListAsync(),
            x => Assert.NotNull(x.RevokedAtUtc));
    }

    private async Task<LoginMethodResponse> AddMobile(HttpClient client, string mobile, bool primary)
    {
        var response = await client.PostAsJsonAsync("/api/v1/me/login-methods/mobile/otp",
            new LoginMethodOtpRequest(mobile));
        response.EnsureSuccessStatusCode();
        var challenge = (await response.Content.ReadFromJsonAsync<RequestOtpResponse>())!;
        var code = testOtpDelivery!.CodeFor(mobile);
        var added = await client.PostAsJsonAsync("/api/v1/me/login-methods/mobile",
            new AddLoginMethodRequest(mobile, challenge.ChallengeReference, code, primary));
        added.EnsureSuccessStatusCode();
        return (await added.Content.ReadFromJsonAsync<LoginMethodResponse>())!;
    }

    private async Task<(int Users, int Parties, int Links, int Memberships)> IdentityCounts(long userId)
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        return (await db.Users.CountAsync(), await db.Parties.CountAsync(),
            await db.UserPartyLinks.CountAsync(x => x.UserId == userId),
            await db.AccessMemberships.CountAsync(x => x.UserId == userId));
    }

    private void RequireMilestoneCSql()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
    }
}

internal static class LoginMethodTestExtensions
{
    public static bool IsActiveLoginMethod(this LoginMethodResponse value) =>
        value.IsVerified && value.ReleasedAtUtc == null && value.StatusKey == IamKeys.LoginStatuses.Active;
}
