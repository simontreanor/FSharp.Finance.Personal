namespace FSharp.Finance.Personal

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module Scheduling =

    open Calculation
    open DateDay
    open Formatting
    open UnitPeriod

    /// a rescheduled payment, including the day on which the payment was created
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type RescheduledPayment = {
        /// the original payment amount
        Value: int64<Cent>
        /// the day on which the rescheduled payment was created
        RescheduleDay: int<OffsetDay>
    } with

        member x.Html = formatCent x.Value

    /// any original or rescheduled payment, affecting how any payment due is calculated
    [<StructuredFormatDisplay("{Html}")>]
    type ScheduledPayment = {
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
    } with

        /// HTML formatting to display the scheduled payment in a concise way
        member x.Html =
            let previous =
                if Array.isEmpty x.PreviousRescheduled then
                    ""
                else
                    x.PreviousRescheduled
                    |> Array.map (fun pr -> $"<s><i>r</i> {formatCent pr.Value}</s>&nbsp;")
                    |> Array.reduce (+)

            match x.Original, x.Rescheduled with
            | ValueSome o, ValueSome r when r.Value = 0L<Cent> ->
                $"""<i><s>original</i> {formatCent o}</s>{if previous = "" then " cancelled" else $"&nbsp;{previous}"}"""
            | ValueSome o, ValueSome r -> $"<s><i>o</i> {formatCent o}</s>&nbsp;{previous}<i>r</i> {formatCent r.Value}"
            | ValueSome o, ValueNone -> $"<i>original</i> {formatCent o}"
            | ValueNone, ValueSome r ->
                $"""{if previous = "" then
                         "<i>rescheduled</i>&nbsp;"
                     else
                         previous}{formatCent r.Value}"""
            | ValueNone, ValueNone -> "<i>n/a<i>"
            |> fun s ->
                match x.Adjustment with
                | a when a < 0L<Cent> -> $"{s}&nbsp;-&nbsp;{formatCent <| abs a}"
                | a when a > 0L<Cent> -> $"{s}&nbsp;+&nbsp;{formatCent a}"
                | _ -> s

    module ScheduledPayment =
        /// the total amount of the payment
        let total sp =
            match sp.Original, sp.Rescheduled with
            | _, ValueSome r -> r.Value
            | ValueSome o, ValueNone -> o
            | ValueNone, ValueNone -> 0L<Cent>
            |> (+) sp.Adjustment

        /// whether the payment has either an original or a rescheduled value
        let isSome sp =
            sp.Original.IsSome || sp.Rescheduled.IsSome

        /// a default value with no data
        let zero = {
            Original = ValueNone
            Rescheduled = ValueNone
            PreviousRescheduled = [||]
            Adjustment = 0L<Cent>
            Metadata = Map.empty
        }

        /// a quick convenient method to create a basic scheduled payment
        let quick originalAmount rescheduledAmount = {
            zero with
                Original = originalAmount
                Rescheduled = rescheduledAmount
        }

    /// the status of the payment, allowing for delays due to payment-provider processing times
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type ActualPaymentStatus =
        /// a write-off payment has been applied
        | WriteOff of int64<Cent>
        /// the payment has been initiated but is not yet confirmed
        | Pending of int64<Cent>
        /// the payment had been initiated but was not confirmed within the timeout
        | TimedOut of int64<Cent>
        /// the payment has been confirmed
        | Confirmed of int64<Cent>
        /// the payment has been failed, with optional charges (e.g. due to insufficient-funds penalties)
        | Failed of int64<Cent> * Charge.ChargeType voption

        /// HTML formatting to display the actual payment status in a readable format
        member aps.Html =
            match aps with
            | WriteOff amount -> $"<i>write-off</i> {formatCent amount}"
            | Pending amount -> $"<i>pending</i> {formatCent amount}"
            | TimedOut amount -> $"{formatCent amount} <i>timed out</i>"
            | Confirmed amount -> $"<i>confirmed</i> {formatCent amount}"
            | Failed(amount, ValueSome charge) -> $"{formatCent amount} <i>failed ({charge})</i>"
            | Failed(amount, ValueNone) -> $"{formatCent amount} <i>failed</i>"

    /// the status of the payment, allowing for delays due to payment-provider processing times
    module ActualPaymentStatus =
        /// the total amount of the payment
        let total =
            function
            | ActualPaymentStatus.WriteOff ap
            | ActualPaymentStatus.Pending ap
            | ActualPaymentStatus.Confirmed ap -> ap
            | ActualPaymentStatus.TimedOut _
            | ActualPaymentStatus.Failed _ -> 0L<Cent>

    /// an actual payment made by the customer, optionally including metadata such as bank references etc.
    [<StructuredFormatDisplay("{Html}")>]
    type ActualPayment = {
        /// the status of the payment
        ActualPaymentStatus: ActualPaymentStatus
        // // /// a map of the scheduled payments and amounts covered by this payment
        // // ScheduledPayments: Map<int<OffsetDay>, int64<Cent>>
        /// any extra info such as references
        Metadata: Map<string, obj>
    } with

        /// HTML formatting to display the actual payment in a readable format
        member ap.Html =
            [
                yield $"{ap.ActualPaymentStatus}"
                // // if not <| Map.isEmpty ap.ScheduledPayments then
                // //     yield $"{ap.ScheduledPayments |> Map.toStringOrNa}"
                if not <| Map.isEmpty ap.Metadata then
                    yield $"{ap.Metadata |> Map.toStringOrNa}"
            ]
            |> String.concat "; "

    /// an actual payment made by the customer, optionally including metadata such as bank references etc.
    module ActualPayment =
        /// the total amount of the payment
        let total = _.ActualPaymentStatus >> ActualPaymentStatus.total

        let totalMade =
            _.ActualPaymentStatus
            >> function
                | ActualPaymentStatus.Confirmed ap -> ap
                | ActualPaymentStatus.WriteOff ap -> ap
                | _ -> 0L<Cent>

        let totalPending =
            _.ActualPaymentStatus
            >> function
                | ActualPaymentStatus.Pending ap -> ap
                | _ -> 0L<Cent>

        /// a quick convenient method to create a confirmed actual payment
        let quickConfirmed amount = {
            ActualPaymentStatus = ActualPaymentStatus.Confirmed amount
            // // ScheduledPayments = Map.empty
            Metadata = Map.empty
        }

        /// a quick convenient method to create a pending actual payment
        let quickPending amount = {
            ActualPaymentStatus = ActualPaymentStatus.Pending amount
            // // ScheduledPayments = Map.empty
            Metadata = Map.empty
        }

        /// a quick convenient method to create a failed actual payment along with any applicable penalty charges
        let quickFailed amount charges = {
            ActualPaymentStatus = ActualPaymentStatus.Failed(amount, charges)
            // // ScheduledPayments = Map.empty
            Metadata = Map.empty
        }

        /// a quick convenient method to create a written off actual payment
        let quickWriteOff amount = {
            ActualPaymentStatus = ActualPaymentStatus.WriteOff amount
            // // ScheduledPayments = Map.empty
            Metadata = Map.empty
        }

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

    /// type alias to represent a scheduled payment indexed by offset day
    type internal PaymentMap = Map<int<OffsetDay>, ScheduledPayment>

    /// whether a payment plan is generated according to a regular schedule or is an irregular array of payments
    [<Struct>]
    type ScheduleConfig =
        /// a schedule based on a unit-period config with a specific number of payments with an auto-calculated amount, optionally limited to a maximum duration
        | AutoGenerateSchedule of AutoGenerateSchedule: AutoGenerateSchedule
        /// a  schedule based on one or more unit-period configs each with a specific number of payments of a specified amount and type
        | FixedSchedules of FixedSchedules: FixedSchedule array
        /// just a bunch of payments
        | CustomSchedule of CustomSchedule: PaymentMap

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
                let renderRow day (sp: ScheduledPayment) =
                    $"""
                <tr>
                    <td>day: <i>{OffsetDay.toInt day}</i></td>
                    <td>scheduled payment: <i>{sp}</i></td>
                </tr>"""

                $"""
            <table>
                <tr>
                    <td colspan="2">config: <i>custom schedule</i></td>
                </tr>{cs
                      |> Map.toList
                      |> List.map (fun (day, sp) -> renderRow day sp)
                      |> String.concat ""}
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
        let toTargetTolerance =
            function
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

    /// how to handle cases where the payment due is less than the minimum that payment providers can process
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type MinimumPayment =
        /// no minimum payment
        | NoMinimumPayment
        /// add the payment due to the next payment or close the balance if the final payment
        | DeferOrWriteOff of int64<Cent>
        /// take the minimum payment regardless
        | ApplyMinimumPayment of int64<Cent>

        /// HTML formatting to display the minimum payment in a readable format
        member mp.Html =
            match mp with
            | NoMinimumPayment -> "no minimum payment"
            | DeferOrWriteOff upToValue -> $"defer or write off up to {formatCent upToValue}"
            | ApplyMinimumPayment minimumPayment -> $"apply minimum payment of {formatCent minimumPayment}"

    module Payment =

        /// basic payment options
        [<Struct>]
        type BasicConfig = {
            /// what tolerance to use for the final principal balance when calculating the level payments
            LevelPaymentOption: LevelPaymentOption
            /// how to round payments
            Rounding: Rounding
        }

        /// basic payment options
        module BasicConfig =
            ///formats the payment config as an HTML table
            let toHtmlTable basicConfig =
                $"""
            <table>
                <tr>
                    <td>rounding: <i>{basicConfig.Rounding}</i></td>
                </tr>
                <tr>
                    <td>level-payment option: <i>{basicConfig.LevelPaymentOption.Html.Replace(" ", "&nbsp;")}</i></td>
                </tr>
            </table>"""

        /// advanced payment options
        [<Struct>]
        type AdvancedConfig = {
            /// whether to modify scheduled payment amounts to keep the schedule on-track
            ScheduledPaymentOption: ScheduledPaymentOption
            /// the minimum payment that can be taken and how to handle it
            Minimum: MinimumPayment
            /// the duration after which a pending payment is considered a missed payment and charges are applied
            Timeout: int<DurationDay>
        }

        /// advanced payment options
        module AdvancedConfig =
            ///formats the payment config as an HTML table
            let toHtmlTable advancedConfig =
                $"""
                <table>
                    <tr>
                        <td>scheduling: <i>{advancedConfig.ScheduledPaymentOption}</i></td>
                        <td>timeout: <i>{advancedConfig.Timeout} days</i></td>
                    </tr>
                    <tr>
                        <td colspan="2">minimum: <i>{advancedConfig.Minimum.Html.Replace(" ", "&nbsp;")}</i></td>
                    </tr>
                </table>"""

    /// parameters for creating a payment schedule
    [<Struct>]
    type BasicParameters = {
        /// the date on which the schedule is evaluated, typically today, but can be used to evaluate it at any point (affects e.g. whether scheduled payments are deemed as not yet due)
        EvaluationDate: Date
        /// the start date of the schedule, typically the day on which the principal is advanced
        StartDate: Date
        /// the principal
        Principal: int64<Cent>
        /// the scheduled payments or the parameters for generating them
        ScheduleConfig: ScheduleConfig
        /// options relating to scheduled payments
        PaymentConfig: Payment.BasicConfig
        /// options relating to fees
        FeeConfig: Fee.BasicConfig voption
        /// options relating to interest
        InterestConfig: Interest.BasicConfig
    }

    /// parameters for creating a payment schedule
    module BasicParameters =
        /// formats the parameters as an HTML table
        let toHtmlTable bp =
            $"""
<table>
    <tr>
        <td>Evaluation Date</td>
        <td>%A{bp.EvaluationDate}</td>
    </tr>
    <tr>
        <td>Start Date</td>
        <td>%A{bp.StartDate}</td>
    </tr>
    <tr>
        <td>Principal</td>
        <td>{formatCent bp.Principal}</td>
    </tr>
    <tr>
        <td>Schedule options</td>
        <td>{ScheduleConfig.toHtmlTable bp.ScheduleConfig}
        </td>
    </tr>
    <tr>
        <td>Payment options</td>
        <td>{Payment.BasicConfig.toHtmlTable bp.PaymentConfig}
        </td>
    </tr>
    <tr>
        <td>Fee options</td>
        <td>{Fee.BasicConfig.toHtmlTable bp.FeeConfig}
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>{Interest.Config.toHtmlTable bp.InterestConfig}
        </td>
    </tr>
</table>"""

    /// the intended day on which to quote a settlement
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type SettlementDay =
        /// quote a settlement figure on the evaluation day
        | SettlementOnEvaluationDay
        /// no settlement figure is required
        | NoSettlement

        member x.Html =
            match x with
            | SettlementOnEvaluationDay -> $"<i>on evaluation day</i>"
            | NoSettlement -> "<i>n/a</i>"

    /// parameters required for applying payments and amortisation
    type AdvancedParameters = {
        // payment options relevant to the amortisation schedule
        PaymentConfig: Payment.AdvancedConfig
        // interest options relevant to the amortisation schedule
        InterestConfig: Interest.AdvancedConfig
        // fee options relevant to the amortisation schedule
        FeeConfig: Fee.AdvancedConfig voption
        /// options relating to charges
        ChargeConfig: Charge.Config option
        /// whether and on what day to generate a settlement figure
        SettlementDay: SettlementDay
        /// whether to trim unrequired scheduled payments from the end of the schedule
        TrimEnd: bool
    }

    /// parameters required for amortisation
    module AdvancedParameters =
        ///formats the payment config as an HTML table
        let toHtmlTable ap =
            $"""
<table>
    <tr>
        <td>Payment options</td>
        <td>{Payment.AdvancedConfig.toHtmlTable ap.PaymentConfig}
        </td>
    </tr>
    <tr>
        <td>Interest options</td>
        <td>{Interest.AdvancedConfig.toHtmlTable ap.InterestConfig}
        </td>
    </tr>
    <tr>
        <td>Fee options</td>
        <td>{Fee.AdvancedConfig.toHtmlTable ap.FeeConfig}
        </td>
    </tr>
    <tr>
        <td>Charge options</td>
        <td>{Charge.Config.toHtmlTable ap.ChargeConfig}
        </td>
    </tr>
    <tr>
        <td>Settlement day</td><td><i>{ap.SettlementDay}</i></td>
    </tr>
    <tr>
        <td>Trim unrequired payments</td><td><i>{ap.TrimEnd.ToString().ToLower()}</i></td>
    </tr>
</table>"""

    /// basic schedule generation parameters and advanced parameters for amortisation
    [<RequireQualifiedAccess>]
    type Parameters = {
        /// the basic schedule generation parameters
        Basic: BasicParameters
        /// the basic schedule generation parameters
        Advanced: AdvancedParameters
    }

    /// a scheduled payment item, with running calculations of interest and principal balance
    type BasicItem = {
        /// the day expressed as an offset from the start date
        Day: int<OffsetDay>
        /// the scheduled payment
        ScheduledPayment: ScheduledPayment
        /// the actuarial interest accrued since the previous payment
        ActuarialInterest: decimal<Cent>
        /// the interest portion paid off by the payment
        InterestPortion: int64<Cent>
        /// the principal portion paid off by the payment
        PrincipalPortion: int64<Cent>
        /// the interest balance carried forward
        InterestBalance: int64<Cent>
        /// the principal balance carried forward
        PrincipalBalance: int64<Cent>
        /// the total actuarial interest accrued from the start date to the current date
        TotalActuarialInterest: decimal<Cent>
        /// the total interest payable from the start date to the current date
        TotalInterest: int64<Cent>
        /// the total principal payable from the start date to the current date
        TotalPrincipal: int64<Cent>
    }

    /// a scheduled payment item, with running calculations of interest and principal balance
    module BasicItem =
        /// a default value with no data
        let zero = {
            Day = 0<OffsetDay>
            ScheduledPayment = ScheduledPayment.zero
            ActuarialInterest = 0m<Cent>
            InterestPortion = 0L<Cent>
            PrincipalPortion = 0L<Cent>
            InterestBalance = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            TotalActuarialInterest = 0m<Cent>
            TotalInterest = 0L<Cent>
            TotalPrincipal = 0L<Cent>
        }

        /// formats the basic item as an HTML row
        let toHtmlRow basicItem =
            $"""
    <tr style="text-align: right;">
        <td class="ci00">{basicItem.Day}</td>
        <td class="ci01" style="white-space: nowrap;">{basicItem.ScheduledPayment |> ScheduledPayment.total |> formatCent}</td>
        <td class="ci02">{formatDecimalCent basicItem.ActuarialInterest}</td>
        <td class="ci03">{formatCent basicItem.InterestPortion}</td>
        <td class="ci04">{formatCent basicItem.PrincipalPortion}</td>
        <td class="ci05">{formatCent basicItem.InterestBalance}</td>
        <td class="ci06">{formatCent basicItem.PrincipalBalance}</td>
        <td class="ci07">{formatDecimalCent basicItem.TotalActuarialInterest}</td>
        <td class="ci08">{formatCent basicItem.TotalInterest}</td>
        <td class="ci09">{formatCent basicItem.TotalPrincipal}</td>
    </tr>"""

    /// final statistics based on the payments being made on time and in full
    [<Struct>]
    type InitialStats = {
        /// the initial interest balance when using the add-on interest method
        InitialInterestBalance: int64<Cent>
        /// the final day of the schedule, expressed as an offset from the start date
        LastScheduledPaymentDay: int<OffsetDay>
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

    /// statistics resulting from the basic schedule calculations
    module InitialStats =
        /// renders the final APR as a string, or "n/a" if not available
        let finalAprString =
            function
            | Solution.Found _, ValueSome percent -> $"{percent}"
            | _ -> "<i>n/a</i>"

        /// formats the schedule stats as an HTML table (excluding the items, which are rendered separately)
        let toHtmlTable initialStats =
            $"""
<table>
    <tr>
        <td>Initial interest balance: <i>{formatCent initialStats.InitialInterestBalance}</i></td>
        <td>Initial cost-to-borrowing ratio: <i>{initialStats.InitialCostToBorrowingRatio}</i></td>
        <td>Initial APR: <i>{initialStats.InitialApr}</i></td>
    </tr>
    <tr>
        <td>Level payment: <i>{formatCent initialStats.LevelPayment}</i></td>
        <td>Final payment: <i>{formatCent initialStats.FinalPayment}</i></td>
        <td>Last scheduled payment day: <i>{initialStats.LastScheduledPaymentDay}</i></td>
    </tr>
    <tr>
        <td>Total scheduled payments: <i>{formatCent initialStats.ScheduledPaymentTotal}</i></td>
        <td>Total principal: <i>{formatCent initialStats.PrincipalTotal}</i></td>
        <td>Total interest: <i>{formatCent initialStats.InterestTotal}</i></td>
    </tr>
</table>"""

    ///  a schedule of payments, with statistics
    type BasicSchedule = {
        /// the day, expressed as an offset from the start date, on which the schedule is evaluated
        EvaluationDay: int<OffsetDay>
        /// the items of the schedule
        Items: BasicItem array
        /// the statistics from the schedule
        Stats: InitialStats
    }

    ///  a schedule of payments, with statistics
    module BasicSchedule =
        /// formats the schedule items as an HTML table (stats can be rendered separately)
        let toHtmlTable schedule =
            $"""
<table>
    <thead style="vertical-align: bottom;">
        <th style="text-align: right;">Day</th>
        <th style="text-align: right;">Scheduled payment</th>
        <th style="text-align: right;">Actuarial interest</th>
        <th style="text-align: right;">Interest portion</th>
        <th style="text-align: right;">Principal portion</th>
        <th style="text-align: right;">Interest balance</th>
        <th style="text-align: right;">Principal balance</th>
        <th style="text-align: right;">Total actuarial interest</th>
        <th style="text-align: right;">Total interest</th>
        <th style="text-align: right;">Total principal</th>
    </thead>{schedule.Items |> Array.map BasicItem.toHtmlRow |> String.concat ""}
</table>"""

        /// renders the basic schedule as an HTML table within a markup file, which can both be previewed in VS Code and imported as XML into Excel
        let outputHtmlToFile folder title description bp schedule =
            let htmlTitle = $"<h2>{title}</h2>"
            let htmlSchedule = toHtmlTable schedule

            let htmlDescription =
                $"""
<h4>Description</h4>
<p><i>{description}</i></p>"""

            let htmlParams =
                $"""
<h4>Basic Parameters</h4>{BasicParameters.toHtmlTable bp}"""

            let htmlDatestamp =
                $"""
<p>Generated: <i><a href="../GeneratedDate.html">see details</a></i></p>"""

            let htmlFinalStats =
                $"""
<h4>Initial Stats</h4>{InitialStats.toHtmlTable schedule.Stats}"""

            let filename = $"out/{folder}/{title}.md"

            $"""{htmlTitle}{htmlSchedule}{htmlDescription}{htmlDatestamp}{htmlParams}{htmlFinalStats}"""
            |> outputToFile' filename false

    /// convert an option to a value option
    let toValueOption =
        function
        | Some x -> ValueSome x
        | None -> ValueNone

    /// generates a map of offset days and payments based on a start date and payment schedule
    let generatePaymentMap startDate paymentSchedule =
        match paymentSchedule with
        | CustomSchedule payments -> if Map.isEmpty payments then Map.empty else payments
        | FixedSchedules regularFixedSchedules ->
            regularFixedSchedules
            |> Array.collect (fun rfs ->
                if rfs.PaymentCount = 0 then
                    [||]
                else
                    let unitPeriodConfigStartDate = Config.startDate rfs.UnitPeriodConfig

                    if startDate > unitPeriodConfigStartDate then
                        [||]
                    else
                        generatePaymentSchedule (PaymentCount rfs.PaymentCount) Direction.Forward rfs.UnitPeriodConfig
                        |> Array.map (
                            OffsetDay.fromDate startDate
                            >> fun d ->
                                let originalValue, rescheduledValue =
                                    match rfs.ScheduleType with
                                    | ScheduleType.Original -> ValueSome rfs.PaymentValue, ValueNone
                                    | ScheduleType.Rescheduled rescheduleDay ->
                                        ValueNone,
                                        ValueSome {
                                            Value = rfs.PaymentValue
                                            RescheduleDay = rescheduleDay
                                        }

                                d, ScheduledPayment.quick originalValue rescheduledValue
                        )
            )
            |> Array.sortBy fst
            |> Array.groupBy fst
            |> Array.map (fun (d, spp) ->
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

                d,
                {
                    ScheduledPayment.zero with
                        Original = original
                        Rescheduled = rescheduled
                }
            )
            |> Map.ofArray
        | AutoGenerateSchedule rs ->
            match rs.ScheduleLength with
            | PaymentCount 0
            | MaxDuration(_, 0<DurationDay>) -> Map.empty
            | _ ->
                let unitPeriodConfigStartDate = Config.startDate rs.UnitPeriodConfig

                if startDate > unitPeriodConfigStartDate then
                    Map.empty
                else
                    generatePaymentSchedule rs.ScheduleLength Direction.Forward rs.UnitPeriodConfig
                    |> Array.map (fun d ->
                        OffsetDay.fromDate startDate d, ScheduledPayment.quick (ValueSome 0L<Cent>) ValueNone
                    )
                    |> Map.ofArray

    // calculate the approximate level-payment value
    let calculateLevelPayment principal fee interest paymentCount paymentRounding =
        if paymentCount = 0 then
            0L<Cent>
        else
            (Cent.toDecimalCent principal + Cent.toDecimalCent fee + interest)
            / decimal paymentCount
            |> Cent.fromDecimalCent paymentRounding


    // calculates the interest accruing on a particular day based on the interest method, payment and previous balances, taking into account any daily and total interest caps
    let calculateInterest bp interestMethod payment previousItem day =
        match interestMethod with
        | Interest.Method.Actuarial ->
            Interest.dailyRates bp.StartDate false bp.InterestConfig.StandardRate [||] previousItem.Day day
            |> Interest.calculate
                previousItem.PrincipalBalance
                bp.InterestConfig.Cap.DailyAmount
                bp.InterestConfig.Rounding
        | Interest.Method.AddOn ->
            Cent.toDecimalCent payment
            |> min (Cent.toDecimalCent previousItem.InterestBalance)

    // generates a schedule item for a particular day by calculating the interest accruing and apportioning the scheduled payment to interest then principal
    let generateItem bp interestMethod scheduledPayment previousItem day =
        let scheduledPaymentTotal = ScheduledPayment.total scheduledPayment

        let actuarialInterest =
            calculateInterest bp Interest.Method.Actuarial scheduledPaymentTotal previousItem day
            |> fun i ->
                Interest.Cap.cappedAddedValue
                    bp.InterestConfig.Cap.TotalAmount
                    bp.Principal
                    previousItem.TotalActuarialInterest
                    i

        let interestPortion =
            calculateInterest bp interestMethod scheduledPaymentTotal previousItem day
            |> fun i ->
                Interest.Cap.cappedAddedValue
                    bp.InterestConfig.Cap.TotalAmount
                    bp.Principal
                    (Cent.toDecimalCent previousItem.TotalInterest)
                    i
            |> Cent.fromDecimalCent bp.InterestConfig.Rounding

        let principalPortion = scheduledPaymentTotal - interestPortion

        let basicItem = {
            Day = day
            ScheduledPayment = scheduledPayment
            ActuarialInterest = actuarialInterest
            InterestPortion = interestPortion
            PrincipalPortion = principalPortion
            InterestBalance =
                match interestMethod with
                | Interest.Method.AddOn -> previousItem.InterestBalance - interestPortion
                | _ -> 0L<Cent>
            PrincipalBalance = previousItem.PrincipalBalance - principalPortion
            TotalActuarialInterest = previousItem.TotalActuarialInterest + actuarialInterest
            TotalInterest = previousItem.TotalInterest + interestPortion
            TotalPrincipal = previousItem.TotalPrincipal + principalPortion
        }

        basicItem

    // the state of the interest maximisation process, which is used to iterate over the schedule until the interest balance converges
    [<Struct>]
    type InterestBalance = {
        Iteration: int
        InterestBalance: decimal<Cent>
    }

    // for the add-on interest method: take the final interest total from the schedule and use it as the initial interest balance and calculate a new schedule,
    // repeating until the two figures equalise, which yields the maximum interest that can be accrued with this interest method
    let equaliseInterest bp paymentDays firstItem paymentCount feeTotal (paymentMap: PaymentMap) state =
        if state = ValueOption<InterestBalance>.None then
            None
        elif Array.isEmpty paymentDays then
            None
        else
            let state = state.Value

            let regularScheduledPayment =
                calculateLevelPayment bp.Principal feeTotal state.InterestBalance paymentCount bp.PaymentConfig.Rounding

            let newSchedule =
                paymentDays
                |> Array.scan
                    (fun basicItem pd ->
                        let scheduledPayment =
                            match bp.ScheduleConfig with
                            | AutoGenerateSchedule _ ->
                                ScheduledPayment.quick (ValueSome regularScheduledPayment) ValueNone
                            | FixedSchedules _
                            | CustomSchedule _ -> paymentMap[pd]

                        generateItem bp Interest.Method.AddOn scheduledPayment basicItem pd
                    )
                    {
                        firstItem with
                            InterestBalance = state.InterestBalance |> Cent.fromDecimalCent bp.InterestConfig.Rounding
                    }

            let finalInterestTotal =
                newSchedule
                |> Array.last
                |> _.TotalActuarialInterest
                |> max 0m<Cent> // interest must not go negative
                |> Interest.Cap.cappedAddedValue bp.InterestConfig.Cap.TotalAmount bp.Principal 0m<Cent>

            let principalBalance = newSchedule |> Array.last |> _.PrincipalBalance
            let tolerance = int64 paymentCount * 1L<Cent>

            let minBalance, maxBalance =
                match bp.PaymentConfig.LevelPaymentOption with
                | LowerFinalPayment -> -tolerance, 0L<Cent>
                | SimilarFinalPayment -> -tolerance, tolerance
                | HigherFinalPayment -> 0L<Cent>, tolerance

            let difference =
                state.InterestBalance - finalInterestTotal
                |> Cent.fromDecimalCent bp.InterestConfig.Rounding

            if
                difference = 0L<Cent>
                && principalBalance >= minBalance
                && principalBalance <= maxBalance
                || state.Iteration = 100
            then
                Some(newSchedule, ValueNone)
            else
                Some(
                    newSchedule,
                    ValueSome {
                        Iteration = state.Iteration + 1
                        InterestBalance = finalInterestTotal
                    }
                )

    // calculate the initial total interest accruing over the entire schedule
    // for the add-on interest method: this is only an initial value that will need to be iterated against the schedule to determine the actual value
    // for other interest methods: the initial interest is zero as interest is accrued later
    let totalAddOnInterest (bp: BasicParameters) finalPaymentDay =
        let dailyInterestRate =
            bp.InterestConfig.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        match bp.InterestConfig.Method with
        | Interest.Method.AddOn ->
            Cent.toDecimalCent bp.Principal * dailyInterestRate * decimal finalPaymentDay
            |> Interest.Cap.cappedAddedValue bp.InterestConfig.Cap.TotalAmount bp.Principal 0m<Cent>
            |> Cent.fromDecimalCent bp.InterestConfig.Rounding
        | _ -> 0L<Cent>

    // generates a payment value based on an approximation, creates a schedule based on that payment value and returns the principal balance at the end of the schedule,
    // the intention being to use this generator in an iteration by varying the payment value until the final principal balance is zero
    let generatePaymentValue (bp: BasicParameters) paymentDays firstItem roughPayment =
        let scheduledPayment =
            roughPayment
            |> Cent.round bp.PaymentConfig.Rounding
            |> fun rp -> ScheduledPayment.quick (ValueSome rp) ValueNone

        let schedule =
            paymentDays
            |> Array.fold
                (fun basicItem pd -> generateItem bp bp.InterestConfig.Method scheduledPayment basicItem pd)
                firstItem

        let principalBalance = decimal schedule.PrincipalBalance
        principalBalance, ScheduledPayment.total schedule.ScheduledPayment |> Cent.toDecimal

    /// handle any principal balance overpayment (due to rounding) on the final payment of a schedule
    let adjustFinalPayment finalScheduledPaymentDay isAutoGenerateSchedule basicItems =
        basicItems
        |> Array.map (fun bi ->
            if bi.Day = finalScheduledPaymentDay && isAutoGenerateSchedule then
                let adjustedPayment =
                    bi.ScheduledPayment
                    |> fun sp -> {
                        bi.ScheduledPayment with
                            Original =
                                if sp.Rescheduled.IsNone then
                                    sp.Original |> ValueOption.map (fun o -> o + bi.PrincipalBalance)
                                else
                                    sp.Original
                            Rescheduled =
                                if sp.Rescheduled.IsSome then
                                    sp.Rescheduled
                                    |> ValueOption.map (fun r -> {
                                        r with
                                            Value = r.Value + bi.PrincipalBalance
                                    })
                                else
                                    sp.Rescheduled
                    }

                let adjustedPrincipal = bi.PrincipalPortion + bi.PrincipalBalance
                let adjustedTotalPrincipal = bi.TotalPrincipal + bi.PrincipalBalance

                {
                    bi with
                        ScheduledPayment = adjustedPayment
                        PrincipalPortion = adjustedPrincipal
                        PrincipalBalance = 0L<Cent>
                        TotalPrincipal = adjustedTotalPrincipal
                }
            else
                bi
        )

    /// calculates the number of days between two offset days on which interest is chargeable
    let calculateBasicSchedule bp =
        // create a map of scheduled payments for a given schedule configuration, using the payment day as the key (only one scheduled payment per day)
        let paymentMap = generatePaymentMap bp.StartDate bp.ScheduleConfig
        // get the payment days for use in further calculations
        let paymentDays = paymentMap |> Map.keys |> Seq.toArray
        // take the last payment day for use in further calculations
        let finalScheduledPaymentDay =
            paymentDays |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        // get the payment count for use in further calculations
        let paymentCount = paymentDays |> Array.length
        // calculate the total fee value for the entire schedule
        let feeTotal = Fee.total bp.FeeConfig bp.Principal
        // get the initial interest balance
        let initialInterestBalance = totalAddOnInterest bp finalScheduledPaymentDay
        // create the initial item for the schedule based on the initial interest and principal
        // note: for simplicity, principal includes fee
        let initialBasicItem = {
            BasicItem.zero with
                InterestBalance = initialInterestBalance
                PrincipalBalance = bp.Principal + feeTotal
        }
        // get the appropriate tolerance steps for determining payment value
        // note: tolerance steps allow for gradual relaxation of the tolerance if no solution is found for the original tolerance
        let toleranceSteps = ToleranceSteps.forPaymentValue paymentCount
        // generate a schedule based on a map of scheduled payments
        let generateItems (payments: PaymentMap) =
            paymentDays
            |> Array.scan
                (fun basicItem pd -> generateItem bp bp.InterestConfig.Method payments[pd] basicItem pd)
                initialBasicItem
        // generates a schedule based on the schedule configuration
        let basicItems =
            match bp.ScheduleConfig with
            | AutoGenerateSchedule _ ->
                // calculate the estimated interest payable over the entire schedule
                let roughInterest =
                    match bp.InterestConfig.Method with
                    | Interest.Method.AddOn -> initialInterestBalance |> Cent.toDecimalCent
                    | Interest.Method.Actuarial ->
                        let dailyInterestRate =
                            bp.InterestConfig.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

                        Cent.toDecimalCent bp.Principal
                        * dailyInterestRate
                        * decimal finalScheduledPaymentDay
                        * Fraction.toDecimal (Fraction.Simple(2, 3))
                // determines the payment value and generates the schedule iteratively based on that
                let generator = generatePaymentValue bp paymentDays initialBasicItem
                let iterationLimit = 100u

                let roughPayment =
                    calculateLevelPayment bp.Principal feeTotal roughInterest paymentCount bp.PaymentConfig.Rounding
                    |> Cent.toDecimalCent
                    |> decimal

                match
                    Array.solveBisection
                        generator
                        iterationLimit
                        roughPayment
                        (LevelPaymentOption.toTargetTolerance bp.PaymentConfig.LevelPaymentOption)
                        toleranceSteps
                with
                | Solution.Found(paymentValue, _, _) ->
                    let paymentMap' =
                        paymentMap
                        |> Map.map (fun _ sp -> {
                            sp with
                                Original = sp.Original |> ValueOption.map (fun _ -> paymentValue |> Cent.fromDecimal)
                        })

                    generateItems paymentMap'
                | _ -> [||]
            | FixedSchedules _
            | CustomSchedule _ ->
                // the days and payment values are known so the schedule can be generated directly
                generateItems paymentMap
        // fail if the schedule is empty
        if Array.isEmpty basicItems then
            failwith "Unable to calculate basic schedule"
        else
            // for the add-on interest method, now the schedule days and payment values are known, iterate through the schedule until the final principal balance is zero
            // note: this step is required because the initial interest balance is non-zero, meaning that any payments are apportioned to interest first, meaning that
            // the principal balance is paid off at a different pace than it would otherwise be; this, in turn, generates different interest, which leads to a different
            // initial interest balance, so the process must be repeated until the total interest and the initial interest are equalised
            let items =
                match bp.InterestConfig.Method with
                | Interest.Method.AddOn ->
                    let finalInterestTotal = basicItems |> Array.last |> _.TotalActuarialInterest

                    ValueSome {
                        Iteration = 0
                        InterestBalance = finalInterestTotal
                    }
                    |> Array.unfold (equaliseInterest bp paymentDays initialBasicItem paymentCount feeTotal paymentMap)
                    |> Array.last
                | _ -> basicItems
                |> adjustFinalPayment finalScheduledPaymentDay bp.ScheduleConfig.IsAutoGenerateSchedule
            // calculate the total principal paid over the schedule
            let principalTotal = items |> Array.sumBy _.PrincipalPortion
            // calculate the total interest accrued over the schedule
            let interestTotal = items |> Array.sumBy _.InterestPortion
            // calculate the APR (using the appropriate calculation method) based on the finalised schedule
            let aprSolution =
                items
                |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
                |> Array.map (fun si -> {
                    Apr.TransferType = Apr.Payment
                    Apr.TransferDate = bp.StartDate.AddDays(int si.Day)
                    Apr.Value = ScheduledPayment.total si.ScheduledPayment
                })
                |> Apr.calculate bp.InterestConfig.AprMethod bp.Principal bp.StartDate
            // take the scheduled payments for use in further calculations
            let scheduledPayments =
                items |> Array.map _.ScheduledPayment |> Array.filter ScheduledPayment.isSome
            // determine the final payment value, which is often different from the level payment value
            let finalPayment =
                scheduledPayments
                |> Array.tryLast
                |> Option.map ScheduledPayment.total
                |> Option.defaultValue 0L<Cent>
            // return the schedule (as `Items`) plus associated information and statistics
            {
                EvaluationDay = (bp.EvaluationDate - bp.StartDate).Days * 1<OffsetDay>
                Items = items
                Stats = {
                    InitialInterestBalance =
                        match bp.InterestConfig.Method with
                        | Interest.Method.AddOn -> interestTotal
                        | _ -> 0L<Cent>
                    LastScheduledPaymentDay = finalScheduledPaymentDay
                    LevelPayment =
                        scheduledPayments
                        |> Array.countBy ScheduledPayment.total
                        |> fun a ->
                            if Seq.isEmpty a then
                                None
                            else
                                a |> Seq.maxBy snd |> fst |> Some
                        |> Option.defaultValue finalPayment
                    FinalPayment = finalPayment
                    ScheduledPaymentTotal = scheduledPayments |> Array.sumBy ScheduledPayment.total
                    PrincipalTotal = principalTotal
                    InterestTotal = interestTotal
                    InitialApr = Apr.toPercent bp.InterestConfig.AprMethod aprSolution
                    InitialCostToBorrowingRatio =
                        if principalTotal = 0L<Cent> then
                            Percent 0m
                        else
                            decimal (feeTotal + interestTotal) / decimal principalTotal
                            |> Percent.fromDecimal
                            |> Percent.round 2
                }
            }
