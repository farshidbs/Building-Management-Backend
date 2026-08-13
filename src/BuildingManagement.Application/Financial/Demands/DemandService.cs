using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class DemandService(IApplicationDbContext db, TimeProvider clock) : FinancialServiceBase(db, clock)
{
    public async Task<Page<DemandResponse>> List(PageQuery query, string? status, CancellationToken ct)
    {
        var (pageNumber, pageSize) = query.Validated();
        var demands = Db.Demands.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) demands = demands.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(query.Search)) demands = demands.Where(x => x.Title.Contains(query.Search));
        var total = await demands.CountAsync(ct);
        var items = await demands.OrderByDescending(x => x.DemandDate)
            .Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(x => new DemandResponse(x.Code, x.Title, x.Status, x.DemandDate, x.DueDate, null))
            .ToListAsync(ct);
        return new(items, pageNumber, pageSize, total);
    }

    public async Task<DemandResponse> Get(string code, CancellationToken ct)
    {
        var demand = await Db.Demands.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("demand");
        var allocations = demand.Status == FinancialKeys.Statuses.Finalized
            ? await FinalizedPreview(demand.Id, ct)
            : null;
        return new(demand.Code, demand.Title, demand.Status, demand.DemandDate, demand.DueDate, allocations);
    }

    public async Task<DemandResponse> Create(DemandRequest request, CancellationToken ct) =>
        await Db.ExecuteInTransaction<DemandResponse>(async token =>
        {
            if (request is null) throw Validation("request", "Required.");
            if (request.Rule is null) throw Validation("rule", "Required.");
            var fund = await FundAccount(request.FundBuildingCode, request.FundComplexCode,
                request.FundAccountKindKey, token);
            if (fund.UnitId != null) throw Validation("fundAccount", "A building or complex fund is required.");
            var typeId = await Db.DemandTypes.Where(x => x.Key == request.DemandTypeKey && x.IsActive)
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("demand_type");
            var demand = new Demand(await Unique(Db.Demands, token), fund.Id, typeId, request.Title,
                request.Description, request.DemandDate, request.DueDate, Now);
            Db.Demands.Add(demand);
            await Save(token);
            Db.DemandAllocationRules.Add(new DemandAllocationRule(demand.Id, request.Rule.AllocationMethodKey,
                request.Rule.AmountModeKey, request.Rule.TotalAmount, request.Rule.RateAmount,
                request.Rule.IncludeVacantUnits, request.Rule.ResponsiblePartyTypeKey,
                request.Rule.RedistributionPolicyKey, request.Rule.Notes));
            await AddLinks(demand.Id, fund, request, token);
            await Save(token);
            return new(demand.Code, demand.Title, demand.Status, demand.DemandDate, demand.DueDate, null);
        }, ct);
    public async Task<DemandPreviewResponse> Preview(string code, DemandPreviewRequest request, CancellationToken ct) { var demand = await Db.Demands.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct) ?? throw AppException.NotFound("demand"); if (demand.Status != FinancialKeys.Statuses.Draft) throw AppException.Conflict("demand.preview_not_draft", "Preview is only available for a Draft Demand; read finalized allocations from Demand details."); var rule = await Db.DemandAllocationRules.AsNoTracking().SingleAsync(x => x.DemandId == demand.Id, ct); var dto = new DemandRuleRequest(rule.AllocationMethodKey, rule.AmountModeKey, rule.TotalAmount, rule.RateAmount, rule.IncludeVacantUnits, rule.ResponsiblePartyTypeKey, rule.RedistributionPolicyKey, rule.Notes); return DemandAllocationCalculator.Calculate(await Inputs(demand, rule, ct), dto, request?.Overrides); }
    public async Task<DemandResponse> Update(string code, DemandRequest request, CancellationToken ct)
    {
        var demand = await Db.Demands.SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("demand");
        if (demand.Status != FinancialKeys.Statuses.Draft)
            throw AppException.Conflict("demand.not_draft", "Only a Draft Demand can be updated.");
        var requestedFund = await FundAccount(request.FundBuildingCode, request.FundComplexCode,
            request.FundAccountKindKey, ct);
        if (requestedFund.Id != demand.FundAccountId)
            throw Validation("fundScope", "A Draft Demand cannot be moved to another Fund.");
        var requestedTypeId = await Db.DemandTypes.Where(x => x.Key == request.DemandTypeKey && x.IsActive)
            .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("demand_type");
        if (requestedTypeId != demand.DemandTypeId)
            throw Validation("demandTypeKey", "Demand type cannot be changed by this update.");
        demand.UpdateDraft(request.Title, request.Description, request.DemandDate, request.DueDate, Now);
        var rule = await Db.DemandAllocationRules.SingleAsync(x => x.DemandId == demand.Id, ct);
        rule.Update(request.Rule.AllocationMethodKey, request.Rule.AmountModeKey, request.Rule.TotalAmount,
            request.Rule.RateAmount, request.Rule.IncludeVacantUnits, request.Rule.ResponsiblePartyTypeKey,
            request.Rule.RedistributionPolicyKey, request.Rule.Notes);
        await Save(ct);
        return new(demand.Code, demand.Title, demand.Status, demand.DemandDate, demand.DueDate, null);
    }
    public async Task<DemandResponse> Finalize(string code, DemandPreviewRequest request, CancellationToken ct) =>
        await Db.ExecuteInTransaction<DemandResponse>(async token =>
        {
            var demand = await Db.Demands.SingleOrDefaultAsync(x => x.Code == Code(code), token)
                ?? throw AppException.NotFound("demand");
            if (await Db.FinancialTransactions.AnyAsync(x => x.DemandId == demand.Id, token))
                return new(demand.Code, demand.Title, demand.Status, demand.DemandDate, demand.DueDate,
                    await FinalizedPreview(demand.Id, token));

            var preview = await Preview(code, request, token);
            if (preview.FinalTotal <= 0) throw Validation("allocations", "Final total must be positive.");
            var fund = await Db.FinancialAccounts.SingleAsync(x => x.Id == demand.FundAccountId, token);
            var transaction = new FinancialTransaction(FinancialKeys.TransactionTypes.Demand, demand.Id,
                null, null, null, Now, demand.Title, Now);
            Db.FinancialTransactions.Add(transaction);
            demand.Finalize(Now);
            await Save(token);

            // Persist every preview row as the immutable final allocation snapshot. Only included,
            // positive rows create receivables and affect Unit balances.
            foreach (var item in preview.Items)
            {
                var unit = await Db.Units.SingleAsync(x => x.Code == Code(item.UnitCode), token);
                var partyId = string.IsNullOrWhiteSpace(item.ResponsiblePartyCode)
                    ? null
                    : await Db.Parties.Where(x => x.Code == Code(item.ResponsiblePartyCode))
                        .Select(x => (long?)x.Id).SingleOrDefaultAsync(token);
                var allocation = new DemandAllocation(demand.Id, unit.Id, item.ResponsiblePartyTypeKey,
                    partyId, item.BasisValue, item.CalculatedAmount, item.FinalAmount, item.IsIncluded,
                    item.AdjustmentReason);
                Db.DemandAllocations.Add(allocation);
                await Save(token);

                if (!item.IsIncluded || item.FinalAmount <= 0) continue;
                var unitAccount = await Db.FinancialAccounts.SingleOrDefaultAsync(
                    x => x.UnitId == unit.Id && x.AccountKindKey == FinancialKeys.AccountKinds.Unit, token);
                if (unitAccount is null)
                {
                    unitAccount = new FinancialAccount(unit.Id, null, null, FinancialKeys.AccountKinds.Unit, Now);
                    Db.FinancialAccounts.Add(unitAccount);
                    await Save(token);
                }
                Db.UnitReceivables.Add(new UnitReceivable(await Unique(Db.UnitReceivables, token), unitAccount.Id, fund.Id, allocation.Id, null,
                    item.FinalAmount, item.ResponsiblePartyTypeKey, partyId, demand.DueDate, Now));
                await Apply(unitAccount, FinancialKeys.Effects.Decrease, item.FinalAmount, transaction, token);
            }
            await Save(token);
            return new(demand.Code, demand.Title, demand.Status, demand.DemandDate, demand.DueDate, preview);
        }, ct);

    private async Task<DemandPreviewResponse> FinalizedPreview(long demandId, CancellationToken ct)
    {
        var items = await Db.DemandAllocations.AsNoTracking()
            .Where(x => x.DemandId == demandId)
            .OrderBy(x => x.UnitId)
            .Select(x => new DemandAllocationResponse(
                Db.Units.Where(u => u.Id == x.UnitId).Select(u => u.Code).Single(),
                x.BasisValue,
                x.CalculatedAmount,
                x.FinalAmount,
                x.IsIncluded,
                x.ResponsiblePartyTypeKey,
                x.ResponsiblePartyId == null ? null : Db.Parties.Where(p => p.Id == x.ResponsiblePartyId).Select(p => p.Code).Single(),
                x.AdjustmentReason))
            .ToListAsync(ct);
        var calculatedTotal = items.Sum(x => x.CalculatedAmount);
        var finalTotal = items.Sum(x => x.FinalAmount);
        return new(items, calculatedTotal, finalTotal, finalTotal - calculatedTotal);
    }
    private async Task<IReadOnlyList<AllocationInput>> Inputs(Demand demand, DemandAllocationRule rule, CancellationToken ct) { var fund = await Db.FinancialAccounts.AsNoTracking().SingleAsync(x => x.Id == demand.FundAccountId, ct); var units = Db.Units.AsNoTracking().Where(x => x.IsActive); if (fund.BuildingId is long buildingId) units = units.Where(x => x.BuildingId == buildingId); else if (fund.ComplexId is long complexId) units = units.Where(x => Db.Buildings.Any(b => b.Id == x.BuildingId && b.ComplexId == complexId)); var rows = await units.OrderBy(x => x.Id).Select(x => new { x.Id, x.Code, x.Area, x.CurrentOccupantsCount }).ToListAsync(ct); var result = new List<AllocationInput>(); foreach (var unit in rows) { var vacant = unit.CurrentOccupantsCount == 0; var eligible = rule.AllocationMethodKey == FinancialKeys.AllocationMethods.Occupants ? !vacant : (rule.IncludeVacantUnits ?? true) || !vacant; var basis = rule.AllocationMethodKey switch { FinancialKeys.AllocationMethods.Occupants => unit.CurrentOccupantsCount, FinancialKeys.AllocationMethods.Area => unit.Area ?? 0, _ => 1 }; var ownership = rule.ResponsiblePartyTypeKey == FinancialKeys.ResponsibleParties.Owner; var partyCode = await Db.UnitPartyRelations.AsNoTracking().Where(x => x.UnitId == unit.Id && x.IsActive && x.EndDate == null && Db.UnitPartyRelationTypes.Any(t => t.Id == x.UnitPartyRelationTypeId && (ownership ? t.IsOwnershipRelation : t.IsOccupancyRelation))).OrderBy(x => x.Id).Select(x => Db.Parties.Where(p => p.Id == x.PartyId).Select(p => p.Code).Single()).FirstOrDefaultAsync(ct); result.Add(new(unit.Code, basis, eligible, rule.ResponsiblePartyTypeKey, partyCode)); } return result; }

    private async Task AddLinks(long demandId, FinancialAccount fund, DemandRequest request, CancellationToken ct)
    {
        foreach (var link in request.Expenses ?? [])
        {
            var expense = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(link.ExpenseCode), ct)
                ?? throw AppException.NotFound("expense");
            EnsureScope(fund, expense.BuildingId, expense.ComplexId, "expenses");
            Db.DemandExpenses.Add(new DemandExpense(demandId, expense.Id, link.RelatedAmount));
        }
        foreach (var code in DistinctCodes(request.AssetCodes))
        {
            var asset = await Db.Assets.SingleOrDefaultAsync(x => x.Code == code, ct)
                ?? throw AppException.NotFound("asset");
            await EnsureAssetScope(fund, asset, ct);
            Db.DemandAssets.Add(new DemandAsset(demandId, asset.Id));
        }
        foreach (var link in request.AssetEvents ?? [])
        {
            var asset = await Db.Assets.SingleOrDefaultAsync(x => x.Code == Code(link.AssetCode), ct)
                ?? throw AppException.NotFound("asset");
            await EnsureAssetScope(fund, asset, ct);
            var eventId = await Db.AssetEvents.Where(x => x.Id == link.EventId && x.AssetId == asset.Id)
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("asset_event");
            Db.DemandAssetEvents.Add(new DemandAssetEvent(demandId, eventId));
        }
        foreach (var code in DistinctCodes(request.RelatedBuildingCodes))
        {
            var building = await Db.Buildings.Where(x => x.Code == code)
                .Select(x => new { x.Id, x.ComplexId }).SingleOrDefaultAsync(ct)
                ?? throw AppException.NotFound("building");
            EnsureScope(fund, building.Id, building.ComplexId, "relatedBuildingCodes");
            Db.DemandBuildings.Add(new DemandBuilding(demandId, building.Id));
        }
        foreach (var code in DistinctCodes(request.RelatedComplexCodes))
        {
            var complexId = await Db.Complexes.Where(x => x.Code == code).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
            EnsureScope(fund, null, complexId, "relatedComplexCodes");
            Db.DemandComplexes.Add(new DemandComplex(demandId, complexId));
        }
    }

    private async Task EnsureAssetScope(FinancialAccount fund, Asset asset, CancellationToken ct)
    {
        var complexId = asset.ComplexId ?? (asset.BuildingId.HasValue
            ? await Db.Buildings.Where(x => x.Id == asset.BuildingId).Select(x => x.ComplexId).SingleAsync(ct)
            : null);
        EnsureScope(fund, asset.BuildingId, complexId, "assetCodes");
    }

    private static void EnsureScope(FinancialAccount fund, long? buildingId, long? complexId, string field)
    {
        var matches = fund.BuildingId.HasValue ? buildingId == fund.BuildingId : complexId == fund.ComplexId;
        if (!matches) throw Validation(field, "Linked object must be inside the Demand fund scope.");
    }

    private static IEnumerable<string> DistinctCodes(IReadOnlyList<string>? codes) =>
        (codes ?? []).Select(Code).Distinct(StringComparer.OrdinalIgnoreCase);
}
