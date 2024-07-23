namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Calculation
    open Currency
    open CustomerPayments
    open FeesAndCharges
    open PaymentSchedule
    open ValueOptionCE

    /// the result of a quote with a breakdown of constituent amounts where relevant
    [<Struct>]
    type QuoteResult =
        | PaymentQuote of PaymentQuote: int64<Cent> * PaymentBreakdown: PaymentBreakdown
        | AwaitPaymentConfirmation

    /// a settlement quote
    [<Struct>]
    type Quote = {
        IntendedPurpose: IntendedPurpose
        QuoteResult: QuoteResult
        AmendedSchedule: Amortisation.Schedule
    }

    /// <summary>calculates a revised schedule showing the generated payment for the given quote type</summary>
    /// <param name="settlementDay">the day for which to generate the settlement quote</param>
    /// <param name="sp">the parameters for creating the schedule</param>
    /// <param name="actualPayments">an array of the actual payments</param>
    /// <returns>the requested quote, if possible</returns>
    let getQuote intendedPurpose sp (actualPayments: CustomerPayment array) =
        voption {
            let generateSchedule ip = Amortisation.generate sp ip ScheduleType.Original false actualPayments

            let! amendedSchedule = generateSchedule intendedPurpose

            let! si =
                amendedSchedule.ScheduleItems
                |> Array.tryFind(fun asi -> asi.OffsetDate = sp.AsOfDate)
                |> toValueOption

            let! amendedSchedule' =
                match si.GeneratedPayment, intendedPurpose with
                | ValueNone, _
                | _, IntendedPurpose.Settlement _
                | _, IntendedPurpose.Statement StatementType.InformationOnly
                | _, IntendedPurpose.Statement (StatementType.Internal _) ->
                    voption { return amendedSchedule }
                | _, IntendedPurpose.Statement st ->
                    amendedSchedule.StatementInfo.Item st
                    |> StatementType.Internal
                    |> IntendedPurpose.Statement
                    |> generateSchedule

            let! si' =
                amendedSchedule'.ScheduleItems
                |> Array.tryFind(fun asi -> asi.OffsetDate = sp.AsOfDate)
                |> toValueOption

            let confirmedPayments =
                si'.ActualPayments
                |> Array.sumBy(fun ap ->
                    match ap.ActualPaymentStatus with
                    | ActualPaymentStatus.Confirmed ap
                    | ActualPaymentStatus.WriteOff ap -> ap
                    | _ -> 0L<Cent>
                )

            let pendingPayments =
                si'.ActualPayments
                |> Array.sumBy(fun ap ->
                    match ap.ActualPaymentStatus with
                    | ActualPaymentStatus.Pending ap -> ap
                    | _ -> 0L<Cent>
                )

            let feesRefund =
                match intendedPurpose, sp.FeesAndCharges.FeesSettlementRefund with
                | IntendedPurpose.Statement _, _
                | _, Fees.SettlementRefund.None ->
                    0L<Cent>
                | _, _ ->
                    si'.FeesRefundIfSettled

            let quoteResult =
                if pendingPayments <> 0L<Cent> then 
                    AwaitPaymentConfirmation
                else
                    PaymentQuote <|
                    if si'.GeneratedPayment.IsNone then
                        si'.SettlementFigure,
                        {
                            OfWhichCharges = 0L<Cent>
                            OfWhichInterest = 0L<Cent>
                            FeesRefund = 0L<Cent>
                            OfWhichFees = 0L<Cent>
                            OfWhichPrincipal = 0L<Cent>
                        }
                    else
                        if confirmedPayments <> 0L<Cent> then
                            let chargesPortion = Cent.min si'.ChargesPortion confirmedPayments
                            let interestPortion = Cent.min si'.InterestPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion))
                            let feesPortion = Cent.min si'.FeesPortion (Cent.max 0L<Cent> (confirmedPayments - chargesPortion - interestPortion))
                            let principalPortion = Cent.max 0L<Cent> (confirmedPayments - feesPortion - chargesPortion - interestPortion)
                            si'.GeneratedPayment.Value,
                            {
                                OfWhichCharges = si'.ChargesPortion - chargesPortion
                                OfWhichInterest = si'.InterestPortion - interestPortion
                                FeesRefund = feesRefund
                                OfWhichFees = si'.FeesPortion - feesPortion
                                OfWhichPrincipal = si'.PrincipalPortion - principalPortion
                            }
                        else
                            si'.GeneratedPayment.Value,
                            {
                                OfWhichCharges = si'.ChargesPortion
                                OfWhichInterest = si'.InterestPortion
                                FeesRefund = feesRefund
                                OfWhichFees = si'.FeesPortion
                                OfWhichPrincipal = si'.PrincipalPortion
                            }

            return {
                IntendedPurpose = intendedPurpose
                QuoteResult = quoteResult
                AmendedSchedule = amendedSchedule'
            }
        }
