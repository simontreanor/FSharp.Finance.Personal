namespace FSharp.Finance.Personal

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module PaymentSchedule =

    open CustomerPayments

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
        /// the principal balance carried forward
        Balance: int64<Cent>
    }

    ///  a schedule of payments, with final statistics based on the payments being made on time and in full
    [<Struct>]
    type Schedule = {
        /// the day, expressed as an offset from the start date, on which the schedule is inspected
        AsOfDay: int<OffsetDay>
        /// the items of the schedule
        Items: Item array
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

    /// how to handle the principal balance overpayment (due to rounding) on the final payment of a schedule
    [<Struct>]
    type FinalPaymentAdjustment =
        /// adjust the final payment to account for the difference
        | AdjustFinalPayment
        /// spread the difference over the level payments (not yet implemented)
        | SpreadOverLevelPayments

    /// technical calculation options
    [<Struct>]
    type Calculation = {
        /// which APR calculation method to use
        AprMethod: Apr.CalculationMethod
        /// which rounding method to use
        RoundingOptions: RoundingOptions
        /// how to adjust the final payment
        FinalPaymentAdjustment: FinalPaymentAdjustment
        /// the minimum payment that can be taken, usually due to payment-provider restrictions
        MinimumPaymentAmount: int64<Cent>
    }

    module Calculation =
        /// the recommended default calculation options
        let recommended = {
            AprMethod = Apr.CalculationMethod.UsActuarial 8
            RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
            FinalPaymentAdjustment = AdjustFinalPayment
            MinimumPaymentAmount = 50L<Cent>
        }

    /// parameters for creating a payment schedule
    [<Struct>]
    type Parameters = {
        /// the date on which the schedule is inspected, typically today, but can be used to inspect it at any point (affects e.g. whether schedule payments are deemed as not yet due)
        AsOfDate: Date
        /// the start date of the schedule, typically the day on which the principal is advanced
        StartDate: Date
        /// the principal
        Principal: int64<Cent>
        /// the unit-period config, specifying the payment frequency, first payment date and optionally whether the payments track a particular day of the month
        UnitPeriodConfig: UnitPeriod.Config
        /// the number of payments
        PaymentCount: int
        /// options relating to fees
        FeesAndCharges: FeesAndCharges
        /// options relating to interest
        Interest: Interest.Options
        /// technical calculation options
        Calculation: Calculation
    }

    /// calculates the number of days between two offset days on which interest is chargeable
    let calculate toleranceOption sp =
        if sp.PaymentCount = 0 then ValueNone else
        if sp.StartDate > UnitPeriod.Config.startDate sp.UnitPeriodConfig then ValueNone else
        let paymentDates = UnitPeriod.generatePaymentSchedule sp.PaymentCount UnitPeriod.Direction.Forward sp.UnitPeriodConfig
        let finalPaymentDate = paymentDates |> Array.max
        let finalPaymentDay = (finalPaymentDate - sp.StartDate).Days * 1<OffsetDay>
        let paymentCount = paymentDates |> Array.length
        let fees = Fees.total sp.Principal sp.FeesAndCharges.Fees
        let dailyInterestRate = sp.Interest.Rate |> Interest.Rate.daily
        let totalInterestCap = sp.Interest.Cap.Total |> Interest.Cap.total sp.Principal sp.Calculation.RoundingOptions.InterestRounding
        let roughPayment = (decimal sp.Principal + decimal fees) / decimal paymentCount
        let initial = { Day = 0<OffsetDay>; Payment = ValueNone; Interest = 0L<Cent>; AggregateInterest = 0L<Cent>; Principal = 0L<Cent>; Balance = sp.Principal + fees }
        let toleranceSteps = ValueSome { ToleranceSteps.Min = 0; ToleranceSteps.Step = paymentCount; ToleranceSteps.Max = paymentCount * 2 }
        let paymentDays = paymentDates |> Array.map(fun d -> (d - sp.StartDate).Days * 1<OffsetDay>)
        let mutable schedule = [||]
        let generator payment =
            schedule <-
                paymentDays
                |> Array.scan(fun si d ->
                    let interestChargeableDays = Interest.chargeableDays sp.StartDate ValueNone sp.Interest.GracePeriod sp.Interest.Holidays si.Day d
                    let dailyInterestCap = sp.Interest.Cap.Daily |> Interest.Cap.daily si.Balance interestChargeableDays sp.Calculation.RoundingOptions.InterestRounding
                    let interest = Interest.calculate dailyInterestCap si.Balance dailyInterestRate interestChargeableDays sp.Calculation.RoundingOptions.InterestRounding
                    let interest' = if si.AggregateInterest + interest >= totalInterestCap then totalInterestCap - si.AggregateInterest else interest
                    let payment' = Cent.round sp.Calculation.RoundingOptions.PaymentRounding payment
                    let principalPortion = payment' - interest'
                    {
                        Day = d
                        Payment = ValueSome payment'
                        Interest = interest'
                        AggregateInterest = si.AggregateInterest + interest'
                        Principal = principalPortion
                        Balance = si.Balance - principalPortion
                    }
                ) initial
            let principalBalance = schedule |> Array.last |> _.Balance |> decimal
            principalBalance
        match Array.solve generator 100 roughPayment toleranceOption toleranceSteps with
        | Solution.Found _ -> // note: payment is discarded because it is in the schedule
            let items =
                match sp.Calculation.FinalPaymentAdjustment with
                | AdjustFinalPayment ->
                    schedule
                    |> Array.map(fun si ->
                        if si.Day = finalPaymentDay then
                            { si with Payment = si.Payment |> ValueOption.map(fun p -> p + si.Balance); Principal = si.Principal + si.Balance; Balance = 0L<Cent> }
                        else si
                    )
                | SpreadOverLevelPayments ->
                    failwith "Not yet implemented" // to-do: this is tricky because adjusting payments pays off principal and affects interest calculations during the loan
            let principalTotal = items |> Array.sumBy _.Principal
            let interestTotal = items |> Array.sumBy _.Interest
            let aprSolution =
                items
                |> Array.filter(fun si -> si.Payment.IsSome)
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.TransferDate = sp.StartDate.AddDays(int si.Day); Apr.Amount = si.Payment.Value })
                |> Apr.calculate sp.Calculation.AprMethod sp.Principal sp.StartDate
            let finalPayment = items |> Array.filter _.Payment.IsSome |> Array.last |> _.Payment.Value
            ValueSome {
                AsOfDay = (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
                Items = items
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

    /// creates an array of actual payments made on time and in full according to an array of scheduled payments
    let internal allPaidOnTime (scheduleItems: Item array) =
        scheduleItems
        |> Array.filter(fun si -> si.Payment.IsSome)
        |> Array.map(fun si -> {
            PaymentDay = si.Day
            PaymentDetails = ActualPayment (ActualPayment.Confirmed si.Payment.Value)
            }
        )
