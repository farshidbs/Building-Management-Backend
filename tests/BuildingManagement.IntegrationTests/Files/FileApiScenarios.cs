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

}
