namespace FSharp.Finance.Personal

open System

/// a way to unambiguously express percentages and avoid potential confusion with decimal values
module Percentages =

    open Calculation

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    type Percent = Percent of decimal

    /// utility functions for percent values
    [<RequireQualifiedAccess>]
    module Percent =
        /// create a percent value from a decimal
        let fromDecimal (m: decimal) = m * 100m |> Percent
        /// round a percent value to two decimal places
        let round (places: int) (Percent p) = roundTo (MidpointRounding.AwayFromZero |> Round |> ValueSome) places p |> Percent
        /// convert a percent value to a decimal
        let toDecimal (Percent p) = p / 100m
        /// multiply two percentages together
        let multiply (Percent p1) (Percent p2) = p1 * p2 |> fromDecimal
