namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Calculation
    open DateDay
    open Scheduling

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
    let getQuote intendedPurpose schedulingParameters (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        // generate a statement showing the current state of the amortisation schedule - this will only be used in the return value in case the caller requires a comparison
        let currentAmortisationSchedule = Amortisation.generate schedulingParameters IntendedPurpose.Statement false actualPayments
        // generate a revised statement showing a generated settlement figure on the relevant date
        let revisedAmortisationSchedule = Amortisation.generate schedulingParameters intendedPurpose false actualPayments
        // try to get the schedule item containing the generated value
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
        // get an array of payments pending anywhere in the revised amortisation schedule
        let pendingPayments = revisedAmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy (_.ActualPayments >> Array.sumBy ActualPayment.totalPending)
        // produce a quote result
        let quoteResult =
            // if there are any payments pending, inform the caller that a quote cannot be generated for this reason
            if pendingPayments <> 0L<Cent> then 
                AwaitPaymentConfirmation
            else
                // get an array of confirmed or written-off payments - these are ones that have a net effect
                let existingPayments = si.ActualPayments |> Array.sumBy ActualPayment.totalConfirmedOrWrittenOff
                match si.GeneratedPayment with
                // where there is a generated payment, create a quote detailing the payment
                | GeneratedValue generatedValue ->
                    // if there are no existing payments on the day, simply apportion the payment according to the schedule item
                    if existingPayments = 0L<Cent> then
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
                    // if there is an existing payment on the day and the generated value is positive, apportion the existing payment first (in the order charges->interest->fees->principal), then apportion the generated payment
                    elif generatedValue >= 0L<Cent> then
                        let chargesPortion = Cent.min si.ChargesPortion existingPayments
                        let interestPortion = Cent.min si.InterestPortion (Cent.max 0L<Cent> (existingPayments - chargesPortion))
                        let feesPortion = Cent.min si.FeesPortion (Cent.max 0L<Cent> (existingPayments - chargesPortion - interestPortion))
                        let principalPortion = Cent.max 0L<Cent> (existingPayments - feesPortion - chargesPortion - interestPortion)
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
                    // if there is an existing payment on the day and the generated value is negative, because of the apportionment order, any negative balance lies with the principal only, so the generated payment only has a principal portion
                    else
                        PaymentQuote {
                            PaymentValue = GeneratedPayment.Total si.GeneratedPayment
                            Apportionment = { Apportionment.Zero with PrincipalPortion = GeneratedPayment.Total si.GeneratedPayment }
                            FeesRefundIfSettled = si.FeesRefundIfSettled
                        }
                // where there is no generated payment (e.g. if intended purpose was statement), inform the caller that a quote could not be generated
                | _ ->
                    UnableToGenerateQuote
        // return the quote result
        {
            IntendedPurpose = intendedPurpose
            QuoteResult = quoteResult
            CurrentSchedule = currentAmortisationSchedule
            RevisedSchedule = revisedAmortisationSchedule
        }
