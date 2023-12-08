namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module General =
    
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
