namespace FSharp.Finance.Personal

open System

/// methods for calculating interest and unambiguously expressing interest rates, as well as enforcing regulatory caps on interest chargeable
module Interest =

    open Amount
    open Calculation
    open Currency
    open DateDay
    open Percentages

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

        /// example settings
        static member example = {
            Total = ValueSome (Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
            Daily = ValueSome (Percentage (Percent 0.8m, ValueNone, ValueNone))
        }

        /// calculates the total interest cap
        static member total (initialPrincipal: int64<Cent>) = function
            | ValueSome amount -> Amount.total initialPrincipal amount
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

    /// interest options
    [<Struct>]
    type Options = {
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
