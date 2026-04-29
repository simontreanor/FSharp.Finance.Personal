namespace FSharp.Finance.Personal

open Scheduling
open Quotes

/// functions for modelling accounts in financial difficulty: arrears balance tracking, default/penal interest, and repayment arrangements
///
/// **Regulatory context:** FCA CONC 7 (arrears, default, and recovery) requires lenders to model and communicate these scenarios accurately to borrowers.
module Arrears =

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Interest

    // ============================
    // Section 1: Arrears balance tracking
    // ============================

    /// a summary of the arrears position of an account as at the evaluation date
    [<Struct>]
    type ArrearsSummary = {
        /// the cumulative shortfall: total of scheduled payment amounts that have not been paid (missed or underpaid)
        ArrearsBalance: int64<Cent>
        /// the date on which the account first entered arrears, if it has done so
        ArrearsStartDate: Date voption
        /// the number of days elapsed since the account first entered arrears (zero if not in arrears)
        DaysInArrears: int<DurationDay>
        /// the approximate number of months in arrears (DaysInArrears / 30.4375), rounded to two decimal places
        MonthsInArrears: decimal
        /// the count of scheduled payment dates on which a missed payment or underpayment occurred
        MissedPaymentCount: int
        /// the interest accrued on the arrears balance over the period in arrears, at the supplied arrears interest rate
        ArrearsInterest: int64<Cent>
    }

    /// computes the arrears summary for an account as at the evaluation date, based on an existing amortisation schedule
    ///
    /// - `p`                  the schedule parameters (used for start date and evaluation date)
    /// - `scheduleItems`      the amortisation schedule items, typically from `amortise`
    /// - `arrearsInterestRate` the interest rate to apply to the arrears balance; use `Interest.Rate.Zero` if no arrears interest is chargeable
    let calculateArrearsSummary
        (p: Parameters)
        (scheduleItems: Map<int<OffsetDay>, ScheduleItem>)
        (arrearsInterestRate: Rate)
        =
        let evaluationDay = OffsetDay.fromDate p.Basic.StartDate p.Basic.EvaluationDate

        // collect schedule items up to and including the evaluation day
        let pastItems =
            scheduleItems
            |> Map.filter (fun d _ -> d <= evaluationDay)
            |> Map.toArray

        // the arrears balance = cumulative shortfall (missed or underpaid amounts) up to the evaluation date
        let arrearsBalance =
            pastItems
            |> Array.sumBy (fun (_, si) ->
                match si.PaymentStatus with
                | MissedPayment -> si.PaymentDue
                | Underpayment -> si.PaymentDue - si.NetEffect
                | PaidLaterOwing shortfall -> shortfall
                | _ -> 0L<Cent>
            )

        // find the first offset day on which a missed payment or underpayment occurred
        let arrearsStartDay =
            pastItems
            |> Array.tryFind (fun (_, si) ->
                match si.PaymentStatus with
                | MissedPayment
                | Underpayment -> true
                | _ -> false
            )
            |> Option.map fst

        let arrearsStartDate =
            arrearsStartDay
            |> Option.map (OffsetDay.toDate p.Basic.StartDate)
            |> function
                | Some d -> ValueSome d
                | None -> ValueNone

        let daysInArrears =
            match arrearsStartDay with
            | Some startDay -> (int evaluationDay - int startDay) * 1<DurationDay>
            | None -> 0<DurationDay>

        let monthsInArrears =
            System.Math.Round(decimal daysInArrears / 30.4375m, 2)

        let missedPaymentCount =
            pastItems
            |> Array.sumBy (fun (_, si) ->
                match si.PaymentStatus with
                | MissedPayment
                | Underpayment -> 1
                | _ -> 0
            )

        // calculate interest on the arrears balance for the period in arrears
        let arrearsInterest =
            if arrearsBalance = 0L<Cent> || arrearsInterestRate = Rate.Zero then
                0L<Cent>
            else
                match arrearsStartDay with
                | None -> 0L<Cent>
                | Some startDay ->
                    let dailyRates =
                        dailyRates
                            p.Basic.StartDate
                            false
                            arrearsInterestRate
                            [||]
                            startDay
                            evaluationDay

                    calculate arrearsBalance Amount.Unlimited (RoundWith System.MidpointRounding.AwayFromZero) dailyRates
                    |> Cent.fromDecimalCent (RoundWith System.MidpointRounding.AwayFromZero)

        {
            ArrearsBalance = arrearsBalance
            ArrearsStartDate = arrearsStartDate
            DaysInArrears = daysInArrears
            MonthsInArrears = monthsInArrears
            MissedPaymentCount = missedPaymentCount
            ArrearsInterest = arrearsInterest
        }

    // ============================
    // Section 2: Default / penal interest
    // ============================

    /// the default (penal) interest rate applied once an account enters formal default, distinct from the contractual rate
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type DefaultInterestRate =
        /// a fixed default rate that replaces the contractual rate from the default date
        | FixedRate of Rate: Rate
        /// a floating rate defined as a spread (basis-points addition) over a reference base rate
        | SpreadOverBase of BaseRate: Rate * Spread: Percent

        /// HTML formatting to display the default rate in a readable format
        member x.Html =
            match x with
            | FixedRate r -> $"fixed default rate: {r}"
            | SpreadOverBase(baseRate, spread) -> $"{spread} spread over base rate ({baseRate})"

    /// utility functions for `DefaultInterestRate`
    module DefaultInterestRate =
        /// converts a `DefaultInterestRate` to the equivalent `Interest.Rate`
        let toInterestRate =
            function
            | DefaultInterestRate.FixedRate rate -> rate
            | DefaultInterestRate.SpreadOverBase(baseRate, Percent spread) ->
                // effective annual rate = base annual rate (%) + spread (annual percentage points)
                let (Percent baseAnnual) = Rate.annual baseRate
                Rate.Annual(Percent(baseAnnual + spread))

    /// applies a default interest rate from a given default date onwards, by adding it as a promotional rate in the parameters
    ///
    /// - `p`                    the original schedule parameters
    /// - `defaultDate`          the date from which the default rate applies
    /// - `defaultInterestRate`  the default rate (fixed or spread over base)
    ///
    /// Returns a modified `Parameters` record with the default rate applied.
    let applyDefaultInterest (p: Parameters) (defaultDate: Date) (defaultInterestRate: DefaultInterestRate) =
        let effectiveRate = DefaultInterestRate.toInterestRate defaultInterestRate

        // represent "indefinitely" by extending the rate to a far-future date
        let farFuture = defaultDate.AddYears 50

        let defaultRatePromo: PromotionalRate = {
            DateRange = {
                DateRangeStart = defaultDate
                DateRangeEnd = farFuture
            }
            Rate = effectiveRate
        }

        {
            p with
                Advanced.InterestConfig.PromotionalRates =
                    p.Advanced.InterestConfig.PromotionalRates
                    |> Array.append [| defaultRatePromo |]
        }

    // ============================
    // Section 3: Repayment arrangement modelling
    // ============================

    /// how interest should be treated during a forbearance / repayment arrangement
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type ForbearanceTreatment =
        /// interest is frozen (zero rate) during the arrangement
        | InterestFrozen
        /// interest accrues at a reduced rate during the arrangement
        | InterestReduced of ReducedRate: Rate
        /// interest continues at the full contractual rate during the arrangement
        | InterestContinued

        /// HTML formatting to display the forbearance treatment in a readable format
        member x.Html =
            match x with
            | InterestFrozen -> "interest frozen"
            | InterestReduced rate -> $"interest reduced to {rate}"
            | InterestContinued -> "interest at full contractual rate"

    /// configuration for a repayment arrangement agreed with a borrower in financial difficulty
    [<RequireQualifiedAccess>]
    type ArrangementConfig = {
        /// the date on which the arrangement begins
        ArrangementStartDate: Date
        /// the date on which the arrangement ends; `ValueNone` = open-ended
        ArrangementEndDate: Date voption
        /// the reduced scheduled payment amount during the arrangement; `ValueNone` = payments suspended (zero)
        ReducedPaymentAmount: int64<Cent> voption
        /// how interest is treated during the arrangement period
        ForbearanceTreatment: ForbearanceTreatment
        /// parameters for generating a return-to-normal schedule once the arrangement ends; `ValueNone` = no post-arrangement schedule
        PostArrangementRescheduleParameters: Refinancing.RescheduleParameters voption
    }

    /// the result of modelling a repayment arrangement
    [<Struct>]
    type ArrangementResult = {
        /// arrears summary computed immediately before the arrangement takes effect
        ArrearsSummary: ArrearsSummary
        /// projected schedule covering the arrangement period (and beyond if open-ended)
        ArrangementSchedules: GenerationResult
        /// projected return-to-normal schedule after the arrangement ends, if applicable
        PostArrangementSchedules: GenerationResult voption
    }

    /// models a repayment arrangement agreed with a borrower in financial difficulty
    ///
    /// - `p`                    the original schedule parameters
    /// - `actualPayments`       actual payments already made by the borrower
    /// - `arrangementConfig`    configuration for the arrangement
    /// - `arrearsInterestRate`  rate at which arrears interest is charged; use `Interest.Rate.Zero` if not applicable
    ///
    /// Returns an `ArrangementResult` with the arrears summary, the projected arrangement schedule,
    /// and (where applicable) the return-to-normal schedule.
    let modelRepaymentArrangement
        (p: Parameters)
        (actualPayments: Map<int<OffsetDay>, ActualPayment array>)
        (arrangementConfig: ArrangementConfig)
        (arrearsInterestRate: Rate)
        =
        // generate the pre-arrangement schedule to compute the current arrears position
        let preArrangementSchedules = amortise p actualPayments

        let arrearsSummary =
            calculateArrearsSummary
                p
                preArrangementSchedules.AmortisationSchedule.ScheduleItems
                arrearsInterestRate

        // resolve the arrangement end date (or a far-future proxy for open-ended arrangements)
        let arrangementEndDate =
            arrangementConfig.ArrangementEndDate
            |> ValueOption.defaultValue (arrangementConfig.ArrangementStartDate.AddYears 50)

        // build the forbearance promotional rate for the arrangement period, if the interest is frozen or reduced
        let forbearancePromoRate =
            match arrangementConfig.ForbearanceTreatment with
            | ForbearanceTreatment.InterestFrozen ->
                ValueSome {
                    PromotionalRate.DateRange = {
                        DateRangeStart = arrangementConfig.ArrangementStartDate
                        DateRangeEnd = arrangementEndDate
                    }
                    PromotionalRate.Rate = Rate.Zero
                }
            | ForbearanceTreatment.InterestReduced reducedRate ->
                ValueSome {
                    PromotionalRate.DateRange = {
                        DateRangeStart = arrangementConfig.ArrangementStartDate
                        DateRangeEnd = arrangementEndDate
                    }
                    PromotionalRate.Rate = reducedRate
                }
            | ForbearanceTreatment.InterestContinued -> ValueNone

        // build the payment schedule for the arrangement period
        let arrangementPaymentSchedule =
            let arrangementStartDay = OffsetDay.fromDate p.Basic.StartDate arrangementConfig.ArrangementStartDate
            let arrangementEndDay = OffsetDay.fromDate p.Basic.StartDate arrangementEndDate
            let paymentValue = arrangementConfig.ReducedPaymentAmount |> ValueOption.defaultValue 0L<Cent>

            // generate a fixed monthly schedule for the arrangement period
            let arrangementStartDate = arrangementConfig.ArrangementStartDate
            let firstPaymentDate = arrangementStartDate.AddMonths 1

            [|
                let mutable paymentDate = firstPaymentDate
                while OffsetDay.fromDate p.Basic.StartDate paymentDate <= arrangementEndDay do
                    let day = OffsetDay.fromDate p.Basic.StartDate paymentDate
                    let sp =
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = paymentValue
                                RescheduleDay = arrangementStartDay
                            })
                    yield day, sp
                    paymentDate <- paymentDate.AddMonths 1
            |]
            |> Map.ofArray

        // apply forbearance promotional rates to the parameters
        let arrangementParams = {
            p with
                Basic.EvaluationDate = arrangementEndDate
                Advanced.InterestConfig.PromotionalRates =
                    match forbearancePromoRate with
                    | ValueSome fr ->
                        p.Advanced.InterestConfig.PromotionalRates
                        |> Array.append [| fr |]
                    | ValueNone -> p.Advanced.InterestConfig.PromotionalRates
        }

        // merge the arrangement payments into the existing schedule
        let arrangementSchedules =
            match arrangementConfig.PostArrangementRescheduleParameters with
            | ValueSome rp ->
                // use the Refinancing module to build the rescheduled arrangement
                let rescheduled = Refinancing.reschedule arrangementParams rp actualPayments
                rescheduled.NewSchedules
            | ValueNone ->
                // merge existing scheduled payments with arrangement payments; arrangement payments override
                let existingPayments =
                    preArrangementSchedules.AmortisationSchedule.ScheduleItems
                    |> Map.filter (fun _ si -> ScheduledPayment.isSome si.ScheduledPayment)
                    |> Map.map (fun _ si -> si.ScheduledPayment)
                    |> Map.toArray

                let mergedPayments =
                    [| existingPayments; Map.toArray arrangementPaymentSchedule |]
                    |> Array.concat
                    |> Refinancing.mergeScheduledPayments

                let pMerged = {
                    arrangementParams with
                        Basic.ScheduleConfig = CustomSchedule mergedPayments
                        Advanced.TrimEnd = true
                }

                amortise pMerged actualPayments

        // if there is a defined end date, model the return-to-normal schedule after the arrangement ends
        let postArrangementSchedules =
            match arrangementConfig.ArrangementEndDate with
            | ValueNone -> ValueNone
            | ValueSome endDate ->
                match arrangementConfig.PostArrangementRescheduleParameters with
                | ValueNone -> ValueNone
                | ValueSome rp ->
                    let endDay = OffsetDay.fromDate p.Basic.StartDate endDate

                    // find the outstanding principal balance at the end of the arrangement
                    let outstandingBalance =
                        arrangementSchedules.AmortisationSchedule.ScheduleItems
                        |> Map.filter (fun d _ -> d <= endDay)
                        |> fun m ->
                            if m |> Map.isEmpty then
                                0L<Cent>
                            else
                                m |> Map.maxKeyValue |> snd |> _.PrincipalBalance

                    if outstandingBalance <= 0L<Cent> then
                        ValueNone
                    else
                        // build post-arrangement parameters starting from the end of the arrangement
                        let postParams = {
                            p with
                                Basic.StartDate = endDate
                                Basic.EvaluationDate = endDate
                                Basic.Principal = outstandingBalance
                                Basic.ScheduleConfig = rp.PaymentSchedule
                                Advanced.InterestConfig.PromotionalRates = rp.PromotionalInterestRates
                                Advanced.InterestConfig.RateOnNegativeBalance = rp.RateOnNegativeBalance
                                Advanced.SettlementDay = rp.SettlementDay
                                Advanced.TrimEnd = true
                        }

                        amortise postParams Map.empty |> ValueSome

        {
            ArrearsSummary = arrearsSummary
            ArrangementSchedules = arrangementSchedules
            PostArrangementSchedules = postArrangementSchedules
        }
