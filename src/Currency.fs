namespace FSharp.Finance.Personal

open System

/// a unit-of-measure and associated functions for a base currency unit to represent real nonfractional currency amounts
module Currency =

    open Calculation

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
            m
            |> Rounding.round rounding
            |> int64
            |> ( * ) 1L<Cent>

        /// round a decimal cent value to the specified number of places
        let roundTo rounding decimalPlaces (m: decimal<Cent>) =
            m
            |> decimal
            |> Rounding.roundTo rounding decimalPlaces
            |> ( * ) 1m<Cent>

        /// lower to the base currency unit, e.g. $12.34 -> 1234¢
        let fromDecimal (m: decimal) = round (RoundWith MidpointRounding.AwayFromZero) (m * 100m)

        /// raise to the standard currency unit, e.g. 1234¢ -> $12.34
        let toDecimal (c: int64<Cent>) = decimal c / 100m

        /// convert a decimal cent value to an integer cent value, dropping any fractional value, 1234.5678¢ -> 1234¢
        let fromDecimalCent rounding (c: decimal<Cent>) = c |> decimal |> round rounding
        
        /// convert an integer cent value to a decimal cent value, e.g. for precise interest calculation, 1234¢ -> 1234.0000¢
        let toDecimalCent (c: int64<Cent>) = decimal c * 1m<Cent>
