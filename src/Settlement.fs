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
        OfWhichPrincipal: int64<Cent>
        OfWhichInterest: int64<Cent>
        CurrentSchedule: Amortisation.Schedule
        RevisedSchedule: Amortisation.Schedule
    }

    /// calculates the single final payment required to settle in full
    let getSettlement sp negativeInterestOption (actualPayments: CustomerPayment array) =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement DoNotCalculateFinalApr negativeInterestOption ValueNone actualPayments
            let paymentDay = int (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
            let generatedPayment = {
                PaymentDay = paymentDay
                PaymentDetails = GeneratedPayment (sp.Principal * 10L, SettlementPayment)
            }
            let! amortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote (ValueSome sp.AsOfDate)) DoNotCalculateFinalApr negativeInterestOption (ValueSome generatedPayment) actualPayments
            let finalItem = amortisationSchedule.ScheduleItems |> Array.filter(fun a -> a.OffsetDate <= sp.AsOfDate) |> Array.last
            let actualPaymentsTotal = finalItem.ActualPayments |> Array.sum
            let paymentAmount = finalItem.NetEffect + finalItem.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = {
                generatedPayment with PaymentDetails = GeneratedPayment(paymentAmount, SettlementPayment)
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote (ValueSome sp.AsOfDate)) DoNotCalculateFinalApr negativeInterestOption (ValueSome settlementPayment) actualPayments
            let finalRevisedItem = revisedAmortisationSchedule.ScheduleItems |> Array.last
            let ofWhichPrincipal, ofWhichInterest =
                if paymentAmount <= 0L<Cent> && finalRevisedItem.PrincipalPortion > 0L<Cent> then // when an overpayment/settlement occur on the same day this quote is for
                    0L<Cent>, 0L<Cent>
                else
                    finalRevisedItem.PrincipalPortion, finalRevisedItem.InterestPortion
            return {
                QuoteType = Settlement
                PaymentAmount = paymentAmount
                OfWhichPrincipal = ofWhichPrincipal
                OfWhichInterest = ofWhichInterest
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }

    /// calculates the next scheduled payment
    let getNextScheduled (sp: PaymentSchedule.Parameters) (actualPayments: CustomerPayment array) =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement DoNotCalculateFinalApr DoNotApplyNegativeInterest ValueNone actualPayments
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
            let! revisedAmortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote ValueNone) DoNotCalculateFinalApr DoNotApplyNegativeInterest (ValueSome generatedPayment) actualPayments
            let finalRevisedItem = revisedAmortisationSchedule.ScheduleItems |> Array.last
            return {
                QuoteType = NextScheduled
                PaymentAmount = paymentAmount
                OfWhichPrincipal = finalRevisedItem.PrincipalPortion
                OfWhichInterest = finalRevisedItem.InterestPortion
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }

    /// calculates the total of all overdue payments
    let getAllOverdue sp actualPayments =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement DoNotCalculateFinalApr DoNotApplyNegativeInterest ValueNone actualPayments
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
                PaymentDay = int (sp.AsOfDate - sp.StartDate).Days * 1<OffsetDay>
                PaymentDetails = GeneratedPayment(paymentAmount, AllOverduePayments)
            }
            let! revisedAmortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote ValueNone) DoNotCalculateFinalApr DoNotApplyNegativeInterest (ValueSome generatedPayment) actualPayments
            let finalRevisedItem = revisedAmortisationSchedule.ScheduleItems |> Array.last
            return {
                QuoteType = AllOverdue
                PaymentAmount = paymentAmount
                OfWhichPrincipal = finalRevisedItem.PrincipalPortion
                OfWhichInterest = finalRevisedItem.InterestPortion
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }
