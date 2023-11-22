namespace FSharp.Finance.Personal

open System

/// functions for handling received payments and calculating interest and/or penalty charges where necessary
module IrregularPayment =

    [<Struct>]
    type PaymentStatus =
        | Overpayment
        | PaidInFull
        | Underpayment
        | NotYetDue

    /// a real payment
    [<Struct>]
    type Payment = {
        Day: int<Day>
        Amounts: int<Cent> array
        PaymentStatus: PaymentStatus voption
        PenaltyCharges: PenaltyCharge array
    }
 
    /// detail of a payment with apportionment to principal, product fees, interest and penalty charges
    [<Struct>]
    type Apportionment = {
        Date: DateTime
        Day: int<Day>
        Advance: int<Cent>
        Payments: int<Cent> array
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
        RegularPaymentSchedule: RegularPayment.Schedule
        Payments: Payment array
    }

    /// merges scheduled and actual payments by date, adds a payment status and a late payment penalty charge if underpaid
    let mergePayments asOfDay latePaymentPenaltyCharge scheduledPayments actualPayments =
        let toMap = Array.map(fun (p: Payment) -> (p.Day, p)) >> Map.ofArray
        scheduledPayments
        |> toMap
        |> Map.fold(fun s (d: int<Day>) scheduledPayment ->
            let negate = Array.map(fun (amount: int<Cent>) -> amount * 1)
            match s |> Map.tryFind d with
            | Some actualPayment -> s |> Map.add d { scheduledPayment with Amounts = [| negate scheduledPayment.Amounts; actualPayment.Amounts |] |> Array.concat }
            | None -> s |> Map.add d { scheduledPayment with Amounts = negate scheduledPayment.Amounts }
        ) (actualPayments |> toMap)
        |> Map.map(fun d payment ->
            let paymentStatus =
                match payment with
                | _ when d >= asOfDay -> NotYetDue
                | p when p.Amounts |> Array.sum > 0<Cent> -> Overpayment
                | p when p.Amounts |> Array.sum = 0<Cent> -> PaidInFull
                | _ -> Underpayment
            let penaltyCharges = ([| payment.PenaltyCharges; [| LatePayment latePaymentPenaltyCharge |] |] |> Array.concat)
            { Day = d; Amounts = payment.Amounts |> Array.filter(fun a -> a > 0<Cent>); PaymentStatus = ValueSome paymentStatus; PenaltyCharges = penaltyCharges }
        )
        |> Map.toArray
        |> Array.map snd
        
    /// calculate amortisation schedule detailing how elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (sp: RegularPayment.ScheduleParameters) (payments: Payment array) =
        let dailyInterestRate = sp.InterestRate |> dailyInterestRate
        let productFeesTotal = productFeesTotal sp.Principal sp.ProductFees
        let productFeesPercentage = decimal productFeesTotal / decimal sp.Principal |> Percent.fromDecimal
        let maxPaymentDay = payments |> Array.maxBy _.Day |> _.Day
        let advance = {
            Date = sp.StartDate
            Day = 0<Day>
            Advance = sp.Principal
            Payments = [||]
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 0<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = sp.Principal
            ProductFeesBalance = productFeesTotal
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        payments
        |> Array.scan(fun a p ->
            let newPenaltyCharges = penaltyChargesTotal p.PenaltyCharges
            let penaltyChargesPortion = newPenaltyCharges + a.PenaltyChargesBalance

            let newInterest = decimal (a.PrincipalBalance + a.ProductFeesBalance) * Percent.toDecimal dailyInterestRate * decimal (p.Day - a.Day) |> Cent.round
            let interestPortion = newInterest + a.InterestBalance
            
            let productFeesDue = Cent.min productFeesTotal (decimal productFeesTotal * decimal p.Day / decimal maxPaymentDay |> Cent.round)
            let productFeesRemaining = productFeesTotal - productFeesDue
            let settlementFigure = a.PrincipalBalance + a.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let totalAmount = p.Amounts |> Array.sum
            let isSettlement = (totalAmount > settlementFigure && p.Day <> maxPaymentDay) || p.Day = maxPaymentDay

            let actualPayment =
                if a.PrincipalBalance = 0<Cent> then
                    0<Cent>
                elif isSettlement then
                    settlementFigure
                else
                    totalAmount

            let principalPortion, cabFeePortion, productFeesRefund =
                if (penaltyChargesPortion > actualPayment) || (penaltyChargesPortion + interestPortion > actualPayment) then
                    0<Cent>, 0<Cent>, 0<Cent>
                else
                    if isSettlement then
                        let cabFeePayment = a.ProductFeesBalance - productFeesRemaining
                        actualPayment - penaltyChargesPortion - interestPortion - cabFeePayment, cabFeePayment, productFeesRemaining
                    else
                        let principalPayment = decimal (actualPayment - penaltyChargesPortion - interestPortion) / decimal (100m<Percent> + productFeesPercentage) |> Cent.round
                        principalPayment, actualPayment - penaltyChargesPortion - interestPortion - principalPayment, 0<Cent>
                        
            let principalPortion = Cent.min a.PrincipalBalance principalPortion
            let productFeesPortion = Cent.min a.ProductFeesBalance cabFeePortion

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualPayment then
                    penaltyChargesPortion - actualPayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualPayment then
                    0<Cent>, interestPortion - (actualPayment - penaltyChargesPortion)
                else
                    0<Cent>, 0<Cent>

            {
                Date = sp.StartDate.AddDays(float p.Day)
                Day = p.Day
                Advance = 0<Cent>
                Payments = if actualPayment > 0<Cent> then [| actualPayment |] else [||]
                NewInterest = newInterest
                NewPenaltyCharges = newPenaltyCharges
                PrincipalPortion = principalPortion
                ProductFeesPortion = productFeesPortion
                InterestPortion = interestPortion - carriedInterest
                PenaltyChargesPortion = penaltyChargesPortion - carriedPenaltyCharges
                ProductFeesRefund = productFeesRefund
                PrincipalBalance = a.PrincipalBalance - principalPortion
                ProductFeesBalance = a.ProductFeesBalance - productFeesPortion - productFeesRefund
                InterestBalance = a.InterestBalance + newInterest - interestPortion + carriedInterest
                PenaltyChargesBalance = a.PenaltyChargesBalance + newPenaltyCharges - penaltyChargesPortion + carriedPenaltyCharges
            }
        ) advance
