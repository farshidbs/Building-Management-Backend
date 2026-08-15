using BuildingManagement.Application;

namespace BuildingManagement.Api;

public static class IamEndpoints
{
    public const string CustomerUserPolicy = "CustomerUser";

    public static void MapIamEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/v1/auth").WithTags("Identity & Access");
        auth.MapPost("/otp/request", (RequestOtpRequest request, IamService service, CancellationToken ct) => service.RequestOtp(request, ct)).AllowAnonymous();
        auth.MapPost("/otp/verify", (VerifyOtpRequest request, IamService service, CancellationToken ct) => service.VerifyOtp(request, ct)).AllowAnonymous();
        auth.MapPost("/refresh", (RefreshTokenRequest request, IamService service, CancellationToken ct) => service.Refresh(request, ct)).AllowAnonymous();
        auth.MapPost("/recovery", (StartRecoveryRequest request, RecoveryService service,
            CancellationToken ct) => service.Start(request, ct)).AllowAnonymous();
        auth.MapGet("/recovery/{reference}", (string reference, RecoveryService service,
            CancellationToken ct) => service.Status(reference, ct)).AllowAnonymous();
        auth.MapPost("/recovery/{reference}/otp", (string reference, RecoveryService service,
            CancellationToken ct) => service.RequestOtp(reference, ct)).AllowAnonymous();
        auth.MapPost("/recovery/{reference}/verify-mobile", (string reference,
            VerifyRecoveryMobileRequest request, RecoveryService service, CancellationToken ct) =>
            service.VerifyMobile(reference, request, ct)).AllowAnonymous();
        auth.MapPost("/recovery/{reference}/complete", (string reference,
            CompleteRecoveryRequest request, RecoveryService service, CancellationToken ct) =>
            service.Complete(reference, request, ct)).AllowAnonymous();
        auth.MapGet("/me", (ICurrentActor actor, IamService service, CancellationToken ct) => service.Current(UserId(actor), ct)).RequireAuthorization(CustomerUserPolicy);
        auth.MapPost("/logout", async (ICurrentActor actor, IamService service, CancellationToken ct) => { await service.Logout(SessionId(actor), false, ct); return Results.NoContent(); }).RequireAuthorization(CustomerUserPolicy);
        auth.MapPost("/logout-all", async (ICurrentActor actor, IamService service, CancellationToken ct) => { await service.Logout(SessionId(actor), true, ct); return Results.NoContent(); }).RequireAuthorization(CustomerUserPolicy);
        auth.MapGet("/contexts", (ICurrentActor actor, AccessAuthorizationService service, CancellationToken ct) => service.GetContexts(UserId(actor), ct)).RequireAuthorization(CustomerUserPolicy);
        auth.MapGet("/invitations/preview", (string token, InvitationService service, CancellationToken ct) => service.Preview(token, ct)).AllowAnonymous();
        auth.MapPost("/invitations/otp", (InvitationOtpRequest request, InvitationService service, CancellationToken ct) => service.RequestAcceptanceOtp(request, ct)).AllowAnonymous();
        auth.MapPost("/invitations/accept", (AcceptInvitationRequest request, InvitationService service, CancellationToken ct) => service.Accept(request, ct)).AllowAnonymous();

        var me = app.MapGroup("/api/v1/me").WithTags("Identity & Access").RequireAuthorization(CustomerUserPolicy);
        me.MapGet("/login-methods", (ICurrentActor actor, IamService service, CancellationToken ct) =>
            service.LoginMethods(UserId(actor), ct));
        me.MapPost("/login-methods/mobile/otp", (ICurrentActor actor, LoginMethodOtpRequest request,
            IamService service, CancellationToken ct) => service.RequestLoginMethodOtp(UserId(actor), request, ct));
        me.MapPost("/login-methods/mobile", async (ICurrentActor actor, AddLoginMethodRequest request,
            IamService service, CancellationToken ct) => Results.Created("/api/v1/me/login-methods",
                await service.AddLoginMethod(UserId(actor), request, ct)));
        me.MapPost("/login-methods/{code}/primary", async (ICurrentActor actor, string code,
            IamService service, CancellationToken ct) =>
        { await service.SetPrimaryLoginMethod(UserId(actor), code, ct); return Results.NoContent(); });
        me.MapPost("/login-methods/{code}/release", async (ICurrentActor actor, string code,
            ReleaseLoginMethodRequest request, IamService service, CancellationToken ct) =>
        { await service.ReleaseLoginMethod(UserId(actor), code, request, ct); return Results.NoContent(); });
        me.MapGet("/sessions", (ICurrentActor actor, IamService service, CancellationToken ct) =>
            service.Sessions(UserId(actor), SessionId(actor), ct));
        me.MapPost("/sessions/{code}/revoke", async (ICurrentActor actor, string code,
            IamService service, CancellationToken ct) =>
        { await service.RevokeSession(UserId(actor), code, ct); return Results.NoContent(); });

        var invitations = app.MapGroup("/api/v1/invitations").WithTags("Identity & Access").RequireAuthorization();
        invitations.MapPost("/unit", async (CreateUnitInvitationRequest request, InvitationService service, CancellationToken ct) => Results.Created("/api/v1/invitations", await service.CreateUnit(request, ct)));
        invitations.MapPost("/building", async (CreateBuildingInvitationRequest request, InvitationService service, CancellationToken ct) => Results.Created("/api/v1/invitations", await service.CreateBuilding(request, ct)));
        invitations.MapPost("/building/{buildingCode}/bulk", (string buildingCode, InvitationService service, CancellationToken ct) => service.CreateBulk(buildingCode, ct));
        invitations.MapGet("/", (string? buildingCode, string? unitCode, InvitationService service, CancellationToken ct) => service.List(buildingCode, unitCode, ct));
        invitations.MapPost("/{code}/revoke", async (string code, InvitationService service, CancellationToken ct) => { await service.Revoke(code, ct); return Results.NoContent(); });
    }
    private static long UserId(ICurrentActor actor) => actor.UserId
        ?? throw new AppException(403, "authorization.customer_user_required", "A customer User is required.");
    private static long SessionId(ICurrentActor actor) => actor.SessionId
        ?? throw new AppException(401, "authentication.session_required", "An authenticated Session is required.");
}
