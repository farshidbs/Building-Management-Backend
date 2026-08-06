# ADR 002: Internal bigint keys and public codes

## Context

Operators prefer sequential numeric database keys, while exposing predictable numeric IDs in URLs is undesirable and makes support references easy to mistype or enumerate.

## Decision

Use SQL Server `bigint IDENTITY(1,1)` for internal primary and foreign keys. Never expose them through HTTP. Give every entity an immutable, server-generated five-character code using uppercase English letters and digits. Enforce exact format and uniqueness per table in SQL Server. Use codes for routes, relationship inputs, responses, client references, and support.

## Consequences

Database joins and clustered keys remain compact and sequential. Public URLs do not reveal record counts or adjacent IDs. The five-character space has 60,466,176 combinations per table, so generation checks for existing values and the unique database index provides the final concurrency guarantee. Collision conflicts remain possible at high scale and may later justify longer codes; changing code length would require a versioned contract and migration.

## Status

Accepted; supersedes the earlier UUIDv7 decision.
