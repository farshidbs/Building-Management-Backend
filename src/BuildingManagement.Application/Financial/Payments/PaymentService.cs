using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public sealed class PaymentService(IApplicationDbContext db, TimeProvider clock) : FinancialServiceBase(db, clock)
{
    public async Task<PaymentResponse> Get(string code, CancellationToken ct)
    {
        var payment = await Db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("payment");
        return await Response(payment, ct);
    }

    public async Task<PaymentResponse> ConfirmGateway(string gatewayReference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(gatewayReference)) throw Validation("gatewayReference", "Required.");
        var code = await Db.Payments.AsNoTracking()
            .Where(x => x.GatewayReference == gatewayReference.Trim())
            .Select(x => x.Code)
            .SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("payment");
        return await Confirm(code, ct);
    }

    public async Task<PaymentResponse> Create(PaymentRequest request, CancellationToken ct) =>
        await Db.ExecuteInTransaction(async token =>
        {
            if (request is null) throw Validation("request", "Required.");
            if (request.Allocations is null) throw Validation("allocations", "Required.");
            if (request.Allocations.GroupBy(x => x.DemandCode, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1))
                throw Validation("allocations", "Each receivable may be allocated once per payment.");
            if (request.Allocations.Any(x => x.Amount <= 0))
                throw Validation("allocations", "Allocation amounts must be positive.");
            if (request.Allocations.Sum(x => x.Amount) > request.Amount)
                throw Validation("allocations", "Allocation total cannot exceed payment amount.");

            var unitId = await Db.Units.Where(x => x.Code == Code(request.UnitCode))
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("unit");
            var unitAccount = await Db.FinancialAccounts.SingleOrDefaultAsync(
                x => x.UnitId == unitId && x.AccountKindKey == FinancialKeys.AccountKinds.Unit, token)
                ?? throw AppException.NotFound("unit_financial_account");
            var fund = await AccountByOwner(request.FundOwnerCode, request.FundAccountKindKey, token);
            if (fund.UnitId != null || !await FundMatchesUnit(fund, unitId, token))
                throw Validation("fundAccount", "Fund must contain the payment unit.");
            long? payerId = string.IsNullOrWhiteSpace(request.PayerPartyCode)
                ? null
                : await Db.Parties.Where(x => x.Code == Code(request.PayerPartyCode))
                    .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("party");
            var method = await Db.PaymentMethods.SingleOrDefaultAsync(
                x => x.Key == request.PaymentMethodKey && x.IsActive, token)
                ?? throw AppException.NotFound("payment_method");

            var payment = new Payment(await Unique(Db.Payments, token), unitAccount.Id, fund.Id, payerId,
                method.Id, request.Amount, request.GatewayReference, request.BankTrackingCode, request.Notes, Now);
            Db.Payments.Add(payment);
            await Save(token);
            foreach (var item in request.Allocations)
            {
                var demandCode = Code(item.DemandCode);
                var receivable = await Db.UnitReceivables.SingleOrDefaultAsync(x =>
                    x.UnitAccountId == unitAccount.Id && x.FundAccountId == fund.Id && x.IsActive &&
                    x.DemandAllocationId != null && Db.DemandAllocations.Any(a =>
                        a.Id == x.DemandAllocationId && Db.Demands.Any(d => d.Id == a.DemandId && d.Code == demandCode)),
                    token) ?? throw AppException.NotFound("unit_receivable");
                if (item.Amount > receivable.OutstandingAmount)
                    throw Validation("allocations", "Allocation exceeds outstanding amount.");
                Db.PaymentAllocations.Add(new PaymentAllocation(payment.Id, receivable.Id, item.Amount));
            }
            payment.Submit(method.RequiresManagerApproval, Now);
            await Save(token);
            return await Response(payment, token);
        }, ct);
    public async Task<PaymentResponse> Confirm(string code, CancellationToken ct) => await Db.ExecuteInTransaction(async token => { var payment = await Db.Payments.SingleOrDefaultAsync(x => x.Code == Code(code), token) ?? throw AppException.NotFound("payment"); if (await Db.FinancialTransactions.AnyAsync(x => x.PaymentId == payment.Id, token)) return await Response(payment, token); var allocations = await Db.PaymentAllocations.Where(x => x.PaymentId == payment.Id).ToListAsync(token); var allocated = allocations.Sum(x => x.Amount); if (allocated > payment.Amount) throw AppException.Conflict("payment.allocation_exceeded", "Allocations exceed payment amount."); foreach (var allocation in allocations) { var receivable = await Db.UnitReceivables.SingleAsync(x => x.Id == allocation.UnitReceivableId, token); if (allocation.Amount > receivable.OutstandingAmount) throw AppException.Conflict("payment.receivable_changed", "Receivable outstanding amount changed."); receivable.ApplyPayment(allocation.Amount, Now); } var unit = await Db.FinancialAccounts.SingleAsync(x => x.Id == payment.UnitAccountId, token); var fund = await Db.FinancialAccounts.SingleAsync(x => x.Id == payment.ReceivingFundAccountId, token); payment.Confirm(Now, Now); var tx = new FinancialTransaction(FinancialKeys.TransactionTypes.Payment, null, payment.Id, null, null, Now, $"Payment {payment.Code}", Now); Db.FinancialTransactions.Add(tx); await Apply(unit, FinancialKeys.Effects.Increase, payment.Amount, tx, token); await Apply(fund, FinancialKeys.Effects.Increase, payment.Amount, tx, token); await Save(token); return await Response(payment, token); }, ct);
    public async Task<PaymentResponse> Reject(string code, CancellationToken ct) { var payment = await Db.Payments.SingleOrDefaultAsync(x => x.Code == Code(code), ct) ?? throw AppException.NotFound("payment"); payment.Reject(Now); await Save(ct); return await Response(payment, ct); }
    public async Task<IReadOnlyList<ReceivableResponse>> Receivables(string unitCode, CancellationToken ct) { var normalized = Code(unitCode); return await Db.UnitReceivables.AsNoTracking().Where(x => x.IsActive && x.OutstandingAmount > 0 && x.DemandAllocationId != null && Db.FinancialAccounts.Any(a => a.Id == x.UnitAccountId && a.UnitId != null && Db.Units.Any(u => u.Id == a.UnitId && u.Code == normalized))).OrderBy(x => x.DueDate).Select(x => new ReceivableResponse(Db.DemandAllocations.Where(a => a.Id == x.DemandAllocationId).Select(a => Db.Demands.Where(d => d.Id == a.DemandId).Select(d => d.Code).Single()).Single(), normalized, x.OriginalAmount, x.OutstandingAmount, x.Status, x.ResponsiblePartyTypeKey, x.ResponsiblePartyId == null ? null : Db.Parties.Where(p => p.Id == x.ResponsiblePartyId).Select(p => p.Code).Single(), x.DueDate)).ToListAsync(ct); }
    private async Task<bool> FundMatchesUnit(FinancialAccount fund, long unitId, CancellationToken ct) => fund.BuildingId != null ? await Db.Units.AnyAsync(x => x.Id == unitId && x.BuildingId == fund.BuildingId, ct) : await Db.Units.AnyAsync(x => x.Id == unitId && Db.Buildings.Any(b => b.Id == x.BuildingId && b.ComplexId == fund.ComplexId), ct);
    private async Task<PaymentResponse> Response(Payment p, CancellationToken ct) => new(p.Code, p.Amount, p.Status, p.PayerPartyId == null ? null : await Db.Parties.Where(x => x.Id == p.PayerPartyId).Select(x => x.Code).SingleAsync(ct), p.ConfirmedAtUtc);
}
