namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module Currency =

    /// the base unit of a currency (cent, penny, øre etc.)
    [<Measure>]
    type Cent

    /// utility functions for base currency unit values
    [<RequireQualifiedAccess>]
    module Cent =
        /// max of two cent values
        let max (c1: int64<Cent>) (c2: int64<Cent>) = max (int64 c1) (int64 c2) * 1L<Cent>
        /// min of two cent values
        let min (c1: int64<Cent>) (c2: int64<Cent>) = min (int64 c1) (int64 c2) * 1L<Cent>
        /// derive a rounded cent value from a decimal according to the specified rounding method
        let round rounding (m: decimal) =
            match rounding with
            | RoundDown -> floor m
            | RoundUp -> ceil m
            | Round mpr -> Math.Round(m, 0, mpr)
            |> int64
            |> (( * ) 1L<Cent>)
        /// lower to the base currency unit
        let fromDecimal (m: decimal) = round (Round MidpointRounding.AwayFromZero) (m * 100m)
        /// raise to the standard currency unit
        let toDecimal (c: int64<Cent>) = decimal c / 100m
