namespace FSharp.Finance.Personal

open System

/// functions for settling outstanding payments
module Settlement =

    open ActualPayment

    [<Struct>]
    type QuoteType =
        | Settlement
        | NextScheduled
        | AllOverdue

    [<Struct>]
    type Quote = {
        QuoteType: QuoteType
        PaymentAmount: int64<Cent>
        CurrentSchedule: AmortisationSchedule
        WhatIfSchedule: AmortisationSchedule
    }

    let getSettlement (settlementDate: DateTime) (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone false actualPayments
            let newPaymentAmount = sp.Principal * 10L
            let newPayment = {
                PaymentDay = int (settlementDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                Charges = [||]
            }
            let! amortisationSchedule = generateAmortisationSchedule sp (ValueSome settlementDate) false (Array.concat [| actualPayments; [| newPayment |] |])
            let finalItem = amortisationSchedule.Items |> Array.filter(fun a -> a.OffsetDate <= settlementDate) |> Array.last
            let actualPaymentAmounts = finalItem.ActualPayments |> Array.filter(fun p -> p <> newPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementPaymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { newPayment with ActualPayments = [| settlementPaymentAmount |] }
            let! whatIfAmortisationSchedule = generateAmortisationSchedule sp (ValueSome settlementDate) false (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { QuoteType = Settlement; PaymentAmount = settlementPaymentAmount; CurrentSchedule = currentAmortisationSchedule; WhatIfSchedule = whatIfAmortisationSchedule }
        }

    let getNextScheduled asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone false actualPayments
            let! item =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.OffsetDate >= asOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let newPayment = {
                PaymentDay = item.OffsetDay
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| item.ScheduledPayment |]
                Charges = [||]
            }
            let! whatIfAmortisationSchedule = generateAmortisationSchedule sp ValueNone false (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = NextScheduled; PaymentAmount = item.ScheduledPayment; CurrentSchedule = currentAmortisationSchedule; WhatIfSchedule = whatIfAmortisationSchedule }
        }

    let getAllOverdue asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone false actualPayments
            let missedPayments =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.PaymentStatus = ValueSome MissedPayment)
                |> Array.sumBy _.ScheduledPayment
            let interestAndCharges =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.OffsetDate <= asOfDate)
                |> Array.last
                |> fun a -> a.InterestBalance + a.ChargesBalance
            let newPaymentAmount = missedPayments + interestAndCharges
            let newPayment = {
                PaymentDay = int (asOfDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                Charges = [||]
            }
            let! whatIfAmortisationSchedule = generateAmortisationSchedule sp ValueNone false (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = AllOverdue; PaymentAmount = newPaymentAmount; CurrentSchedule = currentAmortisationSchedule; WhatIfSchedule = whatIfAmortisationSchedule }
        }
