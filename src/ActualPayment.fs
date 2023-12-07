namespace FSharp.Finance.Personal

open System

/// functions for handling received payments and calculating interest and/or penalty charges where necessary
module ActualPayment =

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
        static member deserialise = function "Settled" -> Settled | "RefundDue" -> RefundDue | _ -> OpenBalance

    /// a real payment
    [<Struct>]
    type Payment = {
        Day: int<Day>
        ScheduledPayment: int64<Cent>
        ActualPayments: int64<Cent> array
        NetEffect: int64<Cent>
        PaymentStatus: PaymentStatus voption
        PenaltyCharges: PenaltyCharge array
    }
 
    /// detail of a payment with apportionment to principal, product fees, interest and penalty charges
    [<Struct>]
    type Apportionment = {
        Date: DateTime
        TermDay: int<Day>
        Advance: int64<Cent>
        ScheduledPayment: int64<Cent>
        ActualPayments: int64<Cent> array
        NetEffect: int64<Cent>
        PaymentStatus: PaymentStatus voption
        BalanceStatus: BalanceStatus
        CumulativeInterest: int64<Cent>
        NewInterest: int64<Cent>
        NewPenaltyCharges: int64<Cent>
        PrincipalPortion: int64<Cent>
        ProductFeesPortion: int64<Cent>
        InterestPortion: int64<Cent>
        PenaltyChargesPortion: int64<Cent>
        ProductFeesRefund: int64<Cent>
        PrincipalBalance: int64<Cent>
        ProductFeesBalance: int64<Cent>
        InterestBalance: int64<Cent>
        PenaltyChargesBalance: int64<Cent>
    }

    type AmortisationSchedule = {
        Items: Apportionment array
        FinalPaymentCount: int
        FinalApr: Percent
    }

    /// merges scheduled and actual payments by date, adds a payment status and a late payment penalty charge if underpaid
    let mergePayments asOfDay latePaymentPenaltyCharge actualPayments (scheduledPayments: ScheduledPayment.ScheduleItem array) =
        if Array.isEmpty scheduledPayments then [||] else
        let finalScheduledPaymentDay = scheduledPayments |> Array.maxBy _.Day |> _.Day
        scheduledPayments
        |> Array.map(fun si -> { Day = si.Day; ScheduledPayment = si.Payment; ActualPayments = [||]; NetEffect=0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
        |> fun sp -> Array.concat [| actualPayments; sp |]
        |> Array.groupBy _.Day
        |> Array.map(fun (d, pp) ->
            let scheduledPayment = pp |> Array.sumBy _.ScheduledPayment
            let actualPayments = pp |> Array.collect _.ActualPayments
            let netEffect, paymentStatus =
                match scheduledPayment, Array.sum actualPayments with
                | 0L<Cent>, 0L<Cent> -> 0L<Cent>, ValueNone
                | 0L<Cent>, ap when ap < 0L<Cent> -> ap, ValueSome Refunded
                | 0L<Cent>, ap -> ap, ValueSome ExtraPayment
                | sp, _ when d >= asOfDay && d <= finalScheduledPaymentDay -> sp, ValueSome NotYetDue
                | _, 0L<Cent> -> 0L<Cent>, ValueSome MissedPayment
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
    let calculateSchedule (sp: ScheduledPayment.ScheduleParameters) earlySettlementDate (mergedPayments: Payment array) =
        if Array.isEmpty mergedPayments then [||] else
        let dailyInterestRate = sp.InterestRate |> dailyInterestRate
        let interestCap = sp.InterestCap |> calculateInterestCap sp.Principal
        let productFeesTotal = productFeesTotal sp.Principal sp.ProductFees
        let productFeesPercentage = decimal productFeesTotal / decimal sp.Principal |> Percent.fromDecimal
        let maxScheduledPaymentDay = mergedPayments |> Array.filter(fun p -> p.ScheduledPayment > 0L<Cent>) |> Array.maxBy _.Day |> _.Day
        let dayZeroPayment = mergedPayments |> Array.head
        let ``don't apportion for a refund`` paymentTotal amount = if paymentTotal < 0L<Cent> then 0L<Cent> else amount
        let dayZeroPrincipalBalance = sp.Principal - dayZeroPayment.NetEffect
        let dayZeroPrincipalPortion = decimal dayZeroPayment.NetEffect / (1m + Percent.toDecimal productFeesPercentage) |> Cent.floor
        let dayZeroProductFeesPortion = dayZeroPayment.NetEffect - dayZeroPrincipalPortion
        let getBalanceStatus principalBalance = if principalBalance = 0L<Cent> then Settled elif principalBalance < 0L<Cent> then RefundDue else OpenBalance
        let advance = {
            Date = sp.StartDate
            TermDay = 0<Day>
            Advance = sp.Principal
            ScheduledPayment = dayZeroPayment.ScheduledPayment
            ActualPayments = dayZeroPayment.ActualPayments
            NetEffect = dayZeroPayment.NetEffect
            PaymentStatus = dayZeroPayment.PaymentStatus
            BalanceStatus = getBalanceStatus dayZeroPrincipalBalance
            CumulativeInterest = 0L<Cent>
            NewInterest = 0L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = dayZeroPrincipalPortion
            ProductFeesPortion = dayZeroProductFeesPortion
            InterestPortion = 0L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = dayZeroPrincipalBalance
            ProductFeesBalance = productFeesTotal
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        }
        mergedPayments
        |> Array.tail
        |> Array.scan(fun a p ->
            let newPenaltyCharges = penaltyChargesTotal p.PenaltyCharges
            let penaltyChargesPortion = newPenaltyCharges + a.PenaltyChargesBalance |> ``don't apportion for a refund`` p.NetEffect

            let interestChargeableDays = ScheduledPayment.interestChargeableDays sp.StartDate earlySettlementDate sp.InterestGracePeriod sp.InterestHolidays a.TermDay p.Day

            let newInterest = if a.PrincipalBalance <= 0L<Cent> then 0L<Cent> else decimal (a.PrincipalBalance + a.ProductFeesBalance) * Percent.toDecimal dailyInterestRate * decimal interestChargeableDays |> Cent.floor
            let newInterest' = a.CumulativeInterest + newInterest |> fun i -> if i >= interestCap then interestCap - a.CumulativeInterest else newInterest
            let interestPortion = newInterest' + a.InterestBalance |> ``don't apportion for a refund`` p.NetEffect
            
            let productFeesDue =
                match sp.ProductFeesSettlement with
                | DueInFull -> productFeesTotal
                | ProRataRefund -> Cent.min productFeesTotal (decimal productFeesTotal * decimal p.Day / decimal maxScheduledPaymentDay |> Cent.floor)
            let productFeesRemaining = productFeesTotal - productFeesDue

            let settlementFigure = a.PrincipalBalance + a.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = p.NetEffect = settlementFigure
            let isOverpayment = p.NetEffect > settlementFigure // to-do: only if the overpayment comes from a manual, not a scheduled payment that is not yet due

            let productFeesRefund =
                match sp.ProductFeesSettlement with
                | DueInFull -> 0L<Cent>
                | ProRataRefund -> if isOverpayment || isSettlement then productFeesRemaining else 0L<Cent>

            let actualPayment = abs p.NetEffect
            let sign: int64<Cent> -> int64<Cent> = if p.NetEffect < 0L<Cent> then (( * ) -1L) else id

            let principalPortion, productFeesPortion =
                if (penaltyChargesPortion > actualPayment) || (penaltyChargesPortion + interestPortion > actualPayment) then
                    0L<Cent>, 0L<Cent>
                else
                    if isOverpayment || isSettlement then
                        let productFeesPayment = a.ProductFeesBalance - productFeesRemaining
                        actualPayment - penaltyChargesPortion - interestPortion - productFeesPayment, productFeesPayment
                    else
                        let principalPayment = decimal (actualPayment - penaltyChargesPortion - interestPortion) / (1m + Percent.toDecimal productFeesPercentage) |> Cent.floor
                        principalPayment, actualPayment - penaltyChargesPortion - interestPortion - principalPayment

            let principalBalance = a.PrincipalBalance - sign principalPortion

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualPayment then
                    penaltyChargesPortion - actualPayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualPayment then
                    0L<Cent>, interestPortion - (actualPayment - penaltyChargesPortion)
                else
                    0L<Cent>, 0L<Cent>

            let finalPaymentAdjustment = if isOverpayment && (p.ScheduledPayment > abs principalBalance) then principalBalance else 0L<Cent>

            {
                Date = sp.StartDate.AddDays(float p.Day)
                TermDay = p.Day
                Advance = 0L<Cent>
                ScheduledPayment = p.ScheduledPayment + finalPaymentAdjustment
                ActualPayments = p.ActualPayments
                NetEffect = p.NetEffect + finalPaymentAdjustment
                PaymentStatus = if a.BalanceStatus = Settled then ValueNone elif isOverpayment && finalPaymentAdjustment = 0L<Cent> then ValueSome Overpayment else p.PaymentStatus
                BalanceStatus = getBalanceStatus (principalBalance - finalPaymentAdjustment)
                CumulativeInterest = a.CumulativeInterest + newInterest'
                NewInterest = newInterest'
                NewPenaltyCharges = newPenaltyCharges
                PrincipalPortion = sign (principalPortion + finalPaymentAdjustment)
                ProductFeesPortion = sign productFeesPortion
                InterestPortion = interestPortion - carriedInterest
                PenaltyChargesPortion = penaltyChargesPortion - carriedPenaltyCharges
                ProductFeesRefund = productFeesRefund
                PrincipalBalance = principalBalance - finalPaymentAdjustment
                ProductFeesBalance = a.ProductFeesBalance - sign productFeesPortion - productFeesRefund
                InterestBalance = a.InterestBalance + newInterest' - interestPortion + carriedInterest
                PenaltyChargesBalance = a.PenaltyChargesBalance + newPenaltyCharges - penaltyChargesPortion + carriedPenaltyCharges
            }
        ) advance
        |> Array.takeWhile(fun a -> a.TermDay = 0<Day> || (a.TermDay > 0<Day> && a.PaymentStatus.IsSome))

    let applyPayments (sp: ScheduledPayment.ScheduleParameters) earlySettlementDate (actualPayments: Payment array) =
        voption {
            let! schedule = ScheduledPayment.calculateSchedule sp
            return
                schedule
                |> _.Items
                |> mergePayments (Day.todayAsOffset sp.StartDate) 1000L<Cent> actualPayments
                |> calculateSchedule sp earlySettlementDate
        }

    let allPaidOnTime (scheduleItems: ScheduledPayment.ScheduleItem array) =
        scheduleItems
        |> Array.map(fun si -> { Day = si.Day; ScheduledPayment = 0L<Cent>; ActualPayments = [| si.Payment |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
