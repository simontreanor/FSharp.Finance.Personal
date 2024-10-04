namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit
open System.Text.Json

open FSharp.Finance.Personal

module AprActuarialTestsExtra =

    open ArrayExtension
    open Apr
    open Currency
    open DateDay
    open Percentages
    open Util

    type AprUsActuarialTestItemDto = {
        StartDate: DateOnly
        Principal: decimal
        PaymentAmount: decimal
        PaymentDates: DateOnly array
        ExpectedApr: decimal
        ActualApr: decimal
    }

    type AprUsActuarialTestItem = {
        StartDate: Date
        Principal: int64<Cent>
        PaymentAmount: int64<Cent>
        PaymentDates: Date array
        ExpectedApr: Percent
        ActualApr: Percent
    }

    let aprUsActuarialTestData =
        IO.File.ReadAllText $"{__SOURCE_DIRECTORY__}/../tests/io/in/AprUsActuarialTestData.json"
        |> JsonSerializer.Deserialize<AprUsActuarialTestItemDto array>
        |> Array.choose(fun ssi ->
            let startDate = ssi.StartDate |> fun d -> Date(d.Year, d.Month, d.Day)
            let principal = Cent.fromDecimal ssi.Principal
            let paymentAmount = Cent.fromDecimal ssi.PaymentAmount
            let paymentDates = ssi.PaymentDates |> Array.map(fun d -> Date(d.Year, d.Month, d.Day))
            let payments = paymentDates |> Array.map(fun d -> { TransferType = Payment; TransferDate = d; Amount = paymentAmount })
            let actualApr = calculate (CalculationMethod.UsActuarial 8) principal startDate payments
            match actualApr with
            | Solution.Found (apr, _, _) ->
                Some {
                    StartDate = startDate
                    Principal = principal
                    PaymentAmount = paymentAmount
                    PaymentDates = paymentDates
                    ExpectedApr = Percent ssi.ExpectedApr
                    ActualApr = apr |> Percent.fromDecimal |> Percent.round 4
                }
            | _ -> None
        )
        |> toMemberData

    [<Theory>]
    [<MemberData(nameof(aprUsActuarialTestData))>]
    let ``Actual APRs match expected APRs under the US actuarial method`` testItem =
        testItem.ActualApr |> should equal testItem.ExpectedApr
