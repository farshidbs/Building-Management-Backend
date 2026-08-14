using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace BuildingManagement.Application;

public interface ITrustedPaymentResultProcessor
{
    Task<PaymentResponse> ProcessSuccessfulPayment(string paymentCode, string providerReference,
        DateTimeOffset paidAtUtc, CancellationToken ct);
}

public sealed class PaymentService(IApplicationDbContext db, TimeProvider clock, ResourceAuthorization authorization)
    : FinancialServiceBase(db, clock, authorization), ITrustedPaymentResultProcessor
{
    public async Task<PaymentDetailResponse> Get(string code, CancellationToken ct)
    {
        var payment = await Db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(code), ct)
            ?? throw AppException.NotFound("payment");
        await AuthorizePayment(payment, "payment_view", ct);
        return await Detail(payment, ct);
    }

    public Task<PaymentResponse> Create(PaymentRequest request, CancellationToken ct) =>
        Db.ExecuteInTransaction(async token =>
        {
            Validate(request);
            var unitId = await Db.Units.Where(x => x.Code == Code(request.UnitCode))
                .Select(x => (long?)x.Id).SingleOrDefaultAsync(token) ?? throw AppException.NotFound("unit");
            var unitAccount = await UnitAccount(request.UnitCode, token);
            await Authorize(unitAccount, "payment_submit", token);
            var fund = await FundAccount(request.FundBuildingCode, request.FundComplexCode,
                request.FundAccountKindKey, token);
            if (!await FundMatchesUnit(fund, unitId, token))
                throw Validation("fundScope", "Fund must contain the payment Unit.");
            var payerId = await AuthorizedPartyId(request.PayerPartyCode, token);
            var method = await Db.PaymentMethods.SingleOrDefaultAsync(
                x => x.Key == request.PaymentMethodKey && x.IsActive, token)
                ?? throw AppException.NotFound("payment_method");
            if (method.Key is "card_to_card" or "bank_transfer" && string.IsNullOrWhiteSpace(request.BankTrackingCode))
                throw Validation("bankTrackingCode", "Bank tracking code is required for bank-originated offline payments.");
            var payment = new Payment(await Unique(Db.Payments, token), unitAccount.Id, fund.Id, payerId,
                method.Id, request.Amount, null, request.BankTrackingCode, request.PaidAtUtc, request.Notes, Now);
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
        await AuthorizePayment(payment, "payment_confirm", ct);
        payment.Reject(Now);
        await Save(ct);
        return await Response(payment, ct);
    }

    public async Task<IReadOnlyList<ReceivableResponse>> Receivables(string unitCode, CancellationToken ct)
    {
        var normalized = Code(unitCode);
        var account = await UnitAccount(unitCode, ct);
        await Authorize(account, "financial_unit_view_own", ct);
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
            if (!trustedGateway) await AuthorizePayment(payment, "payment_confirm", token);
            if (await Db.FinancialTransactions.AnyAsync(x => x.PaymentId == payment.Id, token))
                return await Response(payment, token);
            if (trustedGateway) payment.ConfirmGateway(providerReference!, paidAtUtc!.Value, Now);
            else payment.ConfirmManual(payment.PaidAtUtc ?? Now, Now);
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
                payment.Id, null, null, payment.PaidAtUtc ?? Now, $"Payment {payment.Code}", Now);
            Db.FinancialTransactions.Add(transaction);
            await Apply(unit, FinancialKeys.Effects.Increase, payment.Amount, transaction, token);
            var unappliedAmount = payment.Amount - allocations.Sum(x => x.Amount);
            if (unappliedAmount > 0) unit.AddAvailableCredit(unappliedAmount, Now);
            await Apply(fund, FinancialKeys.Effects.Increase, payment.Amount, transaction, token);
            await Save(token);
            return await Response(payment, token);
        }, ct);

    private async Task AuthorizePayment(Payment payment, string permission, CancellationToken ct)
    {
        var account = await Db.FinancialAccounts.AsNoTracking()
            .SingleAsync(x => x.Id == payment.UnitAccountId, ct);
        await Authorize(account, permission, ct);
    }

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

    public async Task<Page<PaymentDetailResponse>> History(string unitCode, PageQuery query, CancellationToken ct)
    { var (number, size) = query.Validated(); var account = await UnitAccount(unitCode, ct); await Authorize(account, "financial_unit_view_own", ct); var source = Db.Payments.AsNoTracking().Where(x => x.UnitAccountId == account.Id); var total = await source.CountAsync(ct); var rows = await source.OrderByDescending(x => x.PaidAtUtc ?? x.SubmittedAtUtc).ThenByDescending(x => x.Id).Skip((number - 1) * size).Take(size).ToListAsync(ct); var items = new List<PaymentDetailResponse>(); foreach (var row in rows) items.Add(await Detail(row, ct)); return new(items, number, size, total); }

    private async Task<PaymentDetailResponse> Detail(Payment payment, CancellationToken ct)
    { var unitCode = await Db.FinancialAccounts.Where(x => x.Id == payment.UnitAccountId).Select(x => Db.Units.Where(u => u.Id == x.UnitId).Select(u => u.Code).Single()).SingleAsync(ct); var fund = await Db.FinancialAccounts.Where(x => x.Id == payment.ReceivingFundAccountId).Select(x => new { x.AccountKindKey, OwnerCode = x.BuildingId != null ? Db.Buildings.Where(b => b.Id == x.BuildingId).Select(b => b.Code).Single() : Db.Complexes.Where(c => c.Id == x.ComplexId).Select(c => c.Code).Single() }).SingleAsync(ct); var method = await Db.PaymentMethods.Where(x => x.Id == payment.PaymentMethodId).Select(x => x.Key).SingleAsync(ct); var allocations = await Db.PaymentAllocations.AsNoTracking().Where(x => x.PaymentId == payment.Id).Select(x => new PaymentAllocationResponse(Db.UnitReceivables.Where(r => r.Id == x.UnitReceivableId).Select(r => r.Code).Single(), x.Amount, Db.UnitReceivables.Where(r => r.Id == x.UnitReceivableId).Select(r => r.DemandAllocationId == null ? null : Db.DemandAllocations.Where(a => a.Id == r.DemandAllocationId).Select(a => Db.Demands.Where(d => d.Id == a.DemandId).Select(d => d.Code).Single()).Single()).Single(), Db.UnitReceivables.Where(r => r.Id == x.UnitReceivableId).Select(r => r.AccountAdjustmentId == null ? null : Db.AccountAdjustments.Where(a => a.Id == r.AccountAdjustmentId).Select(a => a.Code).Single()).Single())).ToListAsync(ct); var evidence = await Db.PaymentEvidenceFiles.AsNoTracking().Where(x => x.PaymentId == payment.Id && x.IsActive).Select(x => new FinancialFileResponse(Db.StoredFiles.Where(f => f.Id == x.StoredFileId).Select(f => new StoredFileResponse(f.Code, "/api/v1/files/" + f.Code + "/content", f.OriginalFileName, f.ContentType, f.FileExtension, f.FileSizeBytes)).Single(), x.Title, x.Description)).ToListAsync(ct); return new(payment.Code, payment.Amount, payment.Status, method, unitCode, fund.OwnerCode, fund.AccountKindKey, payment.PayerPartyId == null ? null : await Db.Parties.Where(x => x.Id == payment.PayerPartyId).Select(x => x.Code).SingleAsync(ct), payment.PaidAtUtc, payment.SubmittedAtUtc, payment.ConfirmedAtUtc, payment.BankTrackingCode, payment.GatewayReference, allocations, evidence); }
}
