# ADR 011: Party and unit occupancy foundation

Status: Accepted — 2026-08-07

## Context

Managers often know that a unit is occupied before they know a resident's mobile number,
national identifier, or legal role. Requiring complete identity data would block onboarding,
while storing residents directly on Unit would lose history and prevent reuse across units.

## Decision

- Party represents a real person or organization; User remains a future login concept.
- Party requires only a type and display name. Contacts are optional child records. The optional
  `IdentityNumber` is stored directly on Party and omitted from generic list/search responses.
- Party and occupancy feature tables use the application's default `bms` schema.
- Mobile becomes mandatory only in the future Invitation workflow, not Party creation.
- PartyContact is a child entity identified internally and has no public Code. Its verification
  state and timestamp are retained for the future Invitation/User workflow.
- One active primary contact is allowed per Party and contact type. Selecting a new primary is an
  explicit transactional operation and does not require contact verification.
- IdentityNumber remains optional, is not verified, and never merges Parties.
- UnitPartyRelation records independent historical facts such as owner,
  tenant, and resident. An owner who resides in a unit has separate owner and resident facts.
- A relation is current when it is not soft-deleted and `EndDate` is null. Ending a relation sets
  `EndDate` but keeps `IsActive`; `IsActive = false` is reserved for soft deletion.
- Preferred contact selection belongs only to `PartyContact.IsPrimary`, not UnitPartyRelation.
- `Unit.CurrentOccupantsCount` is the fast current snapshot. Zero means vacant and a positive
  value means occupied.
- `UnitOccupancyHistory` is the historical source of truth. The open history row and Unit
  snapshot are changed in one SQL transaction.
- Relationship and occupancy effective dates may be null when the exact date is unknown.
- An occupied Unit requires an active occupancy relation; a vacant Unit has none. Ownership
  may remain active while vacant.
- Legacy UnitStatus values `occupied` and `vacant` are inactive. Operational statuses remain
  separate, and occupancy is derived only from current count.
- Future financial debt remains attached to Unit, not Party.

## Consequences

Generic Unit updates cannot change occupancy. Onboarding and subsequent occupancy changes use
dedicated transactional use cases. Identity verification, User, Invitation, authorization,
deduplication, and finance remain deliberately deferred. Future debt/payment workflows must not
shape Party or UnitPartyRelation before they have a concrete use case.
