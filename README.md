# Building Management Backend

Production-oriented .NET 10 modular monolith implementing hierarchical physical structure,
file management, reusable Parties, historical Unit relationships, and transactional occupancy.

## Party and occupancy

Party is separate from a future User account and may be created with only `partyTypeKey` and
`displayName`. Mobile, email, and the directly stored `IdentityNumber` are optional and can be added later.

Unit onboarding explicitly supplies vacant or occupied state. `CurrentOccupantsCount` is
returned with Unit reads, while `UnitOccupancyHistory` preserves changes. Occupancy updates
must use the dedicated history endpoint so current count, history, and tenant/resident
relations remain transactionally consistent.

## Identifier policy

Database primary/foreign keys are internal SQL Server `bigint IDENTITY` values and are never exposed by the HTTP API. Addressable resources receive an immutable server-generated five-character public `Code` containing only `A-Z` and `0-9`. Codes are unique within their table and are used in URLs, filters, requests, responses, support conversations, and client bookmarks. Reference-data rows use stable unique semantic keys such as `residential` and intentionally do not receive random public codes.

## Setup

Requires .NET 10 SDK, Docker Desktop, and `dotnet-ef`. Copy `.env.example` to `.env`, replace its placeholder, then:

```powershell
docker compose up -d sqlserver
dotnet restore BuildingManagement.slnx
dotnet ef database update --project src/BuildingManagement.Infrastructure --startup-project src/BuildingManagement.Api
dotnet run --project src/BuildingManagement.Api
```

Use `docker compose up --build` for the full stack. Swagger UI is `/swagger`, OpenAPI is `/openapi/v1.json`, and database readiness is `/health` in Development. Never enable development seed in production.

## Verify

```powershell
dotnet build BuildingManagement.slnx --no-restore
dotnet test BuildingManagement.slnx --no-build
$env:RUN_SQLSERVER_INTEGRATION_TESTS="true"; dotnet test tests/BuildingManagement.IntegrationTests
dotnet format BuildingManagement.slnx --verify-no-changes
```

Integration tests use a real SQL Server Testcontainer, never EF InMemory. They are skipped by default and also skip gracefully when the opted-in Docker/SQL Server infrastructure cannot start.

`Domain` holds invariants; `Application` DTOs/use cases; `Infrastructure` EF Core SQL Server; `Api` versioned Minimal APIs, Problem Details, Swagger/OpenAPI, CORS, correlation, and health.

`ConnectionStrings__BuildingManagement` is required. Local secrets use .NET User Secrets or environment variables and are not committed. `Cors__AllowedOrigins__0` etc. configure explicit origins.

Known limitations: no authentication/tenant isolation, deep location-cycle detection, localization tables, bulk import, or client-driven optimistic concurrency token. RowVersion is enforced by EF Core for changes tracked during one request, but it is not yet exposed to clients, so sequential stale updates remain last-write-wins. Next planned: Party/User invitation and historical `UnitPartyRelation`.

See [domain overview](docs/domain-overview.md), [data model](docs/data-model.md), [API conventions](docs/api-conventions.md), and [roadmap](docs/roadmap.md).

## File storage

The first file-management module supports image galleries and documents for buildings and complexes. Metadata is stored in SQL Server; bytes use `IFileStorage` with the local provider by default. Configure it with:

```text
FileStorage__Provider=Local
FileStorage__LocalRootPath=fileuploads
FileStorage__MaximumImageFileSizeBytes=10485760
FileStorage__MaximumDocumentFileSizeBytes=26214400
```

Relative roots resolve from the API content root. In containers, mount this directory as a persistent volume. Do not expose it as a static-files directory. Allowed gallery formats are jpg/jpeg/png/webp; documents allow pdf, images, doc/docx, and xls/xlsx. Both extension and declared content type are validated.

Apply migration `AddFileManagement` before using these endpoints. Document types are seeded and returned by `GET /api/v1/reference-data`. Uploads use `multipart/form-data`; metadata updates use JSON and never replace file bytes. File content is streamed by public file code through `GET /api/v1/files/{fileCode}/content`.

See [ADR 010](docs/adr/010-file-storage.md) for storage, cleanup, backup, and future object-storage decisions.
