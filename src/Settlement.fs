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
        RevisedSchedule: AmortisationSchedule
    }

    let getSettlement (settlementDate: Date) (sp: ScheduledPayment.ScheduleParameters) applyNegativeInterest (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone false applyNegativeInterest actualPayments
            let newPaymentAmount = sp.Principal * 10L
            let newPayment = {
                PaymentDay = int (settlementDate - sp.StartDate).Days * 1<OffsetDay>
                PaymentDetails = ActualPayments ([| newPaymentAmount |], [||])
            }
            let! amortisationSchedule = generateAmortisationSchedule sp (ValueSome settlementDate) false applyNegativeInterest (Array.concat [| actualPayments; [| newPayment |] |])
            let finalItem = amortisationSchedule.Items |> Array.filter(fun a -> a.OffsetDate <= settlementDate) |> Array.last
            let actualPaymentAmounts = finalItem.ActualPayments |> Array.filter(fun p -> p <> newPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementPaymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { newPayment with PaymentDetails = ActualPayments ([| settlementPaymentAmount |], [||]) }
            let! revisedAmortisationSchedule = generateAmortisationSchedule sp (ValueSome settlementDate) false applyNegativeInterest (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { QuoteType = Settlement; PaymentAmount = settlementPaymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }

    let getNextScheduled asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone false false actualPayments
            let! item =
                currentAmortisationSchedule.Items
                |> Array.filter(fun a -> a.OffsetDate >= asOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let newPayment = {
                PaymentDay = item.OffsetDay
                PaymentDetails = ActualPayments ([| item.ScheduledPayment |], [||])
            }
            let! revisedAmortisationSchedule = generateAmortisationSchedule sp ValueNone false false (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = NextScheduled; PaymentAmount = item.ScheduledPayment; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }

    let getAllOverdue asOfDate (sp: ScheduledPayment.ScheduleParameters) (actualPayments: Payment array) =
        voption {
            let! currentAmortisationSchedule = generateAmortisationSchedule sp ValueNone false false actualPayments
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
                PaymentDay = int (asOfDate - sp.StartDate).Days * 1<OffsetDay>
                PaymentDetails = ActualPayments ([| newPaymentAmount |], [||])
            }
            let! revisedAmortisationSchedule = generateAmortisationSchedule sp ValueNone false false (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = AllOverdue; PaymentAmount = newPaymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }
