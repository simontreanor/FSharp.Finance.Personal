namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module General =
    
    /// the interest rate expressed as either an annual or a daily rate
    [<Struct>]
    type InterestRate =
        | AnnualInterestRate of AnnualInterestRate:decimal<Percent>
        | DailyInterestRate of DailyInterestRate:decimal<Percent>

    let dailyInterestRate = function
        | AnnualInterestRate ir -> ir / 365m
        | DailyInterestRate ir -> ir

    /// the type and amount of any product fees, taking into account any constraints
    [<Struct>]
    type ProductFees =
        | Percentage of Percentage:decimal<Percent> * Cap:int<Cent> voption
        | Simple of Simple:int<Cent>

    let productFeesTotal (principal: int<Cent>) productFees =
        match productFees with
        | ValueSome (Percentage (percentage, ValueSome cap)) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
            |> fun cents -> Cent.min cents cap
        | ValueSome (Percentage (percentage, ValueNone)) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
        | ValueSome (Simple simple) -> simple
        | ValueNone -> 0<Cent>

    /// the type and amount of penalty charge
    [<Struct>]
    type PenaltyCharge =
        | LatePayment of LatePayment:int<Cent>
        | InsufficientFunds of InsufficientFunds:int<Cent>

    let penaltyChargesTotal penaltyCharges =
        penaltyCharges
        |> Array.sumBy(function LatePayment m | InsufficientFunds m -> m)

    [<Struct>]
    type InterestCap =
        | PercentageOfPrincipal of PercentageOfPrincipal:decimal<Percent>
        | Fixed of int<Cent>

    let calculateInterestCap (principal: int<Cent>) interestCap =
        match interestCap with
        | ValueSome(PercentageOfPrincipal percentage) -> decimal principal * Percent.toDecimal percentage |> Cent.round
        | ValueSome(Fixed i) -> i
        | ValueNone -> Int32.MaxValue * 1<Cent> // if anyone is charging more than $42,949,672.96 interest, they need "regulating" // note: famous last words // flag: potential year 2100 bug
