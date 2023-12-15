namespace FSharp.Finance.Personal

open System

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module ScheduledPayment =

    /// a scheduled payment item, with running calculations of interest and principal balance
    [<Struct>]
    type ScheduleItem = {
        /// the day expressed as an offset from the start date
        Day: int<OffsetDay>
        /// the principal
        Advance: int64<Cent>
        /// the scheduled payment amount
        Payment: int64<Cent>
        /// the interest accrued since the previous payment
        Interest: int64<Cent>
        /// the total interest accrued from the start date to the current date
        CumulativeInterest: int64<Cent>
        /// the principal portion paid off by the payment
        Principal: int64<Cent>
        /// the principal balance carried forward
        PrincipalBalance: int64<Cent>
    }

    ///  a schedule of payments, with final statistics based on the payments being made on time and in full
    type Schedule = {
        /// the day, expressed as an offset from the start date, on which the schedule is inspected
        AsOfDay: int<OffsetDay>
        /// the items of the schedule
        Items: ScheduleItem array
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
        Apr: Percent
        /// the cost of borrowing, expressed as a ratio of interest to principal
        CostToBorrowingRatio: Percent
    }

    /// technical calculation options
    [<Struct>]
    type Calculation = {
        /// which APR calculation method to use
        AprMethod: Apr.CalculationMethod
        RoundingOptions: RoundingOptions
        FinalPaymentAdjustment: FinalPaymentAdjustment
    }

    module Calculation =
        /// the recommended default calculation options
        let recommended = {
            AprMethod = Apr.CalculationMethod.UsActuarial 8
            RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
            FinalPaymentAdjustment = AdjustFinalPayment
        }

    /// parameters for creating a payment schedule
    [<Struct>]
    type ScheduleParameters = {
        /// the date on which the schedule is inspected, typically today, but can be used to inspect it at any point (affects e.g. whether schedule payments are deemed as not yet due)
        AsOfDate: DateTime
        /// the start date of the schedule, typically the day on which the principal is advanced
        StartDate: DateTime
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
    let calculateSchedule toleranceOption sp =
        if sp.PaymentCount = 0 then ValueNone else
        if sp.StartDate > UnitPeriod.configStartDate sp.UnitPeriodConfig then ValueNone else
        let paymentDates = Schedule.generate sp.PaymentCount Schedule.Forward sp.UnitPeriodConfig
        let finalPaymentDate = paymentDates |> Array.max
        let finalPaymentDay = (finalPaymentDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
        let paymentCount = paymentDates |> Array.length
        let fees = Fees.total sp.Principal sp.FeesAndCharges.Fees
        let dailyInterestRate = sp.Interest.Rate |> Interest.Rate.daily
        let totalInterestCap = sp.Interest.Cap.Total |> Interest.Cap.total sp.Principal sp.Calculation.RoundingOptions.InterestRounding
        let roughPayment = decimal sp.Principal / decimal paymentCount
        let advance = { Day = 0<OffsetDay>; Advance = sp.Principal + fees; Payment = 0L<Cent>; Interest = 0L<Cent>; CumulativeInterest = 0L<Cent>; Principal = 0L<Cent>; PrincipalBalance = sp.Principal + fees }
        let toleranceSteps = ValueSome { ToleranceSteps.Min = 0; ToleranceSteps.Step = paymentCount; ToleranceSteps.Max = paymentCount * 2 }
        let paymentDays = paymentDates |> Array.map(fun dt -> (dt.Date - sp.StartDate.Date).Days * 1<OffsetDay>)
        let mutable schedule = [||]
        let generator payment =
            schedule <-
                paymentDays
                |> Array.scan(fun si d ->
                    let interestChargeableDays = Interest.chargeableDays sp.StartDate ValueNone sp.Interest.GracePeriod sp.Interest.Holidays si.Day d
                    let dailyInterestCap = sp.Interest.Cap.Daily |> Interest.Cap.daily si.PrincipalBalance interestChargeableDays sp.Calculation.RoundingOptions.InterestRounding
                    let interest = Interest.calculate dailyInterestCap si.PrincipalBalance dailyInterestRate interestChargeableDays sp.Calculation.RoundingOptions.InterestRounding
                    let interest' = si.CumulativeInterest + interest |> fun i -> if i >= totalInterestCap then totalInterestCap - si.CumulativeInterest else interest
                    let payment' = Cent.round sp.Calculation.RoundingOptions.PaymentRounding payment
                    let principalPortion = payment' - interest'
                    {
                        Day = d
                        Advance = 0L<Cent>
                        Payment = payment'
                        Interest = interest'
                        CumulativeInterest = si.CumulativeInterest + interest'
                        Principal = principalPortion
                        PrincipalBalance = si.PrincipalBalance - principalPortion
                    }
                ) advance
            let principalBalance = schedule |> Array.last |> _.PrincipalBalance |> decimal
            principalBalance
        match Array.solve generator 100 roughPayment toleranceOption toleranceSteps with
        | Solution.Found _ -> // note: payment is discarded because it is in the schedule
            let items =
                match sp.Calculation.FinalPaymentAdjustment with
                | AdjustFinalPayment ->
                    schedule
                    |> Array.map(fun si ->
                        if si.Day = finalPaymentDay then
                            { si with Payment = si.Payment + si.PrincipalBalance; Principal = si.Principal + si.PrincipalBalance; PrincipalBalance = si.PrincipalBalance - si.PrincipalBalance }
                        else si
                    )
                | SpreadOverLevelPayments ->
                    failwith "Not yet implemented" // to-do: this is tricky because adjusting payments pays off principal and affects interest calculations during the loan
            let principalTotal = items |> Array.sumBy _.Principal
            let interestTotal = items |> Array.sumBy _.Interest
            ValueSome {
                AsOfDay = (sp.AsOfDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                Items = items
                FinalPaymentDay = finalPaymentDay
                LevelPayment = items |> Array.countBy _.Payment |> Array.maxByOrDefault snd fst 0L<Cent>
                FinalPayment = items |> Array.last |> _.Payment
                PaymentTotal = items |> Array.sumBy _.Payment
                PrincipalTotal = principalTotal
                InterestTotal = interestTotal
                Apr =
                    items
                    |> Array.filter(fun si -> si.Payment > 0L<Cent>)
                    |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.Date = sp.StartDate.AddDays(float si.Day); Apr.Amount = si.Payment })
                    |> Apr.calculate sp.Calculation.AprMethod sp.Principal sp.StartDate
                CostToBorrowingRatio =
                    if principalTotal = 0L<Cent> then Percent 0m else
                    decimal (fees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 2
            }
        | _ ->
            ValueNone
