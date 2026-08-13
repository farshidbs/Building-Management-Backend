using BuildingManagement.Application;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagement.Api;

internal static class FinancialEndpoints
{
    internal static void MapFinancialEndpoints(this RouteGroupBuilder api)
    {
        var accounts = api.MapGroup("/financial/accounts").WithTags("Financial Accounts");
        accounts.MapPost("/", async (FinancialAccountRequest request, FinancialAccountService service, CancellationToken ct) => Results.Created("/api/v1/financial/accounts", await service.Create(request, ct)));
        accounts.MapGet("/units/{unitCode}", (string unitCode, FinancialAccountService service, CancellationToken ct) => service.GetUnit(unitCode, ct));
        accounts.MapGet("/units/{unitCode}/statement", (string unitCode, FinancialAccountService service, CancellationToken ct) => service.UnitStatement(unitCode, ct));
        accounts.MapGet("/funds", (string? buildingCode, string? complexCode, string accountKindKey, FinancialAccountService service, CancellationToken ct) => service.GetFund(buildingCode, complexCode, accountKindKey, ct));
        accounts.MapGet("/funds/statement", (string? buildingCode, string? complexCode, string accountKindKey, FinancialAccountService service, CancellationToken ct) => service.FundStatement(buildingCode, complexCode, accountKindKey, ct));
        accounts.MapPost("/adjustments", async (AccountAdjustmentRequest request, FinancialAccountService service, CancellationToken ct) => Results.Created("/api/v1/financial/accounts/adjustments", await service.CreateAdjustment(request, ct)));
        accounts.MapPost("/adjustments/{code}/finalize", (string code, FinancialAccountService service, CancellationToken ct) => service.FinalizeAdjustment(code, ct));

        var expenses = api.MapGroup("/financial/expenses").WithTags("Financial Expenses");
        expenses.MapPost("/types", async (ExpenseTypeRequest request, ExpenseService service, CancellationToken ct) => Results.Created("/api/v1/financial/expenses/types", await service.CreateType(request, ct)));
        expenses.MapGet("/types", (string? buildingCode, string? complexCode, ExpenseService service, CancellationToken ct) => service.Types(buildingCode, complexCode, ct));
        expenses.MapGet("/", ([AsParameters] PageQuery query, string? status, ExpenseService service, CancellationToken ct) => service.List(query, status, ct));
        expenses.MapGet("/{expenseCode}", (string expenseCode, ExpenseService service, CancellationToken ct) => service.Get(expenseCode, ct));
        expenses.MapPost("/", async (ExpenseRequest request, ExpenseService service, CancellationToken ct) => { var result = await service.Create(request, ct); return Results.Created($"/api/v1/financial/expenses/{result.Code}", result); });
        expenses.MapPost("/{expenseCode}/finalize", (string expenseCode, ExpenseService service, CancellationToken ct) => service.Finalize(expenseCode, ct));
        expenses.MapPost("/{expenseCode}/disbursements", async (string expenseCode, ExpenseDisbursementRequest request, ExpenseService service, CancellationToken ct) => { var result = await service.CreateDisbursement(expenseCode, request, ct); return Results.Created($"/api/v1/financial/expenses/{expenseCode}/disbursements/{result.Code}", result); });
        expenses.MapPost("/{expenseCode}/disbursements/{disbursementCode}/finalize", (string expenseCode, string disbursementCode, ExpenseService service, CancellationToken ct) => service.FinalizeDisbursement(expenseCode, disbursementCode, ct));
        expenses.MapPost("/{expenseCode}/documents", async ([FromRoute] string expenseCode, [FromForm] FinancialUploadForm form, FinancialFileService service, CancellationToken ct) => Results.Created($"/api/v1/financial/expenses/{expenseCode}/documents", await service.UploadExpense(expenseCode, new(form.File.OpenReadStream(), form.File.FileName, form.File.ContentType, form.File.Length), form.Title, form.Description, ct))).DisableAntiforgery();
        expenses.MapGet("/{expenseCode}/documents", (string expenseCode, FinancialFileService service, CancellationToken ct) => service.ExpenseDocuments(expenseCode, ct));
        expenses.MapPost("/{expenseCode}/disbursements/{disbursementCode}/files", async ([FromRoute] string expenseCode, [FromRoute] string disbursementCode, [FromForm] FinancialUploadForm form, FinancialFileService service, CancellationToken ct) => Results.Created($"/api/v1/financial/expenses/{expenseCode}/disbursements/{disbursementCode}/files", await service.UploadDisbursement(expenseCode, disbursementCode, new(form.File.OpenReadStream(), form.File.FileName, form.File.ContentType, form.File.Length), form.Title, ct))).DisableAntiforgery();

        var demands = api.MapGroup("/financial/demands").WithTags("Financial Demands");
        demands.MapGet("/", ([AsParameters] PageQuery query, string? status, DemandService service, CancellationToken ct) => service.List(query, status, ct));
        demands.MapGet("/{demandCode}", (string demandCode, DemandService service, CancellationToken ct) => service.Get(demandCode, ct));
        demands.MapPost("/", async (DemandRequest request, DemandService service, CancellationToken ct) => { var result = await service.Create(request, ct); return Results.Created($"/api/v1/financial/demands/{result.Code}", result); });
        demands.MapPut("/{demandCode}", (string demandCode, UpdateDemandDraftRequest request, DemandService service, CancellationToken ct) => service.Update(demandCode, request, ct));
        demands.MapPost("/{demandCode}/preview", (string demandCode, DemandPreviewRequest request, DemandService service, CancellationToken ct) => service.Preview(demandCode, request, ct));
        demands.MapPost("/{demandCode}/finalize", (string demandCode, DemandPreviewRequest request, DemandService service, CancellationToken ct) => service.Finalize(demandCode, request, ct));

        var payments = api.MapGroup("/financial/payments").WithTags("Financial Payments");
        payments.MapGet("/{paymentCode}", (string paymentCode, PaymentService service, CancellationToken ct) => service.Get(paymentCode, ct));
        payments.MapPost("/", async (PaymentRequest request, PaymentService service, CancellationToken ct) => { var result = await service.Create(request, ct); return Results.Created($"/api/v1/financial/payments/{result.Code}", result); });
        payments.MapPost("/{paymentCode}/manager-confirm", (string paymentCode, PaymentService service, CancellationToken ct) => service.ConfirmManual(paymentCode, ct));
        payments.MapPost("/{paymentCode}/reject", (string paymentCode, PaymentService service, CancellationToken ct) => service.Reject(paymentCode, ct));
        payments.MapPost("/{paymentCode}/evidence", async ([FromRoute] string paymentCode, [FromForm] FinancialUploadForm form, FinancialFileService service, CancellationToken ct) => Results.Created($"/api/v1/financial/payments/{paymentCode}/evidence", await service.UploadPayment(paymentCode, new(form.File.OpenReadStream(), form.File.FileName, form.File.ContentType, form.File.Length), form.Title, form.Description, ct))).DisableAntiforgery();
        payments.MapGet("/{paymentCode}/evidence", (string paymentCode, FinancialFileService service, CancellationToken ct) => service.PaymentEvidence(paymentCode, ct));
        api.MapGet("/units/{unitCode}/financial/receivables", (string unitCode, PaymentService service, CancellationToken ct) => service.Receivables(unitCode, ct)).WithTags("Financial Payments");
        api.MapPost("/financial/units/{unitCode}/credit-settlements", async (string unitCode, UnitCreditSettlementRequest request, UnitCreditSettlementService service, CancellationToken ct) => { var result = await service.Create(unitCode, request, ct); return Results.Created($"/api/v1/financial/units/{unitCode}/credit-settlements/{result.Code}", result); }).WithTags("Financial Accounts");
        api.MapGet("/financial/units/{unitCode}/credit-settlements", (string unitCode, [AsParameters] PageQuery query, UnitCreditSettlementService service, CancellationToken ct) => service.History(unitCode, query, ct)).WithTags("Financial Accounts");
    }
}

public sealed record FinancialUploadForm(IFormFile File, string? Title, string? Description);
