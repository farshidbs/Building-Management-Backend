using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public interface ITrustedPaymentResultProcessor
{
    Task<PaymentResponse> ProcessSuccessfulPayment(string paymentCode, string providerReference,
        DateTimeOffset paidAtUtc, CancellationToken ct);
}

public sealed class PaymentService(IApplicationDbContext db, TimeProvider clock)
    : FinancialServiceBase(db, clock), ITrustedPaymentResultProcessor
{
    public async Task<PaymentResponse> Get(string code, CancellationToken ct) =>
        await Response(await Db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("payment"), ct);

    public Task<PaymentResponse> Create(PaymentRequest request, CancellationToken ct) =>
        Db.ExecuteInTransaction(async token =>
        {
            Validate(request);
            var unitId = await Db.Units.Where(x => x.Code == Code(request.UnitCode))
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("unit");
            var unitAccount = await UnitAccount(request.UnitCode, token);
            var fund = await FundAccount(request.FundBuildingCode, request.FundComplexCode,
                request.FundAccountKindKey, token);
            if (!await FundMatchesUnit(fund, unitId, token))
                throw Validation("fundScope", "Fund must contain the payment Unit.");
            long? payerId = string.IsNullOrWhiteSpace(request.PayerPartyCode) ? null :
                await Db.Parties.Where(x => x.Code == Code(request.PayerPartyCode))
                    .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("party");
            var method = await Db.PaymentMethods.SingleOrDefaultAsync(
                x => x.Key == request.PaymentMethodKey && x.IsActive, token)
                ?? throw AppException.NotFound("payment_method");
            var payment = new Payment(await Unique(Db.Payments, token), unitAccount.Id, fund.Id, payerId,
                method.Id, request.Amount, null, request.BankTrackingCode, request.Notes, Now);
            Db.Payments.Add(payment);
            await Save(token);
            foreach (var item in request.Allocations)
            {
                var receivableCode = Code(item.ReceivableCode);
                var receivable = await Db.UnitReceivables.SingleOrDefaultAsync(x => x.Code == receivableCode &&
                    x.UnitAccountId == unitAccount.Id && x.FundAccountId == fund.Id && x.IsActive, token)
                    ?? throw AppException.NotFound("unit_receivable");
                if (item.Amount > receivable.OutstandingAmount)
                    throw Validation("allocations", "Allocation exceeds outstanding amount.");
                Db.PaymentAllocations.Add(new PaymentAllocation(payment.Id, receivable.Id, item.Amount));
            }
            payment.Submit(method.RequiresManagerApproval, Now);
            await Save(token);
            return await Response(payment, token);
        }, ct);

    public Task<PaymentResponse> ConfirmManual(string code, CancellationToken ct) =>
        ApplyConfirmation(code, null, null, false, ct);

    Task<PaymentResponse> ITrustedPaymentResultProcessor.ProcessSuccessfulPayment(string paymentCode,
        string providerReference, DateTimeOffset paidAtUtc, CancellationToken ct) =>
        ApplyConfirmation(paymentCode, providerReference, paidAtUtc, true, ct);

    public async Task<PaymentResponse> Reject(string code, CancellationToken ct)
    {
        var payment = await Db.Payments.SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("payment");
        payment.Reject(Now);
        await Save(ct);
        return await Response(payment, ct);
    }

    public async Task<IReadOnlyList<ReceivableResponse>> Receivables(string unitCode, CancellationToken ct)
    {
        var normalized = Code(unitCode);
        return await Db.UnitReceivables.AsNoTracking().Where(x => x.IsActive && x.OutstandingAmount > 0 &&
                Db.FinancialAccounts.Any(a => a.Id == x.UnitAccountId && a.UnitId != null &&
                    Db.Units.Any(u => u.Id == a.UnitId && u.Code == normalized)))
            .OrderBy(x => x.DueDate).Select(x => new ReceivableResponse(x.Code,
                x.DemandAllocationId == null ? null : Db.DemandAllocations.Where(a => a.Id == x.DemandAllocationId)
                    .Select(a => Db.Demands.Where(d => d.Id == a.DemandId).Select(d => d.Code).Single()).Single(),
                x.AccountAdjustmentId == null ? null : Db.AccountAdjustments
                    .Where(a => a.Id == x.AccountAdjustmentId).Select(a => a.Code).Single(),
                normalized, x.OriginalAmount, x.OutstandingAmount, x.Status, x.ResponsiblePartyTypeKey,
                x.ResponsiblePartyId == null ? null : Db.Parties.Where(p => p.Id == x.ResponsiblePartyId)
                    .Select(p => p.Code).Single(), x.DueDate)).ToListAsync(ct);
    }

    private Task<PaymentResponse> ApplyConfirmation(string code, string? providerReference,
        DateTimeOffset? paidAtUtc, bool trustedGateway, CancellationToken ct) =>
        Db.ExecuteInTransaction(async token =>
        {
            var payment = await Db.Payments.SingleOrDefaultAsync(x => x.Code == Code(code), token)
                ?? throw AppException.NotFound("payment");
            if (await Db.FinancialTransactions.AnyAsync(x => x.PaymentId == payment.Id, token))
                return await Response(payment, token);
            if (trustedGateway) payment.ConfirmGateway(providerReference!, paidAtUtc!.Value, Now);
            else payment.ConfirmManual(Now, Now);
            var allocations = await Db.PaymentAllocations.Where(x => x.PaymentId == payment.Id).ToListAsync(token);
            foreach (var allocation in allocations)
            {
                var receivable = await Db.UnitReceivables.SingleAsync(x => x.Id == allocation.UnitReceivableId, token);
                if (allocation.Amount > receivable.OutstandingAmount)
                    throw AppException.Conflict("payment.receivable_changed", "Receivable balance changed.");
                receivable.ApplyPayment(allocation.Amount, Now);
            }
            var unit = await Db.FinancialAccounts.SingleAsync(x => x.Id == payment.UnitAccountId, token);
            var fund = await Db.FinancialAccounts.SingleAsync(x => x.Id == payment.ReceivingFundAccountId, token);
            var transaction = new FinancialTransaction(FinancialKeys.TransactionTypes.Payment, null,
                payment.Id, null, null, paidAtUtc ?? Now, $"Payment {payment.Code}", Now);
            Db.FinancialTransactions.Add(transaction);
            await Apply(unit, FinancialKeys.Effects.Increase, payment.Amount, transaction, token);
            await Apply(fund, FinancialKeys.Effects.Increase, payment.Amount, transaction, token);
            await Save(token);
            return await Response(payment, token);
        }, ct);

    private static void Validate(PaymentRequest request)
    {
        if (request is null) throw Validation("request", "Required.");
        if (request.Allocations is null) throw Validation("allocations", "Required.");
        if (request.Allocations.GroupBy(x => x.ReceivableCode, StringComparer.OrdinalIgnoreCase)
            .Any(x => x.Count() > 1)) throw Validation("allocations", "Each Receivable may be allocated once.");
        if (request.Allocations.Any(x => x.Amount <= 0))
            throw Validation("allocations", "Allocation amounts must be positive.");
        if (request.Allocations.Sum(x => x.Amount) > request.Amount)
            throw Validation("allocations", "Allocation total cannot exceed Payment amount.");
    }

    private async Task<bool> FundMatchesUnit(FinancialAccount fund, long unitId, CancellationToken ct) =>
        fund.BuildingId != null
            ? await Db.Units.AnyAsync(x => x.Id == unitId && x.BuildingId == fund.BuildingId, ct)
            : await Db.Units.AnyAsync(x => x.Id == unitId && Db.Buildings.Any(b =>
                b.Id == x.BuildingId && b.ComplexId == fund.ComplexId), ct);

    private async Task<PaymentResponse> Response(Payment payment, CancellationToken ct) =>
        new(payment.Code, payment.Amount, payment.Status, payment.PayerPartyId == null ? null :
            await Db.Parties.Where(x => x.Id == payment.PayerPartyId).Select(x => x.Code).SingleAsync(ct),
            payment.ConfirmedAtUtc);
}
