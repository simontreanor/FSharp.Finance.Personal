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
        CurrentAmortisationSchedule: AmortisationSchedule
        WhatIfAmortisationSchedule: AmortisationSchedule
    }

    let getSettlement (settlementDate: DateTime) (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone actualPayments
            let newPaymentAmount = sp.Principal * 10L
            let newPayment = {
                PaymentDay = int (settlementDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                PenaltyCharges = [||]
            }
            let! amortisationSchedule = generateAmortisationSchedule sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| newPayment |] |])
            let finalItem = amortisationSchedule.Items |> Array.filter(fun a -> a.OffsetDate <= settlementDate) |> Array.last
            let actualPaymentAmounts = finalItem.ActualPayments |> Array.filter(fun p -> p <> newPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementPaymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { newPayment with ActualPayments = [| settlementPaymentAmount |] }
            let! whatIfAmortisationSchedule = generateAmortisationSchedule sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { QuoteType = Settlement; PaymentAmount = settlementPaymentAmount; CurrentAmortisationSchedule = currentAmortisationSchedule; WhatIfAmortisationSchedule = whatIfAmortisationSchedule }
        }

    let getNextScheduled asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone actualPayments
            let! item =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.OffsetDate >= asOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let newPayment = {
                PaymentDay = item.OffsetDay
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| item.ScheduledPayment |]
                PenaltyCharges = [||]
            }
            let! whatIfAmortisationSchedule = generateAmortisationSchedule sp ValueNone (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = NextScheduled; PaymentAmount = item.ScheduledPayment; CurrentAmortisationSchedule = currentAmortisationSchedule; WhatIfAmortisationSchedule = whatIfAmortisationSchedule }
        }

    let getAllOverdue asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone actualPayments
            let missedPayments =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.PaymentStatus = ValueSome MissedPayment)
                |> Array.sumBy _.ScheduledPayment
            let interestAndPenaltyCharges =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.OffsetDate <= asOfDate)
                |> Array.last
                |> fun a -> a.InterestBalance + a.PenaltyChargesBalance
            let newPaymentAmount = missedPayments + interestAndPenaltyCharges
            let newPayment = {
                PaymentDay = int (asOfDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                PenaltyCharges = [||]
            }
            let! whatIfAmortisationSchedule = generateAmortisationSchedule sp ValueNone (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = AllOverdue; PaymentAmount = newPaymentAmount; CurrentAmortisationSchedule = currentAmortisationSchedule; WhatIfAmortisationSchedule = whatIfAmortisationSchedule }
        }
