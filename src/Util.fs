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

    /// rounds decimals to 2 places, to represent exact money values
    let roundMoney m =
        Decimal.Round(m, 2, MidpointRounding.ToEven)
