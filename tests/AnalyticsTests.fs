namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module AnalyticsTests =

    open Analytics
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    // ── helpers ───────────────────────────────────────────────────────────────

    /// create a simple monthly repayment loan with the given annual interest rate (as a decimal percent)
    let private monthlyParams (annualRatePercent: decimal) : BasicParameters =
        let startDate = Date(2025, 1, 1)
        {
            EvaluationDate = startDate
            StartDate      = startDate
            Principal      = 1000_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2025, 1, 31)
                    ScheduleLength   = PaymentCount 12
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding           = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method       = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual (Percent annualRatePercent)
                Cap          = Interest.Cap.zero
                Rounding     = RoundDown
                AprMethod    = Apr.CalculationMethod.UnitedKingdom 4
            }
        }

    // ── Currency tests ────────────────────────────────────────────────────────

    module CurrencyTests =

        [<Fact>]
        let ``GBP fromMajorUnit converts 12.34 to 1234 minor units`` () =
            let result = Currency.fromMajorUnit Currency.GBP 12.34m
            result |> should equal 1234L<Cent>

        [<Fact>]
        let ``GBP toMajorUnit converts 1234 minor units to 12.34`` () =
            let result = Currency.toMajorUnit Currency.GBP 1234L<Cent>
            result |> should equal 12.34m

        [<Fact>]
        let ``JPY fromMajorUnit converts 1234 to 1234 minor units (no decimal places)`` () =
            let result = Currency.fromMajorUnit Currency.JPY 1234m
            result |> should equal 1234L<Cent>

        [<Fact>]
        let ``JPY toMajorUnit converts 1234 minor units to 1234`` () =
            let result = Currency.toMajorUnit Currency.JPY 1234L<Cent>
            result |> should equal 1234m

        [<Fact>]
        let ``KWD fromMajorUnit converts 1.234 to 1234 minor units (3 decimal places)`` () =
            let result = Currency.fromMajorUnit Currency.KWD 1.234m
            result |> should equal 1234L<Cent>

        [<Fact>]
        let ``KWD toMajorUnit converts 1234 minor units to 1.234`` () =
            let result = Currency.toMajorUnit Currency.KWD 1234L<Cent>
            result |> should equal 1.234m

        [<Fact>]
        let ``USD fromMajorUnit converts 99.99 to 9999 minor units`` () =
            let result = Currency.fromMajorUnit Currency.USD 99.99m
            result |> should equal 9999L<Cent>

        [<Fact>]
        let ``EUR fromMajorUnit rounds 0.005 to 1 minor unit (AwayFromZero)`` () =
            let result = Currency.fromMajorUnit Currency.EUR 0.005m
            result |> should equal 1L<Cent>

        [<Fact>]
        let ``JPY minorUnitFactor is 1`` () =
            let result = Currency.minorUnitFactor Currency.JPY
            result |> should equal 1m

        [<Fact>]
        let ``KWD minorUnitFactor is 1000`` () =
            let result = Currency.minorUnitFactor Currency.KWD
            result |> should equal 1000m

        [<Fact>]
        let ``defaultCurrency is GBP`` () =
            (Currency.defaultCurrency = Currency.GBP) |> should equal true

    // ── Loan Comparison tests ─────────────────────────────────────────────────

    module LoanComparisonTests =

        [<Fact>]
        let ``compare two offers returns both in Offers array`` () =
            let offers = [|
                "Lender A", monthlyParams 10m
                "Lender B", monthlyParams 15m
            |]
            let result = LoanComparison.compare offers
            result.Offers |> Array.length |> should equal 2

        [<Fact>]
        let ``lower rate offer has lower TotalCostOfCredit`` () =
            let offerA = "Lender A", monthlyParams 10m
            let offerB = "Lender B", monthlyParams 15m
            let result = LoanComparison.compare [| offerA; offerB |]
            let a = result.Offers |> Array.find (fun o -> o.Name = "Lender A")
            let b = result.Offers |> Array.find (fun o -> o.Name = "Lender B")
            (a.Metrics.TotalCostOfCredit < b.Metrics.TotalCostOfCredit) |> should equal true

        [<Fact>]
        let ``lower rate offer is first in RankedByTotalCostOfCredit`` () =
            let offers = [| "Lender B", monthlyParams 15m; "Lender A", monthlyParams 10m |]
            let result = LoanComparison.compare offers
            result.RankedByTotalCostOfCredit[0].Name |> should equal "Lender A"

        [<Fact>]
        let ``lower rate offer is first in RankedByApr`` () =
            let offers = [| "Lender B", monthlyParams 15m; "Lender A", monthlyParams 10m |]
            let result = LoanComparison.compare offers
            result.RankedByApr[0].Name |> should equal "Lender A"

        [<Fact>]
        let ``lower rate offer is first in RankedByTotalAmountPayable`` () =
            let offers = [| "Lender B", monthlyParams 15m; "Lender A", monthlyParams 10m |]
            let result = LoanComparison.compare offers
            result.RankedByTotalAmountPayable[0].Name |> should equal "Lender A"

        [<Fact>]
        let ``lower rate offer is first in RankedByLevelPayment`` () =
            let offers = [| "Lender B", monthlyParams 15m; "Lender A", monthlyParams 10m |]
            let result = LoanComparison.compare offers
            result.RankedByLevelPayment[0].Name |> should equal "Lender A"

        [<Fact>]
        let ``TotalAmountPayable equals TotalCostOfCredit plus Principal`` () =
            let bp = monthlyParams 12m
            let result = LoanComparison.compare [| "Test", bp |]
            let offer = result.Offers[0]
            offer.Metrics.TotalAmountPayable - offer.Metrics.TotalCostOfCredit
            |> should equal bp.Principal

        [<Fact>]
        let ``compare with three offers returns correctly ranked list`` () =
            let offers = [|
                "C", monthlyParams 20m
                "A", monthlyParams 10m
                "B", monthlyParams 15m
            |]
            let result = LoanComparison.compare offers
            result.RankedByTotalCostOfCredit |> Array.map _.Name |> should equal [| "A"; "B"; "C" |]

    // ── Sensitivity Analysis tests ────────────────────────────────────────────

    module SensitivityAnalysisTests =

        [<Fact>]
        let ``analyse returns one scenario per adjustment`` () =
            let adjustments = [| Percent 0m; Percent 1m; Percent 2m; Percent 3m |]
            let result = SensitivityAnalysis.analyse (monthlyParams 10m) adjustments
            result |> Array.length |> should equal 4

        [<Fact>]
        let ``zero adjustment scenario matches base parameters`` () =
            let bp = monthlyParams 10m
            let result = SensitivityAnalysis.analyse bp [| Percent 0m |]
            let scenario = result[0]
            let baseSchedule = calculateBasicSchedule bp
            scenario.TotalAmountPayable |> should equal baseSchedule.Stats.ScheduledPaymentTotal

        [<Fact>]
        let ``TotalCostOfCredit increases as rate adjustment increases`` () =
            let adjustments = [| Percent 0m; Percent 1m; Percent 2m; Percent 3m |]
            let result = SensitivityAnalysis.analyse (monthlyParams 10m) adjustments
            let costs = result |> Array.map _.TotalCostOfCredit
            costs
            |> Array.pairwise
            |> Array.forall (fun (a, b) -> b >= a)
            |> should equal true

        [<Fact>]
        let ``LevelPayment increases as rate adjustment increases`` () =
            let adjustments = [| Percent 0m; Percent 1m; Percent 2m; Percent 3m |]
            let result = SensitivityAnalysis.analyse (monthlyParams 10m) adjustments
            let payments = result |> Array.map _.LevelPayment
            payments
            |> Array.pairwise
            |> Array.forall (fun (a, b) -> b >= a)
            |> should equal true

        [<Fact>]
        let ``RateAdjustment is preserved in scenario`` () =
            let adjustments = [| Percent 1m; Percent 2m; Percent 3m |]
            let result = SensitivityAnalysis.analyse (monthlyParams 10m) adjustments
            result |> Array.map _.RateAdjustment |> should equal adjustments

        [<Fact>]
        let ``EffectiveRate reflects base plus adjustment for Annual rate`` () =
            let bp = monthlyParams 10m  // Annual 10%
            let result = SensitivityAnalysis.analyse bp [| Percent 5m |]
            let scenario = result[0]
            (scenario.EffectiveRate = Interest.Rate.Annual (Percent 15m)) |> should equal true

        [<Fact>]
        let ``negative adjustment decreases TotalCostOfCredit`` () =
            let bp = monthlyParams 10m
            let adjustments = [| Percent 0m; Percent -1m |]
            let result = SensitivityAnalysis.analyse bp adjustments
            (result[1].TotalCostOfCredit <= result[0].TotalCostOfCredit) |> should equal true

        [<Fact>]
        let ``TotalCostOfCredit equals TotalAmountPayable minus Principal`` () =
            let bp = monthlyParams 10m
            let adjustments = [| Percent 0m; Percent 1m; Percent 2m |]
            let result = SensitivityAnalysis.analyse bp adjustments
            result
            |> Array.forall (fun s -> s.TotalAmountPayable - s.TotalCostOfCredit = bp.Principal)
            |> should equal true
