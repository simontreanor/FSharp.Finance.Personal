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
        CurrentSchedule: Amortisation.Schedule
        RevisedSchedule: Amortisation.Schedule
    }

    /// calculates the single final payment required to settle in full
    let getSettlement sp applyNegativeInterest (actualPayments: CustomerPayment array) =
        voption {
            let sp' = { sp with Parameters.FeesAndCharges.LatePaymentGracePeriod = 0<DurationDay> }
            let! currentAmortisationSchedule = Amortisation.generate sp' ValueNone false applyNegativeInterest actualPayments
            let newPaymentAmount = sp'.Principal * 10L
            let newPayment = {
                PaymentDay = int (sp.AsOfDate - sp'.StartDate).Days * 1<OffsetDay>
                PaymentDetails = ActualPayment (newPaymentAmount, [||])
            }
            let! amortisationSchedule = Amortisation.generate sp' (ValueSome sp.AsOfDate) false applyNegativeInterest (Array.concat [| actualPayments; [| newPayment |] |])
            let finalItem = amortisationSchedule.ScheduleItems |> Array.filter(fun a -> a.OffsetDate <= sp.AsOfDate) |> Array.last
            let actualPaymentAmounts = finalItem.ActualPayments |> Array.filter(fun p -> p <> newPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementPaymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { newPayment with PaymentDetails = ActualPayment (settlementPaymentAmount, [||]) }
            let! revisedAmortisationSchedule = Amortisation.generate sp' (ValueSome sp.AsOfDate) false applyNegativeInterest (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { QuoteType = Settlement; PaymentAmount = settlementPaymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }

    /// calculates the next scheduled payment
    let getNextScheduled (sp: PaymentSchedule.Parameters) (actualPayments: CustomerPayment array) =
        voption {
            let sp' = { sp with Parameters.FeesAndCharges.LatePaymentGracePeriod = 0<DurationDay> }
            let! currentAmortisationSchedule = Amortisation.generate sp' ValueNone false false actualPayments
            let! item =
                currentAmortisationSchedule.ScheduleItems
                |> Array.filter(fun a -> a.OffsetDate >= sp.AsOfDate)
                |> Array.tryFind(fun a -> a.ScheduledPayment > 0L<Cent>)
                |> function Some a -> ValueSome a | _ -> ValueNone
            let newPayment = {
                PaymentDay = item.OffsetDay
                PaymentDetails = ActualPayment (item.ScheduledPayment, [||])
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp' ValueNone false false (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = NextScheduled; PaymentAmount = item.ScheduledPayment; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }

    /// calculates the total of all overdue payments
    let getAllOverdue sp actualPayments =
        voption {
            let sp' = { sp with Parameters.FeesAndCharges.LatePaymentGracePeriod = 0<DurationDay> }
            let! currentAmortisationSchedule = Amortisation.generate sp' ValueNone false false actualPayments
            let missedPayments =
                currentAmortisationSchedule.ScheduleItems
                |> Array.filter(fun a -> a.PaymentStatus = ValueSome MissedPayment)
                |> Array.sumBy _.ScheduledPayment
            let interestAndCharges =
                currentAmortisationSchedule.ScheduleItems
                |> Array.filter(fun a -> a.OffsetDate <= sp.AsOfDate)
                |> Array.last
                |> fun a -> a.InterestBalance + a.ChargesBalance
            let newPaymentAmount = missedPayments + interestAndCharges
            let newPayment = {
                PaymentDay = int (sp.AsOfDate - sp'.StartDate).Days * 1<OffsetDay>
                PaymentDetails = ActualPayment (newPaymentAmount, [||])
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp' ValueNone false false (Array.concat [| actualPayments; [| newPayment |] |])
            return { QuoteType = AllOverdue; PaymentAmount = newPaymentAmount; CurrentSchedule = currentAmortisationSchedule; RevisedSchedule = revisedAmortisationSchedule }
        }
