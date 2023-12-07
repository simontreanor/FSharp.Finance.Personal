namespace FSharp.Finance.Personal

open System

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module ScheduledPayment =

    /// 
    [<Struct>]
    type ScheduleItem = {
        Day: int<Day>
        Advance: int64<Cent>
        Payment: int64<Cent>
        Interest: int64<Cent>
        CumulativeInterest: int64<Cent>
        Principal: int64<Cent>
        PrincipalBalance: int64<Cent>
    }

    type Schedule = {
        Items: ScheduleItem array
        FinalPaymentDay: int<Day>
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
        StartDate: DateTime
        Principal: int64<Cent>
        ProductFees: ProductFees voption
        ProductFeesSettlement: ProductFeesSettlement
        InterestRate: InterestRate
        InterestCap: InterestCap voption
        InterestGracePeriod: int<Duration>
        InterestHolidays: InterestHoliday array
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
    }

    let interestChargeableDays (startDate: DateTime) (earlySettlementDate: DateTime voption) (interestGracePeriod: int<Duration>) interestHolidays (fromDay: int<Day>) (toDay: int<Day>) =
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
        |> fun l -> max 0 (l - 1)

    let calculateSchedule sp =
        voption {
            if sp.PaymentCount = 0 then return! ValueNone else
            if sp.StartDate > UnitPeriod.configStartDate sp.UnitPeriodConfig then return! ValueNone else
            let paymentDates = Schedule.generate sp.PaymentCount Schedule.Forward sp.UnitPeriodConfig
            let finalPaymentDate = paymentDates |> Array.max
            let finalPaymentDay = (finalPaymentDate.Date - sp.StartDate.Date).Days * 1<Day>
            let paymentCount = paymentDates |> Array.length
            let productFees = productFeesTotal sp.Principal sp.ProductFees
            let dailyInterestRate = sp.InterestRate |> dailyInterestRate
            let interestCap = sp.InterestCap |> calculateInterestCap sp.Principal
            let roughPayment = decimal sp.Principal / decimal paymentCount
            let advance = { Day = 0<Day>; Advance = sp.Principal + productFees; Payment = 0L<Cent>; Interest = 0L<Cent>; CumulativeInterest = 0L<Cent>; Principal = 0L<Cent>; PrincipalBalance = sp.Principal + productFees }
            let tolerance = int64 paymentCount * 2L<Cent>
            let paymentDays = paymentDates |> Array.map(fun dt -> (dt.Date - sp.StartDate.Date).Days * 1<Day>)
            let! schedule =
                roughPayment
                |> Array.solve(fun roughPayment' ->
                    if roughPayment' = 0m then ValueNone else
                    let schedule =
                        paymentDays
                        |> Array.scan(fun si d ->
                            let interestChargeableDays = interestChargeableDays sp.StartDate ValueNone sp.InterestGracePeriod sp.InterestHolidays si.Day d
                            let interest = decimal si.PrincipalBalance * Percent.toDecimal dailyInterestRate * decimal interestChargeableDays |> Cent.floor
                            let interest' = si.CumulativeInterest + interest |> fun i -> if i >= interestCap then interestCap - si.CumulativeInterest else interest
                            let payment = Cent.ceil roughPayment'
                            let principalPortion = payment - interest
                            {
                                Day = d
                                Advance = 0L<Cent>
                                Payment = payment
                                Interest = interest'
                                CumulativeInterest = si.CumulativeInterest + interest'
                                Principal = principalPortion
                                PrincipalBalance = si.PrincipalBalance - principalPortion
                            }
                        ) advance
                    let principalBalance = schedule |> Array.last |> fun psi -> psi.PrincipalBalance
                    if principalBalance >= -tolerance && principalBalance <= 0L<Cent> then
                        ValueSome (schedule, 0m)
                    else
                        ValueSome (schedule, roughPayment' + (decimal principalBalance / decimal paymentCount))
                ) 100
            let items =
                schedule
                |> Array.map(fun si ->
                    if si.Day = finalPaymentDay then { si with Payment = si.Payment + si.PrincipalBalance; Principal = si.Principal + si.PrincipalBalance; PrincipalBalance = si.PrincipalBalance - si.PrincipalBalance }
                    else si
                )
            let principalTotal =
                try
                    items |> Array.sumBy _.Principal
                with
                    | ex ->
                        items |> Formatting.outputListToHtml $"out/SchedulePayment.calculateSchedule.principalTotalError.{DateTime.Now.Ticks}.md" (ValueSome 300)
                        failwith $"Principal total calculation error: {ex.Message}"
            let interestTotal = items |> Array.sumBy _.Interest
            return! ValueSome {
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
                    |> Apr.calculate Apr.UsActuarial 8 sp.Principal sp.StartDate
                CostToBorrowingRatio =
                    if principalTotal = 0L<Cent> then Percent 0m else
                    decimal (productFees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 6
            }
        }
