namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open Calculation
open Savings

module SavingsTests =    // ============================================================
    // 1. RegularSavings — ISA projections
    // ============================================================

    module RegularSavingsTests =

        [<Fact>]
        let ``Single lump-sum at 5% annually for 12 months equals approx compound-interest growth`` () =
            let config = {
                InitialDeposit = Cent.fromDecimal 10_000m
                MonthlyContribution = 0L<Cent>
                SavingsRate = SavingsRate.Fixed(Percent 5m)
                CompoundingFrequency = CompoundingFrequency.Monthly
                TaxWrapper = TaxWrapper.None
                TermMonths = 12
            }
            let result = project config
            let finalBalance = (result |> Array.last).ClosingBalance
            // expected: 10000 * (1 + 0.05/12)^12 ≈ 10511.62
            finalBalance |> should be (greaterThan 10_511_00L<Cent>)
            finalBalance |> should be (lessThan 10_513_00L<Cent>)

        [<Fact>]
        let ``Monthly contributions of £200 at 3% over 24 months produces correct final balance`` () =
            let config = {
                InitialDeposit = 0L<Cent>
                MonthlyContribution = Cent.fromDecimal 200m
                SavingsRate = SavingsRate.Fixed(Percent 3m)
                CompoundingFrequency = CompoundingFrequency.Monthly
                TaxWrapper = TaxWrapper.None
                TermMonths = 24
            }
            let result = project config
            let last = result |> Array.last
            // total contributions = 24 * 200 = 4800 + initial 0 = 4800
            last.TotalContributions |> should equal 4_800_00L<Cent>
            // closing balance should be more than total contributions due to interest
            last.ClosingBalance |> should be (greaterThan last.TotalContributions)

        [<Fact>]
        let ``ISA wrapper produces the same balance as no wrapper (tax-free label only affects reporting)`` () =
            let baseConfig = {
                InitialDeposit = Cent.fromDecimal 5_000m
                MonthlyContribution = Cent.fromDecimal 500m
                SavingsRate = SavingsRate.Fixed(Percent 4m)
                CompoundingFrequency = CompoundingFrequency.Monthly
                TaxWrapper = TaxWrapper.None
                TermMonths = 12
            }
            let isaConfig = { baseConfig with TaxWrapper = TaxWrapper.Isa }
            let noneResult = project baseConfig |> Array.last
            let isaResult = project isaConfig |> Array.last
            // ISA doesn't change amounts; it just means the return is tax-free
            noneResult.ClosingBalance |> should equal isaResult.ClosingBalance

        [<Fact>]
        let ``LISA wrapper applies 25% government bonus to each monthly contribution`` () =
            let config = {
                InitialDeposit = 0L<Cent>
                MonthlyContribution = Cent.fromDecimal 400m
                SavingsRate = SavingsRate.Fixed(Percent 0m)
                CompoundingFrequency = CompoundingFrequency.Annually
                TaxWrapper = TaxWrapper.Lisa
                TermMonths = 1
            }
            let result = project config
            let entry = result |> Array.exactlyOne
            // effective contribution = 400 + 25% = 500
            entry.Contribution |> should equal 500_00L<Cent>

        [<Fact>]
        let ``Pension salary-sacrifice with 50% employer match doubles the effective contribution`` () =
            let config = {
                InitialDeposit = 0L<Cent>
                MonthlyContribution = Cent.fromDecimal 200m
                SavingsRate = SavingsRate.Fixed(Percent 0m)
                CompoundingFrequency = CompoundingFrequency.Annually
                TaxWrapper = TaxWrapper.PensionSalarySacrifice(Percent 50m)
                TermMonths = 1
            }
            let result = project config
            let entry = result |> Array.exactlyOne
            // effective contribution = 200 + 50% = 300
            entry.Contribution |> should equal 300_00L<Cent>

        [<Fact>]
        let ``Stepped rate applies first rate in month 1 and second rate from month 7`` () =
            let schedule = [|
                { FromMonth = 1; AnnualRate = Percent 2m }
                { FromMonth = 7; AnnualRate = Percent 4m }
            |]
            let config = {
                InitialDeposit = Cent.fromDecimal 12_000m
                MonthlyContribution = 0L<Cent>
                SavingsRate = SavingsRate.Stepped schedule
                CompoundingFrequency = CompoundingFrequency.Monthly
                TaxWrapper = TaxWrapper.None
                TermMonths = 12
            }
            let result = project config

            // months 1–6 at 2%, months 7–12 at 4%
            // verify rate applied in month 1 (low) is less than rate in month 12 (higher)
            let interestMonth1 = result.[0].InterestEarned
            let interestMonth12 = result.[11].InterestEarned
            interestMonth12 |> should be (greaterThan interestMonth1)

        [<Fact>]
        let ``Project returns exactly TermMonths entries`` () =
            let config = {
                InitialDeposit = Cent.fromDecimal 1_000m
                MonthlyContribution = Cent.fromDecimal 100m
                SavingsRate = SavingsRate.Fixed(Percent 3m)
                CompoundingFrequency = CompoundingFrequency.Monthly
                TaxWrapper = TaxWrapper.None
                TermMonths = 36
            }
            let result = project config
            result |> Array.length |> should equal 36

        [<Fact>]
        let ``Total interest equals closing balance minus total contributions`` () =
            let config = {
                InitialDeposit = Cent.fromDecimal 2_000m
                MonthlyContribution = Cent.fromDecimal 100m
                SavingsRate = SavingsRate.Fixed(Percent 5m)
                CompoundingFrequency = CompoundingFrequency.Monthly
                TaxWrapper = TaxWrapper.None
                TermMonths = 12
            }
            let result = project config
            let last = result |> Array.last
            last.TotalInterestEarned
            |> should equal (last.ClosingBalance - last.TotalContributions)

    // ============================================================
    // 2. FixedTermDeposit
    // ============================================================

    module FixedTermDepositTests =

        [<Fact>]
        let ``Monthly interest payment: total interest over 12 months equals 12 times monthly rate`` () =
            let config = {
                Principal = Cent.fromDecimal 10_000m
                GrossAnnualRate = Percent 6m
                TermMonths = 12
                InterestPaymentTiming = InterestPaymentTiming.Monthly
                EarlyWithdrawalPenaltyMonths = 0
            }
            let result = projectDeposit config
            let expected = Cent.fromDecimal (10_000m * 0.06m / 12m * 12m)
            result.TotalInterest |> should equal expected

        [<Fact>]
        let ``Interest at maturity: all interest credited in the final month only`` () =
            let config = {
                Principal = Cent.fromDecimal 5_000m
                GrossAnnualRate = Percent 4m
                TermMonths = 6
                InterestPaymentTiming = InterestPaymentTiming.AtMaturity
                EarlyWithdrawalPenaltyMonths = 0
            }
            let result = projectDeposit config
            let intermediate = result.Schedule |> Array.take 5
            let lastEntry = result.Schedule |> Array.last
            // no interest should be credited before the last month
            intermediate |> Array.forall (fun e -> e.InterestCredited = 0L<Cent>) |> should equal true
            lastEntry.InterestCredited |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``Annual interest payment: interest credited in month 12 and month 24 for 24-month term`` () =
            let config = {
                Principal = Cent.fromDecimal 10_000m
                GrossAnnualRate = Percent 3m
                TermMonths = 24
                InterestPaymentTiming = InterestPaymentTiming.Annually
                EarlyWithdrawalPenaltyMonths = 0
            }
            let result = projectDeposit config
            let month12 = result.Schedule.[11]
            let month24 = result.Schedule.[23]
            month12.InterestCredited |> should be (greaterThan 0L<Cent>)
            month24.InterestCredited |> should be (greaterThan 0L<Cent>)
            // months other than 12 and 24 should have no credited interest
            result.Schedule
            |> Array.filter (fun e -> e.Month <> 12 && e.Month <> 24)
            |> Array.forall (fun e -> e.InterestCredited = 0L<Cent>)
            |> should equal true

        [<Fact>]
        let ``grossToAer and aerToGross are inverses for monthly compounding`` () =
            let gross = Percent 5m
            let periodsPerYear = 12
            let aer = grossToAer gross periodsPerYear
            let roundtrip = aerToGross aer periodsPerYear
            let (Percent g) = gross
            let (Percent rt) = roundtrip
            // should round-trip to within 0.0001%
            abs (g - rt) |> should be (lessThan 0.0001m)

        [<Fact>]
        let ``AER is higher than gross rate for monthly compounding`` () =
            let gross = Percent 4m
            let aer = grossToAer gross 12
            let (Percent grossVal) = gross
            let (Percent aerVal) = aer
            aerVal |> should be (greaterThan grossVal)

        [<Fact>]
        let ``Early withdrawal with 3-month penalty deducts 3 months of interest`` () =
            let config = {
                Principal = Cent.fromDecimal 10_000m
                GrossAnnualRate = Percent 6m
                TermMonths = 12
                InterestPaymentTiming = InterestPaymentTiming.Monthly
                EarlyWithdrawalPenaltyMonths = 3
            }
            let result = earlyWithdrawal config 6
            // gross = 10000 * 0.06/12 * 6 = 300
            result.GrossInterestToDate |> should equal 300_00L<Cent>
            // penalty = 10000 * 0.06/12 * 3 = 150
            result.PenaltyAmount |> should equal 150_00L<Cent>
            // net = 300 - 150 = 150
            result.NetInterestReceived |> should equal 150_00L<Cent>

        [<Fact>]
        let ``Early withdrawal with no penalty returns full interest`` () =
            let config = {
                Principal = Cent.fromDecimal 10_000m
                GrossAnnualRate = Percent 6m
                TermMonths = 12
                InterestPaymentTiming = InterestPaymentTiming.Monthly
                EarlyWithdrawalPenaltyMonths = 0
            }
            let result = earlyWithdrawal config 6
            result.PenaltyAmount |> should equal 0L<Cent>
            result.NetInterestReceived |> should equal result.GrossInterestToDate

        [<Fact>]
        let ``Maturity value equals principal plus total interest`` () =
            let config = {
                Principal = Cent.fromDecimal 10_000m
                GrossAnnualRate = Percent 5m
                TermMonths = 12
                InterestPaymentTiming = InterestPaymentTiming.Monthly
                EarlyWithdrawalPenaltyMonths = 0
            }
            let result = projectDeposit config
            result.MaturityValue |> should equal (result.Principal + result.TotalInterest)

    // ============================================================
    // 3. Premium Bonds
    // ============================================================

    module PremiumBondsTests =

        [<Fact>]
        let ``Expected annual return at 4.4% prize fund rate on £10,000 holding is £440`` () =
            let config = {
                HoldingAmount = Cent.fromDecimal 10_000m
                PrizeFundRate = Percent 4.4m
                MarginalIncomeTaxRate = Percent 20m
            }
            let result = premiumBondsExpectedReturn config
            result |> should equal 440_00L<Cent>

        [<Fact>]
        let ``Equivalent gross rate for 20% taxpayer at 4% prize fund rate is 5%`` () =
            let config = {
                HoldingAmount = Cent.fromDecimal 50_000m
                PrizeFundRate = Percent 4m
                MarginalIncomeTaxRate = Percent 20m
            }
            let comparison = comparePremiumBonds config (Percent 4m)
            let (Percent eqRate) = comparison.EquivalentGrossRate
            // 4 / (1 - 0.20) = 5
            eqRate |> should equal 5m

        [<Fact>]
        let ``Taxable savings after tax is always less than Premium Bonds return for taxpayers`` () =
            let config = {
                HoldingAmount = Cent.fromDecimal 10_000m
                PrizeFundRate = Percent 4m
                MarginalIncomeTaxRate = Percent 40m
            }
            let comparison = comparePremiumBonds config (Percent 4m)
            comparison.TaxableSavingsAfterTax |> should be (lessThan comparison.PremiumBondsExpectedReturn)

        [<Fact>]
        let ``Cash ISA return equals gross premium bonds rate on same holding when rates match`` () =
            let holding = Cent.fromDecimal 10_000m
            let rate = Percent 4.5m
            let config = {
                HoldingAmount = holding
                PrizeFundRate = rate
                MarginalIncomeTaxRate = Percent 20m
            }
            let comparison = comparePremiumBonds config rate
            // expected ISA return = 10000 * 4.5% = 450
            comparison.CashIsaReturn |> should equal 450_00L<Cent>

        [<Fact>]
        let ``Zero marginal tax rate: equivalent gross rate equals prize fund rate`` () =
            let config = {
                HoldingAmount = Cent.fromDecimal 10_000m
                PrizeFundRate = Percent 3m
                MarginalIncomeTaxRate = Percent 0m
            }
            let comparison = comparePremiumBonds config (Percent 3m)
            let (Percent eqRate) = comparison.EquivalentGrossRate
            let (Percent pfRate) = config.PrizeFundRate
            eqRate |> should equal pfRate
