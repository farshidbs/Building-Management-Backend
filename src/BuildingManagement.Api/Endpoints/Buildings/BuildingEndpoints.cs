using BuildingManagement.Application;

namespace BuildingManagement.Api;

internal static class BuildingEndpoints
{
    internal static void MapBuildingEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/buildings").WithTags("Buildings");
        group.MapPost("/", async (BuildingRequest request, BuildingService service, CancellationToken ct) =>
        {
            var response = await service.CreateBuilding(request, ct);
            return Results.Created($"/api/v1/buildings/{response.Code}", response);
        });
        group.MapGet("/{code}", (string code, BuildingService service, CancellationToken ct) =>
            service.GetBuilding(code, ct));
        group.MapGet("/", (string? complexCode, string? locationCode, string? buildingTypeKey,
            string? search, bool? isActive, int? pageNumber, int? pageSize, string? sortBy,
            string? sortDirection, BuildingService service, CancellationToken ct) =>
            service.GetBuildings(EndpointRegistration.Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection),
                complexCode, locationCode, buildingTypeKey, ct));
        group.MapPut("/{code}", (string code, BuildingRequest request, BuildingService service, CancellationToken ct) =>
            service.UpdateBuilding(code, request, ct));
        group.MapPatch("/{code}/activation", async (string code, ActivationRequest request, BuildingService service, CancellationToken ct) =>
        {
            await service.Activate(code, request.IsActive, ct);
            return Results.NoContent();
        });
        group.MapDelete("/{code}", async (string code, BuildingService service, CancellationToken ct) =>
        {
            await service.Delete(code, ct);
            return Results.NoContent();
        });
    }
}
