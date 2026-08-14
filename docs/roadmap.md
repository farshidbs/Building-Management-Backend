# Roadmap and Delivery Status

Status only; this is not authorization to implement future work.

## Completed and reviewed

1. Modular-monolith foundation.
2. Physical structure and Iran location data.
3. Validation, migration and integration-test hardening.
4. Building/Complex file management.
5. Party and Unit occupancy history.
6. Asset and AssetEvent management.
7. Structural readability refactor.
8. Financial domain, read side, concurrency/idempotency and merge-gate tests.

## Deferred until explicitly requested

User/Invitation/authentication/authorization; organization isolation; notifications and schedules;
financial refund/reversal/cancellation; real gateway provider; reporting/BI; localization and
deliberate multi-currency; bulk import.

Do not add speculative fields or tables for these items. Start each future phase with a reviewed
domain prompt and update ADRs when decisions change.
