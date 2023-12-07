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
        let max (c1: int64<Cent>) (c2: int64<Cent>) = max (int64 c1) (int64 c2) * 1L<Cent>
        /// min of two cent values
        let min (c1: int64<Cent>) (c2: int64<Cent>) = min (int64 c1) (int64 c2) * 1L<Cent>
        /// round a decimal value to whole cents
        let round (m: decimal) = int64 (round m) * 1L<Cent>
        let ceil (m: decimal) = int64 (ceil m) * 1L<Cent>
        let floor (m: decimal) = int64 (floor m) * 1L<Cent>
        /// lower to the base currency unit
        let fromDecimal (m: decimal) = round (m * 100m)
        /// raise to the standard currency unit
        let toDecimal (c: int64<Cent>) = decimal c / 100m

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    type Percent = Percent of decimal

    /// raises a decimal to an int power
    let powi (power: int64) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// raises a decimal to a decimal power
    let powm (power: decimal) (base': decimal) = decimal (Math.Pow(double base', double power))

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

    /// the offset of a date from the start date, in days
    [<Measure>] type OffsetDay

    /// a duration of a number of days
    [<Measure>] type Days

    /// day of month, bug: specifying 29, 30, or 31 means the dates will track the specific day of the month where
    /// possible, otherwise the day will be the last day of the month; so 31 will track the month end; also note that it is
    /// possible to start with e.g. (2024, 02, 31) and this will yield 2024-02-29 29 2024-03-31 2024-04-30 etc.
    [<Measure>]
    type TrackingDay

    [<RequireQualifiedAccess>]
    module TrackingDay =
        /// create a tracking day from an int
        let fromInt (i: int) = i * 1<TrackingDay>
        /// create a date from a year, month, and tracking day
        let toDate y m td = DateTime(y, m, min (DateTime.DaysInMonth(y, m)) td)

    /// utility functions for arrays
    module Array =
        /// gets the last but one member of an array
        let lastButOne a = a |> Array.rev |> Array.tail |> Array.head
        /// equivalent of Array.last but yields a default value instead of an error if the array is empty
        let lastOrDefault defaultValue a = if Array.isEmpty a then defaultValue else Array.last a
        /// equivalent of Array.maxBy but yields a default value instead of an error if the array is empty
        let maxByOrDefault maxByProp getProp defaultValue a = if Array.isEmpty a then defaultValue else a |> Array.maxBy maxByProp |> getProp
        /// equivalent of Array.unfold but only holding the previous and current state and returning the final state rather than maintaining a full state history
        let solve<'T, 'State> (generator: 'State -> ('T * 'State) voption) iterationLimit (state: 'State) =
            let rec loop i res state =
                match i, generator state with
                | i, _ when i = iterationLimit ->
                    res
                | _, ValueNone ->
                    res
                | _, ValueSome(x, s') ->
                    loop (i + 1) (snd res, ValueSome x) s'
            loop 0 (ValueNone, ValueNone) state
            |> snd
