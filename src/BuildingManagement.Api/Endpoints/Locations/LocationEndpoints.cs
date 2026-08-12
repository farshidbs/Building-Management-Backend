using BuildingManagement.Application;

namespace BuildingManagement.Api;

internal static class LocationEndpoints
{
    internal static void MapLocationEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/locations").WithTags("Locations");
        group.MapPost("/", async (LocationRequest request, LocationService service, CancellationToken ct) =>
        {
            var response = await service.CreateLocation(request, ct);
            return Results.Created($"/api/v1/locations/{response.Code}", response);
        });
        group.MapGet("/{code}", (string code, LocationService service, CancellationToken ct) =>
            service.GetLocation(code, ct));
        group.MapGet("/", (string? parentCode, string? locationTypeKey, string? search, bool? isActive,
            int? pageNumber, int? pageSize, string? sortBy, string? sortDirection,
            LocationService service, CancellationToken ct) =>
            service.GetLocations(EndpointRegistration.Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection),
                parentCode, locationTypeKey, ct));
        group.MapPut("/{code}", (string code, LocationRequest request, LocationService service, CancellationToken ct) =>
            service.UpdateLocation(code, request, ct));
        group.MapPatch("/{code}/activation", async (string code, ActivationRequest request, LocationService service, CancellationToken ct) =>
        {
            await service.Activate(code, request.IsActive, ct);
            return Results.NoContent();
        });
        group.MapDelete("/{code}", async (string code, LocationService service, CancellationToken ct) =>
        {
            await service.Delete(code, ct);
            return Results.NoContent();
        });
    }
}
