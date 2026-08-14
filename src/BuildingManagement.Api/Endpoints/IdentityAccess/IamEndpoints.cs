using System.Security.Claims;
using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static class IamEndpoints
{
    public static void MapIamEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/v1/auth").WithTags("Identity & Access");
        auth.MapPost("/otp/request", (RequestOtpRequest request, IamService service, CancellationToken ct) => service.RequestOtp(request, ct)).AllowAnonymous();
        auth.MapPost("/otp/verify", (VerifyOtpRequest request, IamService service, CancellationToken ct) => service.VerifyOtp(request, ct)).AllowAnonymous();
        auth.MapPost("/refresh", (RefreshTokenRequest request, IamService service, CancellationToken ct) => service.Refresh(request, ct)).AllowAnonymous();
        auth.MapGet("/me", (ClaimsPrincipal principal, IamService service, CancellationToken ct) => service.Current(UserId(principal), ct)).RequireAuthorization();
        auth.MapPost("/logout", async (ClaimsPrincipal principal, IamService service, CancellationToken ct) => { await service.Logout(SessionId(principal), false, ct); return Results.NoContent(); }).RequireAuthorization();
        auth.MapPost("/logout-all", async (ClaimsPrincipal principal, IamService service, CancellationToken ct) => { await service.Logout(SessionId(principal), true, ct); return Results.NoContent(); }).RequireAuthorization();
        auth.MapGet("/contexts", (ClaimsPrincipal principal, AccessAuthorizationService service, CancellationToken ct) => service.GetContexts(UserId(principal), ct)).RequireAuthorization();
    }
    private static long UserId(ClaimsPrincipal principal) => long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
    private static long SessionId(ClaimsPrincipal principal) => long.Parse(principal.FindFirstValue("session_id")!, System.Globalization.CultureInfo.InvariantCulture);
}
