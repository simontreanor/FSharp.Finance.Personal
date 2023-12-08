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
        PaymentDay: int<OffsetDay>
        ScheduledPayment: int64<Cent>
        ActualPayments: int64<Cent> array
        PenaltyCharges: PenaltyCharge array
    }
 
    /// a real payment
    [<Struct>]
    type AppliedPayment = {
        AppliedPaymentDay: int<OffsetDay>
        ScheduledPayment: int64<Cent>
        ActualPayments: int64<Cent> array
        NetEffect: int64<Cent>
        PaymentStatus: PaymentStatus voption
        PenaltyCharges: PenaltyCharge array
    }

    /// amortisation schedule item showing apportionment of payments to principal, product fees, interest and penalty charges
    [<Struct>]
    type AmortisationScheduleItem = {
        OffsetDate: DateTime
        OffsetDay: int<OffsetDay>
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
        PenaltyCharges: PenaltyCharge array
    }

    type AmortisationSchedule = {
        Items: AmortisationScheduleItem array
        FinalScheduledPaymentCount: int
        FinalActualPaymentCount: int
        FinalApr: Percent
    }

    /// merges scheduled and actual payments by date, adds a payment status and a late payment penalty charge if underpaid
    let applyPayments asOfDay latePaymentPenaltyCharge actualPayments (scheduledPayments: ScheduledPayment.ScheduleItem array) =
        if Array.isEmpty scheduledPayments then [||] else
        scheduledPayments
        |> Array.map(fun si -> { PaymentDay = si.Day; ScheduledPayment = si.Payment; ActualPayments = [||]; PenaltyCharges = [||] })
        |> fun payments -> Array.concat [| actualPayments; payments |]
        |> Array.groupBy _.PaymentDay
        |> Array.map(fun (offsetDay, payments) ->
            let scheduledPayment = payments |> Array.sumBy _.ScheduledPayment
            let actualPayments = payments |> Array.collect _.ActualPayments
            let netEffect, paymentStatus =
                match scheduledPayment, Array.sum actualPayments with
                | 0L<Cent>, 0L<Cent> -> 0L<Cent>, ValueNone
                | 0L<Cent>, ap when ap < 0L<Cent> -> ap, ValueSome Refunded
                | 0L<Cent>, ap -> ap, ValueSome ExtraPayment
                | sp, _ when offsetDay >= asOfDay -> sp, ValueSome NotYetDue
                | _, 0L<Cent> -> 0L<Cent>, ValueSome MissedPayment
                | sp, ap when ap < sp -> ap, ValueSome Underpayment
                | sp, ap when ap > sp -> ap, ValueSome Overpayment
                | sp, ap when sp = ap -> sp, ValueSome PaymentMade
                | sp, ap -> failwith $"Unexpected permutation of scheduled ({sp}) vs actual payments ({ap})"
            let penaltyCharges =
                payments
                |> Array.collect _.PenaltyCharges
                |> Array.append(match paymentStatus with ValueSome MissedPayment | ValueSome Underpayment -> [| LatePayment latePaymentPenaltyCharge |] | _ -> [||])
            { AppliedPaymentDay = offsetDay; ScheduledPayment = scheduledPayment; ActualPayments = actualPayments; NetEffect = netEffect; PaymentStatus = paymentStatus; PenaltyCharges = penaltyCharges }
        )
        |> Array.sortBy _.AppliedPaymentDay
        
    /// calculate amortisation schedule detailing how elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (sp: ScheduledPayment.ScheduleParameters) earlySettlementDate originalFinalPaymentDay (mergedPayments: AppliedPayment array) =
        if Array.isEmpty mergedPayments then [||] else
        let asOfDay = (sp.AsOfDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
        let dailyInterestRate = sp.InterestRate |> InterestRate.daily
        let totalInterestCap = sp.InterestCap.TotalCap |> InterestCap.totalCap sp.Principal
        let productFeesTotal = productFeesTotal sp.Principal sp.ProductFees
        let productFeesPercentage = decimal productFeesTotal / decimal sp.Principal |> Percent.fromDecimal
        let dayZeroPayment = mergedPayments |> Array.head
        let ``don't apportion for a refund`` paymentTotal amount = if paymentTotal < 0L<Cent> then 0L<Cent> else amount
        let dayZeroPrincipalPortion = decimal dayZeroPayment.NetEffect / (1m + Percent.toDecimal productFeesPercentage) |> Cent.floor
        let dayZeroPrincipalBalance = sp.Principal - dayZeroPrincipalPortion
        let dayZeroProductFeesPortion = dayZeroPayment.NetEffect - dayZeroPrincipalPortion
        let dayZeroProductFeesBalance = productFeesTotal - dayZeroProductFeesPortion
        let getBalanceStatus principalBalance = if principalBalance = 0L<Cent> then Settled elif principalBalance < 0L<Cent> then RefundDue else OpenBalance
        let advance = {
            OffsetDate = sp.StartDate
            OffsetDay = 0<OffsetDay>
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
            ProductFeesBalance = dayZeroProductFeesBalance
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
            PenaltyCharges = [||]
        }
        mergedPayments
        |> Array.tail
        |> Array.scan(fun a ap ->
            let newPenaltyCharges = penaltyChargesTotal ap.PenaltyCharges
            let penaltyChargesPortion = newPenaltyCharges + a.PenaltyChargesBalance |> ``don't apportion for a refund`` ap.NetEffect

            let interestChargeableDays = ScheduledPayment.interestChargeableDays sp.StartDate earlySettlementDate sp.InterestGracePeriod sp.InterestHolidays a.OffsetDay ap.AppliedPaymentDay

            let newInterest =
                if a.PrincipalBalance <= 0L<Cent> then
                    0L<Cent>
                else
                    let dailyInterestCap = sp.InterestCap.DailyCap |> InterestCap.dailyCap (a.PrincipalBalance + a.ProductFeesBalance) interestChargeableDays
                    calculateInterest dailyInterestCap (a.PrincipalBalance + a.ProductFeesBalance) dailyInterestRate interestChargeableDays
            let newInterest' = a.CumulativeInterest + newInterest |> fun i -> if i >= totalInterestCap then totalInterestCap - a.CumulativeInterest else newInterest
            let interestPortion = newInterest' + a.InterestBalance |> ``don't apportion for a refund`` ap.NetEffect
            
            let productFeesDue =
                match sp.ProductFeesSettlement with
                | DueInFull -> productFeesTotal
                | ProRataRefund -> decimal productFeesTotal * decimal ap.AppliedPaymentDay / decimal originalFinalPaymentDay |> Cent.floor
            let productFeesRemaining = if ap.AppliedPaymentDay > originalFinalPaymentDay then 0L<Cent> else Cent.max 0L<Cent> (productFeesTotal - productFeesDue)

            let settlementFigure = a.PrincipalBalance + a.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = ap.NetEffect = settlementFigure
            let isOverpayment = ap.NetEffect > settlementFigure && ap.AppliedPaymentDay <= asOfDay
            let isProjection = ap.NetEffect > settlementFigure && ap.AppliedPaymentDay > asOfDay

            let productFeesRefund =
                match sp.ProductFeesSettlement with
                | DueInFull -> 0L<Cent>
                | ProRataRefund -> if isProjection || isOverpayment || isSettlement then productFeesRemaining else 0L<Cent>

            let actualPayment = abs ap.NetEffect
            let sign: int64<Cent> -> int64<Cent> = if ap.NetEffect < 0L<Cent> then (( * ) -1L) else id

            let principalPortion, productFeesPortion =
                if (penaltyChargesPortion > actualPayment) || (penaltyChargesPortion + interestPortion > actualPayment) then
                    0L<Cent>, 0L<Cent>
                else
                    if isProjection || isOverpayment || isSettlement then
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

            let finalPaymentAdjustment = if isProjection || isOverpayment && (ap.ScheduledPayment > abs principalBalance) then principalBalance else 0L<Cent>

            {
                OffsetDate = sp.StartDate.AddDays(float ap.AppliedPaymentDay)
                OffsetDay = ap.AppliedPaymentDay
                Advance = 0L<Cent>
                ScheduledPayment = ap.ScheduledPayment + finalPaymentAdjustment
                ActualPayments = ap.ActualPayments
                NetEffect = ap.NetEffect + finalPaymentAdjustment
                PaymentStatus = if a.BalanceStatus = Settled then ValueNone elif isOverpayment && finalPaymentAdjustment = 0L<Cent> then ValueSome Overpayment else ap.PaymentStatus
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
                PenaltyCharges = a.PenaltyCharges
            }
        ) advance
        |> Array.takeWhile(fun a -> a.OffsetDay = 0<OffsetDay> || (a.OffsetDay > 0<OffsetDay> && a.PaymentStatus.IsSome))

    let generateAmortisationSchedule (sp: ScheduledPayment.ScheduleParameters) earlySettlementDate (actualPayments: Payment array) =
        voption {
            let! schedule = ScheduledPayment.calculateSchedule sp
            let items =
                schedule
                |> _.Items
                |> applyPayments schedule.AsOfDay 1000L<Cent> actualPayments
                |> calculateSchedule sp earlySettlementDate schedule.FinalPaymentDay
            return {
                Items = items
                FinalScheduledPaymentCount = items |> Array.filter(fun asi -> asi.ScheduledPayment > 0L<Cent>) |> Array.length
                FinalActualPaymentCount = items |> Array.sumBy(fun asi -> Array.length asi.ActualPayments)
                FinalApr =
                    items
                    |> Array.filter(fun asi -> asi.NetEffect > 0L<Cent>)
                    |> Array.map(fun asi -> { Apr.TransferType = Apr.Payment; Apr.Date = sp.StartDate.AddDays(float asi.OffsetDay); Apr.Amount = asi.NetEffect })
                    |> Apr.calculate sp.AprCalculationMethod 8 sp.Principal sp.StartDate

            }
        }

    let allPaidOnTime (scheduleItems: ScheduledPayment.ScheduleItem array) =
        scheduleItems
        |> Array.map(fun si -> { PaymentDay = si.Day; ScheduledPayment = 0L<Cent>; ActualPayments = [| si.Payment |]; PenaltyCharges = [||] })
