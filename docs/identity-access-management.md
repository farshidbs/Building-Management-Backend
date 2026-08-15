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

## Milestone B — Invitation Acceptance Matrix

All customer invitations are bound to a normalized Iranian mobile and an unguessable token. Creation requires `invitation_send` on the exact target scope; Building collaborator creation additionally requires `membership_manage_scoped` on that same Building. Revocation requires `invitation_revoke`. An equivalent live invitation produces a deterministic conflict; expired rows are explicitly marked inactive and retained before reissue. Unit roles are derived from an active Unit relation; management roles are explicitly delegated and never inferred from occupancy.

Only a creation response may contain the one-time plaintext token. Bulk-created rows with `created` status return it because notification delivery is deferred; all other bulk statuses return a null token, and list responses use a separate DTO with no token field. The database stores only the token hash. Public preview may identify a valid expired token as `expired`, but never exposes mobile, Party identity, user existence, or private contacts; revoked tokens are concealed.

Acceptance serializes on the Invitation row with SQL Server `UPDLOCK, HOLDLOCK` and decides terminal state before OTP consumption, identity/session provisioning, or Membership creation. A repeated acceptance returns deterministic conflict and mints no new session or refresh token. Revocation uses the same row lock, so accept and revoke cannot both win. OTP consumption, identity provisioning, Membership creation, and invitation acceptance share one transaction.

For a new User, the accepted mobile creates a new registration identity Party and UserPartyLink. A source `UnitPartyRelation` may be retained on the Membership for authorization validity, but its Party is never treated as the User's identity merely because a `PartyContact` mobile matched. Existing Users retain their existing identity link.

Unit invitation requests explicitly identify `RelationTypeKey`; the server resolves that exact current Party/Unit relation and derives the Unit IAM role. Current means active, already started when a start date is known, and not ended. Zero matches are not found and multiple exact matches produce a deterministic conflict rather than choosing an arbitrary row. A caller never supplies the resulting role. Both normal registration and invitation registration use the canonical `iranian_person` Party type.

An OTP attempt is reserved durably before invitation provisioning. A wrong code commits its attempt count and blocked state independently, while successful OTP consumption and all identity, session, Membership, and invitation mutations remain atomic in the serialized acceptance transaction. A source-linked Membership is equivalent only while its source relation is active, has started when a start date is known, and has no end date. Historical stale Memberships are ended and preserved before a replacement is provisioned.

Invitation terminal states are mutually exclusive: only a pending, unexpired invitation can be revoked. Expired history is materialized as inactive and retained; sequential or concurrent reissue leaves only one effective pending row. Bulk processing isolates malformed or missing Party mobile values as `no_usable_mobile` without exposing or rewriting them, so other rows continue.

| ID | Caller / target | Rule and expected result |
|---|---|---|
| A01 | Building manager → own Unit owner | Allowed; `unit_owner` Membership at Unit, linked to source relation. |
| A02 | Building manager → foreign Unit | Denied by scoped authorization. |
| A03 | Unit invitation → non-Unit role | Denied by workflow allowlist and `RoleAllowedScope`. |
| A04–A06 | Building manager → own Building collaborator | `building_manager`, `manager_assistant`, and `accountant` allowed at Building. |
| A07 | Building collaborator → `complex_manager` | Denied by workflow allowlist. |
| A08 | Building A manager → Building B collaborator | Denied by scoped authorization. |
| A09 | owner/resident relation | Never infers a Building management Membership. |
| A10 | equivalent pending invitation | Existing pending invitation returned; no duplicate row. |
| B01–B03 | verified existing mobile accepts | Existing User/LoginMethod reused; exactly one Membership. |
| B04–B05 | accepted invite/equivalent Membership | Idempotent; no duplicate Membership. |
| B06 | mobile A attempts invite for mobile B | Denied unless invitation-purpose OTP proves mobile B. |
| C01 | unknown mobile accepts | OTP-bound User, LoginMethod, identity Party, session and Membership created transactionally. |
| C02 | provisioning failure | Invitation remains unaccepted; partial identity/access state rolls back. |
| C03 | invitation linked to unambiguous Party | That Party may be linked; no duplicate Party. |
| C04 | ambiguous identity | No heuristic merge; registration identity Party remains separate. |
| D01–D03 | expired/revoked/already accepted | Expired and revoked denied; accepted is idempotent without second Membership. |
| D04 | Building A revokes Building B invite | Denied/concealed. |
| D05–D06 | concurrent existing/new-user acceptance | DB serialization yields one accepted state, User, LoginMethod and Membership. |
| E01–E04 | public preview | Only type, role title, safe Building/Unit label, status and expiry; no mobile, Party, identity or private data. |
| E05–E06 | unknown/expired preview | Safe not-found or expired state without identity/existence disclosure. |
| F01–F04 | own-Building bulk | Server discovers active eligible relations and maps owner/tenant/resident/representative to Unit roles; never management roles. |
| F05–F06 | member/pending target | Per-item `already_member` / `already_pending`; no duplicate. |
| F07 | relation without usable verified mobile contact | Per-item `no_usable_mobile`. |
| F08 | foreign-Building relation | Never discovered or invited. |
| F09 | mixed eligibility | Per-item result; invalid rows do not abort the batch. |
| G01 | Building A invitation list | DB-filtered to authorized Building/Units; excludes Building B. |
| G02 | direct read/revoke foreign invitation | Denied/concealed. |
| G03 | Unit-scoped actor → sibling Unit | Excluded/denied. |
| G04 | public preview | Grants no authenticated management access. |
| H01–H04 | created Membership scope/role | Exactly one scope; `RoleAllowedScope` enforced; Unit workflow cannot create Building membership and vice versa. |
| H05 | equivalent active Membership | Existing Membership reused; service and DB concurrency prevent duplicates. |

Bulk responses do not expose target mobiles. Scoped onboarding of unrelated vendors, notifications/SMS delivery, recovery, SSO, support acting, AccessGrant UI and membership-exit workflows remain deferred.

## History, recovery, and support

## Milestone C — Login Method & Session Security Acceptance Matrix

All operations below are authenticated as the customer User represented by the current session. Unknown or foreign public codes are concealed as not found. Security mutations are serialized by locking the User row in SQL Server; filtered unique indexes remain the final protection for active identifiers and primaries.

Customer self-service routes use the `CustomerUser` authorization policy, which requires an authenticated `actor_type=user` claim. A Platform session is therefore rejected before customer IDs are resolved or application services run, including when a PlatformUser numeric ID happens to equal a customer User ID.

| IDs | Actor and target | Expected result and database invariant |
|---|---|---|
| LM01–LM02 | User lists methods / targets another User's method | Only own current and historical methods are returned; foreign mutation is concealed and unchanged. |
| LM03–LM07 | User requests and verifies a new mobile | Purpose is `login_method_add`; OTP attempts are durable and purpose/mobile bound; success creates only a verified LoginMethod on the same User. |
| LM08–LM11 | Active or released identifier | Active identifier conflicts without account disclosure; a released identifier creates a new row while history remains unchanged. |
| LM12–LM15 | First/additional/foreign primary selection | A User with usable methods has exactly one primary; switching is atomic; foreign or unusable targets are concealed. |
| REL01–REL05 | User releases own/foreign method | The last usable method is protected. Releasing a primary deterministically requires a usable replacement; foreign methods are concealed. |
| REL06–REL10 | Released method and related sessions | Released rows remain historical, identifier uniqueness is freed, sessions authenticated through that method and their refresh tokens are revoked; unrelated sessions remain active. |
| CON01–CON03 | Concurrent release, identifier claim, or primary switch | User-row serialization prevents zero usable methods and multiple primaries; SQL uniqueness allows one identifier claimant and expected races become deterministic conflicts. |
| SES01–SES05 | User lists/revokes sessions | Only own sessions are visible or mutable, current session is identified, and target session plus refresh tokens are revoked atomically. |
| SES06–SES10 | Logout current/all and refresh | Current logout preserves other sessions; logout-all affects only the User; revoked sessions cannot authenticate or refresh and token rows remain historical. |

When releasing a primary method, `ReplacementPrimaryLoginMethodCode` is required even if only one alternative exists. This keeps the security decision explicit and deterministic. Recovery, support override, email/SSO, MFA, and device trust remain deferred. Docker-dependent SQL race tests remain available for external execution; this environment validates domain/application behavior and statically reviews the SQL locking and filtered-index guarantees.

Existing-customer login and invitation acceptance acquire the same User security lock used by LoginMethod release, then re-read the LoginMethod and require it to remain active, verified, unreleased, and in `active` status before creating a Session. The bearer handler independently enforces the same ownership and usability predicate for customer Sessions backed by a LoginMethod. Thus a stale Session cannot authenticate after its method is released even if revocation side effects were unexpectedly incomplete.

Refresh, specific Session revoke, and current-session logout serialize through a SQL Server Session-row `UPDLOCK, HOLDLOCK`. Refresh re-reads both Session and refresh token after acquiring that lock. Logout-all follows the deterministic order User lock first, then active Session locks in ascending internal ID order; LoginMethod release uses the same User-then-Session ordering. Once revocation commits, a later refresh observes the revoked Session and cannot issue credentials. These are database locks inside active transactions, not process-local synchronization.

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

The default OTP provider remains intentionally unconfigured until an SMS vendor is chosen. Invitation creation, preview, invitation-purpose OTP, acceptance, scoped list, revocation, and bulk creation are implemented. Login-method lifecycle and Session list/revoke APIs are implemented. Context-selection UX, Platform/Support operations, account-recovery operations, audit workflows, and notification delivery remain deferred.
