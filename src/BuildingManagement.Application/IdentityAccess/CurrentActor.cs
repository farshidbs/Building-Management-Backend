namespace BuildingManagement.Application;

public interface ICurrentActor
{
    bool IsAuthenticated { get; }
    string? ActorType { get; }
    long? UserId { get; }
    long? PlatformUserId { get; }
    long? SessionId { get; }
    long? SupportActingSessionId { get; }
}
