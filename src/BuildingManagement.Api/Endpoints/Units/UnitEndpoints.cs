using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static partial class Endpoints
{
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
        api.MapPut("/units/{code}", (string code, UnitUpdateRequest request, PhysicalStructureService service, CancellationToken ct) =>
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
}
