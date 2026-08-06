# API Conventions

- `/api/v1`, plural routes; units list/create under building scope.
- Resources are addressed by immutable five-character uppercase alphanumeric codes, for example `/api/v1/buildings/A7K2P`. Numeric database IDs are never accepted or returned.
- Relationship fields use codes: `parentCode`, `locationCode`, `complexCode`, and `buildingCode`.
- Read responses embed parent summaries as { code, name }: buildings include location and optional complex; units include building and optional complex. This avoids extra client round trips without exposing internal IDs.
- Codes are created by the server; create/update bodies never choose or change them.
- Reference data is read from /api/v1/reference-data. Requests and filters use its stable semantic keys (for example esidential), not numeric IDs or public codes.
- Timestamps are ISO 8601 UTC.
- Pagination defaults 1/20, max 100; envelope includes items and page metadata.
- Filters/search/sort are allow-listed. Code is a supported search/sort value.
- Create returns 201 plus a code-based Location header; read/update return 200; activation/delete return 204.
- RFC 9457 Problem Details include stable error code, traceId, and field errors.

Error codes include `validation.failed`, `{resource}.not_found`, `location.name_conflict`, `building.location_mismatch`, `building.has_units`, `unit.number_conflict`, `persistence.conflict`, and `concurrency.conflict`.

Breaking changes require a new route version.
