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

public sealed partial class ApiScenarios
{
    [Fact]
    public async Task PartyAndOccupancyFoundationWorks()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var minimalParty = await Post<PartyResponse>("/api/v1/parties",
            new PartyRequest(PartyReferenceKeys.PartyTypes.IranianPerson,
                $"ساکن بدون اطلاعات تماس {suffix}"));
        Assert.Null(minimalParty.IdentityNumber);
        var minimalContacts = await client!.GetFromJsonAsync<PartyContactResponse[]>(
            $"/api/v1/parties/{minimalParty.Code}/contacts");
        Assert.Empty(minimalContacts!);

        var party = await Post<PartyResponse>("/api/v1/parties",
            new PartyRequest(PartyReferenceKeys.PartyTypes.IranianPerson, $"ساکن واحد {suffix}",
                IdentityNumber: "0012345678"));
        Assert.NotEmpty(party.Code);
        Assert.Equal("0012345678", party.IdentityNumber);

        var contacts = await client!.GetFromJsonAsync<PartyContactResponse[]>(
            $"/api/v1/parties/{party.Code}/contacts");
        Assert.Empty(contacts!);

        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts",
            new PartyContactRequest(PartyReferenceKeys.ContactTypes.Mobile, "09120000000", IsPrimary: true));
        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts",
            new PartyContactRequest(PartyReferenceKeys.ContactTypes.Mobile, "09350000000"));
        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts",
            new PartyContactRequest(PartyReferenceKeys.ContactTypes.Mobile, "09900000000"));
        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts",
            new PartyContactRequest(PartyReferenceKeys.ContactTypes.Email, "owner@example.com",
                IsPrimary: true));

        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts/set-primary",
            new PartyContactSelectorRequest(PartyReferenceKeys.ContactTypes.Mobile, "09350000000"));
        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts/set-primary",
            new PartyContactSelectorRequest(PartyReferenceKeys.ContactTypes.Mobile, "09350000000"));
        var selectedContacts = (await client!.GetFromJsonAsync<PartyContactResponse[]>(
            $"/api/v1/parties/{party.Code}/contacts"))!;
        Assert.False(selectedContacts.Single(x => x.Value == "09120000000").IsPrimary);
        Assert.True(selectedContacts.Single(x => x.Value == "09350000000").IsPrimary);
        Assert.False(selectedContacts.Single(x => x.Value == "09900000000").IsPrimary);
        Assert.True(selectedContacts.Single(x => x.Value == "owner@example.com").IsPrimary);

        using var wrongPartyPrimary = await client!.PostAsJsonAsync(
            $"/api/v1/parties/{minimalParty.Code}/contacts/set-primary",
            new PartyContactSelectorRequest(PartyReferenceKeys.ContactTypes.Mobile, "09350000000"));
        Assert.Equal(HttpStatusCode.NotFound, wrongPartyPrimary.StatusCode);

        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var partyId = await db.Parties.Where(x => x.Code == party.Code).Select(x => x.Id).SingleAsync();
            var mobileTypeId = await db.PartyContactTypes
                .Where(x => x.Key == PartyReferenceKeys.ContactTypes.Mobile).Select(x => x.Id).SingleAsync();
            var inactive = await db.PartyContacts.SingleAsync(x => x.PartyId == partyId &&
                x.PartyContactTypeId == mobileTypeId && x.NormalizedValue == "09900000000");
            inactive.SetActivation(false, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        using var inactivePrimary = await client!.PostAsJsonAsync(
            $"/api/v1/parties/{party.Code}/contacts/set-primary",
            new PartyContactSelectorRequest(PartyReferenceKeys.ContactTypes.Mobile, "09900000000"));
        Assert.Equal(HttpStatusCode.NotFound, inactivePrimary.StatusCode);

        var updatedContact = await Put<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts",
            new PartyContactUpdateRequest(PartyReferenceKeys.ContactTypes.Mobile, "09120000000",
                "0912 111 2233", "شماره دوم"));
        Assert.Equal("0912 111 2233", updatedContact.Value);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            Assert.True(await db.PartyContacts.AnyAsync(x => x.Value == "0912 111 2233" &&
                x.NormalizedValue == "09121112233"));
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var partyId = await db.Parties.Where(x => x.Code == party.Code).Select(x => x.Id).SingleAsync();
            var mobileTypeId = await db.PartyContactTypes
                .Where(x => x.Key == PartyReferenceKeys.ContactTypes.Mobile).Select(x => x.Id).SingleAsync();
            db.PartyContacts.Add(new PartyContact(partyId, mobileTypeId, "09010000000",
                "09010000000", null, true, DateTimeOffset.UtcNow));
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        await Post<PartyContactResponse>($"/api/v1/parties/{party.Code}/contacts",
            new PartyContactRequest(PartyReferenceKeys.ContactTypes.Mobile, "09020000000",
                IsPrimary: true));
        var contactsAfterPrimaryAdd = (await client!.GetFromJsonAsync<PartyContactResponse[]>(
            $"/api/v1/parties/{party.Code}/contacts"))!;
        Assert.True(contactsAfterPrimaryAdd.Single(x => x.Value == "09020000000").IsPrimary);
        Assert.False(contactsAfterPrimaryAdd.Single(x => x.Value == "09350000000").IsPrimary);
        Assert.True(contactsAfterPrimaryAdd.Single(x => x.Value == "owner@example.com").IsPrimary);

        var country = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(null, $"Party country {suffix}", ReferenceKeys.LocationTypes.Country));
        var city = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(country.Code, $"Party city {suffix}", ReferenceKeys.LocationTypes.City));
        var building = await Post<BuildingResponse>("/api/v1/buildings",
            new BuildingRequest(null, city.Code, ReferenceKeys.BuildingTypes.Residential,
                $"Party building {suffix}", "Address", suffix, null, null, 2, 2020, null));
        var secondBuilding = await Post<BuildingResponse>("/api/v1/buildings",
            new BuildingRequest(null, city.Code, ReferenceKeys.BuildingTypes.Residential,
                $"Second party building {suffix}", "Address 2", $"2{suffix}", null, null,
                2, 2021, null));

        var vacant = await Post<UnitResponse>($"/api/v1/buildings/{building.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential, ReferenceKeys.UnitStatuses.Available,
                $"V-{suffix}", 1, 80, 2, 0, 0, null,
                new UnitOccupancyRequest(ReferenceKeys.UnitStatuses.Vacant, 0)));
        Assert.Equal(0, vacant.CurrentOccupancy.OccupantsCount);
        Assert.Equal(ReferenceKeys.UnitStatuses.Vacant, vacant.CurrentOccupancy.Status);
        await Post<UnitPartyRelationResponse>($"/api/v1/units/{vacant.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(party.Code, null)));
        var vacantRelations = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{vacant.Code}/parties?currentOnly=true");
        Assert.Contains(vacantRelations!, x => x.RelationType.Key == PartyReferenceKeys.RelationTypes.Owner);
        Assert.Null(vacantRelations!.Single(x =>
            x.RelationType.Key == PartyReferenceKeys.RelationTypes.Owner).StartDate);
        var vacantHistory = await client!.GetFromJsonAsync<UnitOccupancyHistoryResponse[]>(
            $"/api/v1/units/{vacant.Code}/occupancy-history");
        Assert.Null(vacantHistory!.Single(x => x.IsActive).EffectiveFrom);

        await Post<UnitPartyRelationResponse>($"/api/v1/units/{vacant.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(minimalParty.Code, null)));
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var softDeletedRelation = await db.UnitPartyRelations.SingleAsync(relation =>
                db.Units.Any(unit => unit.Id == relation.UnitId && unit.Code == vacant.Code) &&
                db.Parties.Any(relatedParty => relatedParty.Id == relation.PartyId &&
                    relatedParty.Code == minimalParty.Code));
            softDeletedRelation.SoftDelete(DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }
        var currentAfterSoftDelete = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{vacant.Code}/parties?currentOnly=true");
        Assert.DoesNotContain(currentAfterSoftDelete!, x => x.PartyCode == minimalParty.Code);

        var secondBuildingUnit = await Post<UnitResponse>(
            $"/api/v1/buildings/{secondBuilding.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential,
                ReferenceKeys.UnitStatuses.Available, $"R-{suffix}", 1, 75, 2, 0, 0, null,
                new UnitOccupancyRequest(ReferenceKeys.UnitStatuses.Vacant, 0)));
        await Post<UnitPartyRelationResponse>(
            $"/api/v1/units/{secondBuildingUnit.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(party.Code, null)));
        var reusedPartyRelations = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{secondBuildingUnit.Code}/parties?currentOnly=true");
        Assert.Contains(reusedPartyRelations!, x => x.PartyCode == party.Code);

        var occupied = await Post<UnitResponse>($"/api/v1/buildings/{building.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential, ReferenceKeys.UnitStatuses.Available,
                $"O-{suffix}", 1, 90, 2, 0, 0, null,
                new UnitOccupancyRequest(ReferenceKeys.UnitStatuses.Occupied, 3, DateTimeOffset.UtcNow,
                [
                    new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Resident,
                        new PartySelectionRequest(party.Code, null)),
                    new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                        new PartySelectionRequest(party.Code, null))
                ])));
        Assert.Equal(3, occupied.CurrentOccupancy.OccupantsCount);
        var beforeCountChange = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{occupied.Code}/parties?currentOnly=true");

        var changed = await Post<CurrentOccupancyResponse>(
            $"/api/v1/units/{occupied.Code}/occupancy-history",
            new OccupancyChangeRequest(4, DateTimeOffset.UtcNow.AddMinutes(1)));
        Assert.Equal(4, changed.OccupantsCount);
        var afterCountChange = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{occupied.Code}/parties?currentOnly=true");
        Assert.Equal(beforeCountChange!.Length, afterCountChange!.Length);

        var vacantAgain = await Post<CurrentOccupancyResponse>(
            $"/api/v1/units/{occupied.Code}/occupancy-history",
            new OccupancyChangeRequest(0, DateTimeOffset.UtcNow.AddMinutes(2)));
        Assert.Equal(0, vacantAgain.OccupantsCount);
        var relations = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{occupied.Code}/parties?currentOnly=true");
        Assert.Contains(relations!, x => x.RelationType.Key == PartyReferenceKeys.RelationTypes.Owner);
        Assert.DoesNotContain(relations!, x => x.RelationType.Key == PartyReferenceKeys.RelationTypes.Resident);
        var historicalRelations = await client!.GetFromJsonAsync<UnitPartyRelationResponse[]>(
            $"/api/v1/units/{occupied.Code}/parties?currentOnly=false");
        var endedResident = historicalRelations!.Single(x =>
            x.RelationType.Key == PartyReferenceKeys.RelationTypes.Resident);
        Assert.NotNull(endedResident.EndDate);
        Assert.True(endedResident.IsActive);

        var history = await client!.GetFromJsonAsync<UnitOccupancyHistoryResponse[]>(
            $"/api/v1/units/{occupied.Code}/occupancy-history");
        Assert.Equal(3, history!.Length);
        Assert.Equal(0, history.Single(x => x.IsActive).OccupantsCount);

        using var invalid = await client!.PostAsJsonAsync($"/api/v1/buildings/{building.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential, ReferenceKeys.UnitStatuses.Available,
                $"I-{suffix}", 1, 70, 1, 0, 0, null,
                new UnitOccupancyRequest(ReferenceKeys.UnitStatuses.Occupied, 1, DateTimeOffset.UtcNow)));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        var orphanName = $"Orphan {suffix}";
        using var failedRelation = await client!.PostAsJsonAsync(
            $"/api/v1/units/{vacant.Code}/party-relations",
            new UnitOnboardingRelationRequest(PartyReferenceKeys.RelationTypes.Owner,
                new PartySelectionRequest(null, new NewPartyInput(
                    new PartyRequest(PartyReferenceKeys.PartyTypes.IranianPerson, orphanName),
                    [new PartyContactRequest("missing_contact_type", "09121111111")]))));
        Assert.Equal(HttpStatusCode.NotFound, failedRelation.StatusCode);
        var orphanSearch = await client!.GetFromJsonAsync<Page<PartySummaryResponse>>(
            $"/api/v1/parties?search={Uri.EscapeDataString(orphanName)}");
        Assert.Equal(0, orphanSearch!.TotalCount);
    }

}
