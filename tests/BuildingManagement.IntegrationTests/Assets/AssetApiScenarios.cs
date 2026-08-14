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
    public async Task AssetScopeEventsDerivedReviewAndGalleryCoverWork()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var country = await CreateLocationFixture(
            new LocationRequest(null, $"کشور دارایی {suffix}", ReferenceKeys.LocationTypes.Country));
        var city = await CreateLocationFixture(
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
        using var defaultEventDate = await client.PostAsJsonAsync(
            $"/api/v1/assets/{buildingAsset.Code}/events", new AssetEventRequest(
                AssetReferenceKeys.EventTypes.Inspection, default, "تاریخ نامعتبر", null,
                null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, defaultEventDate.StatusCode);

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
}
