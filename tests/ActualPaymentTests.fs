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
            last |> Array.map(fun d -> { PaymentDay =   d * 1<OffsetDay>; PaymentDetails = ActualPayments ([| finalPayment |], [||]) })
            rest |> Array.map(fun d -> { PaymentDay =   d * 1<OffsetDay>; PaymentDetails = ActualPayments ([| levelPayment |], [||]) })
        |]
        |> Array.concat
        |> Array.rev

    let quickExpectedFinalItem date offsetDay paymentAmount cumulativeInterest newInterest principalPortion = ValueSome {
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
        NewCharges = [||]
        PrincipalPortion = principalPortion
        FeesPortion = 0L<Cent>
        InterestPortion = newInterest
        ChargesPortion = 0L<Cent>
        FeesRefund = 0L<Cent>
        PrincipalBalance = 0L<Cent>
        FeesBalance = 0L<Cent>
        InterestBalance = 0L<Cent>
        ChargesBalance = 0L<Cent>
    }

    [<Fact>]
    let ``1) Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 4, 1)
                StartDate = DateTime(2022, 11, 26)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 45688L<Cent> 45684L<Cent>

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter (_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest001.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
        let expected = quickExpectedFinalItem (DateTime(2023, 3, 31)) 125<OffsetDay> 45684L<Cent> 78436L<Cent> 9078L<Cent> 36606L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 4, 1)
                StartDate = DateTime(2022, 10, 29)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 55605L<Cent> 55600L<Cent>

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest002.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
        let expected = quickExpectedFinalItem (DateTime(2023, 3, 31)) 153<OffsetDay> 55600L<Cent> 128020L<Cent> 11048L<Cent> 44552L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 16)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 49153L<Cent> 49153L<Cent>

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest003.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
        let expected = quickExpectedFinalItem (DateTime(2023, 3, 15)) 134<OffsetDay> 49153L<Cent> 95765L<Cent> 8995L<Cent> 40158L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 22)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 49153L<Cent> 121391L<Cent>

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest004.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
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
            NewCharges = [||]
            PrincipalPortion = 55745L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 60646L<Cent>
            ChargesPortion = 5000L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``5) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 22)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 49153L<Cent> (49153L<Cent> * 3L)

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest005.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
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
            NewCharges = [||]
            PrincipalPortion = 81813L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 60646L<Cent>
            ChargesPortion = 5000L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -26068L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``6) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2023, 3, 25)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =   2<OffsetDay>; PaymentDetails = ActualPayments ([|  49153L<Cent>      |], [||]) }
            { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayments ([|  49153L<Cent>      |], [||]) }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayments ([|  49153L<Cent> * 3L |], [||]) }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayments ([| -26068L<Cent>      |], [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest006.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
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
            NewCharges = [||]
            PrincipalPortion = -26068L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``7) Zero-day loan`` () =
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime(2022, 11, 2)
                StartDate = DateTime(2022, 11, 1)
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayments ([| 150000L<Cent> |], [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp (ValueSome (DateTime(2022, 11, 1))) false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest007.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
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
            NewCharges = [||]
            PrincipalPortion = 150000L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``8) Check that charge for late payment is not applied on scheduled payment date when payment has not yet been made`` () =
        let startDate = DateTime.Today.AddDays -56.
        let (sp: ScheduledPayment.ScheduleParameters) =
            {
                AsOfDate = DateTime.Today
                StartDate = startDate
                Principal = 150000L<Cent>
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 14.)
                PaymentCount = 11
                FeesAndCharges = {
                    Fees = ValueNone
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment 1000L<Cent> |]
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDays>
                    Holidays = [||]
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayments ([| 24386L<Cent> |], [||]) }
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayments ([| 24386L<Cent> |], [||]) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayments ([| 24386L<Cent> |], [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> generateAmortisationSchedule sp ValueNone false

        irregularSchedule |> ValueOption.iter(_.Items >> Formatting.outputListToHtml "out/ActualPaymentTest008.md" (ValueSome 300))

        let actual = irregularSchedule |> ValueOption.map (_.Items >> Array.last)
        let expected = ValueSome {
            OffsetDate = startDate.AddDays 154.
            OffsetDay = 154<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 24366L<Cent> // to-do: this should be less than the level payment
            ActualPayments = [||]
            NetEffect = 24366L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = Settled
            CumulativeInterest = 118226L<Cent>
            NewInterest = 2454L<Cent>
            NewCharges = [||]
            PrincipalPortion = 21912L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 2454L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected
