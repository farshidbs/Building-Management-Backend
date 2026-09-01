using System.Security.Claims;
using System.Text.Encodings.Web;
using BuildingManagement.Application;
using BuildingManagement.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BuildingManagement.Api;

public sealed class DatabaseBearerHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder, IApplicationDbContext db, IIamSecretProtector protector,
    TimeProvider clock) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return AuthenticateResult.NoResult();
        var token = header[7..].Trim();
        if (token.Length < 20) return AuthenticateResult.Fail("Invalid bearer token.");
        var hash = protector.Hash(token);
        var session = await db.AuthSessions.AsNoTracking().SingleOrDefaultAsync(x => x.AccessTokenHash == hash, Context.RequestAborted);
        if (session is null)
        {
            var acting = await db.SupportActingSessions.AsNoTracking().SingleOrDefaultAsync(x =>
                x.TokenHash == hash, Context.RequestAborted);
            var now = clock.GetUtcNow();
            if (acting is null || !acting.IsActive || acting.EndedAtUtc.HasValue || acting.ExpiresAtUtc <= now)
                return AuthenticateResult.Fail("Session or access credential is invalid or expired.");
            var source = await db.AuthSessions.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Id == acting.PlatformAuthSessionId && x.PlatformUserId == acting.PlatformUserId,
                Context.RequestAborted);
            if (source is null || !source.IsUsable(now) ||
                !await db.PlatformUsers.AsNoTracking().AnyAsync(x => x.Id == acting.PlatformUserId &&
                    x.IsActive && x.StatusKey == "active", Context.RequestAborted) ||
                !await db.Users.AsNoTracking().AnyAsync(x => x.Id == acting.TargetUserId && x.IsActive &&
                    x.StatusKey == IamKeys.UserStatuses.Active, Context.RequestAborted))
                return AuthenticateResult.Fail("Support acting session is invalid or expired.");
            var actingClaims = new List<Claim>
            {
                new("session_id", source.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new(ClaimTypes.NameIdentifier, acting.TargetUserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("platform_user_id", acting.PlatformUserId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("support_acting_session_id", acting.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new("actor_type", "support_acting")
            };
            var actingPrincipal = new ClaimsPrincipal(new ClaimsIdentity(actingClaims, Scheme.Name));
            return AuthenticateResult.Success(new AuthenticationTicket(actingPrincipal, Scheme.Name));
        }
        if (!session.IsUsable(clock.GetUtcNow()) || session.AccessTokenExpiresAtUtc <= clock.GetUtcNow()) return AuthenticateResult.Fail("Session or access credential is invalid or expired.");
        if (session.UserId.HasValue)
        {
            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == session.UserId && x.IsActive && x.StatusKey == IamKeys.UserStatuses.Active, Context.RequestAborted);
            if (user is null) return AuthenticateResult.Fail("User is disabled.");
            if (session.AuthenticatedViaLoginMethodId.HasValue &&
                !await db.UserLoginMethods.AsNoTracking().AnyAsync(x =>
                    x.Id == session.AuthenticatedViaLoginMethodId && x.UserId == session.UserId &&
                    x.IsActive && x.IsVerified && x.ReleasedAtUtc == null &&
                    x.StatusKey == IamKeys.LoginStatuses.Active, Context.RequestAborted))
                return AuthenticateResult.Fail("The Session LoginMethod is no longer usable.");
        }
        else if (!await db.PlatformUsers.AsNoTracking().AnyAsync(x => x.Id == session.PlatformUserId &&
            x.IsActive && x.StatusKey == "active", Context.RequestAborted))
            return AuthenticateResult.Fail("Platform User is disabled.");
        var claims = new List<Claim> { new("session_id", session.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)), new(ClaimTypes.NameIdentifier, (session.UserId ?? session.PlatformUserId)!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), new("actor_type", session.UserId.HasValue ? "user" : "platform") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
