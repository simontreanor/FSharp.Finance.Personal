namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module RegularPaymentTests =

    open RegularPayment

    let biweeklyParameters principal offset =
        let startDate = DateTime(2023, 11, 15)
        {
            StartDate = startDate
            Principal = decimal principal |> Cent.round
            ProductFees = ValueSome <| Percentage (189.47m<Percent>, ValueNone)
            InterestRate = AnnualInterestRate 9.95m<Percent>
            InterestCap = ValueNone
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(float offset))
            PaymentCount = 11
        }

    [<Fact>]
    let ``1) Biweekly schedule $1200 with short first period`` () =
        let actual = biweeklyParameters 120000<Cent> 8<Duration> |> calculateSchedule
        actual.Items |> Formatting.outputListToHtml "RegularPaymentTest001.md" (ValueSome 300)
        let expected = {
            Items = actual.Items
            FinalPaymentDay = 148<Day>
            LevelPayment = 32253<Cent>
            FinalPayment = 32253<Cent>
            PaymentTotal = 354783<Cent>
            PrincipalTotal = 347364<Cent>
            InterestTotal = 7419<Cent>
            Apr = 717.412507m<Percent>
            CostToBorrowingRatio = 67.589906m<Percent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``2) Biweekly schedule $1200 with first period equal to unit-period length`` () =
        let actual = biweeklyParameters 120000<Cent> 14<Duration> |> calculateSchedule
        actual.Items |> Formatting.outputListToHtml "RegularPaymentTest002.md" (ValueSome 300)
        let expected = {
            Items = actual.Items
            FinalPaymentDay = 154<Day>
            LevelPayment = 32306<Cent>
            FinalPayment = 32303<Cent>
            PaymentTotal = 355363<Cent>
            PrincipalTotal = 347364<Cent>
            InterestTotal = 7999<Cent>
            Apr = 637.159359m<Percent>
            CostToBorrowingRatio = 67.756878m<Percent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``3) Biweekly schedule $1200 with long first period`` () =
        let actual = biweeklyParameters 120000<Cent> 15<Duration> |> calculateSchedule
        actual.Items |> Formatting.outputListToHtml "RegularPaymentTest003.md" (ValueSome 300)
        let expected = {
            Items = actual.Items
            FinalPaymentDay = 155<Day>
            LevelPayment = 32315<Cent>
            FinalPayment = 32310<Cent>
            PaymentTotal = 355460<Cent>
            PrincipalTotal = 347364<Cent>
            InterestTotal = 8096<Cent>
            Apr = 623.703586m<Percent>
            CostToBorrowingRatio = 67.784802m<Percent>
        }
        actual |> should equal expected
