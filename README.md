# Building Management Backend

Production-oriented .NET 10 modular monolith. Phase 1 implements hierarchical locations, complexes, buildings, and units.

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

Integration tests use a real SQL Server Testcontainer, never EF InMemory.

`Domain` holds invariants; `Application` DTOs/use cases; `Infrastructure` EF Core SQL Server; `Api` versioned Minimal APIs, Problem Details, Swagger/OpenAPI, CORS, correlation, and health.

`ConnectionStrings__BuildingManagement` is required. Local secrets use .NET User Secrets or environment variables and are not committed. `Cors__AllowedOrigins__0` etc. configure explicit origins.

Known limitations: no authentication/tenant isolation, deep location-cycle detection, localization tables, or bulk import. Next planned: Party/User invitation and historical `UnitPartyRelation`.

See [domain overview](docs/domain-overview.md), [data model](docs/data-model.md), [API conventions](docs/api-conventions.md), and [roadmap](docs/roadmap.md).
