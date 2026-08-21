# Repository Operating Guide for AI Agents

This file is the first source of truth for any new AI session. Read it completely before proposing or changing code. Then read `docs/project-context.md`, the feature-specific document relevant to the task, and the applicable ADRs. Do not infer the product only from entity names.

## Product identity

This repository is the backend of a Persian-first Building Management System. It manages the physical hierarchy of Iranian properties, reusable people/organizations, unit occupancy history, files, assets and their events, and the operational financial domain of buildings/complexes/units.

It is a production-oriented .NET 10 backend, not a tutorial project. The current architecture and implemented domains have passed several human/AI review and hardening passes. Preserve approved decisions unless the user explicitly authorizes a redesign.

## Architecture

- One ASP.NET Core service and one SQL Server database: a modular monolith.
- `BuildingManagement.Domain`: entities, value rules, invariants; no dependency on Application, Infrastructure, or API.
- `BuildingManagement.Application`: explicit DTOs and use-case services over `IApplicationDbContext`; cancellation propagates.
- `BuildingManagement.Infrastructure`: EF Core SQL Server mappings, migrations, deterministic seeds, local file-storage adapter.
- `BuildingManagement.Api`: thin versioned Minimal API handlers, Problem Details, Swagger/OpenAPI, health and CORS.
- Tests: domain unit tests plus opt-in real SQL Server integration tests. EF InMemory is not an integration substitute.

Do not introduce microservices, CQRS, MediatR, generic repositories, AutoMapper, event sourcing, or a new architectural pattern without a demonstrated requirement and explicit approval. Prefer existing patterns and platform capabilities.

## Implemented modules

1. Physical structure: Location, Complex, Building, Unit and reference data.
2. File management: StoredFile, building/complex galleries and documents, secure streamed content.
3. Party and occupancy: Party, PartyContact, UnitPartyRelation, UnitOccupancyHistory.
4. Asset management: Asset, AssetEvent, gallery/documents/event files and derived review dates.
5. Financial domain: FinancialAccount, immutable transactions/entries, Expense/Disbursement, Demand/allocation/Receivable, Payment/allocation, AccountAdjustment and explicit UnitCreditSettlement.
6. Identity and access: customer OTP/login/session security, Invitations and scoped Memberships, recovery, separate Platform/support acting, Membership exits, and narrow individual AccessGrants.

Notification delivery/provider integration, reporting/BI, localization tables, multitenancy, SSO/MFA, refunds/reversals, real gateway adapters and advanced finance workflows are not implemented.

## Identifier and persistence rules

- Internal PK/FK values are SQL Server `bigint IDENTITY`; never expose or accept them in ordinary HTTP contracts.
- Addressable aggregate resources use an immutable server-generated five-character uppercase alphanumeric `Code`, unique per table. Routes, requests and support references use codes.
- Reference-data rows use internal Id plus stable lowercase semantic `Key`; they do not receive random public codes.
- Child/relation/history rows normally use internal IDs only. `AssetEvent.Id` is the deliberate nested-child exception.
- Application tables use schema `bms`. The deliberate file metadata exception is `base.StoredFiles`.
- Money is `decimal(18,2)` in the current implementation. Do not silently introduce floating-point money.
- Canonical timestamps are UTC `DateTimeOffset`. Presentation/calendar conversion belongs to clients.
- Deletes affecting history are restricted. `IsActive` is soft deletion/activation, not a substitute for business lifecycle state.
- EF rowversion protects tracked concurrent writes. HTTP clients do not yet send concurrency tokens.

## Core domain rules

### Physical structure

- Location is hierarchical and classified by key-based LocationType.
- A Building belongs to a Location and optionally a Complex. A Unit belongs to exactly one Building.
- Read DTOs include useful parent summaries so the frontend does not need avoidable follow-up calls.
- Iran country/province/city bootstrap data is retained locally under `data/`; do not fetch it again casually.

### Party and occupancy

- Party is a real person/organization; User is authentication identity. Never conflate them.
- IAM separates User, replaceable UserLoginMethod, Party, and scoped AccessMembership. OTP/access/refresh secrets are hash-only; active context never grants permission; platform staff remain separate customer identities. Read `docs/identity-access-management.md` before IAM changes.
- Party can be created with only type and display name. IdentityNumber and contacts are optional.
- PartyContact has no public Code. `IsPrimary`, verification state and `Verify()` are intentional.
- UnitPartyRelation preserves owner/tenant/resident history. Current means `IsActive = true AND EndDate IS NULL`; ended means active record with non-null EndDate; `IsActive = false` means soft-deleted.
- `Unit.CurrentOccupantsCount` and the single open UnitOccupancyHistory row change atomically.
- Vacating ends tenant/resident relations; ownership remains until explicitly ended.

### Files and assets

- File bytes are behind `IFileStorage`; only relative storage keys are persisted. Never expose raw disk paths or static upload roots.
- `base.StoredFiles` is storage-neutral metadata. Parent/file link tables enforce scope and restricted history.
- Asset belongs to exactly one Building or Complex. AssetEvent is historical; suggested next review is derived, not a schedule.
- AssetEvent cost is informational and must not mutate finance.

### Finance

- Expense records a cost and does not move Fund money. ExpenseDisbursement is actual Fund payment.
- Demand requests money from Units. Finalizing creates Receivables and decreases Unit net balance; it does not increase a Fund.
- Confirmed Payment increases Unit CurrentBalance and receiving Fund, applies explicit allocations, and places any unapplied amount in Unit AvailableCredit.
- Unit credit belongs to the Unit, not a Party. It is consumed only by explicit UnitCreditSettlement.
- Credit settlement changes AvailableCredit and Receivable outstanding only; it does not change CurrentBalance or Fund and creates no fake transaction entry.
- Every approved CurrentBalance mutation creates a FinancialTransaction and FinancialTransactionEntries atomically.
- PaidAtUtc is the real business time; ConfirmedAtUtc is confirmation time; transaction occurrence uses the real paid time where available.
- Offline bank-originated methods require normalized tracking code; uniqueness is DB-backed per receiving Fund.
- Financial history and allocation snapshots are immutable. Never recalculate or rewrite historical snapshots from current state.
- Fund balances may be negative. Do not auto-create Demand or fabricate funding concepts.

Read `docs/financial-domain.md` before any financial change.

## API and error rules

- Base route `/api/v1`; plural resources; public codes instead of numeric IDs.
- Explicit request/response DTOs only; never serialize EF entities.
- Lists use existing pagination conventions (default 1/20, maximum 100) where applicable.
- Invalid client input returns RFC 9457 Problem Details, normally 400; conflicts use stable codes and appropriate 409 behavior.
- Do not leak SQL exceptions, secrets, internal keys or filesystem paths.
- Swagger UI is available at `/swagger` in Development; OpenAPI is `/openapi/v1.json`.

## Workflow for any new task

1. Run `git status`, identify the branch and inspect recent commits. Preserve unrelated user changes.
2. Read this file, `docs/project-context.md`, relevant feature docs, ADRs, and current code before editing.
3. State scope and assumptions. Do not implement future concepts incidentally.
4. Follow the existing module/layer patterns and keep API handlers thin.
5. If the model changes, inspect migrations and ModelSnapshot carefully. Never rewrite already-applied migrations without an explicit safe plan.
6. Add tests that assert domain and persisted state, not only HTTP status.
7. Run restore, build, test, format verification and any relevant EF model check.
8. Review the entire diff for unrelated refactors, secrets, formatting noise and migration junk.
9. Commit/push only when explicitly requested.
10. Update documentation whenever behavior or an architectural decision changes.

## Standard verification

```powershell
dotnet restore BuildingManagement.slnx
dotnet build BuildingManagement.slnx --no-restore
dotnet test BuildingManagement.slnx --no-build
dotnet format BuildingManagement.slnx --verify-no-changes --no-restore
```

SQL Server integration tests are opt-in and use Testcontainers:

```powershell
$env:RUN_SQLSERVER_INTEGRATION_TESTS="true"
dotnet test tests/BuildingManagement.IntegrationTests
```

They skip gracefully if Docker/SQL Server infrastructure is unavailable. Never claim they ran against SQL Server when they were skipped.

## Security and configuration

- Never commit passwords, connection strings, tokens or production file paths.
- Use environment variables, User Secrets or deployment secrets. Treat credentials from chat as ephemeral.
- Development seed must never be enabled in production.
- Local file storage needs a persistent mounted volume and a separate backup from SQL Server.

## Documentation map

- `docs/project-context.md`: complete onboarding and current state.
- `docs/implementation-history.md`: chronological phases and why decisions were made.
- `docs/domain-overview.md`: module relationships and boundaries.
- `docs/data-model.md`: persistence model and schema conventions.
- `docs/api-conventions.md`: HTTP behavior and error conventions.
- `docs/asset-management.md`, `docs/financial-domain.md`: detailed feature rules.
- `docs/adr/`: accepted architecture decisions.
- `docs/roadmap.md`: implemented/deferred boundaries; it is planning, not authorization.

When documents disagree, verify against current code and migrations, then correct the stale document as part of an authorized documentation task. Do not silently choose the most convenient interpretation.
