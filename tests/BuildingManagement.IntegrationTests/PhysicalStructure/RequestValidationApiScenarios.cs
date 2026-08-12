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

}
