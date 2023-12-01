namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module General =
    
    /// the interest rate expressed as either an annual or a daily rate
    [<Struct>]
    type InterestRate =
        | AnnualInterestRate of AnnualInterestRate:Percent
        | DailyInterestRate of DailyInterestRate:Percent

    module InterestRate =

        let serialise = function
        | AnnualInterestRate (Percent air) -> $"AnnualInterestRate{air}%%"
        | DailyInterestRate (Percent dir) -> $"DailyInterestRate{dir}%%"

    let annualInterestRate = function
        | AnnualInterestRate (Percent ir) -> ir |> Percent
        | DailyInterestRate (Percent ir) -> ir * 365m |> Percent

    let dailyInterestRate = function
        | AnnualInterestRate (Percent ir) -> ir / 365m |> Percent
        | DailyInterestRate (Percent ir) -> ir |> Percent

    /// the type and amount of any product fees, taking into account any constraints
    [<RequireQualifiedAccess>]
    [<Struct>]
    type ProductFees =
        | Percentage of Percentage:Percent * Cap:int<Cent> voption
        | Simple of Simple:int<Cent>

    let productFeesTotal (principal: int<Cent>) productFees =
        match productFees with
        | ValueSome (ProductFees.Percentage (Percent percentage, ValueSome cap)) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
            |> fun cents -> Cent.min cents cap
        | ValueSome (ProductFees.Percentage (Percent percentage, ValueNone)) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
        | ValueSome (ProductFees.Simple simple) -> simple
        | ValueNone -> 0<Cent>

    [<Struct>]
    type ProductFeesSettlement =
        | DueInFull
        | ProRataRefund

    /// the type and amount of penalty charge
    [<Struct>]
    type PenaltyCharge =
        | LatePayment of LatePayment:int<Cent>
        | InsufficientFunds of InsufficientFunds:int<Cent>

    let penaltyChargesTotal penaltyCharges =
        penaltyCharges
        |> Array.sumBy(function LatePayment m | InsufficientFunds m -> m)

    [<RequireQualifiedAccess>]
    [<Struct>]
    type InterestCap =
        | PercentageOfPrincipal of PercentageOfPrincipal:Percent
        | Fixed of int<Cent>

    let calculateInterestCap (principal: int<Cent>) interestCap =
        match interestCap with
        | ValueSome(InterestCap.PercentageOfPrincipal percentage) -> decimal principal * Percent.toDecimal percentage |> Cent.floor
        | ValueSome(InterestCap.Fixed i) -> i
        | ValueNone -> Int32.MaxValue * 1<Cent> // if anyone is charging more than $42,949,672.96 interest, they need "regulating" // note: famous last words // flag: potential year 2100 bug

    [<Struct>]
    type InterestHoliday = {
        InterestHolidayStart: DateTime
        InterestHolidayEnd: DateTime
    }
