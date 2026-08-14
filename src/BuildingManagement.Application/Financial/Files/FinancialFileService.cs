using BuildingManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuildingManagement.Application;

#pragma warning disable CA1848 // Cleanup logging is only used on an exceptional rollback path.

public sealed class FinancialFileService(IApplicationDbContext db, IFileStorage storage, FileStorageOptions options, TimeProvider clock, ILogger<FinancialFileService> logger, ResourceAuthorization authorization) : FinancialServiceBase(db, clock, authorization)
{
    public async Task<IReadOnlyList<FinancialFileResponse>> ExpenseDocuments(string expenseCode, CancellationToken ct)
    {
        var expense = await Db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(expenseCode), ct)
            ?? throw AppException.NotFound("expense");
        await Authorize(expense, "expense_view", ct);
        var expenseId = expense.Id;
        return await Db.ExpenseDocuments.AsNoTracking().Where(x => x.ExpenseId == expenseId && x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => new FinancialFileResponse(
                Db.StoredFiles.Where(f => f.Id == x.StoredFileId).Select(f => new StoredFileResponse(f.Code,
                    "/api/v1/files/" + f.Code + "/content", f.OriginalFileName, f.ContentType,
                    f.FileExtension, f.FileSizeBytes)).Single(), x.Title, x.Description)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinancialFileResponse>> PaymentEvidence(string paymentCode, CancellationToken ct)
    {
        var payment = await Db.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(paymentCode), ct)
            ?? throw AppException.NotFound("payment");
        var account = await Db.FinancialAccounts.AsNoTracking().SingleAsync(x => x.Id == payment.UnitAccountId, ct);
        await Authorize(account, "payment_view", ct);
        var paymentId = payment.Id;
        return await Db.PaymentEvidenceFiles.AsNoTracking().Where(x => x.PaymentId == paymentId && x.IsActive)
            .OrderByDescending(x => x.CreatedAtUtc).Select(x => new FinancialFileResponse(
                Db.StoredFiles.Where(f => f.Id == x.StoredFileId).Select(f => new StoredFileResponse(f.Code,
                    "/api/v1/files/" + f.Code + "/content", f.OriginalFileName, f.ContentType,
                    f.FileExtension, f.FileSizeBytes)).Single(), x.Title, x.Description)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinancialFileResponse>> DisbursementFiles(string expenseCode, string disbursementCode, CancellationToken ct)
    {
        var expense = await Db.Expenses.AsNoTracking().SingleOrDefaultAsync(x => x.Code == Code(expenseCode), ct) ?? throw AppException.NotFound("expense");
        await Authorize(expense, "expense_view", ct);
        var expenseId = expense.Id;
        var disbursementId = await Db.ExpenseDisbursements.Where(x => x.Code == Code(disbursementCode) && x.ExpenseId == expenseId).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct) ?? throw AppException.NotFound("expense_disbursement");
        return await Db.ExpenseDisbursementFiles.AsNoTracking().Where(x => x.ExpenseDisbursementId == disbursementId && x.IsActive).OrderByDescending(x => x.CreatedAtUtc).Select(x => new FinancialFileResponse(Db.StoredFiles.Where(f => f.Id == x.StoredFileId).Select(f => new StoredFileResponse(f.Code, "/api/v1/files/" + f.Code + "/content", f.OriginalFileName, f.ContentType, f.FileExtension, f.FileSizeBytes)).Single(), x.Title, null)).ToListAsync(ct);
    }

    public async Task<FinancialFileResponse> UploadExpense(string expenseCode, IncomingFile incoming,
        string? title, string? description, CancellationToken ct)
    {
        var expense = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(expenseCode), ct)
            ?? throw AppException.NotFound("expense");
        await Authorize(expense, "expense_update", ct);
        return await Store(expense.Code, "documents", incoming, async file =>
        { Db.ExpenseDocuments.Add(new ExpenseDocument(expense.Id, file.Id, title, description, Now)); await Save(ct); }, title, description, ct);
    }

    public async Task<FinancialFileResponse> UploadDisbursement(string expenseCode, string disbursementCode,
        IncomingFile incoming, string? title, CancellationToken ct)
    {
        var expense = await Db.Expenses.SingleOrDefaultAsync(x => x.Code == Code(expenseCode), ct)
            ?? throw AppException.NotFound("expense");
        await Authorize(expense, "expense_finalize", ct);
        var disbursement = await Db.ExpenseDisbursements.SingleOrDefaultAsync(x =>
            x.Code == Code(disbursementCode) && x.ExpenseId == expense.Id, ct)
            ?? throw AppException.NotFound("expense_disbursement");
        return await Store(expense.Code, $"disbursements/{disbursement.Code}", incoming, async file =>
        { Db.ExpenseDisbursementFiles.Add(new ExpenseDisbursementFile(disbursement.Id, file.Id, title, Now)); await Save(ct); }, title, null, ct);
    }

    public async Task<FinancialFileResponse> UploadPayment(string paymentCode, IncomingFile incoming,
        string? title, string? description, CancellationToken ct)
    {
        var payment = await Db.Payments.SingleOrDefaultAsync(x => x.Code == Code(paymentCode), ct)
            ?? throw AppException.NotFound("payment");
        var account = await Db.FinancialAccounts.SingleAsync(x => x.Id == payment.UnitAccountId, ct);
        await Authorize(account, "payment_submit", ct);
        return await Store(payment.Code, "evidence", incoming, async file =>
        { Db.PaymentEvidenceFiles.Add(new PaymentEvidenceFile(payment.Id, file.Id, title, description, Now)); await Save(ct); }, title, description, ct);
    }
    private async Task<FinancialFileResponse> Store(string ownerCode, string category,
        IncomingFile incoming, Func<StoredFile, Task> link, string? title, string? description,
        CancellationToken ct)
    {
        var valid = FileStoragePolicy.ValidateDocument(incoming, options);
        var name = $"{Guid.NewGuid():N}{valid.Extension}";
        var segments = category.Split('/');
        var key = segments.Length == 1
            ? FileStoragePolicy.CreateStorageKey("financial", ownerCode, segments[0], name)
            : FileStoragePolicy.CreateStorageKey("financial", ownerCode, segments[0], segments[1], name);
        await storage.SaveAsync(new(key, incoming.Content), ct);
        var file = new StoredFile(await Unique(Db.StoredFiles, ct), valid.OriginalFileName, name, key,
            valid.ContentType, valid.Extension, valid.Length, null, StorageProviders.Local, Now);
        Db.StoredFiles.Add(file);
        try
        {
            return await Db.ExecuteInTransaction<FinancialFileResponse>(async token =>
            {
                // Persist metadata first so relation rows always receive a real identity value.
                await Save(token);
                await link(file);
                return new(new(file.Code, $"/api/v1/files/{file.Code}/content", file.OriginalFileName,
                    file.ContentType, file.FileExtension, file.FileSizeBytes), title, description);
            }, ct);
        }
        catch (Exception exception)
        {
            foreach (var entry in Db.ExpenseDocuments.Local.Where(x => x.StoredFileId == file.Id).ToList()) Db.Detach(entry);
            foreach (var entry in Db.ExpenseDisbursementFiles.Local.Where(x => x.StoredFileId == file.Id).ToList()) Db.Detach(entry);
            foreach (var entry in Db.PaymentEvidenceFiles.Local.Where(x => x.StoredFileId == file.Id).ToList()) Db.Detach(entry);
            Db.Detach(file);
            try { await storage.DeleteAsync(key, ct); }
            catch (Exception cleanup) when (cleanup is not OperationCanceledException)
            { logger.LogError(cleanup, "Financial file cleanup failed for {StorageKey} after {ErrorType}.", key, exception.GetType().Name); }
            throw;
        }
    }
}
