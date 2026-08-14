namespace BuildingManagement.Application;

public sealed class ResourceAuthorization(ICurrentActor actor, AccessAuthorizationService authorization)
{
    public long UserId => actor.IsAuthenticated && actor.UserId.HasValue
        ? actor.UserId.Value
        : throw new AppException(401, "authentication.required", "Authentication is required.");

    public async Task Ensure(string permissionKey, long? complexId, long? buildingId, long? unitId,
        CancellationToken cancellationToken)
    {
        await EnsureSession(cancellationToken);
        await authorization.Ensure(UserId, permissionKey, complexId, buildingId, unitId, cancellationToken);
    }

    public async Task EnsureAny(string permissionKey, CancellationToken cancellationToken)
    {
        await EnsureSession(cancellationToken);
        await authorization.EnsureAny(UserId, permissionKey, cancellationToken);
    }

    public async Task<bool> Can(string permissionKey, long? complexId, long? buildingId, long? unitId,
        CancellationToken cancellationToken)
    {
        try
        {
            await Ensure(permissionKey, complexId, buildingId, unitId, cancellationToken);
            return true;
        }
        catch (AppException exception) when (exception.Status == 403)
        {
            return false;
        }
    }

    public async Task EnsureParty(string permissionKey, long partyId, CancellationToken cancellationToken)
    {
        await EnsureSession(cancellationToken);
        await authorization.EnsureParty(UserId, permissionKey, partyId, cancellationToken);
    }

    public async Task<long> ResolvePartyReference(string partyCode, CancellationToken cancellationToken)
    {
        await EnsureSession(cancellationToken);
        return await authorization.ResolvePartyReference(UserId, partyCode, cancellationToken);
    }

    public async Task<bool> HasActiveMembership(CancellationToken cancellationToken)
    {
        await EnsureSession(cancellationToken);
        return await authorization.HasActiveMembership(UserId, cancellationToken);
    }

    public async Task<AccessibleResourceIds> Accessible(string permissionKey, CancellationToken cancellationToken)
    {
        await EnsureSession(cancellationToken);
        return await authorization.Accessible(UserId, permissionKey, cancellationToken);
    }

    private Task EnsureSession(CancellationToken cancellationToken) => actor.SessionId.HasValue
        ? authorization.EnsureSession(UserId, actor.SessionId.Value, cancellationToken)
        : throw new AppException(401, "authentication.invalid_session", "The authenticated session is no longer valid.");
}
