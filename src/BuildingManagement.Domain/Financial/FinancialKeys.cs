namespace BuildingManagement.Domain;

public static class FinancialKeys
{
    public static class AccountKinds { public const string Unit = "unit_account"; public const string CurrentFund = "current_fund"; public const string ReserveFund = "reserve_fund"; }
    public static class Statuses { public const string Draft = "draft"; public const string Finalized = "finalized"; public const string Cancelled = "cancelled"; public const string WaitingForApproval = "waiting_for_approval"; public const string Confirmed = "confirmed"; public const string Rejected = "rejected"; public const string GatewayPending = "gateway_pending"; public const string Failed = "failed"; }
    public static class ReceivableStatuses { public const string Open = "open"; public const string PartiallyPaid = "partially_paid"; public const string Paid = "paid"; public const string Cancelled = "cancelled"; }
    public static class Effects { public const string Increase = "increase"; public const string Decrease = "decrease"; }
    public static class TransactionTypes { public const string Demand = "demand"; public const string Payment = "payment"; public const string ExpenseDisbursement = "expense_disbursement"; public const string AccountAdjustment = "account_adjustment"; }
    public static class AllocationMethods { public const string Equal = "equal"; public const string Occupants = "occupants"; public const string Area = "area"; public const string Custom = "custom"; }
    public static class AmountModes { public const string Total = "total_amount"; public const string PerUnit = "per_unit"; public const string PerPerson = "per_person"; public const string PerArea = "per_area"; }
    public static class Redistribution { public const string None = "no_redistribution"; public const string ToOthers = "redistribute_to_others"; }
    public static class ResponsibleParties
    {
        public const string Owner = "owner";
        public const string CurrentOccupant = "current_occupant";
        public static string RequireValid(string? value)
        {
            if (value is not (Owner or CurrentOccupant))
                throw new DomainValidationException("responsiblePartyTypeKey", "Must be owner or current_occupant.");
            return value;
        }
    }
    public static class Adjustments { public const string OpeningDebt = "opening_debt"; public const string OpeningCredit = "opening_credit"; public const string Correction = "correction"; }
}
