# Persistent Repository Guidance

Use C#, ASP.NET Core, EF Core SQL Server, and a one-service/one-database modular monolith. Keep Domain independent, Application use-case oriented, Infrastructure persistence-only, and API handlers thin. Inspect patterns before dependencies; prefer the platform. No microservices, generic repositories, MediatR, or AutoMapper without demonstrated need.

Current module: Location, Complex, Building, Unit. Internal primary and foreign keys are SQL Server `bigint IDENTITY`. Never expose them through HTTP contracts. Aggregate entities have an immutable server-generated five-character uppercase alphanumeric public `Code`, unique per table; routes and relationship requests use codes. Reference-data rows use a stable unique semantic `Key` and do not have a public `Code`. Other conventions: UTC DateTimeOffset, explicit DTOs, parent-scoped pagination, IsActive, restricted deletes, rowversion.

Party is a real person/organization; User is a login. Never put changing residents on Unit; future UnitPartyRelation stores history. Charge, Expense, Payment, and immutable ledger entries are distinct. Never alter historical financial snapshots.

Do not incidentally implement future identity, assets, finance, notification, reporting, or localization boundaries. Do not fabricate multitenancy. Money uses decimal plus currency.

Tests cover invariants and real SQL Server; never use EF InMemory as an integration substitute. Propagate cancellation, return RFC 9457 Problem Details, keep secrets out of source, and update docs/ADRs when decisions change.
