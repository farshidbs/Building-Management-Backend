using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class ExpenseService(IApplicationDbContext db, TimeProvider clock, ResourceAuthorization authorization) : FinancialServiceBase(db, clock, authorization)
{
    public async Task<Page<ExpenseResponse>> List(PageQuery query, string? status, CancellationToken ct)
    {
        var (pageNumber, pageSize) = query.Validated();
        var accessible = await Authorization.Accessible("expense_view", ct);
        var expenses = Db.Expenses.AsNoTracking().Where(x =>
            x.ComplexId.HasValue && accessible.ComplexIds.Contains(x.ComplexId.Value) ||
            x.BuildingId.HasValue && accessible.BuildingIds.Contains(x.BuildingId.Value));
        if (!string.IsNullOrWhiteSpace(status)) expenses = expenses.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search)) expenses = expenses.Where(x => x.Title.Contains(query.Search));
        var total = await expenses.CountAsync(ct);
        var rows = await expenses.OrderByDescending(x => x.ExpenseDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = new List<ExpenseResponse>(rows.Count);
        foreach (var row in rows) items.Add(await Response(row, ct));
        return new(items, pageNumber, pageSize, total);
    }

    public async Task<ExpenseDetailResponse> Get(string code, CancellationToken ct)
    {
        var expense = await Db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("expense");
        await Authorize(expense, "expense_view", ct);
        return await Detail(expense, ct);
    }

    public async Task<ExpenseTypeResponse> CreateType(ExpenseTypeRequest r, CancellationToken ct)
    {
        var building = string.IsNullOrWhiteSpace(r.BuildingCode) ? (long?)null : await Db.Buildings.Where(x => x.Code == Code(r.BuildingCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        var complex = string.IsNullOrWhiteSpace(r.ComplexCode) ? (long?)null : await Db.Complexes.Where(x => x.Code == Code(r.ComplexCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
        if (!building.HasValue && !complex.HasValue) throw Validation("scope", "Custom expense type requires a scope.");
        await Authorize("expense_create", complex, building, null, ct);
        var type = new ExpenseType(null, r.Title, complex, building, r.SortOrder, Now); Db.ExpenseTypes.Add(type); await Save(ct);
        return new(type.Id, type.Key, type.Title, r.BuildingCode, r.ComplexCode, type.IsActive);
    }

    public async Task<IReadOnlyList<ExpenseTypeResponse>> Types(string? buildingCode, string? complexCode, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(buildingCode) && !string.IsNullOrWhiteSpace(complexCode))
            throw Validation("scope", "Supply either buildingCode or complexCode, not both.");
        long? building = null, complex = null;
        if (!string.IsNullOrWhiteSpace(buildingCode)) { var row = await Db.Buildings.Where(x => x.Code == Code(buildingCode)).Select(x => new { x.Id, x.ComplexId }).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building"); building = row.Id; complex = row.ComplexId; }
        else if (!string.IsNullOrWhiteSpace(complexCode)) complex = await Db.Complexes.Where(x => x.Code == Code(complexCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
        if (building.HasValue || complex.HasValue) await Authorize("expense_view", complex, building, null, ct);
        else await Authorization.EnsureAny("expense_view", ct);
        return await AvailableTypes(building, complex).AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Title).Select(x => new ExpenseTypeResponse(x.Id, x.Key, x.Title, x.BuildingId == null ? null : Db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => b.Code).Single(), x.ComplexId == null ? null : Db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => c.Code).Single(), x.IsActive)).ToListAsync(ct);
    }
    public async Task<ExpenseResponse> Create(ExpenseRequest request, CancellationToken ct) =>
        await Db.ExecuteInTransaction(async token =>
        {
            if (request is null) throw Validation("request", "Required.");
            if (!string.IsNullOrWhiteSpace(request.BuildingCode) && !string.IsNullOrWhiteSpace(request.ComplexCode))
                throw Validation("scope", "Supply either BuildingCode or ComplexCode, not both.");
            long? parentComplexId = null;
            long? buildingId = null;
            if (!string.IsNullOrWhiteSpace(request.BuildingCode))
            {
                var building = await Db.Buildings.Where(x => x.Code == Code(request.BuildingCode))
                    .Select(x => new { x.Id, x.ComplexId }).SingleOrDefaultAsync(token)
                    ?? throw AppException.NotFound("building");
                buildingId = building.Id;
                parentComplexId = building.ComplexId;
            }
            long? complexId = string.IsNullOrWhiteSpace(request.ComplexCode)
                ? null
                : await Db.Complexes.Where(x => x.Code == Code(request.ComplexCode))
                    .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("complex");
            await Authorize("expense_create", complexId ?? parentComplexId, buildingId, null, token);
            var typeId = await AvailableTypes(buildingId, buildingId.HasValue ? parentComplexId : complexId)
                .Where(x => x.Id == request.ExpenseTypeId)
                .Select(x => (long?)x.Id).FirstOrDefaultAsync(token) ?? throw AppException.NotFound("expense_type");
            long? vendorId = string.IsNullOrWhiteSpace(request.VendorPartyCode)
                ? null
                : await Db.Parties.Where(x => x.Code == Code(request.VendorPartyCode))
                    .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("party");
            var expense = new Expense(await Unique(Db.Expenses, token), buildingId, complexId, typeId,
                vendorId, request.Title, request.Amount, request.ExpenseDate, request.DueDate,
                request.Description, Now);
            Db.Expenses.Add(expense);
            await Save(token);
            await AddLinks(expense, request, token);
            await Save(token);
            return await Response(expense, token);
        }, ct);
    public async Task<ExpenseResponse> Finalize(string code, CancellationToken ct) { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(code), ct) ?? throw AppException.NotFound("expense"); await Authorize(e, "expense_finalize", ct); e.Finalize(Now); await Save(ct); return await Response(e, ct); }
    public async Task<ExpenseResponse> Update(string code, UpdateExpenseDraftRequest r, CancellationToken ct) { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(code), ct) ?? throw AppException.NotFound("expense"); await Authorize(e, "expense_update", ct); var parentComplexId = e.BuildingId.HasValue ? await Db.Buildings.Where(x => x.Id == e.BuildingId).Select(x => x.ComplexId).SingleAsync(ct) : e.ComplexId; var typeId = await AvailableTypes(e.BuildingId, parentComplexId).Where(x => x.Id == r.ExpenseTypeId).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("expense_type"); long? vendorId = string.IsNullOrWhiteSpace(r.VendorPartyCode) ? null : await Db.Parties.Where(x => x.Code == Code(r.VendorPartyCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party"); e.Update(e.BuildingId, e.ComplexId, typeId, vendorId, r.Title, r.Amount, r.ExpenseDate, r.DueDate, r.Description, Now); await Save(ct); return await Response(e, ct); }
    public async Task<ExpenseDisbursementResponse> CreateDisbursement(string expenseCode, ExpenseDisbursementRequest r, CancellationToken ct) { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(expenseCode), ct) ?? throw AppException.NotFound("expense"); await Authorize(e, "expense_finalize", ct); if (e.Status != FinancialKeys.Statuses.Finalized) throw AppException.Conflict("expense.not_finalized", "Expense must be finalized."); var fund = await FundAccount(r.FundBuildingCode, r.FundComplexCode, r.FundAccountKindKey, ct); if ((e.BuildingId != null && fund.BuildingId != e.BuildingId) || (e.ComplexId != null && fund.ComplexId != e.ComplexId)) throw Validation("fundAccount", "Fund must belong to the expense scope."); var payee = string.IsNullOrWhiteSpace(r.PayeePartyCode) ? (long?)null : await Db.Parties.Where(x => x.Code == Code(r.PayeePartyCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party"); var d = new ExpenseDisbursement(await Unique(Db.ExpenseDisbursements, ct), e.Id, fund.Id, payee, r.Amount, r.PaymentMethodKey, r.PaidAtUtc, r.Notes, Now); Db.ExpenseDisbursements.Add(d); await Save(ct); return await DisbursementResponse(d, e.Code, ct); }
    public async Task<ExpenseDisbursementResponse> FinalizeDisbursement(string expenseCode, string disbursementCode, CancellationToken ct) => await Db.ExecuteInTransaction<ExpenseDisbursementResponse>(async token => { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(expenseCode), token) ?? throw AppException.NotFound("expense"); await Authorize(e, "expense_finalize", token); var d = await Db.ExpenseDisbursements.SingleOrDefaultAsync(x => x.Code == Code(disbursementCode) && x.ExpenseId == e.Id, token) ?? throw AppException.NotFound("expense_disbursement"); if (await Db.FinancialTransactions.AnyAsync(x => x.ExpenseDisbursementId == d.Id, token)) return await DisbursementResponse(d, e.Code, token); var alreadyPaid = await Db.ExpenseDisbursements.Where(x => x.ExpenseId == e.Id && x.Status == FinancialKeys.Statuses.Finalized).SumAsync(x => (decimal?)x.Amount, token) ?? 0; if (alreadyPaid + d.Amount > e.Amount) throw AppException.Conflict("expense.over_disbursement", "Disbursements cannot exceed expense amount."); var fund = await Db.FinancialAccounts.SingleAsync(x => x.Id == d.FundAccountId, token); d.Finalize(Now); e.TouchFinancialState(Now); var tx = new FinancialTransaction(FinancialKeys.TransactionTypes.ExpenseDisbursement, null, null, d.Id, null, d.PaidAtUtc!.Value, $"Expense {e.Code}", Now); Db.FinancialTransactions.Add(tx); await Apply(fund, FinancialKeys.Effects.Decrease, d.Amount, tx, token); await Save(token); return await DisbursementResponse(d, e.Code, token); }, ct);
    private async Task<ExpenseResponse> Response(Expense e, CancellationToken ct) { var paid = await Db.ExpenseDisbursements.Where(x => x.ExpenseId == e.Id && x.Status == FinancialKeys.Statuses.Finalized).SumAsync(x => (decimal?)x.Amount, ct) ?? 0; return new(e.Code, e.Title, e.Amount, paid, Math.Max(0, e.Amount - paid), e.Status, e.ExpenseDate, e.DueDate); }

    private async Task<ExpenseDisbursementResponse> DisbursementResponse(ExpenseDisbursement d, string expenseCode, CancellationToken ct)
    { var fund = await Db.FinancialAccounts.Where(x => x.Id == d.FundAccountId).Select(x => new { x.AccountKindKey, OwnerCode = x.BuildingId != null ? Db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => b.Code).Single() : Db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => c.Code).Single() }).SingleAsync(ct); var files = await Db.ExpenseDisbursementFiles.AsNoTracking().Where(x => x.ExpenseDisbursementId == d.Id).Select(x => new FinancialFileResponse(Db.StoredFiles.Where(f => f.Id == x.StoredFileId).Select(f => new StoredFileResponse(f.Code, "/api/v1/files/" + f.Code + "/content", f.OriginalFileName, f.ContentType, f.FileExtension, f.FileSizeBytes)).Single(), x.Title, null)).ToListAsync(ct); return new(d.Code, expenseCode, d.Amount, d.Status, d.PaymentMethodKey, d.PaidAtUtc, d.PayeePartyId == null ? null : await Db.Parties.Where(x => x.Id == d.PayeePartyId).Select(x => x.Code).SingleAsync(ct), fund.OwnerCode, files); }

    private async Task<ExpenseDetailResponse> Detail(Expense e, CancellationToken ct)
    { var summary = await Response(e, ct); var ds = await Db.ExpenseDisbursements.AsNoTracking().Where(x => x.ExpenseId == e.Id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct); var disbursements = new List<ExpenseDisbursementResponse>(); foreach (var d in ds) disbursements.Add(await DisbursementResponse(d, e.Code, ct)); var docs = await Db.ExpenseDocuments.AsNoTracking().Where(x => x.ExpenseId == e.Id).Select(x => new FinancialFileResponse(Db.StoredFiles.Where(f => f.Id == x.StoredFileId).Select(f => new StoredFileResponse(f.Code, "/api/v1/files/" + f.Code + "/content", f.OriginalFileName, f.ContentType, f.FileExtension, f.FileSizeBytes)).Single(), x.Title, x.Description)).ToListAsync(ct); return new(e.Code, e.Title, e.Amount, summary.PaidAmount, summary.RemainingAmount, e.Status, e.ExpenseDate, e.DueDate, e.Description, e.VendorPartyId == null ? null : await Db.Parties.Where(x => x.Id == e.VendorPartyId).Select(x => x.Code).SingleAsync(ct), disbursements, docs, await Db.ExpenseAssets.Where(x => x.ExpenseId == e.Id).Select(x => Db.Assets.Where(a => a.Id == x.AssetId).Select(a => a.Code).Single()).ToListAsync(ct), await Db.ExpenseAssetEvents.Where(x => x.ExpenseId == e.Id).Select(x => x.AssetEventId).ToListAsync(ct), await Db.ExpenseBuildings.Where(x => x.ExpenseId == e.Id).Select(x => Db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => b.Code).Single()).ToListAsync(ct), await Db.ExpenseComplexes.Where(x => x.ExpenseId == e.Id).Select(x => Db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => c.Code).Single()).ToListAsync(ct), await Db.DemandExpenses.Where(x => x.ExpenseId == e.Id).Select(x => Db.Demands.Where(d => d.Id == x.DemandId).Select(d => d.Code).Single()).ToListAsync(ct)); }

    private async Task AddLinks(Expense expense, ExpenseRequest request, CancellationToken ct)
    {
        foreach (var code in DistinctCodes(request.AssetCodes))
        {
            var asset = await Db.Assets.SingleOrDefaultAsync(x => x.Code == code, ct)
                ?? throw AppException.NotFound("asset");
            if (!await AssetMatchesScope(asset, expense.BuildingId, expense.ComplexId, ct))
                throw Validation("assetCodes", "Linked Asset must be inside the Expense scope.");
            Db.ExpenseAssets.Add(new ExpenseAsset(expense.Id, asset.Id));
        }
        foreach (var link in request.AssetEvents ?? [])
        {
            var asset = await Db.Assets.SingleOrDefaultAsync(x => x.Code == Code(link.AssetCode), ct)
                ?? throw AppException.NotFound("asset");
            if (!await AssetMatchesScope(asset, expense.BuildingId, expense.ComplexId, ct))
                throw Validation("assetEvents", "Linked AssetEvent must be inside the Expense scope.");
            var eventId = await Db.AssetEvents.Where(x => x.Id == link.EventId && x.AssetId == asset.Id)
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("asset_event");
            Db.ExpenseAssetEvents.Add(new ExpenseAssetEvent(expense.Id, eventId));
        }
        foreach (var code in DistinctCodes(request.RelatedBuildingCodes))
        {
            var building = await Db.Buildings.Where(x => x.Code == code)
                .Select(x => new { x.Id, x.ComplexId }).SingleOrDefaultAsync(ct)
                ?? throw AppException.NotFound("building");
            if (expense.BuildingId.HasValue && building.Id != expense.BuildingId ||
                expense.ComplexId.HasValue && building.ComplexId != expense.ComplexId)
                throw Validation("relatedBuildingCodes", "Linked Building must be inside the Expense scope.");
            Db.ExpenseBuildings.Add(new ExpenseBuilding(expense.Id, building.Id));
        }
        foreach (var code in DistinctCodes(request.RelatedComplexCodes))
        {
            var complexId = await Db.Complexes.Where(x => x.Code == code).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
            if (expense.ComplexId != complexId)
                throw Validation("relatedComplexCodes", "Linked Complex must match the Expense scope.");
            Db.ExpenseComplexes.Add(new ExpenseComplex(expense.Id, complexId));
        }
    }

    private async Task<bool> AssetMatchesScope(Asset asset, long? buildingId, long? complexId,
        CancellationToken ct) => buildingId.HasValue
        ? asset.BuildingId == buildingId
        : asset.ComplexId == complexId || asset.BuildingId.HasValue &&
          await Db.Buildings.AnyAsync(x => x.Id == asset.BuildingId && x.ComplexId == complexId, ct);

    private static IEnumerable<string> DistinctCodes(IReadOnlyList<string>? codes) =>
        (codes ?? []).Select(Code).Distinct(StringComparer.OrdinalIgnoreCase);

    private IQueryable<ExpenseType> AvailableTypes(long? buildingId, long? complexId)
    {
        var types = Db.ExpenseTypes.Where(x => x.IsActive);
        if (buildingId.HasValue)
            return types.Where(x => x.BuildingId == null && x.ComplexId == null ||
                x.BuildingId == buildingId || complexId.HasValue && x.ComplexId == complexId);
        if (complexId.HasValue)
            return types.Where(x => x.BuildingId == null && x.ComplexId == null || x.ComplexId == complexId);
        return types.Where(x => x.BuildingId == null && x.ComplexId == null);
    }
}
