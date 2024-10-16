namespace FSharp.Finance.Personal

/// extension to the array module of a solve function, allowing an unknown value to be calculated through iteration
module MapExtension =

    /// Contains operations for working with F# map values
    module Map =

        /// creates a map from an array of key-value tuples with array values
        let ofArrayWithMerge (array: ('a * 'b array) array) =
            array
            |> Array.groupBy fst
            |> Array.map(fun (k, v) -> k, Array.collect snd v)
            |> Map.ofArray
