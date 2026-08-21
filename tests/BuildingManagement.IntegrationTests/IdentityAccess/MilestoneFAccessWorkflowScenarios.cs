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
    public async Task MembershipExitApprovalEndsOnlyMembershipAndRetainsDecisionHistory()
    {
        RequireMilestoneFSql();
        long unitId; string unitCode; string membershipCode; int relationCount;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var unit = await db.Units.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
            unitId = unit.Id; unitCode = unit.Code;
            relationCount = await db.UnitPartyRelations.CountAsync(x => x.UnitId == unitId);
        }
        var member = await CreateAuthenticatedClientWithIdentity("unit_resident", unitId: unitId);
        using var memberClient = member.Client;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            membershipCode = await db.AccessMemberships.Where(x => x.UserId == member.UserId && x.UnitId == unitId)
                .Select(x => x.Code).SingleAsync();
        }
        var requested = await memberClient.PostAsJsonAsync($"/api/v1/me/memberships/{membershipCode}/exit-request",
            new MembershipExitRequestDto("خروج آزمایشی"));
        Assert.Equal(HttpStatusCode.Created, requested.StatusCode);
        var exit = (await requested.Content.ReadFromJsonAsync<MembershipExitResponse>())!;
        var approved = await client!.PostAsJsonAsync($"/api/v1/membership-exit-requests/{exit.Code}/approve",
            new MembershipExitDecisionRequest("تأیید"));
        Assert.Equal(HttpStatusCode.NoContent, approved.StatusCode);

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var membership = await verifyDb.AccessMemberships.SingleAsync(x => x.Code == membershipCode);
        var decision = await verifyDb.MembershipExitRequests.SingleAsync(x => x.Code == exit.Code);
        Assert.False(membership.IsActive); Assert.NotNull(membership.EndsAtUtc);
        Assert.Equal("approved", decision.StatusKey); Assert.False(decision.IsActive);
        Assert.Equal(relationCount, await verifyDb.UnitPartyRelations.CountAsync(x => x.UnitId == unitId));
        Assert.True(await verifyDb.SecurityAuditEvents.AnyAsync(x =>
            x.EventTypeKey == "membership_exit_approved" && x.ResourceCode == exit.Code));
    }

    [Fact]
    public async Task IndividualGrantCreatesNoMembershipAndRevocationIsImmediateAndAudited()
    {
        RequireMilestoneFSql();
        var target = await CreateAuthenticatedClientWithIdentity();
        using var targetClient = target.Client;
        string userCode; string unitCode; int membershipsBefore;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            userCode = await db.Users.Where(x => x.Id == target.UserId).Select(x => x.Code).SingleAsync();
            unitCode = await db.Units.OrderBy(x => x.Id).Select(x => x.Code).FirstAsync();
            membershipsBefore = await db.AccessMemberships.CountAsync(x => x.UserId == target.UserId);
        }
        var created = await client!.PostAsJsonAsync("/api/v1/access-grants", new CreateAccessGrantRequest(
            userCode, "financial_unit_pay", "unit", unitCode, null, DateTimeOffset.UtcNow.AddHours(1),
            "پرداخت موقت"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var grant = (await created.Content.ReadFromJsonAsync<AccessGrantResponse>())!;

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var persisted = await db.AccessGrants.SingleAsync(x => x.Code == grant.Code);
            Assert.True(persisted.IsActive); Assert.Null(persisted.RevokedAtUtc);
            Assert.Equal(membershipsBefore, await db.AccessMemberships.CountAsync(x => x.UserId == target.UserId));
        }
        using var revoked = await client!.PostAsync($"/api/v1/access-grants/{grant.Code}/revoke", null);
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var revokedGrant = await verifyDb.AccessGrants.SingleAsync(x => x.Code == grant.Code);
        Assert.False(revokedGrant.IsActive); Assert.NotNull(revokedGrant.RevokedAtUtc);
        Assert.True(await verifyDb.SecurityAuditEvents.AnyAsync(x =>
            x.EventTypeKey == "access_grant_revoked" && x.ResourceCode == grant.Code));
    }

    private void RequireMilestoneFSql()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
    }
}
