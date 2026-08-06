# ADR 008: Separate frontend repository
## Context
Frontend and backend have different build/deployment concerns.
## Decision
This repository contains only the HTTP backend; the frontend consumes versioned APIs.
## Consequences
Independent delivery requires disciplined contracts and CORS configuration.
## Status
Accepted.
