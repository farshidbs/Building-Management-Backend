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

public sealed class ApiScenarios : IAsyncLifetime
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

    [Fact]
    public async Task PhysicalStructureAndReferenceDataScenariosWork()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");

        var referenceData = await client!.GetFromJsonAsync<ReferenceDataResponse>("/api/v1/reference-data");
        Assert.Contains(referenceData!.BuildingTypes, value => value.Key == ReferenceKeys.BuildingTypes.Residential);

        var country = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(null, "کشور آزمایشی", ReferenceKeys.LocationTypes.Country));
        Assert.Matches("^[A-Z0-9]{5}$", country.Code);
        var city = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(country.Code, "شهر آزمایشی", ReferenceKeys.LocationTypes.City));
        var complex = await Post<ComplexResponse>("/api/v1/complexes",
            new ComplexRequest(country.Code, "مجتمع آزمایشی", "نشانی", "۱۲۳", null, null, null));
        var building = await Post<BuildingResponse>("/api/v1/buildings",
            new BuildingRequest(complex.Code, city.Code, ReferenceKeys.BuildingTypes.Residential,
                "ساختمان آزمایشی", "نشانی", "۱۲۳", null, null, 2, 2020, null));
        Assert.Equal(ReferenceKeys.BuildingTypes.Residential, building.BuildingType.Key);
        Assert.Equal(complex.Code, building.Complex!.Code);
        Assert.Equal(complex.Name, building.Complex.Name);
        Assert.Equal(city.Code, building.Location.Code);
        var unit = await Post<UnitResponse>($"/api/v1/buildings/{building.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential, ReferenceKeys.UnitStatuses.Available,
                "۱۰۱", 1, 80, 2, 1, 0, null,
                new UnitOccupancyRequest(ReferenceKeys.UnitStatuses.Vacant, 0, DateTimeOffset.UtcNow)));
        Assert.Equal(ReferenceKeys.UnitStatuses.Available, unit.Status.Key);
        Assert.Equal(building.Code, unit.Building.Code);
        Assert.Equal(building.Name, unit.Building.Name);
        Assert.Equal(complex.Code, unit.Complex!.Code);

        var duplicateUnit = await client!.PostAsJsonAsync($"/api/v1/buildings/{building.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential, ReferenceKeys.UnitStatuses.Available,
                " ۱۰۱ ", 1, 80, 2, 1, 0, null,
                new UnitOccupancyRequest(ReferenceKeys.UnitStatuses.Vacant, 0, DateTimeOffset.UtcNow)));
        Assert.Equal(HttpStatusCode.Conflict, duplicateUnit.StatusCode);
    }

    [Fact]
    public async Task InvalidRequestBodiesReturnBadRequest()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");

        foreach (var uri in new[]
                 {
                     "/api/v1/locations",
                     "/api/v1/complexes",
                     "/api/v1/buildings",
                     "/api/v1/buildings/ABCDE/units"
                 })
        {
            using var response = await client!.PostAsJsonAsync(uri, new { });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        using var invalidJson = new StringContent("{", System.Text.Encoding.UTF8, "application/json");
        using var malformedResponse = await client!.PostAsync("/api/v1/locations", invalidJson);
        Assert.Equal(HttpStatusCode.BadRequest, malformedResponse.StatusCode);

        using var nullJson = new StringContent("null", System.Text.Encoding.UTF8, "application/json");
        using var nullResponse = await client!.PostAsync("/api/v1/locations", nullJson);
        Assert.Equal(HttpStatusCode.BadRequest, nullResponse.StatusCode);
    }

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

    [Fact]
    public async Task BuildingGalleryUploadDownloadAndDeleteWorks()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var country = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(null, $"File country {suffix}", ReferenceKeys.LocationTypes.Country));
        var city = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(country.Code, $"File city {suffix}", ReferenceKeys.LocationTypes.City));
        var building = await Post<BuildingResponse>("/api/v1/buildings",
            new BuildingRequest(null, city.Code, ReferenceKeys.BuildingTypes.Residential,
                $"File building {suffix}", "Address", suffix, null, null, 1, 2020, null));

        using var form = new MultipartFormDataContent();
        using var content = new ByteArrayContent([0x89, 0x50, 0x4e, 0x47]);
        content.Headers.ContentType = new("image/png");
        form.Add(content, "file", "front.png");
        form.Add(new StringContent("نمای اصلی"), "title");
        form.Add(new StringContent("true"), "isCover");
        using var upload = await client!.PostAsync($"/api/v1/buildings/{building.Code}/gallery", form);
        upload.EnsureSuccessStatusCode();
        var gallery = (await upload.Content.ReadFromJsonAsync<GalleryFileResponse>())!;
        Assert.True(gallery.IsCover);
        Assert.Matches("^[A-Z0-9]{5}$", gallery.File.Code);

        using var download = await client.GetAsync($"/api/v1/files/{gallery.File.Code}/content");
        download.EnsureSuccessStatusCode();
        Assert.Equal("image/png", download.Content.Headers.ContentType!.MediaType);

        using var delete = await client.DeleteAsync($"/api/v1/buildings/{building.Code}/gallery/{gallery.Code}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        using var missing = await client.GetAsync($"/api/v1/files/{gallery.File.Code}/content");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task AssetScopeEventsDerivedReviewAndGalleryCoverWork()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var country = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(null, $"کشور دارایی {suffix}", ReferenceKeys.LocationTypes.Country));
        var city = await Post<LocationResponse>("/api/v1/locations",
            new LocationRequest(country.Code, $"شهر دارایی {suffix}", ReferenceKeys.LocationTypes.City));
        var complex = await Post<ComplexResponse>("/api/v1/complexes",
            new ComplexRequest(city.Code, $"مجتمع دارایی {suffix}", "نشانی", suffix, null, null, null));
        var building = await Post<BuildingResponse>("/api/v1/buildings",
            new BuildingRequest(complex.Code, city.Code, ReferenceKeys.BuildingTypes.Residential,
                $"ساختمان دارایی {suffix}", "نشانی", suffix, null, null, 5, 2024, null));

        var buildingAsset = await Post<AssetResponse>("/api/v1/assets", new AssetRequest(
            AssetReferenceKeys.Types.Elevator, null, building.Code, "آسانسور تست", null, null, null,
            null, null, 30, null));
        Assert.Equal(building.Code, buildingAsset.Building!.Code);
        Assert.Null(buildingAsset.Complex);
        var complexAsset = await Post<AssetResponse>("/api/v1/assets", new AssetRequest(
            AssetReferenceKeys.Types.Generator, complex.Code, null, "ژنراتور تست", null, null, null,
            null, null, null, null));
        Assert.Equal(complex.Code, complexAsset.Complex!.Code);

        using var noScope = await client!.PostAsJsonAsync("/api/v1/assets", new AssetRequest(
            AssetReferenceKeys.Types.Other, null, null, "نامعتبر", null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, noScope.StatusCode);
        using var bothScopes = await client!.PostAsJsonAsync("/api/v1/assets", new AssetRequest(
            AssetReferenceKeys.Types.Other, complex.Code, building.Code, "نامعتبر", null, null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, bothScopes.StatusCode);

        var eventDate = DateTimeOffset.UtcNow.AddDays(-2);
        var next = eventDate.AddDays(45);
        var firstEvent = await Post<AssetEventResponse>($"/api/v1/assets/{buildingAsset.Code}/events", new AssetEventRequest(
            AssetReferenceKeys.EventTypes.Maintenance, eventDate, "سرویس کامل", null, next, null, null));
        var secondEvent = await Post<AssetEventResponse>($"/api/v1/assets/{buildingAsset.Code}/events", new AssetEventRequest(
            AssetReferenceKeys.EventTypes.Repair, eventDate, "تعمیر درب", "رویداد دوم در همان تاریخ", null, null, 0));
        Assert.NotEqual(firstEvent.Id, secondEvent.Id);
        var firstDetails = await client!.GetFromJsonAsync<AssetEventResponse>(
            $"/api/v1/assets/{buildingAsset.Code}/events/{firstEvent.Id}");
        var secondDetails = await client!.GetFromJsonAsync<AssetEventResponse>(
            $"/api/v1/assets/{buildingAsset.Code}/events/{secondEvent.Id}");
        Assert.Equal("سرویس کامل", firstDetails!.Title);
        Assert.Equal("تعمیر درب", secondDetails!.Title);
        Assert.Equal(0, secondDetails.Cost);
        using var crossAsset = await client!.GetAsync(
            $"/api/v1/assets/{complexAsset.Code}/events/{firstEvent.Id}");
        Assert.Equal(HttpStatusCode.NotFound, crossAsset.StatusCode);

        var corrected = await Put<AssetEventResponse>(
            $"/api/v1/assets/{buildingAsset.Code}/events/{secondEvent.Id}",
            new AssetEventRequest(AssetReferenceKeys.EventTypes.Repair, eventDate, "تعمیر اصلاح‌شده",
                "اصلاح سابقه", null, null, 1_250_000));
        Assert.Equal(secondEvent.Id, corrected.Id);
        Assert.Equal("تعمیر اصلاح‌شده", corrected.Title);
        Assert.Equal(1_250_000, corrected.Cost);

        async Task<AssetFileResponse> UploadEventFile(long eventId, string name)
        {
            using var form = new MultipartFormDataContent();
            using var bytes = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
            bytes.Headers.ContentType = new("application/pdf");
            form.Add(bytes, "file", name);
            form.Add(new StringContent($"پیوست {name}"), "title");
            using var response = await client.PostAsync(
                $"/api/v1/assets/{buildingAsset.Code}/events/{eventId}/files", form);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AssetFileResponse>())!;
        }
        var eventAFile1 = await UploadEventFile(firstEvent.Id, "گزارش-اول.pdf");
        var eventAFile2 = await UploadEventFile(firstEvent.Id, "گزارش-دوم.pdf");
        var eventBFile = await UploadEventFile(secondEvent.Id, "فاکتور.pdf");
        var eventAFiles = await client.GetFromJsonAsync<AssetFileResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/events/{firstEvent.Id}/files");
        var eventBFiles = await client.GetFromJsonAsync<AssetFileResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/events/{secondEvent.Id}/files");
        Assert.Equal(2, eventAFiles!.Length);
        Assert.Single(eventBFiles!);
        Assert.DoesNotContain(eventBFile.File.Code, eventAFiles.Select(x => x.File.Code));

        var detail = await client!.GetFromJsonAsync<AssetResponse>($"/api/v1/assets/{buildingAsset.Code}");
        Assert.Equal(eventDate.AddDays(30).ToUnixTimeSeconds(),
            detail!.SuggestedNextReviewDate!.Value.ToUnixTimeSeconds());

        var explicitAsset = await Post<AssetResponse>("/api/v1/assets", new AssetRequest(
            AssetReferenceKeys.Types.WaterPump, null, building.Code, "پمپ با پیشنهاد صریح", null, null,
            null, null, null, 90, null));
        var explicitDate = eventDate.AddDays(45);
        await Post<AssetEventResponse>($"/api/v1/assets/{explicitAsset.Code}/events", new AssetEventRequest(
            AssetReferenceKeys.EventTypes.Inspection, eventDate, "بازرسی", null, explicitDate, null, null));
        Assert.Equal(explicitDate.ToUnixTimeSeconds(), (await client.GetFromJsonAsync<AssetResponse>(
            $"/api/v1/assets/{explicitAsset.Code}"))!.SuggestedNextReviewDate!.Value.ToUnixTimeSeconds());
        var noReviewAsset = await Post<AssetResponse>("/api/v1/assets", new AssetRequest(
            AssetReferenceKeys.Types.Other, null, building.Code, "دارایی بدون پیشنهاد", null, null,
            null, null, null, null, null));
        await Post<AssetEventResponse>($"/api/v1/assets/{noReviewAsset.Code}/events", new AssetEventRequest(
            AssetReferenceKeys.EventTypes.Inspection, eventDate, "بازرسی", null, null, null, null));
        Assert.Null((await client.GetFromJsonAsync<AssetResponse>(
            $"/api/v1/assets/{noReviewAsset.Code}"))!.SuggestedNextReviewDate);

        var party = await Post<PartyResponse>("/api/v1/parties", new PartyRequest(
            PartyReferenceKeys.PartyTypes.IranianOrganization, $"شرکت سرویس {suffix}"));
        var providerEvent = await Post<AssetEventResponse>(
            $"/api/v1/assets/{buildingAsset.Code}/events", new AssetEventRequest(
                AssetReferenceKeys.EventTypes.Maintenance, eventDate.AddDays(1), "سرویس شرکتی",
                null, null, party.Code, null));
        Assert.Equal(party.Code, providerEvent.ServiceProvider!.Code);
        using var missingProvider = await client.PostAsJsonAsync(
            $"/api/v1/assets/{buildingAsset.Code}/events", new AssetEventRequest(
                AssetReferenceKeys.EventTypes.Maintenance, eventDate, "نامعتبر", null, null,
                "ZZZZZ", null));
        Assert.Equal(HttpStatusCode.NotFound, missingProvider.StatusCode);

        var changedScope = await Put<AssetResponse>($"/api/v1/assets/{buildingAsset.Code}", new AssetRequest(
            AssetReferenceKeys.Types.Elevator, complex.Code, null, "آسانسور ویرایش‌شده", "برند جدید",
            null, null, null, null, 30, "انتقال دامنه"));
        Assert.Equal(complex.Code, changedScope.Complex!.Code);
        Assert.Null(changedScope.Building);
        changedScope = await Put<AssetResponse>($"/api/v1/assets/{buildingAsset.Code}", new AssetRequest(
            AssetReferenceKeys.Types.Elevator, null, building.Code, "آسانسور ویرایش‌شده", "برند جدید",
            null, null, null, null, 30, "بازگشت دامنه"));
        Assert.Equal(building.Code, changedScope.Building!.Code);

        async Task<AssetGalleryResponse> Upload(string name, bool cover)
        {
            using var form = new MultipartFormDataContent(); using var bytes = new ByteArrayContent([0x89, 0x50, 0x4e, 0x47]);
            bytes.Headers.ContentType = new("image/png"); form.Add(bytes, "file", name); form.Add(new StringContent(cover.ToString()), "isCover");
            using var response = await client!.PostAsync($"/api/v1/assets/{buildingAsset.Code}/gallery", form);
            response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<AssetGalleryResponse>())!;
        }
        var first = await Upload("اول.png", true); var second = await Upload("دوم.png", true);
        var gallery = await client!.GetFromJsonAsync<AssetGalleryResponse[]>($"/api/v1/assets/{buildingAsset.Code}/gallery");
        Assert.False(gallery!.Single(x => x.File.Code == first.File.Code).IsCover);
        Assert.True(gallery!.Single(x => x.File.Code == second.File.Code).IsCover);

        using var documentForm = new MultipartFormDataContent();
        using var documentBytes = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        documentBytes.Headers.ContentType = new("application/pdf");
        documentForm.Add(documentBytes, "file", "قرارداد.pdf");
        documentForm.Add(new StringContent(DocumentTypeKeys.Contract), "documentTypeKey");
        documentForm.Add(new StringContent("قرارداد سرویس آسانسور"), "title");
        documentForm.Add(new StringContent("CN-1405-01"), "documentNumber");
        documentForm.Add(new StringContent("2026-08-01T00:00:00+00:00"), "documentDate");
        documentForm.Add(new StringContent("2026-08-01T00:00:00+00:00"), "effectiveFrom");
        documentForm.Add(new StringContent("2027-08-01T00:00:00+00:00"), "expiresAt");
        documentForm.Add(new StringContent("نسخه امضاشده"), "description");
        documentForm.Add(new StringContent("true"), "isConfidential");
        using var documentUpload = await client.PostAsync(
            $"/api/v1/assets/{buildingAsset.Code}/documents", documentForm);
        documentUpload.EnsureSuccessStatusCode();
        var document = (await documentUpload.Content.ReadFromJsonAsync<AssetDocumentResponse>())!;
        Assert.Equal(DocumentTypeKeys.Contract, document.DocumentType.Key);
        Assert.Equal("CN-1405-01", document.DocumentNumber);
        Assert.True(document.IsConfidential);
        Assert.Single((await client.GetFromJsonAsync<AssetDocumentResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/documents"))!);

        using var scope = factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var assetId = await db.Assets.Where(x => x.Code == buildingAsset.Code).Select(x => x.Id).SingleAsync();
        var buildingId = await db.Buildings.Where(x => x.Code == building.Code).Select(x => x.Id).SingleAsync();
        var complexId = await db.Complexes.Where(x => x.Code == complex.Code).Select(x => x.Id).SingleAsync();
        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [bms].[Assets] ([AssetTypeId],[ComplexId],[BuildingId],[Name],[Code],[IsActive],[CreatedAtUtc]) VALUES (1,NULL,NULL,N'نامعتبر','Z9X8Y',1,SYSUTCDATETIME())"));
        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [bms].[Assets] ([AssetTypeId],[ComplexId],[BuildingId],[Name],[Code],[IsActive],[CreatedAtUtc]) VALUES (1,{complexId},{buildingId},N'نامعتبر','Z9X8Z',1,SYSUTCDATETIME())"));
        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [bms].[Assets] ([AssetTypeId],[ComplexId],[BuildingId],[Name],[SuggestedReviewIntervalDays],[Code],[IsActive],[CreatedAtUtc]) VALUES (1,NULL,{buildingId},N'نامعتبر',0,'Z9X8W',1,SYSUTCDATETIME())"));
        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [bms].[AssetEvents] ([AssetId],[AssetEventTypeId],[EventDate],[Title],[SuggestedNextDate],[IsActive],[CreatedAtUtc]) VALUES ({assetId},1,'2026-08-11',N'نامعتبر','2026-08-10',1,SYSUTCDATETIME())"));

        await Assert.ThrowsAnyAsync<DbException>(async () =>
            await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [bms].[AssetGalleryFiles] SET [IsCover]=1 WHERE [StoredFileId]=(SELECT [Id] FROM [base].[StoredFiles] WHERE [Code]={first.File.Code})"));
        var eventStorageKeys = await db.StoredFiles.Where(x =>
            x.Code == eventAFile1.File.Code || x.Code == eventAFile2.File.Code || x.Code == eventBFile.File.Code)
            .Select(x => x.StorageKey).ToListAsync();
        Assert.Contains(eventStorageKeys, x => x.Contains($"/events/{firstEvent.Id}/", StringComparison.Ordinal));
        Assert.Contains(eventStorageKeys, x => x.Contains($"/events/{secondEvent.Id}/", StringComparison.Ordinal));

        var storedCount = await db.StoredFiles.CountAsync();
        var physicalCount = Directory.Exists(fileRoot!) ? Directory.GetFiles(fileRoot!, "*", SearchOption.AllDirectories).Length : 0;
        using var invalidDocumentForm = new MultipartFormDataContent();
        using var invalidDocumentBytes = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        invalidDocumentBytes.Headers.ContentType = new("application/pdf");
        invalidDocumentForm.Add(invalidDocumentBytes, "file", "نامعتبر.pdf");
        invalidDocumentForm.Add(new StringContent(DocumentTypeKeys.Contract), "documentTypeKey");
        invalidDocumentForm.Add(new StringContent("سند نامعتبر"), "title");
        invalidDocumentForm.Add(new StringContent("2027-01-01T00:00:00+00:00"), "effectiveFrom");
        invalidDocumentForm.Add(new StringContent("2026-01-01T00:00:00+00:00"), "expiresAt");
        using var invalidDocument = await client.PostAsync(
            $"/api/v1/assets/{buildingAsset.Code}/documents", invalidDocumentForm);
        Assert.Equal(HttpStatusCode.BadRequest, invalidDocument.StatusCode);
        Assert.Equal(storedCount, await db.StoredFiles.CountAsync());
        Assert.Equal(physicalCount, Directory.GetFiles(fileRoot!, "*", SearchOption.AllDirectories).Length);

        using var deactivate = await client.PatchAsJsonAsync($"/api/v1/assets/{buildingAsset.Code}/activation",
            new ActivationRequest(false));
        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        Assert.NotEmpty((await client.GetFromJsonAsync<AssetEventResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/events"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<AssetGalleryResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/gallery"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<AssetDocumentResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/documents"))!);
        Assert.NotEmpty((await client.GetFromJsonAsync<AssetFileResponse[]>(
            $"/api/v1/assets/{buildingAsset.Code}/events/{firstEvent.Id}/files"))!);
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
