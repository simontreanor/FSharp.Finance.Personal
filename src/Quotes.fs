namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Calculation
    open Currency
    open DateDay
    open PaymentSchedule

    /// a quote containing information on the payment required to settle
    type PaymentQuote = {
        PaymentValue: int64<Cent>
        Apportionment: Apportionment
        FeesRefundIfSettled: int64<Cent>
    }

    /// the result of a quote with a breakdown of constituent amounts where relevant
    [<Struct>]
    type QuoteResult =
        | PaymentQuote of PaymentQuote
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

    /// calculates a revised schedule showing the generated payment for the given quote type
    let getQuote intendedPurpose sp (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =

        let currentAmortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement false actualPayments

        let revisedAmortisationSchedule = Amortisation.generate sp intendedPurpose false actualPayments

        let si =
            revisedAmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.tryFind(fun si ->
                match si.GeneratedPayment, si.PaymentStatus with
                | ToBeGenerated, _
                | GeneratedValue _, _
                | _, InformationOnly -> true
                | _, _ -> false
            )
            |> Option.defaultWith (fun () -> failwith "Unable to find relevant schedule item")

        let confirmedPayments = si.ActualPayments |> Array.sumBy ActualPayment.totalConfirmedOrWrittenOff

        let pendingPayments = si.ActualPayments |> Array.sumBy ActualPayment.totalPending

        let quoteResult =
            if pendingPayments <> 0L<Cent> then 
                AwaitPaymentConfirmation
            else
                match si.GeneratedPayment with
                | GeneratedValue gp ->
                    if confirmedPayments <> 0L<Cent> then
                        if gp > 0L<Cent> then
                            let chargesPortion = Cent.min si.ChargesPortion confirmedPayments
                            let interestPortion = Cent.min si.InterestPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion))
                            let feesPortion = Cent.min si.FeesPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion - interestPortion))
                            let principalPortion = Cent.max 0L<Cent> (confirmedPayments - feesPortion - chargesPortion - interestPortion)
                            PaymentQuote {
                                PaymentValue = GeneratedPayment.Total si.GeneratedPayment
                                Apportionment = {
                                    PrincipalPortion = si.PrincipalPortion - principalPortion
                                    FeesPortion = si.FeesPortion - feesPortion
                                    InterestPortion = si.InterestPortion - interestPortion
                                    ChargesPortion = si.ChargesPortion - chargesPortion
                                }
                                FeesRefundIfSettled = si.FeesRefundIfSettled
                            }
                        else
                            // to do: this is in the event of a refund being due: perhaps interest should be non-zero
                            PaymentQuote {
                                PaymentValue = GeneratedPayment.Total si.GeneratedPayment
                                Apportionment = {
                                    PrincipalPortion = GeneratedPayment.Total si.GeneratedPayment
                                    FeesPortion = 0L<Cent>
                                    InterestPortion = 0L<Cent>
                                    ChargesPortion = 0L<Cent>
                                }
                                FeesRefundIfSettled = si.FeesRefundIfSettled
                            }
                    else
                        PaymentQuote {
                            PaymentValue = GeneratedPayment.Total si.GeneratedPayment
                            Apportionment = {
                                PrincipalPortion = si.PrincipalPortion
                                FeesPortion = si.FeesPortion
                                InterestPortion = si.InterestPortion
                                ChargesPortion = si.ChargesPortion
                            }
                            FeesRefundIfSettled = si.FeesRefundIfSettled
                        }
                | _ ->
                    UnableToGenerateQuote

        {
            IntendedPurpose = intendedPurpose
            QuoteResult = quoteResult
            CurrentSchedule = currentAmortisationSchedule
            RevisedSchedule = revisedAmortisationSchedule
        }
