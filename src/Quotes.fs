namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open CustomerPayments

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

    /// calculates a revised schedule showing the generated payment for the given quote type
    let getQuote quoteType sp negativeInterestOption (actualPayments: CustomerPayment array) =
        voption {
            let! currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement DoNotCalculateFinalApr negativeInterestOption actualPayments
            let! revisedAmortisationSchedule = Amortisation.generate sp (IntendedPurpose.Quote quoteType) DoNotCalculateFinalApr negativeInterestOption actualPayments
            let! si = revisedAmortisationSchedule.ScheduleItems |> Array.tryFind(_.GeneratedPayment.IsSome) |> toValueOption
            let principalPortion, interestPortion =
                if si.GeneratedPayment.IsNone || si.GeneratedPayment.Value = 0L<Cent> || Array.sum si.ActualPayments = 0L<Cent> then
                    0L<Cent>, 0L<Cent>
                else
                    let ratio = decimal si.GeneratedPayment.Value / (decimal si.NetEffect)
                    Cent.round RoundUp (decimal si.PrincipalPortion * ratio), Cent.round RoundDown (decimal si.InterestPortion * ratio)
            return {
                QuoteType = quoteType
                PaymentAmount = si.GeneratedPayment |> ValueOption.defaultValue 0L<Cent>
                OfWhichPrincipal = principalPortion
                OfWhichInterest = interestPortion
                CurrentSchedule = currentAmortisationSchedule
                RevisedSchedule = revisedAmortisationSchedule
            }
        }
