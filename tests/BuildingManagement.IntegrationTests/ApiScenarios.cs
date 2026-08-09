using System.Net;
using System.Net.Http.Json;
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
    private async Task<T> Post<T>(string uri, object value)
    {
        var response = await client!.PostAsJsonAsync(uri, value);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
