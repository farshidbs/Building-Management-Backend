using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static class Endpoints
{
    public static void MapPhysicalStructureEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        api.MapGet("/reference-data", (PhysicalStructureService service, CancellationToken ct) =>
            service.GetReferenceData(ct)).WithTags("Reference Data");
        MapLocations(api);
        MapComplexes(api);
        MapBuildings(api);
        MapUnits(api);
    }

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

    private static void MapComplexes(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/complexes").WithTags("Complexes");
        group.MapPost("/", async (ComplexRequest request, PhysicalStructureService service, CancellationToken ct) =>
        {
            var response = await service.CreateComplex(request, ct);
            return Results.Created($"/api/v1/complexes/{response.Code}", response);
        });
        group.MapGet("/{code}", (string code, PhysicalStructureService service, CancellationToken ct) =>
            service.GetComplex(code, ct));
        group.MapGet("/", (string? locationCode, string? search, bool? isActive, int? pageNumber,
            int? pageSize, string? sortBy, string? sortDirection, PhysicalStructureService service, CancellationToken ct) =>
            service.GetComplexes(Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection), locationCode, ct));
        group.MapPut("/{code}", (string code, ComplexRequest request, PhysicalStructureService service, CancellationToken ct) =>
            service.UpdateComplex(code, request, ct));
        group.MapPatch("/{code}/activation", async (string code, ActivationRequest request, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Activate("complex", code, request.IsActive, ct);
            return Results.NoContent();
        });
        group.MapDelete("/{code}", async (string code, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Delete("complex", code, ct);
            return Results.NoContent();
        });
    }

    private static void MapBuildings(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/buildings").WithTags("Buildings");
        group.MapPost("/", async (BuildingRequest request, PhysicalStructureService service, CancellationToken ct) =>
        {
            var response = await service.CreateBuilding(request, ct);
            return Results.Created($"/api/v1/buildings/{response.Code}", response);
        });
        group.MapGet("/{code}", (string code, PhysicalStructureService service, CancellationToken ct) =>
            service.GetBuilding(code, ct));
        group.MapGet("/", (string? complexCode, string? locationCode, string? buildingTypeKey,
            string? search, bool? isActive, int? pageNumber, int? pageSize, string? sortBy,
            string? sortDirection, PhysicalStructureService service, CancellationToken ct) =>
            service.GetBuildings(Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection),
                complexCode, locationCode, buildingTypeKey, ct));
        group.MapPut("/{code}", (string code, BuildingRequest request, PhysicalStructureService service, CancellationToken ct) =>
            service.UpdateBuilding(code, request, ct));
        group.MapPatch("/{code}/activation", async (string code, ActivationRequest request, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Activate("building", code, request.IsActive, ct);
            return Results.NoContent();
        });
        group.MapDelete("/{code}", async (string code, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Delete("building", code, ct);
            return Results.NoContent();
        });
    }

    private static void MapUnits(RouteGroupBuilder api)
    {
        api.MapPost("/buildings/{buildingCode}/units",
            async (string buildingCode, UnitRequest request, PhysicalStructureService service, CancellationToken ct) =>
            {
                var response = await service.CreateUnit(buildingCode, request, ct);
                return Results.Created($"/api/v1/units/{response.Code}", response);
            }).WithTags("Units");
        api.MapGet("/units/{code}", (string code, PhysicalStructureService service, CancellationToken ct) =>
            service.GetUnit(code, ct)).WithTags("Units");
        api.MapGet("/buildings/{buildingCode}/units", (string buildingCode, int? floorNumber,
            string? usageTypeKey, string? statusKey, string? search, bool? isActive, int? pageNumber,
            int? pageSize, string? sortBy, string? sortDirection, PhysicalStructureService service, CancellationToken ct) =>
            service.GetUnits(buildingCode, Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection),
                floorNumber, usageTypeKey, statusKey, ct)).WithTags("Units");
        api.MapPut("/units/{code}", (string code, UnitRequest request, PhysicalStructureService service, CancellationToken ct) =>
            service.UpdateUnit(code, request, ct)).WithTags("Units");
        api.MapPatch("/units/{code}/activation",
            async (string code, ActivationRequest request, PhysicalStructureService service, CancellationToken ct) =>
            {
                await service.Activate("unit", code, request.IsActive, ct);
                return Results.NoContent();
            }).WithTags("Units");
        api.MapDelete("/units/{code}", async (string code, PhysicalStructureService service, CancellationToken ct) =>
        {
            await service.Delete("unit", code, ct);
            return Results.NoContent();
        }).WithTags("Units");
    }

    private static PageQuery Query(int? pageNumber, int? pageSize, string? search, bool? active,
        string? sort, string? direction) =>
        new(pageNumber ?? 1, pageSize ?? 20, search, active, sort ?? "name", direction ?? "asc");
}
