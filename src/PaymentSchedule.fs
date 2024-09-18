namespace FSharp.Finance.Personal

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module PaymentSchedule =

    open ArrayExtension
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open Percentages
    open UnitPeriod

    /// a scheduled payment item, with running calculations of interest and principal balance
    type Item = {
        /// the day expressed as an offset from the start date
        Day: int<OffsetDay>
        /// the scheduled payment amount
        Payment: int64<Cent> voption
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

    /// the type of the scheduled; for scheduled payments, this affects how any payment due is calculated
    [<RequireQualifiedAccess; Struct>]
    type ScheduleType =
        /// an original schedule
        | Original
        /// a schedule based on a previous one
        | Rescheduled

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
        PaymentSchedule: CustomerPaymentSchedule
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
        | IrregularSchedule payments ->
            if Array.isEmpty payments then
                [||]
            else
                payments |> Array.map(fun p -> p.PaymentDay, CustomerPaymentDetails.total p.PaymentDetails)
        | RegularFixedSchedule regularFixedSchedules ->
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
                        |> Array.map(fun d -> d, rfs.PaymentAmount)
            )
            |> Array.concat
        | RegularSchedule (unitPeriodConfig, paymentCount, maxDuration) ->
            if paymentCount = 0 then
                [||]
            else
                let unitPeriodConfigStartDate = Config.startDate unitPeriodConfig
                if startDate > unitPeriodConfigStartDate then
                    [||]
                else
                    generatePaymentSchedule paymentCount maxDuration Direction.Forward unitPeriodConfig
                    |> Array.map(fun d -> OffsetDay.fromDate startDate d, 0L<Cent>)
        |> Map.ofArray

    /// calculates the number of days between two offset days on which interest is chargeable
    let calculate toleranceOption sp =
        let paymentMap = generatePaymentMap sp.StartDate sp.PaymentSchedule

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

        let initialItem = { Day = 0<OffsetDay>; Payment = ValueNone; SimpleInterest = 0L<Cent>; InterestPortion = 0L<Cent>; PrincipalPortion = 0L<Cent>; InterestBalance = totalAddOnInterest; PrincipalBalance = sp.Principal + fees; TotalSimpleInterest = 0L<Cent>; TotalInterest = 0L<Cent>; TotalPrincipal = 0L<Cent> }

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

        let generateItem interestMethod payment previousItem day =
            let simpleInterest = calculateInterest Interest.Method.Simple payment previousItem day
            let interestPortion = calculateInterest interestMethod payment previousItem day
            let principalPortion = payment - interestPortion
            {
                Day = day
                Payment = ValueSome payment
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
            let payment = roughPayment |> Cent.round (ValueSome sp.Calculation.RoundingOptions.PaymentRounding)
            schedule <-
                paymentDays
                |> Array.scan (generateItem interestMethod payment) firstItem
            let principalBalance = schedule |> Array.last |> _.PrincipalBalance |> decimal
            principalBalance

        // for the add-on interest method: take the final interest total from the schedule and use it as the initial interest balance and calculate a new schedule,
        // repeating until the two figures equalise, which yields the maximum interest that can be accrued with this interest method
        let maximiseInterest firstItem (iteration, initialInterestBalance) =
            if Array.isEmpty paymentDays && initialInterestBalance = 0m && firstItem.Day = 0<OffsetDay> then
                None
            else
                let regularSchedulePayment = initialInterestBalance |> calculateLevelPayment |> ( * ) 1m<Cent> |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.PaymentRounding)
                schedule <-
                    paymentDays
                    |> Array.scan (fun state pd ->
                        let payment =
                            match sp.PaymentSchedule with
                            | RegularSchedule _ -> regularSchedulePayment
                            | RegularFixedSchedule _
                            | IrregularSchedule _ -> paymentMap[pd]
                        generateItem Interest.Method.AddOn payment state pd
                    ) { firstItem with InterestBalance = int64 initialInterestBalance * 1L<Cent> }

                // schedule
                // |> Formatting.generateHtmlFromArray [||]
                // |> Formatting.outputToFile' $"""out/GenerateMaximumInterest_{System.DateTime.UtcNow.ToString("yyyyMMdd_HHmm")}.md""" true

                let finalInterestTotal = schedule |> Array.last |> _.TotalSimpleInterest |> min totalInterestCap |> decimal
                let diff = initialInterestBalance - finalInterestTotal |> roundTo (ValueSome sp.Calculation.RoundingOptions.InterestRounding) 0
                if iteration = 100 || (diff <= 0m && diff > -(decimal paymentCount)) then
                    None
                else
                    Some (initialInterestBalance, (iteration + 1, finalInterestTotal))

        let solution =
            match sp.PaymentSchedule with
            | RegularSchedule _ ->
                Array.solve (generatePaymentAmount initialItem sp.Interest.Method) 100 (totalAddOnInterest |> decimal |> calculateLevelPayment) toleranceOption toleranceSteps
            | RegularFixedSchedule _
            | IrregularSchedule _ ->
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
                        let adjustedPayment = si.Payment |> ValueOption.map(fun p -> p + si.PrincipalBalance)
                        let adjustedPrincipal = si.PrincipalPortion + si.PrincipalBalance
                        let adjustedTotalPrincipal = si.TotalPrincipal + si.PrincipalBalance
                        { si with Payment = adjustedPayment; PrincipalPortion = adjustedPrincipal; PrincipalBalance = 0L<Cent>; TotalPrincipal = adjustedTotalPrincipal }
                    else
                        si
                )

            let principalTotal = items |> Array.sumBy _.PrincipalPortion
            let interestTotal = items |> Array.sumBy _.InterestPortion
            let aprSolution =
                items
                |> Array.filter(fun si -> si.Payment.IsSome)
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.TransferDate = sp.StartDate.AddDays(int si.Day); Apr.Amount = si.Payment.Value })
                |> Apr.calculate sp.Calculation.AprMethod sp.Principal sp.StartDate
            let finalPayment = items |> Array.filter _.Payment.IsSome |> Array.tryLast |> Option.map _.Payment.Value |> Option.defaultValue 0L<Cent>
            ValueSome {
                AsOfDay = (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
                Items = items
                InitialInterestBalance = match sp.Interest.Method with Interest.Method.AddOn -> interestTotal | _ -> 0L<Cent>
                FinalPaymentDay = finalPaymentDay
                LevelPayment = items |> Array.filter _.Payment.IsSome |> Array.countBy _.Payment.Value |> Array.vTryMaxBy snd fst |> ValueOption.defaultValue finalPayment
                FinalPayment = finalPayment
                PaymentTotal = items |> Array.filter _.Payment.IsSome |> Array.sumBy _.Payment.Value
                PrincipalTotal = principalTotal
                InterestTotal = interestTotal
                Apr = aprSolution, Apr.toPercent sp.Calculation.AprMethod aprSolution
                CostToBorrowingRatio =
                    if principalTotal = 0L<Cent> then Percent 0m else
                    decimal (fees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 2
            }
        | _ ->
            ValueNone
