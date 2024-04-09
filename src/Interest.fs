namespace FSharp.Finance.Personal

open System

/// methods for calculating interest and unambiguously expressing interest rates, as well as enforcing regulatory caps on interest chargeable
module Interest =

    open Amount

    /// calculate the interest accrued on a balance at a particular interest rate over a number of days, optionally capped by a daily amount
    let calculate (dailyCap: decimal<Cent>) (balance: int64<Cent>) (dailyRate: Percent) (chargeableDays: int<DurationDay>) =
        decimal balance * Percent.toDecimal dailyRate * decimal chargeableDays
        |> min (decimal dailyCap)
        |> ( * ) 1m<Cent>

    /// the interest rate expressed as either an annual or a daily rate
    [<Struct>]
    type Rate =
        /// the annual interest rate, or the daily interest rate multiplied by 365
        | Annual of Annual:Percent
        /// the daily interest rate, or the annual interest rate divided by 365
        | Daily of Daily:Percent

    [<RequireQualifiedAccess>]
    module Rate =
        /// used to pretty-print the interest rate for debugging
        let serialise = function
        | Annual (Percent air) -> $"AnnualInterestRate{air}pc"
        | Daily (Percent dir) -> $"DailyInterestRate{dir}pc"
        /// calculates the annual interest rate from the daily one
        let annual = function
            | Annual (Percent air) -> air |> Percent
            | Daily (Percent dir) -> dir * 365m |> Percent
        /// calculates the daily interest rate from the annual one
        let daily = function
            | Annual (Percent air) -> air / 365m |> Percent
            | Daily (Percent dir) -> dir |> Percent

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

        /// calculates the daily interest cap
        static member daily (balance: int64<Cent>) (interestChargeableDays: int<DurationDay>) = function
            | ValueSome amount -> Amount.total balance amount * decimal interestChargeableDays
            | ValueNone -> decimal Int64.MaxValue * 1m<Cent>

    /// interest options
    [<Struct>]
    type Options = {
        /// the rate of interest
        Rate: Rate
        /// any total or daily caps on interest
        Cap: Cap
        /// any grace period at the start of a product, if a product is settled before which no interest is payable
        InitialGracePeriod: int<DurationDay>
        /// any date ranges during which no interest is applicable
        Holidays: Holiday array
        /// the interest rate applicable for any period in which a refund is owing
        RateOnNegativeBalance: Rate voption
    }

    /// calculates the number of interest-chargeable days between two dates
    let chargeableDays (startDate: Date) (earlySettlementDate: Date voption) (gracePeriod: int<DurationDay>) holidays (fromDay: int<OffsetDay>) (toDay: int<OffsetDay>) =
        let interestFreeDays =
            holidays
            |> Array.collect(fun (ih: Holiday) ->
                [| (ih.Start - startDate).Days .. (ih.End - startDate).Days |]
            )
            |> Array.filter(fun d -> d >= int fromDay && d <= int toDay)
        let isWithinGracePeriod d = d <= int gracePeriod
        let isSettledWithinGracePeriod = earlySettlementDate |> ValueOption.map(fun sd -> isWithinGracePeriod (sd - startDate).Days) |> ValueOption.defaultValue false
        [| int fromDay .. int toDay |]
        |> Array.filter(fun d -> not (isSettledWithinGracePeriod && isWithinGracePeriod d))
        |> Array.filter(fun d -> interestFreeDays |> Array.exists ((=) d) |> not)
        |> Array.length
        |> fun l -> (max 0 (l - 1)) * 1<DurationDay>

