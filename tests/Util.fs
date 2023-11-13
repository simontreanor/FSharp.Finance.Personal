namespace FSharp.Finance.Tests

open System

module Tolerance =

    /// specifies if a result is exact, almost exact, or way out
    type ToleranceResult =
        | Exact of Actual:decimal
        | WithinTolerance of Expected: decimal * Actual:decimal * Variance:decimal
        | OutOfTolerance of Expected: decimal * Actual:decimal * Variance:decimal

    /// verify the accuracy a result rounded to a number of decimal places
    /// 
    /// (tolerance is 0.125 percentage points of the expected value)
    let checkTolerance (decimalPlaces: int) (expectedApr: decimal) (actualApr: decimal) =
        let variance = Math.Abs(Math.Round(actualApr, decimalPlaces) - Math.Round(expectedApr, decimalPlaces))
        if variance = 0m then
            Exact actualApr
        elif variance <= 0.125m then
            WithinTolerance (expectedApr, actualApr, variance)
        else
            OutOfTolerance (expectedApr, actualApr, variance)

    /// format the tolerance result
    let toleranceString (decimalPlaces: int) result =
        let format (m: decimal) = m.ToString $"N{decimalPlaces}"
        match result with
        | Exact actual ->
            $"Exact result: {format actual} %% APR"
        | WithinTolerance (expected, actual, variance) ->
            $"Result: actual APR ({format actual} %%) within tolerance of expected APR ({format expected} %%): variance {format variance} percentage points" 
        | OutOfTolerance (expected, actual, variance) ->
            $"Out of tolerance: actual APR ({format actual} %%) differs from expected APR ({format expected} %%) by {format variance} percentage points"
