# Identity and Access Management

## Purpose and boundaries

IAM keeps four concepts separate: `Party` is a real person or organization and owns business history; `User` is an account; `UserLoginMethod` is a verified and replaceable login identifier; `AccessMembership` grants a Role at exactly one Complex, Building, or Unit scope. Internal keys are `bigint`; HTTP uses public codes. IAM tables use `bms`; `base.StoredFiles` is unchanged.

## Authentication

`POST /api/v1/auth/otp/request` centrally normalizes Iranian mobiles and creates a purpose-bound challenge. Only an HMAC hash is persisted, with expiry, attempt, and request-rate limits. `IOtpDelivery` is the SMS-provider boundary. Its default implementation fails closed and never logs plaintext OTPs.

`POST /api/v1/auth/otp/verify` either logs in the existing verified account or, for a registration purpose, transactionally creates one User, verified primary login method, minimal person Party, and primary UserPartyLink. It issues random opaque access and refresh secrets while the database stores only hashes. Refresh tokens rotate once; reuse revokes the session. Logout and logout-all revoke sessions.

`Iam:Secret` is mandatory outside Development and must have at least 32 characters. Store it in environment/secrets management, never tracked settings. Web and mobile session lifetimes are configured under `Iam`.

## Authorization

Roles, Permissions, RolePermissions, RoleAllowedScopes, Memberships, building overrides, and explicit AccessGrants are persisted. SQL and domain checks require exactly one scope. Membership authorization requires an active, non-ended membership. Grant authorization requires an active, non-revoked, non-expired grant.

`AccessAuthorizationService` is the central authorization/read-context abstraction. Active context is only navigation state and never grants access. Knowing a public resource or StoredFile code is not authorization. Every resource endpoint must resolve its real scope and call this boundary.

Seed data defines building manager, accountant, owner, tenant, and resident roles plus permissions for physical structure, parties, files, assets, finance, invitations, and memberships. Global Location mutation is guarded by `location_manage`; that permission is intentionally not assigned to normal customer roles.

## History, recovery, and support

Login methods are released, not deleted. Filtered unique indexes prevent two active Users from sharing a normalized identifier while allowing later reuse after release. UserPartyLink and PartyAffiliation preserve history. Party BirthDate is nullable.

IdentityConflictReview, AccountRecoveryCase, MembershipExitRequest, Invitation, SecurityAuditEvent, and SupportActingSession are explicit workflows. PlatformUser authentication, roles, and permissions are separate from customer mobile identities so support never silently becomes a customer.

## Database guarantees

- AuthSession has exactly one customer or platform actor.
- Membership, Invitation, and AccessGrant have exactly one scope.
- A User has at most one active primary login method and primary Party link.
- A Party belongs to at most one active User.
- Active normalized login identifiers are unique.
- Invitation, access-token, and refresh-token hashes are unique.
- BuildingAccessSettings is unique per Building.
- Mutable aggregate/workflow records use rowversion.

Migration `AddIdentityAccessManagement` is additive and also adds nullable `BirthDate` to `bms.Parties`. Historical migrations are unchanged.

## Current branch status

Implemented: persistence model and constraints, OTP registration/login, opaque DB-backed sessions, refresh rotation/reuse handling, logout, current profile, context listing, central permission evaluation, migration, role/permission seeds, and resource authorization across the current physical-structure, Party, file, Asset, and Finance application services. Destination authorization is enforced when a Building or Asset changes scope; unattached Parties are not exposed to ordinary scoped users; confidential files require their distinct permission.

The default OTP provider remains intentionally unconfigured until an SMS vendor is chosen. Login-method lifecycle APIs, Invitations, context-selection UX, Platform/Support operations, account-recovery operations, audit workflows, and notifications remain deferred. Their persistence foundations do not imply that those workflows are available.
