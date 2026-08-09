using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public interface IApplicationDbContext
{
    DbSet<LocationType> LocationTypes { get; }
    DbSet<BuildingType> BuildingTypes { get; }
    DbSet<UnitUsageType> UnitUsageTypes { get; }
    DbSet<UnitStatus> UnitStatuses { get; }
    DbSet<DocumentType> DocumentTypes { get; }
    DbSet<PartyType> PartyTypes { get; }
    DbSet<PartyContactType> PartyContactTypes { get; }
    DbSet<UnitPartyRelationType> UnitPartyRelationTypes { get; }
    DbSet<Location> Locations { get; }
    DbSet<Complex> Complexes { get; }
    DbSet<Building> Buildings { get; }
    DbSet<Unit> Units { get; }
    DbSet<StoredFile> StoredFiles { get; }
    DbSet<BuildingGalleryFile> BuildingGalleryFiles { get; }
    DbSet<ComplexGalleryFile> ComplexGalleryFiles { get; }
    DbSet<BuildingDocument> BuildingDocuments { get; }
    DbSet<ComplexDocument> ComplexDocuments { get; }
    DbSet<Party> Parties { get; }
    DbSet<PartyContact> PartyContacts { get; }
    DbSet<UnitPartyRelation> UnitPartyRelations { get; }
    DbSet<UnitOccupancyHistory> UnitOccupancyHistories { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);
}

public sealed class AppException(int status, string code, string message, IDictionary<string, string[]>? errors = null) : Exception(message)
{
    public int Status { get; } = status;
    public string Code { get; } = code;
    public IDictionary<string, string[]>? Errors { get; } = errors;
    public static AppException NotFound(string resource) => new(404, $"{resource}.not_found", $"{resource} was not found.");
    public static AppException Conflict(string code, string message) => new(409, code, message);
}

public sealed record Page<T>(IReadOnlyList<T> Items, int PageNumber, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record PageQuery(int PageNumber = 1, int PageSize = 20, string? Search = null,
    bool? IsActive = null, string SortBy = "name", string SortDirection = "asc")
{
    public (int Number, int Size) Validated() =>
        PageNumber < 1 || PageSize is < 1 or > 100
            ? throw new AppException(400, "validation.failed", "Pagination values are invalid.",
                new Dictionary<string, string[]> { ["pagination"] = ["pageNumber must be at least 1 and pageSize must be between 1 and 100."] })
            : (PageNumber, PageSize);
}

public sealed record ReferenceValueResponse(string Key, string Title);
public sealed record ResourceReferenceResponse(string Code, string Name);
public sealed record ReferenceDataResponse(
    IReadOnlyList<ReferenceValueResponse> LocationTypes,
    IReadOnlyList<ReferenceValueResponse> BuildingTypes,
    IReadOnlyList<ReferenceValueResponse> UnitUsageTypes,
    IReadOnlyList<ReferenceValueResponse> UnitStatuses,
    IReadOnlyList<ReferenceValueResponse> DocumentTypes,
    IReadOnlyList<ReferenceValueResponse> PartyTypes,
    IReadOnlyList<ReferenceValueResponse> PartyContactTypes,
    IReadOnlyList<ReferenceValueResponse> UnitPartyRelationTypes);
public sealed record LocationRequest(string? ParentCode, string Name, string LocationTypeKey);
public sealed record LocationResponse(string Code, ResourceReferenceResponse? Parent, string Name, ReferenceValueResponse LocationType,
    bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record ComplexRequest(string LocationCode, string Name, string Address, string PostalCode,
    decimal? Latitude, decimal? Longitude, string? Description);
public sealed record ComplexResponse(string Code, ResourceReferenceResponse Location, string Name, string Address,
    string PostalCode, decimal? Latitude, decimal? Longitude, string? Description, bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record BuildingRequest(string? ComplexCode, string LocationCode, string BuildingTypeKey,
    string Name, string Address, string PostalCode, decimal? Latitude, decimal? Longitude,
    int? FloorsCount, int? ConstructionYear, string? Description);
public sealed record BuildingResponse(string Code, ResourceReferenceResponse? Complex, ResourceReferenceResponse Location,
    ReferenceValueResponse BuildingType, string Name, string Address, string PostalCode,
    decimal? Latitude, decimal? Longitude, int? FloorsCount, int? ConstructionYear,
    string? Description, bool IsActive, DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record UnitRequest(string UsageTypeKey, string StatusKey, string UnitNumber, int? FloorNumber,
    decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount, string? Description,
    UnitOccupancyRequest? Occupancy = null);
public sealed record UnitUpdateRequest(string UsageTypeKey, string StatusKey, string UnitNumber, int? FloorNumber,
    decimal? Area, int? RoomsCount, int ParkingCount, int StorageCount, string? Description);
public sealed record UnitResponse(string Code, ResourceReferenceResponse Building, ResourceReferenceResponse? Complex,
    ReferenceValueResponse UsageType,
    ReferenceValueResponse Status, string UnitNumber, int? FloorNumber, decimal? Area, int? RoomsCount,
    int ParkingCount, int StorageCount, string? Description, CurrentOccupancyResponse CurrentOccupancy,
    bool IsActive,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? UpdatedAtUtc);
public sealed record ActivationRequest(bool IsActive);

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
public sealed class PhysicalStructureService(
    IApplicationDbContext db,
    TimeProvider clock,
    PartyOccupancyService partyOccupancy)
{
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<ReferenceDataResponse> GetReferenceData(CancellationToken ct) =>
        new(
            await ReferenceList(db.LocationTypes, ct),
            await ReferenceList(db.BuildingTypes, ct),
            await ReferenceList(db.UnitUsageTypes, ct),
            await ReferenceList(db.UnitStatuses, ct),
            await ReferenceList(db.DocumentTypes, ct),
            await ReferenceList(db.PartyTypes, ct),
            await ReferenceList(db.PartyContactTypes, ct),
            await ReferenceList(db.UnitPartyRelationTypes, ct));

    public async Task<LocationResponse> CreateLocation(LocationRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var parentId = await LocationId(request.ParentCode, false, ct);
        var typeId = await ReferenceId(db.LocationTypes, request.LocationTypeKey, "location_type", ct);
        var entity = new Location(await UniqueCode(db.Locations, ct), parentId, typeId, request.Name, Now);
        await EnsureSiblingName(entity, ct);
        db.Locations.Add(entity);
        await Save(ct);
        return await GetLocation(entity.Code, ct);
    }

    public async Task<LocationResponse> GetLocation(string code, CancellationToken ct) =>
        await LocationProjection(db.Locations.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("location");

    public async Task<Page<LocationResponse>> GetLocations(PageQuery page, string? parentCode,
        string? locationTypeKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var parentId = await LocationId(parentCode, false, ct);
        long? typeId = string.IsNullOrWhiteSpace(locationTypeKey)
            ? null
            : await ReferenceId(db.LocationTypes, locationTypeKey, "location_type", ct);
        var query = db.Locations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(parentCode)) query = query.Where(x => x.ParentId == parentId);
        if (typeId.HasValue) query = query.Where(x => x.LocationTypeId == typeId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.Name.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(LocationProjection(Order(query, page, x => x.Name)), number, size, ct);
    }

    public async Task<LocationResponse> UpdateLocation(string code, LocationRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await db.Locations.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("location");
        var parentId = await LocationId(request.ParentCode, false, ct);
        if (parentId == entity.Id)
            throw new AppException(400, "validation.failed", "A location cannot be its own parent.",
                new Dictionary<string, string[]> { ["parentCode"] = ["A location cannot be its own parent."] });
        var typeId = await ReferenceId(db.LocationTypes, request.LocationTypeKey, "location_type", ct);
        entity.Update(parentId, typeId, request.Name, Now);
        await EnsureSiblingName(entity, ct);
        await Save(ct);
        return await GetLocation(entity.Code, ct);
    }

    public async Task<ComplexResponse> CreateComplex(ComplexRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var locationId = await LocationId(request.LocationCode, true, ct);
        var entity = new Complex(await UniqueCode(db.Complexes, ct), locationId!.Value, request.Name,
            request.Address, request.PostalCode, request.Latitude, request.Longitude, request.Description, Now);
        db.Complexes.Add(entity);
        await Save(ct);
        return await GetComplex(entity.Code, ct);
    }

    public async Task<ComplexResponse> GetComplex(string code, CancellationToken ct) =>
        await ComplexProjection(db.Complexes.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("complex");

    public async Task<Page<ComplexResponse>> GetComplexes(PageQuery page, string? locationCode, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var locationId = await LocationId(locationCode, false, ct);
        var query = db.Complexes.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(locationCode)) query = query.Where(x => x.LocationId == locationId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.Name.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(ComplexProjection(Order(query, page, x => x.Name)), number, size, ct);
    }

    public async Task<ComplexResponse> UpdateComplex(string code, ComplexRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await db.Complexes.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("complex");
        var locationId = await LocationId(request.LocationCode, true, ct);
        entity.Update(locationId!.Value, request.Name, request.Address, request.PostalCode,
            request.Latitude, request.Longitude, request.Description, Now);
        await Save(ct);
        return await GetComplex(entity.Code, ct);
    }

    public async Task<BuildingResponse> CreateBuilding(BuildingRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var parents = await BuildingParents(request.LocationCode, request.ComplexCode, ct);
        var typeId = await ReferenceId(db.BuildingTypes, request.BuildingTypeKey, "building_type", ct);
        var entity = new Building(await UniqueCode(db.Buildings, ct), parents.ComplexId, parents.LocationId,
            typeId, request.Name, request.Address, request.PostalCode, request.Latitude, request.Longitude,
            request.FloorsCount, request.ConstructionYear, request.Description, Now);
        db.Buildings.Add(entity);
        await Save(ct);
        return await GetBuilding(entity.Code, ct);
    }

    public async Task<BuildingResponse> GetBuilding(string code, CancellationToken ct) =>
        await BuildingProjection(db.Buildings.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("building");

    public async Task<Page<BuildingResponse>> GetBuildings(PageQuery page, string? complexCode,
        string? locationCode, string? buildingTypeKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var complexId = await ComplexId(complexCode, ct);
        var locationId = await LocationId(locationCode, false, ct);
        long? typeId = string.IsNullOrWhiteSpace(buildingTypeKey)
            ? null
            : await ReferenceId(db.BuildingTypes, buildingTypeKey, "building_type", ct);
        var query = db.Buildings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(complexCode)) query = query.Where(x => x.ComplexId == complexId);
        if (!string.IsNullOrWhiteSpace(locationCode)) query = query.Where(x => x.LocationId == locationId);
        if (typeId.HasValue) query = query.Where(x => x.BuildingTypeId == typeId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.Name.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(BuildingProjection(Order(query, page, x => x.Name)), number, size, ct);
    }

    public async Task<BuildingResponse> UpdateBuilding(string code, BuildingRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await db.Buildings.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("building");
        var parents = await BuildingParents(request.LocationCode, request.ComplexCode, ct);
        var typeId = await ReferenceId(db.BuildingTypes, request.BuildingTypeKey, "building_type", ct);
        entity.Update(parents.ComplexId, parents.LocationId, typeId, request.Name, request.Address,
            request.PostalCode, request.Latitude, request.Longitude, request.FloorsCount,
            request.ConstructionYear, request.Description, Now);
        await Save(ct);
        return await GetBuilding(entity.Code, ct);
    }

    public async Task<UnitResponse> CreateUnit(string buildingCode, UnitRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var buildingId = await db.Buildings.Where(x => x.Code == NormalizeCode(buildingCode))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        var usageTypeId = await ReferenceId(db.UnitUsageTypes, request.UsageTypeKey, "unit_usage_type", ct);
        var statusId = await ReferenceId(db.UnitStatuses, request.StatusKey, "unit_status", ct);
        RejectOccupancyStatus(request.StatusKey);
        var entity = new Unit(await UniqueCode(db.Units, ct), buildingId, usageTypeId, statusId,
            request.UnitNumber, request.FloorNumber, request.Area, request.RoomsCount, request.ParkingCount,
            request.StorageCount, request.Description, Now);
        await EnsureUnitNumber(entity, null, ct);
        await db.ExecuteInTransaction(async token =>
        {
            db.Units.Add(entity);
            await partyOccupancy.OnboardUnit(entity, request.Occupancy!, token);
            return entity.Code;
        }, ct);
        return await GetUnit(entity.Code, ct);
    }

    public async Task<UnitResponse> GetUnit(string code, CancellationToken ct) =>
        await UnitProjection(db.Units.AsNoTracking().Where(x => x.Code == NormalizeCode(code))).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound("unit");

    public async Task<Page<UnitResponse>> GetUnits(string buildingCode, PageQuery page, int? floor,
        string? usageTypeKey, string? statusKey, CancellationToken ct)
    {
        var (number, size) = page.Validated();
        var buildingId = await db.Buildings.Where(x => x.Code == NormalizeCode(buildingCode))
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        long? usageTypeId = string.IsNullOrWhiteSpace(usageTypeKey)
            ? null
            : await ReferenceId(db.UnitUsageTypes, usageTypeKey, "unit_usage_type", ct);
        long? statusId = string.IsNullOrWhiteSpace(statusKey)
            ? null
            : await ReferenceId(db.UnitStatuses, statusKey, "unit_status", ct);
        var query = db.Units.AsNoTracking().Where(x => x.BuildingId == buildingId);
        if (floor.HasValue) query = query.Where(x => x.FloorNumber == floor);
        if (usageTypeId.HasValue) query = query.Where(x => x.UsageTypeId == usageTypeId);
        if (statusId.HasValue) query = query.Where(x => x.StatusId == statusId);
        if (page.IsActive.HasValue) query = query.Where(x => x.IsActive == page.IsActive);
        if (!string.IsNullOrWhiteSpace(page.Search)) query = query.Where(x => x.UnitNumber.Contains(page.Search) || x.Code.Contains(page.Search));
        return await Page(UnitProjection(Order(query, page, x => x.UnitNumber)), number, size, ct);
    }

    public async Task<UnitResponse> UpdateUnit(string code, UnitUpdateRequest request, CancellationToken ct)
    {
        RequestValidation.Validate(request);
        var entity = await db.Units.SingleOrDefaultAsync(x => x.Code == NormalizeCode(code), ct)
            ?? throw AppException.NotFound("unit");
        var usageTypeId = await ReferenceId(db.UnitUsageTypes, request.UsageTypeKey, "unit_usage_type", ct);
        var statusId = await ReferenceId(db.UnitStatuses, request.StatusKey, "unit_status", ct);
        RejectOccupancyStatus(request.StatusKey);
        entity.Update(usageTypeId, statusId, request.UnitNumber, request.FloorNumber, request.Area,
            request.RoomsCount, request.ParkingCount, request.StorageCount, request.Description, Now);
        await EnsureUnitNumber(entity, entity.Id, ct);
        await Save(ct);
        return await GetUnit(entity.Code, ct);
    }

    public async Task Activate(string resource, string code, bool active, CancellationToken ct)
    {
        var normalized = NormalizeCode(code);
        Entity entity = resource switch
        {
            "location" => await db.Locations.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            "complex" => await db.Complexes.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            "building" => await db.Buildings.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            "unit" => await db.Units.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource),
            _ => throw new InvalidOperationException()
        };
        entity.SetActivation(active, Now);
        await Save(ct);
    }

    public async Task Delete(string resource, string code, CancellationToken ct)
    {
        var normalized = NormalizeCode(code);
        switch (resource)
        {
            case "location":
                var location = await db.Locations.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                if (await db.Locations.AnyAsync(x => x.ParentId == location.Id, ct) ||
                    await db.Complexes.AnyAsync(x => x.LocationId == location.Id, ct) ||
                    await db.Buildings.AnyAsync(x => x.LocationId == location.Id, ct))
                    throw AppException.Conflict("location.has_dependents", "Location has dependent records.");
                db.Locations.Remove(location);
                break;
            case "complex":
                var complex = await db.Complexes.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                if (await db.Buildings.AnyAsync(x => x.ComplexId == complex.Id, ct))
                    throw AppException.Conflict("complex.has_buildings", "Complex has buildings.");
                db.Complexes.Remove(complex);
                break;
            case "building":
                var building = await db.Buildings.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource);
                if (await db.Units.AnyAsync(x => x.BuildingId == building.Id, ct))
                    throw AppException.Conflict("building.has_units", "Building has units.");
                db.Buildings.Remove(building);
                break;
            case "unit":
                db.Units.Remove(await db.Units.SingleOrDefaultAsync(x => x.Code == normalized, ct) ?? throw AppException.NotFound(resource));
                break;
            default:
                throw new InvalidOperationException();
        }
        await Save(ct);
    }

    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw AppException.Conflict("concurrency.conflict", "The resource changed since it was read."); }
        catch (DbUpdateException) { throw AppException.Conflict("persistence.conflict", "The change conflicts with existing data."); }
    }

    private static string NormalizeCode(string code) => PublicCode.Normalize(code);
    private static void RejectOccupancyStatus(string statusKey)
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
    private static string NormalizeKey(string? key) =>
        string.IsNullOrWhiteSpace(key)
            ? throw new AppException(400, "validation.failed", "Reference key is required.")
            : key.Trim().ToLowerInvariant();

    private static async Task<string> UniqueCode<T>(IQueryable<T> set, CancellationToken ct) where T : Entity
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var code = PublicCode.Create();
            if (!await set.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new AppException(500, "code.generation_failed", "A unique public code could not be generated.");
    }

    private static async Task<long> ReferenceId<T>(IQueryable<T> set, string key, string resource, CancellationToken ct)
        where T : ReferenceDataItem =>
        await set.Where(x => x.Key == NormalizeKey(key) && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
        ?? throw AppException.NotFound(resource);

    private static Task<List<ReferenceValueResponse>> ReferenceList<T>(IQueryable<T> set, CancellationToken ct)
        where T : ReferenceDataItem =>
        set.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.SortOrder)
            .Select(x => new ReferenceValueResponse(x.Key, x.Title)).ToListAsync(ct);

    private async Task<long?> LocationId(string? code, bool required, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            if (required) throw new AppException(400, "validation.failed", "Location code is required.");
            return null;
        }
        return await db.Locations.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("location");
    }

    private async Task<long?> ComplexId(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return await db.Complexes.Where(x => x.Code == NormalizeCode(code)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
            ?? throw AppException.NotFound("complex");
    }

    private async Task<(long LocationId, long? ComplexId)> BuildingParents(string locationCode, string? complexCode, CancellationToken ct)
    {
        var locationId = (await LocationId(locationCode, true, ct))!.Value;
        var complexId = await ComplexId(complexCode, ct);
        // A complex is an operational grouping and may use a broad location (for example a city),
        // while a building uses a more precise location (for example a neighborhood). Both must exist,
        // but equality is not a domain invariant and hierarchy traversal here would be premature.
        return (locationId, complexId);
    }

    private async Task EnsureSiblingName(Location entity, CancellationToken ct)
    {
        if (await db.Locations.AnyAsync(x => x.Id != entity.Id && x.ParentId == entity.ParentId &&
            x.LocationTypeId == entity.LocationTypeId && x.NormalizedName == entity.NormalizedName, ct))
            throw AppException.Conflict("location.name_conflict", "A sibling location with this name and type already exists.");
    }

    private async Task EnsureUnitNumber(Unit entity, long? id, CancellationToken ct)
    {
        if (await db.Units.AnyAsync(x => x.Id != id && x.BuildingId == entity.BuildingId &&
            x.NormalizedUnitNumber == entity.NormalizedUnitNumber, ct))
            throw AppException.Conflict("unit.number_conflict", "Unit number already exists in this building.");
    }

    private static IQueryable<T> Order<T>(IQueryable<T> query, PageQuery page,
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

    private static async Task<Page<T>> Page<T>(IQueryable<T> query, int number, int size, CancellationToken ct)
    {
        var count = await query.CountAsync(ct);
        var items = await query.Skip((number - 1) * size).Take(size).ToListAsync(ct);
        return new(items, number, size, count);
    }

    private IQueryable<LocationResponse> LocationProjection(IQueryable<Location> query) =>
        query.Select(x => new LocationResponse(
            x.Code,
            db.Locations.Where(parent => parent.Id == x.ParentId)
                .Select(parent => new ResourceReferenceResponse(parent.Code, parent.Name)).SingleOrDefault(),
            x.Name,
            db.LocationTypes.Where(type => type.Id == x.LocationTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<ComplexResponse> ComplexProjection(IQueryable<Complex> query) =>
        query.Select(x => new ComplexResponse(
            x.Code,
            db.Locations.Where(location => location.Id == x.LocationId)
                .Select(location => new ResourceReferenceResponse(location.Code, location.Name)).Single(),
            x.Name, x.Address, x.PostalCode, x.Latitude, x.Longitude, x.Description,
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<BuildingResponse> BuildingProjection(IQueryable<Building> query) =>
        query.Select(x => new BuildingResponse(
            x.Code,
            db.Complexes.Where(complex => complex.Id == x.ComplexId)
                .Select(complex => new ResourceReferenceResponse(complex.Code, complex.Name)).SingleOrDefault(),
            db.Locations.Where(location => location.Id == x.LocationId)
                .Select(location => new ResourceReferenceResponse(location.Code, location.Name)).Single(),
            db.BuildingTypes.Where(type => type.Id == x.BuildingTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            x.Name, x.Address, x.PostalCode, x.Latitude, x.Longitude, x.FloorsCount,
            x.ConstructionYear, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));

    private IQueryable<UnitResponse> UnitProjection(IQueryable<Unit> query) =>
        query.Select(x => new UnitResponse(
            x.Code,
            db.Buildings.Where(building => building.Id == x.BuildingId)
                .Select(building => new ResourceReferenceResponse(building.Code, building.Name)).Single(),
            (from building in db.Buildings
             join complex in db.Complexes on building.ComplexId equals complex.Id
             where building.Id == x.BuildingId
             select new ResourceReferenceResponse(complex.Code, complex.Name)).SingleOrDefault(),
            db.UnitUsageTypes.Where(type => type.Id == x.UsageTypeId)
                .Select(type => new ReferenceValueResponse(type.Key, type.Title)).Single(),
            db.UnitStatuses.Where(status => status.Id == x.StatusId)
                .Select(status => new ReferenceValueResponse(status.Key, status.Title)).Single(),
            x.UnitNumber, x.FloorNumber, x.Area, x.RoomsCount, x.ParkingCount, x.StorageCount,
            x.Description, new CurrentOccupancyResponse(
                x.CurrentOccupantsCount == 0 ? ReferenceKeys.UnitStatuses.Vacant : ReferenceKeys.UnitStatuses.Occupied,
                x.CurrentOccupantsCount),
            x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc));
}
