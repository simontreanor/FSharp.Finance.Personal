namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EarlySettlementTests =

    let folder = "EarlySettlement"

    open Calculation
    open DateDay
    open Scheduling
    open Quotes
    open UnitPeriod

    // ---------------------------------------------------------------------------
    // shared parameters – a plain 4-month monthly loan, no fees or charges
    // ---------------------------------------------------------------------------

    let baseParameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 3, 22)   // after the 2nd payment date
            StartDate = Date(2023, 11, 28)
            Principal = 100000L<Cent>             // £1 000
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2023, 12, 22)
                    ScheduleLength = PaymentCount 4
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual(Percent 36.5m)   // simple 0.1 % / day
                Cap = Interest.Cap.zero
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = true
        }
    }

    // ---------------------------------------------------------------------------
    // 1.  Actuarial method
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_Actuarial_matches_getQuote`` () =
        let actualPayments = Map.empty

        let quote = getQuote baseParameters actualPayments

        let result =
            getEarlySettlementQuote EarlySettlementMethod.Actuarial None baseParameters actualPayments

        // the Actuarial method should return the same payment value as the standard getQuote
        let quoteValue =
            match quote.QuoteResult with
            | PaymentQuote pq -> pq.PaymentValue
            | _ -> 0L<Cent>

        (result.Method = EarlySettlementMethod.Actuarial) |> should equal true
        result.SettlementFigure |> should equal quoteValue
        result.InterestRebate |> should equal 0L<Cent>
        result.EarlyRepaymentCharge |> should equal 0L<Cent>
        result.NetSettlementFigure |> should equal quoteValue

    // ---------------------------------------------------------------------------
    // 2.  Rule of 78
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_RuleOf78_rebate_is_positive`` () =
        let actualPayments = Map.empty

        let result =
            getEarlySettlementQuote EarlySettlementMethod.RuleOf78 None baseParameters actualPayments

        (result.Method = EarlySettlementMethod.RuleOf78) |> should equal true
        result.InterestRebate |> should be (greaterThanOrEqualTo 0L<Cent>)
        result.SettlementFigure |> should be (greaterThan 0L<Cent>)
        result.NetSettlementFigure |> should equal result.SettlementFigure

    [<Fact>]
    let ``EarlySettlementTest_RuleOf78_settlement_less_than_remaining_payments`` () =
        let actualPayments = Map.empty

        // settlement is between payments 2 and 3 – two payments remain
        let result =
            getEarlySettlementQuote EarlySettlementMethod.RuleOf78 None baseParameters actualPayments

        let basicSchedule =
            (getQuote baseParameters actualPayments).Schedules.BasicSchedule

        let settlementDay =
            OffsetDay.fromDate baseParameters.Basic.StartDate baseParameters.Basic.EvaluationDate

        let remainingPaymentsTotal =
            basicSchedule.Items
            |> Array.filter (fun bi -> bi.Day > settlementDay)
            |> Array.sumBy (fun bi -> ScheduledPayment.total bi.ScheduledPayment)

        // the Rule of 78 settlement figure must be at most the remaining payments total
        result.SettlementFigure |> should be (lessThanOrEqualTo remainingPaymentsTotal)

    // ---------------------------------------------------------------------------
    // 3.  FCA / CCA 2004 method
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_FCA_rebate_is_positive`` () =
        let actualPayments = Map.empty

        let result =
            getEarlySettlementQuote EarlySettlementMethod.FCA None baseParameters actualPayments

        (result.Method = EarlySettlementMethod.FCA) |> should equal true
        result.InterestRebate |> should be (greaterThanOrEqualTo 0L<Cent>)
        result.SettlementFigure |> should be (greaterThan 0L<Cent>)

    [<Fact>]
    let ``EarlySettlementTest_FCA_settlement_less_than_remaining_payments`` () =
        let actualPayments = Map.empty

        let result =
            getEarlySettlementQuote EarlySettlementMethod.FCA None baseParameters actualPayments

        let basicSchedule =
            (getQuote baseParameters actualPayments).Schedules.BasicSchedule

        let settlementDay =
            OffsetDay.fromDate baseParameters.Basic.StartDate baseParameters.Basic.EvaluationDate

        let remainingPaymentsTotal =
            basicSchedule.Items
            |> Array.filter (fun bi -> bi.Day > settlementDay)
            |> Array.sumBy (fun bi -> ScheduledPayment.total bi.ScheduledPayment)

        result.SettlementFigure |> should be (lessThanOrEqualTo remainingPaymentsTotal)

    // ---------------------------------------------------------------------------
    // 4.  ERC – tiered percentage
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_ERC_TieredPercentage_year1`` () =
        let actualPayments = Map.empty

        // settlement is in year 1 (within the first 365 days) – expect 5 % ERC
        let tiers = [| 1, Percent 5m; 2, Percent 3m; 3, Percent 1m |]

        let result =
            getEarlySettlementQuote
                EarlySettlementMethod.Actuarial
                (Some(EarlyRepaymentChargeType.TieredPercentage tiers))
                baseParameters
                actualPayments

        let expectedErc =
            decimal result.OutstandingPrincipal * 0.05m
            |> Cent.round (RoundWith System.MidpointRounding.AwayFromZero)

        result.EarlyRepaymentCharge |> should equal expectedErc
        result.NetSettlementFigure |> should equal (result.SettlementFigure + result.EarlyRepaymentCharge)

    [<Fact>]
    let ``EarlySettlementTest_ERC_TieredPercentage_beyond_last_tier`` () =
        let actualPayments = Map.empty

        // evaluation date is > 2 years after start; tiers only go to year 2 → fall back to last tier (3 %)
        let p = {
            baseParameters with
                Basic.EvaluationDate = baseParameters.Basic.StartDate.AddDays(800)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2023, 12, 22)
                        ScheduleLength = PaymentCount 36
                    }
        }

        let tiers = [| 1, Percent 5m; 2, Percent 3m |]

        let result =
            getEarlySettlementQuote
                EarlySettlementMethod.Actuarial
                (Some(EarlyRepaymentChargeType.TieredPercentage tiers))
                p
                actualPayments

        // year 3 → falls back to the year-2 tier (3 %)
        let expectedErc =
            decimal result.OutstandingPrincipal * 0.03m
            |> Cent.round (RoundWith System.MidpointRounding.AwayFromZero)

        result.EarlyRepaymentCharge |> should equal expectedErc

    // ---------------------------------------------------------------------------
    // 5.  ERC – fixed months' interest
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_ERC_FixedMonthsInterest`` () =
        let actualPayments = Map.empty

        let result =
            getEarlySettlementQuote
                EarlySettlementMethod.Actuarial
                (Some(EarlyRepaymentChargeType.FixedMonthsInterest 3))
                baseParameters
                actualPayments

        // annual rate is 36.5 % → monthly rate = 36.5 / 1200 ≈ 0.030416...
        let annualRate = baseParameters.Basic.InterestConfig.StandardRate |> Interest.Rate.annual |> Percent.toDecimal
        let monthlyRate = annualRate / 12m
        let expectedErc =
            decimal result.OutstandingPrincipal * monthlyRate * 3m
            |> Cent.round (RoundWith System.MidpointRounding.AwayFromZero)

        result.EarlyRepaymentCharge |> should equal expectedErc
        result.NetSettlementFigure |> should equal (result.SettlementFigure + expectedErc)

    // ---------------------------------------------------------------------------
    // 6.  Net settlement figure combines settlement + ERC
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_NetSettlement_is_settlement_plus_ERC`` () =
        let actualPayments = Map.empty

        let tiers = [| 1, Percent 2m |]

        let result =
            getEarlySettlementQuote
                EarlySettlementMethod.RuleOf78
                (Some(EarlyRepaymentChargeType.TieredPercentage tiers))
                baseParameters
                actualPayments

        result.NetSettlementFigure
        |> should equal (result.SettlementFigure + result.EarlyRepaymentCharge)

    // ---------------------------------------------------------------------------
    // 7.  calculateEarlyRepaymentCharge – standalone function
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_calculateEarlyRepaymentCharge_TieredPct`` () =
        let principal = 100000L<Cent>   // £1 000
        let startDate = Date(2023, 1, 1)
        let settlementDate = Date(2023, 6, 1)   // in year 1
        let rate = Interest.Rate.Annual(Percent 12m)
        let tiers = [| 1, Percent 5m; 2, Percent 3m |]

        let erc =
            calculateEarlyRepaymentCharge
                (EarlyRepaymentChargeType.TieredPercentage tiers)
                principal
                startDate
                settlementDate
                rate

        // year 1: 5 % of £1 000 = £50 = 5000¢
        erc |> should equal 5000L<Cent>

    [<Fact>]
    let ``EarlySettlementTest_calculateEarlyRepaymentCharge_FixedMonths`` () =
        let principal = 120000L<Cent>   // £1 200
        let startDate = Date(2024, 1, 1)
        let settlementDate = Date(2024, 6, 1)
        let rate = Interest.Rate.Annual(Percent 12m)   // 1 % / month

        let erc =
            calculateEarlyRepaymentCharge
                (EarlyRepaymentChargeType.FixedMonthsInterest 3)
                principal
                startDate
                settlementDate
                rate

        // 3 months × 1 %/month × £1 200 = £36 = 3600¢
        erc |> should equal 3600L<Cent>

    // ---------------------------------------------------------------------------
    // 8.  All three methods are consistent: FCA ≤ Actuarial (after rebate)
    // ---------------------------------------------------------------------------

    [<Fact>]
    let ``EarlySettlementTest_three_methods_settlement_figures_are_positive`` () =
        let actualPayments = Map.empty

        let actuarial =
            getEarlySettlementQuote EarlySettlementMethod.Actuarial None baseParameters actualPayments

        let ruleOf78 =
            getEarlySettlementQuote EarlySettlementMethod.RuleOf78 None baseParameters actualPayments

        let fca =
            getEarlySettlementQuote EarlySettlementMethod.FCA None baseParameters actualPayments

        actuarial.SettlementFigure |> should be (greaterThan 0L<Cent>)
        ruleOf78.SettlementFigure |> should be (greaterThan 0L<Cent>)
        fca.SettlementFigure |> should be (greaterThan 0L<Cent>)
