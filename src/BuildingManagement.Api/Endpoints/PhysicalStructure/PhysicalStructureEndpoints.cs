using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static partial class Endpoints
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

    private static PageQuery Query(int? pageNumber, int? pageSize, string? search, bool? active,
        string? sort, string? direction) =>
        new(pageNumber ?? 1, pageSize ?? 20, search, active, sort ?? "name", direction ?? "asc");
}
