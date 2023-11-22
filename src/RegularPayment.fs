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
        ProductFees: ProductFees
        InterestRate: InterestRate
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        /// the maximum amount by which the final payment may be less than the level payment (default is `100<Cent>`)
        FinalPaymentTolerance: int<Cent> voption
    }

    let calculateSchedule sp =
        let paymentDates = Schedule.generate sp.PaymentCount Schedule.Forward sp.UnitPeriodConfig
        let finalPaymentDate = paymentDates |> Array.max
        let paymentCount = paymentDates |> Array.length
        let productFees = productFeesTotal sp.Principal sp.ProductFees
        let roughPayment = decimal sp.Principal / decimal paymentCount
        let advance = { Day = 0<Day>; Advance = ValueSome (sp.Principal + productFees); Payment = ValueNone; Interest = 0<Cent>; Principal = 0<Cent>; PrincipalBalance = sp.Principal + productFees }
        let finalPaymentTolerance = sp.FinalPaymentTolerance |> ValueOption.defaultValue 100<Cent>
        let dailyInterestRate = sp.InterestRate |> dailyInterestRate
        let items =
            roughPayment
            |> Array.unfold(fun roughPayment' ->
                if roughPayment' = 0m then None else
                let schedule =
                    paymentDates |> Array.map(fun dt -> dt, int (dt.Date - sp.StartDate.Date).TotalDays * 1<Day>)
                    |> Array.scan(fun si (dt, d) ->
                        let interest = decimal si.PrincipalBalance * Percent.toDecimal dailyInterestRate * decimal (d - si.Day) |> Cent.round
                        let payment = Cent.round roughPayment'
                        let principalPortion = payment - interest
                        let principalBalance = si.PrincipalBalance - principalPortion
                        let finalPaymentCorrection = if principalBalance > -finalPaymentTolerance && principalBalance < 0<Cent> then principalBalance else 0<Cent>
                        {
                            Day = d
                            Advance = ValueNone
                            Payment = ValueSome (payment + finalPaymentCorrection)
                            Interest = interest
                            Principal = principalPortion + finalPaymentCorrection
                            PrincipalBalance = if finalPaymentCorrection < 0<Cent> then 0<Cent> else principalBalance
                        }
                    ) advance
                let principalBalance = schedule |> Array.last |> fun psi -> psi.PrincipalBalance
                if principalBalance > -finalPaymentTolerance && principalBalance <= 0<Cent> then
                    Some (schedule, 0m)
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
