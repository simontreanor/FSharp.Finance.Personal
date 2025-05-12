namespace FSharp.Finance.Personal.Tests

module Util =

    /// format an array as a list of object arrays, for feeding into a test theory
    let toMemberData (a: _ array) =
        Array.toList a |> List.map (fun ssi -> [| box ssi |])
