# ADR 005: Separate financial concepts and immutable balance evidence

## Context

Requested charges, incurred costs, actual Fund disbursements, received money, Unit debt and unapplied credit are different facts. Combining them would make balances and historical support investigation unreliable.

## Decision

- Expense records a cost; ExpenseDisbursement records actual Fund outflow.
- Demand finalization creates allocation snapshots and UnitReceivables.
- Payment records received money and explicit Receivable allocations.
- FinancialTransaction and immutable entries explain CurrentBalance changes.
- Unit AvailableCredit stores received but unapplied value and is consumed only by explicit UnitCreditSettlement.
- Credit settlement creates no transaction entry because it changes neither CurrentBalance nor Fund.
- Financial source links use explicit foreign keys, not polymorphic EntityType/EntityId.

## Consequences

Balances remain auditable and historical allocations are not overwritten. Fund money is pooled, credit remains Unit-owned, and Party responsibility/payer facts do not create Party wallets.

## Status

Accepted and implemented. Refund/reversal/cancellation accounting, real gateway adapters, automatic credit consumption and owner/tenant subaccounts remain deferred.
