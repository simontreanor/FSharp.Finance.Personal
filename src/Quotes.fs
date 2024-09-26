namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open PaymentSchedule
    open ValueOptionCE

    /// the result of a quote with a breakdown of constituent amounts where relevant
    [<Struct>]
    type QuoteResult =
        | PaymentQuote of PaymentQuote: int64<Cent> * OfWhichPrincipal: int64<Cent> * OfWhichFees: int64<Cent> * OfWhichInterest: int64<Cent> * OfWhichCharges: int64<Cent> * FeesRefundIfSettled: int64<Cent>
        | AwaitPaymentConfirmation
        | UnableToGenerateQuote

    /// a settlement quote
    [<Struct>]
    type Quote = {
        IntendedPurpose: IntendedPurpose
        QuoteResult: QuoteResult
        CurrentSchedule: Amortisation.Schedule
        RevisedSchedule: Amortisation.Schedule
    }

    /// <summary>calculates a revised schedule showing the generated payment for the given quote type</summary>
    /// <param name="settlementDay">the day for which to generate the settlement quote</param>
    /// <param name="sp">the parameters for creating the schedule</param>
    /// <param name="actualPayments">an array of the actual payments</param>
    /// <returns>the requested quote, if possible</returns>
    let getQuote intendedPurpose sp (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
            let! revisedAmortisationSchedule = Amortisation.generate sp intendedPurpose ScheduleType.Original false actualPayments
            let! si = revisedAmortisationSchedule.ScheduleItems |> Map.values |> Seq.tryFind _.GeneratedPayment.IsSome |> toValueOption
            let confirmedPayments = si.ActualPayments |> Array.sumBy(function { ActualPaymentStatus = ActualPaymentStatus.Confirmed ap } -> ap | { ActualPaymentStatus = ActualPaymentStatus.WriteOff ap } -> ap | _ -> 0L<Cent>)
            let pendingPayments = si.ActualPayments |> Array.sumBy(function { ActualPaymentStatus = ActualPaymentStatus.Pending ap } -> ap | _ -> 0L<Cent>)
            let quoteResult =
                if si.GeneratedPayment.IsNone then
                    UnableToGenerateQuote
                elif pendingPayments <> 0L<Cent> then 
                    AwaitPaymentConfirmation
                else
                    let principalPortion, feesPortion, interestPortion, chargesPortion, feesRefundIfSettled =
                        if si.GeneratedPayment.Value = 0L<Cent> then
                            0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, si.FeesRefundIfSettled
                        else
                            if confirmedPayments <> 0L<Cent> then
                                if si.GeneratedPayment.Value > 0L<Cent> then
                                    let chargesPortion = Cent.min si.ChargesPortion confirmedPayments
                                    let interestPortion = Cent.min si.InterestPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion))
                                    let feesPortion = Cent.min si.FeesPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion - interestPortion))
                                    let principalPortion = Cent.max 0L<Cent> (confirmedPayments - feesPortion - chargesPortion - interestPortion)
                                    si.PrincipalPortion - principalPortion, si.FeesPortion - feesPortion, si.InterestPortion - interestPortion, si.ChargesPortion - chargesPortion, si.FeesRefundIfSettled
                                else
                                    si.GeneratedPayment.Value, 0L<Cent>, 0L<Cent>, 0L<Cent>, si.FeesRefundIfSettled
                            else
                                si.PrincipalPortion, si.FeesPortion, si.InterestPortion, si.ChargesPortion, si.FeesRefundIfSettled
                    PaymentQuote (si.GeneratedPayment.Value, principalPortion, feesPortion, interestPortion, chargesPortion, feesRefundIfSettled)
            return {
                IntendedPurpose = intendedPurpose
                QuoteResult = quoteResult
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }
