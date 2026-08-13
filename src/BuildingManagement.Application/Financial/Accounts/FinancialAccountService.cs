using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class FinancialAccountService(IApplicationDbContext db, TimeProvider clock)
    : FinancialServiceBase(db, clock)
{
    public async Task<FinancialAccountResponse> Create(FinancialAccountRequest request, CancellationToken ct)
    {
        if (request is null) throw Validation("request", "Required.");
        var owners = (string.IsNullOrWhiteSpace(request.UnitCode) ? 0 : 1) +
                     (string.IsNullOrWhiteSpace(request.BuildingCode) ? 0 : 1) +
                     (string.IsNullOrWhiteSpace(request.ComplexCode) ? 0 : 1);
        if (owners != 1) throw Validation("owner", "Exactly one owner code is required.");

        long? unitId = null, buildingId = null, complexId = null;
        string ownerCode;
        if (!string.IsNullOrWhiteSpace(request.UnitCode))
        {
            ownerCode = Code(request.UnitCode);
            unitId = await Db.Units.Where(x => x.Code == ownerCode).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("unit");
        }
        else if (!string.IsNullOrWhiteSpace(request.BuildingCode))
        {
            ownerCode = Code(request.BuildingCode);
            buildingId = await Db.Buildings.Where(x => x.Code == ownerCode).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("building");
        }
        else
        {
            ownerCode = Code(request.ComplexCode!);
            complexId = await Db.Complexes.Where(x => x.Code == ownerCode).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("complex");
        }

        var account = new FinancialAccount(unitId, buildingId, complexId, request.AccountKindKey, Now);
        Db.FinancialAccounts.Add(account);
        await Save(ct);
        return new(ownerCode, account.AccountKindKey, account.CurrentBalance, account.IsActive);
    }

    public async Task<FinancialAccountResponse> GetUnit(string unitCode, CancellationToken ct)
    { var account = await UnitAccount(unitCode, ct); return Response(Code(unitCode), account); }

    public async Task<FinancialAccountResponse> GetFund(string? buildingCode, string? complexCode,
        string kind, CancellationToken ct)
    { var account = await FundAccount(buildingCode, complexCode, kind, ct); return Response(Code(buildingCode ?? complexCode!), account); }

    public async Task<IReadOnlyList<FinancialEntryResponse>> UnitStatement(string unitCode, CancellationToken ct) =>
        await Statement(await UnitAccount(unitCode, ct), ct);

    public async Task<IReadOnlyList<FinancialEntryResponse>> FundStatement(string? buildingCode,
        string? complexCode, string kind, CancellationToken ct) =>
        await Statement(await FundAccount(buildingCode, complexCode, kind, ct), ct);

    public async Task<AccountAdjustmentResponse> CreateAdjustment(AccountAdjustmentRequest request,
        CancellationToken ct)
    {
        if (request is null) throw Validation("request", "Required.");
        if (request.AdjustmentTypeKey is not (FinancialKeys.Adjustments.OpeningDebt or FinancialKeys.Adjustments.OpeningCredit))
            throw Validation("adjustmentTypeKey", "Only opening debt and opening credit are supported.");
        var accountOwners = (string.IsNullOrWhiteSpace(request.AccountUnitCode) ? 0 : 1) +
                            (string.IsNullOrWhiteSpace(request.AccountBuildingCode) ? 0 : 1) +
                            (string.IsNullOrWhiteSpace(request.AccountComplexCode) ? 0 : 1);
        if (accountOwners != 1) throw Validation("accountScope", "Exactly one adjusted account scope is required.");
        var adjustedAccount = !string.IsNullOrWhiteSpace(request.AccountUnitCode)
            ? await UnitAccount(request.AccountUnitCode, ct)
            : await FundAccount(request.AccountBuildingCode, request.AccountComplexCode, request.AccountKindKey, ct);
        if (request.AdjustmentTypeKey == FinancialKeys.Adjustments.OpeningDebt && adjustedAccount.UnitId is null)
            throw Validation("accountScope", "Opening debt requires a Unit account.");
        FinancialAccount? fund = null;
        if (request.AdjustmentTypeKey == FinancialKeys.Adjustments.OpeningDebt)
            fund = await FundAccount(request.FundBuildingCode, request.FundComplexCode,
                request.FundAccountKindKey ?? "", ct);
        if (fund is not null && !await FundMatchesUnit(fund, adjustedAccount.UnitId!.Value, ct))
            throw Validation("fundScope", "Destination Fund must contain the Unit.");
        long? responsiblePartyId = string.IsNullOrWhiteSpace(request.ResponsiblePartyCode) ? null :
            await Db.Parties.Where(x => x.Code == Code(request.ResponsiblePartyCode)).Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("party");
        var adjustment = new AccountAdjustment(await Unique(Db.AccountAdjustments, ct), adjustedAccount.Id,
            fund?.Id, request.AdjustmentTypeKey, request.Amount, request.EffectiveDate, request.Reason,
            request.ResponsiblePartyTypeKey, responsiblePartyId, Now);
        Db.AccountAdjustments.Add(adjustment);
        await Save(ct);
        return new(adjustment.Code, adjustment.Status, adjustment.Amount, adjustment.AdjustmentTypeKey);
    }

    public Task<AccountAdjustmentResponse> FinalizeAdjustment(string code, CancellationToken ct) =>
        Db.ExecuteInTransaction<AccountAdjustmentResponse>(async token =>
        {
            var adjustment = await Db.AccountAdjustments.SingleOrDefaultAsync(x => x.Code == Code(code), token)
                ?? throw AppException.NotFound("account_adjustment");
            if (await Db.FinancialTransactions.AnyAsync(x => x.AccountAdjustmentId == adjustment.Id, token))
                return new(adjustment.Code, adjustment.Status, adjustment.Amount, adjustment.AdjustmentTypeKey);
            var account = await Db.FinancialAccounts.SingleAsync(x => x.Id == adjustment.FinancialAccountId, token);
            var effect = adjustment.AdjustmentTypeKey == FinancialKeys.Adjustments.OpeningDebt
                ? FinancialKeys.Effects.Decrease : FinancialKeys.Effects.Increase;
            adjustment.Finalize(Now);
            var transaction = new FinancialTransaction(FinancialKeys.TransactionTypes.AccountAdjustment,
                null, null, null, adjustment.Id, adjustment.EffectiveDate, adjustment.Reason, Now);
            Db.FinancialTransactions.Add(transaction);
            await Apply(account, effect, adjustment.Amount, transaction, token);
            if (adjustment.AdjustmentTypeKey == FinancialKeys.Adjustments.OpeningDebt)
            {
                var receivable = new UnitReceivable(await Unique(Db.UnitReceivables, token), account.Id,
                    adjustment.FundAccountId!.Value, null, adjustment.Id, adjustment.Amount,
                    adjustment.ResponsiblePartyTypeKey ?? FinancialKeys.ResponsibleParties.Owner,
                    adjustment.ResponsiblePartyId, null, Now);
                Db.UnitReceivables.Add(receivable);
            }
            await Save(token);
            return new(adjustment.Code, adjustment.Status, adjustment.Amount, adjustment.AdjustmentTypeKey);
        }, ct);

    private async Task<IReadOnlyList<FinancialEntryResponse>> Statement(FinancialAccount account,
        CancellationToken ct) => await Db.FinancialTransactionEntries.AsNoTracking()
        .Where(x => x.FinancialAccountId == account.Id).OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new FinancialEntryResponse(
            Db.FinancialTransactions.Where(t => t.Id == x.FinancialTransactionId)
                .Select(t => t.TransactionTypeKey).Single(), x.EffectKey, x.Amount, x.BalanceAfter,
            Db.FinancialTransactions.Where(t => t.Id == x.FinancialTransactionId)
                .Select(t => t.OccurredAtUtc).Single(),
            Db.FinancialTransactions.Where(t => t.Id == x.FinancialTransactionId)
                .Select(t => t.Description).Single())).ToListAsync(ct);

    private async Task<bool> FundMatchesUnit(FinancialAccount fund, long unitId, CancellationToken ct) =>
        fund.BuildingId != null
            ? await Db.Units.AnyAsync(x => x.Id == unitId && x.BuildingId == fund.BuildingId, ct)
            : await Db.Units.AnyAsync(x => x.Id == unitId && Db.Buildings.Any(b =>
                b.Id == x.BuildingId && b.ComplexId == fund.ComplexId), ct);

    private static FinancialAccountResponse Response(string code, FinancialAccount account) =>
        new(code, account.AccountKindKey, account.CurrentBalance, account.IsActive);
}
