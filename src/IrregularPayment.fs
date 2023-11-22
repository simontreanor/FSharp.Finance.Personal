namespace FSharp.Finance.Personal

open System

/// functions for handling received payments and calculating interest and/or penalty charges where necessary
module IrregularPayment =

    [<Struct>]
    type PaymentStatus =
        | Overpayment
        | PaymentMade
        | RefundMade
        | Underpayment
        | NotYetDue
        | PaidInFull
        | OpenBalance
        | Superfluous

    [<Struct>]
    type Adjustment =
        | ScheduledPayment of ScheduledPayment:int<Cent>
        | ActualPayment of ManualPayment:int<Cent>

    /// a real payment
    [<Struct>]
    type Payment = {
        Day: int<Day>
        Adjustments: Adjustment array
        PaymentStatus: PaymentStatus voption
        PenaltyCharges: PenaltyCharge array
    }
 
    /// detail of a payment with apportionment to principal, product fees, interest and penalty charges
    [<Struct>]
    type Apportionment = {
        Date: DateTime
        TermDay: int<Day>
        Advance: int<Cent>
        Adjustments: Adjustment array
        Payments: int<Cent> array
        PaymentStatus: PaymentStatus voption
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
        FinalApr: decimal<Percent>
    }

    [<Struct>]
    type ScheduleParameters = {
        RegularPaymentScheduleParameters: RegularPayment.ScheduleParameters
        RegularPaymentSchedule: RegularPayment.Schedule
        Payments: Payment array
        InterestCap: InterestCap
    }

    let scheduled = Array.choose(function ScheduledPayment a -> Some a | _ -> None)

    /// merges scheduled and actual payments by date, adds a payment status and a late payment penalty charge if underpaid
    let mergePayments asOfDay latePaymentPenaltyCharge scheduledPayments actualPayments =
        let toMap = Array.map(fun p -> (p.Day, p)) >> Map.ofArray
        let finalDayOf = Array.maxBy _.Day >> _.Day
        let actual = Array.choose(function ActualPayment a -> Some a | _ -> None)
        let netEffectOf = Array.sumBy(function ScheduledPayment a -> -a | ActualPayment a -> a)
        scheduledPayments
        |> toMap
        |> Map.fold(fun s (d: int<Day>) scheduledPayment ->
            match s |> Map.tryFind d with
            | Some actualPayment -> s |> Map.add d { scheduledPayment with Adjustments = [| scheduledPayment.Adjustments; actualPayment.Adjustments |] |> Array.concat }
            | None -> s |> Map.add d { scheduledPayment with Adjustments = scheduledPayment.Adjustments }
        ) (actualPayments |> toMap)
        |> Map.map(fun d payment ->
            let paymentStatus, newPenaltyCharges =
                match payment with
                | _ when d = 0<Day> ->
                    ValueNone, [||]
                | _ when d >= asOfDay && d <= finalDayOf scheduledPayments ->
                    ValueSome NotYetDue, [||]
                | _ when d > finalDayOf scheduledPayments && d = finalDayOf actualPayments ->
                    ValueSome OpenBalance, [||]
                | p when netEffectOf p.Adjustments > 0<Cent> ->
                    ValueSome Overpayment, [||]
                | p when netEffectOf p.Adjustments = 0<Cent> ->
                    ValueSome PaymentMade, [||]
                | p when scheduled p.Adjustments |> Array.isEmpty ->
                    ValueSome RefundMade, [||]
                | _ ->
                    ValueSome Underpayment, [| LatePayment latePaymentPenaltyCharge |]
            let penaltyCharges = ([| payment.PenaltyCharges; newPenaltyCharges |] |> Array.concat)
            { Day = d; Adjustments = payment.Adjustments; PaymentStatus = paymentStatus; PenaltyCharges = penaltyCharges }
        )
        |> Map.toArray
        |> Array.map snd
        
    /// calculate amortisation schedule detailing how elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (irsp: ScheduleParameters) =
        let rpsp = irsp.RegularPaymentScheduleParameters
        let dailyInterestRate = rpsp.InterestRate |> dailyInterestRate
        let interestCap = irsp.InterestCap |> calculateInterestCap rpsp.Principal
        let productFeesTotal = productFeesTotal rpsp.Principal rpsp.ProductFees
        let productFeesPercentage = decimal productFeesTotal / decimal rpsp.Principal |> Percent.fromDecimal
        let payments = irsp.Payments
        let maxScheduledPaymentDay = payments |> Array.choose(fun p -> p.Adjustments |> Array.tryPick(function ScheduledPayment _ -> Some p.Day | _ -> None)) |> Array.max
        let maxActualPaymentDay = payments |> Array.choose(fun p -> p.Adjustments |> Array.tryPick(function ActualPayment _ -> Some p.Day | _ -> None)) |> Array.max
        let dayZeroPayment = payments |> Array.head
        let advance = {
            Date = rpsp.StartDate
            TermDay = 0<Day>
            Advance = rpsp.Principal
            Adjustments = dayZeroPayment.Adjustments 
            Payments = dayZeroPayment.Adjustments |> Array.choose(function ActualPayment a -> Some a | _ -> None)
            PaymentStatus = dayZeroPayment.PaymentStatus
            CumulativeInterest = 0<Cent>
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 0<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = rpsp.Principal
            ProductFeesBalance = productFeesTotal
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }

        let ``don't apportion for a refund`` paymentTotal amount = if paymentTotal < 0<Cent> then 0<Cent> else amount

        payments
        |> Array.tail
        |> Array.scan(fun a p ->
            let paymentTotal = p.Adjustments |> Array.choose(function ActualPayment a -> Some a | ScheduledPayment a when p.PaymentStatus = ValueSome NotYetDue -> Some a | _ -> None) |> Array.sum

            let newPenaltyCharges = penaltyChargesTotal p.PenaltyCharges
            let penaltyChargesPortion = newPenaltyCharges + a.PenaltyChargesBalance |> ``don't apportion for a refund`` paymentTotal

            let newInterest = decimal (a.PrincipalBalance + a.ProductFeesBalance) * Percent.toDecimal dailyInterestRate * decimal (p.Day - a.TermDay) |> Cent.round
            let newInterest' = a.CumulativeInterest + newInterest |> fun i -> if i >= interestCap then interestCap - a.CumulativeInterest else newInterest
            let interestPortion = newInterest' + a.InterestBalance |> ``don't apportion for a refund`` paymentTotal
            
            let productFeesDue = Cent.min productFeesTotal (decimal productFeesTotal * decimal p.Day / decimal maxScheduledPaymentDay |> Cent.round)
            let productFeesRemaining = productFeesTotal - productFeesDue

            let settlementFigure = a.PrincipalBalance + a.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = paymentTotal >= settlementFigure //&& p.Day < maxScheduledPaymentDay) || p.Day = maxScheduledPaymentDay && p.Day >= maxActualPaymentDay

            let productFeesRefund = if isSettlement then productFeesRemaining else 0<Cent>

            let actualPayment, (sign: int<Cent> -> int<Cent>) =
                if a.PrincipalBalance = 0<Cent> then
                    0<Cent>, id
                elif isSettlement then
                    settlementFigure, id
                else
                    abs paymentTotal, if paymentTotal < 0<Cent> then (( * ) -1) else id

            let principalPortion, cabFeePortion =
                if (penaltyChargesPortion > actualPayment) || (penaltyChargesPortion + interestPortion > actualPayment) then
                    0<Cent>, 0<Cent>
                else
                    if isSettlement then
                        let cabFeePayment = a.ProductFeesBalance - productFeesRemaining
                        actualPayment - penaltyChargesPortion - interestPortion - cabFeePayment, cabFeePayment
                    else
                        let principalPayment = decimal (actualPayment - penaltyChargesPortion - interestPortion) / (1m + Percent.toDecimal productFeesPercentage) |> Cent.round
                        principalPayment, actualPayment - penaltyChargesPortion - interestPortion - principalPayment
                        
            let principalPortion = Cent.min a.PrincipalBalance principalPortion
            let productFeesPortion = Cent.min a.ProductFeesBalance cabFeePortion

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualPayment then
                    penaltyChargesPortion - actualPayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualPayment then
                    0<Cent>, interestPortion - (actualPayment - penaltyChargesPortion)
                else
                    0<Cent>, 0<Cent>

            let paymentStatus =
                if a.PaymentStatus = ValueSome PaidInFull then 
                    ValueSome Superfluous
                elif isSettlement then // to-do: figure out how to prevent this status when not yet due
                    ValueSome PaidInFull
                elif p.Day = maxActualPaymentDay && a.PrincipalBalance > 0<Cent> then
                    ValueSome OpenBalance
                else
                    p.PaymentStatus

            {
                Date = rpsp.StartDate.AddDays(float p.Day)
                TermDay = p.Day
                Advance = 0<Cent>
                Adjustments = p.Adjustments
                Payments = if actualPayment <> 0<Cent> then [| actualPayment |] else [||]
                PaymentStatus = paymentStatus
                CumulativeInterest = a.CumulativeInterest + newInterest'
                NewInterest = newInterest'
                NewPenaltyCharges = newPenaltyCharges
                PrincipalPortion = sign principalPortion
                ProductFeesPortion = sign productFeesPortion
                InterestPortion = interestPortion - carriedInterest
                PenaltyChargesPortion = penaltyChargesPortion - carriedPenaltyCharges
                ProductFeesRefund = productFeesRefund
                PrincipalBalance = a.PrincipalBalance - sign principalPortion
                ProductFeesBalance = a.ProductFeesBalance - sign productFeesPortion - productFeesRefund
                InterestBalance = a.InterestBalance + newInterest' - interestPortion + carriedInterest
                PenaltyChargesBalance = a.PenaltyChargesBalance + newPenaltyCharges - penaltyChargesPortion + carriedPenaltyCharges
            }
        ) advance
        |> Array.takeWhile(fun a -> a.PaymentStatus <> ValueSome Superfluous)
