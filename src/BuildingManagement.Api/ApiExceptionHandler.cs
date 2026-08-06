using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BuildingManagement.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> Unhandled = LoggerMessage.Define<string>(LogLevel.Error, new EventId(1, "UnhandledRequest"), "Unhandled request failure. TraceId: {TraceId}");

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, code, title, errors) = exception switch
        {
            AppException e => (e.Status, e.Code, e.Message, e.Errors),
            DomainValidationException e => (400, "validation.failed", "One or more validation errors occurred.",
                (IDictionary<string, string[]>?)new Dictionary<string, string[]> { [e.Field] = [e.Message] }),
            BadHttpRequestException e => (400, "validation.failed", "The request is invalid.",
                (IDictionary<string, string[]>?)new Dictionary<string, string[]> { ["request"] = [e.Message] }),
            _ => (500, "server.error", "An unexpected error occurred.", null)
        };
        if (status == 500) Unhandled(logger, httpContext.TraceIdentifier, exception);
        var details = new ProblemDetails { Status = status, Title = title, Type = $"https://errors.building-management.local/{code}" };
        details.Extensions["code"] = code; details.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (errors is not null) details.Extensions["errors"] = errors;
        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = httpContext, ProblemDetails = details, Exception = exception });
    }
}
