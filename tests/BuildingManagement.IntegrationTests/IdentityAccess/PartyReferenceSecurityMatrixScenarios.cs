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
    private sealed record PartySecurityFixture(HttpClient Client, long UserId, long BuildingId,
        string BuildingCode, long UnitId, string UnitCode, long AssetId, string AssetCode,
        long ForeignPartyId, string ForeignPartyCode, long SelfPartyId, string SelfPartyCode,
        long ExpenseTypeId);

    private async Task<PartySecurityFixture> CreatePartySecurityFixture()
    {
        long buildingId;
        string buildingCode;
        long unitId;
        string unitCode;
        long foreignUnitId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var unit = await db.Units.AsNoTracking().OrderBy(x => x.Id).FirstAsync();
            var building = await db.Buildings.AsNoTracking().SingleAsync(x => x.Id == unit.BuildingId);
            buildingId = building.Id;
            buildingCode = building.Code;
            unitId = unit.Id;
            unitCode = unit.Code;
            foreignUnitId = await db.Units.Where(x => x.BuildingId != buildingId)
                .Select(x => x.Id).FirstAsync();
        }

        var actor = await CreateAuthenticatedClientWithIdentity("building_manager", buildingId: buildingId);
        long assetId;
        string assetCode;
        long foreignPartyId;
        string foreignPartyCode;
        long selfPartyId;
        string selfPartyCode;
        long expenseTypeId;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var now = DateTimeOffset.UtcNow;
            var partyTypeId = await db.PartyTypes.Where(x => x.Key == PartyReferenceKeys.PartyTypes.IranianPerson)
                .Select(x => x.Id).SingleAsync();
            var ownerTypeId = await db.UnitPartyRelationTypes.Where(x => x.Key == PartyReferenceKeys.RelationTypes.Owner)
                .Select(x => x.Id).SingleAsync();
            var foreignParty = new Party(PublicCode.Create(), partyTypeId, "شخص محدوده دیگر", null, null,
                null, null, null, now);
            var selfParty = new Party(PublicCode.Create(), partyTypeId, "شخص هویتی مدیر", null, null,
                null, null, null, now);
            db.Parties.AddRange(foreignParty, selfParty);
            await db.SaveChangesAsync();
            db.UnitPartyRelations.Add(new UnitPartyRelation(foreignUnitId, foreignParty.Id,
                ownerTypeId, null, null, null, now));
            db.UserPartyLinks.Add(new UserPartyLink(PublicCode.Create(), actor.UserId, selfParty.Id, true, now));

            var assetTypeId = await db.AssetTypes.Where(x => x.IsActive).Select(x => x.Id).FirstAsync();
            var asset = new Asset(PublicCode.Create(), assetTypeId, null, buildingId,
                $"دارایی امنیتی {Guid.NewGuid():N}", null, null, null, null, null, null, null, now);
            db.Assets.Add(asset);
            if (!await db.FinancialAccounts.AnyAsync(x => x.UnitId == unitId &&
                x.AccountKindKey == FinancialKeys.AccountKinds.Unit))
                db.FinancialAccounts.Add(new FinancialAccount(unitId, null, null,
                    FinancialKeys.AccountKinds.Unit, now));
            if (!await db.FinancialAccounts.AnyAsync(x => x.BuildingId == buildingId &&
                x.AccountKindKey == FinancialKeys.AccountKinds.CurrentFund))
                db.FinancialAccounts.Add(new FinancialAccount(null, buildingId, null,
                    FinancialKeys.AccountKinds.CurrentFund, now));
            var parentComplexId = await db.Buildings.Where(x => x.Id == buildingId)
                .Select(x => x.ComplexId).SingleAsync();
            expenseTypeId = await db.ExpenseTypes.Where(x => x.IsActive &&
                (x.BuildingId == null && x.ComplexId == null || x.BuildingId == buildingId ||
                 parentComplexId.HasValue && x.ComplexId == parentComplexId)).Select(x => x.Id).FirstAsync();
            await db.SaveChangesAsync();
            assetId = asset.Id;
            assetCode = asset.Code;
            foreignPartyId = foreignParty.Id;
            foreignPartyCode = foreignParty.Code;
            selfPartyId = selfParty.Id;
            selfPartyCode = selfParty.Code;
        }
        return new(actor.Client, actor.UserId, buildingId, buildingCode, unitId, unitCode,
            assetId, assetCode, foreignPartyId, foreignPartyCode, selfPartyId, selfPartyCode,
            expenseTypeId);
    }

    [Fact]
    public async Task AssetEventRejectsCrossScopeServiceProviderParty()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        var marker = $"asset-attack-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync($"/api/v1/assets/{fixture.AssetCode}/events",
            new AssetEventRequest(AssetReferenceKeys.EventTypes.Repair, DateTimeOffset.UtcNow, marker,
                null, null, fixture.ForeignPartyCode, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False(await db.AssetEvents.AnyAsync(x => x.AssetId == fixture.AssetId &&
            x.ServiceProviderPartyId == fixture.ForeignPartyId && x.Title == marker));
    }

    [Fact]
    public async Task OwnIdentityPartyCanBeReferencedInAuthorizedAssetWorkflow()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        var marker = $"asset-self-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync($"/api/v1/assets/{fixture.AssetCode}/events",
            new AssetEventRequest(AssetReferenceKeys.EventTypes.Repair, DateTimeOffset.UtcNow, marker,
                null, null, fixture.SelfPartyCode, null));
        response.EnsureSuccessStatusCode();
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.True(await db.AssetEvents.AnyAsync(x => x.AssetId == fixture.AssetId &&
            x.ServiceProviderPartyId == fixture.SelfPartyId && x.Title == marker));
    }

    [Fact]
    public async Task ExpenseRejectsCrossScopeVendorParty()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        var marker = $"expense-attack-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseRequest(
            fixture.BuildingCode, null, fixture.ExpenseTypeId, fixture.ForeignPartyCode, marker,
            100m, DateTimeOffset.UtcNow, null, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False(await db.Expenses.AnyAsync(x => x.Title == marker &&
            x.VendorPartyId == fixture.ForeignPartyId));
    }

    [Fact]
    public async Task ExpenseDisbursementRejectsCrossScopePayeeParty()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        var expenseResponse = await client.PostAsJsonAsync("/api/v1/financial/expenses", new ExpenseRequest(
            fixture.BuildingCode, null, fixture.ExpenseTypeId, null, $"payee-{Guid.NewGuid():N}",
            100m, DateTimeOffset.UtcNow, null, null));
        expenseResponse.EnsureSuccessStatusCode();
        var expense = (await expenseResponse.Content.ReadFromJsonAsync<ExpenseResponse>())!;
        (await client.PostAsync($"/api/v1/financial/expenses/{expense.Code}/finalize", null))
            .EnsureSuccessStatusCode();
        var response = await client.PostAsJsonAsync(
            $"/api/v1/financial/expenses/{expense.Code}/disbursements",
            new ExpenseDisbursementRequest(fixture.BuildingCode, null,
                FinancialKeys.AccountKinds.CurrentFund, fixture.ForeignPartyCode, 10m, "cash", null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        var expenseId = await db.Expenses.Where(x => x.Code == expense.Code).Select(x => x.Id).SingleAsync();
        Assert.False(await db.ExpenseDisbursements.AnyAsync(x => x.ExpenseId == expenseId &&
            x.PayeePartyId == fixture.ForeignPartyId));
    }

    [Fact]
    public async Task PaymentRejectsCrossScopePayerParty()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        var marker = $"PAY-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/financial/payments", new PaymentRequest(
            fixture.UnitCode, fixture.BuildingCode, null, FinancialKeys.AccountKinds.CurrentFund,
            fixture.ForeignPartyCode, "cash", 100m, marker, null, []));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False(await db.Payments.AnyAsync(x => x.PayerPartyId == fixture.ForeignPartyId &&
            x.BankTrackingCode == marker));
    }

    [Fact]
    public async Task AccountAdjustmentRejectsCrossScopeResponsibleParty()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        var marker = $"adjustment-attack-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync("/api/v1/financial/accounts/adjustments",
            new AccountAdjustmentRequest(fixture.UnitCode, null, null, FinancialKeys.AccountKinds.Unit,
                null, null, null, FinancialKeys.Adjustments.OpeningCredit, 100m, DateTimeOffset.UtcNow,
                marker, FinancialKeys.ResponsibleParties.Owner, fixture.ForeignPartyCode));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var scope = factory!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False(await db.AccountAdjustments.AnyAsync(x => x.Reason == marker &&
            x.ResponsiblePartyId == fixture.ForeignPartyId));
    }

    [Fact]
    public async Task UnattachedExternalPartyCannotBeReferencedByCode()
    {
        if (!enabled) Assert.Skip(skipReason ?? "SQL Server integration infrastructure is unavailable.");
        var fixture = await CreatePartySecurityFixture();
        using var client = fixture.Client;
        long unattachedId;
        string unattachedCode;
        await using (var scope = factory!.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
            var partyTypeId = await db.PartyTypes.Where(x => x.Key == PartyReferenceKeys.PartyTypes.IranianOrganization)
                .Select(x => x.Id).SingleAsync();
            var party = new Party(PublicCode.Create(), partyTypeId, "پیمانکار بدون محدوده", null, null,
                "پیمانکار بدون محدوده", null, null, DateTimeOffset.UtcNow);
            db.Parties.Add(party);
            await db.SaveChangesAsync();
            unattachedId = party.Id;
            unattachedCode = party.Code;
        }
        var marker = $"unattached-{Guid.NewGuid():N}";
        var response = await client.PostAsJsonAsync($"/api/v1/assets/{fixture.AssetCode}/events",
            new AssetEventRequest(AssetReferenceKeys.EventTypes.Repair, DateTimeOffset.UtcNow, marker,
                null, null, unattachedCode, null));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var verification = factory!.Services.CreateAsyncScope();
        var verificationDb = verification.ServiceProvider.GetRequiredService<BuildingManagementDbContext>();
        Assert.False(await verificationDb.AssetEvents.AnyAsync(x => x.AssetId == fixture.AssetId &&
            x.ServiceProviderPartyId == unattachedId && x.Title == marker));
    }
}
