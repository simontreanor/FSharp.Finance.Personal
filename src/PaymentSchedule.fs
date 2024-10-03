namespace FSharp.Finance.Personal

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module PaymentSchedule =

    open ArrayExtension
    open Calculation
    open Currency
    open DateDay
    open FeesAndCharges
    open Formatting
    open Percentages
    open UnitPeriod
    open ValueOptionCE

    /// an originally scheduled payment, including the original simple interest and contractual interest calculations
    [<Struct>]
    type OriginalPayment =
        {
            /// the original payment amount
            Amount: int64<Cent>
            /// the original simple interest
            SimpleInterest: int64<Cent>
            /// the contractually calculated interest
            ContractualInterest: decimal<Cent>
        }
                

    /// any original or rescheduled payment, affecting how any payment due is calculated
    [<StructuredFormatDisplay("{Html}")>]
    type ScheduledPayment =
        {
            /// any original payment
            Original: OriginalPayment voption
            /// any rescheduled payment
            /// > NB: if set to `ValueSome 0L<Cent>` this indicates that the original payment is no longer due
            Rescheduled: int64<Cent> voption
            /// any adjustment due to interest or charges being applied to the relevant payment rather than being amortised later
            Adjustment: int64<Cent> voption
            /// any reference numbers or other information pertaining to this payment
            Metadata: Map<string, obj>
        }
        /// the total amount of the payment
        member x.Total =
            match x.Original, x.Rescheduled with 
            | _, ValueSome r ->
                r
            | ValueSome o, ValueNone ->
                o.Amount
            | ValueNone, ValueNone ->
                0L<Cent>
            |> fun t ->
                match x.Adjustment with
                | ValueSome a ->
                    t + a
                | ValueNone ->
                    t
        /// whether the payment has either an original or a rescheduled value
        member x.IsSome =
            x.Original.IsSome || x.Rescheduled.IsSome
        /// a default value with no data
        static member DefaultValue =
            {
                Original = ValueNone
                Rescheduled = ValueNone
                Adjustment = ValueNone
                Metadata = Map.empty
            }
        /// a quick convenient method to create a basic scheduled payment
        static member Quick originalAmount rescheduledAmount =
            { ScheduledPayment.DefaultValue with
                Original =  originalAmount |> ValueOption.map(fun oa -> { Amount = oa; SimpleInterest = 0L<Cent>; ContractualInterest = 0m<Cent> })
                Rescheduled = rescheduledAmount
            }
        /// HTML formatting to display the scheduled payment in a concise way
        member x.Html =
            match x.Original, x.Rescheduled with
            | ValueSome o, ValueSome r ->
                $"""<s>{formatCent o.Amount}</s>&nbsp;{formatCent r}"""
            | ValueSome o, ValueNone ->
                $"original {formatCent o.Amount}"
            | ValueNone, ValueSome r ->
                $"rescheduled {formatCent r}"
            | ValueNone, ValueNone ->
                ""
            |> fun s ->
                match x.Adjustment with
                | ValueSome a when a < 0L<Cent> ->
                    $"{s}&nbsp;-&nbsp;{formatCent <| abs a}"
                | ValueSome a when a > 0L<Cent> ->
                    $"{s}&nbsp;+&nbsp;{formatCent a}"
                | _ ->
                    s

    /// the status of the payment, allowing for delays due to payment-provider processing times
    [<RequireQualifiedAccess; Struct>]
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
        | Failed of Failed: int64<Cent> * Charges: Charge array
        with
            /// the total amount of the payment
            static member Total = function
                | WriteOff ap -> ap
                | Pending ap -> ap
                | TimedOut _ -> 0L<Cent>
                | Confirmed ap -> ap
                | Failed _ -> 0L<Cent>

    /// an actual payment made by the customer, optionally including metadata such as bank references etc.
    type ActualPayment =
        {
            /// the status of the payment
            ActualPaymentStatus: ActualPaymentStatus
            /// any extra info such as references
            Metadata: Map<string, obj>
        }
        with
            /// the total amount of the payment
            static member Total =
                function { ActualPaymentStatus = aps } -> ActualPaymentStatus.Total aps
            /// a quick convenient method to create a confirmed actual payment
            static member QuickConfirmed amount =
                {
                    ActualPaymentStatus = ActualPaymentStatus.Confirmed amount
                    Metadata = Map.empty
                }
            /// a quick convenient method to create a pending actual payment
            static member QuickPending amount =
                {
                    ActualPaymentStatus = ActualPaymentStatus.Pending amount
                    Metadata = Map.empty
                }
            /// a quick convenient method to create a failed actual payment along with any applicable penalty charges
            static member QuickFailed amount charges =
                {
                    ActualPaymentStatus = ActualPaymentStatus.Failed (amount, charges)
                    Metadata = Map.empty
                }
            /// a quick convenient method to create a written off actual payment
            static member QuickWriteOff amount =
                {
                    ActualPaymentStatus = ActualPaymentStatus.WriteOff amount
                    Metadata = Map.empty
                }

    /// the status of a payment made by the customer
    [<Struct>]
    type CustomerPaymentStatus =
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

    /// a regular schedule based on a unit-period config with a specific number of payments with an auto-calculated amount
    [<RequireQualifiedAccess; Struct>]
    type AutoGenerateSchedule = {
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        MaxDuration: Duration voption
    }

    /// the type of the scheduled; for scheduled payments, this affects how any payment due is calculated
    [<RequireQualifiedAccess; Struct>]
    type ScheduleType =
        /// an original schedule
        | Original
        /// a schedule based on a previous one
        | Rescheduled

    /// a regular schedule based on a unit-period config with a specific number of payments of a specified amount
    [<RequireQualifiedAccess; Struct>]
    type FixedSchedule = {
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        PaymentAmount: int64<Cent>
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

    /// a scheduled payment item, with running calculations of interest and principal balance
    type Item = {
        /// the day expressed as an offset from the start date
        Day: int<OffsetDay>
        /// the scheduled payment
        ScheduledPayment: ScheduledPayment voption
        /// the simple interest accrued since the previous payment
        SimpleInterest: int64<Cent>
        /// the interest portion paid off by the payment
        InterestPortion: int64<Cent>
        /// the principal portion paid off by the payment
        PrincipalPortion: int64<Cent>
        /// the interest balance carried forward
        InterestBalance: int64<Cent>
        /// the principal balance carried forward
        PrincipalBalance: int64<Cent>
        /// the total simple interest accrued from the start date to the current date
        TotalSimpleInterest: int64<Cent>
        /// the total interest payable from the start date to the current date
        TotalInterest: int64<Cent>
        /// the total principal payable from the start date to the current date
        TotalPrincipal: int64<Cent>
    }
        with
            /// a default value with no data
            static member DefaultValue =
                { 
                    Day = 0<OffsetDay>
                    ScheduledPayment = ValueNone
                    SimpleInterest = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    PrincipalPortion = 0L<Cent>
                    InterestBalance = 0L<Cent>
                    PrincipalBalance = 0L<Cent>
                    TotalSimpleInterest = 0L<Cent>
                    TotalInterest = 0L<Cent>
                    TotalPrincipal = 0L<Cent>
                }

    ///  a schedule of payments, with final statistics based on the payments being made on time and in full
    type Schedule = {
        /// the day, expressed as an offset from the start date, on which the schedule is inspected
        AsOfDay: int<OffsetDay>
        /// the items of the schedule
        Items: Item array
        /// the initial interest balance when using the add-on interest method
        InitialInterestBalance: int64<Cent>
        /// the final day of the schedule, expressed as an offset from the start date
        FinalPaymentDay: int<OffsetDay>
        /// the amount of all the payments except the final one
        LevelPayment: int64<Cent>
        /// the amount of the final payment
        FinalPayment: int64<Cent>
        /// the total of all payments
        PaymentTotal: int64<Cent>
        /// the total principal paid, which should equal the initial advance (principal)
        PrincipalTotal: int64<Cent>
        /// the total interest accrued
        InterestTotal: int64<Cent>
        /// the APR according to the calculation method specified in the schedule parameters and based on the schedule being settled as agreed
        Apr: Solution * Percent voption
        /// the cost of borrowing, expressed as a ratio of interest to principal
        CostToBorrowingRatio: Percent
    }

    /// how to handle cases where the payment due is less than the minimum that payment providers can process
     [<Struct>]
    type MinimumPayment =
        /// no minimum payment
        | NoMinimumPayment
        /// add the payment due to the next payment or close the balance if the final payment
        | DeferOrWriteOff of DeferOrWriteOff: int64<Cent>
        /// take the minimum payment regardless
        | ApplyMinimumPayment of ApplyMinimumPayment: int64<Cent>

   /// technical calculation options
    [<Struct>]
    type Calculation = {
        /// which APR calculation method to use
        AprMethod: Apr.CalculationMethod
        /// which rounding method to use
        RoundingOptions: RoundingOptions
        /// the minimum payment that can be taken and how to handle it
        MinimumPayment: MinimumPayment
        /// the duration after which a pending payment is considered a missed payment
        PaymentTimeout: int<DurationDay>
    }

    /// whether to stick to scheduled payment amounts or add charges and interest to them
    [<Struct>]
    type ScheduledPaymentOption =
        /// keep to the scheduled payment amounts even if this results in an open balance
        | AsScheduled
        /// add any charges and interest to the payment in order to close the balance at the end of the schedule
        | AddChargesAndInterest

    /// how to handle a final balance if not closed: leave it open or modify/add payments at the end of the schedule
    [<Struct>]
    type CloseBalanceOption =
        /// do not modify the final payment and leave any open balance as is
        | LeaveOpenBalance
        /// increase the final payment to close any open balance
        | IncreaseFinalPayment
        /// add a single payment to the schedule to close any open balance immediately (interval based on unit-period config)
        | AddSingleExtraPayment
        /// add multiple payments to the schedule to close any open balance gradually (interval based on unit-period config)
        | AddMultipleExtraPayments

    /// how to treat scheduled payments
    type PaymentOptions = {
        /// whether to modify scheduled payment amounts to keep the schedule on-track
        ScheduledPaymentOption: ScheduledPaymentOption
        /// whether to leave a final balance open or close it using various methods
        CloseBalanceOption: CloseBalanceOption
    }

    /// parameters for creating a payment schedule
    type Parameters = {
        /// the date on which the schedule is inspected, typically today, but can be used to inspect it at any point (affects e.g. whether schedule payments are deemed as not yet due)
        AsOfDate: Date
        /// the start date of the schedule, typically the day on which the principal is advanced
        StartDate: Date
        /// the principal
        Principal: int64<Cent>
        /// the scheduled payments or the parameters for generating them
        ScheduleConfig: ScheduleConfig
        /// options relating to scheduled payments
        PaymentOptions: PaymentOptions
        /// options relating to fees
        FeesAndCharges: FeesAndCharges
        /// options relating to interest
        Interest: Interest.Options
        /// technical calculation options
        Calculation: Calculation
    }

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
            |> Array.map(fun rfs ->
                if rfs.PaymentCount = 0 then
                    [||]
                else
                    let unitPeriodConfigStartDate = Config.startDate rfs.UnitPeriodConfig
                    if startDate > unitPeriodConfigStartDate then
                        [||]
                    else
                        generatePaymentSchedule rfs.PaymentCount ValueNone Direction.Forward rfs.UnitPeriodConfig |> Array.map (OffsetDay.fromDate startDate)
                        |> Array.map(fun d ->
                            let originalAmount, rescheduledAmount =
                                match rfs.ScheduleType with
                                | ScheduleType.Original -> ValueSome rfs.PaymentAmount, ValueNone
                                | ScheduleType.Rescheduled -> ValueNone, ValueSome rfs.PaymentAmount
                            d, ScheduledPayment.Quick originalAmount rescheduledAmount
                        )
            )
            |> Array.concat
            |> Array.sortBy fst
            |> Array.groupBy fst
            |> Array.map(fun (d, spp) ->
                let original = spp |> Array.map (snd >> _.Original) |> Array.tryFind _.IsSome |> toValueOption |> ValueOption.flatten
                let rescheduled = spp |> Array.map (snd >> _.Rescheduled) |> Array.filter _.IsSome |> Array.tryLast |> toValueOption |> ValueOption.flatten
                d, { ScheduledPayment.DefaultValue with Original = original; Rescheduled = rescheduled }
            )
            |> Map.ofArray
        | AutoGenerateSchedule rs ->
            if rs.PaymentCount = 0 then
                Map.empty
            else
                let unitPeriodConfigStartDate = Config.startDate rs.UnitPeriodConfig
                if startDate > unitPeriodConfigStartDate then
                    Map.empty
                else
                    generatePaymentSchedule rs.PaymentCount rs.MaxDuration Direction.Forward rs.UnitPeriodConfig
                    |> Array.map(fun d -> OffsetDay.fromDate startDate d, ScheduledPayment.Quick (ValueSome 0L<Cent>) ValueNone)
                    |> Map.ofArray

    /// calculates the number of days between two offset days on which interest is chargeable
    let calculate toleranceOption sp =
        let paymentMap = generatePaymentMap sp.StartDate sp.ScheduleConfig

        let paymentDays = paymentMap |> Map.keys |> Seq.toArray

        let finalPaymentDay = paymentDays |> Array.tryLast |> Option.defaultValue 0<OffsetDay>

        let paymentCount = paymentDays |> Array.length

        let fees = Fees.total sp.Principal sp.FeesAndCharges.Fees |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.FeesRounding)

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let totalInterestCap = sp.Interest.Cap.Total |> Interest.Cap.total sp.Principal |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)

        let totalAddOnInterest =
            match sp.Interest.Method with
            | Interest.Method.AddOn ->
                (sp.Principal |> Cent.toDecimalCent) * dailyInterestRate * decimal finalPaymentDay
                |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
                |> Cent.min totalInterestCap
            | _ -> 0L<Cent>

        let calculateLevelPayment interest = if paymentCount = 0 then 0m else (decimal sp.Principal + decimal fees + interest) / decimal paymentCount

        let initialItem = { Item.DefaultValue with InterestBalance = totalAddOnInterest; PrincipalBalance = sp.Principal + fees }

        let mutable schedule = [||]

        let toleranceSteps = ValueSome <| ToleranceSteps.forPaymentAmount paymentCount

        let calculateInterest interestMethod payment previousItem day =
            match interestMethod with
            | Interest.Method.Simple ->
                let dailyRates = Interest.dailyRates sp.StartDate false sp.Interest.StandardRate sp.Interest.PromotionalRates previousItem.Day day
                let simpleInterest = Interest.calculate previousItem.PrincipalBalance sp.Interest.Cap.Daily (ValueSome sp.Calculation.RoundingOptions.InterestRounding) dailyRates |> decimal |> Cent.round (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
                if previousItem.TotalSimpleInterest + simpleInterest >= totalInterestCap then totalInterestCap - previousItem.TotalInterest else simpleInterest
            | Interest.Method.AddOn ->
                if payment <= previousItem.InterestBalance then
                    payment
                else
                    previousItem.InterestBalance

        let generateItem interestMethod (scheduledPayment: ScheduledPayment) previousItem day =
            let scheduledPaymentTotal = scheduledPayment.Total
            let simpleInterest = calculateInterest Interest.Method.Simple scheduledPaymentTotal previousItem day
            let interestPortion = calculateInterest interestMethod scheduledPaymentTotal previousItem day
            let principalPortion = scheduledPaymentTotal - interestPortion
            {
                Day = day
                ScheduledPayment = ValueSome scheduledPayment
                SimpleInterest = simpleInterest
                InterestPortion = interestPortion
                PrincipalPortion = principalPortion
                InterestBalance = match interestMethod with Interest.Method.AddOn -> previousItem.InterestBalance - interestPortion | _ -> 0L<Cent>
                PrincipalBalance = previousItem.PrincipalBalance - principalPortion
                TotalSimpleInterest = previousItem.TotalSimpleInterest + simpleInterest
                TotalInterest = previousItem.TotalInterest + interestPortion
                TotalPrincipal = previousItem.TotalPrincipal + principalPortion
            }

        let generatePaymentAmount firstItem interestMethod roughPayment =
            let scheduledPayment =
                roughPayment
                |> Cent.round (ValueSome sp.Calculation.RoundingOptions.PaymentRounding)
                |> fun rp -> ScheduledPayment.Quick (ValueSome rp) ValueNone
            schedule <-
                paymentDays
                |> Array.scan (generateItem interestMethod scheduledPayment) firstItem
            let principalBalance = schedule |> Array.last |> _.PrincipalBalance |> decimal
            principalBalance

        // for the add-on interest method: take the final interest total from the schedule and use it as the initial interest balance and calculate a new schedule,
        // repeating until the two figures equalise, which yields the maximum interest that can be accrued with this interest method
        let maximiseInterest firstItem (iteration, initialInterestBalance) =
            if Array.isEmpty paymentDays && initialInterestBalance = 0m && firstItem.Day = 0<OffsetDay> then
                None
            else
                let regularScheduledPayment = initialInterestBalance |> calculateLevelPayment |> ( * ) 1m<Cent> |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.PaymentRounding)
                schedule <-
                    paymentDays
                    |> Array.scan (fun state pd ->
                        let scheduledPayment =
                            match sp.ScheduleConfig with
                            | AutoGenerateSchedule _ -> ScheduledPayment.Quick (ValueSome regularScheduledPayment) ValueNone
                            | FixedSchedules _
                            | CustomSchedule _ -> paymentMap[pd]
                        generateItem Interest.Method.AddOn scheduledPayment state pd
                    ) { firstItem with InterestBalance = int64 initialInterestBalance * 1L<Cent> }

                // schedule
                // |> generateHtmlFromArray [||]
                // |> outputToFile' $"""out/GenerateMaximumInterest_{System.DateTime.UtcNow.ToString("yyyyMMdd_HHmm")}.md""" true

                let finalInterestTotal = schedule |> Array.last |> _.TotalSimpleInterest |> min totalInterestCap |> decimal
                let diff = initialInterestBalance - finalInterestTotal |> roundTo (ValueSome sp.Calculation.RoundingOptions.InterestRounding) 0
                if iteration = 100 || (diff <= 0m && diff > -(decimal paymentCount)) then
                    None
                else
                    Some (initialInterestBalance, (iteration + 1, finalInterestTotal))

        let solution =
            match sp.ScheduleConfig with
            | AutoGenerateSchedule _ ->
                Array.solve (generatePaymentAmount initialItem sp.Interest.Method) 100 (totalAddOnInterest |> decimal |> calculateLevelPayment) toleranceOption toleranceSteps
            | FixedSchedules _
            | CustomSchedule _ ->
                schedule <-
                    paymentDays
                    |> Array.scan (fun state pd -> generateItem sp.Interest.Method paymentMap[pd] state pd) initialItem
                Solution.Bypassed

        match solution with
        | Solution.Found _
        | Solution.Bypassed ->

            match sp.Interest.Method with
            | Interest.Method.AddOn ->
                let finalInterestTotal = schedule |> Array.last |> _.TotalSimpleInterest |> decimal
                Array.unfold (maximiseInterest initialItem) (0, finalInterestTotal) |> ignore
            | _ ->
                ()

            // handle any principal balance overpayment (due to rounding) on the final payment of a schedule
            let items =
                schedule
                |> Array.map(fun si ->
                    if si.Day = finalPaymentDay && solution <> Solution.Bypassed then
                        let adjustedPayment =
                            si.ScheduledPayment
                            |> ValueOption.map(fun p ->
                                { p with
                                    Original = if p.Rescheduled.IsNone then p.Original |> ValueOption.map(fun o -> { o with Amount = o.Amount + si.PrincipalBalance }) else p.Original
                                    Rescheduled = if p.Rescheduled.IsSome then p.Rescheduled |> ValueOption.map(fun r -> r + si.PrincipalBalance) else p.Rescheduled
                                }
                            )
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

            let principalTotal = items |> Array.sumBy _.PrincipalPortion
            let interestTotal = items |> Array.sumBy _.InterestPortion
            let aprSolution =
                items
                |> Array.filter _.ScheduledPayment.IsSome
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.TransferDate = sp.StartDate.AddDays(int si.Day); Apr.Amount = si.ScheduledPayment.Value.Total })
                |> Apr.calculate sp.Calculation.AprMethod sp.Principal sp.StartDate
            let finalPayment =
                items
                |> Array.filter _.ScheduledPayment.IsSome
                |> Array.tryLast
                |> Option.map _.ScheduledPayment.Value.Total
                |> Option.defaultValue 0L<Cent>
            ValueSome {
                AsOfDay = (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
                Items = items
                InitialInterestBalance = match sp.Interest.Method with Interest.Method.AddOn -> interestTotal | _ -> 0L<Cent>
                FinalPaymentDay = finalPaymentDay
                LevelPayment = items |> Array.filter _.ScheduledPayment.IsSome |> Array.countBy _.ScheduledPayment.Value.Total |> fun a -> (if Seq.isEmpty a then None else a |> Seq.maxBy snd |> fst |> Some) |> Option.defaultValue finalPayment
                FinalPayment = finalPayment
                PaymentTotal = items |> Array.filter _.ScheduledPayment.IsSome |> Array.sumBy _.ScheduledPayment.Value.Total
                PrincipalTotal = principalTotal
                InterestTotal = interestTotal
                Apr = aprSolution, Apr.toPercent sp.Calculation.AprMethod aprSolution
                CostToBorrowingRatio =
                    if principalTotal = 0L<Cent> then Percent 0m else
                    decimal (fees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 2
            }
        | _ ->
            ValueNone
