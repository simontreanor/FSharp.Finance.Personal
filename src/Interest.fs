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

    [<RequireQualifiedAccess>]
    [<Struct>]
    type InterestCap =
        | PercentageOfPrincipal of PercentageOfPrincipal:Percent
        | Daily of Daily:int64<Cent>
        | Fixed of Fixed:int64<Cent>

    /// calculates the overall and daily interest cap
    let calculateInterestCaps (principal: int64<Cent>) interestCap =
        match interestCap with
        | ValueSome(InterestCap.PercentageOfPrincipal percentage) -> decimal principal * Percent.toDecimal percentage |> Cent.floor, Int64.MaxValue * 1L<Cent>
        | ValueSome(InterestCap.Daily i) -> Int64.MaxValue * 1L<Cent>, i
        | ValueSome(InterestCap.Fixed i) -> i, Int64.MaxValue * 1L<Cent>
        | ValueNone -> Int64.MaxValue * 1L<Cent>, Int64.MaxValue * 1L<Cent>

    [<Struct>]
    type InterestHoliday = {
        InterestHolidayStart: DateTime
        InterestHolidayEnd: DateTime
    }
