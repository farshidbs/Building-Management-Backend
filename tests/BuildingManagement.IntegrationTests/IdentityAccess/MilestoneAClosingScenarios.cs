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
    public async Task BuildingOverrideDenyIsConsistentForDirectAndListAssetAccess()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        Building building;
        Asset asset;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            building = await db.Buildings.AsNoTracking().FirstAsync();
            var typeId = await db.AssetTypes.Select(x => x.Id).FirstAsync();
            asset = new Asset(PublicCode.Create(), typeId, null, building.Id, "دارایی منع‌شده",
                null, null, null, null, null, null, null, DateTimeOffset.UtcNow);
            db.Assets.Add(asset);
            await db.SaveChangesAsync();
        }
        using var deniedClient = await CreateAuthenticatedClient("building_manager", buildingId: building.Id);
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var userId = await db.AuthSessions.Where(x => x.DeviceIdentifier == "integration-security")
                .OrderByDescending(x => x.Id).Select(x => x.UserId!.Value).FirstAsync();
            var membership = await db.AccessMemberships.Where(x => x.UserId == userId).SingleAsync();
            var permissionId = await db.AccessPermissions.Where(x => x.Key == "asset_view")
                .Select(x => x.Id).SingleAsync();
            db.BuildingRolePermissionOverrides.Add(new BuildingRolePermissionOverride(PublicCode.Create(),
                building.Id, membership.RoleId, permissionId, IamKeys.Effects.Deny, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.Forbidden,
            (await deniedClient.GetAsync($"/api/v1/assets/{asset.Code}")).StatusCode);
        var page = await deniedClient.GetFromJsonAsync<Page<AssetResponse>>("/api/v1/assets");
        Assert.DoesNotContain(page!.Items, x => x.Code == asset.Code);
    }

    [Fact]
    public async Task UnattachedPartyAndGlobalLocationMutationsAreDenied()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        string partyCode;
        string locationCode;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var typeId = await db.PartyTypes.Where(x => x.Key == "person").Select(x => x.Id).SingleAsync();
            var party = new Party(PublicCode.Create(), typeId, "شخص بدون وابستگی", null, null, null,
                null, null, DateTimeOffset.UtcNow);
            db.Parties.Add(party);
            await db.SaveChangesAsync();
            partyCode = party.Code;
            locationCode = await db.Locations.Where(x => x.IsActive).Select(x => x.Code).FirstAsync();
        }

        Assert.Equal(HttpStatusCode.Forbidden,
            (await client!.GetAsync($"/api/v1/parties/{partyCode}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.PatchAsJsonAsync($"/api/v1/locations/{locationCode}/activation",
                new ActivationRequest(false))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await client.DeleteAsync($"/api/v1/locations/{locationCode}")).StatusCode);
    }

    [Fact]
    public async Task BuildingManagerCannotReparentBuildingOutsideAuthorizedComplex()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        Building building;
        Complex destination;
        string locationCode;
        string buildingTypeKey;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            building = await db.Buildings.AsNoTracking().FirstAsync(x => x.ComplexId != null);
            destination = await db.Complexes.AsNoTracking().FirstAsync(x => x.Id != building.ComplexId);
            locationCode = await db.Locations.Where(x => x.Id == building.LocationId).Select(x => x.Code).SingleAsync();
            buildingTypeKey = await db.BuildingTypes.Where(x => x.Id == building.BuildingTypeId).Select(x => x.Key).SingleAsync();
        }

        using var scopedClient = await CreateAuthenticatedClient("building_manager", buildingId: building.Id);
        var request = new BuildingRequest(destination.Code, locationCode, buildingTypeKey, building.Name,
            building.Address, building.PostalCode, building.Latitude, building.Longitude,
            building.FloorsCount, building.ConstructionYear, building.Description);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await scopedClient.PutAsJsonAsync($"/api/v1/buildings/{building.Code}", request)).StatusCode);

        await using var verification = factory!.Services.CreateAsyncScope();
        var dbVerification = verification.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(building.ComplexId,
            await dbVerification.Buildings.Where(x => x.Id == building.Id).Select(x => x.ComplexId).SingleAsync());
    }

    [Fact]
    public async Task AssetAndUnitIdorAndAssetDestinationMoveAreDenied()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        Building ownBuilding;
        Building otherBuilding;
        Unit ownUnit;
        Unit otherUnit;
        Asset ownAsset;
        Asset otherAsset;
        string assetTypeKey;
        string otherUsageTypeKey;
        string otherStatusKey;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            ownBuilding = await db.Buildings.AsNoTracking().FirstAsync(x => db.Units.Any(u => u.BuildingId == x.Id));
            otherBuilding = await db.Buildings.AsNoTracking().FirstAsync(x => x.Id != ownBuilding.Id && db.Units.Any(u => u.BuildingId == x.Id));
            ownUnit = await db.Units.AsNoTracking().FirstAsync(x => x.BuildingId == ownBuilding.Id);
            otherUnit = await db.Units.AsNoTracking().FirstAsync(x => x.BuildingId == otherBuilding.Id);
            otherUsageTypeKey = await db.UnitUsageTypes.Where(x => x.Id == otherUnit.UsageTypeId)
                .Select(x => x.Key).SingleAsync();
            otherStatusKey = await db.UnitStatuses.Where(x => x.Id == otherUnit.StatusId)
                .Select(x => x.Key).SingleAsync();
            var typeId = await db.AssetTypes.Select(x => x.Id).FirstAsync();
            assetTypeKey = await db.AssetTypes.Where(x => x.Id == typeId).Select(x => x.Key).SingleAsync();
            ownAsset = new Asset(PublicCode.Create(), typeId, null, ownBuilding.Id, "دارایی مجاز",
                null, null, null, null, null, null, null, DateTimeOffset.UtcNow);
            otherAsset = new Asset(PublicCode.Create(), typeId, null, otherBuilding.Id, "دارایی غیرمجاز",
                null, null, null, null, null, null, null, DateTimeOffset.UtcNow);
            db.Assets.AddRange(ownAsset, otherAsset);
            await db.SaveChangesAsync();
        }

        using var scopedClient = await CreateAuthenticatedClient("building_manager", buildingId: ownBuilding.Id);
        Assert.Equal(HttpStatusCode.OK, (await scopedClient.GetAsync($"/api/v1/assets/{ownAsset.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await scopedClient.GetAsync($"/api/v1/assets/{otherAsset.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await scopedClient.GetAsync($"/api/v1/units/{ownUnit.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await scopedClient.GetAsync($"/api/v1/units/{otherUnit.Code}")).StatusCode);

        var ownAssetRequest = new AssetRequest(assetTypeKey, null, ownBuilding.Code, ownAsset.Name,
            null, null, null, null, null, null, null);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await scopedClient.PutAsJsonAsync($"/api/v1/assets/{otherAsset.Code}", ownAssetRequest)).StatusCode);
        var moveRequest = ownAssetRequest with { BuildingCode = otherBuilding.Code };
        Assert.Equal(HttpStatusCode.Forbidden,
            (await scopedClient.PutAsJsonAsync($"/api/v1/assets/{ownAsset.Code}", moveRequest)).StatusCode);

        using var unitClient = await CreateAuthenticatedClient("unit_resident", unitId: ownUnit.Id);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unitClient.GetAsync($"/api/v1/units/{otherUnit.Code}")).StatusCode);
        var unrelatedUpdate = new UnitUpdateRequest(otherUsageTypeKey, otherStatusKey,
            otherUnit.UnitNumber, otherUnit.FloorNumber, otherUnit.Area, otherUnit.RoomsCount,
            otherUnit.ParkingCount, otherUnit.StorageCount, otherUnit.Description);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await unitClient.PutAsJsonAsync($"/api/v1/units/{otherUnit.Code}", unrelatedUpdate)).StatusCode);
    }

    [Fact]
    public async Task FinancePreviewExpenseReadAndDisbursementFinalizeRejectCrossBuildingAccess()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        Building authorizedBuilding;
        Building protectedBuilding;
        long expenseTypeId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            authorizedBuilding = await db.Buildings.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
            protectedBuilding = await db.Buildings.AsNoTracking().FirstAsync(x => x.Id != authorizedBuilding.Id);
            expenseTypeId = await db.ExpenseTypes.Where(x => x.Key == "maintenance")
                .Select(x => x.Id).FirstAsync();
            if (!await db.FinancialAccounts.AnyAsync(x => x.BuildingId == protectedBuilding.Id &&
                x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund))
                db.FinancialAccounts.Add(new FinancialAccount(null, protectedBuilding.Id, null,
                    FinancialKeys.AccountKinds.CurrentFund, DateTimeOffset.UtcNow));
            var unitIds = await db.Units.Where(x => x.BuildingId == protectedBuilding.Id).Select(x => x.Id).ToListAsync();
            foreach (var unitId in unitIds.Where(unitId => !db.FinancialAccounts.Any(x => x.UnitId == unitId)))
                db.FinancialAccounts.Add(new FinancialAccount(unitId, null, null,
                    FinancialKeys.AccountKinds.Unit, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var expense = await Post<ExpenseResponse>("/api/v1/financial/expenses",
            new ExpenseRequest(protectedBuilding.Code, null, expenseTypeId, null, "هزینه محافظت‌شده",
                1_000_000m, DateTimeOffset.UtcNow, null, null));
        await Post<ExpenseResponse>($"/api/v1/financial/expenses/{expense.Code}/finalize", new { });
        var disbursement = await Post<ExpenseDisbursementResponse>(
            $"/api/v1/financial/expenses/{expense.Code}/disbursements",
            new ExpenseDisbursementRequest(protectedBuilding.Code, null,
                FinancialKeys.AccountKinds.CurrentFund, null, 100_000m, "cash", null));
        var demand = await Post<DemandResponse>("/api/v1/financial/demands",
            new DemandRequest(protectedBuilding.Code, null, FinancialKeys.AccountKinds.CurrentFund,
                "monthly_charge", "شارژ محافظت‌شده", null, DateTimeOffset.UtcNow, null,
                new DemandRuleRequest(FinancialKeys.AllocationMethods.Equal,
                    FinancialKeys.AmountModes.PerUnit, null, 10_000m, true,
                    FinancialKeys.ResponsibleParties.Owner, FinancialKeys.Redistribution.None, null)));

        Assert.Equal(HttpStatusCode.OK,
            (await client!.PostAsJsonAsync($"/api/v1/financial/demands/{demand.Code}/preview",
                new DemandPreviewRequest())).StatusCode);
        using var attacker = await CreateAuthenticatedClient("building_manager", buildingId: authorizedBuilding.Id);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await attacker.GetAsync($"/api/v1/financial/expenses/{expense.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await attacker.PostAsJsonAsync($"/api/v1/financial/demands/{demand.Code}/preview",
                new DemandPreviewRequest())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await attacker.PostAsync(
                $"/api/v1/financial/expenses/{expense.Code}/disbursements/{disbursement.Code}/finalize", null)).StatusCode);

        await using var verification = factory!.Services.CreateAsyncScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.Equal(FinancialKeys.Statuses.Draft, await verificationDb.ExpenseDisbursements
            .Where(x => x.Code == disbursement.Code).Select(x => x.Status).SingleAsync());
    }

    [Fact]
    public async Task ConfidentialBuildingDocumentMetadataAndFileMutationsRespectDistinctPermissions()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        Building building;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            building = await db.Buildings.AsNoTracking().FirstAsync();
        }
        var ordinary = await UploadBuildingDocument(building.Code, "سند عادی", false);
        var confidential = await UploadBuildingDocument(building.Code, "سند محرمانه", true);
        using var reader = await CreateAuthenticatedClient("manager_assistant", buildingId: building.Id);

        var visible = await reader.GetFromJsonAsync<List<DocumentFileResponse>>(
            $"/api/v1/buildings/{building.Code}/documents");
        Assert.Contains(visible!, x => x.Code == ordinary.Code);
        Assert.DoesNotContain(visible!, x => x.Code == confidential.Code);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.GetAsync(
            $"/api/v1/buildings/{building.Code}/documents/{confidential.Code}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await reader.GetAsync($"/api/v1/files/{confidential.File.Code}/content")).StatusCode);

        using var mutation = DocumentForm("تلاش غیرمجاز", false);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostAsync(
            $"/api/v1/buildings/{building.Code}/documents", mutation)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await client!.GetAsync($"/api/v1/files/{confidential.File.Code}/content")).StatusCode);
    }

    private async Task<DocumentFileResponse> UploadBuildingDocument(string buildingCode, string title,
        bool confidential)
    {
        using var form = DocumentForm(title, confidential);
        using var response = await client!.PostAsync($"/api/v1/buildings/{buildingCode}/documents", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<DocumentFileResponse>())!;
    }

    private static MultipartFormDataContent DocumentForm(string title, bool confidential)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([0x25, 0x50, 0x44, 0x46]);
        file.Headers.ContentType = new("application/pdf");
        form.Add(file, "file", "document.pdf");
        form.Add(new StringContent("contract"), "documentTypeKey");
        form.Add(new StringContent(title), "title");
        form.Add(new StringContent(confidential.ToString()), "isConfidential");
        return form;
    }
}
