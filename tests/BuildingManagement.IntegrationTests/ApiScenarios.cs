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
            await database.StartAsync();
            factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:BuildingManagement"] = database.GetConnectionString(),
                        ["SeedDevelopmentData"] = "true"
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
                "۱۰۱", 1, 80, 2, 1, 0, null));
        Assert.Equal(ReferenceKeys.UnitStatuses.Available, unit.Status.Key);
        Assert.Equal(building.Code, unit.Building.Code);
        Assert.Equal(building.Name, unit.Building.Name);
        Assert.Equal(complex.Code, unit.Complex!.Code);

        var duplicateUnit = await client!.PostAsJsonAsync($"/api/v1/buildings/{building.Code}/units",
            new UnitRequest(ReferenceKeys.UnitUsageTypes.Residential, ReferenceKeys.UnitStatuses.Available,
                " ۱۰۱ ", 1, 80, 2, 1, 0, null));
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
    private async Task<T> Post<T>(string uri, object value)
    {
        var response = await client!.PostAsJsonAsync(uri, value);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
