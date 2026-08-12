using BuildingManagement.Application;

namespace BuildingManagement.Api;

internal static class PartyEndpoints
{
    internal static void MapPartyEndpoints(this RouteGroupBuilder api)
    {
        var parties = api.MapGroup("/parties").WithTags("Parties");
        parties.MapPost("/", async (PartyRequest request, PartyService service,
            CancellationToken ct) =>
        {
            var response = await service.CreateParty(request, ct);
            return Results.Created($"/api/v1/parties/{response.Code}", response);
        });
        parties.MapGet("/{partyCode}", (string partyCode, PartyService service,
            CancellationToken ct) => service.GetParty(partyCode, ct));
        parties.MapGet("/", (string? partyTypeKey, string? contact, string? search, bool? isActive,
            int? pageNumber, int? pageSize, string? sortBy, string? sortDirection,
            PartyService service, CancellationToken ct) =>
            service.GetParties(new PageQuery(pageNumber ?? 1, pageSize ?? 20, search, isActive,
                sortBy ?? "displayName", sortDirection ?? "asc"), partyTypeKey, contact, ct));
        parties.MapPut("/{partyCode}", (string partyCode, PartyRequest request,
            PartyService service, CancellationToken ct) =>
            service.UpdateParty(partyCode, request, ct));
        parties.MapPatch("/{partyCode}/activation", async (string partyCode, ActivationRequest request,
            PartyService service, CancellationToken ct) =>
        {
            await service.ActivateParty(partyCode, request.IsActive, ct);
            return Results.NoContent();
        });

        parties.MapPost("/{partyCode}/contacts", async (string partyCode, PartyContactRequest request,
            PartyContactService service, CancellationToken ct) =>
        {
            var response = await service.AddContact(partyCode, request, ct);
            return Results.Created($"/api/v1/parties/{partyCode}/contacts", response);
        });
        parties.MapGet("/{partyCode}/contacts", (string partyCode, PartyContactService service,
            CancellationToken ct) => service.GetContacts(partyCode, ct));
        parties.MapPut("/{partyCode}/contacts", (string partyCode, PartyContactUpdateRequest request,
            PartyContactService service, CancellationToken ct) =>
            service.UpdateContact(partyCode, request, ct));
        parties.MapPost("/{partyCode}/contacts/set-primary", (string partyCode,
            PartyContactSelectorRequest request, PartyContactService service, CancellationToken ct) =>
            service.SetPrimaryContact(partyCode, request, ct));
    }
}
