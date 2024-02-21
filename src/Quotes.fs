namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open CustomerPayments

    [<Struct>]
    type QuoteResult =
        | PaymentQuote of PaymentQuote: int64<Cent> * OfWhichPrincipal: int64<Cent> * OfWhichInterest: int64<Cent>
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

    /// calculates a revised schedule showing the generated payment for the given quote type
    let getQuote quoteType sp negativeInterestOption (actualPayments: CustomerPayment array) =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement DoNotCalculateFinalApr negativeInterestOption actualPayments
            let! revisedAmortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote quoteType) DoNotCalculateFinalApr negativeInterestOption actualPayments
            let! si = revisedAmortisationSchedule.ScheduleItems |> Array.tryFind(_.GeneratedPayment.IsSome) |> toValueOption
            let confirmedPayments = si.ActualPayments |> Array.sumBy(function ActualPayment.Confirmed ap -> ap | _ -> 0L<Cent>)
            let pendingPayments = si.ActualPayments |> Array.sumBy(function ActualPayment.Pending ap -> ap | _ -> 0L<Cent>)
            let quoteResult =
                if si.GeneratedPayment.IsNone then
                    UnableToGenerateQuote
                elif pendingPayments <> 0L<Cent> then 
                    AwaitPaymentConfirmation
                else
                    let principalPortion, interestPortion =
                        if si.GeneratedPayment.IsNone || si.GeneratedPayment.Value = 0L<Cent> || confirmedPayments = 0L<Cent> then
                            0L<Cent>, 0L<Cent>
                        else
                            let ratio = decimal si.GeneratedPayment.Value / (decimal si.NetEffect)
                            Cent.round RoundUp (decimal si.PrincipalPortion * ratio), Cent.round RoundDown (decimal si.InterestPortion * ratio)
                    PaymentQuote (si.GeneratedPayment.Value, principalPortion, interestPortion)
            return {
                QuoteType = quoteType
                QuoteResult = quoteResult
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }
