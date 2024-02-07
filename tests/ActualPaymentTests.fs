namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTests =

    open CustomerPayments
    open PaymentSchedule
    open Amortisation

    let quickActualPayments days levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last |> Array.map(fun d -> { PaymentDay =   d * 1<OffsetDay>; PaymentDetails = ActualPayment (finalPayment, [||]) })
            rest |> Array.map(fun d -> { PaymentDay =   d * 1<OffsetDay>; PaymentDetails = ActualPayment (levelPayment, [||]) })
        |]
        |> Array.concat
        |> Array.rev

    let quickExpectedFinalItem date offsetDay paymentAmount cumulativeInterest newInterest principalPortion = ValueSome {
        OffsetDate = date
        OffsetDay = offsetDay
        Advances = [||]
        ScheduledPayment = paymentAmount
        ActualPayments = [| paymentAmount |]
        GeneratedPayment = 0L<Cent>
        NetEffect = paymentAmount
        PaymentStatus = ValueSome PaymentMade
        BalanceStatus = ClosedBalance
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
        let sp =
            {
                AsOfDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest001.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 125<OffsetDay> 456_84L<Cent> 784_36L<Cent> 90_78L<Cent> 366_06L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let sp =
            {
                AsOfDate = Date(2023, 4, 1)
                StartDate = Date(2022, 10, 29)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 556_05L<Cent> 556_00L<Cent>

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest002.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 153<OffsetDay> 556_00L<Cent> 1280_20L<Cent> 110_48L<Cent> 445_52L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 16)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 491_53L<Cent> 491_53L<Cent>

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest003.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 15)) 134<OffsetDay> 491_53L<Cent> 957_65L<Cent> 89_95L<Cent> 401_58L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 22)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> 1213_91L<Cent>

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest004.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 1213_91L<Cent> |]
            GeneratedPayment = 0L<Cent>
            NetEffect = 1213_91L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = ClosedBalance
            CumulativeInterest = 646_97L<Cent>
            NewInterest = 26_75L<Cent>
            NewCharges = [||]
            PrincipalPortion = 557_45L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 606_46L<Cent>
            ChargesPortion = 50_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``5) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 22)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> (491_53L<Cent> * 3L)

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest005.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 1474_59L<Cent> |]
            GeneratedPayment = 0L<Cent>
            NetEffect = 1474_59L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = RefundDue
            CumulativeInterest = 646_97L<Cent>
            NewInterest = 26_75L<Cent>
            NewCharges = [||]
            PrincipalPortion = 818_13L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 606_46L<Cent>
            ChargesPortion = 50_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -260_68L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``6) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 25)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =   2<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>     , [||]) }
            { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>     , [||]) }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent> * 3L, [||]) }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment (-260_68L<Cent>     , [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest006.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| -260_68L<Cent> |]
            GeneratedPayment = 0L<Cent>
            NetEffect = -260_68L<Cent>
            PaymentStatus = ValueSome Refunded
            BalanceStatus = ClosedBalance
            CumulativeInterest = 646_97L<Cent>
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = -260_68L<Cent>
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
        let sp =
            {
                AsOfDate = Date(2022, 11, 2)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment (1500_00L<Cent>, [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement (ValueSome (Date(2022, 11, 1))) DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest007.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2022, 11, 1)
            OffsetDay = 0<OffsetDay>
            Advances = [| 1500_00L<Cent> |]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 1500_00L<Cent> |]
            GeneratedPayment = 0L<Cent>
            NetEffect = 1500_00L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = ClosedBalance
            CumulativeInterest = 0L<Cent>
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1500_00L<Cent>
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
        let startDate = Date(2024, 10, 1).AddDays -56
        let sp =
            {
                AsOfDate = Date(2024, 10, 1)
                StartDate = startDate
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 14)
                PaymentCount = 11
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (243_86L<Cent>, [||]) }
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (243_86L<Cent>, [||]) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (243_86L<Cent>, [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest008.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = startDate.AddDays 154
            OffsetDay = 154<OffsetDay>
            Advances = [||]
            ScheduledPayment = 243_66L<Cent> // to-do: this should be less than the level payment
            ActualPayments = [||]
            GeneratedPayment = 0L<Cent>
            NetEffect = 243_66L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = ClosedBalance
            CumulativeInterest = 1182_26L<Cent>
            NewInterest = 24_54L<Cent>
            NewCharges = [||]
            PrincipalPortion = 219_12L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 24_54L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``9) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded (with interest due to the customer on the negative balance)`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 25)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =   2<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>     , [||]) }
            { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>     , [||]) }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent> * 3L, [||]) }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment (-260_86L<Cent>     , [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest009.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| -260_86L<Cent> |]
            GeneratedPayment = 0L<Cent>
            NetEffect = -260_86L<Cent>
            PaymentStatus = ValueSome Refunded
            BalanceStatus = ClosedBalance
            CumulativeInterest = 646_79L<Cent>
            NewInterest = -18L<Cent>
            NewCharges = [||]
            PrincipalPortion = -260_68L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = -18L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``10) Underpayment made should show scheduled payment as net effect while in grace period`` () =
        let sp =
            {
                AsOfDate = Date(2023, 1, 18)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>, [||]) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>, [||]) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment ( 400_00L<Cent>, [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest010.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 0L<Cent>
            NetEffect = 491_53L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = ClosedBalance
            CumulativeInterest = 957_65L<Cent>
            NewInterest = 89_95L<Cent>
            NewCharges = [||]
            PrincipalPortion = 401_58L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 89_95L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``11) Underpayment made should show scheduled payment as underpayment after grace period has expired`` () =
        let sp =
            {
                AsOfDate = Date(2023, 1, 19)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>, [||]) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment ( 491_53L<Cent>, [||]) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment ( 400_00L<Cent>, [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest011.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 0L<Cent>
            NetEffect = 491_53L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = OpenBalance
            CumulativeInterest = 1011_21L<Cent>
            NewInterest = 118_33L<Cent>
            NewCharges = [||]
            PrincipalPortion = 373_20L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 118_33L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 155_09L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``12) Settled loan`` () =
        let sp =
            {
                AsOfDate = Date(2034, 1, 31)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest012.md")

        let actual = irregularSchedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = 491_53L<Cent>
            ActualPayments = [| 500_00L<Cent> |]
            GeneratedPayment = 0L<Cent>
            NetEffect = 500_00L<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = RefundDue
            CumulativeInterest = 932_07L<Cent>
            NewInterest = 79_07L<Cent>
            NewCharges = [||]
            PrincipalPortion = 420_93L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 79_07L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -67_93L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``13) Scheduled payment total can be less than principal when early actual payments are made but net effect is never less`` () =
        let sp =
            {
                AsOfDate = Date(2024, 2, 7)
                StartDate = Date(2024, 2, 2)
                Principal = 250_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 22)
                PaymentCount = 4
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [||]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  0<OffsetDay>; PaymentDetails = ActualPayment ( 97_01L<Cent>, [||]) }
        |]

        let irregularSchedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone DoNotCalculateFinalApr ApplyNegativeInterest ValueNone

        irregularSchedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest013.md")

        let actual = irregularSchedule |> ValueOption.map (fun s -> (s.ScheduleItems |> Array.sumBy _.NetEffect) >= sp.Principal)
        let expected = ValueSome true
        actual |> should equal expected
