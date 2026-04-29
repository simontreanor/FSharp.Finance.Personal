namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module MortgageTests =

    open Calculation
    open DateDay
    open Mortgage

    // -------------------------------------------------------------------------
    // Offset mortgage tests
    // -------------------------------------------------------------------------

    [<Fact>]
    let OffsetMortgageTest001 () =
        // £200,000 mortgage, £50,000 in savings, 3% annual rate, 20-year term
        // Net balance = £150,000; verify monthly saving is positive
        let p: OffsetMortgageParameters = {
            MortgageBalance = 200_000_00L<Cent>
            LinkedSavingsBalance = 50_000_00L<Cent>
            AnnualInterestRate = Percent 3m
            RemainingTermMonths = 240
            MonthlyPayment = ValueNone
        }

        let result = calculateOffsetMortgage p

        // net balance should be £150,000
        result.NetInterestBearingBalance |> should equal 150_000_00L<Cent>

        // full payment on £200,000 should be greater than net payment on £150,000
        result.EffectiveMonthlyPayment < result.EffectiveMonthlyPayment + result.MonthlyInterestSaving
        |> should equal true

        // some months should be saved
        result.EstimatedMonthsSaved |> should be (greaterThan 0)

        // total interest on net balance should be less than on full balance
        result.TotalInterestOnNetBalance < result.TotalInterestOnFullBalance
        |> should equal true

    [<Fact>]
    let OffsetMortgageTest002 () =
        // savings exceed mortgage balance – net balance should be zero
        let p: OffsetMortgageParameters = {
            MortgageBalance = 100_000_00L<Cent>
            LinkedSavingsBalance = 150_000_00L<Cent>
            AnnualInterestRate = Percent 2.5m
            RemainingTermMonths = 120
            MonthlyPayment = ValueNone
        }

        let result = calculateOffsetMortgage p

        result.NetInterestBearingBalance |> should equal 0L<Cent>
        result.EstimatedMonthsSaved |> should equal 120

    [<Fact>]
    let OffsetMortgageTest003 () =
        // no linked savings – net balance equals mortgage balance and saving is zero
        let p: OffsetMortgageParameters = {
            MortgageBalance = 150_000_00L<Cent>
            LinkedSavingsBalance = 0L<Cent>
            AnnualInterestRate = Percent 4m
            RemainingTermMonths = 180
            MonthlyPayment = ValueNone
        }

        let result = calculateOffsetMortgage p

        result.NetInterestBearingBalance |> should equal 150_000_00L<Cent>
        result.MonthlyInterestSaving |> should equal 0L<Cent>
        result.EstimatedMonthsSaved |> should equal 0
        (result.TotalInterestOnNetBalance = result.TotalInterestOnFullBalance) |> should equal true

    [<Fact>]
    let OffsetMortgageTest004 () =
        // explicit monthly payment provided
        let p: OffsetMortgageParameters = {
            MortgageBalance = 200_000_00L<Cent>
            LinkedSavingsBalance = 30_000_00L<Cent>
            AnnualInterestRate = Percent 3.5m
            RemainingTermMonths = 300
            MonthlyPayment = ValueSome 100_000L<Cent>  // £1,000/month
        }

        let result = calculateOffsetMortgage p

        result.NetInterestBearingBalance |> should equal 170_000_00L<Cent>
        // monthly saving should be positive since the full payment > net payment
        result.MonthlyInterestSaving |> should be (greaterThanOrEqualTo 0L<Cent>)

    // -------------------------------------------------------------------------
    // Equity release tests
    // -------------------------------------------------------------------------

    [<Fact>]
    let EquityReleaseTest001 () =
        // £100,000 advance at 5% annual rate over 10 years, no repayments
        let p: EquityReleaseParameters = {
            InitialAdvance = 100_000_00L<Cent>
            StartDate = Date(2020, 1, 1)
            AnnualInterestRate = Percent 5m
            ProjectionDate = Date(2030, 1, 1)
            PartialRepayments = [||]
            PropertyValue = ValueNone
        }

        let result = calculateEquityRelease p

        // projected balance should be greater than initial advance
        result.ProjectedBalance > p.InitialAdvance |> should equal true
        // total rolled-up interest should equal the difference
        (result.TotalRolledUpInterest = result.ProjectedBalance - p.InitialAdvance) |> should equal true
        // NNEG not triggered (no property value set)
        result.NnegTriggered |> should equal false

    [<Fact>]
    let EquityReleaseTest002 () =
        // NNEG triggered: property value smaller than projected balance
        let p: EquityReleaseParameters = {
            InitialAdvance = 100_000_00L<Cent>
            StartDate = Date(2000, 1, 1)
            AnnualInterestRate = Percent 7m
            ProjectionDate = Date(2040, 1, 1)
            PartialRepayments = [||]
            PropertyValue = ValueSome 200_000_00L<Cent>
        }

        let result = calculateEquityRelease p

        // after 40 years at 7%, £100k becomes ~£1.5M – well above £200k property value
        result.NnegTriggered |> should equal true
        result.BalanceAfterNneg |> should equal 200_000_00L<Cent>

    [<Fact>]
    let EquityReleaseTest003 () =
        // partial repayment reduces projected balance
        let withoutRepayment: EquityReleaseParameters = {
            InitialAdvance = 100_000_00L<Cent>
            StartDate = Date(2020, 1, 1)
            AnnualInterestRate = Percent 5m
            ProjectionDate = Date(2030, 1, 1)
            PartialRepayments = [||]
            PropertyValue = ValueNone
        }

        let withRepayment: EquityReleaseParameters = {
            withoutRepayment with
                PartialRepayments = [|
                    { RepaymentDate = Date(2025, 1, 1); RepaymentAmount = 10_000_00L<Cent> }
                |]
        }

        let balanceWithout = (calculateEquityRelease withoutRepayment).ProjectedBalance
        let balanceWith = (calculateEquityRelease withRepayment).ProjectedBalance

        balanceWith < balanceWithout |> should equal true

    [<Fact>]
    let EquityReleaseTest004 () =
        // start date equals projection date – balance should equal initial advance
        let p: EquityReleaseParameters = {
            InitialAdvance = 50_000_00L<Cent>
            StartDate = Date(2025, 6, 15)
            AnnualInterestRate = Percent 4m
            ProjectionDate = Date(2025, 6, 15)
            PartialRepayments = [||]
            PropertyValue = ValueNone
        }

        let result = calculateEquityRelease p

        result.ProjectedBalance |> should equal p.InitialAdvance
        result.TotalRolledUpInterest |> should equal 0L<Cent>

    // -------------------------------------------------------------------------
    // Shared ownership tests
    // -------------------------------------------------------------------------

    [<Fact>]
    let SharedOwnershipTest001 () =
        // £200,000 property, 50% share, mortgage on £100,000 at 3%, 25-year term
        // rent on 50% unowned at 2.5% annual rent rate
        let p: SharedOwnershipParameters = {
            PropertyValue = 200_000_00L<Cent>
            InitialShare = Percent 50m
            MortgageBalance = 100_000_00L<Cent>
            MortgageAnnualRate = Percent 3m
            MortgageTermMonths = 300
            AnnualRentRate = Percent 2.5m
            StaircasingTranches = [||]
            HorizonMonths = 60
        }

        let outgoing = calculateSharedOwnershipOutgoing p

        // mortgage payment on £100k at 3% over 25 years
        outgoing.MonthlyMortgagePayment |> should be (greaterThan 0L<Cent>)
        // monthly rent = £200,000 * 50% * 2.5% / 12
        let expectedRent = Cent.round (RoundWith System.MidpointRounding.AwayFromZero) (200_000_00m * 0.5m * 0.025m / 12m)
        outgoing.MonthlyRent |> should equal expectedRent
        // total = mortgage + rent
        (outgoing.TotalMonthlyOutgoing = outgoing.MonthlyMortgagePayment + outgoing.MonthlyRent)
        |> should equal true

    [<Fact>]
    let SharedOwnershipTest002 () =
        // staircasing: buy an additional 25% tranche at a £210,000 property value
        let p: SharedOwnershipParameters = {
            PropertyValue = 200_000_00L<Cent>
            InitialShare = Percent 50m
            MortgageBalance = 95_000_00L<Cent>
            MortgageAnnualRate = Percent 3m
            MortgageTermMonths = 240
            AnnualRentRate = Percent 2.5m
            StaircasingTranches = [||]
            HorizonMonths = 60
        }

        let tranche: StaircasingTranche = {
            PurchaseDate = Date(2026, 6, 1)
            AdditionalShare = Percent 25m
            PropertyValueAtPurchase = 210_000_00L<Cent>
        }

        let result = calculateStaircasing p tranche 95_000_00L<Cent> 240

        // tranche cost = £210,000 * 25% = £52,500
        result.TrancheCost |> should equal 52_500_00L<Cent>
        // new owned share = 50% + 25% = 75%
        (result.NewOwnedShare = Percent 75m) |> should equal true
        // new unowned share = 25%
        (result.NewUnownedShare = Percent 25m) |> should equal true
        // new rent should be lower (only 25% unowned)
        let originalOutgoing = calculateSharedOwnershipOutgoing p
        result.NewMonthlyRent < originalOutgoing.MonthlyRent |> should equal true

    [<Fact>]
    let SharedOwnershipTest003 () =
        // total cost comparison: shared ownership vs outright purchase
        let p: SharedOwnershipParameters = {
            PropertyValue = 200_000_00L<Cent>
            InitialShare = Percent 40m
            MortgageBalance = 80_000_00L<Cent>
            MortgageAnnualRate = Percent 3.5m
            MortgageTermMonths = 300
            AnnualRentRate = Percent 2.75m
            StaircasingTranches = [||]
            HorizonMonths = 120
        }

        let result = calculateTotalCostComparison p

        result.TotalSharedOwnershipCost |> should be (greaterThan 0L<Cent>)
        result.TotalOutrightPurchaseCost |> should be (greaterThan 0L<Cent>)
        (result.CostDifference = result.TotalSharedOwnershipCost - result.TotalOutrightPurchaseCost)
        |> should equal true

    [<Fact>]
    let SharedOwnershipTest004 () =
        // 100% ownership: no rent, mortgage payment only
        let p: SharedOwnershipParameters = {
            PropertyValue = 150_000_00L<Cent>
            InitialShare = Percent 100m
            MortgageBalance = 150_000_00L<Cent>
            MortgageAnnualRate = Percent 3m
            MortgageTermMonths = 180
            AnnualRentRate = Percent 2.5m
            StaircasingTranches = [||]
            HorizonMonths = 60
        }

        let outgoing = calculateSharedOwnershipOutgoing p

        outgoing.MonthlyRent |> should equal 0L<Cent>
        (outgoing.TotalMonthlyOutgoing = outgoing.MonthlyMortgagePayment) |> should equal true
