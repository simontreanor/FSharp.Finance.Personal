namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module Util =

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

    /// the base unit of a currency
    [<Measure>]
    type Cent

    /// utility functions for base currency unit values
    [<RequireQualifiedAccess>]
    module Cent =
        /// max of two cent values
        let max (c1: int<Cent>) (c2: int<Cent>) = min (int c1) (int c2) * 1<Cent>
        /// min of two cent values
        let min (c1: int<Cent>) (c2: int<Cent>) = min (int c1) (int c2) * 1<Cent>
        /// round a decimal value to whole cents
        let round (m: decimal) = int (round m) * 1<Cent>
        /// lower to the base currency unit
        let floor (m: decimal) = int (floor m) * 1<Cent>
        /// lower to the base currency unit
        let fromDecimal (m: decimal) = round (m * 100m)
        /// raise to the standard currency unit
        let toDecimal (c: int<Cent>) = decimal c / 100m

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    type Percent = Percent of decimal

    /// overrides existing power function to take and return decimals
    let inline internal ( ** ) (base': decimal) (power: int) = decimal (Math.Pow(double base', double power))

    /// round a percent value to two decimal places
    let roundTo (places: int) (m: decimal) = Math.Round(m, places)

    /// utility functions for percent values
    [<RequireQualifiedAccess>]
    module Percent =

        /// create a percent value from a decimal
        let fromDecimal (m: decimal) = m * 100m |> Percent
        /// round a percent value to two decimal places
        let round (places: int) (Percent p) = roundTo places p |> Percent
        /// convert a percent value to a decimal
        let toDecimal (Percent p) = p / 100m
        // multiply two percentages together
        let multiply (Percent p1) (Percent p2) = p1 * p2 |> fromDecimal

    /// an offset from the start date
    [<Measure>] type Day

    /// utility functions for days
    [<RequireQualifiedAccess>]
    module Day =
        let todayAsOffset (startDate: DateTime) =
            (DateTime.Today - startDate).Days * 1<Day>

    /// a number of days
    [<Measure>] type Duration

    /// day of month, bug: specifying 29, 30, or 31 means the dates will track the specific day of the month where
    /// possible, otherwise the day will be the last day of the month; so 31 will track the month end; also note that it is
    /// possible to start with e.g. (2024, 02, 31) and this will yield 2024-02-29 29 2024-03-31 2024-04-30 etc.
    [<Measure>]
    type TrackingDay

    [<RequireQualifiedAccess>]
    module TrackingDay =
        /// create a tracking day from an int
        let fromInt (i: int) = i * 1<TrackingDay>

    /// utility functions for arrays
    module Array =
        /// equivalent of Array.last but yields a default value instead of an error if the array is empty
        let lastOrDefault defaultValue a = if Array.isEmpty a then defaultValue else Array.last a
        /// equivalent of Array.maxBy but yields a default value instead of an error if the array is empty
        let maxByOrDefault maxByProp getProp defaultValue a = if Array.isEmpty a then defaultValue else a |> Array.maxBy maxByProp |> getProp
        /// equivalent of Array.unfold but only holding the previous and current state and returning the final state rather than maintaining a full state history
        let solve<'T, 'State> (generator: 'State -> ('T * 'State) option) (state: 'State) =
            let mutable res = (None, None)
            let rec loop state =
                match generator state with
                | None -> ()
                | Some(x, s') ->
                    res <- (snd res, Some x)
                    loop s'

            loop state
            snd res
