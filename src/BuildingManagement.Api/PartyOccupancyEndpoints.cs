using BuildingManagement.Application;

namespace BuildingManagement.Api;

public sealed record EndRelationRequest(string PartyCode, string RelationTypeKey,
    DateTimeOffset? EndDate = null);

public static class PartyOccupancyEndpoints
{
    public static void MapPartyOccupancyEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        MapParties(api);
        MapUnitOccupancy(api);
    }

    private static void MapParties(RouteGroupBuilder api)
    {
        var parties = api.MapGroup("/parties").WithTags("Parties");
        parties.MapPost("/", async (PartyRequest request, PartyOccupancyService service,
            CancellationToken ct) =>
        {
            var response = await service.CreateParty(request, ct);
            return Results.Created($"/api/v1/parties/{response.Code}", response);
        });
        parties.MapGet("/{partyCode}", (string partyCode, PartyOccupancyService service,
            CancellationToken ct) => service.GetParty(partyCode, ct));
        parties.MapGet("/", (string? partyTypeKey, string? contact, string? search, bool? isActive,
            int? pageNumber, int? pageSize, string? sortBy, string? sortDirection,
            PartyOccupancyService service, CancellationToken ct) =>
            service.GetParties(new PageQuery(pageNumber ?? 1, pageSize ?? 20, search, isActive,
                sortBy ?? "displayName", sortDirection ?? "asc"), partyTypeKey, contact, ct));
        parties.MapPut("/{partyCode}", (string partyCode, PartyRequest request,
            PartyOccupancyService service, CancellationToken ct) =>
            service.UpdateParty(partyCode, request, ct));
        parties.MapPatch("/{partyCode}/activation", async (string partyCode, ActivationRequest request,
            PartyOccupancyService service, CancellationToken ct) =>
        {
            await service.ActivateParty(partyCode, request.IsActive, ct);
            return Results.NoContent();
        });

        parties.MapPost("/{partyCode}/contacts", async (string partyCode, PartyContactRequest request,
            PartyOccupancyService service, CancellationToken ct) =>
        {
            var response = await service.AddContact(partyCode, request, ct);
            return Results.Created($"/api/v1/parties/{partyCode}/contacts", response);
        });
        parties.MapGet("/{partyCode}/contacts", (string partyCode, PartyOccupancyService service,
            CancellationToken ct) => service.GetContacts(partyCode, ct));
        parties.MapPut("/{partyCode}/contacts", (string partyCode, PartyContactUpdateRequest request,
            PartyOccupancyService service, CancellationToken ct) =>
            service.UpdateContact(partyCode, request, ct));
        parties.MapPost("/{partyCode}/contacts/set-primary", (string partyCode,
            PartyContactSelectorRequest request, PartyOccupancyService service, CancellationToken ct) =>
            service.SetPrimaryContact(partyCode, request, ct));
    }

    private static void MapUnitOccupancy(RouteGroupBuilder api)
    {
        var units = api.MapGroup("/units/{unitCode}").WithTags("Unit Occupancy");
        units.MapGet("/parties", (string unitCode, bool? currentOnly, string? relationType,
            PartyOccupancyService service, CancellationToken ct) =>
            service.GetUnitParties(unitCode, currentOnly ?? true, relationType, ct));
        units.MapPost("/party-relations", async (string unitCode,
            UnitOnboardingRelationRequest request, PartyOccupancyService service, CancellationToken ct) =>
        {
            var response = await service.AddUnitRelation(unitCode, request, ct);
            return Results.Created($"/api/v1/units/{unitCode}/parties", response);
        });
        units.MapPost("/party-relations/end", async (string unitCode,
            EndRelationRequest request, PartyOccupancyService service,
            CancellationToken ct) =>
        {
            await service.EndUnitRelation(unitCode, request.PartyCode, request.RelationTypeKey,
                request.EndDate, ct);
            return Results.NoContent();
        });
        units.MapGet("/occupancy-history", (string unitCode, PartyOccupancyService service,
            CancellationToken ct) => service.GetOccupancyHistory(unitCode, ct));
        units.MapPost("/occupancy-history", (string unitCode, OccupancyChangeRequest request,
            PartyOccupancyService service, CancellationToken ct) =>
            service.ChangeOccupancy(unitCode, request, ct));
    }
}
