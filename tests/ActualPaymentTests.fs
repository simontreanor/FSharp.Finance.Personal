namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTests =

    open ActualPayment

    let quickActualPayments days levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last |> Array.map(fun d -> { Day =   d * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| finalPayment |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
            rest |> Array.map(fun d -> { Day =   d * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| levelPayment |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] })
        |]
        |> Array.concat
        |> Array.rev

    let quickExpectedFinalApportionment date offsetDay paymentAmount cumulativeInterest newInterest principalPortion = ValueSome {
        OffsetDate = date
        OffsetDay = offsetDay
        Advance = 0L<Cent>
        ScheduledPayment = paymentAmount
        ActualPayments = [| paymentAmount |]
        NetEffect = paymentAmount
        PaymentStatus = ValueSome PaymentMade
        BalanceStatus = Settled
        CumulativeInterest = cumulativeInterest
        NewInterest = newInterest
        NewPenaltyCharges = 0L<Cent>
        PrincipalPortion = principalPortion
        ProductFeesPortion = 0L<Cent>
        InterestPortion = newInterest
        PenaltyChargesPortion = 0L<Cent>
        ProductFeesRefund = 0L<Cent>
        PrincipalBalance = 0L<Cent>
        ProductFeesBalance = 0L<Cent>
        InterestBalance = 0L<Cent>
        PenaltyChargesBalance = 0L<Cent>
    }

    [<Fact>]
    let ``1) Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 4, 1)
                StartDate = DateTime(2022, 11, 26)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 31<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 45688L<Cent> 45684L<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter (Formatting.outputListToHtml "out/ActualPaymentTest001.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = quickExpectedFinalApportionment (DateTime(2023, 3, 31)) 125<OffsetDay> 45684L<Cent> 78436L<Cent> 9078L<Cent> 36606L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 4, 1)
                StartDate = DateTime(2022, 10, 29)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 31<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 55605L<Cent> 55600L<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest002.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = quickExpectedFinalApportionment (DateTime(2023, 3, 31)) 153<OffsetDay> 55600L<Cent> 128020L<Cent> 11048L<Cent> 44552L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 16)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 15<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 49154L<Cent> 49148L<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest003.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = quickExpectedFinalApportionment (DateTime(2023, 3, 15)) 134<OffsetDay> 49148L<Cent> 95764L<Cent> 8994L<Cent> 40154L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 22)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 15<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 49153L<Cent> 121391L<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest004.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            OffsetDate = DateTime(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 121391L<Cent> |]
            NetEffect = 121391L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 64697L<Cent>
            NewInterest = 2675L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 55745L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 60646L<Cent>
            PenaltyChargesPortion = 5000L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``5) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 22)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 15<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 49153L<Cent> (49153L<Cent> * 3L)

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest005.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            OffsetDate = DateTime(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 147459L<Cent> |]
            NetEffect = 147459L<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = RefundDue
            CumulativeInterest = 64697L<Cent>
            NewInterest = 2675L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 81813L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 60646L<Cent>
            PenaltyChargesPortion = 5000L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = -26068L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``6) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 25)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 15<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = [|
            { Day =   2<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [|  49153L<Cent>      |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =   4<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [|  49153L<Cent>      |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 140<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [|  49153L<Cent> * 3L |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 143<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| -26068L<Cent>      |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest006.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            OffsetDate = DateTime(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| -26068L<Cent> |]
            NetEffect = -26068L<Cent>
            PaymentStatus = ValueSome Refunded
            BalanceStatus = Settled
            CumulativeInterest = 64697L<Cent>
            NewInterest = 0L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = -26068L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``7) Zero-day loan`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2022, 11, 2)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Monthly(1, UnitPeriod.MonthlyConfig(2022, 11, 15<TrackingDay>))
                PaymentCount = 5
            }
        let actualPayments = [|
            { Day = 0<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 150000L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]

        let irregularSchedule =
            actualPayments
            |> applyPayments sp (ValueSome (DateTime(2022, 11, 1)))

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest007.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            OffsetDate = DateTime(2022, 11, 1)
            OffsetDay = 0<OffsetDay>
            Advance = 150000L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 150000L<Cent> |]
            NetEffect = 150000L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 0L<Cent>
            NewInterest = 0L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 150000L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``8) Check that penalty charge for late payment is not applied on scheduled payment date when payment has not yet been made`` () =
        let startDate = DateTime.Today.AddDays -56.
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime.Today
                StartDate = startDate
                Principal = 150000L<Cent>
                ProductFees = ValueNone
                ProductFeesSettlement = ProRataRefund
                InterestRate = DailyInterestRate (Percent 0.8m)
                InterestCap = { TotalCap = ValueSome <| TotalPercentageCap (Percent 100m); DailyCap = ValueNone }
                InterestGracePeriod = 3<Days>
                InterestHolidays = [||]
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 14.)
                PaymentCount = 11
            }
        let actualPayments = [|
            { Day = 14<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 24386L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 28<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 24386L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 42<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 24386L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]

        let irregularSchedule =
            actualPayments
            |> applyPayments sp ValueNone

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "out/ActualPaymentTest008.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            OffsetDate = startDate.AddDays 154.
            OffsetDay = 154<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 24366L<Cent> // to-do: this should be less than the level payment
            ActualPayments = [| |]
            NetEffect = 24366L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = Settled
            CumulativeInterest = 118226L<Cent>
            NewInterest = 2454L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 21912L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 2454L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        }
        actual |> should equal expected
