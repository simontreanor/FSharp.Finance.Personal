namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open System

open FSharp.Finance.Personal

module AprDisabledTests =

    open Apr
    open Calculation
    open DateDay

    [<Fact>]
    let ``calculate returns Impossible when AprMethod is Disabled`` () =
        calculate CalculationMethod.Disabled 500_00L<Cent> (Date(2025, 1, 1)) [|
            {
                TransferType = Payment
                TransferDate = Date(2025, 2, 1)
                Value = 500_00L<Cent>
            }
        |]
        |> should equal Solution.Impossible

    [<Fact>]
    let ``toPercent returns Percent 0m when AprMethod is Disabled`` () =
        toPercent CalculationMethod.Disabled Solution.Impossible
        |> should equal (Percent 0m)

    [<Fact>]
    let ``Html of Disabled is n/a`` () =
        CalculationMethod.Disabled.Html
        |> should equal "n/a"

    [<Fact>]
    let ``calculate with Disabled ignores transfers and returns Impossible`` () =
        calculate CalculationMethod.Disabled 1000_00L<Cent> (Date(2025, 3, 1)) [||]
        |> should equal Solution.Impossible
