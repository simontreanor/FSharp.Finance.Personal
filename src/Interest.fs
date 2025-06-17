namespace FSharp.Finance.Personal

open System

/// methods for calculating interest and unambiguously expressing interest rates, as well as enforcing regulatory caps on interest chargeable
module Interest =

    open Calculation
    open DateDay

    /// the interest rate expressed as either an annual or a daily rate
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type Rate =
        /// a zero rate
        | Zero
        /// the annual interest rate, or the daily interest rate multiplied by 365
        | Annual of Percent
        /// the daily interest rate, or the annual interest rate divided by 365
        | Daily of Percent

        /// HTML formatting to display the rate in a readable format
        member r.Html =
            match r with
            | Zero -> "zero"
            | Annual percent -> $"{percent} per year"
            | Daily percent -> $"{percent} per day"

    module Rate =
        /// calculates the annual interest rate from the daily one
        let annual =
            function
            | Rate.Zero -> Percent 0m
            | Rate.Annual air -> air
            | Rate.Daily dir -> dir |> Percent.map ((*) 365m)

        /// calculates the daily interest rate from the annual one
        let daily =
            function
            | Rate.Zero -> Percent 0m
            | Rate.Annual air -> air |> Percent.map ((*) (1m / 365m))
            | Rate.Daily dir -> dir

    /// the daily interest rate
    [<Struct>]
    type DailyRate = {
        /// the day expressed as an offset from the start date
        RateDay: uint<OffsetDay>
        /// the interest rate applicable on the given day
        InterestRate: Rate
    }

    /// caps on the total interest accruable
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type Cap = {
        /// a cap on the total amount of interest chargeable over the entire schedule
        TotalAmount: Amount
        /// a cap on the daily amount of interest chargeable
        DailyAmount: Amount
    } with

        /// HTML formatting to display the cap in a readable format
        member c.Html = $"total {c.TotalAmount}; daily {c.DailyAmount}"

    /// caps on the total interest accruable
    module Cap =
        /// no cap
        let zero = {
            Cap.TotalAmount = Amount.Unlimited
            Cap.DailyAmount = Amount.Unlimited
        }

        /// if the base value plus the added value exceeds the cap total, return the difference between the cap total and the base value
        let cappedAddedValue capAmount principalBalance baseValue addedValue =
            match capAmount with
            | Amount.Unlimited -> addedValue
            | amount ->
                let capTotal = Amount.total principalBalance amount |> max 0m<Cent>

                if baseValue + addedValue > capTotal then
                    capTotal - baseValue
                else
                    addedValue

    /// a promotional interest rate valid during the specified date range
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type PromotionalRate = {
        DateRange: DateRange
        Rate: Rate
    } with

        /// HTML formatting to display the cap in a readable format
        member c.Html = $"{c.DateRange} at {c.Rate}"

    /// a promotional interest rate valid during the specified date range
    module PromotionalRate =
        /// creates a map of offset days and promotional interest rates
        let toMap (startDate: Date) (promotionalRates: PromotionalRate array) =
            promotionalRates
            |> Array.collect (fun pr ->
                [|
                    (pr.DateRange.DateRangeStart - startDate).Days .. (pr.DateRange.DateRangeEnd - startDate).Days
                |]
                |> Array.map (fun d -> uint d * 1u<OffsetDay>, pr.Rate)
            )
            |> Map.ofArray

    /// the method used to calculate the interest
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type Method =
        /// actuarial interest method, where interest is based on the principal balance and the number of days outstanding
        | Actuarial
        /// add-on interest method, where the interest accrued over the loan is added to the initial balance and the interest is paid off before the principal balance
        | AddOn

        /// HTML formatting to display the method in a readable format
        member m.Html =
            match m with
            | Actuarial -> "actuarial"
            | AddOn -> "add-on"

    /// basic interest options
    [<Struct>]
    type BasicConfig = {
        /// the method for calculating interest
        Method: Method
        /// the standard rate of interest
        StandardRate: Rate
        /// any total or daily caps on interest
        Cap: Cap
        /// how to round interest
        Rounding: Rounding
        /// which APR calculation method to use
        AprMethod: Apr.CalculationMethod
        /// which APR decimal-place precision (note that this is two places more than the percent precision)
        AprPrecision: uint
    }

    /// basic interest options
    module Config =
        /// formats the interest config as HTML
        let toHtml basicConfig =
            $"""
            <div>
                <div>standard rate: <i>{basicConfig.StandardRate}</i></div>
                <div>method: <i>{basicConfig.Method}</i></div>
                <div>rounding: <i>{basicConfig.Rounding}</i></div>
                <div>APR method: <i>{basicConfig.AprMethod}</i></div>
                <div>APR precision: <i>{basicConfig.AprPrecision - 2u} d.p.</i></div>
                <div>cap: <i>{basicConfig.Cap}</div>
            </div>"""

    /// advanced interest options
    [<Struct>]
    type AdvancedConfig = {
        /// any grace period at the start of a schedule, during which if settled no interest is payable
        InitialGracePeriod: uint<OffsetDay>
        /// any promotional or introductory offers during which a different interest rate is applicable
        PromotionalRates: PromotionalRate array
        /// the interest rate applicable for any period in which a refund is owing
        RateOnNegativeBalance: Rate
    }

    /// advanced interest options
    module AdvancedConfig =
        /// formats the interest config as HTML
        let toHtml advancedConfig =
            let promotionalRates =
                if advancedConfig.PromotionalRates |> Array.isEmpty then
                    "no promotional rates"
                else
                    let rates =
                        advancedConfig.PromotionalRates
                        |> Array.map (fun pr -> $"<div>{pr.Html}</div>")
                        |> String.concat ""

                    $"""<fieldset><legend>promotional rates</legend>{rates}"""

            $"""
            <div>
                <div>initial grace period: <i>{advancedConfig.InitialGracePeriod} day(s)</i></div>
                <div>rate on negative balance: <i>{advancedConfig.RateOnNegativeBalance}</i></div>
                <div>{promotionalRates}</div>
            </div>"""

    /// calculates the interest chargeable on a range of days
    let dailyRates
        startDate
        isSettledWithinGracePeriod
        standardRate
        promotionalRates
        (fromDay: uint<OffsetDay>)
        (toDay: uint<OffsetDay>)
        =
        let promoRates = promotionalRates |> PromotionalRate.toMap startDate

        [| uint fromDay + 1u .. uint toDay |]
        |> Array.map (fun d ->
            let offsetDay = d * 1u<OffsetDay>

            if isSettledWithinGracePeriod then
                {
                    RateDay = offsetDay
                    InterestRate = Rate.Zero
                }
            else
                match promoRates |> Map.tryFind offsetDay with
                | Some rate -> {
                    RateDay = offsetDay
                    InterestRate = rate
                  }
                | None -> {
                    RateDay = offsetDay
                    InterestRate = standardRate
                  }
        )

    /// calculates the interest accrued on a balance at a particular interest rate over a number of days, optionally capped by a daily amount
    let calculate (principalBalance: int64<Cent>) dailyInterestCap interestRounding dailyRates =
        dailyRates
        |> Array.sumBy (fun dr ->
            let rate = dr.InterestRate |> Rate.daily |> Percent.toDecimal
            let interest = decimal principalBalance * rate * 1m<Cent>
            Cap.cappedAddedValue dailyInterestCap principalBalance 0m<Cent> interest
        )
        |> Cent.roundTo interestRounding 8

    /// type alias to represent an indexed transfer
    type internal TransferMap = Map<int, decimal<Cent>>

    /// type alias to represent an indexed interval
    type internal IntervalMap = Map<int, int>

    /// calculates the settlement figure based on Consumer Credit (Early Settlement) Regulations 2004 regulation 4(1):
    ///
    /// 4.—(1) The amount of the rebate is the difference between the total amount of the repayments of credit that would fall due for payment after the settlement date
    /// if early settlement did not take place and the amount given by the following formula:
    ///
    /// $$\sum_{i=1}^{m} A_i(1 + r)^{a_i} - \sum_{j=1}^{n} B_j(1 + r)^{b_j}$$
    ///
    /// where:
    ///
    /// $A_i$ = the amount of the ith advance of credit,
    /// $B_j$ = the amount of the jth repayment of credit,
    /// $r$ = the periodic rate equivalent of the APR/100,
    /// $m$ = the number of advances of credit made before the settlement date,
    /// $n$ = the number of repayments of credit made before the settlement date,
    /// $a_i$ = the time between the ith advance of credit and the settlement date, expressed in periods,
    /// $b_j$ = the time between the jth repayment of credit and the settlement date, expressed in periods, and
    /// $\sum$ represents the sum of all the terms indicated.
    let ``CCA 2004 regulation 4(1) formula`` (A: TransferMap) (B: TransferMap) r m n (a: IntervalMap) (b: IntervalMap) =
        if A.Count < m || B.Count < n || a.Count < m || b.Count < n then
            0m<Cent>
        else
            let round = Cent.roundTo (RoundWith MidpointRounding.AwayFromZero) 2

            let sum1 =
                [ 1..m ]
                |> List.sumBy (fun i -> A[i] * (1m + r |> powi a[i] |> decimal) |> round)

            let sum2 =
                [ 1..n ]
                |> List.sumBy (fun j -> B[j] * (1m + r |> powi b[j] |> decimal) |> round)

            sum1 - sum2

    /// calculates the amount of rebate due following an early settlement
    ///
    /// note: the APR is the initial APR as determined at the start of the agreement
    let calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding =
        if payments |> Array.isEmpty then
            0L<Cent>
        else
            let advanceMap = Map [ (1, Cent.toDecimalCent principal) ]

            let paymentMap =
                payments
                |> Array.map (fun (index, payment) -> index, decimal payment * 1m<Cent>)
                |> Map.ofArray

            let aprUnitPeriodRate =
                apr
                |> Apr.ukUnitPeriodRate unitPeriod
                |> Percent.toDecimal
                |> Rounding.roundTo (RoundWith MidpointRounding.AwayFromZero) 6

            let advanceCount = 1
            let paymentCount = payments |> Array.length |> min settlementPeriod
            let advanceIntervalMap = Map [ (1, settlementPeriod) ]

            let paymentIntervalMap =
                payments
                |> Array.take paymentCount
                |> Array.scan (fun (index, _) (day, _) -> index + 1, settlementPeriod - day) (0, settlementPeriod)
                |> Array.tail
                |> Map.ofArray

            let addPartPeriodTotal (sf: decimal<Cent>) =
                sf
                * (1m + aprUnitPeriodRate
                   |> powm (Fraction.toDecimal settlementPartPeriod)
                   |> decimal)

            let wholePeriodTotal =
                ``CCA 2004 regulation 4(1) formula``
                    advanceMap
                    paymentMap
                    aprUnitPeriodRate
                    advanceCount
                    paymentCount
                    advanceIntervalMap
                    paymentIntervalMap

            let settlementFigure =
                wholePeriodTotal |> addPartPeriodTotal |> Cent.fromDecimalCent paymentRounding

            let remainingPaymentTotal =
                payments
                |> Array.filter (fun (index, _) -> index > settlementPeriod)
                |> Array.sumBy snd

            remainingPaymentTotal - settlementFigure

    /// if there is less than one cent remaining, discards any fraction
    let ignoreFractionalCents (multiplier: uint) value =
        if abs value < decimal multiplier * 1m<Cent> then
            0m<Cent>
        else
            value
