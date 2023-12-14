namespace FSharp.Finance.Personal

open System

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module ScheduledPayment =

    /// 
    [<Struct>]
    type ScheduleItem = {
        Day: int<OffsetDay>
        Advance: int64<Cent>
        Payment: int64<Cent>
        Interest: int64<Cent>
        CumulativeInterest: int64<Cent>
        Principal: int64<Cent>
        PrincipalBalance: int64<Cent>
    }

    type Schedule = {
        AsOfDay: int<OffsetDay>
        Items: ScheduleItem array
        FinalPaymentDay: int<OffsetDay>
        LevelPayment: int64<Cent>
        FinalPayment: int64<Cent>
        PaymentTotal: int64<Cent>
        PrincipalTotal: int64<Cent>
        InterestTotal: int64<Cent>
        Apr: Percent
        CostToBorrowingRatio: Percent
    }

    [<Struct>]
    type ScheduleParameters = {
        AsOfDate: DateTime
        StartDate: DateTime
        Principal: int64<Cent>
        ProductFees: ProductFees voption
        ProductFeesSettlement: ProductFeesSettlement
        InterestRate: InterestRate
        InterestCap: InterestCap
        InterestGracePeriod: int<Days>
        InterestHolidays: InterestHoliday array
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        AprCalculationMethod: Apr.CalculationMethod
        PenaltyCharges: PenaltyCharge array
        RoundingOptions: RoundingOptions
        FinalPaymentAdjustment: FinalPaymentAdjustment
    }

    let interestChargeableDays (startDate: DateTime) (earlySettlementDate: DateTime voption) (interestGracePeriod: int<Days>) interestHolidays (fromDay: int<OffsetDay>) (toDay: int<OffsetDay>) =
        let interestFreeDays =
            interestHolidays
            |> Array.collect(fun ih ->
                [| (ih.InterestHolidayStart.Date - startDate.Date).Days .. (ih.InterestHolidayEnd.Date - startDate.Date).Days |]
            )
            |> Array.filter(fun d -> d >= int fromDay && d <= int toDay)
        let isWithinGracePeriod d = d <= int interestGracePeriod
        let isSettledWithinGracePeriod = earlySettlementDate |> ValueOption.map(fun sd -> isWithinGracePeriod (sd.Date - startDate.Date).Days) |> ValueOption.defaultValue false
        [| int fromDay .. int toDay |]
        |> Array.filter(fun d -> not (isSettledWithinGracePeriod && isWithinGracePeriod d))
        |> Array.filter(fun d -> interestFreeDays |> Array.exists ((=) d) |> not)
        |> Array.length
        |> fun l -> (max 0 (l - 1)) * 1<Days>

    let calculateSchedule sp =
        if sp.PaymentCount = 0 then ValueNone else
        if sp.StartDate > UnitPeriod.configStartDate sp.UnitPeriodConfig then ValueNone else
        let paymentDates = Schedule.generate sp.PaymentCount Schedule.Forward sp.UnitPeriodConfig
        let finalPaymentDate = paymentDates |> Array.max
        let finalPaymentDay = (finalPaymentDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
        let paymentCount = paymentDates |> Array.length
        let productFees = productFeesTotal sp.Principal sp.ProductFees
        let dailyInterestRate = sp.InterestRate |> InterestRate.daily
        let totalInterestCap = sp.InterestCap.TotalCap |> InterestCap.totalCap sp.Principal sp.RoundingOptions.InterestRounding
        let roughPayment = decimal sp.Principal / decimal paymentCount
        let advance = { Day = 0<OffsetDay>; Advance = sp.Principal + productFees; Payment = 0L<Cent>; Interest = 0L<Cent>; CumulativeInterest = 0L<Cent>; Principal = 0L<Cent>; PrincipalBalance = sp.Principal + productFees }
        let toleranceSteps = ValueSome { ToleranceSteps.Min = 0L<Cent>; ToleranceSteps.Step = int64 paymentCount * 1L<Cent>; ToleranceSteps.Max = int64 paymentCount * 2L<Cent> }
        let paymentDays = paymentDates |> Array.map(fun dt -> (dt.Date - sp.StartDate.Date).Days * 1<OffsetDay>)
        let mutable schedule = [||]
        let generator payment =
            schedule <-
                paymentDays
                |> Array.scan(fun si d ->
                    let interestChargeableDays = interestChargeableDays sp.StartDate ValueNone sp.InterestGracePeriod sp.InterestHolidays si.Day d
                    let dailyInterestCap = sp.InterestCap.DailyCap |> InterestCap.dailyCap si.PrincipalBalance interestChargeableDays sp.RoundingOptions.InterestRounding
                    let interest = calculateInterest dailyInterestCap si.PrincipalBalance dailyInterestRate interestChargeableDays sp.RoundingOptions.InterestRounding
                    let interest' = si.CumulativeInterest + interest |> fun i -> if i >= totalInterestCap then totalInterestCap - si.CumulativeInterest else interest
                    let payment' = Cent.round sp.RoundingOptions.PaymentRounding payment
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
        Array.solve generator 100 roughPayment BelowZero toleranceSteps
        |> function
            | Solution.Found _ -> // note: payment is discarded because it is in the schedule
                let items =
                    match sp.FinalPaymentAdjustment with
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
                        |> Apr.calculate sp.AprCalculationMethod 8 sp.Principal sp.StartDate
                    CostToBorrowingRatio =
                        if principalTotal = 0L<Cent> then Percent 0m else
                        decimal (productFees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 6
                }
            | _ ->
                ValueNone
