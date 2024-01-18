namespace FSharp.Finance.Personal

open System

module Interest =

    /// calculate the interest accrued on a balance at a particular interest rate over a number of days, optionally capped bya daily amount
    let calculate (dailyCap: int64<Cent>) (balance: int64<Cent>) (dailyRate: Percent) (chargeableDays: int<DurationDay>) rounding =
        decimal balance * Percent.toDecimal dailyRate * decimal chargeableDays
        |> min (decimal dailyCap) 
        |> Cent.round rounding

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

    /// the maximum interest that can accrue over the full schedule
    [<Struct>]
    type TotalCap =
        /// total interest is capped at a percentage of the initial principal
        | TotalPercentageCap of TotalPercentageCap:Percent
        /// total interest is capped at a fixed amount
        | TotalFixedCap of TotalFixedCap:int64<Cent>

    /// the maximum interest that can accrue over a single day
    [<Struct>]
    type DailyCap =
        /// daily interest is capped at a percentage of the principal balance
        | DailyPercentageCap of DailyPercentageCap:Percent
        /// daily interest is capped at a fixed amount
        | DailyFixedCap of DailyFixedCap:int64<Cent>

    /// the interest cap options
    [<RequireQualifiedAccess; Struct>]
    type Cap = {
        Total: TotalCap voption
        Daily: DailyCap voption
    }

    [<RequireQualifiedAccess>]
    module Cap =

        /// calculates the total interest cap
        let total (initialPrincipal: int64<Cent>) rounding = function
            | ValueSome (TotalPercentageCap percentage) -> decimal initialPrincipal * Percent.toDecimal percentage |> Cent.round rounding
            | ValueSome (TotalFixedCap i) -> i
            | ValueNone -> Int64.MaxValue * 1L<Cent>

        /// calculates the daily interest cap
        let daily (balance: int64<Cent>) (interestChargeableDays: int<DurationDay>) rounding = function
            | ValueSome (DailyPercentageCap percentage) -> decimal balance * Percent.toDecimal percentage * decimal interestChargeableDays |> Cent.round rounding
            | ValueSome (DailyFixedCap i) -> i
            | ValueNone -> Int64.MaxValue * 1L<Cent>

    /// an interest holiday, i.e. a period when no interest is accrued
    [<RequireQualifiedAccess; Struct>]
    type Holiday = {
        Start: Date
        End: Date
    }

    [<Struct>]
    type Options = {
        Rate: Rate
        Cap: Cap
        GracePeriod: int<DurationDay>
        Holidays: Holiday array
        RateOnNegativeBalance: Rate voption
    }

    module Options =
        /// recommended interest options
        let recommended = {
            Rate = Daily (Percent 0.8m)
            Cap = { Total = ValueSome (TotalPercentageCap (Percent 100m)); Daily = ValueSome (DailyPercentageCap (Percent 0.8m)) }
            GracePeriod = 3<DurationDay>
            Holidays = [||]
            RateOnNegativeBalance = ValueSome (Annual (Percent 8m))
        }

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

