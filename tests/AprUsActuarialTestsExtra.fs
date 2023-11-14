namespace FSharp.Finance.Tests

open System
open Xunit
open FsUnit.Xunit
open System.Text.Json

open FSharp.Finance

module AprActuarialTestsExtra =

    open Apr

    type AprUsActuarialTestItem = {
        StartDate: DateTime
        Principal: decimal
        PaymentAmount: decimal
        PaymentDates: DateTime array
        ExpectedApr: decimal
        ActualApr: decimal
    }

    let aprUsActuarialTestData =
        IO.File.ReadAllText $"{__SOURCE_DIRECTORY__}/../tests/io/AprUsActuarialTestData.json"
        |> JsonSerializer.Deserialize<AprUsActuarialTestItem array>
        |> Array.map(fun ssi ->
            let payments = ssi.PaymentDates |> Array.map(fun dt -> { TransferType = Payment; Date = dt; Amount = ssi.PaymentAmount })
            let actualApr = Apr.calculate UsActuarial 8 ssi.Principal ssi.StartDate payments |> fun m -> Decimal.Round(m * 100m, 4)
            { ssi with ActualApr = actualApr }
        )
        |> toMemberData

    [<Theory>]
    [<MemberData(nameof(aprUsActuarialTestData))>]
    let ``Actual APRs match expected APRs under the US actuarial method`` testItem =
        testItem.ActualApr |> should equal testItem.ExpectedApr
