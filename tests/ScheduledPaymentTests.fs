namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ScheduledPaymentTests =

    open ScheduledPayment

    let biweeklyParameters principal offset =
        let startDate = DateTime(2023, 11, 15)
        {
            AsOfDate = startDate
            StartDate = startDate
            Principal = decimal principal |> Cent.floor
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = { TotalCap = ValueNone; DailyCap = ValueNone }
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(float offset))
            PaymentCount = 11
        }

    [<Fact>]
    let ``1) Biweekly schedule $1200 with short first period`` () =
        let actual = biweeklyParameters 120000L<Cent> 8<Days> |> calculateSchedule
        actual |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest001.md" (ValueSome 300))
        let expected = ValueSome {
            AsOfDay = 0<OffsetDay>
            Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
            FinalPaymentDay = 148<OffsetDay>
            LevelPayment = 32253L<Cent>
            FinalPayment = 32253L<Cent>
            PaymentTotal = 354783L<Cent>
            PrincipalTotal = 347364L<Cent>
            InterestTotal = 7419L<Cent>
            Apr = Percent 717.412507m
            CostToBorrowingRatio = Percent 67.589906m
        }
        actual |> should equal expected

    [<Fact>]
    let ``2) Biweekly schedule $1200 with first period equal to unit-period length`` () =
        let actual = biweeklyParameters 120000L<Cent> 14<Days> |> calculateSchedule
        actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest002.md" (ValueSome 300))
        let expected = ValueSome {
            AsOfDay = 0<OffsetDay>
            Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
            FinalPaymentDay = 154<OffsetDay>
            LevelPayment = 32306L<Cent>
            FinalPayment = 32303L<Cent>
            PaymentTotal = 355363L<Cent>
            PrincipalTotal = 347364L<Cent>
            InterestTotal = 7999L<Cent>
            Apr = Percent 637.159359m
            CostToBorrowingRatio = Percent 67.756878m
        }
        actual |> should equal expected

    [<Fact>]
    let ``3) Biweekly schedule $1200 with long first period`` () =
        let actual = biweeklyParameters 120000L<Cent> 15<Days> |> calculateSchedule
        actual |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ScheduledPaymentTest003.md" (ValueSome 300))
        let expected = ValueSome {
            AsOfDay = 0<OffsetDay>
            Items = actual |> ValueOption.map _.Items |> ValueOption.defaultValue [||]
            FinalPaymentDay = 155<OffsetDay>
            LevelPayment = 32315L<Cent>
            FinalPayment = 32310L<Cent>
            PaymentTotal = 355460L<Cent>
            PrincipalTotal = 347364L<Cent>
            InterestTotal = 8096L<Cent>
            Apr = Percent 623.703586m
            CostToBorrowingRatio = Percent 67.784802m
        }
        actual |> should equal expected
