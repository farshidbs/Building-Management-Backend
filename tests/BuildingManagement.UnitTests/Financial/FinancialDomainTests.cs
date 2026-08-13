using BuildingManagement.Application;
using BuildingManagement.Domain;
using Xunit;

namespace BuildingManagement.UnitTests;

public sealed class FinancialDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void FinancialAccountRequiresExactlyOneOwner()
    {
        Assert.Throws<DomainValidationException>(() =>
            new FinancialAccount(null, null, null, FinancialKeys.AccountKinds.Unit, Now));
        Assert.Throws<DomainValidationException>(() =>
            new FinancialAccount(1, 2, null, FinancialKeys.AccountKinds.Unit, Now));

        var account = new FinancialAccount(null, 1, null, FinancialKeys.AccountKinds.CurrentFund, Now);
        Assert.Equal(-8_000_000m, account.Apply(FinancialKeys.Effects.Decrease, 8_000_000m, Now));
    }

    [Fact]
    public void ExpenseFinalizationDoesNotMoveMoney()
    {
        var expense = new Expense("A1B2C", 1, null, 1, null, "قبض برق", 20_000_000m,
            Now, null, null, Now);

        expense.Finalize(Now);

        Assert.Equal(FinancialKeys.Statuses.Finalized, expense.Status);
    }

    [Fact]
    public void PaymentSupportsIndependentPayerAndOverpayment()
    {
        var payment = new Payment("P1A2Y", 10, 20, 999, 1, 800_000m, null, null, null, Now);
        payment.Submit(false, Now);
        payment.ConfirmGateway("provider-1", Now, Now);

        Assert.Equal(999, payment.PayerPartyId);
        Assert.Equal(FinancialKeys.Statuses.Confirmed, payment.Status);
    }

    [Fact]
    public void DraftPaymentCannotBeConfirmedDirectly()
    {
        var payment = new Payment("P1A2Y", 10, 20, null, 1, 800_000m, null, null, null, Now);

        Assert.Throws<DomainValidationException>(() => payment.ConfirmManual(Now, Now));
    }

    [Fact]
    public void ManualAndGatewayConfirmationPathsCannotCross()
    {
        var manual = new Payment("M1A2N", 1, 2, null, 1, 100m, null, null, null, Now);
        manual.Submit(true, Now);
        Assert.Throws<DomainValidationException>(() => manual.ConfirmGateway("provider", Now, Now));
        manual.ConfirmManual(Now, Now);

        var online = new Payment("O1N2L", 1, 2, null, 1, 100m, null, null, null, Now);
        online.Submit(false, Now);
        Assert.Throws<DomainValidationException>(() => online.ConfirmManual(Now, Now));
        online.ConfirmGateway("provider", Now, Now);
    }

    [Fact]
    public void AccountKindMustMatchOwnerKind()
    {
        Assert.Throws<DomainValidationException>(() =>
            new FinancialAccount(1, null, null, FinancialKeys.AccountKinds.ReserveFund, Now));
        Assert.Throws<DomainValidationException>(() =>
            new FinancialAccount(null, 1, null, FinancialKeys.AccountKinds.Unit, Now));
    }

    [Fact]
    public void TransactionTypeMustMatchExplicitSource()
    {
        Assert.Throws<DomainValidationException>(() => new FinancialTransaction(
            FinancialKeys.TransactionTypes.Payment, 1, null, null, null, Now, null, Now));
        Assert.Throws<DomainValidationException>(() =>
            new FinancialTransactionEntry(1, 1, "invalid", 10, 10, Now));
    }

    [Fact]
    public void RedistributionIsDeterministicAndPreservesTotal()
    {
        var units = FourEqualUnits();
        var rule = new DemandRuleRequest("equal", "total_amount", 8_000_000m, null, true,
            "owner", "redistribute_to_others", null);

        var result = DemandAllocationCalculator.Calculate(units, rule,
            [new("U0004", false, 0, "معاف")]);

        Assert.Equal(8_000_000m, result.FinalTotal);
        Assert.Equal(0, result.Items.Single(x => x.UnitCode == "U0004").FinalAmount);
        Assert.Equal(2_666_666.67m, result.Items[0].FinalAmount);
    }

    [Fact]
    public void OverrideWithoutRedistributionRetainsDifference()
    {
        var rule = new DemandRuleRequest("equal", "total_amount", 8_000_000m, null, true,
            "owner", "no_redistribution", null);

        var result = DemandAllocationCalculator.Calculate(FourEqualUnits(), rule,
            [new("U0004", false, null, "معاف")]);

        Assert.Equal(6_000_000m, result.FinalTotal);
        Assert.Equal(-2_000_000m, result.Difference);
    }

    [Fact]
    public void RedistributionRequiresAtLeastOneRemainingUnit()
    {
        var rule = new DemandRuleRequest("equal", "total_amount", 8_000_000m, null, true,
            "owner", "redistribute_to_others", null);
        var overrides = FourEqualUnits().Select(x =>
            new DemandOverrideRequest(x.UnitCode, false, null, "معاف")).ToList();

        Assert.Throws<AppException>(() => DemandAllocationCalculator.Calculate(FourEqualUnits(), rule, overrides));
    }

    [Fact]
    public void OccupantRateUsesSnapshotBasis()
    {
        var units = new List<AllocationInput>
        {
            new("U0001", 3, true, "current_occupant", null),
            new("U0002", 2, true, "current_occupant", null),
            new("U0003", 0, false, "current_occupant", null)
        };
        var rule = new DemandRuleRequest("occupants", "per_person", null, 100_000m, false,
            "current_occupant", "no_redistribution", null);

        var result = DemandAllocationCalculator.Calculate(units, rule, null);

        Assert.Equal(300_000m, result.Items[0].FinalAmount);
        Assert.Equal(200_000m, result.Items[1].FinalAmount);
        Assert.Equal(0, result.Items[2].FinalAmount);
    }

    [Fact]
    public void ReceivableAllowsPartialButNotExcessAllocation()
    {
        var receivable = new UnitReceivable("R1A2B", 1, 2, 3, null, 790_000m, "owner", null, null, Now);
        receivable.ApplyPayment(780_000m, Now);

        Assert.Equal(10_000m, receivable.OutstandingAmount);
        Assert.Equal(FinancialKeys.ReceivableStatuses.PartiallyPaid, receivable.Status);
        Assert.Throws<DomainValidationException>(() => receivable.ApplyPayment(20_000m, Now));
    }

    [Fact]
    public void AllocationRuleRejectsMixedTotalAndRateValues()
    {
        Assert.Throws<DomainValidationException>(() => new DemandAllocationRule(1, "equal", "total_amount",
            1_000_000m, 100_000m, true, "owner", "no_redistribution", null));
    }

    [Theory]
    [InlineData(FinancialKeys.ResponsibleParties.Owner)]
    [InlineData(FinancialKeys.ResponsibleParties.CurrentOccupant)]
    public void AllocationRuleAcceptsApprovedResponsiblePartyTypes(string responsiblePartyType)
    {
        var rule = new DemandAllocationRule(1, FinancialKeys.AllocationMethods.Equal,
            FinancialKeys.AmountModes.Total, 1_000_000m, null, true, responsiblePartyType,
            FinancialKeys.Redistribution.None, null);
        Assert.Equal(responsiblePartyType, rule.ResponsiblePartyTypeKey);
    }

    [Theory]
    [InlineData("current_ocupant")]
    [InlineData("random")]
    public void FinancialSnapshotsRejectUnknownResponsiblePartyTypes(string responsiblePartyType)
    {
        Assert.Throws<DomainValidationException>(() => new DemandAllocationRule(1,
            FinancialKeys.AllocationMethods.Equal, FinancialKeys.AmountModes.Total, 1_000_000m, null,
            true, responsiblePartyType, FinancialKeys.Redistribution.None, null));
        Assert.Throws<DomainValidationException>(() => new AccountAdjustment("A1B2C", 1, 2,
            FinancialKeys.Adjustments.OpeningDebt, 1_000m, Now, "opening", responsiblePartyType, null, Now));
    }

    private static List<AllocationInput> FourEqualUnits() =>
    [
        new("U0001", 1, true, "owner", null),
        new("U0002", 1, true, "owner", null),
        new("U0003", 1, true, "owner", null),
        new("U0004", 1, true, "owner", null)
    ];
}
