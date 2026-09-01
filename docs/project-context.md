# Project Context and AI Onboarding

This document is the durable briefing for a developer or a new AI conversation that has no prior chat history. Read it with the repository-root `AGENTS.md` before doing work.

## What this product is

Building Management Backend is a Persian-first operational backend for managing residential and mixed property structures. The system is intended to support managers who need a reliable history of buildings, units, occupants, assets, costs, charges, payments and supporting files.

The project deliberately favors explicit business workflows and auditable SQL data over clever abstractions. It is one ASP.NET Core application backed by one SQL Server database. Modules share transactions where the business operation requires atomic consistency.

## Current technology and solution layout

- .NET 10, C#, ASP.NET Core Minimal APIs.
- EF Core SQL Server and generated migrations/snapshot.
- Swagger/OpenAPI in Development.
- xUnit unit tests and real SQL Server Testcontainers integration tests.
- Local file storage behind an Application abstraction; object storage can be added later without changing contracts.

The four production projects have strict responsibilities:

| Project | Responsibility | Must not contain |
|---|---|---|
| Domain | Entities and invariants | EF/API dependencies |
| Application | DTOs and explicit use cases | Web handlers or physical storage |
| Infrastructure | EF mappings, migrations, seeds, storage adapters | Business orchestration |
| API | Routing, transport binding and host configuration | Domain logic |

Files are grouped by feature for navigation, but namespaces and layering remain stable.

## Implemented capability map

### Physical structure

The hierarchy is Location → optional Complex → Building → Unit. Locations contain Iran country/province/city data. Reference classifications use semantic keys rather than enums exposed as numbers. CRUD, activation, filtered pagination and parent-enriched read models are implemented.

### Files

Building and Complex galleries/documents are implemented. Metadata resides in SQL Server and bytes reside behind `IFileStorage`. Asset and Financial evidence reuse the same StoredFile foundation. Content is streamed by public file code.

### Party and occupancy

Party represents a reusable real person or organization, even when contact/identity information is incomplete. Contacts are child rows and can have one primary per Party/contact type. UnitPartyRelation records owner, tenant and resident history. UnitOccupancyHistory and Unit.CurrentOccupantsCount provide historical truth plus efficient current reads.

### Assets

Assets belong to one Building or Complex and have events, galleries, documents and event files. Event dates are historical facts. Next review dates are derived; there is no scheduling or notification subsystem.

### Finance

The implemented financial foundation includes:

- Unit/Fund FinancialAccounts with CurrentBalance and Unit-only AvailableCredit.
- FinancialTransaction headers and immutable FinancialTransactionEntries for every net balance mutation.
- Expense and ExpenseDisbursement as separate concepts.
- Demand draft/preview/finalize with allocation snapshots and UnitReceivables.
- Payment submission, manager confirmation/rejection, trusted gateway application boundary, allocations and evidence.
- Opening debt/credit AccountAdjustments.
- Explicit UnitCreditSettlement with RequestId idempotency, multi-receivable allocations and historical AvailableCreditAfter.
- Operational read models for statements, expenses, payments, receivables and credit settlement history.

See `financial-domain.md` for exact money movement rules.

## Data identity and schema

SQL joins use internal sequential `bigint IDENTITY` keys. Ordinary HTTP clients never see or send them. Addressable resources have immutable random five-character codes using `A-Z0-9`. Reference data uses stable lowercase keys and Persian titles. Child/history/relation rows do not receive meaningless public codes.

Application data is stored in schema `bms`. The intentional exception is `base.StoredFiles`. Historical tables use restricted deletes. Aggregate records use rowversion where configured.

## Typical request flow

```text
HTTP endpoint
  -> explicit request DTO
  -> Application feature service
  -> Domain invariant methods
  -> IApplicationDbContext / EF Core
  -> atomic SQL transaction where required
  -> explicit response DTO / Problem Details
```

Do not bypass Application services from API handlers for business operations. Do not add generic repositories around EF Core.

## Financial mental model

The easiest way to misunderstand this project is to merge distinct financial facts. Keep these separate:

| Operation | Unit balance | Unit credit | Fund | Receivable |
|---|---:|---:|---:|---:|
| Demand finalize | decreases | unchanged | unchanged | created |
| Payment confirm | increases by full payment | increases by unapplied part | increases by full payment | allocations reduce it |
| Expense create/finalize | unchanged | unchanged | unchanged | unchanged |
| Disbursement finalize | unchanged | unchanged | decreases | unchanged |
| Unit opening credit | increases | increases | unchanged | unchanged |
| Fund opening credit | unchanged | unchanged | increases | unchanged |
| Credit settlement | unchanged | decreases | unchanged | decreases |

FinancialTransactionEntries exist for CurrentBalance changes, not for a credit settlement that only assigns already-received Unit credit.

## Concurrency and idempotency

- EF rowversion conflicts become stable `concurrency.conflict` responses.
- Disbursements touch their parent Expense so concurrent payments against the same Expense serialize through its rowversion.
- Gateway reference and offline bank tracking uniqueness are DB-backed.
- Credit settlement is idempotent by `(UnitAccountId, RequestId)`; exact semantic replay succeeds, changed semantics conflict, and recognized persistence/concurrency races attempt replay.
- Client-driven ETag/If-Match is not yet implemented.

## Files and security

Only relative storage keys are persisted. Generated physical filenames prevent disclosure of object IDs or original names. The upload directory is not public static content. Database backup does not include file bytes; both SQL and storage root require backup.

Authentication and scoped authorization are implemented through the IAM module. Customer OTP login, database-backed sessions, Invitations, Memberships, recovery, Platform/support acting, Membership exits, and narrow individual AccessGrants are available. This is scoped resource authorization, not multitenancy; do not describe the service as tenant-isolated. Read `docs/identity-access-management.md` before changing IAM behavior.

## Development and validation

See the root README for setup. The normal quality gate is restore, build, default tests and format verification. SQL integration tests require `RUN_SQLSERVER_INTEGRATION_TESTS=true` and Docker. Default tests intentionally remain usable without Docker.

Development seeding creates Persian reference/demo data. Never enable demo seeding in production. Never place a real credential in appsettings or documentation.

## Where to continue

Before starting a new phase:

1. Confirm the current branch and clean/dirty status.
2. Read relevant docs and ADRs.
3. Inspect existing patterns in the closest implemented feature.
4. State what is inside and outside scope.
5. Avoid designing future domains opportunistically.
6. Preserve public code/key conventions and schemas.
7. Add persisted-state integration coverage for critical invariants.
8. Update this context if a decision changes.

The next product phase has not been authorized merely because it appears in the roadmap. Notification delivery, SSO/MFA, reporting, localization, multitenancy and advanced financial reversals remain deferred until explicitly requested.
