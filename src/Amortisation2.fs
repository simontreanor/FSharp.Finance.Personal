namespace FSharp.Finance.Personal

open System

module Amortisation2 =

    [<Struct>]
    type InterestRate =
        | AnnualInterestRate of AnnualInterestRate:decimal<Percent>
        | DailyInterestRate of DailyInterestRate:decimal<Percent>

    /// 
    [<Struct>]
    type RegularPaymentScheduleItem = {
        Date: DateTime
        Day: int<Day>
        Advance: int<Cent>
        Payment: int<Cent>
        Interest: int<Cent>
        Principal: int<Cent>
        PrincipalBalance: int<Cent>
    }

    type RegularPaymentSchedule = {
        Items: RegularPaymentScheduleItem array
        FinalPaymentDay: int<Day>
        LevelPayment: int<Cent>
        FinalPayment: int<Cent>
        PaymentTotal: int<Cent>
        PrincipalTotal: int<Cent>
        InterestTotal: int<Cent>
        Apr: decimal<Percent>
        CostToBorrowingRatio: decimal<Percent>
    }

    /// the type and amount of any product fees, taking into account any constraints
    [<Struct>]
    type ProductFees =
        | Percentage of Percentage:decimal<Percent> * Cap:int<Cent> voption
        | Simple of Simple:int<Cent>

    [<Struct>]
    type RegularPaymentScheduleParameters = {
        StartDate: DateTime
        Principal: int<Cent>
        ProductFees: ProductFees
        InterestRate: InterestRate
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        /// the maximum amount by which the final payment may be less than the level payment (default is `100<Cent>`)
        FinalPaymentTolerance: int<Cent> voption
    }

    let calculateProductFees (principal: int<Cent>) productFees =
        match productFees with
        | Percentage (percentage, ValueSome cap) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
            |> fun cents -> Cent.min cents cap
        | Percentage (percentage, ValueNone) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
        | Simple simple -> simple

    let calculateRegularPaymentSchedule psp =
        let paymentDates = Schedule.generate psp.PaymentCount Schedule.Forward psp.UnitPeriodConfig
        let finalPaymentDate = paymentDates |> Array.max
        let paymentCount = paymentDates |> Array.length
        let productFees = calculateProductFees psp.Principal psp.ProductFees
        let roughPayment = decimal psp.Principal / decimal paymentCount
        let advance = { Date = psp.StartDate; Day = 0<Day>; Advance = psp.Principal + productFees; Payment = 0<Cent>; Interest = 0<Cent>; Principal = 0<Cent>; PrincipalBalance = psp.Principal + productFees }
        let finalPaymentTolerance = psp.FinalPaymentTolerance |> ValueOption.defaultValue 100<Cent>
        let dailyInterestRate = psp.InterestRate |> function AnnualInterestRate ir -> ir / 365m | DailyInterestRate ir -> ir
        let items =
            roughPayment
            |> Array.unfold(fun roughPayment' ->
                if roughPayment' = 0m then None else
                let schedule =
                    paymentDates |> Array.map(fun dt -> dt, int (dt.Date - psp.StartDate.Date).TotalDays * 1<Day>)
                    |> Array.scan(fun psi (dt, d) ->
                        let interest = decimal psi.PrincipalBalance * Percent.toDecimal dailyInterestRate * decimal (d - psi.Day) |> Cent.round
                        let payment = Cent.round roughPayment'
                        let principalPortion = payment - interest
                        let principalBalance = psi.PrincipalBalance - principalPortion
                        let finalPaymentCorrection = if principalBalance > -finalPaymentTolerance && principalBalance < 0<Cent> then principalBalance else 0<Cent>
                        {
                            Date = dt
                            Day = d
                            Advance = 0<Cent>
                            Payment = payment + finalPaymentCorrection
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
        let finalPaymentDay = int (finalPaymentDate.Date - psp.StartDate.Date).TotalDays * 1<Day>
        {
            Items = items
            FinalPaymentDay = finalPaymentDay
            LevelPayment = items |> Array.countBy _.Payment |> Array.maxBy snd |> fst
            FinalPayment = items |> Array.last |> _.Payment
            PaymentTotal = items |> Array.sumBy _.Payment
            PrincipalTotal = principalTotal
            InterestTotal = interestTotal
            Apr =
                items
                |> Array.filter(fun psi -> psi.Payment > 0<Cent>)
                |> Array.map(fun psi -> { Apr.TransferType = Apr.Payment; Apr.Date = psi.Date; Apr.Amount = psi.Payment })
                |> Apr.calculate Apr.UsActuarial 8 psp.Principal psp.StartDate
            CostToBorrowingRatio = decimal (productFees + interestTotal) / decimal principalTotal |> Percent.fromDecimal |> Percent.round 6
        }
