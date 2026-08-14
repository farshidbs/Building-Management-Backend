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
    public async Task ConcurrentFirstComplexRequestsCreateOnlyOneRootMembership()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var marker = Guid.NewGuid().ToString("N");
        var location = await CreateLocationFixture(new LocationRequest(null, $"Concurrent {marker}",
            ReferenceKeys.LocationTypes.Country));
        var actor = await CreateAuthenticatedClientWithIdentity();
        using var authenticated = actor.Client;

        var requests = Enumerable.Range(1, 2).Select(index => authenticated.PostAsJsonAsync(
            "/api/v1/complexes", new ComplexRequest(location.Code, $"Concurrent root {marker}-{index}",
                marker, marker[..10], null, null, null))).ToArray();
        var responses = await Task.WhenAll(requests);

        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.Single(responses, x => x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(1, await db.Complexes.CountAsync(x => x.Address == marker));
        Assert.Single(await db.AccessMemberships.Where(x => x.UserId == actor.UserId && x.IsActive &&
            x.EndsAtUtc == null).ToListAsync());
    }

    [Fact]
    public async Task ConcurrentFirstComplexAndStandaloneBuildingCreateOnlyOneRootMembership()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var marker = Guid.NewGuid().ToString("N");
        var location = await CreateLocationFixture(new LocationRequest(null, $"Mixed concurrent {marker}",
            ReferenceKeys.LocationTypes.Country));
        var actor = await CreateAuthenticatedClientWithIdentity();
        using var authenticated = actor.Client;

        var complexTask = authenticated.PostAsJsonAsync("/api/v1/complexes", new ComplexRequest(
            location.Code, $"Mixed complex {marker}", marker, marker[..10], null, null, null));
        var buildingTask = authenticated.PostAsJsonAsync("/api/v1/buildings", new BuildingRequest(null,
            location.Code, ReferenceKeys.BuildingTypes.Residential, $"Mixed building {marker}", marker,
            marker[..10], null, null, 1, 1400, null));
        var responses = await Task.WhenAll(complexTask, buildingTask);

        Assert.Single(responses, x => x.IsSuccessStatusCode);
        Assert.Single(responses, x => x.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.Forbidden);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var roots = await db.Complexes.CountAsync(x => x.Address == marker) +
                    await db.Buildings.CountAsync(x => x.Address == marker);
        Assert.Equal(1, roots);
        Assert.Single(await db.AccessMemberships.Where(x => x.UserId == actor.UserId && x.IsActive &&
            x.EndsAtUtc == null).ToListAsync());
    }

    [Fact]
    public async Task UserWithoutMembershipCanCreateFirstComplexAndReceivesOnlyManagerMembership()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var location = await CreateLocationFixture(new LocationRequest(null, $"Onboarding {Guid.NewGuid():N}",
            ReferenceKeys.LocationTypes.Country));
        var actor = await CreateAuthenticatedClientWithIdentity();
        using var authenticated = actor.Client;

        var response = await authenticated.PostAsJsonAsync("/api/v1/complexes", new ComplexRequest(
            location.Code, "مجتمع نخست", "تهران", "1234567890", null, null, null));
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<ComplexResponse>())!;

        Assert.Equal(HttpStatusCode.OK,
            (await authenticated.GetAsync($"/api/v1/complexes/{created.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await authenticated.PutAsJsonAsync($"/api/v1/complexes/{created.Code}",
            new ComplexRequest(location.Code, "مجتمع نخست ویرایش‌شده", "تهران", "1234567890",
                null, null, null))).StatusCode);
        var page = await authenticated.GetFromJsonAsync<Page<ComplexResponse>>("/api/v1/complexes?pageSize=100");
        Assert.Contains(page!.Items, x => x.Code == created.Code);

        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var memberships = await db.AccessMemberships.Where(x => x.UserId == actor.UserId).ToListAsync();
        var membership = Assert.Single(memberships);
        Assert.Equal(created.Code, await db.Complexes.Where(x => x.Id == membership.ComplexId)
            .Select(x => x.Code).SingleAsync());
        Assert.Equal("complex_manager", await db.AccessRoles.Where(x => x.Id == membership.RoleId)
            .Select(x => x.Key).SingleAsync());
    }

    [Fact]
    public async Task UserWithoutMembershipCanCreateFirstStandaloneBuildingButNotBuildingInExistingComplex()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var location = await CreateLocationFixture(new LocationRequest(null, $"Standalone {Guid.NewGuid():N}",
            ReferenceKeys.LocationTypes.Country));
        var actor = await CreateAuthenticatedClientWithIdentity();
        using var authenticated = actor.Client;

        string existingComplexCode;
        string existingLocationCode;
        await using (var initialScope = factory!.Services.CreateAsyncScope())
        {
            var initialDb = initialScope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var existing = await initialDb.Complexes.Select(x => new
            {
                x.Code,
                LocationCode = initialDb.Locations.Where(location => location.Id == x.LocationId)
                    .Select(location => location.Code).Single()
            }).FirstAsync();
            existingComplexCode = existing.Code;
            existingLocationCode = existing.LocationCode;
        }
        var nested = await authenticated.PostAsJsonAsync("/api/v1/buildings", new BuildingRequest(
            existingComplexCode, existingLocationCode, ReferenceKeys.BuildingTypes.Residential,
            "ساختمان غیرمجاز", "تهران", "1234567890", null, null, 1, 1400, null));
        Assert.Equal(HttpStatusCode.Forbidden, nested.StatusCode);

        var response = await authenticated.PostAsJsonAsync("/api/v1/buildings", new BuildingRequest(null,
            location.Code, ReferenceKeys.BuildingTypes.Residential, "ساختمان نخست", "تهران",
            "1234567890", null, null, 3, 1400, null));
        response.EnsureSuccessStatusCode();
        var created = (await response.Content.ReadFromJsonAsync<BuildingResponse>())!;
        Assert.Equal(HttpStatusCode.OK,
            (await authenticated.GetAsync($"/api/v1/buildings/{created.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await authenticated.PutAsJsonAsync($"/api/v1/buildings/{created.Code}",
            new BuildingRequest(null, location.Code, ReferenceKeys.BuildingTypes.Residential,
                "ساختمان نخست ویرایش‌شده", "تهران", "1234567890", null, null, 3, 1400, null))).StatusCode);
        var page = await authenticated.GetFromJsonAsync<Page<BuildingResponse>>("/api/v1/buildings?pageSize=100");
        Assert.Contains(page!.Items, x => x.Code == created.Code);

        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var membership = Assert.Single(await db.AccessMemberships.Where(x => x.UserId == actor.UserId).ToListAsync());
        Assert.Equal(created.Code, await db.Buildings.Where(x => x.Id == membership.BuildingId)
            .Select(x => x.Code).SingleAsync());
        Assert.Equal("building_manager", await db.AccessRoles.Where(x => x.Id == membership.RoleId)
            .Select(x => x.Key).SingleAsync());
    }

    [Fact]
    public async Task ExistingUnrelatedMembershipDoesNotEnableFirstScopeException()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var location = await CreateLocationFixture(new LocationRequest(null, $"Restricted {Guid.NewGuid():N}",
            ReferenceKeys.LocationTypes.Country));
        long unitId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            unitId = await db.Units.Select(x => x.Id).FirstAsync();
        }
        using var resident = await CreateAuthenticatedClient("unit_resident", unitId: unitId);

        var response = await resident.PostAsJsonAsync("/api/v1/complexes", new ComplexRequest(
            location.Code, "مجتمع غیرمجاز", "تهران", "1234567890", null, null, null));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExistingPartyReferenceRequiresPriorVisibilityAcrossRelationAndOccupancyWorkflows()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        long buildingAId;
        Unit unitA;
        Unit secondUnitA;
        Party visibleParty;
        Party foreignParty;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            buildingAId = await db.Units.GroupBy(x => x.BuildingId).Where(x => x.Count() >= 2)
                .Select(x => x.Key).FirstAsync();
            var unitsA = await db.Units.Where(x => x.BuildingId == buildingAId).Take(2).ToListAsync();
            unitA = unitsA[0];
            secondUnitA = unitsA[1];
            var unitB = await db.Units.FirstAsync(x => x.BuildingId != buildingAId);
            var partyTypeId = await db.PartyTypes.Where(x => x.Key == PartyReferenceKeys.PartyTypes.IranianPerson)
                .Select(x => x.Id).SingleAsync();
            var ownerTypeId = await db.UnitPartyRelationTypes.Where(x => x.Key == PartyReferenceKeys.RelationTypes.Owner)
                .Select(x => x.Id).SingleAsync();
            visibleParty = new Party(PublicCode.Create(), partyTypeId, "شخص قابل مشاهده", null, null, null,
                null, null, DateTimeOffset.UtcNow);
            foreignParty = new Party(PublicCode.Create(), partyTypeId, "شخص خارج از محدوده", null, null, null,
                null, null, DateTimeOffset.UtcNow);
            db.Parties.AddRange(visibleParty, foreignParty);
            await db.SaveChangesAsync();
            db.UnitPartyRelations.AddRange(
                new UnitPartyRelation(unitA.Id, visibleParty.Id, ownerTypeId, null, null, null, DateTimeOffset.UtcNow),
                new UnitPartyRelation(unitB.Id, foreignParty.Id, ownerTypeId, null, null, null, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: buildingAId);

        var deniedRelation = await manager.PostAsJsonAsync($"/api/v1/units/{secondUnitA.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(foreignParty.Code, null)));
        Assert.Equal(HttpStatusCode.Forbidden, deniedRelation.StatusCode);
        var deniedOccupancy = await manager.PostAsJsonAsync($"/api/v1/units/{secondUnitA.Code}/occupancy-history",
            new OccupancyChangeRequest(1, null,
            [new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Resident,
                new PartySelectionRequest(foreignParty.Code, null))]));
        Assert.Equal(HttpStatusCode.Forbidden, deniedOccupancy.StatusCode);

        var allowed = await manager.PostAsJsonAsync($"/api/v1/units/{secondUnitA.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(visibleParty.Code, null)));
        allowed.EnsureSuccessStatusCode();

        await using var verification = factory!.Services.CreateAsyncScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False(await verificationDb.UnitPartyRelations.AnyAsync(x =>
            x.UnitId == secondUnitA.Id && x.PartyId == foreignParty.Id));
        Assert.Equal(secondUnitA.CurrentOccupantsCount,
            await verificationDb.Units.Where(x => x.Id == secondUnitA.Id)
                .Select(x => x.CurrentOccupantsCount).SingleAsync());
        Assert.True(await verificationDb.UnitPartyRelations.AnyAsync(x =>
            x.UnitId == secondUnitA.Id && x.PartyId == visibleParty.Id));
    }

    [Fact]
    public async Task OwnIdentityPartyCanBeReusedWithoutMakingItGloballyVisibleOrDuplicatingIt()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        long buildingId;
        Unit unit;
        long unrelatedBuildingId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            unit = await db.Units.FirstAsync();
            buildingId = unit.BuildingId;
            unrelatedBuildingId = await db.Buildings.Where(x => x.Id != buildingId).Select(x => x.Id).FirstAsync();
        }
        var actor = await CreateAuthenticatedClientWithIdentity("building_manager", buildingId: buildingId);
        using var manager = actor.Client;
        Party selfParty;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var partyTypeId = await db.PartyTypes.Where(x => x.Key == PartyReferenceKeys.PartyTypes.IranianPerson)
                .Select(x => x.Id).SingleAsync();
            selfParty = new Party(PublicCode.Create(), partyTypeId, "پروفایل شخصی", null, null, null,
                null, null, DateTimeOffset.UtcNow);
            db.Parties.Add(selfParty);
            await db.SaveChangesAsync();
            db.UserPartyLinks.Add(new UserPartyLink(PublicCode.Create(), actor.UserId, selfParty.Id,
                true, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var response = await manager.PostAsJsonAsync($"/api/v1/units/{unit.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(selfParty.Code, null)));
        response.EnsureSuccessStatusCode();

        await using var verification = factory!.Services.CreateAsyncScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(1, await verificationDb.Parties.CountAsync(x => x.Code == selfParty.Code));
        Assert.True(await verificationDb.UnitPartyRelations.AnyAsync(x =>
            x.UnitId == unit.Id && x.PartyId == selfParty.Id));
        using var unrelated = await CreateAuthenticatedClient("building_manager", buildingId: unrelatedBuildingId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unrelated.GetAsync($"/api/v1/parties/{selfParty.Code}")).StatusCode);
    }

    [Fact]
    public async Task ScopedNewPartyInputPersistsAtomicallyAndInvalidContactRollsBack()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        Unit unit;
        long unrelatedBuildingId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            unit = await db.Units.FirstAsync();
            unrelatedBuildingId = await db.Buildings.Where(x => x.Id != unit.BuildingId)
                .Select(x => x.Id).FirstAsync();
        }
        using var manager = await CreateAuthenticatedClient("building_manager", buildingId: unit.BuildingId);
        var displayName = $"شخص جدید {Guid.NewGuid():N}";
        var success = await manager.PostAsJsonAsync($"/api/v1/units/{unit.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(null, new NewPartyInput(
                    new PartyRequest(PartyReferenceKeys.PartyTypes.IranianPerson, displayName),
                    [new PartyContactRequest(PartyReferenceKeys.ContactTypes.Mobile, "09125550123", IsPrimary: true)]))));
        success.EnsureSuccessStatusCode();
        var relation = (await success.Content.ReadFromJsonAsync<UnitPartyRelationResponse>())!;
        Assert.Equal(HttpStatusCode.OK,
            (await manager.GetAsync($"/api/v1/parties/{relation.PartyCode}")).StatusCode);
        using var unrelated = await CreateAuthenticatedClient("building_manager", buildingId: unrelatedBuildingId);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unrelated.GetAsync($"/api/v1/parties/{relation.PartyCode}")).StatusCode);

        var orphanName = $"Orphan {Guid.NewGuid():N}";
        var failed = await manager.PostAsJsonAsync($"/api/v1/units/{unit.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(null, new NewPartyInput(
                    new PartyRequest(PartyReferenceKeys.PartyTypes.IranianPerson, orphanName),
                    [new PartyContactRequest("missing_contact_type", "09120000001")]))));
        Assert.Equal(HttpStatusCode.NotFound, failed.StatusCode);

        await using var verification = factory!.Services.CreateAsyncScope();
        var dbVerification = verification.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var createdParty = await dbVerification.Parties.SingleAsync(x => x.Code == relation.PartyCode);
        Assert.True(await dbVerification.PartyContacts.AnyAsync(x => x.PartyId == createdParty.Id));
        Assert.True(await dbVerification.UnitPartyRelations.AnyAsync(x =>
            x.UnitId == unit.Id && x.PartyId == createdParty.Id));
        Assert.False(await dbVerification.Parties.AnyAsync(x => x.DisplayName == orphanName));
    }
}
