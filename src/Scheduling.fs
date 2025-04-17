namespace FSharp.Finance.Personal

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module Scheduling =

    open System
    open Calculation
    open DateDay
    open Formatting
    open UnitPeriod

    /// a rescheduled payment, including the day on which the payment was created
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type RescheduledPayment =
        {
            /// the original payment amount
            Value: int64<Cent>
            /// the day on which the rescheduled payment was created
            RescheduleDay: int<OffsetDay>
        }
        member x.Html =
            formatCent x.Value

    /// any original or rescheduled payment, affecting how any payment due is calculated
    [<StructuredFormatDisplay("{Html}")>]
    type ScheduledPayment =
        {
            /// any original payment
            Original: int64<Cent> voption
            /// the payment relating to the latest rescheduling, if any
            /// > NB: if set to `ValueSome 0L<Cent>` this indicates that the original payment is no longer due
            Rescheduled: RescheduledPayment voption
            /// any payments relating to previous reschedulings *sorted in creation order*, if any
            PreviousRescheduled: RescheduledPayment array
            /// any adjustment due to interest or charges being applied to the relevant payment rather than being amortised later
            Adjustment: int64<Cent>
            /// any reference numbers or other information pertaining to this payment
            Metadata: Map<string, obj>
        }
        /// HTML formatting to display the scheduled payment in a concise way
        member x.Html =
            let previous = if Array.isEmpty x.PreviousRescheduled then "" else x.PreviousRescheduled |> Array.map(fun pr -> $"<s><i>r</i> {formatCent pr.Value}</s>&nbsp;") |> Array.reduce (+)
            match x.Original, x.Rescheduled with
            | ValueSome o, ValueSome r when r.Value = 0L<Cent> ->
                $"""<i><s>original</i> {formatCent o}</s>{if previous = "" then "" else $"&nbsp;{previous}"}"""
            | ValueSome o, ValueSome r ->
                $"<s><i>o</i> {formatCent o}</s>&nbsp;{previous}<i>r</i> {formatCent r.Value}"
            | ValueSome o, ValueNone ->
                $"<i>original</i> {formatCent o}"
            | ValueNone, ValueSome r ->
                $"""{if previous = "" then "<i>rescheduled</i>&nbsp;" else previous}{formatCent r.Value}"""
            | ValueNone, ValueNone ->
                "<i>n/a<i>"
            |> fun s ->
                match x.Adjustment with
                | a when a < 0L<Cent> ->
                    $"{s}&nbsp;-&nbsp;{formatCent <| abs a}"
                | a when a > 0L<Cent> ->
                    $"{s}&nbsp;+&nbsp;{formatCent a}"
                | _ ->
                    s
    
    module ScheduledPayment =
        /// the total amount of the payment
        let total sp =
            match sp.Original, sp.Rescheduled with
            | _, ValueSome r ->
                r.Value
            | ValueSome o, ValueNone ->
                o
            | ValueNone, ValueNone ->
                0L<Cent>
            |> (+) sp.Adjustment
        /// whether the payment has either an original or a rescheduled value
        let isSome sp =
            sp.Original.IsSome || sp.Rescheduled.IsSome
        /// a default value with no data
        let zero =
            {
                Original = ValueNone
                Rescheduled = ValueNone
                PreviousRescheduled = [||]
                Adjustment = 0L<Cent>
                Metadata = Map.empty
            }
        /// a quick convenient method to create a basic scheduled payment
        let quick originalAmount rescheduledAmount =
            { zero with
                Original =  originalAmount
                Rescheduled = rescheduledAmount
            }

    /// the status of the payment, allowing for delays due to payment-provider processing times
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type ActualPaymentStatus =
        /// a write-off payment has been applied
        | WriteOff of WriteOff: int64<Cent>
        /// the payment has been initiated but is not yet confirmed
        | Pending of Pending: int64<Cent>
        /// the payment had been initiated but was not confirmed within the timeout
        | TimedOut of TimedOut: int64<Cent>
        /// the payment has been confirmed
        | Confirmed of Confirmed: int64<Cent>
        /// the payment has been failed, with optional charges (e.g. due to insufficient-funds penalties)
        | Failed of Failed: int64<Cent> * ChargeType: Charge.ChargeType voption
        /// HTML formatting to display the actual payment status in a readable format
        member aps.Html =
            match aps with
            | WriteOff amount -> $"<i>write-off</i> {formatCent amount}"
            | Pending amount -> $"<i>pending</i> {formatCent amount}"
            | TimedOut amount -> $"{formatCent amount} <i>timed out</i>"
            | Confirmed amount -> $"<i>confirmed</i> {formatCent amount}"
            | Failed (amount, ValueSome charge) -> $"{formatCent amount} <i>failed ({charge})</i>"
            | Failed (amount, ValueNone) -> $"{formatCent amount} <i>failed</i>"
    
    /// the status of the payment, allowing for delays due to payment-provider processing times
    module ActualPaymentStatus =
        /// the total amount of the payment
        let total = function
            | ActualPaymentStatus.WriteOff ap
            | ActualPaymentStatus.Pending ap
            | ActualPaymentStatus.Confirmed ap ->
                ap
            | ActualPaymentStatus.TimedOut _
            | ActualPaymentStatus.Failed _ ->
                0L<Cent>

    /// an actual payment made by the customer, optionally including metadata such as bank references etc.
    [<StructuredFormatDisplay("{Html}")>]
    type ActualPayment = {
        /// the status of the payment
        ActualPaymentStatus: ActualPaymentStatus
        /// any extra info such as references
        Metadata: Map<string, obj>
    }
    with
        /// HTML formatting to display the actual payment in a readable format
        member ap.Html =
            $"{ap.ActualPaymentStatus}"

    /// an actual payment made by the customer, optionally including metadata such as bank references etc.
    module ActualPayment =
        /// the total amount of the payment
        let total =
            _.ActualPaymentStatus >> ActualPaymentStatus.total
        let totalConfirmedOrWrittenOff =
            _.ActualPaymentStatus >> function ActualPaymentStatus.Confirmed ap -> ap | ActualPaymentStatus.WriteOff ap  -> ap | _ -> 0L<Cent>
        let totalPending =
            _.ActualPaymentStatus >> function ActualPaymentStatus.Pending ap -> ap | _ -> 0L<Cent>
        /// a quick convenient method to create a confirmed actual payment
        let quickConfirmed amount =
            {
                ActualPaymentStatus = ActualPaymentStatus.Confirmed amount
                Metadata = Map.empty
            }
        /// a quick convenient method to create a pending actual payment
        let quickPending amount =
            {
                ActualPaymentStatus = ActualPaymentStatus.Pending amount
                Metadata = Map.empty
            }
        /// a quick convenient method to create a failed actual payment along with any applicable penalty charges
        let quickFailed amount charges =
            {
                ActualPaymentStatus = ActualPaymentStatus.Failed (amount, charges)
                Metadata = Map.empty
            }
        /// a quick convenient method to create a written off actual payment
        let quickWriteOff amount =
            {
                ActualPaymentStatus = ActualPaymentStatus.WriteOff amount
                Metadata = Map.empty
            }

    /// the status of a payment made by the customer
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type PaymentStatus =
        /// no payment is required on the specified day
        | NoneScheduled
        /// a payment has been initiated but not yet confirmed
        | PaymentPending
        /// a scheduled payment was made in full and on time
        | PaymentMade
        /// no payment is due on the specified day because of earlier extra-/overpayments
        | NothingDue
        /// a scheduled payment is not paid on time, but is paid within the window
        | PaidLaterInFull
        /// a scheduled payment is not paid on time, but is partially paid within the window
        | PaidLaterOwing of Shortfall: int64<Cent>
        /// a scheduled payment was missed completely, i.e. not paid within the window
        | MissedPayment
        /// a scheduled payment was made on time but not in the full amount
        | Underpayment
        /// a scheduled payment was made on time but exceeded the full amount
        | Overpayment
        /// a payment was made on a day when no payments were scheduled
        | ExtraPayment
        /// a refund was processed
        | Refunded
        /// a scheduled payment is in the future (seen from the as-of date)
        | NotYetDue
        /// a scheduled payment has not been made on time but is within the late-charge grace period
        | PaymentDue
        /// a payment generated by a settlement quote
        | Generated
        /// no payment needed because the loan has already been settled
        | NoLongerRequired
        /// a schedule item generated to show the balances on the as-of date
        | InformationOnly
        /// HTML formatting to display the payment status in a readable format
        member ps.Html =
            match ps with
            | NoneScheduled -> "<i>none scheduled</i>"
            | PaymentPending -> "<i>payment pending</i>"
            | PaymentMade -> "<i>payment made</i>"
            | NothingDue -> "<i>nothing due</i>"
            | PaidLaterInFull -> "<i>paid later in full</i>"
            | PaidLaterOwing shortfall -> $"<i>paid later owing</i> {formatCent shortfall}"
            | MissedPayment -> "<i>missed payment</i>"
            | Underpayment -> "<i>underpayment</i>"
            | Overpayment -> "<i>overpayment</i>"
            | ExtraPayment -> "<i>extra payment</i>"
            | Refunded -> "<i>refunded</i>"
            | NotYetDue -> "<i>not yet due</i>"
            | PaymentDue -> "<i>payment due</i>"
            | Generated -> "<i>generated</i>"
            | NoLongerRequired -> "<i>no longer required</i>"
            | InformationOnly -> "<i>information only</i>"

    /// a regular schedule based on a unit-period config with a specific number of payments with an auto-calculated amount
    [<RequireQualifiedAccess; Struct>]
    type AutoGenerateSchedule = {
        // the unit-period config
        UnitPeriodConfig: UnitPeriod.Config
        // the length of the schedule
        ScheduleLength: ScheduleLength
    }

    /// the type of the schedule; for scheduled payments, this affects how any payment due is calculated
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type ScheduleType =
        /// an original schedule
        | Original
        /// a new schedule created after the original schedule, indicating the day it was created
        | Rescheduled of RescheduleDay: int<OffsetDay>
        /// HTML formatting to display the schedule type in a readable format
        member st.Html =
            match st with
            | Original -> "original"
            | Rescheduled rd -> $"rescheduled on day {rd}"

    /// a regular schedule based on a unit-period config with a specific number of payments of a specified amount
    [<RequireQualifiedAccess; Struct>]
    type FixedSchedule = {
        // the unit-period config
        UnitPeriodConfig: UnitPeriod.Config
        // the number of payments (unlimited by duration)
        PaymentCount: int
        // the value of each payment
        PaymentValue: int64<Cent>
        // whether this represents original or rescheduled payments
        ScheduleType: ScheduleType
    }

    /// whether a payment plan is generated according to a regular schedule or is an irregular array of payments
    [<Struct>]
    type ScheduleConfig =
        /// a schedule based on a unit-period config with a specific number of payments with an auto-calculated amount, optionally limited to a maximum duration
        | AutoGenerateSchedule of AutoGenerateSchedule: AutoGenerateSchedule
        /// a  schedule based on one or more unit-period configs each with a specific number of payments of a specified amount and type
        | FixedSchedules of FixedSchedules: FixedSchedule array
        /// just a bunch of payments
        | CustomSchedule of CustomSchedule: Map<int<OffsetDay>, ScheduledPayment>

    /// whether a payment plan is generated according to a regular schedule or is an irregular array of payments
    module ScheduleConfig =
        /// formats the schedule config as an HTML table
        let toHtmlTable scheduleConfig =
            match scheduleConfig with
            | AutoGenerateSchedule ags ->
                $"""
            <table>
                <tr>
                    <td>config: <i>auto-generate schedule</i></td>
                    <td>schedule length: <i>{ags.ScheduleLength}</i></td>
                </tr>
                <tr>
                    <td colspan="2" style="white-space: nowrap;">unit-period config: <i>{ags.UnitPeriodConfig}</i></td>
                </tr>
            </table>"""
            | FixedSchedules fsArray ->
                let renderRow (fs: FixedSchedule) =
                    $"""
                <tr>
                    <td>
                        <table>
                            <tr>
                                <td style="white-space: nowrap;">unit-period config: <i>{fs.UnitPeriodConfig}</i></td>
                                <td>payment count: <i>{fs.PaymentCount}</i></td>
                            </tr>
                            <tr>
                                <td>payment value: <i>{formatCent fs.PaymentValue}</i></td>
                                <td>schedule type: <i>{fs.ScheduleType.Html.Replace(" ", "&nbsp;")}</i></td>
                            </tr>
                        </table>
                    </td>
                </tr>"""
                $"""
            <table>
                <tr>
                    <td colspan="2">config: <i>fixed schedules</i></td>
                </tr>{fsArray |> Array.map renderRow |> String.concat ""}
            </table>"""
            | CustomSchedule cs ->
                let renderRow day sp =
                    $"""
                <tr>
                    <td>day: <i>{day}</i></td>
                    <td>scheduled payment: <i>{sp}</i></td>
                </tr>"""
                $"""
            <table>
                <tr>
                    <td colspan="2">config: <i>custom schedule</i></td>
                </tr>{cs |> Map.toList |> List.map (fun (day, sp) -> renderRow day sp) |> String.concat ""}
            </table>"""

    /// when calculating the level payments, whether the final payment should be lower or higher than the level payment
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type LevelPaymentOption =
        /// the final payment must be lower than the level payment
        | LowerFinalPayment
        /// the final payment must be around the same as the level payment but can be higher or lower
        | SimilarFinalPayment
        /// the final payment must be higher than the level payment
        | HigherFinalPayment
        /// HTML formatting to display the level-payment option in a readable format
        member spo.Html =
            match spo with
            | LowerFinalPayment -> "lower final payment"
            | SimilarFinalPayment -> "similar final payment"
            | HigherFinalPayment -> "higher final payment"

    /// when calculating the level payments, whether the final payment should be lower or higher than the level payment
    module LevelPaymentOption =
        /// converts the level-payment option to a target tolerance for use in the bisection method solver
        let toTargetTolerance = function
            | LowerFinalPayment -> BelowZero
            | SimilarFinalPayment -> AroundZero
            | HigherFinalPayment -> AboveZero

    /// whether to stick to scheduled payment amounts or add charges and interest to them
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type ScheduledPaymentOption =
        /// keep to the scheduled payment amounts even if this results in an open balance
        | AsScheduled
        /// add any charges and interest to the payment in order to close the balance at the end of the schedule
        | AddChargesAndInterest
        /// HTML formatting to display the scheduled payment option in a readable format
        member spo.Html =
            match spo with
            | AsScheduled -> "as scheduled"
            | AddChargesAndInterest -> "add charges and interest"

    /// how to handle a final balance if not closed: leave it open or modify/add payments at the end of the schedule
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type CloseBalanceOption =
        /// do not modify the final payment and leave any open balance as is
        | LeaveOpenBalance
        /// increase the final payment to close any open balance
        | IncreaseFinalPayment
        /// add a single payment to the schedule to close any open balance immediately (interval based on unit-period config)
        | AddSingleExtraPayment
        /// add multiple payments to the schedule to close any open balance gradually (interval based on unit-period config)
        | AddMultipleExtraPayments
        /// HTML formatting to display the close balance option in a readable format
        member cbo.Html =
            match cbo with
            | LeaveOpenBalance -> "leave open balance"
            | IncreaseFinalPayment -> "increase final payment"
            | AddSingleExtraPayment -> "add single extra payment"
            | AddMultipleExtraPayments -> "add multiple extra payments"

    /// how to handle cases where the payment due is less than the minimum that payment providers can process
     [<Struct; StructuredFormatDisplay("{Html}")>]
    type MinimumPayment =
        /// no minimum payment
        | NoMinimumPayment
        /// add the payment due to the next payment or close the balance if the final payment
        | DeferOrWriteOff of DeferOrWriteOff: int64<Cent>
        /// take the minimum payment regardless
        | ApplyMinimumPayment of ApplyMinimumPayment: int64<Cent>
        /// HTML formatting to display the minimum payment in a readable format
        member mp.Html =
            match mp with
            | NoMinimumPayment -> "no minimum payment"
            | DeferOrWriteOff upToValue -> $"defer or write off up to {formatCent upToValue}"
            | ApplyMinimumPayment minimumPayment -> $"apply minimum payment of {formatCent minimumPayment}"

    /// how to treat scheduled payments
    type PaymentConfig = {
        /// what tolerance to use for the final principal balance when calculating the level payments
        LevelPaymentOption: LevelPaymentOption
        /// whether to modify scheduled payment amounts to keep the schedule on-track
        ScheduledPaymentOption: ScheduledPaymentOption
        /// whether to leave a final balance open or close it using various methods
        CloseBalanceOption: CloseBalanceOption
        /// how to round payments
        Rounding: Rounding
        /// the minimum payment that can be taken and how to handle it
        Minimum: MinimumPayment
        /// the duration after which a pending payment is considered a missed payment and charges are applied
        Timeout: int<DurationDay>
    }

    /// how to treat scheduled payments
    module PaymentConfig =
        ///formats the payment config as an HTML table
        let toHtmlTable paymentConfig =
            $"""
            <table>
                <tr>
                    <td>scheduling: <i>{paymentConfig.ScheduledPaymentOption}</i></td>
                    <td>balance-close: <i>{paymentConfig.CloseBalanceOption.Html.Replace(" ", "&nbsp;")}</i></td>
                </tr>
                <tr>
                    <td>rounding: <i>{paymentConfig.Rounding}</i></td>
                    <td>timeout: <i>{paymentConfig.Timeout}</i></td>
                </tr>
                <tr>
                    <td colspan='2'>minimum: <i>{paymentConfig.Minimum.Html.Replace(" ", "&nbsp;")}</i></td>
                </tr>
                <tr>
                    <td colspan='2'>level-payment option: <i>{paymentConfig.LevelPaymentOption.Html.Replace(" ", "&nbsp;")}</i></td>
                </tr>
            </table>"""

   /// parameters for creating a payment schedule
    type Parameters = {
        /// the date on which the schedule is inspected, typically today, but can be used to inspect it at any point (affects e.g. whether scheduled payments are deemed as not yet due)
        AsOfDate: Date
        /// the start date of the schedule, typically the day on which the principal is advanced
        StartDate: Date
        /// the principal
        Principal: int64<Cent>
        /// the scheduled payments or the parameters for generating them
        ScheduleConfig: ScheduleConfig
        /// options relating to scheduled payments
        PaymentConfig: PaymentConfig
        /// options relating to fee
        FeeConfig: Fee.Config option
        /// options relating to charges
        ChargeConfig: Charge.Config option
        /// options relating to interest
        InterestConfig: Interest.Config
    }

    /// parameters for creating a payment schedule
    module Parameters =
        /// formats the parameters as an HTML table
        let toHtmlTable parameters =
            $"""
<table>
    <tr>
        <td>As-of</td>
        <td>%A{parameters.AsOfDate}</td>
    </tr>
    <tr>
        <td>Start</td>
        <td>%A{parameters.StartDate}</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>{formatCent parameters.Principal}</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>{ScheduleConfig.toHtmlTable parameters.ScheduleConfig}
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>{PaymentConfig.toHtmlTable parameters.PaymentConfig}
        </td>
    </tr>
    <tr>
        <td>Fee options</td>
        <td>{Fee.Config.toHtmlTable parameters.FeeConfig}
        </td>
    </tr>
    <tr>
        <td>Charge options</td>
        <td>{Charge.Config.toHtmlTable parameters.ChargeConfig}
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>{Interest.Config.toHtmlTable parameters.InterestConfig}
        </td>
    </tr>
</table>"""

     /// a scheduled payment item, with running calculations of interest and principal balance
    type SimpleItem = {
        /// the day expressed as an offset from the start date
        Day: int<OffsetDay>
        /// the scheduled payment
        ScheduledPayment: ScheduledPayment
        /// the simple interest accrued since the previous payment
        SimpleInterest: decimal<Cent>
        /// the interest portion paid off by the payment
        InterestPortion: int64<Cent>
        /// the principal portion paid off by the payment
        PrincipalPortion: int64<Cent>
        /// the interest balance carried forward
        InterestBalance: int64<Cent>
        /// the principal balance carried forward
        PrincipalBalance: int64<Cent>
        /// the total simple interest accrued from the start date to the current date
        TotalSimpleInterest: decimal<Cent>
        /// the total interest payable from the start date to the current date
        TotalInterest: int64<Cent>
        /// the total principal payable from the start date to the current date
        TotalPrincipal: int64<Cent>
    }
        
    /// a scheduled payment item, with running calculations of interest and principal balance
    module SimpleItem =
        /// a default value with no data
        let initial =
            { 
                Day = 0<OffsetDay>
                ScheduledPayment = ScheduledPayment.zero
                SimpleInterest = 0m<Cent>
                InterestPortion = 0L<Cent>
                PrincipalPortion = 0L<Cent>
                InterestBalance = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                TotalSimpleInterest = 0m<Cent>
                TotalInterest = 0L<Cent>
                TotalPrincipal = 0L<Cent>
            }
        /// formats the simple item as an HTML row
        let toHtmlRow simpleItem = $"""
    <tr style="text-align: right;">
        <td class="ci00">{simpleItem.Day}</td>
        <td class="ci01" style="white-space: nowrap;">{simpleItem.ScheduledPayment |> ScheduledPayment.total |> formatCent}</td>
        <td class="ci02">{formatDecimalCent simpleItem.SimpleInterest}</td>
        <td class="ci03">{formatCent simpleItem.InterestPortion}</td>
        <td class="ci04">{formatCent simpleItem.PrincipalPortion}</td>
        <td class="ci05">{formatCent simpleItem.InterestBalance}</td>
        <td class="ci06">{formatCent simpleItem.PrincipalBalance}</td>
        <td class="ci07">{formatDecimalCent simpleItem.TotalSimpleInterest}</td>
        <td class="ci08">{formatCent simpleItem.TotalInterest}</td>
        <td class="ci09">{formatCent simpleItem.TotalPrincipal}</td>
    </tr>"""

    /// final statistics based on the payments being made on time and in full
    [<Struct>]
    type SimpleScheduleStats = {
        /// the initial interest balance when using the add-on interest method
        InitialInterestBalance: int64<Cent>
        /// the final day of the schedule, expressed as an offset from the start date
        FinalScheduledPaymentDay: int<OffsetDay>
        /// the amount of all the payments except the final one
        LevelPayment: int64<Cent>
        /// the amount of the final payment
        FinalPayment: int64<Cent>
        /// the total of all payments
        ScheduledPaymentTotal: int64<Cent>
        /// the total principal paid, which should equal the initial advance (principal)
        PrincipalTotal: int64<Cent>
        /// the total interest accrued
        InterestTotal: int64<Cent>
        /// the APR according to the calculation method specified in the schedule parameters and based on the schedule being settled as agreed
        InitialApr: Percent
        /// the cost of borrowing, expressed as a ratio of interest to principal
        InitialCostToBorrowingRatio: Percent
    }

    /// statistics resulting from the simple schedule calculations
    module SimpleScheduleStats =
        /// renders the final APR as a string, or "n/a" if not available
        let finalAprString = function
        | Solution.Found _, ValueSome percent -> $"{percent}"
        | _ -> "<i>n/a</i>"
        /// formats the schedule stats as an HTML table (excluding the items, which are rendered separately)
        let toHtmlTable schedule =
            $"""
<table>
    <tr>
        <td>Initial interest balance: <i>{formatCent schedule.InitialInterestBalance}</i></td>
        <td>Initial cost-to-borrowing ratio: <i>{schedule.InitialCostToBorrowingRatio}</i></td>
        <td>Initial APR: <i>{schedule.InitialApr}</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>{formatCent schedule.LevelPayment}</i></td>
        <td>Final payment: <i>{formatCent schedule.FinalPayment}</i></td>
        <td>Final scheduled payment day: <i>{schedule.FinalScheduledPaymentDay}</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>{formatCent schedule.ScheduledPaymentTotal}</i></td>
        <td>Total principal: <i>{formatCent schedule.PrincipalTotal}</i></td>
        <td>Total interest: <i>{formatCent schedule.InterestTotal}</i></td>
    </tr>
</table>
"""

    ///  a schedule of payments, with statistics
    type SimpleSchedule = {
        /// the day, expressed as an offset from the start date, on which the schedule is inspected
        AsOfDay: int<OffsetDay>
        /// the items of the schedule
        Items: SimpleItem array
        /// the statistics from the schedule
        Stats: SimpleScheduleStats
    }

    ///  a schedule of payments, with statistics
    module SimpleSchedule =
        /// formats the schedule items as an HTML table (stats can be rendered separately)
        let toHtmlTable schedule =
            $"""
<table>
    <thead style="vertical-align: bottom;">
        <th style="text-align: right;">Day</th>
        <th style="text-align: right;">Scheduled payment</th>
        <th style="text-align: right;">Simple interest</th>
        <th style="text-align: right;">Interest portion</th>
        <th style="text-align: right;">Principal portion</th>
        <th style="text-align: right;">Interest balance</th>
        <th style="text-align: right;">Principal balance</th>
        <th style="text-align: right;">Total simple interest</th>
        <th style="text-align: right;">Total interest</th>
        <th style="text-align: right;">Total principal</th>
    </thead>{schedule.Items |> Array.map SimpleItem.toHtmlRow |> String.concat ""}
</table>"""

        /// renders the simple schedule as an HTML table within a markup file, which can both be previewed in VS Code and imported as XML into Excel
        let outputHtmlToFile folder title description sp schedule =
            let htmlTitle = $"<h2>{title}</h2>"
            let htmlSchedule = toHtmlTable schedule
            let htmlDescription = $"""
<h4>Description</h4>
<p><i>{description}</i></p>"""
            let htmlParams = $"""
<h4>Parameters</h4>{Parameters.toHtmlTable sp}"""
            let htmlDatestamp = $"""
<p>Generated: <i>{DateTime.Now.ToString "yyyy-MM-dd"} using library version {Calculation.libraryVersion}</i></p>"""
            let htmlScheduleStats = $"""
<h4>Initial Stats</h4>{SimpleScheduleStats.toHtmlTable schedule.Stats}"""
            let filename = $"out/{folder}/{title}.md"
            $"""{htmlTitle}{htmlSchedule}{htmlDescription}{htmlDatestamp}{htmlParams}{htmlScheduleStats}"""
            |> outputToFile' filename false

    /// convert an option to a value option
    let toValueOption = function Some x -> ValueSome x | None -> ValueNone

    /// generates a map of offset days and payments based on a start date and payment schedule
    let generatePaymentMap startDate paymentSchedule =
        match paymentSchedule with
        | CustomSchedule payments ->
            if Map.isEmpty payments then
                Map.empty
            else
                payments
        | FixedSchedules regularFixedSchedules ->
            regularFixedSchedules
            |> Array.collect(fun rfs ->
                if rfs.PaymentCount = 0 then
                    [||]
                else
                    let unitPeriodConfigStartDate = Config.startDate rfs.UnitPeriodConfig
                    if startDate > unitPeriodConfigStartDate then
                        [||]
                    else
                        generatePaymentSchedule (PaymentCount rfs.PaymentCount) Direction.Forward rfs.UnitPeriodConfig
                        |> Array.map(OffsetDay.fromDate startDate >> fun d ->
                            let originalValue, rescheduledValue =
                                match rfs.ScheduleType with
                                | ScheduleType.Original -> ValueSome rfs.PaymentValue, ValueNone
                                | ScheduleType.Rescheduled rescheduleDay -> ValueNone, ValueSome { Value = rfs.PaymentValue; RescheduleDay = rescheduleDay }
                            d, ScheduledPayment.quick originalValue rescheduledValue
                        )
            )
            |> Array.sortBy fst
            |> Array.groupBy fst
            |> Array.map(fun (d, spp) ->
                let original =
                    spp
                    |> Array.map (snd >> _.Original)
                    |> Array.tryFind _.IsSome
                    |> toValueOption
                    |> ValueOption.flatten
                let rescheduled =
                    spp
                    |> Array.map (snd >> _.Rescheduled)
                    |> Array.filter _.IsSome
                    |> Array.tryLast
                    |> toValueOption
                    |> ValueOption.flatten
                d, { ScheduledPayment.zero with Original = original; Rescheduled = rescheduled }
            )
            |> Map.ofArray
        | AutoGenerateSchedule rs ->
            match rs.ScheduleLength with
            | PaymentCount 0
            | MaxDuration 0<DurationDay> ->
                Map.empty
            | _ ->
                let unitPeriodConfigStartDate = Config.startDate rs.UnitPeriodConfig
                if startDate > unitPeriodConfigStartDate then
                    Map.empty
                else
                    generatePaymentSchedule rs.ScheduleLength Direction.Forward rs.UnitPeriodConfig
                    |> Array.map(fun d -> OffsetDay.fromDate startDate d, ScheduledPayment.quick (ValueSome 0L<Cent>) ValueNone)
                    |> Map.ofArray

    // calculate the approximate level-payment value
    let calculateLevelPayment paymentCount paymentRounding principal fee interest =
        if paymentCount = 0 then
            0L<Cent>
        else
            (Cent.toDecimalCent principal + Cent.toDecimalCent fee + interest) / decimal paymentCount
            |> Cent.fromDecimalCent paymentRounding
            

    // calculates the interest accruing on a particular day based on the interest method, payment and previous balances, taking into account any daily and total interest caps
    let calculateInterest sp interestMethod payment previousItem day =
        match interestMethod with
        | Interest.Method.Simple ->
            Interest.dailyRates sp.StartDate false sp.InterestConfig.StandardRate sp.InterestConfig.PromotionalRates previousItem.Day day
            |> Interest.calculate previousItem.PrincipalBalance sp.InterestConfig.Cap.DailyAmount sp.InterestConfig.Rounding
        | Interest.Method.AddOn ->
            Cent.toDecimalCent payment
            |> min (Cent.toDecimalCent previousItem.InterestBalance)

    // generates a schedule item for a particular day by calculating the interest accruing and apportioning the scheduled payment to interest then principal
    let generateItem sp interestMethod scheduledPayment previousItem day =
        let scheduledPaymentTotal = ScheduledPayment.total scheduledPayment
        let simpleInterest =
            calculateInterest sp Interest.Method.Simple scheduledPaymentTotal previousItem day
            |> fun i -> Interest.Cap.cappedAddedValue sp.InterestConfig.Cap.TotalAmount sp.Principal previousItem.TotalSimpleInterest i
        let interestPortion =
            calculateInterest sp interestMethod scheduledPaymentTotal previousItem day
            |> fun i -> Interest.Cap.cappedAddedValue sp.InterestConfig.Cap.TotalAmount sp.Principal (Cent.toDecimalCent previousItem.TotalInterest) i
            |> Cent.fromDecimalCent sp.InterestConfig.Rounding
        let principalPortion = scheduledPaymentTotal - interestPortion
        let simpleItem =
            {
                Day = day
                ScheduledPayment = scheduledPayment
                SimpleInterest = simpleInterest
                InterestPortion = interestPortion
                PrincipalPortion = principalPortion
                InterestBalance = match interestMethod with Interest.Method.AddOn -> previousItem.InterestBalance - interestPortion | _ -> 0L<Cent>
                PrincipalBalance = previousItem.PrincipalBalance - principalPortion
                TotalSimpleInterest = previousItem.TotalSimpleInterest + simpleInterest
                TotalInterest = previousItem.TotalInterest + interestPortion
                TotalPrincipal = previousItem.TotalPrincipal + principalPortion
            }
        simpleItem

    // for the add-on interest method: take the final interest total from the schedule and use it as the initial interest balance and calculate a new schedule,
    // repeating until the two figures equalise, which yields the maximum interest that can be accrued with this interest method
    let maximiseInterest sp paymentDays firstItem paymentCount feeTotal (paymentMap: Map<int<OffsetDay>, ScheduledPayment>) (stateOption: struct {| Iteration: int; InterestBalance: decimal<Cent> |} voption) =
        if stateOption.IsNone then
            None
        elif Array.isEmpty paymentDays then
            None
        else
            let state = stateOption.Value
            let regularScheduledPayment = calculateLevelPayment paymentCount sp.PaymentConfig.Rounding sp.Principal feeTotal state.InterestBalance
            let newSchedule =
                paymentDays
                |> Array.scan (fun simpleItem pd ->
                    let scheduledPayment =
                        match sp.ScheduleConfig with
                        | AutoGenerateSchedule _ -> ScheduledPayment.quick (ValueSome regularScheduledPayment) ValueNone
                        | FixedSchedules _
                        | CustomSchedule _ -> paymentMap[pd]
                    generateItem sp Interest.Method.AddOn scheduledPayment simpleItem pd
                ) { firstItem with InterestBalance = state.InterestBalance |> Cent.fromDecimalCent sp.InterestConfig.Rounding }
            let finalInterestTotal =
                newSchedule
                |> Array.last
                |> _.TotalSimpleInterest
                |> max 0m<Cent> // interest must not go negative
                |> Interest.Cap.cappedAddedValue sp.InterestConfig.Cap.TotalAmount sp.Principal 0m<Cent>

            let principalBalance = newSchedule |> Array.last |> _.PrincipalBalance
            let tolerance = int64 paymentCount * 1L<Cent>
            let minBalance, maxBalance =
                match sp.PaymentConfig.LevelPaymentOption with
                    | LowerFinalPayment ->
                        -tolerance, 0L<Cent>
                    | SimilarFinalPayment ->
                        -tolerance, tolerance
                    | HigherFinalPayment ->
                        0L<Cent>, tolerance

            let difference = state.InterestBalance - finalInterestTotal |> Cent.fromDecimalCent sp.InterestConfig.Rounding
            if difference = 0L<Cent> && principalBalance >= minBalance && principalBalance <= maxBalance || state.Iteration = 100 then
                Some (newSchedule, ValueNone)
            else
                Some (newSchedule, ValueSome struct {| Iteration = state.Iteration + 1; InterestBalance = finalInterestTotal |})

    // calculate the initial total interest accruing over the entire schedule
    // for the add-on interest method: this is only an initial value that will need to be iterated against the schedule to determine the actual value
    // for other interest methods: the initial interest is zero as interest is accrued later
    let totalAddOnInterest sp finalPaymentDay =
        let dailyInterestRate = sp.InterestConfig.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        match sp.InterestConfig.Method with
        | Interest.Method.AddOn ->
            Cent.toDecimalCent sp.Principal * dailyInterestRate * decimal finalPaymentDay
            |> Interest.Cap.cappedAddedValue sp.InterestConfig.Cap.TotalAmount sp.Principal 0m<Cent>
            |> Cent.fromDecimalCent sp.InterestConfig.Rounding
        | _ ->
            0L<Cent>

    // generates a payment value based on an approximation, creates a schedule based on that payment value and returns the principal balance at the end of the schedule,
    // the intention being to use this generator in an iteration by varying the payment value until the final principal balance is zero
    let generatePaymentValue sp paymentDays firstItem roughPayment =
        let scheduledPayment =
            roughPayment
            |> Cent.round sp.PaymentConfig.Rounding
            |> fun rp -> ScheduledPayment.quick (ValueSome rp) ValueNone
        let schedule =
            paymentDays
            |> Array.fold(fun simpleItem pd ->
                generateItem sp sp.InterestConfig.Method scheduledPayment simpleItem pd
            ) firstItem
        let principalBalance = decimal schedule.PrincipalBalance
        principalBalance, ScheduledPayment.total schedule.ScheduledPayment |> Cent.toDecimal

    /// calculates the number of days between two offset days on which interest is chargeable
    let calculate sp =
        // create a map of scheduled payments for a given schedule configuration, using the payment day as the key (only one scheduled payment per day)
        let paymentMap = generatePaymentMap sp.StartDate sp.ScheduleConfig
        // get the payment days for use in further calculations
        let paymentDays = paymentMap |> Map.keys |> Seq.toArray
        // take the last payment day for use in further calculations
        let finalScheduledPaymentDay = paymentDays |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        // get the payment count for use in further calculations
        let paymentCount = paymentDays |> Array.length
        // calculate the total fee value for the entire schedule
        let feeTotal = Fee.total sp.FeeConfig sp.Principal
        // get the initial interest balance
        let initialInterestBalance = totalAddOnInterest sp finalScheduledPaymentDay
        // create the initial item for the schedule based on the initial interest and principal
        // note: for simplicity, principal includes fee
        let initialSimpleItem = { SimpleItem.initial with InterestBalance = initialInterestBalance; PrincipalBalance = sp.Principal + feeTotal }
        // get the appropriate tolerance steps for determining payment value
        // note: tolerance steps allow for gradual relaxation of the tolerance if no solution is found for the original tolerance
        let toleranceSteps = ToleranceSteps.forPaymentValue paymentCount
        // generate a schedule based on a map of scheduled payments
        let generateItems (payments: Map<int<OffsetDay>, ScheduledPayment>) =
            paymentDays
            |> Array.scan(fun simpleItem pd -> generateItem sp sp.InterestConfig.Method payments[pd] simpleItem pd) initialSimpleItem
        // generates a schedule based on the schedule configuration
        let schedule =
            match sp.ScheduleConfig with
            | AutoGenerateSchedule _ ->
                // calculate the estimated interest payable over the entire schedule
                let estimatedInterestTotal =
                    match sp.InterestConfig.Method with
                    | Interest.Method.AddOn ->
                        initialInterestBalance |> Cent.toDecimalCent
                    | Interest.Method.Simple ->
                        let dailyInterestRate = sp.InterestConfig.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
                        Cent.toDecimalCent sp.Principal * dailyInterestRate * decimal finalScheduledPaymentDay * 0.5m
                // determines the payment value and generates the schedule iteratively based on that
                let generator = generatePaymentValue sp paymentDays initialSimpleItem
                let iterationLimit = 100u
                let initialGuess = calculateLevelPayment paymentCount sp.PaymentConfig.Rounding sp.Principal feeTotal estimatedInterestTotal |> Cent.toDecimalCent |> decimal
                match Array.solveBisection generator iterationLimit initialGuess (LevelPaymentOption.toTargetTolerance sp.PaymentConfig.LevelPaymentOption) toleranceSteps with
                    | Solution.Found (paymentValue, _, _) ->
                        let paymentMap' = paymentMap |> Map.map(fun _ sp -> { sp with Original = sp.Original |> ValueOption.map(fun _ -> paymentValue |> Cent.fromDecimal) })
                        generateItems paymentMap'
                    | _ ->
                        [||]
            | FixedSchedules _
            | CustomSchedule _ ->
                // the days and payment values are known so the schedule can be generated directly
                generateItems paymentMap
        // fail if the schedule is empty
        if Array.isEmpty schedule then
            failwith "Unable to calculate simple schedule"
        else
        // for the add-on interest method, now the schedule days and payment values are known, iterate through the schedule until the final principal balance is zero
        // note: this step is required because the initial interest balance is non-zero, meaning that any payments are apportioned to interest first, meaning that
        // the principal balance is paid off more slowly than it would otherwise be; this, in turn, generates higher interest, which leads to a higher initial interest
        // balance, so the process must be repeated until the total interest and the initial interest are equalised
        let schedule' =
            match sp.InterestConfig.Method with
            | Interest.Method.AddOn ->
                let finalInterestTotal = schedule |> Array.last |> _.TotalSimpleInterest
                ValueSome struct {| Iteration = 0; InterestBalance = finalInterestTotal |}
                |> Array.unfold (maximiseInterest sp paymentDays initialSimpleItem paymentCount feeTotal paymentMap)
                |> Array.last
            | _ ->
                schedule
        // handle any principal balance overpayment (due to rounding) on the final payment of a schedule
        let items =
            schedule'
            |> Array.map(fun si ->
                if si.Day = finalScheduledPaymentDay && sp.ScheduleConfig.IsAutoGenerateSchedule then
                    let adjustedPayment =
                        si.ScheduledPayment
                        |> fun p ->
                            { si.ScheduledPayment with
                                Original = if p.Rescheduled.IsNone then p.Original |> ValueOption.map(fun o -> o + si.PrincipalBalance) else p.Original
                                Rescheduled = if p.Rescheduled.IsSome then p.Rescheduled |> ValueOption.map(fun r -> { r with Value = r.Value + si.PrincipalBalance }) else p.Rescheduled
                            }
                    let adjustedPrincipal = si.PrincipalPortion + si.PrincipalBalance
                    let adjustedTotalPrincipal = si.TotalPrincipal + si.PrincipalBalance
                    { si with
                        ScheduledPayment = adjustedPayment
                        PrincipalPortion = adjustedPrincipal
                        PrincipalBalance = 0L<Cent>
                        TotalPrincipal = adjustedTotalPrincipal
                    }
                else
                    si
            )
        // calculate the total principal paid over the schedule
        let principalTotal = items |> Array.sumBy _.PrincipalPortion
        // calculate the total interest accrued over the schedule
        let interestTotal = items |> Array.sumBy _.InterestPortion
        // calculate the APR (using the appropriate calculation method) based on the finalised schedule
        let aprSolution =
            items
            |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
            |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.TransferDate = sp.StartDate.AddDays(int si.Day); Apr.Value = ScheduledPayment.total si.ScheduledPayment })
            |> Apr.calculate sp.InterestConfig.AprMethod sp.Principal sp.StartDate
        // take the scheduled payments for use in further calculations
        let scheduledPayments = items |> Array.map _.ScheduledPayment |> Array.filter ScheduledPayment.isSome
        // determine the final payment value, which is often different from the level payment value
        let finalPayment = scheduledPayments |> Array.tryLast |> Option.map ScheduledPayment.total |> Option.defaultValue 0L<Cent>
        // return the schedule (as `Items`) plus associated information and statistics
        {
            AsOfDay = (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
            Items = items
            Stats = {
                InitialInterestBalance =
                    match sp.InterestConfig.Method with
                    | Interest.Method.AddOn ->
                        interestTotal
                    | _ ->
                        0L<Cent>
                FinalScheduledPaymentDay = finalScheduledPaymentDay
                LevelPayment =
                    scheduledPayments
                    |> Array.countBy ScheduledPayment.total
                    |> fun a -> if Seq.isEmpty a then None else a |> Seq.maxBy snd |> fst |> Some
                    |> Option.defaultValue finalPayment
                FinalPayment = finalPayment
                ScheduledPaymentTotal =
                    scheduledPayments
                    |> Array.sumBy ScheduledPayment.total
                PrincipalTotal = principalTotal
                InterestTotal = interestTotal
                InitialApr = Apr.toPercent sp.InterestConfig.AprMethod aprSolution
                InitialCostToBorrowingRatio =
                    if principalTotal = 0L<Cent> then
                        Percent 0m
                    else
                        decimal (feeTotal + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 2
            }
        }

    /// merges scheduled payments, determining the currently valid original and rescheduled payments, and preserving a record of any previous payments that have been superseded
    let mergeScheduledPayments (scheduledPayments: (int<OffsetDay> * ScheduledPayment) array) =
        // get a sorted array of all days on which payments are rescheduled
        let rescheduleDays =
            scheduledPayments
            |> Array.map snd
            |> Array.choose(fun sp -> if sp.Rescheduled.IsSome then Some sp.Rescheduled.Value.RescheduleDay else None)
            |> Array.distinct
            |> Array.sort
        // return the list of scheduled payments with the original and rescheduled payments merged
        scheduledPayments
        //group and sort by day
        |> Array.groupBy fst
        |> Array.sortBy fst
        |> Array.mapFold(fun previousRescheduleDay (offsetDay, map) ->
            // inspect the scheduled payment
            let sp = map |> Array.map snd
            // get any original payment due on the day
            let original = sp |> Array.tryFind _.Original.IsSome |> toValueOption
            // get any rescheduled payments due on the day, ordering them so that the most recently rescheduled payments come first
            let rescheduled = sp |> Array.filter _.Rescheduled.IsSome |> Array.sortByDescending _.Rescheduled.Value.RescheduleDay |> Array.toList
            // split any rescheduled payments into latest and previous
            let latestRescheduling, previousReschedulings =
                match rescheduled with
                | [r] -> ValueSome r, []
                | r :: pr -> ValueSome r, pr
                | _ -> ValueNone, []
            // update the previous reschedule day, if any
            let newRescheduleDay = rescheduleDays |> Array.tryFind(fun d -> offsetDay >= d) |> toValueOption |> ValueOption.orElse previousRescheduleDay
            // create the modified scheduled payment
            let newScheduledPayment =
                match original, latestRescheduling with
                // if there are any rescheduled payments, add the latest as well as the list of previously rescheduled payments on the day (also include the original if any on the day)
                | _, ValueSome r ->
                    Some (offsetDay, {
                        Original = original |> ValueOption.bind _.Original
                        Rescheduled = r.Rescheduled
                        PreviousRescheduled = previousReschedulings |> List.rev |> List.map _.Rescheduled.Value |> List.toArray
                        Adjustment = r.Adjustment
                        Metadata = r.Metadata
                    })
                // if there is no rescheduled payment on the day, but just an original payment, include the original payment as-is
                // note: if the original payment day is preceded by any rescheduling, then assume that this cancels the original payment, so enter this as a zero-valued rescheduled payment on the day
                | ValueSome o, ValueNone ->
                    Some (offsetDay, {
                        Original = o.Original
                        Rescheduled =
                            newRescheduleDay
                            |> ValueOption.bind(fun nrd ->
                                if offsetDay >= nrd then
                                    //overwrite original scheduled payments from start of rescheduled payments
                                    ValueSome { Value = 0L<Cent>; RescheduleDay = nrd }
                                else
                                    ValueNone
                            )
                        PreviousRescheduled = [||]
                        Adjustment = o.Adjustment
                        Metadata = o.Metadata
                    })
                // if there are no original or rescheduled payments, ignore
                | ValueNone, ValueNone ->
                    None
            newScheduledPayment, newRescheduleDay
        ) ValueNone
        |> fst
        |> Array.choose id
        // convert the result to a map
        |> Map.ofArray

    /// a breakdown of how an actual payment is apportioned to principal, fee, interest and charges
    type Apportionment = {
        PrincipalPortion: int64<Cent>
        FeePortion: int64<Cent>
        InterestPortion: int64<Cent>
        ChargesPortion: int64<Cent>
    }

    /// a breakdown of how an actual payment is apportioned to principal, fee, interest and charges
    module Apportionment =
        /// add principal, fee, interest and charges to an existing apportionment
        let Add principal fee interest charges apportionment =
            { apportionment with 
                PrincipalPortion = apportionment.PrincipalPortion + principal
                FeePortion = apportionment.FeePortion + fee
                InterestPortion = apportionment.InterestPortion + interest
                ChargesPortion = apportionment.ChargesPortion + charges
            }

        /// a default value for an apportionment, with all portions set to zero
        let Zero = {
            PrincipalPortion = 0L<Cent>
            FeePortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
        }

        /// the total value of all the portions of an apportionment
        let Total apportionment =
            apportionment.PrincipalPortion + apportionment.FeePortion + apportionment.InterestPortion + apportionment.ChargesPortion 

    /// a generated payment, where applicable
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type GeneratedPayment =
        /// no generated payment is required
        | NoGeneratedPayment
        /// the payment value will be generated later
        | ToBeGenerated
        /// the generated payment value
        | GeneratedValue of int64<Cent>
        /// HTML formatting to display the generated payment in a readable format
        member x.Html =
            match x with
            | NoGeneratedPayment
            | ToBeGenerated ->
                "<i>n/a</i>"
            | GeneratedValue gv ->
                formatCent gv

    /// a generated payment, where applicable
    module GeneratedPayment =
        /// the total value of the generated payment
        let Total = function
            | GeneratedValue gv -> gv
            | _ -> 0L<Cent>

    /// the intended day on which to quote a settlement
    [<RequireQualifiedAccess; Struct>]
    type SettlementDay =
        /// quote a settlement figure on the specified day
        | SettlementOn of SettlementDay: int<OffsetDay>
        /// quote a settlement figure on the as-of day
        | SettlementOnAsOfDay
