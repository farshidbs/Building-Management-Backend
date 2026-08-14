# ADR 004: Historical unit relations
## Context
Owners, tenants, and residents change.
## Decision
Do not store them on Unit; UnitPartyRelation carries type and effective dates.
## Consequences
History is preserved and current occupancy becomes a temporal query.
## Status
Accepted and implemented by UnitPartyRelation and UnitOccupancyHistory.
