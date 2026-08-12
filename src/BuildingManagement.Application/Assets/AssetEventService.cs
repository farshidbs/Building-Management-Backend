using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

public sealed partial class AssetManagementService
{
    public async Task<AssetEventResponse> CreateEvent(string assetCode, AssetEventRequest request, CancellationToken ct)
    {
        Validate(request); var asset = await Entity(assetCode, ct);
        var typeId = await RefId(db.AssetEventTypes, request.EventTypeKey, "asset_event_type", ct);
        var partyId = await PartyId(request.ServiceProviderPartyCode, ct);
        var entity = new AssetEvent(asset.Id, typeId, request.EventDate, request.Title, request.Description,
            request.SuggestedNextDate, partyId, request.Cost, Now);
        db.AssetEvents.Add(entity); await Save(ct); return await EventProjection(db.AssetEvents.Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

    public async Task<IReadOnlyList<AssetEventResponse>> Events(string assetCode, CancellationToken ct)
    {
        var id = (await Entity(assetCode, ct)).Id;
        return await EventProjection(db.AssetEvents.AsNoTracking().Where(x => x.AssetId == id && x.IsActive)
            .OrderByDescending(x => x.EventDate).ThenByDescending(x => x.Id)).ToListAsync(ct);
    }

    public async Task<AssetEventResponse> GetEvent(string assetCode, long eventId, CancellationToken ct)
    {
        var asset = await Entity(assetCode, ct);
        return await EventProjection(db.AssetEvents.AsNoTracking().Where(x =>
            x.Id == eventId && x.AssetId == asset.Id)).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("asset_event");
    }

    public async Task<AssetEventResponse> UpdateEvent(string assetCode, long eventId,
        AssetEventRequest request, CancellationToken ct)
    {
        Validate(request);
        var asset = await Entity(assetCode, ct);
        var entity = await EventEntity(asset.Id, eventId, ct);
        var typeId = await RefId(db.AssetEventTypes, request.EventTypeKey, "asset_event_type", ct);
        var partyId = await PartyId(request.ServiceProviderPartyCode, ct);
        entity.Update(typeId, request.EventDate, request.Title, request.Description,
            request.SuggestedNextDate, partyId, request.Cost, Now);
        await Save(ct);
        return await EventProjection(db.AssetEvents.Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

}
