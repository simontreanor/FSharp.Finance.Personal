namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Amortisation
    open AppliedPayment
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
        // the revised schedule showing the settlement, if applicable
        Schedules: GenerationResult
    }

    /// calculates a revised schedule showing the generated payment for the given quote type
    let getQuote (p: Parameters) (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        // generate a revised statement showing a generated settlement figure on the relevant date
        let schedules =
            amortise
                {
                    p with
                        Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
                        Advanced.TrimEnd = false
                }
                actualPayments
        // try to get the schedule item containing the generated value
        let si =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.tryFind (fun si ->
                match si.GeneratedPayment, si.PaymentStatus with
                | ToBeGenerated, _
                | GeneratedValue _, _
                | _, InformationOnly -> true
                | _, _ -> false
            )
            |> Option.defaultWith (fun () -> failwith "Unable to find relevant schedule item")
        // get an array of payments pending anywhere in the revised amortisation schedule
        let pendingPayments =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy (_.ActualPayments >> Array.sumBy ActualPayment.totalPending)
        // produce a quote result
        let quoteResult =
            // if there are any payments pending, inform the caller that a quote cannot be generated for this reason
            if pendingPayments <> 0L<Cent> then
                AwaitPaymentConfirmation
            else
                // get an array of confirmed or written-off payments - these are ones that have a net effect
                let existingPayments =
                    si.ActualPayments |> Array.sumBy ActualPayment.totalConfirmedOrWrittenOff

                match si.GeneratedPayment with
                // where there is a generated payment, create a quote detailing the payment
                | GeneratedValue generatedValue ->
                    // if there are no existing payments on the day, simply apportion the payment according to the schedule item
                    if existingPayments = 0L<Cent> then
                        PaymentQuote {
                            PaymentValue = GeneratedPayment.total si.GeneratedPayment
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

                        let interestPortion =
                            Cent.min si.InterestPortion (Cent.max 0L<Cent> (existingPayments - chargesPortion))

                        let feePortion =
                            Cent.min
                                si.FeePortion
                                (Cent.max 0L<Cent> (existingPayments - chargesPortion - interestPortion))

                        let principalPortion =
                            Cent.max 0L<Cent> (existingPayments - feePortion - chargesPortion - interestPortion)

                        PaymentQuote {
                            PaymentValue = GeneratedPayment.total si.GeneratedPayment
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
                            PaymentValue = GeneratedPayment.total si.GeneratedPayment
                            Apportionment = {
                                Apportionment.zero with
                                    PrincipalPortion = GeneratedPayment.total si.GeneratedPayment
                            }
                            FeeRebateIfSettled = si.FeeRebateIfSettled
                        }
                // where there is no generated payment, inform the caller that a quote could not be generated
                | _ -> UnableToGenerateQuote
        // return the quote result
        {
            QuoteResult = quoteResult
            Schedules = schedules
        }
