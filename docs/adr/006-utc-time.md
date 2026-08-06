# ADR 006: UTC canonical timestamps
## Context
The product spans time zones and calendars.
## Decision
Persist UTC DateTimeOffset; presentation performs calendar conversion.
## Consequences
Unambiguous storage; clients localize.
## Status
Accepted.
