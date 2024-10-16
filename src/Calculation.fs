namespace FSharp.Finance.Personal

open System

/// convenience functions and options to help with calculations
module Calculation =

    open DateDay

    /// the intended purpose of the calculation
    [<RequireQualifiedAccess; Struct>]
    type IntendedPurpose =
        /// intended to quote a settlement figure on the specified day
        | SettlementOn of SettlementDay: int<OffsetDay>
        /// intended to quote a settlement figure on the as-of day
        | SettlementOnAsOfDay
        /// intended just for information, e.g. to view the current status of a loan
        | Statement

    /// holds the result of a division, separated into quotient and remainder
    [<Struct>]
    type DivisionResult = {
        /// the whole number resulting from a division
        Quotient: int
        /// the fractional number remaining after subtracting the quotient from the result
        Remainder: decimal
    }

    /// computes the quotient and remainder of two decimal values
    let divRem (left: decimal) (right: decimal) = 
        left % right
        |> fun r -> { Quotient = int(left - r); Remainder = r }

    /// rounds to the nearest number, and when a number is halfway between two others, it's rounded toward the nearest number that's towards 0L<Cent>
    let roundMidpointTowardsZero m =
        divRem m 1m
        |> fun dr -> if dr.Remainder <= 0.5m then dr.Quotient else dr.Quotient + 1

    /// raises a decimal to an int power
    let internal powi (power: int) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// raises a decimal to a decimal power
    let internal powm (power: decimal) (base': decimal) = Math.Pow(double base', double power)

    /// the type of rounding, specifying midpoint-rounding where necessary
    [<Struct>]
    type Rounding =
        /// do not round at all
        | NoRounding
        /// round up to the specified precision (= ceiling)
        | RoundUp
        /// round down to the specified precision (= floor)
        | RoundDown
        /// round up or down to the specified precision based on the given midpoint rounding rules
        | RoundWith of MidpointRounding

    /// the type of rounding, specifying midpoint-rounding where necessary
    module Rounding =
        /// derive a rounded value from a decimal according to the specified rounding method
        let round rounding (m: decimal) =
            match rounding with
            | NoRounding -> m
            | RoundDown -> floor m
            | RoundUp -> ceil m
            | RoundWith mpr -> Math.Round(m, 0, mpr)

        /// round a value to n decimal places
        let roundTo rounding (places: int) (m: decimal) =
            match rounding with
            | NoRounding -> m
            | RoundDown -> 10m |> powi places |> fun f -> if f = 0m then 0m else m * f |> floor |> fun m -> m / f
            | RoundUp -> 10m |> powi places |> fun f -> if f = 0m then 0m else m * f |> ceil |> fun m -> m / f
            | RoundWith mpr -> Math.Round(m, places, mpr)

    /// a holiday, i.e. a period when no interest and/or charges are accrued
    [<RequireQualifiedAccess; Struct>]
    type DateRange = {
        /// the first date of the holiday period
        Start: Date
        /// the last date of the holiday period
        End: Date
    }

    /// determines whether a pending payment has timed out
    let timedOut paymentTimeout (asOfDay: int<OffsetDay>) (paymentDay: int<OffsetDay>) =
        (int asOfDay - int paymentDay) * 1<DurationDay> > paymentTimeout

    /// a fraction expressed as a numerator and denominator
    [<RequireQualifiedAccess; Struct>]
    type Fraction =
        | Zero
        | Simple of Numerator: int * Denominator: int

    /// a fraction expressed as a numerator and denominator
    module Fraction =
        let toDecimal = function
            | Fraction.Zero ->
                0m
            | Fraction.Simple (numerator, denominator) ->
                if denominator = 0 then 0m else decimal numerator / decimal denominator
