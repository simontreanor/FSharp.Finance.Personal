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

    /// the base unit of a currency (cent, penny, øre etc.)
    [<Measure>]
    type Cent

    /// utility functions for base currency unit values
    [<RequireQualifiedAccess>]
    module Cent =
        /// max of two cent values
        let max (c1: int64<Cent>) (c2: int64<Cent>) = max (int64 c1) (int64 c2) * 1L<Cent>

        /// min of two cent values
        let min (c1: int64<Cent>) (c2: int64<Cent>) = min (int64 c1) (int64 c2) * 1L<Cent>

        /// derive a rounded cent value from a decimal according to the specified rounding method
        let round rounding (m: decimal) =
            m
            |> Rounding.round rounding
            |> int64
            |> ( * ) 1L<Cent>

        /// round a decimal cent value to the specified number of places
        let roundTo rounding decimalPlaces (m: decimal<Cent>) =
            m
            |> decimal
            |> Rounding.roundTo rounding decimalPlaces
            |> ( * ) 1m<Cent>

        /// lower to the base currency unit, e.g. $12.34 -> 1234¢
        let fromDecimal (m: decimal) = round (RoundWith MidpointRounding.AwayFromZero) (m * 100m)

        /// raise to the standard currency unit, e.g. 1234¢ -> $12.34
        let toDecimal (c: int64<Cent>) = decimal c / 100m

        /// convert a decimal cent value to an integer cent value, rounding as appropriate, e.g. 1234.5678¢ -> 1234¢ or 1235¢
        let fromDecimalCent rounding (c: decimal<Cent>) = c |> decimal |> round rounding
        
        /// convert an integer cent value to a decimal cent value, e.g. for precise interest calculation, 1234¢ -> 1234.0000¢
        let toDecimalCent (c: int64<Cent>) = decimal c * 1m<Cent>

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

    /// the type of restriction placed on a possible value
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

    /// an amount specified either as a simple amount or as a percentage of another amount, optionally restricted to lower and/or upper limits
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

    /// the result obtained from the array solver
    [<RequireQualifiedAccess; Struct>]
    type Solution =
        /// a solution could not be found due to an issue with the initial parameters
        | Impossible
        /// a solution could not be found within the iteration limit, but it returns the result of the last iteration and stats on how it was reached
        | IterationLimitReached of PartialSolution:decimal * IterationLimit:int * MaxTolerance:int
        /// a solution was found, returning the solution, the number of iterations required and the final tolerance used
        | Found of Found:decimal * Iteration:int * Tolerance:int
        /// the solver was bypassed and a manual solution was supplied
        | Bypassed

    /// lower and upper bounds, as well as a step value, for tolerance when using the solver
    [<RequireQualifiedAccess; Struct>]
    type ToleranceSteps = {
        /// the initial tolerance value
        Min: int
        /// the step by which to change the tolerance value
        Step: int
        /// the final tolerance value
        Max: int
    }
    
    /// lower and upper bounds, as well as a step value, for tolerance when using the solver
    module ToleranceSteps =
        /// no tolerance steps
        let zero =
            { ToleranceSteps.Min = 0; ToleranceSteps.Step = 0; ToleranceSteps.Max = 0 }
        /// tolerance steps for solving for payment value
        let  forPaymentValue paymentCount =
            { ToleranceSteps.Min = 0; ToleranceSteps.Step = paymentCount; ToleranceSteps.Max = paymentCount * 4 }

    /// what range of values the solver should aim for
    [<Struct>]
    type ToleranceOption =
        /// find a solution less than or equal to 0L<Cent>
        | BelowZero
        /// find a solution either side of 0L<Cent>
        | AroundZero
        /// find a solution greater than or equal to 0L<Cent>
        | AboveZero

    /// functions for working with arrays
    module Array =
        /// iteratively solves for a given input using a generator function until the output is 0L<Cent> or within a set tolerance,
        /// optionally relaxing the tolerance until a solution is found
        [<TailCall>]
        let solve (generator: decimal -> decimal) iterationLimit approximation toleranceOption (toleranceSteps: ToleranceSteps) =
            let rec loop i lowerBound upperBound tolerance =
                let midRange =
                    let x = (upperBound - lowerBound) / 2m
                    if x = upperBound then upperBound * 2m
                    elif x = lowerBound then lowerBound / 2m
                    else x
                let newBound = lowerBound + midRange
                if i = iterationLimit then
                    if tolerance = toleranceSteps.Max then
                        Solution.IterationLimitReached (newBound, i, tolerance)
                    else
                        let newTolerance = min toleranceSteps.Max (tolerance + toleranceSteps.Step)
                        loop 0 0m (approximation * 100m) newTolerance
                else
                    let difference = generator newBound
                    let lowerTolerance, upperTolerance =
                        match toleranceOption with
                        | BelowZero -> decimal -tolerance, 0m
                        | AroundZero -> decimal -tolerance, decimal tolerance
                        | AboveZero -> 0m, decimal tolerance
                    if difference >= lowerTolerance && difference <= upperTolerance then
                        Solution.Found(newBound, i, tolerance)
                    elif difference > upperTolerance then
                        loop (i + 1) newBound upperBound tolerance
                    else //difference < lowerTolerance
                        loop (i + 1) lowerBound newBound tolerance
            loop 0 0m (approximation * 100m) toleranceSteps.Min // to do: improve approximation

    /// functions for working with maps
    module Map =

        /// creates a map from an array of key-value tuples with array values
        let ofArrayWithMerge (array: ('a * 'b array) array) =
            array
            |> Array.groupBy fst
            |> Array.map(fun (k, v) -> k, Array.collect snd v)
            |> Map.ofArray
