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
            last |> Array.map(fun d -> { PaymentDay =   d * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed finalPayment) })
            rest |> Array.map(fun d -> { PaymentDay =   d * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed levelPayment) })
        |]
        |> Array.concat
        |> Array.rev

    let quickExpectedFinalItem date offsetDay paymentAmount cumulativeInterest newInterest principalPortion = ValueSome {
        OffsetDate = date
        OffsetDay = offsetDay
        Advances = [||]
        ScheduledPayment = ValueSome paymentAmount
        PaymentDue = paymentAmount
        ActualPayments = [| ActualPayment.Confirmed paymentAmount |]
        GeneratedPayment = ValueNone
        NetEffect = paymentAmount
        PaymentStatus = PaymentMade
        BalanceStatus = ClosedBalance
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
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest001.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 125<OffsetDay> 456_84L<Cent> 784_36L<Cent> 90_78L<Cent> 366_06L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 10, 29)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 556_05L<Cent> 556_00L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest002.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 153<OffsetDay> 556_00L<Cent> 1280_20L<Cent> 110_48L<Cent> 445_52L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 491_53L<Cent> 491_53L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest003.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 15)) 134<OffsetDay> 491_53L<Cent> 957_65L<Cent> 89_95L<Cent> 401_58L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 22)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }
 
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> 1193_91L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest004.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueNone
            PaymentDue = 0L<Cent>
            ActualPayments = [| ActualPayment.Confirmed 1193_91L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 1193_91L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
            NewInterest = 26_75L<Cent>
            NewCharges = [||]
            PrincipalPortion = 557_45L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 606_46L<Cent>
            ChargesPortion = 30_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``5) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 22)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> (491_53L<Cent> * 3L)

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest005.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueNone
            PaymentDue = 0L<Cent>
            ActualPayments = [| ActualPayment.Confirmed 1474_59L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 1474_59L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = RefundDue
            NewInterest = 26_75L<Cent>
            NewCharges = [||]
            PrincipalPortion = 838_13L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 606_46L<Cent>
            ChargesPortion = 30_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -280_68L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``6) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 25)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 2<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 4<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed (491_53L<Cent> * 3L)) }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed -280_68L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest006.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueNone
            PaymentDue = 0L<Cent>
            ActualPayments = [| ActualPayment.Confirmed -280_68L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = -280_68L<Cent>
            PaymentStatus = Refunded
            BalanceStatus = ClosedBalance
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = -280_68L<Cent>
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
    let ``7) 0L<Cent>-day loan`` () =
        let sp = {
            AsOfDate = Date(2022, 11, 1)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 1500_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest007.md")

        let actual = schedule |> ValueOption.bind (_.ScheduleItems >> (Array.vTryLastBut 5))
        let expected = ValueSome {
            OffsetDate = Date(2022, 11, 1)
            OffsetDay = 0<OffsetDay>
            Advances = [| 1500_00L<Cent> |]
            ScheduledPayment = ValueNone
            PaymentDue = 0L<Cent>
            ActualPayments = [| ActualPayment.Confirmed 1500_00L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 1500_00L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
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
        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 14),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 243_86L<Cent>) }
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 243_86L<Cent>) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 243_86L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest008.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = startDate.AddDays 154
            OffsetDay = 154<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 243_66L<Cent> // to-do: this should be less than the level payment
            PaymentDue = 243_66L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 243_66L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
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
        let sp = {
            AsOfDate = Date(2023, 3, 25)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 2<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 4<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed (491_53L<Cent> * 3L)) }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed -280_87L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest009.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueNone
            PaymentDue = 0L<Cent>
            ActualPayments = [| ActualPayment.Confirmed -280_87L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = -280_87L<Cent>
            PaymentStatus = Refunded
            BalanceStatus = ClosedBalance
            NewInterest = -19L<Cent>
            NewCharges = [||]
            PrincipalPortion = -280_68L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = -19L<Cent>
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
        let sp = {
            AsOfDate = Date(2023, 1, 18)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 400_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest010.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 491_53L<Cent>
            PaymentDue = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 491_53L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
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
        let sp = {
            AsOfDate = Date(2023, 1, 19)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 491_53L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 400_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest011.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 491_53L<Cent>
            PaymentDue = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 491_53L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = OpenBalance
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
        let sp = {
            AsOfDate = Date(2034, 1, 31)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest012.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 491_53L<Cent>
            PaymentDue = 457_65L<Cent>
            ActualPayments = [| ActualPayment.Confirmed 500_00L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 500_00L<Cent>
            PaymentStatus = Overpayment
            BalanceStatus = RefundDue
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
        let sp = {
            AsOfDate = Date(2024, 2, 7)
            StartDate = Date(2024, 2, 2)
            Principal = 250_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 22),
                PaymentCount = 4
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  0<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 97_01L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest013.md")

        let actual = schedule |> ValueOption.map (fun s -> (s.ScheduleItems |> Array.sumBy _.NetEffect) >= sp.Principal)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``14) Something TH spotted`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6)),
                PaymentCount = 24
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  23<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  31<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  38<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  42<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  58<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  67<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  73<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  79<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  86<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  93<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_37L<Cent>) }
            { PaymentDay =  100<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 276_37L<Cent>) }
            { PaymentDay =  107<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  115<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  122<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  129<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  137<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  143<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  149<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  156<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  166<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  171<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  177<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_38L<Cent>) }
            { PaymentDay =  185<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 278_33L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp  IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest014.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -61_40L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``15) Large overpayment should not result in runaway fee refunds`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6)),
                PaymentCount = 24
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 5000_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp  IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest015.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2176_86L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``16) Large overpayment should not result in runaway fee refunds (2 actual payments)`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6)),
                PaymentCount = 24
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 5000_00L<Cent>) }
            { PaymentDay =  20<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest016.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2676_86L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``17) Large overpayment should not result in runaway fee refunds (3 actual payments)`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6)),
                PaymentCount = 24
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 5000_00L<Cent>) }
            { PaymentDay =  20<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
            { PaymentDay =  27<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 500_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest017.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -3176_86L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``18) Pending payments should only apply if not timed out`` () =
        let sp = {
            AsOfDate = Date(2024, 1, 30)
            StartDate = Date(2024, 1, 1)
            Principal = 2500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2024, 1, 14)),
                PaymentCount = 24
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_89L<Cent>) }
            { PaymentDay =  20<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Pending 271_89L<Cent>) }
            { PaymentDay =  27<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Pending 271_89L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest018.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 111_50L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``19) Pending payments should only apply if not timed out`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 1)
            StartDate = Date(2024, 1, 1)
            Principal = 2500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2024, 1, 14)),
                PaymentCount = 24
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  13<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 271_89L<Cent>) }
            { PaymentDay =  20<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Pending 271_89L<Cent>) }
            { PaymentDay =  27<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Pending 271_89L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest019.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 222_71L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``20) Generated settlement figure is correct`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 2)
            StartDate = Date(2023, 8, 20)
            Principal = 250_00L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 9, 5), 4)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = {
                    InterestRounding = RoundDown
                    PaymentRounding = RoundUp
                }
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay =  16<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 116_00L<Cent>) }
            { PaymentDay =  46<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 116_00L<Cent>) }
            { PaymentDay =  77<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 116_00L<Cent>) }
            { PaymentDay = 107<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 116_00L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Quote Settlement) ValueNone

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest020.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = ClosedBalance && si.GeneratedPayment = ValueSome -119_88L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected
