namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open System
    open CustomerPayments

    [<Struct>]
    type QuoteResult =
        | PaymentQuote of PaymentQuote: int64<Cent> * OfWhichPrincipal: int64<Cent> * OfWhichFees: int64<Cent> * OfWhichInterest: int64<Cent> * OfWhichCharges: int64<Cent> * ProRatedFees: int64<Cent>
        | AwaitPaymentConfirmation
        | UnableToGenerateQuote

    /// a settlement quote
    [<Struct>]
    type Quote = {
        QuoteType: QuoteType
        QuoteResult: QuoteResult
        CurrentSchedule: Amortisation.Schedule
        RevisedSchedule: Amortisation.Schedule
    }

    /// <summary>calculates a revised schedule showing the generated payment for the given quote type</summary>
    /// <param name="quoteType">the type of quote</param>
    /// <param name="sp">the parameters for creating the schedule</param>
    /// <param name="actualPayments">an array of the actual payments</param>
    /// <returns>the requested quote, if possible</returns>
    let getQuote quoteType sp (actualPayments: CustomerPayment array) =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduledPaymentType.Original actualPayments
            let! revisedAmortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote quoteType) ScheduledPaymentType.Original actualPayments
            let! si = revisedAmortisationSchedule.ScheduleItems |> Array.tryFind(_.GeneratedPayment.IsSome) |> toValueOption
            let confirmedPayments = si.ActualPayments |> Array.sumBy(function ActualPaymentStatus.Confirmed ap -> ap | _ -> 0L<Cent>)
            let pendingPayments = si.ActualPayments |> Array.sumBy(function ActualPaymentStatus.Pending ap -> ap | _ -> 0L<Cent>)
            let quoteResult =
                if si.GeneratedPayment.IsNone then
                    UnableToGenerateQuote
                elif pendingPayments <> 0L<Cent> then 
                    AwaitPaymentConfirmation
                else
                    let principalPortion, feesPortion, interestPortion, chargesPortion, proRatedFees =
                        if si.GeneratedPayment.Value = 0L<Cent> then
                            0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, si.ProRatedFees
                        else
                            if confirmedPayments <> 0L<Cent> then
                                if si.GeneratedPayment.Value > 0L<Cent> then
                                    let chargesPortion = Cent.min si.ChargesPortion confirmedPayments
                                    let interestPortion = Cent.min si.InterestPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion))
                                    let feesPortion = Cent.min si.FeesPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion - interestPortion))
                                    let principalPortion = Cent.max 0L<Cent> (confirmedPayments - feesPortion - chargesPortion - interestPortion)
                                    si.PrincipalPortion - principalPortion, si.FeesPortion - feesPortion, si.InterestPortion - interestPortion, si.ChargesPortion - chargesPortion, si.ProRatedFees
                                else
                                    si.GeneratedPayment.Value, 0L<Cent>, 0L<Cent>, 0L<Cent>, si.ProRatedFees
                            else
                                si.PrincipalPortion, si.FeesPortion, si.InterestPortion, si.ChargesPortion, si.ProRatedFees
                    PaymentQuote (si.GeneratedPayment.Value, principalPortion, feesPortion, interestPortion, chargesPortion, proRatedFees)
            return {
                QuoteType = quoteType
                QuoteResult = quoteResult
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }
