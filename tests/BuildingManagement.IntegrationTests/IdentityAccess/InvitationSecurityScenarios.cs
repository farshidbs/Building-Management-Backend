using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task BuildingCollaboratorDelegationRequiresBothScopedPermissionsAndAllowlist()
    {
        RequireSql();
        var building = await FirstBuilding();
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: building.Id);
        foreach (var role in new[] { "building_manager", "manager_assistant", "accountant" })
        {
            using var response = await manager.PostAsJsonAsync("/api/v1/invitations/building",
                new CreateBuildingInvitationRequest(building.Code, NextMobile(), role));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        using var assistant = await CreateAuthenticatedClient("manager_assistant", buildingId: building.Id);
        using var escalation = await assistant.PostAsJsonAsync("/api/v1/invitations/building",
            new CreateBuildingInvitationRequest(building.Code, NextMobile(), "building_manager"));
        Assert.Equal(HttpStatusCode.Forbidden, escalation.StatusCode);

        using var forbiddenRole = await manager.PostAsJsonAsync("/api/v1/invitations/building",
            new CreateBuildingInvitationRequest(building.Code, NextMobile(), "complex_manager"));
        Assert.Equal(HttpStatusCode.BadRequest, forbiddenRole.StatusCode);
    }

    [Fact]
    public async Task BulkReturnsOneTimeTokenPreviewWorksAndListHasNoTokenProperty()
    {
        RequireSql();
        var fixture = await CreateEligibleRelation();
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: fixture.BuildingId);
        var rows = await (await manager.PostAsync(
            $"/api/v1/invitations/building/{fixture.BuildingCode}/bulk", null))
            .Content.ReadFromJsonAsync<List<BulkInvitationItemResponse>>();
        var created = Assert.Single(rows!, x => x.UnitCode == fixture.UnitCode && x.Result == "created");
        Assert.False(string.IsNullOrWhiteSpace(created.InvitationCode));
        Assert.False(string.IsNullOrWhiteSpace(created.InvitationToken));

        using var preview = await factory!.CreateClient().GetAsync(
            $"/api/v1/auth/invitations/preview?token={Uri.EscapeDataString(created.InvitationToken!)}");
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        var previewBody = await preview.Content.ReadFromJsonAsync<InvitationPreviewResponse>();
        Assert.Equal("pending", previewBody!.Status);

        using var listResponse = await manager.GetAsync(
            $"/api/v1/invitations/?buildingCode={fixture.BuildingCode}");
        listResponse.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        Assert.All(json.RootElement.EnumerateArray(), item =>
        {
            Assert.False(item.TryGetProperty("token", out _));
            Assert.False(item.TryGetProperty("invitationToken", out _));
        });

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var stored = await db.Invitations.SingleAsync(x => x.Code == created.InvitationCode);
        Assert.NotEqual(created.InvitationToken, stored.TokenHash);
        Assert.DoesNotContain(created.InvitationToken!, stored.TokenHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExpiredInvitationIsRetainedInactiveAndEquivalentInviteCanBeReissued()
    {
        RequireSql();
        var building = await FirstBuilding();
        var actor = await CreateAuthenticatedClientWithIdentity("building_manager", buildingId: building.Id);
        using var manager = actor.Client;
        var mobile = NextMobile();
        long staleId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            var roleId = await db.AccessRoles.Where(x => x.Key == "accountant").Select(x => x.Id).SingleAsync();
            var stale = new Invitation(PublicCode.Create(), protector.Hash("expired-token"), mobile,
                "building_collaborator", roleId, null, building.Id, null, actor.UserId, null,
                DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(-8));
            db.Invitations.Add(stale);
            await db.SaveChangesAsync();
            staleId = stale.Id;
        }

        using var response = await manager.PostAsJsonAsync("/api/v1/invitations/building",
            new CreateBuildingInvitationRequest(building.Code, mobile, "accountant"));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var staleRow = await verifyDb.Invitations.SingleAsync(x => x.Id == staleId);
        Assert.False(staleRow.IsActive);
        Assert.NotNull(staleRow.ExpiredAtUtc);
        Assert.Equal(1, await verifyDb.Invitations.CountAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.IsActive && x.AcceptedAtUtc == null && x.RevokedAtUtc == null));
    }

    [Fact]
    public async Task ConcurrentEquivalentCreationProducesOnePendingRowAndNoServerError()
    {
        RequireSql();
        var building = await FirstBuilding();
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: building.Id);
        var mobile = NextMobile();
        var request = new CreateBuildingInvitationRequest(building.Code, mobile, "accountant");
        var results = await Task.WhenAll(manager.PostAsJsonAsync("/api/v1/invitations/building", request),
            manager.PostAsJsonAsync("/api/v1/invitations/building", request));
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Created);
        Assert.Single(results, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.DoesNotContain(results, x => x.StatusCode == HttpStatusCode.InternalServerError);
        foreach (var result in results) result.Dispose();

        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(1, await db.Invitations.CountAsync(x => x.NormalizedIdentifierValue == mobile &&
            x.IsActive && x.AcceptedAtUtc == null && x.RevokedAtUtc == null));
    }

    [Fact]
    public async Task NewInviteeGetsSeparateIdentityPartyAndRepeatedAcceptanceHasNoSideEffects()
    {
        RequireSql();
        var fixture = await CreateEligibleRelation();
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: fixture.BuildingId);
        var created = await CreateUnitInvitation(manager, fixture.UnitCode, fixture.PartyCode);
        var challenge = await ArrangeInvitationChallenge(created.Token, fixture.Mobile, "123456");
        var request = new AcceptInvitationRequest(created.Token, challenge, "123456", DisplayName: "کاربر دعوت‌شده");
        using var publicClient = factory!.CreateClient();
        using var accepted = await publicClient.PostAsJsonAsync("/api/v1/auth/invitations/accept", request);
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        long userId;
        int sessions;
        int refreshTokens;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var invitation = await db.Invitations.SingleAsync(x => x.Code == created.Code);
            userId = invitation.AcceptedByUserId!.Value;
            var identityPartyId = await db.UserPartyLinks.Where(x => x.UserId == userId && x.IsActive)
                .Select(x => x.PartyId).SingleAsync();
            Assert.NotEqual(fixture.PartyId, identityPartyId);
            Assert.Equal(1, await db.AccessMemberships.CountAsync(x => x.UserId == userId &&
                x.SourceUnitPartyRelationId == fixture.RelationId));
            sessions = await db.AuthSessions.CountAsync(x => x.UserId == userId);
            refreshTokens = await db.AuthRefreshTokens.CountAsync(x =>
                db.AuthSessions.Where(s => s.UserId == userId).Select(s => s.Id).Contains(x.AuthSessionId));
        }

        using var repeated = await publicClient.PostAsJsonAsync("/api/v1/auth/invitations/accept", request);
        Assert.Equal(HttpStatusCode.Conflict, repeated.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(sessions, await verifyDb.AuthSessions.CountAsync(x => x.UserId == userId));
        Assert.Equal(refreshTokens, await verifyDb.AuthRefreshTokens.CountAsync(x =>
            verifyDb.AuthSessions.Where(s => s.UserId == userId).Select(s => s.Id).Contains(x.AuthSessionId)));
        Assert.Equal(1, await verifyDb.AccessMemberships.CountAsync(x => x.UserId == userId &&
            x.SourceUnitPartyRelationId == fixture.RelationId));
    }

    [Fact]
    public async Task ConcurrentNewUserAcceptanceCreatesOneIdentityAndOneMembership()
    {
        RequireSql();
        var fixture = await CreateEligibleRelation();
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: fixture.BuildingId);
        var created = await CreateUnitInvitation(manager, fixture.UnitCode, fixture.PartyCode);
        var firstChallenge = await ArrangeInvitationChallenge(created.Token, fixture.Mobile, "123456");
        var secondChallenge = await ArrangeInvitationChallenge(created.Token, fixture.Mobile, "654321");
        using var publicClient = factory!.CreateClient();
        var responses = await Task.WhenAll(
            publicClient.PostAsJsonAsync("/api/v1/auth/invitations/accept",
                new AcceptInvitationRequest(created.Token, firstChallenge, "123456", DisplayName: "هویت یک")),
            publicClient.PostAsJsonAsync("/api/v1/auth/invitations/accept",
                new AcceptInvitationRequest(created.Token, secondChallenge, "654321", DisplayName: "هویت دو")));
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
        Assert.DoesNotContain(responses, x => x.StatusCode == HttpStatusCode.InternalServerError);
        foreach (var response in responses) response.Dispose();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var users = await db.UserLoginMethods.Where(x => x.NormalizedIdentifierValue == fixture.Mobile &&
            x.IsActive && x.IsVerified).Select(x => x.UserId).ToListAsync();
        var userId = Assert.Single(users);
        Assert.Equal(1, await db.UserPartyLinks.CountAsync(x => x.UserId == userId && x.IsActive));
        Assert.Equal(1, await db.AccessMemberships.CountAsync(x => x.UserId == userId &&
            x.SourceUnitPartyRelationId == fixture.RelationId && x.IsActive));
    }

    [Fact]
    public async Task ExistingVerifiedUserIsReusedWithoutChangingIdentityParty()
    {
        RequireSql();
        var fixture = await CreateEligibleRelation();
        long existingUserId;
        long identityPartyId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var now = DateTimeOffset.UtcNow;
            var partyTypeId = await db.PartyTypes.Select(x => x.Id).FirstAsync();
            var identity = new Party(PublicCode.Create(), partyTypeId, "هویت موجود", null, null, null,
                null, null, now);
            var user = new User(PublicCode.Create(), now);
            db.Parties.Add(identity);
            db.Users.Add(user);
            await db.SaveChangesAsync();
            var method = new UserLoginMethod(PublicCode.Create(), user.Id, IamKeys.LoginTypes.Mobile,
                fixture.Mobile, fixture.Mobile, true, now);
            method.Verify(now);
            db.UserLoginMethods.Add(method);
            db.UserPartyLinks.Add(new UserPartyLink(PublicCode.Create(), user.Id, identity.Id, true, now));
            await db.SaveChangesAsync();
            existingUserId = user.Id;
            identityPartyId = identity.Id;
        }

        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: fixture.BuildingId);
        var created = await CreateUnitInvitation(manager, fixture.UnitCode, fixture.PartyCode);
        var challenge = await ArrangeInvitationChallenge(created.Token, fixture.Mobile, "123456");
        using var accepted = await factory!.CreateClient().PostAsJsonAsync("/api/v1/auth/invitations/accept",
            new AcceptInvitationRequest(created.Token, challenge, "123456"));
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(1, await verifyDb.UserLoginMethods.CountAsync(x => x.UserId == existingUserId &&
            x.NormalizedIdentifierValue == fixture.Mobile));
        Assert.Equal(identityPartyId, await verifyDb.UserPartyLinks.Where(x => x.UserId == existingUserId &&
            x.IsActive && x.IsPrimary).Select(x => x.PartyId).SingleAsync());
        Assert.DoesNotContain(await verifyDb.UserPartyLinks.Where(x => x.UserId == existingUserId)
            .Select(x => x.PartyId).ToListAsync(), x => x == fixture.PartyId);
        Assert.Equal(1, await verifyDb.AccessMemberships.CountAsync(x => x.UserId == existingUserId &&
            x.SourceUnitPartyRelationId == fixture.RelationId));
    }

    [Fact]
    public async Task AcceptAndRevokeRaceProducesOnlyOneTerminalState()
    {
        RequireSql();
        var fixture = await CreateEligibleRelation();
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: fixture.BuildingId);
        var created = await CreateUnitInvitation(manager, fixture.UnitCode, fixture.PartyCode);
        var challenge = await ArrangeInvitationChallenge(created.Token, fixture.Mobile, "123456");
        using var publicClient = factory!.CreateClient();
        var outcomes = await Task.WhenAll(
            publicClient.PostAsJsonAsync("/api/v1/auth/invitations/accept",
                new AcceptInvitationRequest(created.Token, challenge, "123456")),
            manager.PostAsync($"/api/v1/invitations/{created.Code}/revoke", null));
        Assert.DoesNotContain(outcomes, x => x.StatusCode == HttpStatusCode.InternalServerError);
        foreach (var outcome in outcomes) outcome.Dispose();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var invitation = await db.Invitations.SingleAsync(x => x.Code == created.Code);
        Assert.NotEqual(invitation.AcceptedAtUtc.HasValue, invitation.RevokedAtUtc.HasValue);
        Assert.Equal(invitation.AcceptedAtUtc.HasValue ? 1 : 0,
            await db.AccessMemberships.CountAsync(x => x.SourceUnitPartyRelationId == fixture.RelationId));
    }

    private void RequireSql()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
    }

    private async Task<Building> FirstBuilding()
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>()
            .Buildings.AsNoTracking().FirstAsync(x => x.IsActive);
    }

    private static async Task<InvitationResponse> CreateUnitInvitation(HttpClient actor, string unitCode,
        string partyCode)
    {
        using var response = await actor.PostAsJsonAsync("/api/v1/invitations/unit",
            new CreateUnitInvitationRequest(unitCode, partyCode));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvitationResponse>())!;
    }

    private async Task<string> ArrangeInvitationChallenge(string token, string mobile, string otp)
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
        var now = DateTimeOffset.UtcNow;
        var reference = protector.CreateToken(18);
        db.OtpChallenges.Add(new OtpChallenge(reference, IamKeys.LoginTypes.Mobile, mobile, mobile,
            "invitation_accept", protector.Hash(otp), now, now.AddMinutes(5), 5));
        await db.SaveChangesAsync();
        return reference;
    }

    private async Task<InvitationRelationFixture> CreateEligibleRelation()
    {
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var unit = await db.Units.AsNoTracking().FirstAsync(x => x.IsActive);
        var building = await db.Buildings.AsNoTracking().SingleAsync(x => x.Id == unit.BuildingId);
        var partyTypeId = await db.PartyTypes.Where(x => x.Key == PartyReferenceKeys.PartyTypes.IranianPerson)
            .Select(x => x.Id).SingleAsync();
        var relationTypeId = await db.UnitPartyRelationTypes.Where(x => x.Key == PartyReferenceKeys.RelationTypes.Owner)
            .Select(x => x.Id).SingleAsync();
        var mobileTypeId = await db.PartyContactTypes.Where(x => x.Key == PartyReferenceKeys.ContactTypes.Mobile)
            .Select(x => x.Id).SingleAsync();
        var now = DateTimeOffset.UtcNow;
        var mobile = NextMobile();
        var party = new Party(PublicCode.Create(), partyTypeId, "مالک آزمایشی", null, null, null, null, null, now);
        db.Parties.Add(party);
        await db.SaveChangesAsync();
        var relation = new UnitPartyRelation(unit.Id, party.Id, relationTypeId, null, null, null, now);
        db.UnitPartyRelations.Add(relation);
        db.PartyContacts.Add(new PartyContact(party.Id, mobileTypeId, mobile, mobile, "همراه", true, now));
        await db.SaveChangesAsync();
        return new(building.Id, building.Code, unit.Code, party.Id, party.Code, relation.Id, mobile);
    }

    private static string NextMobile() => $"+989{Random.Shared.Next(100000000, 999999999)}";

    private sealed record InvitationRelationFixture(long BuildingId, string BuildingCode, string UnitCode,
        long PartyId, string PartyCode, long RelationId, string Mobile);
}
