using System.Security.Claims;
using BuildingManagement.Application;

namespace BuildingManagement.Api;

public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();
    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated == true;
    public long? UserId => Value("actor_type") == "user" ? LongValue(ClaimTypes.NameIdentifier) : null;
    public long? PlatformUserId => Value("actor_type") == "platform" ? LongValue(ClaimTypes.NameIdentifier) : null;
    public long? SessionId => LongValue("session_id");
    private string? Value(string type) => Principal.FindFirstValue(type);
    private long? LongValue(string type) => long.TryParse(Value(type), out var value) ? value : null;
}
