using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class ComplexService(IApplicationDbContext db, TimeProvider clock) : PhysicalStructureServiceBase(db, clock)
{
    public Task Activate(string code, bool active, CancellationToken ct) => Activate("complex", code, active, ct);
    public Task Delete(string code, CancellationToken ct) => Delete("complex", code, ct);

    public async Task<ComplexResponse> CreateComplex(ComplexRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var locationId = await LocationId(request.LocationCode, true, ct);
        var entity = new Complex(await UniqueCode(Db.Complexes, ct), locationId!.Value, request.Name,
            request.Address, request.PostalCode, request.Latitude, request.Longitude, request.Description, Now);
        Db.Complexes.Add(entity);
        await Save(ct);
        return await GetComplex(entity.Code, ct);
    }

    public async Task<ComplexResponse> GetComplex(string code, CancellationToken ct) =>
        await ComplexProjection(Db.Complexes.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("complex");

    public async Task<Page<ComplexResponse>> GetComplexes(PageQuery page, string? locationCode, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var locationId = await LocationId(locationCode, false, ct);
        var query = Db.Complexes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(locationCode)) query = query.Where(x => x.LocationId == locationId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.Name.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(ComplexProjection(Order(query, page, x => x.Name)), number, size, ct);
    }

    public async Task<ComplexResponse> UpdateComplex(string code, ComplexRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await Db.Complexes.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("complex");
        var locationId = await LocationId(request.LocationCode, true, ct);
        entity.Update(locationId!.Value, request.Name, request.Address, request.PostalCode,
            request.Latitude, request.Longitude, request.Description, Now);
        await Save(ct);
        return await GetComplex(entity.Code, ct);
    }

}
