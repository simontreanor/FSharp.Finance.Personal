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
        | Percentage (percentage, ValueSome cap) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
            |> fun cents -> Cent.min cents cap
        | Percentage (percentage, ValueNone) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
        | Simple simple -> simple

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
        | PercentageOfPrincipal percentage -> decimal principal * Percent.toDecimal percentage |> Cent.round
        | Fixed i -> i
