using BuildingManagement.Application;

namespace BuildingManagement.Api;

internal static class ComplexEndpoints
{
    internal static void MapComplexEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/complexes").WithTags("Complexes");
        group.MapPost("/", async (ComplexRequest request, ComplexService service, CancellationToken ct) =>
        {
            var response = await service.CreateComplex(request, ct);
            return Results.Created($"/api/v1/complexes/{response.Code}", response);
        });
        group.MapGet("/{code}", (string code, ComplexService service, CancellationToken ct) =>
            service.GetComplex(code, ct));
        group.MapGet("/", (string? locationCode, string? search, bool? isActive, int? pageNumber,
            int? pageSize, string? sortBy, string? sortDirection, ComplexService service, CancellationToken ct) =>
            service.GetComplexes(EndpointRegistration.Query(pageNumber, pageSize, search, isActive, sortBy, sortDirection), locationCode, ct));
        group.MapPut("/{code}", (string code, ComplexRequest request, ComplexService service, CancellationToken ct) =>
            service.UpdateComplex(code, request, ct));
        group.MapPatch("/{code}/activation", async (string code, ActivationRequest request, ComplexService service, CancellationToken ct) =>
        {
            await service.Activate(code, request.IsActive, ct);
            return Results.NoContent();
        });
        group.MapDelete("/{code}", async (string code, ComplexService service, CancellationToken ct) =>
        {
            await service.Delete(code, ct);
            return Results.NoContent();
        });
    }
}
