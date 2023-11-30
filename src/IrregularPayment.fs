namespace FSharp.Finance.Personal

open System

/// functions for handling received payments and calculating interest and/or penalty charges where necessary
module IrregularPayment =

    [<Struct>]
    type PaymentStatus =
        | PaymentMade
        | MissedPayment
        | Underpayment
        | Overpayment
        | ExtraPayment
        | Refunded
        | NotYetDue
        | Overpaid

    [<Struct>]
    type BalanceStatus =
        | Settled
        | OpenBalance
        | RefundDue

    /// a real payment
    [<Struct>]
    type Payment = {
        Day: int<Day>
        ScheduledPayment: int<Cent>
        ActualPayments: int<Cent> array
        NetEffect: int<Cent>
        PaymentStatus: PaymentStatus voption
        PenaltyCharges: PenaltyCharge array
    }
 
    /// detail of a payment with apportionment to principal, product fees, interest and penalty charges
    [<Struct>]
    type Apportionment = {
        Date: DateTime
        TermDay: int<Day>
        Advance: int<Cent>
        ScheduledPayment: int<Cent>
        ActualPayments: int<Cent> array
        NetEffect: int<Cent>
        PaymentStatus: PaymentStatus voption
        BalanceStatus: BalanceStatus
        CumulativeInterest: int<Cent>
        NewInterest: int<Cent>
        NewPenaltyCharges: int<Cent>
        PrincipalPortion: int<Cent>
        ProductFeesPortion: int<Cent>
        InterestPortion: int<Cent>
        PenaltyChargesPortion: int<Cent>
        ProductFeesRefund: int<Cent>
        PrincipalBalance: int<Cent>
        ProductFeesBalance: int<Cent>
        InterestBalance: int<Cent>
        PenaltyChargesBalance: int<Cent>
    }

    type AmortisationSchedule = {
        Items: Apportionment array
        FinalPaymentCount: int
        FinalApr: Percent
    }

    /// merges scheduled and actual payments by date, adds a payment status and a late payment penalty charge if underpaid
    let mergePayments asOfDay latePaymentPenaltyCharge actualPayments (scheduledPayments: RegularPayment.ScheduleItem array) =
        if Array.isEmpty scheduledPayments then [||] else
        let finalScheduledPaymentDay = scheduledPayments |> Array.maxBy _.Day |> _.Day
        scheduledPayments
        |> Array.map(fun si -> { Day = si.Day; ScheduledPayment = si.Payment; ActualPayments = [||]; NetEffect=0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
        |> fun sp -> Array.concat [| actualPayments; sp |]
        |> Array.groupBy _.Day
        |> Array.map(fun (d, pp) ->
            let scheduledPayment = pp |> Array.sumBy _.ScheduledPayment
            let actualPayments = pp |> Array.collect _.ActualPayments
            let netEffect, paymentStatus =
                match scheduledPayment, Array.sum actualPayments with
                | 0<Cent>, 0<Cent> -> 0<Cent>, ValueNone
                | 0<Cent>, ap when ap < 0<Cent> -> ap, ValueSome Refunded
                | 0<Cent>, ap -> ap, ValueSome ExtraPayment
                | sp, _ when d >= asOfDay && d <= finalScheduledPaymentDay -> sp, ValueSome NotYetDue
                | _, 0<Cent> -> 0<Cent>, ValueSome MissedPayment
                | sp, ap when ap < sp -> ap, ValueSome Underpayment
                | sp, ap when ap > sp -> ap, ValueSome Overpayment
                | sp, ap when sp = ap -> sp, ValueSome PaymentMade
                | sp, ap -> failwith $"Unexpected permutation of scheduled ({sp}) vs actual payments ({ap})"
            let penaltyCharges =
                pp
                |> Array.collect _.PenaltyCharges
                |> Array.append(match paymentStatus with ValueSome MissedPayment | ValueSome Underpayment -> [| LatePayment latePaymentPenaltyCharge |] | _ -> [||])
            { Day = d; ScheduledPayment = scheduledPayment; ActualPayments = actualPayments; NetEffect = netEffect; PaymentStatus = paymentStatus; PenaltyCharges = penaltyCharges }
        )
        |> Array.sortBy _.Day
        
    /// calculate amortisation schedule detailing how elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (sp: RegularPayment.ScheduleParameters) isEarlySettlement (mergedPayments: Payment array) =
        if Array.isEmpty mergedPayments then [||] else
        let dailyInterestRate = sp.InterestRate |> dailyInterestRate
        let interestCap = sp.InterestCap |> calculateInterestCap sp.Principal
        let productFeesTotal = productFeesTotal sp.Principal sp.ProductFees
        let productFeesPercentage = decimal productFeesTotal / decimal sp.Principal |> Percent.fromDecimal
        let maxScheduledPaymentDay = mergedPayments |> Array.filter(fun p -> p.ScheduledPayment > 0<Cent>) |> Array.maxBy _.Day |> _.Day
        let dayZeroPayment = mergedPayments |> Array.head
        let ``don't apportion for a refund`` paymentTotal amount = if paymentTotal < 0<Cent> then 0<Cent> else amount
        let dayZeroPrincipalBalance = sp.Principal - dayZeroPayment.NetEffect
        let dayZeroPrincipalPortion = decimal dayZeroPayment.NetEffect / (1m + Percent.toDecimal productFeesPercentage) |> Cent.floor
        let dayZeroProductFeesPortion = dayZeroPayment.NetEffect - dayZeroPrincipalPortion
        let getBalanceStatus principalBalance = if principalBalance = 0<Cent> then Settled elif principalBalance < 0<Cent> then RefundDue else OpenBalance
        let advance = {
            Date = sp.StartDate
            TermDay = 0<Day>
            Advance = sp.Principal
            ScheduledPayment = dayZeroPayment.ScheduledPayment
            ActualPayments = dayZeroPayment.ActualPayments
            NetEffect = dayZeroPayment.NetEffect
            PaymentStatus = dayZeroPayment.PaymentStatus
            BalanceStatus = getBalanceStatus dayZeroPrincipalBalance
            CumulativeInterest = 0<Cent>
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = dayZeroPrincipalPortion
            ProductFeesPortion = dayZeroProductFeesPortion
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = dayZeroPrincipalBalance
            ProductFeesBalance = productFeesTotal
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        mergedPayments
        |> Array.tail
        |> Array.scan(fun a p ->
            let newPenaltyCharges = penaltyChargesTotal p.PenaltyCharges
            let penaltyChargesPortion = newPenaltyCharges + a.PenaltyChargesBalance |> ``don't apportion for a refund`` p.NetEffect

            let interestChargeableDays = RegularPayment.interestChargeableDays sp.StartDate isEarlySettlement sp.InterestGracePeriod sp.InterestHolidays a.TermDay p.Day

            let newInterest = if a.PrincipalBalance <= 0<Cent> then 0<Cent> else decimal (a.PrincipalBalance + a.ProductFeesBalance) * Percent.toDecimal dailyInterestRate * decimal interestChargeableDays |> Cent.floor
            let newInterest' = a.CumulativeInterest + newInterest |> fun i -> if i >= interestCap then interestCap - a.CumulativeInterest else newInterest
            let interestPortion = newInterest' + a.InterestBalance |> ``don't apportion for a refund`` p.NetEffect
            
            let productFeesDue = Cent.min productFeesTotal (decimal productFeesTotal * decimal p.Day / decimal maxScheduledPaymentDay |> Cent.floor)
            let productFeesRemaining = productFeesTotal - productFeesDue

            let settlementFigure = a.PrincipalBalance + a.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = p.NetEffect = settlementFigure
            let isOverpayment = p.NetEffect > settlementFigure

            let productFeesRefund = if isSettlement then productFeesRemaining else 0<Cent>

            let actualPayment = abs p.NetEffect
            let sign: int<Cent> -> int<Cent> = if p.NetEffect < 0<Cent> then (( * ) -1) else id

            let principalPortion, productFeesPortion =
                if (penaltyChargesPortion > actualPayment) || (penaltyChargesPortion + interestPortion > actualPayment) then
                    0<Cent>, 0<Cent>
                else
                    if isOverpayment || isSettlement then
                        let productFeesPayment = a.ProductFeesBalance - productFeesRemaining
                        actualPayment - penaltyChargesPortion - interestPortion - productFeesPayment, productFeesPayment
                    else
                        let principalPayment = decimal (actualPayment - penaltyChargesPortion - interestPortion) / (1m + Percent.toDecimal productFeesPercentage) |> Cent.floor
                        principalPayment, actualPayment - penaltyChargesPortion - interestPortion - principalPayment

            // let principalPortion = Cent.min a.PrincipalBalance principalPortion
            // let productFeesPortion = Cent.min a.ProductFeesBalance productFeesPortion

            let principalBalance = a.PrincipalBalance - sign principalPortion

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualPayment then
                    penaltyChargesPortion - actualPayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualPayment then
                    0<Cent>, interestPortion - (actualPayment - penaltyChargesPortion)
                else
                    0<Cent>, 0<Cent>

            {
                Date = sp.StartDate.AddDays(float p.Day)
                TermDay = p.Day
                Advance = 0<Cent>
                ScheduledPayment = p.ScheduledPayment
                ActualPayments = p.ActualPayments
                NetEffect = p.NetEffect
                PaymentStatus = if a.BalanceStatus = Settled then ValueNone elif isOverpayment then ValueSome Overpayment else p.PaymentStatus
                BalanceStatus = getBalanceStatus principalBalance
                CumulativeInterest = a.CumulativeInterest + newInterest'
                NewInterest = newInterest'
                NewPenaltyCharges = newPenaltyCharges
                PrincipalPortion = sign principalPortion
                ProductFeesPortion = sign productFeesPortion
                InterestPortion = interestPortion - carriedInterest
                PenaltyChargesPortion = penaltyChargesPortion - carriedPenaltyCharges
                ProductFeesRefund = productFeesRefund
                PrincipalBalance = principalBalance
                ProductFeesBalance = a.ProductFeesBalance - sign productFeesPortion - productFeesRefund
                InterestBalance = a.InterestBalance + newInterest' - interestPortion + carriedInterest
                PenaltyChargesBalance = a.PenaltyChargesBalance + newPenaltyCharges - penaltyChargesPortion + carriedPenaltyCharges
            }
        ) advance
        |> Array.takeWhile(fun a -> a.TermDay = 0<Day> || (a.TermDay > 0<Day> && a.PaymentStatus.IsSome))

    let applyPayments (sp: RegularPayment.ScheduleParameters) isEarlySettlement (actualPayments: Payment array) =
        voption {
            let! schedule = RegularPayment.calculateSchedule sp
            return
                schedule
                |> _.Items
                |> mergePayments (Day.todayAsOffset sp.StartDate) 1000<Cent> actualPayments
                |> calculateSchedule sp isEarlySettlement
        }
