using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static class EndpointRegistration
{
    public static void MapApiEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/v1");
        api.MapGet("/reference-data", (ReferenceDataService service, CancellationToken ct) =>
            service.GetReferenceData(ct)).WithTags("Reference Data").AllowAnonymous();
        api.MapLocationEndpoints();
        api.MapComplexEndpoints();
        api.MapBuildingEndpoints();
        api.MapUnitEndpoints();
        api.MapPartyEndpoints();
        api.MapUnitPartyOccupancyEndpoints();
        api.MapFinancialEndpoints();
        app.MapFileManagementEndpoints();
        app.MapAssetEndpoints();
        app.MapIamEndpoints();
    }

    internal static PageQuery Query(int? pageNumber, int? pageSize, string? search, bool? active,
        string? sort, string? direction) =>
        new(pageNumber ?? 1, pageSize ?? 20, search, active, sort ?? "name", direction ?? "asc");
}
