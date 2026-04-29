namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module RefinancingTests =

    open Amortisation
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod
    open Refinancing

    // ─── shared helpers ────────────────────────────────────────────────────────

    let basicAdvancedParameters: AdvancedParameters = {
        PaymentConfig = {
            ScheduledPaymentOption = AsScheduled
            Minimum = NoMinimumPayment
            Timeout = 0<DurationDay>
        }
        InterestConfig = {
            InitialGracePeriod = 0<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
        }
        FeeConfig = ValueNone
        ChargeConfig = None
        SettlementDay = SettlementDay.NoSettlement
        TrimEnd = true
    }

    let mortgageParameters (startDate: Date) (evalDate: Date) (principal: int64<Cent>) (annualRate: decimal) (monthCount: int) : Parameters =
        // first payment is one month after start date to avoid payments coinciding with the advance date
        let firstPaymentDate = startDate.AddMonths 1
        {
            Basic = {
                EvaluationDate = evalDate
                StartDate = startDate
                Principal = principal
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, firstPaymentDate.Year, firstPaymentDate.Month, firstPaymentDate.Day)
                        ScheduleLength = PaymentCount monthCount
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundWith System.MidpointRounding.AwayFromZero
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Annual(Percent annualRate)
                    Cap = Interest.Cap.zero
                    Rounding = RoundWith System.MidpointRounding.AwayFromZero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 1
                }
            }
            Advanced = basicAdvancedParameters
        }

    // ─── compareRemortgage tests ───────────────────────────────────────────────

    [<Fact>]
    let ``compareRemortgage: switching to lower rate with no fees gives positive net benefit`` () =
        let startDate = Date(2024, 1, 1)
        let evalDate = Date(2024, 1, 1) // evaluation at start – all payments are future
        let principal = 10_000_00L<Cent>

        let currentParams = mortgageParameters startDate evalDate principal 6m 12

        let newProductParams = mortgageParameters startDate evalDate principal 4m 12

        let rcp: RemortgageComparisonParameters = {
            EarlyRepaymentCharge = Amount.Simple 0L<Cent>
            ArrangementFee = 0L<Cent>
            LegalCosts = 0L<Cent>
            NewProductParameters = newProductParams
        }

        let result = compareRemortgage currentParams Map.empty rcp

        // switching to a lower rate with no upfront costs should always save money
        result.NetBenefit |> should be (greaterThan 0L<Cent>)
        result.StayTotalCost |> should be (greaterThan result.SwitchTotalCost)
        result.UpfrontSwitchCosts |> should equal 0L<Cent>
        result.EarlyRepaymentCharge |> should equal 0L<Cent>
        // break-even is immediate when there are no upfront costs
        result.BreakEvenPeriodCount |> should equal (ValueSome 0)

    [<Fact>]
    let ``compareRemortgage: switching to higher rate gives negative net benefit`` () =
        let startDate = Date(2024, 1, 1)
        let evalDate = Date(2024, 1, 1)
        let principal = 10_000_00L<Cent>

        let currentParams = mortgageParameters startDate evalDate principal 4m 12
        let newProductParams = mortgageParameters startDate evalDate principal 6m 12

        let rcp: RemortgageComparisonParameters = {
            EarlyRepaymentCharge = Amount.Simple 0L<Cent>
            ArrangementFee = 0L<Cent>
            LegalCosts = 0L<Cent>
            NewProductParameters = newProductParams
        }

        let result = compareRemortgage currentParams Map.empty rcp

        // switching to a higher rate should cost more
        result.NetBenefit |> should be (lessThan 0L<Cent>)
        result.StayTotalCost |> should be (lessThan result.SwitchTotalCost)

    [<Fact>]
    let ``compareRemortgage: ERC and fees reduce net benefit`` () =
        let startDate = Date(2024, 1, 1)
        let evalDate = Date(2024, 1, 1)
        let principal = 10_000_00L<Cent>

        let currentParams = mortgageParameters startDate evalDate principal 6m 12
        let newProductParams = mortgageParameters startDate evalDate principal 4m 12

        let rcpNoFees: RemortgageComparisonParameters = {
            EarlyRepaymentCharge = Amount.Simple 0L<Cent>
            ArrangementFee = 0L<Cent>
            LegalCosts = 0L<Cent>
            NewProductParameters = newProductParams
        }

        let rcpWithFees: RemortgageComparisonParameters = {
            EarlyRepaymentCharge = Amount.Percentage(Percent 2m, Restriction.NoLimit) // 2% ERC
            ArrangementFee = 100_00L<Cent>  // £100 arrangement fee
            LegalCosts = 30_00L<Cent>       // £30 legal costs
            NewProductParameters = newProductParams
        }

        let resultNoFees = compareRemortgage currentParams Map.empty rcpNoFees
        let resultWithFees = compareRemortgage currentParams Map.empty rcpWithFees

        // adding upfront costs should reduce the net benefit
        resultWithFees.NetBenefit |> should be (lessThan resultNoFees.NetBenefit)
        // the ERC should be 2% of the outstanding principal balance
        resultWithFees.EarlyRepaymentCharge |> should be (greaterThan 0L<Cent>)
        // upfront costs should be the sum of ERC, arrangement fee and legal costs
        resultWithFees.UpfrontSwitchCosts
        |> should equal (resultWithFees.EarlyRepaymentCharge + 100_00L<Cent> + 30_00L<Cent>)

    [<Fact>]
    let ``compareRemortgage: break-even requires multiple periods when there are upfront costs`` () =
        let startDate = Date(2024, 1, 1)
        let evalDate = Date(2024, 1, 1)
        let principal = 10_000_00L<Cent>

        let currentParams = mortgageParameters startDate evalDate principal 6m 24
        let newProductParams = mortgageParameters startDate evalDate principal 4m 24

        let rcp: RemortgageComparisonParameters = {
            EarlyRepaymentCharge = Amount.Simple 0L<Cent>
            // £50 fee – small enough to be recovered within the 24-month remaining term
            ArrangementFee = 50_00L<Cent>
            LegalCosts = 0L<Cent>
            NewProductParameters = newProductParams
        }

        let result = compareRemortgage currentParams Map.empty rcp

        // with £50 in upfront costs, break-even should be some positive number of periods within the 24-month term
        result.BreakEvenPeriodCount.IsSome |> should equal true
        result.BreakEvenPeriodCount.Value |> should be (greaterThan 0)
        result.BreakEvenPeriodCount.Value |> should be (lessThanOrEqualTo 24)

    [<Fact>]
    let ``compareRemortgage: outstanding balance equals principal at start of schedule`` () =
        let startDate = Date(2024, 1, 1)
        let evalDate = Date(2024, 1, 1)
        let principal = 10_000_00L<Cent>

        let currentParams = mortgageParameters startDate evalDate principal 6m 12

        let newProductParams = mortgageParameters startDate evalDate principal 4m 12

        let rcp: RemortgageComparisonParameters = {
            EarlyRepaymentCharge = Amount.Simple 0L<Cent>
            ArrangementFee = 0L<Cent>
            LegalCosts = 0L<Cent>
            NewProductParameters = newProductParams
        }

        let result = compareRemortgage currentParams Map.empty rcp

        // at start of schedule (no payments made), outstanding balance should equal the principal
        result.OutstandingBalance |> should equal principal

    // ─── compareDebtConsolidation tests ───────────────────────────────────────

    [<Fact>]
    let ``compareDebtConsolidation: consolidating at lower rate saves money (zero discount rate)`` () =
        let evalDate = Date(2024, 1, 1)

        let debt1Params = mortgageParameters evalDate evalDate 5_000_00L<Cent> 10m 12
        let debt2Params = mortgageParameters evalDate evalDate 3_000_00L<Cent> 15m 12

        // consolidated: same total principal, lower rate, same term
        let consolidatedParams = mortgageParameters evalDate evalDate 8_000_00L<Cent> 8m 12

        let dcp: DebtConsolidationParameters = {
            DiscountRate = Interest.Rate.Zero // no discounting – compare nominal totals
            ConsolidatedFacilityParameters = consolidatedParams
            ConsolidationFees = 0L<Cent>
        }

        let result =
            compareDebtConsolidation
                [| debt1Params, Map.empty; debt2Params, Map.empty |]
                dcp

        // consolidating at a lower blended rate should reduce total payments
        result.NetConsolidationSaving |> should be (greaterThan 0L<Cent>)
        result.IndividualPaymentsNpv |> should be (greaterThan result.ConsolidatedPaymentsNpv)

    [<Fact>]
    let ``compareDebtConsolidation: consolidating at higher rate costs more`` () =
        let evalDate = Date(2024, 1, 1)

        let debt1Params = mortgageParameters evalDate evalDate 5_000_00L<Cent> 4m 12
        let debt2Params = mortgageParameters evalDate evalDate 3_000_00L<Cent> 4m 12

        // consolidated at a higher rate than the individual debts
        let consolidatedParams = mortgageParameters evalDate evalDate 8_000_00L<Cent> 10m 12

        let dcp: DebtConsolidationParameters = {
            DiscountRate = Interest.Rate.Zero
            ConsolidatedFacilityParameters = consolidatedParams
            ConsolidationFees = 0L<Cent>
        }

        let result =
            compareDebtConsolidation
                [| debt1Params, Map.empty; debt2Params, Map.empty |]
                dcp

        // consolidating at a higher rate should cost more
        result.NetConsolidationSaving |> should be (lessThan 0L<Cent>)
        result.ConsolidatedPaymentsNpv |> should be (greaterThan result.IndividualPaymentsNpv)

    [<Fact>]
    let ``compareDebtConsolidation: upfront fees reduce net saving`` () =
        let evalDate = Date(2024, 1, 1)

        let debt1Params = mortgageParameters evalDate evalDate 5_000_00L<Cent> 10m 12
        let debt2Params = mortgageParameters evalDate evalDate 3_000_00L<Cent> 15m 12
        let consolidatedParams = mortgageParameters evalDate evalDate 8_000_00L<Cent> 8m 12

        let dcpNoFees: DebtConsolidationParameters = {
            DiscountRate = Interest.Rate.Zero
            ConsolidatedFacilityParameters = consolidatedParams
            ConsolidationFees = 0L<Cent>
        }

        let dcpWithFees: DebtConsolidationParameters = {
            DiscountRate = Interest.Rate.Zero
            ConsolidatedFacilityParameters = consolidatedParams
            ConsolidationFees = 200_00L<Cent>
        }

        let resultNoFees =
            compareDebtConsolidation [| debt1Params, Map.empty; debt2Params, Map.empty |] dcpNoFees

        let resultWithFees =
            compareDebtConsolidation [| debt1Params, Map.empty; debt2Params, Map.empty |] dcpWithFees

        // fees increase consolidated cost, so net saving is lower
        resultWithFees.NetConsolidationSaving |> should be (lessThan resultNoFees.NetConsolidationSaving)
        // individual NPV is unchanged
        resultWithFees.IndividualPaymentsNpv |> should equal resultNoFees.IndividualPaymentsNpv
        // consolidated NPV is higher by exactly the fee amount
        (resultWithFees.ConsolidatedPaymentsNpv - resultNoFees.ConsolidatedPaymentsNpv)
        |> should equal 200_00L<Cent>

    [<Fact>]
    let ``compareDebtConsolidation: applying a positive discount rate lowers NPVs relative to undiscounted total`` () =
        let evalDate = Date(2024, 1, 1)

        let debt1Params = mortgageParameters evalDate evalDate 5_000_00L<Cent> 10m 12
        let debt2Params = mortgageParameters evalDate evalDate 3_000_00L<Cent> 15m 12
        let consolidatedParams = mortgageParameters evalDate evalDate 8_000_00L<Cent> 8m 12

        let dcpZeroDiscount: DebtConsolidationParameters = {
            DiscountRate = Interest.Rate.Zero
            ConsolidatedFacilityParameters = consolidatedParams
            ConsolidationFees = 0L<Cent>
        }

        let dcpPositiveDiscount: DebtConsolidationParameters = {
            DiscountRate = Interest.Rate.Annual(Percent 5m)
            ConsolidatedFacilityParameters = consolidatedParams
            ConsolidationFees = 0L<Cent>
        }

        let resultZero =
            compareDebtConsolidation [| debt1Params, Map.empty; debt2Params, Map.empty |] dcpZeroDiscount

        let resultDiscounted =
            compareDebtConsolidation [| debt1Params, Map.empty; debt2Params, Map.empty |] dcpPositiveDiscount

        // discounting future payments reduces their present value
        resultDiscounted.IndividualPaymentsNpv |> should be (lessThan resultZero.IndividualPaymentsNpv)
        resultDiscounted.ConsolidatedPaymentsNpv |> should be (lessThan resultZero.ConsolidatedPaymentsNpv)
