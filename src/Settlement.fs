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
        CurrentStatement: AmortisationScheduleItem array
        WhatIfStatement: AmortisationScheduleItem array
    }

    let getSettlement (settlementDate: DateTime) (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentStatement = applyPayments sp ValueNone actualPayments
            let newPaymentAmount = sp.Principal * 10L
            let newPayment = {
                Day = int (settlementDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                NetEffect = 0L<Cent>
                PaymentStatus = ValueNone
                PenaltyCharges = [| |]
            }
            let! appliedPayments = applyPayments sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| newPayment |] |])
            let finalItem = appliedPayments |> Array.filter(fun a -> a.OffsetDate <= settlementDate) |> Array.last
            let actualPaymentAmounts = finalItem.ActualPayments |> Array.filter(fun p -> p <> newPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementPaymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { newPayment with ActualPayments = [| settlementPaymentAmount |] }
            let! statement = applyPayments sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { QuoteType = Settlement; PaymentAmount = settlementPaymentAmount; CurrentStatement = currentStatement; WhatIfStatement = statement }
        }

    let getNextScheduled asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentStatement = applyPayments sp ValueNone actualPayments
            let! item =
                currentStatement
                |> Array.filter(fun a -> a.OffsetDate >= asOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let newPayment = {
                Day = item.OffsetDay
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| item.ScheduledPayment |]
                NetEffect = 0L<Cent>
                PaymentStatus = ValueNone
                PenaltyCharges = [| |]
            }
            let! statement = applyPayments sp ValueNone (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = NextScheduled; PaymentAmount = item.ScheduledPayment; CurrentStatement = currentStatement; WhatIfStatement = statement }
        }

    let getAllOverdue asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentStatement = applyPayments sp ValueNone actualPayments
            let missedPayments =
                currentStatement
                |> Array.filter(fun a -> a.PaymentStatus = ValueSome MissedPayment)
                |> Array.sumBy _.ScheduledPayment
            let interestAndPenaltyCharges =
                currentStatement
                |> Array.filter(fun a -> a.OffsetDate <= asOfDate)
                |> Array.last
                |> fun a -> a.InterestBalance + a.PenaltyChargesBalance
            let newPaymentAmount = missedPayments + interestAndPenaltyCharges
            let newPayment = {
                Day = int (asOfDate.Date - sp.StartDate.Date).Days * 1<OffsetDay>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                NetEffect = 0L<Cent>
                PaymentStatus = ValueNone
                PenaltyCharges = [| |]
            }
            let! statement = applyPayments sp ValueNone (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = AllOverdue; PaymentAmount = newPaymentAmount; CurrentStatement = currentStatement; WhatIfStatement = statement }
        }
