using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed class AssetEventService(IApplicationDbContext db, IFileStorage storage, FileStorageOptions options, TimeProvider clock, ILogger<AssetServiceBase> logger) : AssetServiceBase(db, storage, options, clock, logger)
{
    public async Task<AssetEventResponse> CreateEvent(string assetCode, AssetEventRequest request, CancellationToken ct)
    {
        Validate(request); var asset = await Entity(assetCode, ct);
        var typeId = await RefId(Db.AssetEventTypes, request.EventTypeKey, "asset_event_type", ct);
        var partyId = await PartyId(request.ServiceProviderPartyCode, ct);
        var entity = new AssetEvent(asset.Id, typeId, request.EventDate, request.Title, request.Description,
            request.SuggestedNextDate, partyId, request.Cost, Now);
        Db.AssetEvents.Add(entity); await Save(ct); return await EventProjection(Db.AssetEvents.Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

    public async Task<IReadOnlyList<AssetEventResponse>> Events(string assetCode, CancellationToken ct)
    {
        var id = (await Entity(assetCode, ct)).Id;
        return await EventProjection(Db.AssetEvents.AsNoTracking().Where(x => x.AssetId == id && x.IsActive)
            .OrderByDescending(x => x.EventDate).ThenByDescending(x => x.Id)).ToListAsync(ct);
    }

    public async Task<AssetEventResponse> GetEvent(string assetCode, long eventId, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct);
        return await EventProjection(Db.AssetEvents.AsNoTracking().Where(x =>
            x.Id == eventId && x.AssetId == asset.Id)).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("asset_event");
    }

    public async Task<AssetEventResponse> UpdateEvent(string assetCode, long eventId,
        AssetEventRequest request, CancellationToken ct)
    {
        Validate(request);
        var asset = await Entity(assetCode, ct);
        var entity = await EventEntity(asset.Id, eventId, ct);
        var typeId = await RefId(Db.AssetEventTypes, request.EventTypeKey, "asset_event_type", ct);
        var partyId = await PartyId(request.ServiceProviderPartyCode, ct);
        entity.Update(typeId, request.EventDate, request.Title, request.Description,
            request.SuggestedNextDate, partyId, request.Cost, Now);
        await Save(ct);
        return await EventProjection(Db.AssetEvents.Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

}
