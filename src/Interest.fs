namespace FSharp.Finance.Personal

open System

/// methods for calculating interest and unambiguously expressing interest rates, as well as enforcing regulatory caps on interest chargeable
module Interest =

    open Amount
    open Calculation
    open Currency
    open DateDay
    open Percentages
    open ValueOptionCE

    /// the interest rate expressed as either an annual or a daily rate
    [<RequireQualifiedAccess; Struct>]
    type Rate =
        /// a zero rate
        | Zero
        /// the annual interest rate, or the daily interest rate multiplied by 365
        | Annual of Annual:Percent
        /// the daily interest rate, or the annual interest rate divided by 365
        | Daily of Daily:Percent
        with
            /// used to pretty-print the interest rate for debugging
            static member serialise = function
                | Zero -> $"ZeroPc"
                | Annual (Percent air) -> $"AnnualInterestRate{air}pc"
                | Daily (Percent dir) -> $"DailyInterestRate{dir}pc"

            /// calculates the annual interest rate from the daily one
            static member annual = function
                | Zero -> Percent 0m
                | Annual (Percent air) -> air |> Percent
                | Daily (Percent dir) -> dir * 365m |> Percent

            /// calculates the daily interest rate from the annual one
            static member daily = function
                | Zero -> Percent 0m
                | Annual (Percent air) -> air / 365m |> Percent
                | Daily (Percent dir) -> dir |> Percent

    /// the daily interest rate
    [<Struct>]
    type DailyRate = {
        /// the day expressed as an offset from the start date
        RateDay: int<OffsetDay>
        /// the interest rate applicable on the given day
        InterestRate: Rate
    }

    /// the interest cap options
    [<RequireQualifiedAccess; Struct>]
    type Cap = {
        /// a cap on the total amount of interest chargeable over the lifetime of a product
        Total: Amount voption
        /// a cap on the daily amount of interest chargeable
        Daily: Amount voption
    }
    with
        /// no cap
        static member none = {
            Total = ValueNone
            Daily = ValueNone
        }

        /// calculates the total interest cap
        static member total (initialPrincipal: int64<Cent>) = function
            | ValueSome amount -> Amount.total initialPrincipal amount |> max 0m<Cent>
            | ValueNone -> decimal Int64.MaxValue * 1m<Cent>

    /// a promotional interest rate valid during the specified date range
    [<RequireQualifiedAccess; Struct>]
    type PromotionalRate = {
        DateRange: DateRange
        Rate: Rate
    }
    with
        /// creates a map of offset days and promotional interest rates
        static member toMap (startDate: Date) promotionalRates =
            promotionalRates
            |> Array.collect(fun pr ->
                [| (pr.DateRange.Start - startDate).Days .. (pr.DateRange.End - startDate).Days |]
                |> Array.map(fun d -> d, pr.Rate)
            )
            |> Map.ofArray

    /// the method used to calculate the interest
    [<RequireQualifiedAccess; Struct>]
    type Method =
        /// simple interest method, where interest is based on the principal balance and the number of days outstanding
        | Simple
        /// add-on interest method, where the interest accrued over the loan is added to the initial balance and the interest is paid off before the principal balance
        | AddOn

    /// interest options
    [<Struct>]
    type Options = {
        /// the method for calculating interest
        Method: Method
        /// the standard rate of interest
        StandardRate: Rate
        /// any total or daily caps on interest
        Cap: Cap
        /// any grace period at the start of a product, during which if a product is settled no interest is payable
        InitialGracePeriod: int<DurationDay>
        /// any promotional or introductory offers during which a different interest rate is applicable
        PromotionalRates: PromotionalRate array
        /// the interest rate applicable for any period in which a refund is owing
        RateOnNegativeBalance: Rate voption
    }

    /// calculates the interest chargeable on a range of days
    let dailyRates (startDate: Date) isSettledWithinGracePeriod standardRate promotionalRates (fromDay: int<OffsetDay>) (toDay: int<OffsetDay>) =
        let promoRates = promotionalRates |> PromotionalRate.toMap startDate

        [| int fromDay + 1 .. int toDay |]
        |> Array.map(fun d ->
            let offsetDay = d * 1<OffsetDay>
            if isSettledWithinGracePeriod then
                { RateDay = offsetDay; InterestRate = Rate.Zero }
            else
                match promoRates |> Map.tryFind d with
                | Some rate -> { RateDay = offsetDay; InterestRate = rate }
                | None -> { RateDay = offsetDay; InterestRate = standardRate }
        )

    /// calculate the interest accrued on a balance at a particular interest rate over a number of days, optionally capped by a daily amount
    let calculate (balance: int64<Cent>) (dailyInterestCap: Amount voption) interestRounding (dailyRates: DailyRate array) =
        let dailyCap = Cap.total balance dailyInterestCap

        dailyRates
        |> Array.sumBy (fun dr ->
            dr.InterestRate
            |> Rate.daily
            |> Percent.toDecimal
            |> fun r -> decimal balance * r * 1m<Cent>
            |> min dailyCap
        )
        |> Cent.roundTo interestRounding 8

    /// calculate the settlement figure based on Consumer Credit (Early Settlement) Regulations 2004 regulation 4(1)
    let internal ``CCA 2004 regulation 4(1) formula`` (A: Map<int, decimal<Cent>>) (B: Map<int, decimal<Cent>>) (r: decimal) (m: int) (n: int) (a: Map<int, int>) (b: Map<int, int>) =
        if A.Count < m || B.Count < n || a.Count < m || b.Count < n then
            ValueNone
        else
            let round = Cent.roundTo (ValueSome <| Round MidpointRounding.AwayFromZero) 2
            let sum1 =
                [1 .. m]
                |> List.sumBy(fun i -> A[i] * ((1m + r) |> powi a[i] |> decimal) |> round)
            let sum2 =
                [1 .. n]
                |> List.sumBy(fun j -> B[j] * ((1m + r) |> powi b[j] |> decimal) |> round)
            sum1 - sum2
            |> ValueSome

    /// calculate the amount of rebate due following an early settlement
    let calculateRebate (principal: int64<Cent>) (payments: (int * int64<Cent>) array) (apr: Percent) (settlementPeriod: int) (settlementPartPeriod: Fraction voption) unitPeriod paymentRounding =
        if payments |> Array.isEmpty then
            ValueNone
        else
            voption {
                let advanceMap = Map [ (1, decimal principal * 1m<Cent>) ]
                let paymentMap = payments |> Array.map(fun (i, p) -> (i, decimal p * 1m<Cent>)) |> Map.ofArray
                let aprDailyRate = apr |> Apr.ukUnitPeriodRate unitPeriod |> Percent.toDecimal |> roundTo (ValueSome <| Round MidpointRounding.AwayFromZero) 6
                let advanceCount = 1
                let paymentCount = payments |> Array.length |> min settlementPeriod
                let advanceIntervalMap = Map [ (1, settlementPeriod) ]
                let paymentIntervalMap = payments |> Array.take paymentCount |> Array.scan(fun state p -> (fst state + 1), (int settlementPeriod - int (fst p))) (0, settlementPeriod) |> Array.tail |> Map.ofArray
                let addPartPeriodTotal (sf: decimal<Cent>) = settlementPartPeriod |> ValueOption.map(fun spp -> sf * ((1m + aprDailyRate) |> powm (spp.toDecimal) |> decimal)) |> ValueOption.defaultValue sf
                let! wholePeriodTotal = ``CCA 2004 regulation 4(1) formula`` advanceMap paymentMap aprDailyRate advanceCount paymentCount advanceIntervalMap paymentIntervalMap
                let settlementFigure = wholePeriodTotal |> addPartPeriodTotal |> Cent.fromDecimalCent paymentRounding
                let remainingPaymentTotal = payments |> Array.filter (fun (i, _) -> i > settlementPeriod) |> Array.sumBy snd
                return remainingPaymentTotal - settlementFigure
            }
