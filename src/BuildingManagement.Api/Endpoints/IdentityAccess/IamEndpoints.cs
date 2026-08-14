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
        auth.MapGet("/invitations/preview", (string token, InvitationService service, CancellationToken ct) => service.Preview(token, ct)).AllowAnonymous();
        auth.MapPost("/invitations/otp", (InvitationOtpRequest request, InvitationService service, CancellationToken ct) => service.RequestAcceptanceOtp(request, ct)).AllowAnonymous();
        auth.MapPost("/invitations/accept", (AcceptInvitationRequest request, InvitationService service, CancellationToken ct) => service.Accept(request, ct)).AllowAnonymous();

        var invitations = app.MapGroup("/api/v1/invitations").WithTags("Identity & Access").RequireAuthorization();
        invitations.MapPost("/unit", async (CreateUnitInvitationRequest request, InvitationService service, CancellationToken ct) => Results.Created("/api/v1/invitations", await service.CreateUnit(request, ct)));
        invitations.MapPost("/building", async (CreateBuildingInvitationRequest request, InvitationService service, CancellationToken ct) => Results.Created("/api/v1/invitations", await service.CreateBuilding(request, ct)));
        invitations.MapPost("/building/{buildingCode}/bulk", (string buildingCode, InvitationService service, CancellationToken ct) => service.CreateBulk(buildingCode, ct));
        invitations.MapGet("/", (string? buildingCode, string? unitCode, InvitationService service, CancellationToken ct) => service.List(buildingCode, unitCode, ct));
        invitations.MapPost("/{code}/revoke", async (string code, InvitationService service, CancellationToken ct) => { await service.Revoke(code, ct); return Results.NoContent(); });
    }
    private static long UserId(ClaimsPrincipal principal) => long.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
    private static long SessionId(ClaimsPrincipal principal) => long.Parse(principal.FindFirstValue("session_id")!, System.Globalization.CultureInfo.InvariantCulture);
}
