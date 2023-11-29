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
        BalanceStatus = Settled
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

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
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
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 49153<Cent> 121391<Cent>

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "IrregularPaymentTest004.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            Date = DateTime(2023, 3, 21)
            TermDay = 140<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 121391<Cent> |]
            NetEffect = 121391<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 64697<Cent>
            NewInterest = 2675<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 55745<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 60646<Cent>
            PenaltyChargesPortion = 5000<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``5) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue`` () =
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
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 49153<Cent> (49153<Cent> * 3)

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "IrregularPaymentTest005.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            Date = DateTime(2023, 3, 21)
            TermDay = 140<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 147459<Cent> |]
            NetEffect = 147459<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = RefundDue
            CumulativeInterest = 64697<Cent>
            NewInterest = 2675<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 81813<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 60646<Cent>
            PenaltyChargesPortion = 5000<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = -26068<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``6) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded`` () =
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
        let actualPayments = [|
            { Day =   2<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [|  49153<Cent>     |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day =   4<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [|  49153<Cent>     |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 140<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [|  49153<Cent> * 3 |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            { Day = 143<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| -26068<Cent>     |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "IrregularPaymentTest006.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            Date = DateTime(2023, 3, 24)
            TermDay = 143<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| -26068<Cent> |]
            NetEffect = -26068<Cent>
            PaymentStatus = ValueSome Refunded
            BalanceStatus = Settled
            CumulativeInterest = 64697<Cent>
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = -26068<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``7) Zero-day loan`` () =
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
        let actualPayments = [|
            { Day = 0<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 150000<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
        |]

        let irregularSchedule =
            actualPayments
            |> applyPayments sp

        irregularSchedule |> ValueOption.iter(Formatting.outputListToHtml "IrregularPaymentTest007.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map Array.last
        let expected = ValueSome {
            Date = DateTime(2022, 11, 1)
            TermDay = 0<Day>
            Advance = 150000<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 150000<Cent> |]
            NetEffect = 150000<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 0<Cent>
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 150000<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``Settlement quote. Settlement falling on a scheduled payment date`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual = getSettlementQuote (DateTime.Today.AddDays -3.) sp actualPayments |> ValueOption.map Array.last

        let expected = ValueSome {
            Date = (DateTime.Today.AddDays -3.)
            TermDay = 57<Day>
            Advance = 0<Cent>
            ScheduledPayment = 32315<Cent>
            ActualPayments = [| 196970<Cent> |]
            NetEffect = 196970<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = Settled
            CumulativeInterest = 5359<Cent>
            NewInterest = 371<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 79019<Cent>
            InterestPortion = 371<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 143753<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``Settlement quote. Settlement not falling on a scheduled payment date`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual = getSettlementQuote DateTime.Today sp actualPayments |> ValueOption.map Array.last

        let expected = ValueSome {
            Date = DateTime.Today
            TermDay = 60<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 202648<Cent> |]
            NetEffect = 202648<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = Settled
            CumulativeInterest = 5637<Cent>
            NewInterest = 278<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 83419<Cent>
            InterestPortion = 649<Cent>
            PenaltyChargesPortion = 1000<Cent>
            ProductFeesRefund = 139353<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``Settlement quote. Settlement not falling on a scheduled payment date but having an actual payment already made on the same day`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual = getSettlementQuote DateTime.Today sp actualPayments |> ValueOption.map Array.last

        let expected = ValueSome {
            Date = DateTime.Today
            TermDay = 60<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 2500<Cent>; 200148<Cent> |]
            NetEffect = 202648<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = Settled
            CumulativeInterest = 5637<Cent>
            NewInterest = 278<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 83419<Cent>
            InterestPortion = 649<Cent>
            PenaltyChargesPortion = 1000<Cent>
            ProductFeesRefund = 139353<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }

        actual |> should equal expected
