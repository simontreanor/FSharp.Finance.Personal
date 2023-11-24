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
            LevelPayment = 32254<Cent>
            FinalPayment = 32250<Cent>
            PaymentTotal = 354790<Cent>
            PrincipalTotal = 347364<Cent>
            InterestTotal = 7426<Cent>
            Apr = 717.439942m<Percent>
            CostToBorrowingRatio = 67.591921m<Percent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``2) Biweekly schedule $1200 with first period equal to unit-period length`` () =
        let actual = biweeklyParameters 120000<Cent> 14<Duration> |> calculateSchedule
        actual.Items |> Formatting.outputListToHtml "RegularPaymentTest002.md" (ValueSome 300)
        let expected = {
            Items = actual.Items
            FinalPaymentDay = 154<Day>
            LevelPayment = 32307<Cent>
            FinalPayment = 32298<Cent>
            PaymentTotal = 355368<Cent>
            PrincipalTotal = 347364<Cent>
            InterestTotal = 8004<Cent>
            Apr = 637.180799m<Percent>
            CostToBorrowingRatio = 67.758317m<Percent>
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
            FinalPayment = 32315<Cent>
            PaymentTotal = 355465<Cent>
            PrincipalTotal = 347364<Cent>
            InterestTotal = 8101<Cent>
            Apr = 623.706600m<Percent>
            CostToBorrowingRatio = 67.786242m<Percent>
        }
        actual |> should equal expected
