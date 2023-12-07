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
        CurrentStatement: Apportionment array
        WhatIfStatement: Apportionment array
    }

    let getSettlement (settlementDate: DateTime) (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentStatement = applyPayments sp ValueNone actualPayments
            let newPaymentAmount = sp.Principal * 10L
            let newPayment = {
                Day = int (settlementDate.Date - sp.StartDate.Date).Days * 1<Day>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                NetEffect = 0L<Cent>
                PaymentStatus = ValueNone
                PenaltyCharges = [| |]
            }
            let! appliedPayments = applyPayments sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| newPayment |] |])
            let finalApportionment = appliedPayments |> Array.filter(fun a -> a.Date <= settlementDate) |> Array.last
            let actualPaymentAmounts = finalApportionment.ActualPayments |> Array.filter(fun p -> p <> newPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementPaymentAmount = finalApportionment.NetEffect + finalApportionment.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { newPayment with ActualPayments = [| settlementPaymentAmount |] }
            let! statement = applyPayments sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { QuoteType = Settlement; PaymentAmount = settlementPaymentAmount; CurrentStatement = currentStatement; WhatIfStatement = statement }
        }

    let getNextScheduled asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentStatement = applyPayments sp ValueNone actualPayments
            let! apportionment =
                currentStatement
                |> Array.filter(fun a -> a.Date >= asOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let newPayment = {
                Day = apportionment.TermDay
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| apportionment.ScheduledPayment |]
                NetEffect = 0L<Cent>
                PaymentStatus = ValueNone
                PenaltyCharges = [| |]
            }
            let! statement = applyPayments sp ValueNone (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = NextScheduled; PaymentAmount = apportionment.ScheduledPayment; CurrentStatement = currentStatement; WhatIfStatement = statement }
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
                |> Array.filter(fun a -> a.Date <= asOfDate)
                |> Array.last
                |> fun a -> a.InterestBalance + a.PenaltyChargesBalance
            let newPaymentAmount = missedPayments + interestAndPenaltyCharges
            let newPayment = {
                Day = int (asOfDate.Date - sp.StartDate.Date).Days * 1<Day>
                ScheduledPayment = 0L<Cent>
                ActualPayments = [| newPaymentAmount |]
                NetEffect = 0L<Cent>
                PaymentStatus = ValueNone
                PenaltyCharges = [| |]
            }
            let! statement = applyPayments sp ValueNone (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = AllOverdue; PaymentAmount = newPaymentAmount; CurrentStatement = currentStatement; WhatIfStatement = statement }
        }
