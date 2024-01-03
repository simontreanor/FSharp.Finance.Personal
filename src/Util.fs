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

    /// the type of rounding, specifying midpoint-rounding where necessary
    [<Struct>]
    type Rounding =
        | RoundUp
        | RoundDown
        | Round of MidpointRounding

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    type Percent = Percent of decimal

    /// raises a decimal to an int power
    let powi (power: int) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// raises a decimal to a decimal power
    let powm (power: decimal) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// round a percent value to n decimal places
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
    [<Measure>] type DurationDays

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

    /// how to round calculated interest and payments
    [<Struct>]
    type RoundingOptions = {
        InterestRounding: Rounding
        PaymentRounding: Rounding
    }

    /// how to handle the principal balance overpayment (due to rounding) on the final payment of a schedule
    [<Struct>]
    type FinalPaymentAdjustment =
        | AdjustFinalPayment
        | SpreadOverLevelPayments

