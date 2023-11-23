namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module IrregularPaymentTests =

    open IrregularPayment

    [<Fact>]
    let ``1. Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let (sp: RegularPayment.ScheduleParameters) =
            {
                StartDate = DateTime(2022, 11, 26)
                Principal = 1500 * 100<Cent>
                ProductFees = ValueNone
                InterestRate = DailyInterestRate 0.8m<Percent>
                InterestCap = ValueSome <| PercentageOfPrincipal 100m<Percent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 31<TrackingDay>))
                PaymentCount = 5
            }
        let (actualPayments: Payment array) = [|
            { Day =   4<Day>; Adjustments = [| ActualPayment 45688<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =  35<Day>; Adjustments = [| ActualPayment 45688<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =  66<Day>; Adjustments = [| ActualPayment 45688<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =  94<Day>; Adjustments = [| ActualPayment 45688<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 125<Day>; Adjustments = [| ActualPayment 45688<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]
        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> Formatting.outputListToHtml "IrregularPaymentTest001.md" (ValueSome 300)

        let actual = irregularSchedule |> Array.last
        let expected = {
            Date = DateTime(2023, 3, 31)
            TermDay = 125<Day>
            Advance = 0<Cent>
            Adjustments = [| ScheduledPayment 45688<Cent>; ActualPayment 45688<Cent> |]
            Payments = [| 45688<Cent> |]
            PaymentStatus = ValueSome PaidInFull
            CumulativeInterest = 78440<Cent>
            NewInterest = 9079<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 36609<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 9079<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``2. Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let (sp: RegularPayment.ScheduleParameters) =
            {
                StartDate = DateTime(2022, 10, 29)
                Principal = 1500 * 100<Cent>
                ProductFees = ValueNone
                InterestRate = DailyInterestRate 0.8m<Percent>
                InterestCap = ValueSome <| PercentageOfPrincipal 100m<Percent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 31<TrackingDay>))
                PaymentCount = 5
            }
        let (actualPayments: Payment array) = [|
            { Day =  32<Day>; Adjustments = [| ActualPayment 55605<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =  63<Day>; Adjustments = [| ActualPayment 55605<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =  94<Day>; Adjustments = [| ActualPayment 55605<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 122<Day>; Adjustments = [| ActualPayment 55605<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 153<Day>; Adjustments = [| ActualPayment 55603<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]
        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> Formatting.outputListToHtml "IrregularPaymentTest002.md" (ValueSome 300)

        let actual = irregularSchedule |> Array.last
        let expected = {
            Date = DateTime(2023, 3, 31)
            TermDay = 153<Day>
            Advance = 0<Cent>
            Adjustments = [| ScheduledPayment 55603<Cent>; ActualPayment 55603<Cent> |]
            Payments = [| 55603<Cent> |]
            PaymentStatus = ValueSome PaidInFull
            CumulativeInterest = 128023<Cent>
            NewInterest = 11049<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 44554<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 11049<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``99. Non-payer who commits to a long-term payment plan and completes it`` () =
        let startDate = DateTime(2022, 9, 27)
        let (sp: RegularPayment.ScheduleParameters) = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (189.47m<Percent>, ValueNone)
            InterestRate = AnnualInterestRate 9.95m<Percent>
            InterestCap = ValueNone
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }
        let actualPayments =
            [| 180 .. 7 .. 2098 |]
            |> Array.map(fun i -> { Day = i * 1<Day>; Adjustments = [| ActualPayment 2500<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] })
        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> Formatting.outputListToHtml "IrregularPaymentTest099.md" (ValueSome 300)

        let actual = irregularSchedule |> Array.last
        let expected = {
            Date = DateTime(2026, 8, 9)
            TermDay = 1412<Day>
            Advance = 0<Cent>
            Adjustments = [| ActualPayment 2500<Cent> |] // to-do: limit this to the final payment amount
            Payments = [| 1264<Cent> |]
            PaymentStatus = ValueSome PaidInFull
            CumulativeInterest = 82900<Cent>
            NewInterest = 2<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 439<Cent>
            ProductFeesPortion = 823<Cent>
            InterestPortion = 2<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected
