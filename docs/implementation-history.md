# Implementation History and Decision Trail

This is a functional history, not a replacement for Git. It explains the sequence of work and the intent behind each phase so a new conversation understands why the current model looks this way.

## Phase 0 — Foundation

- Created the .NET layered modular-monolith solution.
- Chose ASP.NET Core Minimal APIs, EF Core SQL Server and one database/service.
- Established RFC Problem Details, cancellation, Swagger, health, CORS and explicit DTOs.
- Rejected generic repository, MediatR and premature microservices.

## Phase 1 — Physical structure

- Implemented Location, Complex, Building and Unit.
- Replaced GUID/public numeric API identity with internal `bigint IDENTITY` plus immutable random five-character public Code.
- Replaced persisted enum-like English values with key-based reference tables and Persian titles.
- Added enriched frontend read DTOs and pagination.
- Adopted short application schema `bms`.
- Loaded and retained Iran country/province/city bootstrap data locally.

## Phase 1 hardening

- Ensured invalid requests return 400 rather than normalization-related 500 errors.
- Made SQL integration tests opt-in and gracefully skippable without infrastructure.
- Squashed the early pre-feature migration chain into a clean initial baseline.
- Documented the current rowversion limitation: server-side tracking exists, HTTP ETag workflow does not.
- Relaxed Building/Complex location validation to realistic business scope.

## File-management increment

- Added `base.StoredFiles` and `IFileStorage` with a safe local provider.
- Added Building/Complex galleries and documents, streamed content, metadata operations and cleanup.
- Replaced ambiguous `ownerCode` inputs with resource-specific route names.
- Storage paths use public parent codes rather than internal IDs.
- Added file URLs to relation responses and DB-backed single-cover constraints.

## Party and occupancy foundation

- Separated Party from future User/login.
- Kept Party creation minimal; IdentityNumber and contacts are optional.
- Simplified away speculative Party identifier tables and payment flags.
- Removed public Code from child/reference/history/relation rows where it had no public-resource use case.
- Kept PartyContact primary and verification state for real contact preference/future invitation needs.
- Defined relation lifecycle through EndDate, with IsActive reserved for soft deletion.
- Added Unit occupancy history and atomic synchronization with CurrentOccupantsCount.
- Standardized Party/occupancy tables in `bms`.

## Asset management

- Added Asset and AssetEvent with Building-or-Complex ownership.
- Added Persian reference/demo data and file relations.
- Kept AssetEvent.Id as an explicit nested-child identifier.
- Made last-event/next-review values derived instead of introducing AssetSchedule.
- Added transactional gallery-cover switching and a filtered unique cover index.
- Kept AssetEvent cost informational and isolated from Finance.

## Structural readability refactor

- Grouped Domain, Application, Infrastructure, API and tests by feature.
- Split large routing/service responsibilities into focused feature files/services.
- Preserved namespaces, architecture, public contracts and persistence behavior.
- Did not introduce new patterns or dependencies.

## Financial domain — initial implementation

- Implemented FinancialAccount, FinancialTransaction and immutable entries.
- Kept Expense, Demand, Payment and AccountAdjustment distinct.
- Added Demand allocation preview/finalization, snapshots and UnitReceivables.
- Added Payment allocation, evidence and manager/trusted-gateway separation.
- Added ExpenseDisbursement as the only Expense workflow that moves Fund money.
- Added Persian reference and demo data.

## Financial hardening passes

- Corrected allocation/responsible-party validation, owner constraints, delete behavior and money precision.
- Added Unit AvailableCredit for unapplied received money and Unit opening credit.
- Added explicit UnitCreditSettlement without fake ledger effects.
- Added RequestId idempotency and exact semantic replay.
- Added historical AvailableCreditAfter snapshot.
- Prevented concurrent Expense over-disbursement by participating in parent Expense rowversion.
- Added DB-backed duplicate protection for offline bank transactions per receiving Fund.
- Preserved actual PaidAtUtc separately from confirmation time and used it for transaction occurrence.
- Completed operational Expense, Payment, Unit payment history, statement source linking and file read models.
- Added Draft Expense update with correct Global/parent-Complex/Building type scope.
- Tightened redistribution policy validation.
- Applied incremental Financial migrations because earlier Financial migrations had already been applied to the shared development/demo database; rewriting applied history was unsafe.

## Financial merge-gate testing

- Added focused persisted-state tests for cross-Unit credit rejection.
- Covered one settlement across multiple Receivables.
- Covered advance and fully allocated payments.
- Covered Unit versus Fund opening credit.
- Covered one Payment to multiple Receivables and multiple Payments to one Receivable.
- Covered concurrent disbursement and concurrent credit RequestId replay.
- Default `dotnet test` remains Docker-independent; opted-in tests use real SQL Server.

## Deliberately deferred

- User, Invitation, authentication, roles and authorization.
- Tenant/organization isolation.
- Automated notification, reminders and Asset schedules.
- Reporting/BI and localization tables.
- Real payment gateway provider adapter.
- Refund/reversal/cancellation accounting workflows.
- Automatic Unit-credit consumption.
- Owner/tenant subaccounts, Party wallets or Party-owned credit.
- Manager personal advances and landlord/tenant private settlement.
- Full multi-currency accounting.

Any future phase must start with a focused prompt, preserve existing invariants, and add an ADR when it changes an accepted decision.
