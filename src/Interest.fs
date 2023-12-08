namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module Interest =

    let calculateInterest (dailyInterestCap: int64<Cent>) (balance: int64<Cent>) (dailyInterestRate: Percent) (interestChargeableDays: int<Days>) =
        decimal balance * Percent.toDecimal dailyInterestRate * decimal interestChargeableDays
        |> min (decimal dailyInterestCap) 
        |> Cent.floor

    /// the interest rate expressed as either an annual or a daily rate
    [<Struct>]
    type InterestRate =
        | AnnualInterestRate of AnnualInterestRate:Percent
        | DailyInterestRate of DailyInterestRate:Percent

    [<RequireQualifiedAccess>]
    module InterestRate =

        let serialise = function
        | AnnualInterestRate (Percent air) -> $"AnnualInterestRate{air}pc"
        | DailyInterestRate (Percent dir) -> $"DailyInterestRate{dir}pc"

        let annual = function
            | AnnualInterestRate (Percent air) -> air |> Percent
            | DailyInterestRate (Percent dir) -> dir * 365m |> Percent

        let daily = function
            | AnnualInterestRate (Percent air) -> air / 365m |> Percent
            | DailyInterestRate (Percent dir) -> dir |> Percent

    [<Struct>]
    type TotalInterestCap =
        | TotalPercentageCap of TotalPercentageCap:Percent
        | TotalFixedCap of TotalFixedCap:int64<Cent>

    [<Struct>]
    type DailyInterestCap =
        | DailyPercentageCap of DailyPercentageCap:Percent
        | DailyFixedCap of DailyFixedCap:int64<Cent>

    [<Struct>]
    type InterestCap = {
        TotalCap: TotalInterestCap voption
        DailyCap: DailyInterestCap voption
    }

    [<RequireQualifiedAccess>]
    module InterestCap =

        /// calculates the total interest cap
        let totalCap (initialPrincipal: int64<Cent>) = function
            | ValueSome (TotalPercentageCap percentage) -> decimal initialPrincipal * Percent.toDecimal percentage |> Cent.floor
            | ValueSome (TotalFixedCap i) -> i
            | ValueNone -> Int64.MaxValue * 1L<Cent>

        /// calculates the daily interest cap
        let dailyCap (balance: int64<Cent>) (interestChargeableDays: int<Days>) = function
            | ValueSome (DailyPercentageCap percentage) -> decimal balance * Percent.toDecimal percentage * decimal interestChargeableDays |> Cent.floor
            | ValueSome (DailyFixedCap i) -> i
            | ValueNone -> Int64.MaxValue * 1L<Cent>

    [<Struct>]
    type InterestHoliday = {
        InterestHolidayStart: DateTime
        InterestHolidayEnd: DateTime
    }
