# ADR 007: No generic repository
## Context
EF Core already provides unit-of-work and repository behavior.
## Decision
Expose a narrow application DbContext abstraction and write explicit use cases.
## Consequences
Queryable persistence remains available without redundant CRUD abstractions.
## Status
Accepted.
