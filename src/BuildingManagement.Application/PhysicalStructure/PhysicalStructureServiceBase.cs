using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

internal static class RequestValidation
{
    private static readonly string[] RequiredError = ["The field is required."];

    public static void Validate(LocationRequest? request)
    {
        request = NotNull(request);
        Required((nameof(request.Name), request.Name), (nameof(request.LocationTypeKey), request.LocationTypeKey));
    }

    public static void Validate(ComplexRequest? request)
    {
        request = NotNull(request);
        Required((nameof(request.LocationCode), request.LocationCode), (nameof(request.Name), request.Name),
            (nameof(request.Address), request.Address), (nameof(request.PostalCode), request.PostalCode));
    }

    public static void Validate(BuildingRequest? request)
    {
        request = NotNull(request);
        Required((nameof(request.LocationCode), request.LocationCode),
            (nameof(request.BuildingTypeKey), request.BuildingTypeKey), (nameof(request.Name), request.Name),
            (nameof(request.Address), request.Address), (nameof(request.PostalCode), request.PostalCode));
    }

    public static void Validate(UnitRequest? request)
    {
        request = NotNull(request);
        Required((nameof(request.UsageTypeKey), request.UsageTypeKey), (nameof(request.StatusKey), request.StatusKey),
            (nameof(request.UnitNumber), request.UnitNumber));
        if (request.Occupancy is null)
            throw new AppException(400, "validation.failed", "One or more validation errors occurred.",
                new Dictionary<string, string[]> { ["occupancy"] = ["Occupancy is required."] });
    }

    public static void Validate(UnitUpdateRequest? request)
    {
        request = NotNull(request);
        Required((nameof(request.UsageTypeKey), request.UsageTypeKey),
            (nameof(request.StatusKey), request.StatusKey), (nameof(request.UnitNumber), request.UnitNumber));
    }

    private static T NotNull<T>(T? request) where T : class =>
        request ?? throw new AppException(400, "validation.failed", "A request body is required.",
            new Dictionary<string, string[]> { ["request"] = RequiredError });

    private static void Required(params (string Field, string? Value)[] fields)
    {
        var errors = fields
            .Where(field => string.IsNullOrWhiteSpace(field.Value))
            .ToDictionary(field => char.ToLowerInvariant(field.Field[0]) + field.Field[1..],
                _ => RequiredError);
        if (errors.Count > 0)
            throw new AppException(400, "validation.failed", "One or more validation errors occurred.", errors);
    }
}
public abstract class PhysicalStructureServiceBase(IApplicationDbContext db, TimeProvider clock,
    ResourceAuthorization? resourceAuthorization = null)
{
    protected IApplicationDbContext Db { get; } = db;
    protected ResourceAuthorization Authorization { get; } = resourceAuthorization!;
    protected DateTimeOffset Now => clock.GetUtcNow();

    protected async Task Activate(string resource, string code, bool active, CancellationToken ct)
    {
        var normalized = NormalizeCode(code);
        Entity entity = resource switch
        {
            "location" => await Db.Locations.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            "complex" => await Db.Complexes.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            "building" => await Db.Buildings.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            "unit" => await Db.Units.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            _ => throw new InvalidOperationException()
        };
        await AuthorizeEntity(resource, entity.Id, true, ct);
        entity.SetActivation(active, Now);
        await Save(ct);
    }

    protected async Task Delete(string resource, string code, CancellationToken ct)
    {
        var normalized = NormalizeCode(code);
        switch (resource)
        {
            case "location":
                var location = await Db.Locations.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                if (await Db.Locations.AnyAsync(x => x.ParentId == location.Id, ct) ||
                    await Db.Complexes.AnyAsync(x => x.LocationId == location.Id, ct) ||
                    await Db.Buildings.AnyAsync(x => x.LocationId == location.Id, ct))
                    throw AppException.Conflict("location.has_dependents", "Location has dependent records.");
                Db.Locations.Remove(location);
                break;
            case "complex":
                var complex = await Db.Complexes.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                await Authorization.Ensure("complex_manage", complex.Id, null, null, ct);
                if (await Db.Buildings.AnyAsync(x => x.ComplexId == complex.Id, ct))
                    throw AppException.Conflict("complex.has_buildings", "Complex has buildings.");
                Db.Complexes.Remove(complex);
                break;
            case "building":
                var building = await Db.Buildings.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                await Authorization.Ensure("building_manage", building.ComplexId, building.Id, null, ct);
                if (await Db.Units.AnyAsync(x => x.BuildingId == building.Id, ct))
                    throw AppException.Conflict("building.has_units", "Building has units.");
                Db.Buildings.Remove(building);
                break;
            case "unit":
                var unit = await Db.Units.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                await Authorization.Ensure("unit_manage", null, unit.BuildingId, unit.Id, ct);
                Db.Units.Remove(unit);
                break;
            default:
                throw new InvalidOperationException();
        }
        await Save(ct);
    }

    private async Task AuthorizeEntity(string resource, long id, bool manage, CancellationToken ct)
    {
        if (resource == "complex") await Authorization.Ensure(manage ? "complex_manage" : "complex_view", id, null, null, ct);
        else if (resource == "building")
        {
            var parent = await Db.Buildings.Where(x => x.Id == id).Select(x => x.ComplexId).SingleAsync(ct);
            await Authorization.Ensure(manage ? "building_manage" : "building_view", parent, id, null, ct);
        }
        else if (resource == "unit")
        {
            var parent = await Db.Units.Where(x => x.Id == id).Select(x => x.BuildingId).SingleAsync(ct);
            await Authorization.Ensure(manage ? "unit_manage" : "unit_view", null, parent, id, ct);
        }
    }

    protected async Task Save(CancellationToken ct)
    {
        try { await Db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw AppException.Conflict("concurrency.conflict", "The resource changed since it was read."); }
        catch (DbUpdateException) { throw AppException.Conflict("persistence.conflict", "The change conflicts with existing data."); }
    }

    protected static string NormalizeCode(string code) => PublicCode.Normalize(code);
    protected static void RejectOccupancyStatus(string statusKey)
    {
        var normalized = NormalizeKey(statusKey);
        if (normalized is ReferenceKeys.UnitStatuses.Occupied or ReferenceKeys.UnitStatuses.Vacant)
            throw new AppException(400, "validation.failed",
                "Occupied and vacant are derived from current occupants count.",
                new Dictionary<string, string[]>
                {
                    ["statusKey"] = ["Use an operational status; occupancy is supplied separately."]
                });
    }
    protected static string NormalizeKey(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? throw new AppException(400, "validation.failed", "Reference key is required.")
            : key.Trim().ToLowerInvariant();

    protected static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct) where T : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new AppException(500, "code.generation_failed", "A unique public code could not be generated.");
    }

    protected static async Task<long> ReferenceId<T>(IQueryable<T> set, string key, string resource, CancellationToken ct)
        where T : ReferenceDataItem =>
        await set.Where(x => x.Key == NormalizeKey(key) && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound(resource);

    protected static Task<List<ReferenceValueResponse>> ReferenceList<T>(IQueryable<T> set, CancellationToken ct)
        where T : ReferenceDataItem =>
        set.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new ReferenceValueResponse(x.Key, x.Title)).ToListAsync(ct);

    protected async Task<long?> LocationId(string? code, bool required, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            if (required) throw new AppException(400, "validation.failed", "Location code is required.");
            return null;
        }
        return await Db.Locations.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("location");
    }

    protected async Task<long?> ComplexId(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return await Db.Complexes.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("complex");
    }

    protected async Task<(long LocationId, long? ComplexId)> BuildingParents(string locationCode, string? complexCode, CancellationToken ct)
    {
        var locationId = (await LocationId(locationCode, true, ct))!.Value;
        var complexId = await ComplexId(complexCode, ct);
        // A complex is an operational grouping and may use a broad location (for example a city),
        // while a building uses a more precise location (for example a neighborhood). Both must exist,
        // but equality is not a domain invariant and hierarchy traversal here would be premature.
        return (locationId, complexId);
    }

    protected async Task EnsureSiblingName(Location entity, CancellationToken ct)
    {
        if (await Db.Locations.AnyAsync(x => x.Id != entity.Id && x.ParentId == entity.ParentId &&
            x.LocationTypeId == entity.LocationTypeId && x.NormalizedName == entity.NormalizedName, ct))
            throw AppException.Conflict("location.name_conflict", "A sibling location with this name and type already exists.");
    }

    protected async Task EnsureUnitNumber(Unit entity, long? id, CancellationToken ct)
    {
        if (await Db.Units.AnyAsync(x => x.Id != id && x.BuildingId == entity.BuildingId &&
            x.NormalizedUnitNumber == entity.NormalizedUnitNumber, ct))
            throw AppException.Conflict("unit.number_conflict", "Unit number already exists in this building.");
    }

    protected static IQueryable<T> Order<T>(IQueryable<T> query, PageQuery page,
        System.Linq.Expressions.Expression<Func<T, string>> name) where T : Entity =>
        (page.SortBy.ToLowerInvariant(), page.SortDirection.ToLowerInvariant()) switch
        {
            ("createdatutc", "desc") => query.OrderByDescending(x => x.CreatedAtUtc),
            ("createdatutc", _) => query.OrderBy(x => x.CreatedAtUtc),
            ("code", "desc") => query.OrderByDescending(x => x.Code),
            ("code", _) => query.OrderBy(x => x.Code),
            (_, "desc") => query.OrderByDescending(name),
            _ => query.OrderBy(name)
        };

    protected static async Task<Page<T>> Page<T>(IQueryable<T> query, int number, int size, CancellationToken ct)
    {
        var count = await query.CountAsync(ct);
        var items = await query.Skip((number - 1) * size).Take(size).ToListAsync(ct);
        return new(items, number, size, count);
    }

    protected IQueryable<LocationResponse> LocationProjection(IQueryable<Location> query) =>
        query.Select(x => new LocationResponse(
            x.Code,
            Db.Locations.Where(parent => parent.Id == x.ParentId)
                .Select(parent => new ResourceReferenceResponse(parent.Code, parent.Name)).SingleOrDefault(),
            x.Name,
            Db.LocationTypes.Where(type => type.Id == x.LocationTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<ComplexResponse> ComplexProjection(IQueryable<Complex> query) =>
        query.Select(x => new ComplexResponse(
            x.Code,
            Db.Locations.Where(location => location.Id == x.LocationId)
                .Select(location => new ResourceReferenceResponse(location.Code, location.Name)).Single(),
            x.Name, x.Address, x.PostalCode, x.Latitude, x.Longitude, x.Description,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<BuildingResponse> BuildingProjection(IQueryable<Building> query) =>
        query.Select(x => new BuildingResponse(
            x.Code,
            Db.Complexes.Where(complex => complex.Id == x.ComplexId)
                .Select(complex => new ResourceReferenceResponse(complex.Code, complex.Name)).SingleOrDefault(),
            Db.Locations.Where(location => location.Id == x.LocationId)
                .Select(location => new ResourceReferenceResponse(location.Code, location.Name)).Single(),
            Db.BuildingTypes.Where(type => type.Id == x.BuildingTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Name, x.Address, x.PostalCode, x.Latitude, x.Longitude, x.FloorsCount,
            x.ConstructionYear, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    protected IQueryable<UnitResponse> UnitProjection(IQueryable<Unit> query) =>
        query.Select(x => new UnitResponse(
            x.Code,
            Db.Buildings.Where(building => building.Id == x.BuildingId)
                .Select(building => new ResourceReferenceResponse(building.Code, building.Name)).Single(),
            (from building in Db.Buildings
             join complex in Db.Complexes on building.ComplexId equals complex.Id
             where building.Id == x.BuildingId
             select new ResourceReferenceResponse(complex.Code, complex.Name)).SingleOrDefault(),
            Db.UnitUsageTypes.Where(type => type.Id == x.UsageTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            Db.UnitStatuses.Where(status => status.Id == x.StatusId)
                .Select(status => new ReferenceValueResponse(status.Key, status.Title)).Single(),
            x.UnitNumber, x.FloorNumber, x.Area, x.RoomsCount, x.ParkingCount, x.StorageCount,
            x.Description, new CurrentOccupancyResponse(
                x.CurrentOccupantsCount == 0 ? ReferenceKeys.UnitStatuses.Vacant : ReferenceKeys.UnitStatuses.Occupied,
                x.CurrentOccupantsCount),
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
}

