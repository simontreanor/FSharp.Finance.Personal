namespace FSharp.Finance.Personal

open System

/// convenience functions and options to help with calculations
module Calculation =

    open DateDay

    /// holds the result of a division, separated into quotient and remainder
    [<Struct>]
    type DivisionResult = {
        /// the whole number resulting from a division
        Quotient: int
        /// the fractional number remaining after subtracting the quotient from the result
        Remainder: decimal
    }

    /// computes the quotient and remainder of two decimal values
    let divRem left right =
        left % right
        |> fun r -> {
            Quotient = int (left - r)
            Remainder = r
        }

    /// rounds to the nearest number, and when a number is halfway between two others, it's rounded toward the nearest number that's towards 0L<Cent>
    let roundMidpointTowardsZero m =
        divRem m 1m
        |> fun dr ->
            if dr.Remainder <= 0.5m then
                dr.Quotient
            else
                dr.Quotient + 1

    /// raises a decimal to an int power
    let internal powi (power: int) (base': decimal) =
        decimal (Math.Pow(double base', double power))

    /// raises a decimal to a decimal power
    let internal powm (power: decimal) (base': decimal) = Math.Pow(double base', double power)

    /// the type of rounding, specifying midpoint-rounding where necessary
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type Rounding =
        /// do not round at all
        | NoRounding
        /// round up to the specified precision (= ceiling)
        | RoundUp
        /// round down to the specified precision (= floor)
        | RoundDown
        /// round up or down to the specified precision based on the given midpoint rounding rules
        | RoundWith of MidpointRounding

        /// HTML formatting to display the rounding in a readable format
        member r.Html =
            match r with
            | NoRounding -> "not rounded"
            | RoundUp -> "rounded up"
            | RoundDown -> "rounded down"
            | RoundWith mpr -> $"round using {mpr}"

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
        let roundTo rounding places (m: decimal) =
            match rounding with
            | NoRounding -> m
            | RoundDown ->
                10m
                |> powi places
                |> fun f -> (if f = 0m then 0m else m * f) |> floor |> (fun m -> m / f)
            | RoundUp ->
                10m
                |> powi places
                |> fun f -> (if f = 0m then 0m else m * f) |> ceil |> (fun m -> m / f)
            | RoundWith mpr -> Math.Round(m, places, mpr)

    /// a holiday, i.e. a period when no interest and/or charges are accrued
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type DateRange = {
        /// the first date of the holiday period
        DateRangeStart: Date
        /// the last date of the holiday period
        DateRangeEnd: Date
    } with

        /// HTML formatting to display the date range in a readable format
        member dr.Html = $"%A{dr.DateRangeStart} to %A{dr.DateRangeEnd}"

    /// determines whether a pending payment has timed out
    let isTimedOut paymentTimeout evaluationDay paymentDay =
        (OffsetDay.toInt evaluationDay - OffsetDay.toInt paymentDay) * 1<DurationDay> > paymentTimeout

    /// a fraction
    [<RequireQualifiedAccess; Struct>]
    type Fraction =
        /// no fraction
        | Zero
        /// a simple fraction expressed as a numerator and denominator
        | Simple of Numerator: int * Denominator: int

    /// a fraction expressed as a numerator and denominator
    module Fraction =
        let toDecimal =
            function
            | Fraction.Zero -> 0m
            | Fraction.Simple(numerator, denominator) ->
                if denominator = 0 then
                    0m
                else
                    decimal numerator / decimal denominator

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
            m |> Rounding.round rounding |> int64 |> (*) 1L<Cent>

        /// round a decimal cent value to the specified number of places
        let roundTo rounding decimalPlaces (m: decimal<Cent>) =
            m |> decimal |> Rounding.roundTo rounding decimalPlaces |> (*) 1m<Cent>

        /// lower to the base currency unit, e.g. $12.34 -> 1234¢
        let fromDecimal (m: decimal) =
            round (RoundWith MidpointRounding.AwayFromZero) (m * 100m)

        /// raise to the standard currency unit, e.g. 1234¢ -> $12.34
        let toDecimal (c: int64<Cent>) = decimal c / 100m
        /// convert a decimal cent value to an integer cent value, rounding as appropriate, e.g. 1234.5678¢ -> 1234¢ or 1235¢
        let fromDecimalCent rounding (c: decimal<Cent>) = c |> decimal |> round rounding
        /// convert an integer cent value to a decimal cent value, e.g. for precise interest calculation, 1234¢ -> 1234.0000¢
        let toDecimalCent (c: int64<Cent>) = decimal c * 1m<Cent>

    /// a percentage, e.g. 42%, as opposed to its decimal representation 0.42m
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type Percent =
        | Percent of decimal

        /// HTML formatting to display the percentage in a readable format
        member p.Html =
            let (Percent value) = p
            let format = if value < 1e-03m then "F10" else "G10"
            $"{value.ToString format} %%"

    /// utility functions for percent values
    [<RequireQualifiedAccess>]
    module Percent =
        /// create a percent value from a decimal, e.g. 0.5 -> 50%
        let fromDecimal (m: decimal) = m * 100m |> Percent

        /// round a percent value to the specified number of decimal places
        let round places (Percent percent) =
            Rounding.roundTo (RoundWith MidpointRounding.AwayFromZero) places percent
            |> Percent

        /// convert a percent value to a decimal, e.g. 50% -> 0.5
        let toDecimal (Percent percent) = percent / 100m

    /// the type of restriction placed on a possible value
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type Restriction =
        /// does not constrain values at all
        | NoLimit
        /// prevent values below a certain limit
        | LowerLimit of int64<Cent>
        /// prevent values above a certain limit
        | UpperLimit of int64<Cent>
        /// constrain values to within a range
        | WithinRange of MinValue: int64<Cent> * MaxValue: int64<Cent>

        /// HTML formatting to display the restriction in a readable format
        member r.Html =
            match r with
            | NoLimit -> ""
            | LowerLimit lower -> $"min {Cent.toDecimal lower:N2}"
            | UpperLimit upper -> $"max {Cent.toDecimal upper:N2}"
            | WithinRange(lower, upper) -> $"min {Cent.toDecimal lower:N2} max {Cent.toDecimal upper:N2}"

    /// the type of restriction placed on a possible value
    module Restriction =
        /// calculate a permitted value based on a restriction
        let calculate restriction value =
            match restriction with
            | Restriction.NoLimit -> value
            | Restriction.LowerLimit a -> value |> max (decimal a)
            | Restriction.UpperLimit a -> value |> min (decimal a)
            | Restriction.WithinRange(lower, upper) -> value |> min (decimal upper) |> max (decimal lower)

    /// an amount specified either as a simple amount or as a percentage of another amount, optionally restricted to lower and/or upper limits
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type Amount =
        /// a percentage of the principal, optionally restricted
        | Percentage of Percent * Restriction
        /// a fixed fee
        | Simple of int64<Cent>
        /// nothing
        | Unlimited

        /// HTML formatting to display the amount in a readable format
        member a.Html =
            match a with
            | Percentage(Percent percent, restriction) -> $"{percent} %% {restriction}".Trim()
            | Simple simple -> $"{Cent.toDecimal simple:N2}"
            | Unlimited -> "<i>n/a</i>"

    /// an amount specified either as a simple amount or as a percentage of another amount, optionally restricted to lower and/or upper limits
    module Amount =
        /// calculates the total amount based on any restrictions
        let total (baseValue: int64<Cent>) amount =
            match amount with
            | Amount.Percentage(percent, restriction) ->
                decimal baseValue * Percent.toDecimal percent
                |> Restriction.calculate restriction
            | Amount.Simple simple -> decimal simple
            | Amount.Unlimited -> decimal baseValue
            |> (*) 1m<Cent>

    /// the result obtained from the array solver
    [<RequireQualifiedAccess; Struct>]
    type Solution =
        /// a solution could not be found due to an issue with the initial parameters
        | Impossible
        /// a solution could not be found within the iteration limit, but it returns the result of the last iteration and stats on how it was reached
        | IterationLimitReached of PartialSolution: decimal * IterationLimit: int * MaxTolerance: decimal
        /// a solution was found, returning the solution, the number of iterations required and the final tolerance used
        | Found of decimal * Iteration: int * Tolerance: decimal

    /// lower and upper bounds, as well as a step value, for tolerance when using the solver
    [<Struct>]
    type ToleranceSteps = {
        /// the initial tolerance value
        MinTolerance: decimal
        /// the step by which to change the tolerance value
        ToleranceStep: decimal
        /// the final tolerance value
        MaxTolerance: decimal
    }

    /// lower and upper bounds, as well as a step value, for tolerance when using the solver
    module ToleranceSteps =
        /// no tolerance steps
        let zero = {
            ToleranceSteps.MinTolerance = 0m
            ToleranceSteps.ToleranceStep = 0m
            ToleranceSteps.MaxTolerance = 0m
        }

        /// tolerance steps for solving for APR
        let forApr = {
            ToleranceSteps.MinTolerance = 1e-6m
            ToleranceSteps.ToleranceStep = 1e-6m
            ToleranceSteps.MaxTolerance = 1e-3m
        }

        /// tolerance steps for solving for payment value
        let forPaymentValue paymentCount = {
            ToleranceSteps.MinTolerance = 0m
            ToleranceSteps.ToleranceStep = decimal paymentCount
            ToleranceSteps.MaxTolerance = decimal <| paymentCount * 4
        }

    /// what range of values the solver should aim for
    [<Struct>]
    type TargetTolerance =
        /// find a solution less than or equal to zero
        | BelowZero
        /// find a solution either side of zero
        | AroundZero
        /// find a solution greater than or equal to zero
        | AboveZero

    /// functions for working with arrays
    module Array =
        /// iteratively solves for a given input using a generator function until the output hits zero or within a set tolerance,
        /// optionally relaxing the tolerance until a solution is found
        /// note: the generator function should return a tuple of the result and a relevant value (as the result is converging on zero it is not a very relevant value)
        let solveBisection generator (iterationLimit: uint) initialGuess targetTolerance toleranceSteps =
            let initialLowerBound, initialUpperBound =
                initialGuess * 0.75m, initialGuess * 1.25m
            // recursively iterate through possible solutions
            let rec loop iteration lowerBound upperBound tolerance =
                // find the midpoint of the bounds
                let midpoint = (lowerBound + upperBound) / 2m
                // if within the iteration limit
                if iteration <= int iterationLimit then
                    // generate a result using the new figure
                    let candidate, relevantValue = generator midpoint
                    // determine the target range
                    let lowerTolerance, upperTolerance =
                        match targetTolerance with
                        | BelowZero -> -tolerance, 0m
                        | AroundZero -> -tolerance, tolerance
                        | AboveZero -> 0m, tolerance
                    // if the solution is within target range, return the value
                    if candidate >= lowerTolerance && candidate <= upperTolerance then
                        Solution.Found(relevantValue, iteration, tolerance)
                    // if the solution is too high, alter the range and try again
                    elif candidate > upperTolerance then
                        loop (iteration + 1) midpoint upperBound tolerance
                    // if the solution is too low, alter the range and try again
                    else //candidate < lowerTolerance
                        loop (iteration + 1) lowerBound midpoint tolerance
                // if at the tolerance limit without a solution, return the latest value with a warning
                elif tolerance = toleranceSteps.MaxTolerance then
                    Solution.IterationLimitReached(midpoint, iteration, tolerance)
                // otherwise, increment the tolerance limit and try again
                else
                    let newTolerance =
                        min toleranceSteps.MaxTolerance (tolerance + toleranceSteps.ToleranceStep)

                    let newLowerBound, newUpperBound = midpoint - newTolerance, midpoint + newTolerance
                    loop 0 newLowerBound newUpperBound newTolerance
            // start the first iteration
            loop 0 initialLowerBound initialUpperBound toleranceSteps.MinTolerance

        /// use the Newton-Raphson method to find the solution (particularly suitable for calculating the APR)
        let solveNewtonRaphson f (iterationLimit: uint) initialGuess tolerance =
            // calculate the approximate derivative of the function `f`
            let derivative f x step =
                // evaluate the function at two nearby points and compute the slope of the line connecting them
                (f (x + step) - f (x - step)) / (2m * step)
            // iterate as necessary
            let rec loop x iteration =
                // until the iteration limit is reached
                if iteration <= int iterationLimit then
                    // get the function value of `x`
                    let fx = f x
                    // if the function value is within the tolerance, return the solution
                    if abs fx < tolerance then
                        Solution.Found(x, iteration, tolerance)
                    // otherwise, iterate again using an improved guess
                    else
                        // get the derivative of the function value of `x`
                        let f'x = derivative f x 1e-5m
                        // loop by using the derivative to generate a better guess value
                        loop (if f'x = 0m then 0m else x - fx / f'x) (iteration + 1)
                // if the iteration limit is reached without a solution, return the latest value with a warning
                else
                    Solution.IterationLimitReached(x, iteration, tolerance)

            loop initialGuess 0

        /// concatenates the members of an array into a delimited string or "n/a" if the array is empty or null
        let toStringOrNa a =
            match a with
            | null
            | [||] -> "<i>n/a</i>"
            | _ -> a |> Array.map string |> String.concat "<br/>"

    /// functions for working with maps
    module Map =
        /// creates a map from an array of key-value tuples with array values
        let ofArrayWithMerge (array: ('a * 'b array) array) =
            array
            // group by unique keys
            |> Array.groupBy fst
            // map to new key value pair with the collected array as the value
            |> Array.map (fun (k, v) -> k, Array.collect snd v)
            // convert to a map
            |> Map.ofArray

    /// wrapper to extract APR value from solution
    let getAprOr defaultValue =
        function
        | Solution.Found(apr, _, _) -> apr
        | _ -> defaultValue
