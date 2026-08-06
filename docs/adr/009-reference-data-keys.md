# ADR 009: Semantic Keys for Reference Data

## Status

Accepted

## Context

Persisting classifications such as building type and unit status as enum strings makes display labels and future additions part of application deployments. These values need database-managed Persian titles, stable API values, activation, and ordering. Unlike buildings and units, reference-data rows are not independently addressable business resources and do not need support-friendly random codes.

## Decision

Store location types, building types, unit usage types, and unit statuses in dedicated reference tables. Each row has an internal `bigint IDENTITY` primary key and a stable, unique lowercase semantic `Key`. API requests, responses, and filters use that key. Reference rows do not have the five-character public `Code`; that policy remains limited to addressable aggregate entities.

## Consequences

Display titles can change without breaking API clients, and new values can be added without introducing persisted enum strings. Keys are contractual and must not be renamed casually. Numeric reference IDs remain internal, and random code generation and collision handling remain focused on aggregate tables.
