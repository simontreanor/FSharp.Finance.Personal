namespace FSharp.Finance

open System

[<AutoOpen>]
module Util =

    [<Struct>]
    type DivisionResult = {
        Quotient: decimal
        Remainder: decimal
    }

    /// computes the quotient and remainder of two decimal values
    let divRem (left: decimal) (right: decimal) = 
        left % right
        |> fun r -> { Quotient = left - r; Remainder = r }

    /// rounds to the nearest number, and when a number is halfway between two others, it's rounded toward the nearest number that's towards zero
    let roundMidpointTowardsZero m =
        divRem m 1m
        |> fun dr -> if dr.Remainder <= 0.5m then dr.Quotient else dr.Quotient + 1m

    /// the base unit of a currency
    [<Measure>]
    type Cent

    [<RequireQualifiedAccess>]
    module Cent =
        let max (c1: int<Cent>) (c2: int<Cent>) = Int32.Max(int c1, int c2) * 1<Cent>
        let min (c1: int<Cent>) (c2: int<Cent>) = Int32.Min(int c1, int c2) * 1<Cent>
        let round (m: decimal) = int (Decimal.Round(m, 0, MidpointRounding.ToEven)) * 1<Cent>
        /// lower to the base current unit
        let fromDecimal (m: decimal) = round (m * 100m)
        /// raise to the standard currency unit
        let toDecimal (c: int<Cent>) = decimal c / 100m

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    [<Measure>]
    type Percent

    [<RequireQualifiedAccess>]
    module Percent =
        let fromDecimal (m: decimal) = m * 100m<Percent>
        let round (places: int) (p: decimal<Percent>) = Decimal.Round(decimal p, places, MidpointRounding.ToEven) * 1m<Percent>
        let toDecimal (p: decimal<Percent>) = p / 100m<Percent>

    /// an offset from the start date
    [<Measure>] type Day

    /// a number of days
    [<Measure>] type Duration
