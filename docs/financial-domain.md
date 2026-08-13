# Financial domain

Expense and Demand are deliberately separate. An Expense records a real cost, and finalizing
it does not move money. An `ExpenseDisbursement` is the confirmed payment of that cost and
decreases a Building or Complex Fund. A negative Fund balance is valid and never creates a
Demand automatically.

A Demand is a request for money from Units. Draft and preview operations create no debt.
Finalization freezes allocation snapshots, creates `UnitReceivables`, decreases Unit account
balances, and records immutable `FinancialTransactionEntries`. A monthly charge is a Demand
type, not a separate entity. Redistribution can either preserve the requested total
deterministically (`redistribute_to_others`) or intentionally expose a difference
(`no_redistribution`). Retrying finalization reads the stored snapshot and never recalculates it
from current occupancy data.

The financial account belongs to a Unit, Building, or Complex—not to a Party. The responsible
Party is a historical responsibility snapshot and is independent from the Party who later pays.
A Receivable has its own public Code and may originate from either a finalized Demand allocation
or an opening-debt AccountAdjustment. A confirmed Payment may partially settle one or more
Receivables, may be made by any Party, and
may exceed selected debt; the excess remains Unit credit. Payment confirmation increases both
the Unit account and the receiving Fund. Gateway callbacks resolve a Payment by its unique
`GatewayReference`; repeated callbacks are idempotent. An ordinary Payment request cannot submit
a GatewayReference. Manual confirmation accepts only `waiting_for_approval`; trusted online
confirmation is an application-only `ITrustedPaymentResultProcessor` capability with no public
HTTP endpoint until a real provider adapter exists.

Every finalized balance mutation updates `FinancialAccount.CurrentBalance` and creates a
`FinancialTransaction` plus one or more immutable `FinancialTransactionEntries` atomically.
There is no `LedgerEntries`, `ExpenseFunding`, or polymorphic `EntityType/EntityId` relation.
Expense and Demand links use explicit FK junction tables. `DemandExpenses` is traceability only
and never represents a money movement.

All Financial-domain tables use the `bms` schema. Financial document relations reuse the
existing `base.StoredFiles` table. Foreign-key deletes are restricted to protect history, money
uses `decimal(18,2)`, and aggregate mutations use SQL Server rowversion concurrency.

Opening debt and credit use `AccountAdjustment`. Opening debt requires an explicit compatible
Building/Complex destination Fund and atomically decreases the Unit account while creating a
payable `UnitReceivable` whose origin is `AccountAdjustmentId`. Partial payment, full payment and
overpayment use the same Payment flow as Demand debt. Advanced reversal/correction workflows, personal
manager advances or Party funding of a Fund, and a real payment-gateway provider integration
are intentionally deferred. Demand types remain system reference data; scoped custom Demand
types are a possible future extension.
