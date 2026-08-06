# ADR 005: Separate financial concepts
## Context
Requested charges, incurred expenses, received money, and account entries differ.
## Decision
Model Charge, Expense, Payment, and immutable LedgerTransaction separately; snapshot allocation decisions.
## Consequences
More explicit workflows and reliable historical reporting.
## Status
Accepted; implementation deferred.
