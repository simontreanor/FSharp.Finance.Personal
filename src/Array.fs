namespace FSharp.Finance.Personal

/// extension to the array module of a solve function, allowing an unknown value to be calculated through iteration
[<AutoOpen>]
module Array =

    /// the result obtained from the array solver
    [<RequireQualifiedAccess; Struct>]
    type Solution =
        /// a solution could not be found due to an issue with the initial parameters
        | Impossible
        /// a solution could not be found within the iteration limit, but it returns the result of the last iteration and stats on how it was reached
        | IterationLimitReached of PartialSolution:decimal * IterationLimit:int * MaxTolerance:int
        /// a solution was found, returning the solution, the number of iterations required and the final tolerance used
        | Found of Found:decimal * Iteration:int * Tolerance:int

    /// lower and upper bounds, as well as a step value, for tolerance when using the solver
    [<RequireQualifiedAccess; Struct>]
    type ToleranceSteps = {
        Min: int
        Step: int
        Max: int
    }

    /// what range of values the solver should aim for
    [<Struct>]
    type ToleranceOption =
        /// find a solution less than or equal to zero
        | BelowZero
        /// find a solution either side of zero
        | AroundZero
        /// find a solution greater than or equal to zero
        | AboveZero

    /// utility functions for arrays
    module Array =
        /// gets the last but one member of an array
        let lastButOne a = a |> Array.rev |> Array.tail |> Array.head
        /// equivalent of Array.last but yields a default value instead of an error if the array is empty
        let lastOrDefault defaultValue a = if Array.isEmpty a then defaultValue else Array.last a
        /// equivalent of Array.maxBy but yields a default value instead of an error if the array is empty
        let maxByOrDefault maxByProp getProp defaultValue a = if Array.isEmpty a then defaultValue else a |> Array.maxBy maxByProp |> getProp
        /// iteratively solves for a given input using a generator function until the output is zero or within a set tolerance,
        /// optionally relaxing the tolerance until a solution is found
        [<TailCall>]
        let solve (generator: decimal -> decimal) iterationLimit approximation toleranceOption (toleranceSteps: ToleranceSteps voption) =
            let toleranceSteps' = toleranceSteps |> ValueOption.defaultValue { Min = 0; Step = 0; Max = 0 }
            let rec loop i lowerBound upperBound tolerance =
                let midRange =
                    let x = (upperBound - lowerBound) / 2m
                    if x = upperBound then upperBound * 2m
                    elif x = lowerBound then lowerBound / 2m
                    else x
                let newBound = lowerBound + midRange
                if i = iterationLimit then
                    if tolerance = toleranceSteps'.Max then
                        Solution.IterationLimitReached (newBound, i, tolerance)
                    else
                        let newTolerance = min toleranceSteps'.Max (tolerance + toleranceSteps'.Step)
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
            loop 0 0m (approximation * 100m) toleranceSteps'.Min // to-do: improve approximation
