namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module RefinancingTests =

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open Refinancing
    open UnitPeriod

    // ──────────────────────────────────────────────────────────────────────────
    // Shared helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// A standard monthly actuarial loan:
    ///   £10 000 over 12 months at 5 % p.a., start-date 2024-01-01.
    ///
    /// Scheduled payment days (offset from 2024-01-01):
    ///   30, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
    let private baseParameters : Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 1, 1)
            StartDate = Date(2024, 1, 1)
            Principal = 10_000_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2024, 1, 31)
                    ScheduleLength = PaymentCount 12
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual(Percent 5m)
                Cap = Interest.Cap.zero
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = NoMinimumPayment
                Timeout = 0<DurationDay>
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

    // ──────────────────────────────────────────────────────────────────────────
    // 1. Payment holiday — CapitaliseAsNewPrincipal
    // ──────────────────────────────────────────────────────────────────────────

    [<Fact>]
    let ``PaymentHoliday_CapitaliseAsNewPrincipal_produces_higher_post_holiday_payments`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        // Evaluate the schedule as-of the last day before the holiday starts.
        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        // Payments made for months 1–3 (on the exact scheduled payment days).
        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
            ]

        // Holiday covers scheduled payment days 120 (Apr 30), 151, 181, 212 (Jul 31).
        // Post-holiday payments: days 243, 273, 304, 334, 365.
        let php : PaymentHolidayParameters = {
            HolidayStartDay = 120<OffsetDay>
            HolidayEndDay   = 212<OffsetDay>
            InterestHandling = DeferredInterestHandling.CapitaliseAsNewPrincipal
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = paymentHoliday p php actualPayments

        // The capitalised balance (principal + accrued holiday interest) is higher
        // than the original remaining balance, so each post-holiday payment should
        // be higher than the original level payment.
        let firstPostHolidayItem =
            result.NewSchedules.BasicSchedule.Items
            |> Array.find (fun bi -> bi.Day > php.HolidayEndDay && ScheduledPayment.isSome bi.ScheduledPayment)

        ScheduledPayment.total firstPostHolidayItem.ScheduledPayment
        |> should be (greaterThan levelPayment)

    [<Fact>]
    let ``PaymentHoliday_CapitaliseAsNewPrincipal_new_schedule_eventually_closes`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
            ]

        let php : PaymentHolidayParameters = {
            HolidayStartDay = 120<OffsetDay>
            HolidayEndDay   = 212<OffsetDay>
            InterestHandling = DeferredInterestHandling.CapitaliseAsNewPrincipal
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = paymentHoliday p php actualPayments

        result.NewSchedules.AmortisationSchedule.FinalStats.FinalBalanceStatus
        |> should equal ClosedBalance

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Payment holiday — SpreadOverRemainingTerm
    // ──────────────────────────────────────────────────────────────────────────

    [<Fact>]
    let ``PaymentHoliday_SpreadOverRemainingTerm_raises_each_remaining_payment`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
            ]

        let php : PaymentHolidayParameters = {
            HolidayStartDay = 120<OffsetDay>
            HolidayEndDay   = 212<OffsetDay>
            InterestHandling = DeferredInterestHandling.SpreadOverRemainingTerm
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = paymentHoliday p php actualPayments

        // The deferred interest is spread over the post-holiday payments,
        // so each post-holiday payment must be higher than the original level payment.
        let firstPostHolidayItem =
            result.NewSchedules.BasicSchedule.Items
            |> Array.find (fun bi -> bi.Day > php.HolidayEndDay && ScheduledPayment.isSome bi.ScheduledPayment)

        ScheduledPayment.total firstPostHolidayItem.ScheduledPayment
        |> should be (greaterThan levelPayment)

    [<Fact>]
    let ``PaymentHoliday_SpreadOverRemainingTerm_schedule_closes`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
            ]

        let php : PaymentHolidayParameters = {
            HolidayStartDay = 120<OffsetDay>
            HolidayEndDay   = 212<OffsetDay>
            InterestHandling = DeferredInterestHandling.SpreadOverRemainingTerm
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = paymentHoliday p php actualPayments

        result.NewSchedules.AmortisationSchedule.FinalStats.FinalBalanceStatus
        |> should equal ClosedBalance

    // ──────────────────────────────────────────────────────────────────────────
    // 3. Overpayment — ReduceTerm
    // ──────────────────────────────────────────────────────────────────────────

    [<Fact>]
    let ``Overpayment_ReduceTerm_shortens_the_schedule`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        // Evaluate at month 3 (day 90, = Mar 31 2024).
        // The borrower has made 3 regular payments plus a £2 000 overpayment on day 90.
        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [|
                    ActualPayment.quickConfirmed levelPayment
                    ActualPayment.quickConfirmed 2_000_00L<Cent>
                |]
            ]

        let op : OverpaymentParameters = {
            OverpaymentHandling = OverpaymentHandling.ReduceTerm
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = applyOverpayment p op actualPayments

        // Original total = 12 payments; the overpayment should make at least one
        // payment NoLongerRequired, so RequiredScheduledPaymentCount < 12.
        let newCount =
            result.NewSchedules.AmortisationSchedule.FinalStats.RequiredScheduledPaymentCount

        newCount |> should be (lessThan 12)

        // The schedule should close.
        result.NewSchedules.AmortisationSchedule.FinalStats.FinalBalanceStatus
        |> should equal ClosedBalance

    [<Fact>]
    let ``Overpayment_ReduceTerm_keeps_same_per_payment_amount`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [|
                    ActualPayment.quickConfirmed levelPayment
                    ActualPayment.quickConfirmed 2_000_00L<Cent>
                |]
            ]

        let op : OverpaymentParameters = {
            OverpaymentHandling = OverpaymentHandling.ReduceTerm
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = applyOverpayment p op actualPayments

        // The first post-evaluation rescheduled payment should equal the original level payment.
        let firstFutureItem =
            result.NewSchedules.BasicSchedule.Items
            |> Array.find (fun bi ->
                bi.Day > 90<OffsetDay> && ScheduledPayment.isSome bi.ScheduledPayment)

        ScheduledPayment.total firstFutureItem.ScheduledPayment
        |> should equal levelPayment

    // ──────────────────────────────────────────────────────────────────────────
    // 4. Overpayment — ReducePayment
    // ──────────────────────────────────────────────────────────────────────────

    [<Fact>]
    let ``Overpayment_ReducePayment_lowers_the_regular_payment`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [|
                    ActualPayment.quickConfirmed levelPayment
                    ActualPayment.quickConfirmed 2_000_00L<Cent>
                |]
            ]

        let op : OverpaymentParameters = {
            OverpaymentHandling = OverpaymentHandling.ReducePayment
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = applyOverpayment p op actualPayments

        // The first post-evaluation rescheduled payment should be lower than original.
        let firstFutureItem =
            result.NewSchedules.BasicSchedule.Items
            |> Array.find (fun bi ->
                bi.Day > 90<OffsetDay> && ScheduledPayment.isSome bi.ScheduledPayment)

        ScheduledPayment.total firstFutureItem.ScheduledPayment
        |> should be (lessThan levelPayment)

    [<Fact>]
    let ``Overpayment_ReducePayment_closes_on_same_final_day_as_original`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 3, 31) }

        let actualPayments =
            Map [
                30<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                59<OffsetDay>,  [| ActualPayment.quickConfirmed levelPayment |]
                90<OffsetDay>,  [|
                    ActualPayment.quickConfirmed levelPayment
                    ActualPayment.quickConfirmed 2_000_00L<Cent>
                |]
            ]

        let op : OverpaymentParameters = {
            OverpaymentHandling = OverpaymentHandling.ReducePayment
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = applyOverpayment p op actualPayments

        // ReducePayment preserves the original term, so last payment day must be the same.
        let originalLastDay = (calculateBasicSchedule baseParameters.Basic).Stats.LastScheduledPaymentDay
        let newLastDay = result.NewSchedules.BasicSchedule.Stats.LastScheduledPaymentDay

        newLastDay |> should equal originalLastDay

        // The schedule must close.
        result.NewSchedules.AmortisationSchedule.FinalStats.FinalBalanceStatus
        |> should equal ClosedBalance

    // ──────────────────────────────────────────────────────────────────────────
    // 5. Edge cases
    // ──────────────────────────────────────────────────────────────────────────

    [<Fact>]
    let ``PaymentHoliday_with_no_scheduled_payments_in_holiday_window_leaves_schedule_unchanged`` () =
        // Holiday window falls in the gap between day 30 and day 59 (no scheduled payments).
        // The new schedule should be identical to the original.
        let p = baseParameters

        let php : PaymentHolidayParameters = {
            HolidayStartDay = 31<OffsetDay>   // gap between day 30 (Jan 31) and day 59 (Feb 29)
            HolidayEndDay   = 58<OffsetDay>
            InterestHandling = DeferredInterestHandling.CapitaliseAsNewPrincipal
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = paymentHoliday p php Map.empty

        // With no payments in the window the total interest should be unchanged.
        let oldInterest = result.OldSchedules.BasicSchedule.Stats.InterestTotal
        let newInterest = result.NewSchedules.BasicSchedule.Stats.InterestTotal

        newInterest |> should equal oldInterest

    [<Fact>]
    let ``Overpayment_ReduceTerm_that_clears_balance_entirely_produces_refund_due`` () =
        let levelPayment = (calculateBasicSchedule baseParameters.Basic).Stats.LevelPayment

        // Evaluate at month 1 (day 30).
        // The borrower pays the regular first payment plus enough to over-clear the remaining balance.
        let p = { baseParameters with Basic.EvaluationDate = Date(2024, 1, 31) }

        // Balance after month 1 ≈ 918512 (≈ £9185.12).
        // Overpaying above the remaining balance results in a refund-due final state.
        let actualPayments =
            Map [
                30<OffsetDay>,  [|
                    ActualPayment.quickConfirmed levelPayment
                    ActualPayment.quickConfirmed 920_000L<Cent>   // slightly more than the remaining balance
                |]
            ]

        let op : OverpaymentParameters = {
            OverpaymentHandling = OverpaymentHandling.ReduceTerm
            SettlementDay = SettlementDay.NoSettlement
        }

        let result = applyOverpayment p op actualPayments

        // Paying more than the outstanding balance leaves a refund due.
        result.NewSchedules.AmortisationSchedule.FinalStats.FinalBalanceStatus
        |> should equal RefundDue
