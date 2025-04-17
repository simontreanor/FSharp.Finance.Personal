namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Calculation
    open DateDay
    open Scheduling

    /// a quote containing information on the payment required to settle
    type PaymentQuote = {
        // the value of the payment
        PaymentValue: int64<Cent>
        // how the payment is apportioned to charges, interest, fee, and principal
        Apportionment: Apportionment
        // the value of any fee rebate that would be due if settled
        FeeRebateIfSettled: int64<Cent>
    }

    /// the result of a quote with a breakdown of constituent amounts where relevant
    [<Struct>]
    type QuoteResult =
        // a payment quote was generated
        | PaymentQuote of PaymentQuote
        // a payment quote could not be generated because one or more payments is pending
        | AwaitPaymentConfirmation
        // a payment quote was not possible to generate (typically the case for statements)
        | UnableToGenerateQuote

    /// a settlement quote
    [<Struct>]
    type Quote = {
        // the quote result
        QuoteResult: QuoteResult
        // a statement showing the schedule at the time the quote was generated
        CurrentSchedules: Amortisation.GenerationResult
        // the revised schedule showing the settlement, if applicable
        RevisedSchedules: Amortisation.GenerationResult
    }

    /// calculates a revised schedule showing the generated payment for the given quote type
    let getQuote settlementDay schedulingParameters (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        // generate a statement showing the current state of the amortisation schedule - this will only be used in the return value in case the caller requires a comparison
        let currentSchedules = Amortisation.generate schedulingParameters ValueNone false actualPayments
        // generate a revised statement showing a generated settlement figure on the relevant date
        let revisedSchedules =
            Amortisation.generate schedulingParameters (ValueSome settlementDay) false actualPayments
        // try to get the schedule item containing the generated value
        let si =
            revisedSchedules.AmortisationSchedule.ScheduleItems
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
        let pendingPayments = revisedSchedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy (_.ActualPayments >> Array.sumBy ActualPayment.totalPending)
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
                                FeePortion = si.FeePortion
                                InterestPortion = si.InterestPortion
                                ChargesPortion = si.ChargesPortion
                            }
                            FeeRebateIfSettled = si.FeeRebateIfSettled
                        }
                    // if there is an existing payment on the day and the generated value is positive, apportion the existing payment first (in the order charges->interest->fee->principal), then apportion the generated payment
                    elif generatedValue >= 0L<Cent> then
                        let chargesPortion = Cent.min si.ChargesPortion existingPayments
                        let interestPortion = Cent.min si.InterestPortion (Cent.max 0L<Cent> (existingPayments - chargesPortion))
                        let feePortion = Cent.min si.FeePortion (Cent.max 0L<Cent> (existingPayments - chargesPortion - interestPortion))
                        let principalPortion = Cent.max 0L<Cent> (existingPayments - feePortion - chargesPortion - interestPortion)
                        PaymentQuote {
                            PaymentValue = GeneratedPayment.Total si.GeneratedPayment
                            Apportionment = {
                                PrincipalPortion = si.PrincipalPortion - principalPortion
                                FeePortion = si.FeePortion - feePortion
                                InterestPortion = si.InterestPortion - interestPortion
                                ChargesPortion = si.ChargesPortion - chargesPortion
                            }
                            FeeRebateIfSettled = si.FeeRebateIfSettled
                        }
                    // if there is an existing payment on the day and the generated value is negative, because of the apportionment order, any negative balance lies with the principal only, so the generated payment only has a principal portion
                    else
                        PaymentQuote {
                            PaymentValue = GeneratedPayment.Total si.GeneratedPayment
                            Apportionment = { Apportionment.Zero with PrincipalPortion = GeneratedPayment.Total si.GeneratedPayment }
                            FeeRebateIfSettled = si.FeeRebateIfSettled
                        }
                // where there is no generated payment, inform the caller that a quote could not be generated
                | _ ->
                    UnableToGenerateQuote
        // return the quote result
        {
            QuoteResult = quoteResult
            CurrentSchedules = currentSchedules
            RevisedSchedules = revisedSchedules
        }
