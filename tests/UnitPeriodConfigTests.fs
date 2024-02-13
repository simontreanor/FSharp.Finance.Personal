namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module UnitPeriodConfigTests =

    open UnitPeriod
    module DefaultConfig =

        [<Fact>]
        let ``Default semi-monthly function produces valid unit-period configs`` () =
            let actual =
                [| 0 .. 365 * 5 |]
                |> Array.choose(fun d ->
                    let config = d |> Date(2024, 2, 13).AddDays |> Config.defaultSemiMonthly
                    try
                        Config.constrain config |> ignore
                        None
                    with
                        | ex -> Some ex.Message
                )
            let expected = [||]
            actual |> should equal expected

        [<Fact>]
        let ``Default monthly function produces valid unit-period configs`` () =
            let actual =
                [| 0 .. 365 * 5 |]
                |> Array.choose(fun d ->
                    let config = d |> Date(2024, 2, 13).AddDays |> Config.defaultMonthly 1
                    try
                        Config.constrain config |> ignore
                        None
                    with
                        | ex -> Some ex.Message
                )
            let expected = [||]
            actual |> should equal expected
