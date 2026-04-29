namespace FSharp.Finance.Personal

/// functions for settling outstanding payments
module Quotes =

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

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

    /// the method used to calculate the early settlement figure
    [<RequireQualifiedAccess; Struct>]
    type EarlySettlementMethod =
        /// pro-rata actuarial interest to settlement date — standard for most modern products
        | Actuarial
        /// sum-of-digits method used in older regulated agreements (pre-2004)
        | RuleOf78
        /// statutory right-to-settle per Consumer Credit Act 1974 s.94 and Consumer Credit (Early Settlement) Regulations 2004
        | FCA

    /// the type of early repayment charge
    [<RequireQualifiedAccess>]
    type EarlyRepaymentChargeType =
        /// a tiered percentage on the outstanding balance; each element is (year number, rate)
        | TieredPercentage of (int * Percent) array
        /// a fixed number of months' interest on the outstanding balance
        | FixedMonthsInterest of int

    /// the result of an early settlement calculation, optionally including an early repayment charge
    [<Struct>]
    type EarlySettlementResult = {
        /// the method used for the calculation
        Method: EarlySettlementMethod
        /// the outstanding principal balance at the settlement date (from the original basic schedule)
        OutstandingPrincipal: int64<Cent>
        /// the interest rebate applied under the specified method
        InterestRebate: int64<Cent>
        /// the settlement figure — net amount required to fully repay the loan (before any early repayment charge)
        SettlementFigure: int64<Cent>
        /// the early repayment charge, if any
        EarlyRepaymentCharge: int64<Cent>
        /// the net settlement figure including any early repayment charge
        NetSettlementFigure: int64<Cent>
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
                        let chargesPortion = min si.ChargesPortion existingPayments

                        let interestPortion =
                            min si.InterestPortion (max 0L<Cent> (existingPayments - chargesPortion))

                        let feePortion =
                            min si.FeePortion (max 0L<Cent> (existingPayments - chargesPortion - interestPortion))

                        let principalPortion =
                            max 0L<Cent> (existingPayments - feePortion - chargesPortion - interestPortion)

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

    /// calculates the Rule of 78 (sum-of-digits) settlement figure and rebate from the original basic schedule;
    /// returns (settlementFigure, rebate)
    let private ruleOf78Settlement (basicSchedule: BasicSchedule) (settlementDay: int<OffsetDay>) =
        let items = basicSchedule.Items
        let N = items.Length

        if N = 0 then
            0L<Cent>, 0L<Cent>
        else
            let remainingItems = items |> Array.filter (fun bi -> bi.Day > settlementDay)
            let n = remainingItems.Length

            let remainingTotal =
                remainingItems
                |> Array.sumBy (fun bi -> ScheduledPayment.total bi.ScheduledPayment)

            let totalInterest = basicSchedule.Stats.InterestTotal

            let rebate =
                if N * (N + 1) = 0 then
                    0L<Cent>
                else
                    decimal totalInterest * decimal (n * (n + 1)) / decimal (N * (N + 1))
                    |> Cent.round (RoundWith System.MidpointRounding.AwayFromZero)

            max 0L<Cent> (remainingTotal - rebate), max 0L<Cent> rebate

    /// calculates the FCA/CCA 2004 settlement figure and rebate from the original basic schedule;
    /// returns (settlementFigure, rebate)
    let private fcaSettlement (bp: BasicParameters) (basicSchedule: BasicSchedule) (settlementDay: int<OffsetDay>) =
        let items = basicSchedule.Items

        if Array.isEmpty items then
            0L<Cent>, 0L<Cent>
        else
            // indexed payments, 1-based period index
            let payments =
                items
                |> Array.mapi (fun i bi -> i + 1, ScheduledPayment.total bi.ScheduledPayment)

            // how many complete scheduled periods have elapsed by the settlement day
            let settlementPeriod =
                items |> Array.filter (fun bi -> bi.Day <= settlementDay) |> Array.length

            // determine the unit period from the schedule config
            let unitPeriod =
                match bp.ScheduleConfig with
                | AutoGenerateSchedule ags -> Config.unitPeriod ags.UnitPeriodConfig
                | FixedSchedules _
                | CustomSchedule _ ->
                    let finalDate =
                        basicSchedule.Stats.LastScheduledPaymentDay |> OffsetDay.toDate bp.StartDate

                    let transactionTerm =
                        UnitPeriod.transactionTerm bp.StartDate bp.StartDate finalDate bp.StartDate

                    let paymentDates =
                        items |> Array.map (fun bi -> bi.Day |> OffsetDay.toDate bp.StartDate)

                    UnitPeriod.nearest transactionTerm [| bp.StartDate |] paymentDates

            // the most-recent scheduled payment day at or before the settlement day
            let previousPaymentDay =
                items
                |> Array.filter (fun bi -> bi.Day <= settlementDay)
                |> Array.tryLast
                |> Option.map _.Day
                |> Option.defaultValue 0<OffsetDay>

            // fractional part of the current period elapsed since the last payment date
            let numerator = int settlementDay - int previousPaymentDay
            let denominator = roughLength unitPeriod

            let settlementPartPeriod =
                if denominator = 0 then Fraction.Zero
                else Fraction.Simple(numerator, denominator)

            // compute the FCA rebate per Consumer Credit (Early Settlement) Regulations 2004
            let rebate =
                Interest.calculateRebate
                    bp.Principal
                    payments
                    basicSchedule.Stats.InitialApr
                    settlementPeriod
                    settlementPartPeriod
                    unitPeriod
                    bp.PaymentConfig.Rounding

            // total of remaining scheduled payments after the settlement period
            let remainingTotal =
                payments
                |> Array.filter (fun (i, _) -> i > settlementPeriod)
                |> Array.sumBy snd

            max 0L<Cent> (remainingTotal - rebate), max 0L<Cent> rebate

    /// calculates the early repayment charge on the outstanding principal
    let calculateEarlyRepaymentCharge
        (ercType: EarlyRepaymentChargeType)
        (outstandingPrincipal: int64<Cent>)
        (startDate: Date)
        (settlementDate: Date)
        (annualRate: Interest.Rate)
        =
        match ercType with
        | EarlyRepaymentChargeType.TieredPercentage tiers ->
            // year 1 = first 365 days, year 2 = days 366-730, etc.
            let yearsElapsed = max 1 ((settlementDate - startDate).Days / 365 + 1)
            // find matching tier or fall back to the last defined tier
            let rate =
                tiers
                |> Array.tryFind (fun (year, _) -> year = yearsElapsed)
                |> Option.orElseWith (fun () ->
                    tiers
                    |> Array.tryLast
                    |> Option.bind (fun (maxYear, r) ->
                        if yearsElapsed > maxYear then Some(maxYear, r) else None
                    )
                )
                |> Option.map snd
                |> Option.defaultValue (Percent 0m)

            decimal outstandingPrincipal * Percent.toDecimal rate
            |> Cent.round (RoundWith System.MidpointRounding.AwayFromZero)

        | EarlyRepaymentChargeType.FixedMonthsInterest months ->
            let annualRateDecimal = annualRate |> Interest.Rate.annual |> Percent.toDecimal
            let monthlyRate = annualRateDecimal / 12m

            decimal outstandingPrincipal * monthlyRate * decimal months
            |> Cent.round (RoundWith System.MidpointRounding.AwayFromZero)

    /// calculates an early settlement quote using the specified method, with an optional early repayment charge
    let getEarlySettlementQuote
        (settlementMethod: EarlySettlementMethod)
        (ercType: EarlyRepaymentChargeType option)
        (p: Parameters)
        (actualPayments: Map<int<OffsetDay>, ActualPayment array>)
        =
        // generate the actuarial settlement quote (also needed for Actuarial method settlement figure)
        let quote = getQuote p actualPayments

        // the settlement day expressed as an offset from the start date
        let settlementDay = OffsetDay.fromDate p.Basic.StartDate p.Basic.EvaluationDate

        // outstanding principal at the settlement date from the basic (original) schedule
        let outstandingPrincipal =
            quote.Schedules.BasicSchedule.Items
            |> Array.filter (fun bi -> bi.Day <= settlementDay)
            |> Array.tryLast
            |> Option.map _.PrincipalBalance
            |> Option.defaultValue p.Basic.Principal

        // compute (settlementFigure, interestRebate) based on the chosen method
        let settlementFigure, interestRebate =
            match settlementMethod with
            | EarlySettlementMethod.Actuarial ->
                match quote.QuoteResult with
                | PaymentQuote pq -> pq.PaymentValue, 0L<Cent>
                | _ -> 0L<Cent>, 0L<Cent>
            | EarlySettlementMethod.RuleOf78 ->
                ruleOf78Settlement quote.Schedules.BasicSchedule settlementDay
            | EarlySettlementMethod.FCA ->
                fcaSettlement p.Basic quote.Schedules.BasicSchedule settlementDay

        // compute the ERC if a charge type is provided
        let erc =
            match ercType with
            | Some ercType ->
                calculateEarlyRepaymentCharge
                    ercType
                    outstandingPrincipal
                    p.Basic.StartDate
                    p.Basic.EvaluationDate
                    p.Basic.InterestConfig.StandardRate
            | None -> 0L<Cent>

        {
            Method = settlementMethod
            OutstandingPrincipal = outstandingPrincipal
            InterestRebate = interestRebate
            SettlementFigure = settlementFigure
            EarlyRepaymentCharge = erc
            NetSettlementFigure = settlementFigure + erc
        }
