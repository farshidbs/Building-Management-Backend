using BuildingManagement.Application;

namespace BuildingManagement.Api;

public sealed record EndRelationRequest(string PartyCode, string RelationTypeKey,
    DateTimeOffset? EndDate = null);

internal static class UnitPartyOccupancyEndpoints
{
    internal static void MapUnitPartyOccupancyEndpoints(this RouteGroupBuilder api)
    {
        var units = api.MapGroup("/units/{unitCode}").WithTags("Unit Occupancy");
        units.MapGet("/parties", (string unitCode, bool? currentOnly, string? relationType,
            UnitPartyRelationService service, CancellationToken ct) =>
            service.GetUnitParties(unitCode, currentOnly ?? true, relationType, ct));
        units.MapPost("/party-relations", async (string unitCode,
            UnitOnboardingRelationRequest request, UnitPartyRelationService service, CancellationToken ct) =>
        {
            var response = await service.AddUnitRelation(unitCode, request, ct);
            return Results.Created($"/api/v1/units/{unitCode}/parties", response);
        });
        units.MapPost("/party-relations/end", async (string unitCode,
            EndRelationRequest request, UnitPartyRelationService service, CancellationToken ct) =>
        {
            await service.EndUnitRelation(unitCode, request.PartyCode, request.RelationTypeKey,
                request.EndDate, ct);
            return Results.NoContent();
        });
        units.MapGet("/occupancy-history", (string unitCode, UnitOccupancyService service,
            CancellationToken ct) => service.GetOccupancyHistory(unitCode, ct));
        units.MapPost("/occupancy-history", (string unitCode, OccupancyChangeRequest request,
            UnitOccupancyService service, CancellationToken ct) =>
            service.ChangeOccupancy(unitCode, request, ct));
    }
}
