namespace BuildingManagement.Application;

public interface ICurrentActor
{
    bool IsAuthenticated { get; }
    long? UserId { get; }
    long? PlatformUserId { get; }
    long? SessionId { get; }
}
