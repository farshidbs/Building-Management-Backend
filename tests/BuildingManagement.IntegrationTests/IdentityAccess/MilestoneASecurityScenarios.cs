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
    public async Task CleanMigrationHasUniqueCanonicalPermissionKeys()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var keys = await db.AccessPermissions.Select(x => x.Key).ToListAsync();
        Assert.NotEmpty(keys);
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("expense_finalize", keys);
        Assert.Contains("financial_unit_view_other_detail", keys);
    }

    [Fact]
    public async Task ProtectedEndpointsRejectUnauthenticatedRequests()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        using var anonymous = factory!.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v1/buildings/ABCDE")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/assets", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PostAsJsonAsync("/api/v1/financial/expenses", new { })).StatusCode);
    }

    [Fact]
    public async Task ComplexManagerCannotReadBuildingInUnrelatedComplex()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var now = DateTimeOffset.UtcNow;
        var locationId = await db.Locations.Select(x => x.Id).FirstAsync();
        var typeId = await db.BuildingTypes.Select(x => x.Id).FirstAsync();
        var otherComplex = new Complex(PublicCode.Create(), locationId, "مجتمع خارج از دسترسی",
            "تهران", "1234567890", null, null, null, now);
        db.Complexes.Add(otherComplex);
        await db.SaveChangesAsync();
        var building = new Building(PublicCode.Create(), otherComplex.Id, locationId, typeId,
            "ساختمان خارج از دسترسی", "تهران", "1234567890", null, null, null, null, null, now);
        db.Buildings.Add(building);
        await db.SaveChangesAsync();

        var response = await client!.GetAsync($"/api/v1/buildings/{building.Code}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ConcurrentWrongOtpSubmissionsCannotExceedMaxAttempts()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        string reference;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            reference = protector.CreateToken(18);
            var now = DateTimeOffset.UtcNow;
            db.OtpChallenges.Add(new OtpChallenge(reference, IamKeys.LoginTypes.Mobile,
                "09121111111", "989121111111", "register", protector.Hash("123456"), now,
                now.AddMinutes(5), 5));
            await db.SaveChangesAsync();
        }

        using var anonymous = factory!.CreateClient();
        var attempts = Enumerable.Range(0, 20).Select(index => anonymous.PostAsJsonAsync(
            "/api/v1/auth/otp/verify", new VerifyOtpRequest(reference,
                (900000 + index).ToString(System.Globalization.CultureInfo.InvariantCulture), "web")));
        var responses = await Task.WhenAll(attempts);
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var challenge = await verificationDb.OtpChallenges.SingleAsync(x => x.PublicReference == reference);
        Assert.Equal(5, challenge.AttemptCount);
        Assert.Equal(IamKeys.OtpStatuses.Blocked, challenge.StatusKey);
    }

    [Fact]
    public async Task ConcurrentCorrectOtpSubmissionHasOneConsumptionAndOneRegistration()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        const string mobile = "989129999999";
        const string code = "654321";
        string reference;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var protector = scope.ServiceProvider.GetRequiredService<IIamSecretProtector>();
            reference = protector.CreateToken(18);
            var now = DateTimeOffset.UtcNow;
            db.OtpChallenges.Add(new OtpChallenge(reference, IamKeys.LoginTypes.Mobile,
                "09129999999", mobile, "register", protector.Hash(code), now, now.AddMinutes(5), 5));
            await db.SaveChangesAsync();
        }

        using var anonymous = factory!.CreateClient();
        var responses = await Task.WhenAll(
            anonymous.PostAsJsonAsync("/api/v1/auth/otp/verify", new VerifyOtpRequest(reference, code, "web", "کاربر همزمان")),
            anonymous.PostAsJsonAsync("/api/v1/auth/otp/verify", new VerifyOtpRequest(reference, code, "web", "کاربر همزمان")));
        Assert.Equal(1, responses.Count(x => x.IsSuccessStatusCode));

        await using var verificationScope = factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var challenge = await verificationDb.OtpChallenges.SingleAsync(x => x.PublicReference == reference);
        Assert.Equal(IamKeys.OtpStatuses.Consumed, challenge.StatusKey);
        Assert.Equal(1, await verificationDb.UserLoginMethods.CountAsync(x => x.NormalizedIdentifierValue == mobile));
    }

    [Fact]
    public async Task EndedSourceUnitRelationImmediatelyStopsAuthorizingMembership()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var authorization = scope.ServiceProvider.GetRequiredService<AccessAuthorizationService>();
        var now = DateTimeOffset.UtcNow;
        var unit = await db.Units.FirstAsync();
        var partyTypeId = await db.PartyTypes.Where(x => x.Key == "person").Select(x => x.Id).SingleAsync();
        var party = new Party(PublicCode.Create(), partyTypeId, "مستأجر امنیتی", null, null, null,
            null, null, now);
        var user = new User(PublicCode.Create(), now);
        db.AddRange(party, user);
        await db.SaveChangesAsync();
        var relationTypeId = await db.UnitPartyRelationTypes.Where(x => x.Key == "tenant")
            .Select(x => x.Id).SingleAsync();
        var relation = new UnitPartyRelation(unit.Id, party.Id, relationTypeId, now, null, null, now);
        db.UnitPartyRelations.Add(relation);
        await db.SaveChangesAsync();
        var roleId = await db.AccessRoles.Where(x => x.Key == "unit_tenant").Select(x => x.Id).SingleAsync();
        db.AccessMemberships.Add(new AccessMembership(PublicCode.Create(), user.Id, roleId, null,
            null, unit.Id, relation.Id, now));
        await db.SaveChangesAsync();

        await authorization.Ensure(user.Id, "occupancy_view", null, unit.BuildingId, unit.Id,
            CancellationToken.None);
        relation.End(now, now);
        await db.SaveChangesAsync();
        var denied = await Assert.ThrowsAsync<AppException>(() => authorization.Ensure(user.Id,
            "occupancy_view", null, unit.BuildingId, unit.Id, CancellationToken.None));
        Assert.Equal(403, denied.Status);
        Assert.True(await db.AccessMemberships.AnyAsync(x => x.SourceUnitPartyRelationId == relation.Id));
    }
}
