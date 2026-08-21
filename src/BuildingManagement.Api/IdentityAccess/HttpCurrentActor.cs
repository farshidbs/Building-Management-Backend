using System.Security.Claims;
using BuildingManagement.Application;

namespace BuildingManagement.Api;

public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    public string? ActorType => Value("actor_type");
    public long? UserId => ActorType is "user" or "support_acting" ? LongValue(ClaimTypes.NameIdentifier) : null;
    public long? PlatformUserId => ActorType == "platform" ? LongValue(ClaimTypes.NameIdentifier) : LongValue("platform_user_id");
    public long? SessionId => LongValue("session_id");
    public long? SupportActingSessionId => LongValue("support_acting_session_id");
    private string? Value(string type) => Principal.FindFirstValue(type);
    private long? LongValue(string type) => long.TryParse(Value(type), out var value) ? value : null;
}
