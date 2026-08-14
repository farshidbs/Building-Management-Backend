namespace BuildingManagement.Application;

public sealed record RequestOtpRequest(string Mobile, string PurposeKey);
public sealed record RequestOtpResponse(string ChallengeReference, DateTimeOffset ExpiresAtUtc);
public sealed record VerifyOtpRequest(string ChallengeReference, string Code, string ClientTypeKey = "web",
    string? DisplayName = null, string? DeviceIdentifier = null);
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc,
    string UserCode, string? PartyCode);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ReleaseLoginMethodRequest(string ReasonKey);
public sealed record AddLoginMethodRequest(string Mobile, string ChallengeReference, string OtpCode, bool MakePrimary);
public sealed record LoginMethodResponse(string Code, string TypeKey, string Identifier, bool IsPrimary,
    bool IsVerified, string StatusKey, DateTimeOffset? ReleasedAtUtc);
public sealed record SessionResponse(string Code, string ClientTypeKey, DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? LastSeenAtUtc, bool IsCurrent);
public sealed record CurrentUserResponse(string Code, string StatusKey, string? PartyCode,
    string? DisplayName, IReadOnlyList<LoginMethodResponse> LoginMethods);
public sealed record CreateUnitInvitationRequest(string UnitCode, string PartyCode);
public sealed record CreateBuildingInvitationRequest(string BuildingCode, string Mobile, string RoleKey,
    string? DisplayName = null);
public sealed record InvitationResponse(string Code, string Token, string TypeKey, string RoleKey,
    string ScopeKind, string ScopeCode, string ScopeName, string Status, DateTimeOffset ExpiresAtUtc);
public sealed record InvitationPreviewResponse(string TypeKey, string RoleTitle, string ScopeKind,
    string ScopeName, string? UnitLabel, string Status, DateTimeOffset ExpiresAtUtc);
public sealed record InvitationOtpRequest(string Token);
public sealed record AcceptInvitationRequest(string Token, string ChallengeReference, string OtpCode,
    string ClientTypeKey = "web", string? DisplayName = null, string? DeviceIdentifier = null);
public sealed record InvitationAcceptanceResponse(string Status, string RoleKey, string ScopeKind,
    string ScopeCode, TokenResponse? Tokens);
public sealed record BulkInvitationItemResponse(string PartyDisplayName, string UnitCode, string RoleKey,
    string Result, string? InvitationCode);

public interface IOtpDelivery
{
    Task Send(string normalizedMobile, string code, CancellationToken cancellationToken);
}

public interface IIamSecretProtector
{
    string Hash(string value);
    bool Verify(string value, string hash);
    string CreateToken(int bytes = 32);
    string CreateOtp();
}

public sealed class IamOptions
{
    public int OtpLifetimeMinutes { get; init; } = 3;
    public int OtpMaxAttempts { get; init; } = 5;
    public int WebSessionHours { get; init; } = 12;
    public int MobileSessionDays { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 30;
    public int AccessTokenMinutes { get; init; } = 15;
}
