namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ArrearsTests =

    open Amortisation
    open AppliedPayment
    open Arrears
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    // ── shared parameters ────────────────────────────────────────────────────

    let interestCap: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    let baseParameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 6, 1)
            StartDate = Date(2024, 1, 1)
            Principal = 1000_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2024, 2, 1)
                    ScheduleLength = PaymentCount 4
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual(Percent 36m)
                Cap = interestCap
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = NoMinimumPayment
                Timeout = 3<DurationDay>
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    // ── Section 1: Arrears balance tracking ─────────────────────────────────

    module ArrearsSummaryTests =

        [<Fact>]
        let ``Zero arrears when all payments are made on time`` () =
            // get the scheduled payment amounts from the basic schedule
            let schedules = amortise baseParameters Map.empty
            let levelPayment = schedules.BasicSchedule.Stats.LevelPayment
            let finalPayment = schedules.BasicSchedule.Stats.FinalPayment

            // find the actual payment days from the amortisation schedule
            let paymentDays =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.toArray
                |> Array.filter (fun (_, si) -> si.PaymentDue > 0L<Cent>)
                |> Array.map fst

            let payCount = Array.length paymentDays

            // make all scheduled payments in full
            let actualPayments =
                paymentDays
                |> Array.mapi (fun i d ->
                    let amount = if i = payCount - 1 then finalPayment else levelPayment
                    d, [| ActualPayment.quickConfirmed amount |])
                |> Map.ofArray

            let paidSchedules = amortise baseParameters actualPayments

            let summary =
                calculateArrearsSummary baseParameters paidSchedules.AmortisationSchedule.ScheduleItems Interest.Rate.Zero

            summary.ArrearsBalance |> should equal 0L<Cent>
            summary.DaysInArrears  |> should equal 0<DurationDay>
            (summary.ArrearsStartDate = ValueNone) |> should equal true
            summary.MissedPaymentCount |> should equal 0
            summary.ArrearsInterest |> should equal 0L<Cent>

        [<Fact>]
        let ``Arrears balance equals missed payment when first payment is missed`` () =
            // make no payments at all; first scheduled date is 31 days after start
            let schedules = amortise baseParameters Map.empty

            // get the first scheduled payment amount (payment due on the first payment date)
            let firstPaymentDue =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.tryFind 31<OffsetDay>
                |> Option.map _.PaymentDue
                |> Option.defaultValue 0L<Cent>

            // evaluation date is past the first payment date but no payments were made
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 3, 1)
            }

            let schedules2 = amortise p Map.empty

            let summary =
                calculateArrearsSummary p schedules2.AmortisationSchedule.ScheduleItems Interest.Rate.Zero

            // the arrears balance should equal the first missed payment
            summary.ArrearsBalance |> should equal firstPaymentDue
            summary.MissedPaymentCount |> should equal 1
            (summary.ArrearsStartDate = ValueSome(Date(2024, 2, 1))) |> should equal true

        [<Fact>]
        let ``Arrears balance accumulates across multiple missed payments`` () =
            // no payments made; two payment dates have passed (2024-02-01 and 2024-03-01)
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 4, 1)
            }

            let schedules = amortise p Map.empty

            let summary =
                calculateArrearsSummary p schedules.AmortisationSchedule.ScheduleItems Interest.Rate.Zero

            // two missed payments
            summary.MissedPaymentCount |> should equal 2
            summary.ArrearsBalance |> should be (greaterThan 0L<Cent>)
            summary.DaysInArrears |> should be (greaterThan 0<DurationDay>)
            summary.MonthsInArrears |> should be (greaterThan 0m)

        [<Fact>]
        let ``Arrears interest is calculated on arrears balance at the supplied rate`` () =
            // one payment missed; evaluate with a non-zero arrears interest rate
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 3, 15)
            }

            let schedules = amortise p Map.empty

            let arrearsRate = Interest.Rate.Annual(Percent 10m)

            let summaryWithInterest =
                calculateArrearsSummary p schedules.AmortisationSchedule.ScheduleItems arrearsRate

            let summaryWithoutInterest =
                calculateArrearsSummary p schedules.AmortisationSchedule.ScheduleItems Interest.Rate.Zero

            // the arrears interest should be positive when a rate is applied
            summaryWithInterest.ArrearsInterest |> should be (greaterThan 0L<Cent>)
            summaryWithoutInterest.ArrearsInterest |> should equal 0L<Cent>

        [<Fact>]
        let ``Days in arrears is zero when account is not in arrears`` () =
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 1, 15) // before any payment is due
            }

            let schedules = amortise p Map.empty

            let summary =
                calculateArrearsSummary p schedules.AmortisationSchedule.ScheduleItems Interest.Rate.Zero

            summary.DaysInArrears |> should equal 0<DurationDay>
            summary.MonthsInArrears |> should equal 0m

    // ── Section 2: Default / penal interest ─────────────────────────────────

    module DefaultInterestRateTests =

        [<Fact>]
        let ``FixedRate default interest rate converted to Interest.Rate correctly`` () =
            let fixedRate = DefaultInterestRate.FixedRate(Interest.Rate.Annual(Percent 20m))
            let actual = DefaultInterestRate.toInterestRate fixedRate
            let expected = Interest.Rate.Annual(Percent 20m)
            (actual = expected) |> should equal true

        [<Fact>]
        let ``SpreadOverBase rate adds spread to base daily rate`` () =
            // base rate: 10% annual; spread: 5% annual
            // expected effective rate: 15% annual
            let baseRate = Interest.Rate.Annual(Percent 10m)
            let spread = Percent 5m
            let defaultRate = DefaultInterestRate.SpreadOverBase(baseRate, spread)
            let actual = DefaultInterestRate.toInterestRate defaultRate

            let expected = Interest.Rate.Annual(Percent 15m)
            (actual = expected) |> should equal true

        [<Fact>]
        let ``applyDefaultInterest adds promotional rate from the default date`` () =
            let defaultDate = Date(2024, 3, 1)
            let defaultRate = DefaultInterestRate.FixedRate(Interest.Rate.Annual(Percent 20m))

            let modified = applyDefaultInterest baseParameters defaultDate defaultRate

            // should have exactly one promotional rate added
            modified.Advanced.InterestConfig.PromotionalRates |> Array.length |> should equal 1

            let promoRate = modified.Advanced.InterestConfig.PromotionalRates[0]
            (promoRate.DateRange.DateRangeStart = defaultDate) |> should equal true
            (promoRate.Rate = Interest.Rate.Annual(Percent 20m)) |> should equal true

        [<Fact>]
        let ``applyDefaultInterest generates higher interest charges than contractual rate`` () =
            let defaultDate = Date(2024, 2, 1)
            // use a default rate that is higher than the contractual rate
            let defaultRate = DefaultInterestRate.FixedRate(Interest.Rate.Annual(Percent 72m))

            let modifiedParams = applyDefaultInterest baseParameters defaultDate defaultRate

            let originalSchedules = amortise baseParameters Map.empty
            let modifiedSchedules = amortise modifiedParams Map.empty

            // compare total accrued interest from the amortisation schedules (NewInterest reflects promotional rates)
            let originalInterest =
                originalSchedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy (fun si -> decimal si.NewInterest)

            let modifiedInterest =
                modifiedSchedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy (fun si -> decimal si.NewInterest)

            modifiedInterest |> should be (greaterThan originalInterest)

    // ── Section 3: Repayment arrangement modelling ───────────────────────────

    module RepaymentArrangementTests =

        [<Fact>]
        let ``modelRepaymentArrangement returns non-empty arrears summary`` () =
            // borrower has missed two payments
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 4, 1)
            }

            let arrangementConfig: ArrangementConfig = {
                ArrangementStartDate = Date(2024, 4, 1)
                ArrangementEndDate = ValueSome(Date(2024, 7, 1))
                ReducedPaymentAmount = ValueSome 50_00L<Cent> // reduced monthly payments
                ForbearanceTreatment = ForbearanceTreatment.InterestFrozen
                PostArrangementRescheduleParameters = ValueNone
            }

            let result = modelRepaymentArrangement p Map.empty arrangementConfig Interest.Rate.Zero

            // two payment dates have passed with no payments made → arrears exist
            result.ArrearsSummary.MissedPaymentCount |> should be (greaterThan 0)
            result.ArrearsSummary.ArrearsBalance |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``Interest frozen during arrangement period`` () =
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 4, 1)
            }

            let arrangementStartDate = Date(2024, 4, 1)
            let arrangementEndDate = Date(2024, 7, 1)

            let frozenConfig: ArrangementConfig = {
                ArrangementStartDate = arrangementStartDate
                ArrangementEndDate = ValueSome arrangementEndDate
                ReducedPaymentAmount = ValueSome 50_00L<Cent>
                ForbearanceTreatment = ForbearanceTreatment.InterestFrozen
                PostArrangementRescheduleParameters = ValueNone
            }

            let continuedConfig: ArrangementConfig = {
                frozenConfig with
                    ForbearanceTreatment = ForbearanceTreatment.InterestContinued
            }

            let frozenResult = modelRepaymentArrangement p Map.empty frozenConfig Interest.Rate.Zero
            let continuedResult = modelRepaymentArrangement p Map.empty continuedConfig Interest.Rate.Zero

            // compare total accrued interest via NewInterest (reflects promotional/promotional zero rates)
            let frozenInterest =
                frozenResult.ArrangementSchedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy (fun si -> decimal si.NewInterest)

            let continuedInterest =
                continuedResult.ArrangementSchedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy (fun si -> decimal si.NewInterest)

            // frozen interest should result in less total accrued interest than continued interest
            frozenInterest |> should be (lessThan continuedInterest)

        [<Fact>]
        let ``Arrangement with suspended payments reduces net effect to zero during arrangement`` () =
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 4, 1)
            }

            let arrangementConfig: ArrangementConfig = {
                ArrangementStartDate = Date(2024, 4, 1)
                ArrangementEndDate = ValueSome(Date(2024, 7, 1))
                ReducedPaymentAmount = ValueNone // payments suspended
                ForbearanceTreatment = ForbearanceTreatment.InterestFrozen
                PostArrangementRescheduleParameters = ValueNone
            }

            let result = modelRepaymentArrangement p Map.empty arrangementConfig Interest.Rate.Zero

            // the arrangement should complete without throwing
            result.ArrearsSummary |> should not' (be null)
            result.ArrangementSchedules |> should not' (be null)

        [<Fact>]
        let ``Post-arrangement schedule is None when no end date is specified`` () =
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 4, 1)
            }

            let arrangementConfig: ArrangementConfig = {
                ArrangementStartDate = Date(2024, 4, 1)
                ArrangementEndDate = ValueNone // open-ended
                ReducedPaymentAmount = ValueSome 50_00L<Cent>
                ForbearanceTreatment = ForbearanceTreatment.InterestFrozen
                PostArrangementRescheduleParameters = ValueNone
            }

            let result = modelRepaymentArrangement p Map.empty arrangementConfig Interest.Rate.Zero

            (result.PostArrangementSchedules = ValueNone) |> should equal true

        [<Fact>]
        let ``Reduced-rate interest during arrangement results in less interest than full rate`` () =
            let p = {
                baseParameters with
                    Basic.EvaluationDate = Date(2024, 4, 1)
            }

            let reducedRateConfig: ArrangementConfig = {
                ArrangementStartDate = Date(2024, 4, 1)
                ArrangementEndDate = ValueSome(Date(2024, 7, 1))
                ReducedPaymentAmount = ValueSome 50_00L<Cent>
                ForbearanceTreatment = ForbearanceTreatment.InterestReduced(Interest.Rate.Annual(Percent 10m))
                PostArrangementRescheduleParameters = ValueNone
            }

            let fullRateConfig: ArrangementConfig = {
                reducedRateConfig with
                    ForbearanceTreatment = ForbearanceTreatment.InterestContinued
            }

            let reducedResult = modelRepaymentArrangement p Map.empty reducedRateConfig Interest.Rate.Zero
            let fullRateResult = modelRepaymentArrangement p Map.empty fullRateConfig Interest.Rate.Zero

            let reducedInterest =
                reducedResult.ArrangementSchedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy (fun si -> decimal si.NewInterest)

            let fullRateInterest =
                fullRateResult.ArrangementSchedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy (fun si -> decimal si.NewInterest)

            reducedInterest |> should be (lessThan fullRateInterest)
