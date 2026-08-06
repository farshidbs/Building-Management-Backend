# ADR 001: Modular monolith
## Context
Modules share transactions and the product is early.
## Decision
Deploy one ASP.NET Core service and SQL Server database with code boundaries.
## Consequences
Simple operations and transactions; boundaries must be maintained in-process.
## Status
Accepted.
