using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class ExpenseService(IApplicationDbContext db, TimeProvider clock) : FinancialServiceBase(db, clock)
{
    public async Task<Page<ExpenseResponse>> List(PageQuery query, string? status, CancellationToken ct)
    {
        var (pageNumber, pageSize) = query.Validated();
        var expenses = Db.Expenses.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) expenses = expenses.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search)) expenses = expenses.Where(x => x.Title.Contains(query.Search));
        var total = await expenses.CountAsync(ct);
        var rows = await expenses.OrderByDescending(x => x.ExpenseDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var items = new List<ExpenseResponse>(rows.Count);
        foreach (var row in rows) items.Add(await Response(row, ct));
        return new(items, pageNumber, pageSize, total);
    }

    public async Task<ExpenseResponse> Get(string code, CancellationToken ct)
    {
        var expense = await Db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("expense");
        return await Response(expense, ct);
    }

    public async Task<ExpenseTypeResponse> CreateType(ExpenseTypeRequest r, CancellationToken ct)
    {
        var building = string.IsNullOrWhiteSpace(r.BuildingCode) ? (long?)null : await Db.Buildings.Where(x => x.Code == Code(r.BuildingCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        var complex = string.IsNullOrWhiteSpace(r.ComplexCode) ? (long?)null : await Db.Complexes.Where(x => x.Code == Code(r.ComplexCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
        if (!building.HasValue && !complex.HasValue) throw Validation("scope", "Custom expense type requires a scope.");
        var type = new ExpenseType(null, r.Title, complex, building, r.SortOrder, Now); Db.ExpenseTypes.Add(type); await Save(ct);
        return new(type.Id, type.Key, type.Title, r.BuildingCode, r.ComplexCode, type.IsActive);
    }

    public async Task<IReadOnlyList<ExpenseTypeResponse>> Types(string? buildingCode, string? complexCode, CancellationToken ct)
    {
        long? building = null, complex = null;
        if (!string.IsNullOrWhiteSpace(buildingCode)) { var row = await Db.Buildings.Where(x => x.Code == Code(buildingCode)).Select(x => new { x.Id, x.ComplexId }).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building"); building = row.Id; complex = row.ComplexId; }
        else if (!string.IsNullOrWhiteSpace(complexCode)) complex = await Db.Complexes.Where(x => x.Code == Code(complexCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
        return await Db.ExpenseTypes.AsNoTracking().Where(x => x.IsActive && ((x.BuildingId == null && x.ComplexId == null) || x.BuildingId == building || x.ComplexId == complex)).OrderBy(x => x.SortOrder).ThenBy(x => x.Title).Select(x => new ExpenseTypeResponse(x.Id, x.Key, x.Title, x.BuildingId == null ? null : Db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => b.Code).Single(), x.ComplexId == null ? null : Db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => c.Code).Single(), x.IsActive)).ToListAsync(ct);
    }
    public async Task<ExpenseResponse> Create(ExpenseRequest request, CancellationToken ct) =>
        await Db.ExecuteInTransaction(async token =>
        {
            if (request is null) throw Validation("request", "Required.");
            long? buildingId = string.IsNullOrWhiteSpace(request.BuildingCode)
                ? null
                : await Db.Buildings.Where(x => x.Code == Code(request.BuildingCode))
                    .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("building");
            long? complexId = string.IsNullOrWhiteSpace(request.ComplexCode)
                ? null
                : await Db.Complexes.Where(x => x.Code == Code(request.ComplexCode))
                    .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("complex");
            var typeId = await Db.ExpenseTypes.Where(x => x.Id == request.ExpenseTypeId && x.IsActive &&
                    (x.BuildingId == null || x.BuildingId == buildingId) &&
                    (x.ComplexId == null || x.ComplexId == complexId))
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
    public async Task<ExpenseResponse> Finalize(string code, CancellationToken ct) { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(code), ct) ?? throw AppException.NotFound("expense"); e.Finalize(Now); await Save(ct); return await Response(e, ct); }
    public async Task<ExpenseDisbursementResponse> CreateDisbursement(string expenseCode, ExpenseDisbursementRequest r, CancellationToken ct) { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(expenseCode), ct) ?? throw AppException.NotFound("expense"); if (e.Status != FinancialKeys.Statuses.Finalized) throw AppException.Conflict("expense.not_finalized", "Expense must be finalized."); var fund = await FundAccount(r.FundBuildingCode, r.FundComplexCode, r.FundAccountKindKey, ct); if ((e.BuildingId != null && fund.BuildingId != e.BuildingId) || (e.ComplexId != null && fund.ComplexId != e.ComplexId)) throw Validation("fundAccount", "Fund must belong to the expense scope."); var payee = string.IsNullOrWhiteSpace(r.PayeePartyCode) ? (long?)null : await Db.Parties.Where(x => x.Code == Code(r.PayeePartyCode)).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party"); var d = new ExpenseDisbursement(await Unique(Db.ExpenseDisbursements, ct), e.Id, fund.Id, payee, r.Amount, r.PaymentMethodKey, r.Notes, Now); Db.ExpenseDisbursements.Add(d); await Save(ct); return new(d.Code, e.Code, d.Amount, d.Status, d.PaidAtUtc); }
    public async Task<ExpenseDisbursementResponse> FinalizeDisbursement(string expenseCode, string disbursementCode, CancellationToken ct) => await Db.ExecuteInTransaction<ExpenseDisbursementResponse>(async token => { var e = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(expenseCode), token) ?? throw AppException.NotFound("expense"); var d = await Db.ExpenseDisbursements.SingleOrDefaultAsync(x => x.Code == Code(disbursementCode) && x.ExpenseId == e.Id, token) ?? throw AppException.NotFound("expense_disbursement"); if (await Db.FinancialTransactions.AnyAsync(x => x.ExpenseDisbursementId == d.Id, token)) return new(d.Code, e.Code, d.Amount, d.Status, d.PaidAtUtc); var alreadyPaid = await Db.ExpenseDisbursements.Where(x => x.ExpenseId == e.Id && x.Status == FinancialKeys.Statuses.Finalized).SumAsync(x => (decimal?)x.Amount, token) ?? 0; if (alreadyPaid + d.Amount > e.Amount) throw AppException.Conflict("expense.over_disbursement", "Disbursements cannot exceed expense amount."); var fund = await Db.FinancialAccounts.SingleAsync(x => x.Id == d.FundAccountId, token); d.Finalize(Now, Now); var tx = new FinancialTransaction(FinancialKeys.TransactionTypes.ExpenseDisbursement, null, null, d.Id, null, Now, $"Expense {e.Code}", Now); Db.FinancialTransactions.Add(tx); await Apply(fund, FinancialKeys.Effects.Decrease, d.Amount, tx, token); await Save(token); return new(d.Code, e.Code, d.Amount, d.Status, d.PaidAtUtc); }, ct);
    private async Task<ExpenseResponse> Response(Expense e, CancellationToken ct) { var paid = await Db.ExpenseDisbursements.Where(x => x.ExpenseId == e.Id && x.Status == FinancialKeys.Statuses.Finalized).SumAsync(x => (decimal?)x.Amount, ct) ?? 0; return new(e.Code, e.Title, e.Amount, paid, Math.Max(0, e.Amount - paid), e.Status, e.ExpenseDate, e.DueDate); }

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
}
