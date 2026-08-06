# ADR 010: Local file storage behind an abstraction

Status: Accepted — 2026-08-06

## Context

The first file-management increment needs building and complex galleries and documents. Production object storage is not yet available, while file metadata and ownership must remain independent of physical storage.

## Decision

`StoredFile` is the storage-neutral metadata record in schema `base`. Building/complex gallery and document records in schema `bms` reference it through real foreign keys. `IFileStorage` is the Application boundary; the first implementation writes to a configured local root.

Only relative storage keys are persisted. Physical names are generated GUID values and never derived from the uploaded name. Writes use a sibling `.uploading` file followed by an atomic rename. Path resolution rejects absolute paths and traversal outside the configured root. The upload root is not exposed as static web content; bytes are streamed only through `/api/v1/files/{fileCode}/content`.

Metadata can be edited without replacing file bytes. Deleting the final relation deactivates orphan metadata and deletes the physical object synchronously. Failures remain diagnosable and return Problem Details. A future object-storage adapter may replace the local implementation without changing domain or HTTP contracts.

## Consequences

Local storage requires a persistent mounted volume in deployments. Database backup alone does not back up file bytes. Operators must back up both SQL Server and the configured storage root. Authorization and malware scanning remain later security increments; this module does not claim those boundaries are complete.
