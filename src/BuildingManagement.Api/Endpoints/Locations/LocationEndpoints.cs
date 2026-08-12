using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static partial class Endpoints
{
    private static void MapLocations(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/locations").WithTags("Locations");
        group.MapPost("/", async (LocationRequest request, PhysicalStructureService service, CancellationToken ct) =>
        {
            var response = await service.CreateLocation(request, ct);
            return Results.Created($"/api/v1/locations/{response.Code}", response);
        });
        group.MapGet("/{code}", (string code, PhysicalStructureService service, CancellationToken ct) =>
            service.GetLocation(code, ct));
        group.MapGet("/", (string? parentCode, string? locationTypeKey, string? search, bool? isActive,
            int? pageNumber, int? pageSize, string? sortBy, string? sortDirection,
            PhysicalStructureService service, CancellationToken ct) =>
            service.GetLocations(Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection),
                parentCode, locationTypeKey, ct));
        group.MapPut("/{code}", (string code, LocationRequest request, PhysicalStructureService service, CancellationToken ct) =>
            service.UpdateLocation(code, request, ct));
        group.MapPatch("/{code}/activation", async (string code, ActivationRequest request, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Activate("location", code, request.IsActive, ct);
            return Results.NoContent();
        });
        group.MapDelete("/{code}", async (string code, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Delete("location", code, ct);
            return Results.NoContent();
        });
    }
}
