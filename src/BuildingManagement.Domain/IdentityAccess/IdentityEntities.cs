namespace BuildingManagement.Domain;

public static class IamKeys
{
    public static class UserStatuses { public const string Active = "active"; public const string PendingReview = "pending_identity_review"; public const string Locked = "locked"; public const string Disabled = "disabled"; public const string Closed = "closed"; }
    public static class LoginTypes { public const string Mobile = "mobile"; }
    public static class LoginStatuses { public const string Active = "active"; public const string Pending = "pending"; public const string Released = "released"; }
    public static class SessionStatuses { public const string Active = "active"; public const string Revoked = "revoked"; public const string Expired = "expired"; }
    public static class OtpStatuses { public const string Pending = "pending"; public const string Verified = "verified"; public const string Consumed = "consumed"; public const string Expired = "expired"; public const string Blocked = "blocked"; }
    public static class Effects { public const string Allow = "allow"; public const string Deny = "deny"; }
    public static class Scopes { public const string Platform = "platform"; public const string Complex = "complex"; public const string Building = "building"; public const string Unit = "unit"; }
}

public sealed class User : Entity
{
    private User() { }
    public string StatusKey { get; private set; } = IamKeys.UserStatuses.Active;
    public User(string code, DateTimeOffset now) => Initialize(code, now);
    public void ChangeStatus(string status, DateTimeOffset now) { StatusKey = status; SetActivation(status is IamKeys.UserStatuses.Active or IamKeys.UserStatuses.PendingReview, now); }
    public void TouchSecurityState(DateTimeOffset now) => Touch(now);
}

public sealed class UserLoginMethod : Entity
{
    private UserLoginMethod() { }
    public long UserId { get; private set; }
    public string LoginTypeKey { get; private set; } = "";
    public string IdentifierValue { get; private set; } = "";
    public string NormalizedIdentifierValue { get; private set; } = "";
    public string? ProviderKey { get; private set; }
    public string? ProviderSubject { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsVerified { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset? ReleasedAtUtc { get; private set; }
    public string? ReleaseReasonKey { get; private set; }
    public string StatusKey { get; private set; } = IamKeys.LoginStatuses.Pending;
    public UserLoginMethod(string code, long userId, string type, string value, string normalized, bool primary, DateTimeOffset now) { Initialize(code, now); UserId = userId; LoginTypeKey = Required(type, "loginTypeKey"); IdentifierValue = Required(value, "identifierValue"); NormalizedIdentifierValue = Required(normalized, "normalizedIdentifierValue"); IsPrimary = primary; }
    public void Verify(DateTimeOffset now) { IsVerified = true; VerifiedAtUtc = now; StatusKey = IamKeys.LoginStatuses.Active; Touch(now); }
    public void SetPrimary(bool value, DateTimeOffset now) { IsPrimary = value; Touch(now); }
    public void Release(string reason, DateTimeOffset now) { StatusKey = IamKeys.LoginStatuses.Released; ReleasedAtUtc = now; ReleaseReasonKey = Required(reason, "reason"); IsPrimary = false; SetActivation(false, now); }
}

public sealed class UserPartyLink : Entity
{
    private UserPartyLink() { }
    public long UserId { get; private set; }
    public long PartyId { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTimeOffset LinkedAtUtc { get; private set; }
    public DateTimeOffset? UnlinkedAtUtc { get; private set; }
    public UserPartyLink(string code, long userId, long partyId, bool primary, DateTimeOffset now) { Initialize(code, now); UserId = userId; PartyId = partyId; IsPrimary = primary; LinkedAtUtc = now; }
    public void Unlink(DateTimeOffset now) { UnlinkedAtUtc = now; SetActivation(false, now); }
}

public sealed class PartyAffiliation : Entity
{
    private PartyAffiliation() { }
    public long PersonPartyId { get; private set; }
    public long OrganizationPartyId { get; private set; }
    public string AffiliationTypeKey { get; private set; } = ""; public string? Title { get; private set; }
    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public PartyAffiliation(string code, long person, long organization, string type, string? title, DateTimeOffset? starts, DateTimeOffset? ends, DateTimeOffset now) { if (person == organization) throw new DomainValidationException("organizationParty", "Parties must differ."); Initialize(code, now); PersonPartyId = person; OrganizationPartyId = organization; AffiliationTypeKey = Required(type, "affiliationTypeKey"); Title = Optional(title); StartsAtUtc = starts; EndsAtUtc = ends; }
}

public sealed class OtpChallenge
{
    private OtpChallenge() { }
    public long Id { get; private set; }
    public string PublicReference { get; private set; } = "";
    public string LoginTypeKey { get; private set; } = ""; public string IdentifierValue { get; private set; } = ""; public string NormalizedIdentifierValue { get; private set; } = ""; public string PurposeKey { get; private set; } = ""; public string CodeHash { get; private set; } = ""; public DateTimeOffset SentAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; private set; }
    public DateTimeOffset? VerifiedAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public string StatusKey { get; private set; } = IamKeys.OtpStatuses.Pending; public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public OtpChallenge(string publicReference, string type, string value, string normalized, string purpose, string hash, DateTimeOffset now, DateTimeOffset expires, int maxAttempts) { PublicReference = string.IsNullOrWhiteSpace(publicReference) ? throw new DomainValidationException("publicReference", "Must not be blank.") : publicReference.Trim(); LoginTypeKey = type; IdentifierValue = value; NormalizedIdentifierValue = normalized; PurposeKey = purpose; CodeHash = hash; SentAtUtc = CreatedAtUtc = now; ExpiresAtUtc = expires; MaxAttempts = maxAttempts; }
    public bool CanAttempt(DateTimeOffset now) => StatusKey == IamKeys.OtpStatuses.Pending && now <= ExpiresAtUtc && AttemptCount < MaxAttempts;
    public void Fail(DateTimeOffset now) { AttemptCount++; if (AttemptCount >= MaxAttempts) StatusKey = IamKeys.OtpStatuses.Blocked; else if (now > ExpiresAtUtc) StatusKey = IamKeys.OtpStatuses.Expired; }
    public void Verify(DateTimeOffset now) { StatusKey = IamKeys.OtpStatuses.Verified; VerifiedAtUtc = now; }
    public void Consume(DateTimeOffset now) { if (StatusKey != IamKeys.OtpStatuses.Verified) throw new DomainValidationException("otp", "OTP is not verified."); StatusKey = IamKeys.OtpStatuses.Consumed; ConsumedAtUtc = now; }
}

public sealed class AuthSession : Entity
{
    private AuthSession() { }
    public long? UserId { get; private set; }
    public long? PlatformUserId { get; private set; }
    public long? AuthenticatedViaLoginMethodId { get; private set; }
    public string ClientTypeKey { get; private set; } = ""; public long? ActiveComplexId { get; private set; }
    public long? ActiveBuildingId { get; private set; }
    public long? ActiveRoleId { get; private set; }
    public string AccessTokenHash { get; private set; } = "";
    public DateTimeOffset AccessTokenExpiresAtUtc { get; private set; }
    public DateTimeOffset? LastSeenAtUtc { get; private set; }
    public DateTimeOffset AbsoluteExpiresAtUtc { get; private set; }
    public DateTimeOffset? IdleExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokeReasonKey { get; private set; }
    public string? DeviceIdentifier { get; private set; }
    public string? UserAgent { get; private set; }
    public string StatusKey { get; private set; } = IamKeys.SessionStatuses.Active;
    public AuthSession(string code, long? userId, long? platformUserId, long? loginMethodId, string client, string accessTokenHash, DateTimeOffset accessTokenExpiry, DateTimeOffset now, DateTimeOffset absoluteExpiry, DateTimeOffset? idleExpiry, string? device, string? agent) { if (userId.HasValue == platformUserId.HasValue) throw new DomainValidationException("actor", "Exactly one actor is required."); Initialize(code, now); UserId = userId; PlatformUserId = platformUserId; AuthenticatedViaLoginMethodId = loginMethodId; ClientTypeKey = client; AccessTokenHash = Required(accessTokenHash, "accessTokenHash"); AccessTokenExpiresAtUtc = accessTokenExpiry; AbsoluteExpiresAtUtc = absoluteExpiry; IdleExpiresAtUtc = idleExpiry; DeviceIdentifier = Optional(device); UserAgent = Optional(agent); }
    public bool IsUsable(DateTimeOffset now) => IsActive && StatusKey == IamKeys.SessionStatuses.Active && RevokedAtUtc is null && now < AbsoluteExpiresAtUtc && (!IdleExpiresAtUtc.HasValue || now < IdleExpiresAtUtc);
    public void Revoke(string reason, DateTimeOffset now) { RevokedAtUtc = now; RevokeReasonKey = reason; StatusKey = IamKeys.SessionStatuses.Revoked; SetActivation(false, now); }
    public void RotateAccessToken(string accessTokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset now) { AccessTokenHash = Required(accessTokenHash, "accessTokenHash"); AccessTokenExpiresAtUtc = expiresAtUtc; LastSeenAtUtc = now; Touch(now); }
    public void SelectContext(long? complexId, long? buildingId, long? roleId, DateTimeOffset now) { ActiveComplexId = complexId; ActiveBuildingId = buildingId; ActiveRoleId = roleId; LastSeenAtUtc = now; Touch(now); }
}

public sealed class AuthRefreshToken
{
    private AuthRefreshToken() { }
    public long Id { get; private set; }
    public long AuthSessionId { get; private set; }
    public string TokenHash { get; private set; } = ""; public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public long? ReplacedByRefreshTokenId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public AuthRefreshToken(long sessionId, string hash, DateTimeOffset now, DateTimeOffset expires) { AuthSessionId = sessionId; TokenHash = hash; IssuedAtUtc = CreatedAtUtc = now; ExpiresAtUtc = expires; }
    public void Consume(DateTimeOffset now) { if (ConsumedAtUtc.HasValue || RevokedAtUtc.HasValue) throw new DomainValidationException("refreshToken", "Refresh token was already used."); ConsumedAtUtc = now; }
    public void ReplaceWith(long id) => ReplacedByRefreshTokenId = id;
    public void Revoke(DateTimeOffset now) => RevokedAtUtc = now;
}

public sealed class AccessRole : ReferenceDataItem { private AccessRole() { } public string? Description { get; private set; } public bool IsSystemRole { get; private set; } public AccessRole(string key, string title, string? description, int order, DateTimeOffset now) { Initialize(key, title, order, now); Description = description; IsSystemRole = true; } }
public sealed class AccessCapability : ReferenceDataItem { private AccessCapability() { } public string CategoryKey { get; private set; } = ""; public string? Description { get; private set; } public AccessCapability(string key, string title, string category, int order, DateTimeOffset now) { Initialize(key, title, order, now); CategoryKey = category; } }
public sealed class AccessRoleCapability { private AccessRoleCapability() { } public long Id { get; private set; } public long RoleId { get; private set; } public long PermissionId { get; private set; } public string EffectKey { get; private set; } = IamKeys.Effects.Allow; public AccessRoleCapability(long role, long permission, string effect) { RoleId = role; PermissionId = permission; EffectKey = effect; } }
public sealed class RoleAllowedScope { private RoleAllowedScope() { } public long Id { get; private set; } public long RoleId { get; private set; } public string ScopeKindKey { get; private set; } = ""; public RoleAllowedScope(long role, string scope) { RoleId = role; ScopeKindKey = scope; } }

public sealed class AccessMembership : Entity
{
    private AccessMembership() { }
    public long UserId { get; private set; }
    public long RoleId { get; private set; }
    public long? ComplexId { get; private set; }
    public long? BuildingId { get; private set; }
    public long? UnitId { get; private set; }
    public long? SourceUnitPartyRelationId { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? EndsAtUtc { get; private set; }
    public string StatusKey { get; private set; } = "active";
    public AccessMembership(string code, long user, long role, long? complexId, long? buildingId, long? unitId, long? sourceRelation, DateTimeOffset now) { if ((complexId.HasValue ? 1 : 0) + (buildingId.HasValue ? 1 : 0) + (unitId.HasValue ? 1 : 0) != 1) throw new DomainValidationException("scope", "Exactly one scope is required."); Initialize(code, now); UserId = user; RoleId = role; ComplexId = complexId; BuildingId = buildingId; UnitId = unitId; SourceUnitPartyRelationId = sourceRelation; StartsAtUtc = now; }
    public void End(DateTimeOffset now) { EndsAtUtc = now; StatusKey = "ended"; SetActivation(false, now); }
}

public sealed class AccessGrant : Entity
{
    private AccessGrant() { }
    public long UserId { get; private set; }
    public long GrantedByUserId { get; private set; }
    public long PermissionId { get; private set; }
    public long? ComplexId { get; private set; }
    public long? BuildingId { get; private set; }
    public long? UnitId { get; private set; }
    public DateTimeOffset? StartsAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string Reason { get; private set; } = "";
    public AccessGrant(string code, long user, long by, long permission, long? complexId, long? buildingId, long? unitId, DateTimeOffset? starts, DateTimeOffset? expires, string reason, DateTimeOffset now) { if ((complexId.HasValue ? 1 : 0) + (buildingId.HasValue ? 1 : 0) + (unitId.HasValue ? 1 : 0) != 1) throw new DomainValidationException("scope", "Exactly one scope is required."); if (starts.HasValue && expires.HasValue && expires <= starts) throw new DomainValidationException("expiresAtUtc", "Must be later than startsAtUtc."); Initialize(code, now); UserId = user; GrantedByUserId = by; PermissionId = permission; ComplexId = complexId; BuildingId = buildingId; UnitId = unitId; StartsAtUtc = starts; ExpiresAtUtc = expires; Reason = Required(reason, "reason"); }
    public void Revoke(DateTimeOffset now) { if (RevokedAtUtc.HasValue) return; RevokedAtUtc = now; SetActivation(false, now); }
}

public sealed class BuildingAccessSetting : Entity
{
    private BuildingAccessSetting() { }
    public long BuildingId { get; private set; }
    public string UnitVisibilityKey { get; private set; } = "private"; public string FinancialVisibilityKey { get; private set; } = "private";
    public BuildingAccessSetting(string code, long building, DateTimeOffset now) { Initialize(code, now); BuildingId = building; }
    public void Update(string unitVisibility, string financialVisibility, DateTimeOffset now) { UnitVisibilityKey = unitVisibility; FinancialVisibilityKey = financialVisibility; Touch(now); }
}

public sealed class SecurityAuditEvent
{
    private SecurityAuditEvent() { }
    public long Id { get; private set; }
    public long? UserId { get; private set; }
    public long? PlatformUserId { get; private set; }
    public long? AuthSessionId { get; private set; }
    public string EventTypeKey { get; private set; } = ""; public string? ResourceKindKey { get; private set; }
    public string? ResourceCode { get; private set; }
    public string? Reason { get; private set; }
    public string? IpAddress { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public SecurityAuditEvent(long? user, long? platform, long? session, string type, string? kind, string? code, string? reason, string? ip, DateTimeOffset now) { UserId = user; PlatformUserId = platform; AuthSessionId = session; EventTypeKey = type; ResourceKindKey = kind; ResourceCode = code; Reason = reason; IpAddress = ip; OccurredAtUtc = now; }
}

public sealed class Invitation : Entity
{
    private Invitation() { }
    public string TokenHash { get; private set; } = ""; public string NormalizedIdentifierValue { get; private set; } = ""; public string InvitationTypeKey { get; private set; } = ""; public long RoleId { get; private set; }
    public long? ComplexId { get; private set; }
    public long? BuildingId { get; private set; }
    public long? UnitId { get; private set; }
    public long InvitedByUserId { get; private set; }
    public long? SourceUnitPartyRelationId { get; private set; }
    public long? AcceptedByUserId { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ExpiredAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public Invitation(string code, string tokenHash, string identifier, string invitationTypeKey, long role, long? complexId, long? buildingId, long? unitId, long by, long? sourceRelation, DateTimeOffset expires, DateTimeOffset now) { if ((complexId.HasValue ? 1 : 0) + (buildingId.HasValue ? 1 : 0) + (unitId.HasValue ? 1 : 0) != 1) throw new DomainValidationException("scope", "Exactly one scope is required."); Initialize(code, now); TokenHash = Required(tokenHash, "token"); NormalizedIdentifierValue = Required(identifier, "mobile"); InvitationTypeKey = Required(invitationTypeKey, "invitationType"); RoleId = role; ComplexId = complexId; BuildingId = buildingId; UnitId = unitId; InvitedByUserId = by; SourceUnitPartyRelationId = sourceRelation; ExpiresAtUtc = expires; }
    public void Accept(long userId, DateTimeOffset now) { if (RevokedAtUtc.HasValue || now >= ExpiresAtUtc) throw new DomainValidationException("invitation", "Invitation is not usable."); if (AcceptedAtUtc.HasValue) { if (AcceptedByUserId != userId) throw new DomainValidationException("invitation", "Invitation is not usable."); return; } AcceptedByUserId = userId; AcceptedAtUtc = now; Touch(now); }
    public void Revoke(DateTimeOffset now) { if (AcceptedAtUtc.HasValue || ExpiredAtUtc.HasValue || now >= ExpiresAtUtc) throw new DomainValidationException("invitation", "Only a pending invitation can be revoked."); if (RevokedAtUtc.HasValue) return; RevokedAtUtc = now; SetActivation(false, now); }
    public void Expire(DateTimeOffset now) { if (AcceptedAtUtc.HasValue || RevokedAtUtc.HasValue || ExpiredAtUtc.HasValue) return; if (now < ExpiresAtUtc) throw new DomainValidationException("invitation", "Invitation has not expired."); ExpiredAtUtc = now; SetActivation(false, now); }
}

public sealed class PlatformUser : Entity
{
    private PlatformUser() { }
    public string Username { get; private set; } = ""; public string NormalizedUsername { get; private set; } = ""; public string PasswordHash { get; private set; } = ""; public string StatusKey { get; private set; } = "active"; public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public PlatformUser(string code, string username, string passwordHash, DateTimeOffset now) { Initialize(code, now); Username = Required(username, "username"); NormalizedUsername = Username.ToUpperInvariant(); PasswordHash = passwordHash; }
    public void RecordFailedLogin(int maxAttempts, int lockoutMinutes, DateTimeOffset now)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts) LockedUntilUtc = now.AddMinutes(lockoutMinutes);
        Touch(now);
    }
    public void RecordSuccessfulLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0; LockedUntilUtc = null; Touch(now);
    }
}

public sealed class SupportActingSession : Entity
{
    private SupportActingSession() { }
    public long PlatformUserId { get; private set; }
    public long TargetUserId { get; private set; }
    public long PlatformAuthSessionId { get; private set; }
    public string TokenHash { get; private set; } = "";
    public string Reason { get; private set; } = ""; public DateTimeOffset ExpiresAtUtc { get; private set; }
    public string? TicketReference { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public string? EndReasonKey { get; private set; }
    public SupportActingSession(string code, long platform, long target, long platformSession,
        string tokenHash, string reason, string? ticketReference, DateTimeOffset expires, DateTimeOffset now)
    { Initialize(code, now); PlatformUserId = platform; TargetUserId = target; PlatformAuthSessionId = platformSession; TokenHash = Required(tokenHash, "tokenHash"); Reason = Required(reason, "reason"); TicketReference = Optional(ticketReference); ExpiresAtUtc = expires; }
    public void End(string reason, DateTimeOffset now) { if (EndedAtUtc.HasValue) return; EndedAtUtc = now; EndReasonKey = Required(reason, "reason"); SetActivation(false, now); }
}

public sealed class BuildingRolePermissionOverride : Entity
{
    private BuildingRolePermissionOverride() { }
    public long BuildingId { get; private set; }
    public long RoleId { get; private set; }
    public long PermissionId { get; private set; }
    public string EffectKey { get; private set; } = "";
    public BuildingRolePermissionOverride(string code, long buildingId, long roleId, long permissionId, string effect, DateTimeOffset now) { Initialize(code, now); BuildingId = buildingId; RoleId = roleId; PermissionId = permissionId; EffectKey = Required(effect, "effectKey"); }
}

public sealed class MembershipExitRequest : Entity
{
    private MembershipExitRequest() { }
    public long MembershipId { get; private set; }
    public long RequestedByUserId { get; private set; }
    public string StatusKey { get; private set; } = "pending"; public string? Reason { get; private set; }
    public string? DecisionReason { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public long? DecidedByUserId { get; private set; }
    public MembershipExitRequest(string code, long membershipId, long requestedBy, string? reason, DateTimeOffset now) { Initialize(code, now); MembershipId = membershipId; RequestedByUserId = requestedBy; Reason = Optional(reason); }
    public void Decide(bool approved, long decidedBy, string? reason, DateTimeOffset now) { if (StatusKey != "pending") throw new DomainValidationException("exitRequest", "Request was already decided."); StatusKey = approved ? "approved" : "rejected"; DecidedByUserId = decidedBy; DecidedAtUtc = now; DecisionReason = Optional(reason); SetActivation(false, now); }
    public void Cancel(long requestedBy, DateTimeOffset now) { if (RequestedByUserId != requestedBy) throw new DomainValidationException("exitRequest", "Only the requester can cancel this request."); if (StatusKey != "pending") throw new DomainValidationException("exitRequest", "Request was already decided."); StatusKey = "cancelled"; DecidedByUserId = requestedBy; DecidedAtUtc = now; SetActivation(false, now); }
}

public sealed class IdentityConflictReview : Entity
{
    private IdentityConflictReview() { }
    public long? UserId { get; private set; }
    public long? PartyId { get; private set; }
    public string ConflictTypeKey { get; private set; } = ""; public string StatusKey { get; private set; } = "pending"; public string? Details { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public IdentityConflictReview(string code, long? userId, long? partyId, string type, string? details, DateTimeOffset now) { Initialize(code, now); UserId = userId; PartyId = partyId; ConflictTypeKey = Required(type, "conflictTypeKey"); Details = Optional(details); }
}

public sealed class AccountRecoveryCase : Entity
{
    private AccountRecoveryCase() { }
    public long? UserId { get; private set; }
    public long? OldLoginMethodId { get; private set; }
    public string ReferenceHash { get; private set; } = "";
    public string RecoveryTypeKey { get; private set; } = "";
    public string StatusKey { get; private set; } = "pending_mobile_verification";
    public string? OldNormalizedIdentifier { get; private set; }
    public string? NewNormalizedIdentifier { get; private set; }
    public string? IdentityNumberEvidence { get; private set; }
    public DateOnly? BirthDateEvidence { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? MobileVerifiedAtUtc { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public long? ResolvedByPlatformUserId { get; private set; }
    public AccountRecoveryCase(string code, string referenceHash, long? userId, long? oldLoginMethodId,
        string oldIdentifier, string newIdentifier, string? identityNumber, DateOnly? birthDate,
        DateTimeOffset expiresAtUtc, DateTimeOffset now)
    {
        Initialize(code, now); ReferenceHash = Required(referenceHash, "referenceHash"); UserId = userId;
        OldLoginMethodId = oldLoginMethodId; RecoveryTypeKey = "lost_sim";
        OldNormalizedIdentifier = Required(oldIdentifier, "oldMobile");
        NewNormalizedIdentifier = Required(newIdentifier, "newMobile");
        IdentityNumberEvidence = Optional(identityNumber); BirthDateEvidence = birthDate;
        ExpiresAtUtc = expiresAtUtc;
    }
    public void MarkMobileVerified(DateTimeOffset now) { RequirePending(now); MobileVerifiedAtUtc = now; StatusKey = "pending_review"; Touch(now); }
    public void Approve(long platformUserId, string reason, DateTimeOffset now) { RequireReview(now); StatusKey = "approved"; ResolvedByPlatformUserId = platformUserId; Reason = Required(reason, "reason"); ReviewedAtUtc = ResolvedAtUtc = now; Touch(now); }
    public void Reject(long platformUserId, string reason, DateTimeOffset now) { RequireReview(now); StatusKey = "rejected"; ResolvedByPlatformUserId = platformUserId; Reason = Required(reason, "reason"); ReviewedAtUtc = ResolvedAtUtc = now; SetActivation(false, now); }
    public void Complete(DateTimeOffset now) { if (StatusKey != "approved" || MobileVerifiedAtUtc is null || now >= ExpiresAtUtc) throw new DomainValidationException("recovery", "Recovery is not completable."); StatusKey = "completed"; CompletedAtUtc = ResolvedAtUtc = now; SetActivation(false, now); }
    public void Cancel(DateTimeOffset now)
    {
        if (StatusKey is "completed" or "rejected" or "cancelled" or "expired")
            throw new DomainValidationException("recovery", "Recovery is already terminal.");
        StatusKey = "cancelled"; ResolvedAtUtc = now; SetActivation(false, now);
    }
    public void Expire(DateTimeOffset now) { if (StatusKey is "completed" or "rejected" or "cancelled" or "expired") return; if (now < ExpiresAtUtc) throw new DomainValidationException("recovery", "Recovery has not expired."); StatusKey = "expired"; ResolvedAtUtc = now; SetActivation(false, now); }
    private void RequirePending(DateTimeOffset now) { if (StatusKey != "pending_mobile_verification" || now >= ExpiresAtUtc) throw new DomainValidationException("recovery", "Recovery is not awaiting mobile verification."); }
    private void RequireReview(DateTimeOffset now) { if (StatusKey != "pending_review" || MobileVerifiedAtUtc is null || now >= ExpiresAtUtc) throw new DomainValidationException("recovery", "Recovery is not awaiting review."); }
}

public sealed class PlatformRole : ReferenceDataItem { private PlatformRole() { } public PlatformRole(string key, string title, int order, DateTimeOffset now) => Initialize(key, title, order, now); }
public sealed class PlatformCapability : ReferenceDataItem { private PlatformCapability() { } public PlatformCapability(string key, string title, int order, DateTimeOffset now) => Initialize(key, title, order, now); }
public sealed class PlatformRolePermissionLink { private PlatformRolePermissionLink() { } public long Id { get; private set; } public long RoleId { get; private set; } public long PermissionId { get; private set; } public PlatformRolePermissionLink(long roleId, long permissionId) { RoleId = roleId; PermissionId = permissionId; } }
public sealed class PlatformUserRoleLink { private PlatformUserRoleLink() { } public long Id { get; private set; } public long PlatformUserId { get; private set; } public long RoleId { get; private set; } public PlatformUserRoleLink(long userId, long roleId) { PlatformUserId = userId; RoleId = roleId; } }
