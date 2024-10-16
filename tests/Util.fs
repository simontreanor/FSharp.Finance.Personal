namespace FSharp.Finance.Personal.Tests

open FSharp.Finance.Personal

module Util =

    open Calculation

    /// format an array as a list of object arrays, for feeding into a test theory
    let toMemberData (a: _ array) =
        Array.toList a
        |> List.map(fun ssi -> [| box ssi |])

    /// wrapper to extract APR value from solution
    let getAprOr defaultValue = function
        | Solution.Found(apr, _, _) ->
            apr
        | _ ->
            defaultValue
