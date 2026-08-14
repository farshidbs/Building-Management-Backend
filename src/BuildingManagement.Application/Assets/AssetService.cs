using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class AssetService(IApplicationDbContext db, IFileStorage storage, FileStorageOptions options, TimeProvider clock, ILogger<AssetServiceBase> logger, ResourceAuthorization authorization) : AssetServiceBase(db, storage, options, clock, logger, authorization)
{
    public async Task<AssetResponse> Create(AssetRequest request, CancellationToken ct)
    {
        Validate(request);
        var typeId = await RefId(Db.AssetTypes, request.AssetTypeKey, "asset_type", ct);
        var complexId = await ComplexId(request.ComplexCode, true, ct);
        var buildingId = await BuildingId(request.BuildingCode, true, ct);
        await Authorization.Ensure("asset_manage", complexId, buildingId, null, ct);
        var asset = new Asset(await UniqueCode(Db.Assets, ct), typeId, complexId, buildingId, request.Name,
            request.Brand, request.Model, request.SerialNumber, request.InstallationDate, request.PurchaseDate,
            request.SuggestedReviewIntervalDays, request.Description, Now);
        Db.Assets.Add(asset); await Save(ct); return await Get(asset.Code, ct);
    }

    public async Task<AssetResponse> Get(string code, CancellationToken ct)
    {
        var asset = await Entity(code, ct);
        return await Projection(Db.Assets.AsNoTracking().Where(x => x.Id == asset.Id)).SingleAsync(ct);
    }

    public async Task<Page<AssetResponse>> List(PageQuery query, string? complexCode, string? buildingCode,
        string? assetTypeKey, CancellationToken ct)
    {
        var (number, size) = query.Validated();
        var complexId = await ComplexId(complexCode, false, ct); var buildingId = await BuildingId(buildingCode, false, ct);
        if (complexId.HasValue || buildingId.HasValue) await Authorization.Ensure("asset_view", complexId, buildingId, null, ct);
        var accessible = await Authorization.Accessible("asset_view", ct);
        long? typeId = string.IsNullOrWhiteSpace(assetTypeKey) ? null : await RefId(Db.AssetTypes, assetTypeKey, "asset_type", ct);
        var source = Db.Assets.AsNoTracking().Where(x =>
            (x.ComplexId.HasValue && accessible.ComplexIds.Contains(x.ComplexId.Value) ||
             x.BuildingId.HasValue && accessible.BuildingIds.Contains(x.BuildingId.Value)) &&
            (!query.IsActive.HasValue || x.IsActive == query.IsActive) &&
            (!complexId.HasValue || x.ComplexId == complexId) && (!buildingId.HasValue || x.BuildingId == buildingId) &&
            (!typeId.HasValue || x.AssetTypeId == typeId) && (string.IsNullOrWhiteSpace(query.Search) || x.Name.Contains(query.Search)));
        var total = await source.CountAsync(ct);
        source = query.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase)
            ? source.OrderByDescending(x => x.Name).ThenByDescending(x => x.Id) : source.OrderBy(x => x.Name).ThenBy(x => x.Id);
        return new(await Projection(source.Skip((number - 1) * size).Take(size)).ToListAsync(ct), number, size, total);
    }

    public async Task<AssetResponse> Update(string code, AssetRequest request, CancellationToken ct)
    {
        Validate(request); var asset = await Entity(code, ct, "asset_manage");
        var typeId = await RefId(Db.AssetTypes, request.AssetTypeKey, "asset_type", ct);
        var destinationComplexId = await ComplexId(request.ComplexCode, true, ct);
        var destinationBuildingId = await BuildingId(request.BuildingCode, true, ct);
        if (destinationComplexId != asset.ComplexId || destinationBuildingId != asset.BuildingId)
            await Authorization.Ensure("asset_manage", destinationComplexId, destinationBuildingId, null, ct);
        asset.Update(typeId, destinationComplexId, destinationBuildingId,
            request.Name, request.Brand, request.Model, request.SerialNumber, request.InstallationDate, request.PurchaseDate,
            request.SuggestedReviewIntervalDays, request.Description, Now);
        await Save(ct); return await Get(asset.Code, ct);
    }

    public async Task Activate(string code, bool active, CancellationToken ct)
    { var asset = await Entity(code, ct, "asset_manage"); asset.SetActivation(active, Now); await Save(ct); }

}
