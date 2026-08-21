using BuildingManagement.Domain;

namespace BuildingManagement.Application;

public sealed record RequestOtpRequest(string Mobile, string PurposeKey);
public sealed record RequestOtpResponse(string ChallengeReference, DateTimeOffset ExpiresAtUtc);
public sealed record VerifyOtpRequest(string ChallengeReference, string Code, string ClientTypeKey = "web",
    string? DisplayName = null, string? DeviceIdentifier = null);
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAtUtc,
    string UserCode, string? PartyCode);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record StartRecoveryRequest(string OldMobile, string NewMobile,
    string? IdentityNumber = null, DateOnly? BirthDate = null);
public sealed record RecoveryReferenceResponse(string RecoveryReference, string Status,
    DateTimeOffset ExpiresAtUtc);
public sealed record RecoveryStatusResponse(string Status, DateTimeOffset ExpiresAtUtc,
    bool CanComplete);
public sealed record RecoveryOtpRequestResponse(string ChallengeReference, DateTimeOffset ExpiresAtUtc);
public sealed record VerifyRecoveryMobileRequest(string ChallengeReference, string OtpCode);
public sealed record CompleteRecoveryRequest(string ChallengeReference, string OtpCode,
    string ClientTypeKey = "web", string? DeviceIdentifier = null);
public sealed record PlatformLoginRequest(string Username, string Password, string? DeviceIdentifier = null);
public sealed record PlatformTokenResponse(string AccessToken, string RefreshToken,
    DateTimeOffset ExpiresAtUtc, string PlatformUserCode);
public sealed record RecoveryReviewResponse(string Code, string Status, string? OldMobile,
    string? NewMobile, string? IdentityNumber, DateOnly? BirthDate, DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? MobileVerifiedAtUtc, string? ReviewReason);
public sealed record RecoveryDecisionRequest(string Reason, string? TicketReference = null);
public sealed record StartActingSessionRequest(string TargetUserCode, string Reason,
    string? TicketReference = null, int RequestedMinutes = 30);
public sealed record ActingSessionTokenResponse(string Code, string AccessToken,
    DateTimeOffset ExpiresAtUtc, string TargetUserCode);
public sealed record ActingSessionResponse(string Code, string TargetUserCode, string Reason,
    string? TicketReference, DateTimeOffset ExpiresAtUtc, DateTimeOffset? EndedAtUtc,
    string Status);

public interface IPlatformPasswordHasher
{
    string Hash(PlatformUser user, string password);
    bool Verify(PlatformUser user, string passwordHash, string password);
}
public sealed record LoginMethodOtpRequest(string Mobile);
public sealed record ReleaseLoginMethodRequest(string ReasonKey, string? ReplacementPrimaryLoginMethodCode = null);
public sealed record AddLoginMethodRequest(string Mobile, string ChallengeReference, string OtpCode, bool MakePrimary);
public sealed record LoginMethodResponse(string Code, string TypeKey, string Identifier, bool IsPrimary,
    bool IsVerified, string StatusKey, DateTimeOffset? ReleasedAtUtc);
public sealed record SessionResponse(string Code, string ClientTypeKey, DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? LastSeenAtUtc, bool IsCurrent, string? DeviceIdentifier = null);
public sealed record CurrentUserResponse(string Code, string StatusKey, string? PartyCode,
    string? DisplayName, IReadOnlyList<LoginMethodResponse> LoginMethods);
public sealed record CreateUnitInvitationRequest(string UnitCode, string PartyCode, string RelationTypeKey);
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
    string Result, string? InvitationCode, string? InvitationToken);
public sealed record InvitationListItemResponse(string Code, string TypeKey, string RoleKey,
    string ScopeKind, string ScopeCode, string ScopeName, string Status, DateTimeOffset ExpiresAtUtc);

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
