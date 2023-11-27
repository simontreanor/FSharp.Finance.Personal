namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module IrregularPaymentTests =

    open IrregularPayment

    let quickActualPayments days levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last |> Array.map(fun d -> { Day =   d * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| finalPayment |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
            rest |> Array.map(fun d -> { Day =   d * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| levelPayment |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
        |]
        |> Array.concat
        |> Array.rev

    let quickExpectedFinalApportionment date termDay paymentAmount cumulativeInterest newInterest principalPortion = ValueSome {
        Date = date
        TermDay = termDay
        Advance = 0<Cent>
        ScheduledPayment = paymentAmount
        ActualPayments = [| paymentAmount |]
        NetEffect = paymentAmount
        PaymentStatus = ValueSome PaymentMade
        BalanceStatus = PaidInFull
        CumulativeInterest = cumulativeInterest
        NewInterest = newInterest
        NewPenaltyCharges = 0<Cent>
        PrincipalPortion = principalPortion
        ProductFeesPortion = 0<Cent>
        InterestPortion = newInterest
        PenaltyChargesPortion = 0<Cent>
        ProductFeesRefund = 0<Cent>
        PrincipalBalance = 0<Cent>
        ProductFeesBalance = 0<Cent>
        InterestBalance = 0<Cent>
        PenaltyChargesBalance = 0<Cent>
    }

    [<Fact>]
    let ``1) Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let (sp: RegularPayment.ScheduleParameters) =
            {
                StartDate = DateTime(2022, 11, 26)
                Principal = 1500 * 100<Cent>
                ProductFees = ValueNone
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = ValueSome <| PercentageOfPrincipal (Percent 100m)
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 31<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 45688<Cent> 45684<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter (Formatting.outputListToHtml "IrregularPaymentTest001.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = quickExpectedFinalApportionment (DateTime(2023, 3, 31)) 125<Day> 45684<Cent> 78436<Cent> 9078<Cent> 36606<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let (sp: RegularPayment.ScheduleParameters) =
            {
                StartDate = DateTime(2022, 10, 29)
                Principal = 1500 * 100<Cent>
                ProductFees = ValueNone
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = ValueSome <| PercentageOfPrincipal (Percent 100m)
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 31<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 55605<Cent> 55600<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "IrregularPaymentTest002.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = quickExpectedFinalApportionment (DateTime(2023, 3, 31)) 153<Day> 55600<Cent> 128020<Cent> 11048<Cent> 44552<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let (sp: RegularPayment.ScheduleParameters) =
            {
                StartDate = DateTime(2022, 11, 1)
                Principal = 1500 * 100<Cent>
                ProductFees = ValueNone
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = ValueSome <| PercentageOfPrincipal (Percent 100m)
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 15<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 49153<Cent> 49153<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "IrregularPaymentTest003.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = quickExpectedFinalApportionment (DateTime(2023, 3, 15)) 134<Day> 49153<Cent> 95765<Cent> 8995<Cent> 40158<Cent>
        actual |> should equal expected

