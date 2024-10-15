namespace FSharp.Finance.Personal

open System

/// a way to unambiguously express percentages and avoid potential confusion with decimal values
module Util =

    open Calculation
    open Currency

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    type Percent = Percent of decimal

    /// utility functions for percent values
    [<RequireQualifiedAccess>]
    module Percent =
        /// create a percent value from a decimal
        let fromDecimal (m: decimal) = m * 100m |> Percent
        /// round a percent value to two decimal places
        let round (places: int) (Percent p) = Rounding.roundTo (RoundWith MidpointRounding.AwayFromZero) places p |> Percent
        /// convert a percent value to a decimal
        let toDecimal (Percent p) = p / 100m
        /// multiply two percentages together
        let multiply (Percent p1) (Percent p2) = p1 * p2 |> fromDecimal

    /// the type of restriction placed on a possible value
    [<RequireQualifiedAccess; Struct>]
    type Restriction =
        /// does not constrain values at all
        | NoLimit
        /// prevent values below a certain limit
        | LowerLimit of LowerLimit:int64<Cent>
        /// prevent values above a certain limit
        | UpperLimit of UpperLimit:int64<Cent>
        /// constrain values to within a range
        | WithinRange of MinValue:int64<Cent> * MaxValue:int64<Cent>

    module Restriction =
        /// calculate a permitted value based on a restriction
        let calculate restriction value =
            match restriction with
            | Restriction.NoLimit -> value
            | Restriction.LowerLimit a -> value |> max (decimal a)
            | Restriction.UpperLimit a -> value |> min (decimal a)
            | Restriction.WithinRange (lower, upper) -> value |> min (decimal upper) |> max (decimal lower)

    /// an amount specified either as a simple amount or as a percentage of another amount, optionally restricted to lower and/or upper limits
    [<RequireQualifiedAccess; Struct>]
    type Amount =
        /// a percentage of the principal, optionally restricted
        | Percentage of Percentage:Percent * Restriction:Restriction * Rounding:Rounding
        /// a fixed fee
        | Simple of Simple:int64<Cent>

    module Amount =
        /// calculates the total amount based on any restrictions
        let total (baseValue: int64<Cent>) amount =
            match amount with
            | Amount.Percentage (Percent percentage, restriction, rounding) ->
                decimal baseValue * decimal percentage / 100m
                |> Restriction.calculate restriction
                |> Rounding.round rounding
            | Amount.Simple simple ->
                decimal simple
            |> ( * ) 1m<Cent>
