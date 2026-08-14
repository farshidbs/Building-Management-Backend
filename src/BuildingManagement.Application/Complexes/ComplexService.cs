using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class ComplexService(IApplicationDbContext db, TimeProvider clock, ResourceAuthorization authorization) : PhysicalStructureServiceBase(db, clock, authorization)
{
    public Task Activate(string code, bool active, CancellationToken ct) => Activate("complex", code, active, ct);
    public Task Delete(string code, CancellationToken ct) => Delete("complex", code, ct);

    public async Task<ComplexResponse> CreateComplex(ComplexRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        // An authenticated user with no active scope may establish exactly one initial root scope.
        // Once any active membership exists, normal permission enforcement applies.
        if (await Authorization.HasActiveMembership(ct))
            await Authorization.EnsureAny("complex_manage", ct);
        var locationId = await LocationId(request.LocationCode, true, ct);
        var entity = new Complex(await UniqueCode(Db.Complexes, ct), locationId!.Value, request.Name,
            request.Address, request.PostalCode, request.Latitude, request.Longitude, request.Description, Now);
        await Db.ExecuteInTransaction(async token =>
        {
            Db.Complexes.Add(entity);
            await Save(token);
            var roleId = await Db.AccessRoles.Where(x => x.Key == "complex_manager" && x.IsActive)
                .Select(x => x.Id).SingleAsync(token);
            if (!await Db.RoleAllowedScopes.AnyAsync(x => x.RoleId == roleId &&
                x.ScopeKindKey == IamKeys.Scopes.Complex, token))
                throw AppException.Conflict("authorization.role_scope_invalid",
                    "The creator role cannot be assigned at Complex scope.");
            if (!await Db.AccessMemberships.AnyAsync(x => x.UserId == Authorization.UserId &&
                x.RoleId == roleId && x.ComplexId == entity.Id && x.IsActive && x.EndsAtUtc == null, token))
                Db.AccessMemberships.Add(new AccessMembership(await UniqueCode(Db.AccessMemberships, token),
                    Authorization.UserId, roleId, entity.Id, null, null, null, Now));
            await Save(token);
            return entity.Id;
        }, ct);
        return await GetComplex(entity.Code, ct);
    }

    public async Task<ComplexResponse> GetComplex(string code, CancellationToken ct)
    {
        var entity = await Db.Complexes.AsNoTracking().SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("complex");
        await Authorization.Ensure("complex_view", entity.Id, null, null, ct);
        return await ComplexProjection(Db.Complexes.AsNoTracking().Where(x => x.Id == entity.Id)).SingleAsync(ct);
    }

    public async Task<Page<ComplexResponse>> GetComplexes(PageQuery page, string? locationCode, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var accessible = await Authorization.Accessible("complex_view", ct);
        var locationId = await LocationId(locationCode, false, ct);
        var query = Db.Complexes.AsNoTracking().Where(x => accessible.ComplexIds.Contains(x.Id));
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
        await Authorization.Ensure("complex_manage", entity.Id, null, null, ct);
        var locationId = await LocationId(request.LocationCode, true, ct);
        entity.Update(locationId!.Value, request.Name, request.Address, request.PostalCode,
            request.Latitude, request.Longitude, request.Description, Now);
        await Save(ct);
        return await GetComplex(entity.Code, ct);
    }

}
