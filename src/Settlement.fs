namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Settlement =

    open CustomerPayments
    open PaymentSchedule

    /// the type of settlement quote requested
    [<Struct>]
    type QuoteType =
        /// calculate the single final payment required to settle in full
        | Settlement
        /// calculate the next scheduled payment
        | NextScheduled
        /// calculate the total of all overdue payments
        | AllOverdue

    /// a settlement quote
    [<Struct>]
    type Quote = {
        QuoteType: QuoteType
        PaymentAmount: int64<Cent>
        // OfWhichPrincipal: int64<Cent>
        // OfWhichInterest: int64<Cent>
        CurrentSchedule: Amortisation.Schedule
        RevisedSchedule: Amortisation.Schedule
    }

    /// calculates the single final payment required to settle in full
    let getSettlement sp applyNegativeInterest (actualPayments: CustomerPayment array) =
        voption {
            let sp' = { sp with Parameters.FeesAndCharges.LatePaymentGracePeriod = 0<DurationDay> }
            let! currentAmortisationSchedule = Amortisation.generate sp' ValueNone false applyNegativeInterest ValueNone actualPayments
            let generatedPayment = {
                PaymentDay = int (sp.AsOfDate - sp'.StartDate).Days * 1<OffsetDay>
                PaymentDetails = GeneratedPayment (sp'.Principal * 10L, SettlementPayment)
            }
            let! amortisationSchedule = Amortisation.generate sp' (ValueSome sp.AsOfDate) false applyNegativeInterest (ValueSome generatedPayment) actualPayments
            let finalItem = amortisationSchedule.ScheduleItems |> Array.filter(fun a -> a.OffsetDate <= sp.AsOfDate) |> Array.last
            let actualPaymentsTotal = finalItem.ActualPayments |> Array.sum
            let paymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = {
                generatedPayment with PaymentDetails = GeneratedPayment(paymentAmount, SettlementPayment)
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp' (ValueSome sp.AsOfDate) false applyNegativeInterest (ValueSome settlementPayment) actualPayments
            return { QuoteType = Settlement; PaymentAmount = paymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }

    /// calculates the next scheduled payment
    let getNextScheduled (sp: PaymentSchedule.Parameters) (actualPayments: CustomerPayment array) =
        voption {
            let sp' = { sp with Parameters.FeesAndCharges.LatePaymentGracePeriod = 0<DurationDay> }
            let! currentAmortisationSchedule = Amortisation.generate sp' ValueNone false false ValueNone actualPayments
            let! item =
                currentAmortisationSchedule.ScheduleItems
                |> Array.filter(fun a -> a.OffsetDate >= sp.AsOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let paymentAmount = item.ScheduledPayment
            let generatedPayment = {
                PaymentDay = item.OffsetDay
                PaymentDetails = GeneratedPayment(paymentAmount, NextScheduledPayment)
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp' ValueNone false false (ValueSome generatedPayment) actualPayments
            return { QuoteType = NextScheduled; PaymentAmount = paymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }

    /// calculates the total of all overdue payments
    let getAllOverdue sp actualPayments =
        voption {
            let sp' = { sp with Parameters.FeesAndCharges.LatePaymentGracePeriod = 0<DurationDay> }
            let! currentAmortisationSchedule = Amortisation.generate sp' ValueNone false false ValueNone actualPayments
            let missedPayments =
                currentAmortisationSchedule.ScheduleItems
                |> Array.filter(fun a -> a.PaymentStatus = ValueSome MissedPayment)
                |> Array.sumBy _.ScheduledPayment
            let interestAndCharges =
                currentAmortisationSchedule.ScheduleItems
                |> Array.filter(fun a -> a.OffsetDate <= sp.AsOfDate)
                |> Array.last
                |> fun a -> a.InterestBalance + a.ChargesBalance
            let paymentAmount = missedPayments + interestAndCharges
            let generatedPayment = {
                PaymentDay = int (sp.AsOfDate - sp'.StartDate).Days * 1<OffsetDay>
                PaymentDetails = GeneratedPayment(paymentAmount, AllOverduePayment)
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp' ValueNone false false (ValueSome generatedPayment) actualPayments
            return { QuoteType = AllOverdue; PaymentAmount = paymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }
