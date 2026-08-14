# Identity and Access Management

## Purpose and boundaries

IAM keeps four concepts separate: `Party` is a real person or organization and owns business history; `User` is an account; `UserLoginMethod` is a verified and replaceable login identifier; `AccessMembership` grants a Role at exactly one Complex, Building, or Unit scope. Internal keys are `bigint`; HTTP uses public codes. IAM tables use `bms`; `base.StoredFiles` is unchanged.

## Authentication

`POST /api/v1/auth/otp/request` centrally normalizes Iranian mobiles and creates a purpose-bound challenge. Only an HMAC hash is persisted, with expiry, attempt, and request-rate limits. `IOtpDelivery` is the SMS-provider boundary. Its default implementation fails closed and never logs plaintext OTPs.

`POST /api/v1/auth/otp/verify` either logs in the existing verified account or, for a registration purpose, transactionally creates one User, verified primary login method, minimal person Party, and primary UserPartyLink. It issues random opaque access and refresh secrets while the database stores only hashes. Refresh tokens rotate once; reuse revokes the session. Logout and logout-all revoke sessions.

`Iam:Secret` is mandatory outside Development and must have at least 32 characters. Store it in environment/secrets management, never tracked settings. Web and mobile session lifetimes are configured under `Iam`.

## Authorization

Roles, Permissions, RolePermissions, RoleAllowedScopes, Memberships, building overrides, and explicit AccessGrants are persisted. SQL and domain checks require exactly one scope. Membership authorization requires an active, non-ended membership. Grant authorization requires an active, non-revoked, non-expired grant.

`AccessAuthorizationService` is the central authorization/read-context abstraction. Active context is only navigation state and never grants access. Knowing a public resource or StoredFile code is not authorization. Every resource endpoint must resolve its real scope and call this boundary. List filtering uses the same effective membership, hierarchy, Building override, and supported individual-grant policy as direct access; Building deny overrides therefore remove resources from both direct and list results.

File permissions are intentionally separated: `file_read` permits ordinary metadata/content reads, `file_read_confidential` permits confidential document metadata/content at the exact effective scope, and `file_manage` permits Building/Complex gallery and document mutations. `file_manage` is seeded only to Complex and Building managers. Asset file mutations continue to use `asset_manage` or `asset_event_manage` through the Asset's real scope.

An authenticated active user with no active membership may establish their first root scope by creating either a Complex or a standalone Building. Creation and the single canonical `complex_manager` or `building_manager` membership are committed atomically. The zero-membership decision is serialized per User with a SQL Server row lock inside that transaction, so concurrent Complex/Building requests cannot consume the exception twice. This exception applies only while the user has zero usable active memberships; after onboarding, ordinary scope permissions apply. A Building inside an existing Complex always requires authority on that Complex and never uses the first-root exception or creates a redundant Building membership.

Public standalone Party creation is intentionally rejected for customer users; a new Party must be created atomically through a scoped Unit relation or occupancy workflow so failed onboarding cannot leave an inaccessible orphan. Any caller-supplied existing `PartyCode` (including occupancy, asset provider, and financial references) is resolved through central authorization. It is allowed only when the Party is the caller's own active `UserPartyLink` identity Party, or the caller already has `party_view` visibility through a legitimate Unit relationship. The self exception exists only for reference resolution and does not make profile Parties globally readable. Knowing or guessing any other Party code never permits attaching it to a scope.

Scoped onboarding and management of unattached external vendors and service providers is deferred. Until that workflow has an explicit customer-scope relationship, an unattached Party that is neither the caller's own identity Party nor visible through a Unit relation is denied; no fake Unit relation or global Party lookup is used.

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
