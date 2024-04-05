namespace FSharp.Finance.Personal

open System

/// convenience functions and options to help with calculations
[<AutoOpen>]
module Calculation =

    /// the type of settlement quote requested
    [<Struct>]
    type QuoteType =
        /// calculate the single final payment required to settle in full
        | Settlement
        /// calculate the single final payment required to settle in full on a future day
        | FutureSettlement of SettlementDay: int<OffsetDay>
        /// get the first outstanding payment
        | FirstOutstanding
        /// calculate the total of all overdue payments
        | AllOverdue

    /// the intended purpose of the calculation
    [<RequireQualifiedAccess; Struct>]
    type IntendedPurpose =
        /// intended to quote a calculated amount, e.g. for settlement purposes
        | Quote of QuoteType: QuoteType
        /// intended just for information, e.g. to view the current status of a loan
        | Statement

    /// whether to apply interest on a negative balance (i.e. while a refund is owing)
    [<Struct>]
    type NegativeInterestOption =
        /// apply negative interest (e.g. statutory interest on outstanding refunds)
        | ApplyNegativeInterest
        /// do not apply negative interest (e.g. if the customer has volutarily overpaid)
        | DoNotApplyNegativeInterest

    /// holds the result of a devision, separated into quotient and remainder
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

    /// the type of rounding, specifying midpoint-rounding where necessary
    [<Struct>]
    type Rounding =
        /// round up to the specified precision (= ceiling)
        | RoundUp
        /// round down to the specified precision (= floor)
        | RoundDown
        /// round up or down to the specified precision based on the given midpoint rounding rules
        | Round of MidpointRounding
        with
            /// derive a rounded value from a decimal according to the specified rounding method
            static member round rounding (m: decimal) =
                match rounding with
                | ValueSome (RoundDown) -> floor m
                | ValueSome (RoundUp) -> ceil m
                | ValueSome (Round mpr) -> Math.Round(m, 0, mpr)
                | ValueNone -> m

    /// raises a decimal to an int power
    let powi (power: int) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// raises a decimal to a decimal power
    let powm (power: decimal) (base': decimal) = decimal (Math.Pow(double base', double power))

    /// round a value to n decimal places
    let roundTo (places: int) (m: decimal) = Math.Round(m, places)

    /// how to round calculated interest and payments
    [<Struct>]
    type RoundingOptions = {
        /// how to round charges
        ChargesRounding: Rounding
        /// how to round fees
        FeesRounding: Rounding
        /// how to round interest
        InterestRounding: Rounding
        /// how to round payments
        PaymentRounding: Rounding
    }
    with
        static member recommended = {
            ChargesRounding = RoundDown
            FeesRounding = RoundDown
            InterestRounding = RoundDown
            PaymentRounding = RoundUp
        }

    /// a holiday, i.e. a period when no interest and/or charges are accrued
    [<RequireQualifiedAccess; Struct>]
    type Holiday = {
        /// the first date of the holiday period
        Start: Date
        /// the last date of the holiday period
        End: Date
    }

    /// determines whether a pending payment has timed out
    let timedOut paymentTimeout (asOfDay: int<OffsetDay>) (paymentDay: int<OffsetDay>) =
        (int asOfDay - int paymentDay) * 1<DurationDay> > paymentTimeout
