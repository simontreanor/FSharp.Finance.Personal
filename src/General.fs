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
        | AnnualInterestRate (Percent air) -> $"AnnualInterestRate{air}pc"
        | DailyInterestRate (Percent dir) -> $"DailyInterestRate{dir}pc"

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
        | Percentage of Percentage:Percent * Cap:int64<Cent> voption
        | Simple of Simple:int64<Cent>

    let productFeesTotal (principal: int64<Cent>) productFees =
        match productFees with
        | ValueSome (ProductFees.Percentage (Percent percentage, ValueSome cap)) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int64 m * 1L<Cent>)
            |> fun cents -> Cent.min cents cap
        | ValueSome (ProductFees.Percentage (Percent percentage, ValueNone)) ->
            Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int64 m * 1L<Cent>)
        | ValueSome (ProductFees.Simple simple) -> simple
        | ValueNone -> 0L<Cent>

    [<Struct>]
    type ProductFeesSettlement =
        | DueInFull
        | ProRataRefund

    /// the type and amount of penalty charge
    [<Struct>]
    type PenaltyCharge =
        | LatePayment of LatePayment:int64<Cent>
        | InsufficientFunds of InsufficientFunds:int64<Cent>

    let penaltyChargesTotal penaltyCharges =
        penaltyCharges
        |> Array.sumBy(function LatePayment m | InsufficientFunds m -> m)

    [<RequireQualifiedAccess>]
    [<Struct>]
    type InterestCap =
        | PercentageOfPrincipal of PercentageOfPrincipal:Percent
        | Fixed of int64<Cent>

    let calculateInterestCap (principal: int64<Cent>) interestCap =
        match interestCap with
        | ValueSome(InterestCap.PercentageOfPrincipal percentage) -> decimal principal * Percent.toDecimal percentage |> Cent.floor
        | ValueSome(InterestCap.Fixed i) -> i
        | ValueNone -> Int64.MaxValue * 1L<Cent>

    [<Struct>]
    type InterestHoliday = {
        InterestHolidayStart: DateTime
        InterestHolidayEnd: DateTime
    }
