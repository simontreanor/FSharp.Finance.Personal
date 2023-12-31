namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module Calculation =

    /// holds the result of a devision, separated into quotient and remainder
    [<Struct>]
    type DivisionResult = {
        Quotient: int
        Remainder: decimal
    }

    /// computes the quotient and remainder of two decimal values
    let divRem (left: decimal) (right: decimal) = 
        left % right
        |> fun r -> { Quotient = int(left - r); Remainder = r }

    /// rounds to the nearest number, and when a number is halfway between two others, it's rounded toward the nearest number that's towards zero
    let roundMidpointTowardsZero m =
        divRem m 1m
        |> fun dr -> if dr.Remainder <= 0.5m then dr.Quotient else dr.Quotient + 1

    /// the type of rounding, specifying midpoint-rounding where necessary
    [<Struct>]
    type Rounding =
        | RoundUp
        | RoundDown
        | Round of MidpointRounding

    /// raises a decimal to an int power
    let powi (power: int) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// raises a decimal to a decimal power
    let powm (power: decimal) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// round a percent value to n decimal places
    let roundTo (places: int) (m: decimal) = Math.Round(m, places)

    /// how to round calculated interest and payments
    [<Struct>]
    type RoundingOptions = {
        InterestRounding: Rounding
        PaymentRounding: Rounding
    }
