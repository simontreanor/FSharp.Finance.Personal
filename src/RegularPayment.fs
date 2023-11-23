namespace FSharp.Finance.Personal

open System

/// functions for generating a regular payment schedule, with payment amounts, interest and APR
module RegularPayment =

    /// 
    [<Struct>]
    type ScheduleItem = {
        Day: int<Day>
        Advance: int<Cent> voption
        Payment: int<Cent> voption
        Interest: int<Cent>
        CumulativeInterest: int<Cent>
        Principal: int<Cent>
        PrincipalBalance: int<Cent>
    }

    type Schedule = {
        Items: ScheduleItem array
        FinalPaymentDay: int<Day>
        LevelPayment: int<Cent>
        FinalPayment: int<Cent>
        PaymentTotal: int<Cent>
        PrincipalTotal: int<Cent>
        InterestTotal: int<Cent>
        Apr: decimal<Percent>
        CostToBorrowingRatio: decimal<Percent>
    }

    [<Struct>]
    type ScheduleParameters = {
        StartDate: DateTime
        Principal: int<Cent>
        ProductFees: ProductFees voption
        InterestRate: InterestRate
        InterestCap: InterestCap voption
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
    }

    let calculateSchedule sp =
        let paymentDates = Schedule.generate sp.PaymentCount Schedule.Forward sp.UnitPeriodConfig
        let finalPaymentDate = paymentDates |> Array.max
        let paymentCount = paymentDates |> Array.length
        let productFees = productFeesTotal sp.Principal sp.ProductFees
        let roughPayment = decimal sp.Principal / decimal paymentCount
        let advance = { Day = 0<Day>; Advance = ValueSome (sp.Principal + productFees); Payment = ValueNone; Interest = 0<Cent>; CumulativeInterest = 0<Cent>; Principal = 0<Cent>; PrincipalBalance = sp.Principal + productFees }
        let dailyInterestRate = sp.InterestRate |> dailyInterestRate
        let interestCap = sp.InterestCap |> calculateInterestCap sp.Principal
        let items =
            roughPayment
            |> Array.unfold(fun roughPayment' ->
                if roughPayment' = 0m then None else
                let schedule =
                    paymentDates |> Array.map(fun dt -> dt, int (dt.Date - sp.StartDate.Date).TotalDays * 1<Day>)
                    |> Array.scan(fun si (dt, d) ->
                        let interest = decimal si.PrincipalBalance * Percent.toDecimal dailyInterestRate * decimal (d - si.Day) |> Cent.round
                        let interest' = si.CumulativeInterest + interest |> fun i -> if i >= interestCap then interestCap - si.CumulativeInterest else interest
                        let payment = Cent.round roughPayment'
                        let principalPortion = payment - interest
                        let principalBalance = si.PrincipalBalance - principalPortion
                        {
                            Day = d
                            Advance = ValueNone
                            Payment = ValueSome payment
                            Interest = interest'
                            CumulativeInterest = si.CumulativeInterest + interest'
                            Principal = principalPortion
                            PrincipalBalance = principalBalance
                        }
                    ) advance
                let principalBalance = schedule |> Array.last |> fun psi -> psi.PrincipalBalance
                let adjustFinalPayment principalBalance schedule = // note: principal balance is negative
                    schedule
                    |> Array.splitAt ((schedule |> Array.length) - 1)
                    |> fun (rest, last) ->
                        let last' = last |> Array.exactlyOne
                        let last'' =
                            { last' with
                                Payment = last'.Payment |> ValueOption.map(fun p -> p + principalBalance)
                                Principal = last'.Principal + principalBalance
                                PrincipalBalance = 0<Cent>
                            }
                        [| rest; [| last'' |] |]
                    |> Array.concat
                if principalBalance > -paymentCount * 1<Cent> && principalBalance <= 0<Cent> then // tolerance is the payment count expressed as a number of cents // to-do: check if this is too tolerant with large payment counts
                    Some (adjustFinalPayment principalBalance schedule, 0m)
                else
                    Some (schedule, roughPayment' + (decimal principalBalance / decimal paymentCount))
            )
            |> Array.last
        let principalTotal = items |> Array.sumBy _.Principal
        let interestTotal = items |> Array.sumBy _.Interest
        let finalPaymentDay = int (finalPaymentDate.Date - sp.StartDate.Date).TotalDays * 1<Day>
        {
            Items = items
            FinalPaymentDay = finalPaymentDay
            LevelPayment = items |> Array.countBy (_.Payment >> ValueOption.defaultValue 0<Cent>) |> Array.maxBy snd |> fst
            FinalPayment = items |> Array.last |> _.Payment |> ValueOption.defaultValue 0<Cent>
            PaymentTotal = items |> Array.sumBy (_.Payment >> ValueOption.defaultValue 0<Cent>)
            PrincipalTotal = principalTotal
            InterestTotal = interestTotal
            Apr =
                items
                |> Array.filter(fun si -> si.Payment.IsSome)
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.Date = sp.StartDate.AddDays(float si.Day); Apr.Amount = si.Payment.Value })
                |> Apr.calculate Apr.UsActuarial 8 sp.Principal sp.StartDate
            CostToBorrowingRatio = decimal (productFees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 6
        }
