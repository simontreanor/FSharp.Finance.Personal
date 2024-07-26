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
    [<Struct>]
    type Item = {
        /// the day expressed as an offset from the start date
        Day: int<OffsetDay>
        /// the scheduled payment amount
        Payment: int64<Cent> voption
        /// the interest accrued since the previous payment
        Interest: int64<Cent>
        /// the total interest accrued from the start date to the current date
        AggregateInterest: int64<Cent>
        /// the principal portion paid off by the payment
        Principal: int64<Cent>
        /// the interest balance carried forward
        InterestBalance: int64<Cent>
        /// the principal balance carried forward
        PrincipalBalance: int64<Cent>
    }

    ///  a schedule of payments, with final statistics based on the payments being made on time and in full
    [<Struct>]
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

    /// generates an array of offset days based on a start date and payment schedule
    let paymentDays startDate paymentSchedule =
        match paymentSchedule with
        | IrregularSchedule payments ->
            if Array.isEmpty payments then
                [||]
            else
                payments |> Array.map _.PaymentDay
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
                    generatePaymentSchedule paymentCount maxDuration Direction.Forward unitPeriodConfig |> Array.map (OffsetDay.fromDate startDate)

    /// calculates the number of days between two offset days on which interest is chargeable
    let calculate toleranceOption sp =
        let paymentDays = paymentDays sp.StartDate sp.PaymentSchedule

        let finalPaymentDay = paymentDays |> Array.tryLast |> Option.defaultValue 0<OffsetDay>

        let paymentCount = paymentDays |> Array.length

        let fees = Fees.total sp.Principal sp.FeesAndCharges.Fees |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.FeesRounding)

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let dailyInterestCap principal = sp.Interest.Cap.Daily |> Interest.Cap.total principal |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)

        let totalInterestCap = sp.Interest.Cap.Total |> Interest.Cap.total sp.Principal |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)

        let totalAddOnInterest =
            (sp.Principal |> Cent.toDecimalCent) * dailyInterestRate * decimal finalPaymentDay
            |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
            |> Cent.min totalInterestCap

        let roughPayment = if paymentCount = 0 then 0m else (decimal sp.Principal + decimal fees) / decimal paymentCount

        let initial = { Day = 0<OffsetDay>; Payment = ValueNone; Interest = 0L<Cent>; AggregateInterest = 0L<Cent>; Principal = 0L<Cent>; InterestBalance = 0L<Cent>; PrincipalBalance = sp.Principal + fees }

        let toleranceSteps = ValueSome { ToleranceSteps.Min = 0; ToleranceSteps.Step = paymentCount; ToleranceSteps.Max = paymentCount * 4 }

        let mutable schedule = [||]

        let generatorSimple payment =
            schedule <-
                paymentDays
                |> Array.scan(fun si d ->
                    let dailyRates = Interest.dailyRates sp.StartDate false sp.Interest.StandardRate sp.Interest.PromotionalRates si.Day d
                    let interest = Interest.calculate si.PrincipalBalance sp.Interest.Cap.Daily (ValueSome sp.Calculation.RoundingOptions.InterestRounding) dailyRates |> decimal |> Cent.round (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
                    let interest' = if si.AggregateInterest + interest >= totalInterestCap then totalInterestCap - si.AggregateInterest else interest
                    let payment' = Cent.round (ValueSome sp.Calculation.RoundingOptions.PaymentRounding) payment
                    let principalPortion = payment' - interest'
                    {
                        Day = d
                        Payment = ValueSome payment'
                        Interest = interest'
                        AggregateInterest = si.AggregateInterest + interest'
                        Principal = principalPortion
                        InterestBalance = 0L<Cent>
                        PrincipalBalance = si.PrincipalBalance - principalPortion
                    }
                ) initial
            let principalBalance = schedule |> Array.last |> _.PrincipalBalance |> decimal
            principalBalance

        let generatorAddOn payment =
            schedule <-
                paymentDays
                |> Array.scan(fun si d ->
                    let payment' = Cent.round (ValueSome sp.Calculation.RoundingOptions.PaymentRounding) payment
                    let interest =
                        si.InterestBalance
                        |> Cent.min payment'
                        |> fun i -> if si.AggregateInterest + i >= totalInterestCap then totalInterestCap - si.AggregateInterest else i
                        |> Cent.min (
                            (dailyInterestCap si.PrincipalBalance |> Cent.toDecimalCent) * decimal (d - si.Day)
                            |> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
                        )
                    let principalPortion = Cent.max 0L<Cent> (payment' - interest)
                    {
                        Day = d
                        Payment = ValueSome payment'
                        Interest = interest
                        AggregateInterest = si.AggregateInterest + interest
                        Principal = principalPortion
                        InterestBalance = si.InterestBalance - interest
                        PrincipalBalance = si.PrincipalBalance - principalPortion
                    }
                ) { initial with InterestBalance = totalAddOnInterest }
            let principalBalance = schedule |> Array.last |> _.PrincipalBalance |> decimal
            principalBalance

        let generator payment =
            match sp.Interest.Method with
            | Interest.InterestMethod.Simple -> generatorSimple payment
            | Interest.InterestMethod.Compound -> failwith "Compound interest calculation not yet implemented"
            | Interest.InterestMethod.AddOn -> generatorAddOn payment

        match Array.solve generator 100 roughPayment toleranceOption toleranceSteps with
        | Solution.Found _ ->
            // handle any principal balance overpayment (due to rounding) on the final payment of a schedule
            let items =
                schedule
                |> Array.map(fun si ->
                    if si.Day = finalPaymentDay then
                        { si with Payment = si.Payment |> ValueOption.map(fun p -> p + si.PrincipalBalance); Principal = si.Principal + si.PrincipalBalance; PrincipalBalance = 0L<Cent> }
                    else si
                )
            // handle any final interest balance due to capping by reducing all interest balances
            let finalInterestBalance = items |> Array.last |> _.InterestBalance
            let items' =
                if finalInterestBalance > 0L<Cent> then
                    items |> Array.map(fun si -> { si with InterestBalance = si.InterestBalance - finalInterestBalance })
                else
                    items

            let principalTotal = items' |> Array.sumBy _.Principal
            let interestTotal = items' |> Array.sumBy _.Interest
            let aprSolution =
                items'
                |> Array.filter(fun si -> si.Payment.IsSome)
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.TransferDate = sp.StartDate.AddDays(int si.Day); Apr.Amount = si.Payment.Value })
                |> Apr.calculate sp.Calculation.AprMethod sp.Principal sp.StartDate
            let finalPayment = items' |> Array.filter _.Payment.IsSome |> Array.last |> _.Payment.Value
            ValueSome {
                AsOfDay = (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
                Items = items'
                InitialInterestBalance = match sp.Interest.Method with Interest.InterestMethod.AddOn -> interestTotal | _ -> 0L<Cent>
                FinalPaymentDay = finalPaymentDay
                LevelPayment = items' |> Array.filter _.Payment.IsSome |> Array.countBy _.Payment.Value |> Array.vTryMaxBy snd fst |> ValueOption.defaultValue finalPayment
                FinalPayment = finalPayment
                PaymentTotal = items' |> Array.filter _.Payment.IsSome |> Array.sumBy _.Payment.Value
                PrincipalTotal = principalTotal
                InterestTotal = interestTotal
                Apr = aprSolution, Apr.toPercent sp.Calculation.AprMethod aprSolution
                CostToBorrowingRatio =
                    if principalTotal = 0L<Cent> then Percent 0m else
                    decimal (fees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 2
            }
        | _ ->
            ValueNone
