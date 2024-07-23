namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTests =

    open Amortisation
    open ArrayExtension
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open Interest
    open PaymentSchedule
    open Percentages

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    let quickActualPayments days levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last |> Array.map(fun d -> { PaymentDay =  d * 1<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed finalPayment; Metadata = Map.empty } })
            rest |> Array.map(fun d -> { PaymentDay =  d * 1<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed levelPayment; Metadata = Map.empty } })
        |]
        |> Array.concat
        |> Array.rev

    let quickExpectedFinalItem date offsetDay paymentAmount cumulativeInterest newInterest interestPortion principalPortion = ValueSome {
        OffsetDate = date
        OffsetDay = offsetDay
        Advances = [||]
        ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original paymentAmount; Metadata = Map.empty }
        Window = 5
        PaymentDue = paymentAmount
        ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentAmount; Metadata = Map.empty } |]
        GeneratedPayment = ValueNone
        NetEffect = paymentAmount
        PaymentStatus = PaymentMade
        BalanceStatus = ClosedBalance
        NewInterest = newInterest
        NewCharges = [||]
        PrincipalPortion = principalPortion
        FeesPortion = 0L<Cent>
        InterestPortion = interestPortion
        ChargesPortion = 0L<Cent>
        FeesRefund = 0L<Cent>
        PrincipalBalance = 0L<Cent>
        FeesBalance = 0L<Cent>
        InterestBalance = 0m<Cent>
        ChargesBalance = 0L<Cent>
        SettlementFigure = 0L<Cent>
        FeesRefundIfSettled = 0L<Cent>
    }

    [<Fact>]
    let ``1) Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest001.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 125<OffsetDay> 456_84L<Cent> 784_36L<Cent> 90_78.288m<Cent> 90_78L<Cent> 366_06L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 10, 29)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 556_05L<Cent> 556_00L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest002.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 153<OffsetDay> 556_00L<Cent> 1280_20L<Cent> 110_48.896m<Cent> 110_48L<Cent> 445_52L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 491_53L<Cent> 491_53L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest003.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = quickExpectedFinalItem (Date(2023, 3, 15)) 134<OffsetDay> 491_53L<Cent> 957_65L<Cent> 89_95.392m<Cent> 89_95L<Cent> 401_58L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 22)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }
 
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> 1193_95L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest004.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1193_95L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 1193_95L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
            NewInterest = 26_75.760m<Cent>
            NewCharges = [||]
            PrincipalPortion = 557_45L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 606_50L<Cent>
            ChargesPortion = 30_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> (491_53L<Cent> * 3L)

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest005.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 21)
            OffsetDay = 140<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1474_59L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 1474_59L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = RefundDue
            NewInterest = 26_75.760m<Cent>
            NewCharges = [||]
            PrincipalPortion = 838_09L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 606_50L<Cent>
            ChargesPortion = 30_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -280_64L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = -280_64L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 2<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 4<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed (491_53L<Cent> * 3L); Metadata = Map.empty } }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_64L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest006.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_64L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = -280_64L<Cent>
            PaymentStatus = Refunded
            BalanceStatus = ClosedBalance
            NewInterest = 0m<Cent>
            NewCharges = [||]
            PrincipalPortion = -280_64L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1500_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest007.md")

        let actual = schedule |> ValueOption.bind (_.ScheduleItems >> (Array.vTryLastBut 5))
        let expected = ValueSome {
            OffsetDate = Date(2022, 11, 1)
            OffsetDay = 0<OffsetDay>
            Advances = [| 1500_00L<Cent> |]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 0
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1500_00L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 1500_00L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
            NewInterest = 0m<Cent>
            NewCharges = [||]
            PrincipalPortion = 1500_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 243_86L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 243_86L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 243_86L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest008.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = startDate.AddDays 154
            OffsetDay = 154<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 243_66L<Cent>; Metadata = Map.empty } // to-do: this should be less than the level payment
            Window = 11
            PaymentDue = 243_66L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 243_66L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 24_54.144m<Cent>
            NewCharges = [||]
            PrincipalPortion = 219_12L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 24_54L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 243_66L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 2<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 4<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 140<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed (491_53L<Cent> * 3L); Metadata = Map.empty } }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_83L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest009.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 24)
            OffsetDay = 143<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_83L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = -280_83L<Cent>
            PaymentStatus = Refunded
            BalanceStatus = ClosedBalance
            NewInterest = -18.45304110M<Cent>
            NewCharges = [||]
            PrincipalPortion = -280_64L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = -19L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 400_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest010.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 491_53L<Cent>; Metadata = Map.empty }
            Window = 5
            PaymentDue = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 491_53L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 89_95.392m<Cent>
            NewCharges = [||]
            PrincipalPortion = 401_58L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 89_95L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 491_53L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 491_53L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 400_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest011.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 491_53L<Cent>; Metadata = Map.empty }
            Window = 5
            PaymentDue = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 491_53L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = OpenBalance
            NewInterest = 118_33.696m<Cent>
            NewCharges = [||]
            PrincipalPortion = 373_20L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 118_33L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 155_09L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 646_62L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest012.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 491_53L<Cent>; Metadata = Map.empty }
            Window = 5
            PaymentDue = 457_65L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 500_00L<Cent>
            PaymentStatus = Overpayment
            BalanceStatus = RefundDue
            NewInterest = 79_07.200m<Cent>
            NewCharges = [||]
            PrincipalPortion = 420_93L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 79_07L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -67_93L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = -67_93L<Cent>
            FeesRefundIfSettled = 0L<Cent>
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
                PaymentCount = 4,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 97_01L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

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
                PaymentCount = 24,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 23<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 31<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 38<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 58<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 67<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 73<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 79<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 86<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 93<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 276_37L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 107<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 115<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 122<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 129<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 137<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 143<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 149<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 156<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 166<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 171<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 177<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_38L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 185<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 278_33L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest014.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -61_27L<Cent>)
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
                PaymentCount = 24,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 5000_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest015.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2176_85L<Cent>)
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
                PaymentCount = 24,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 5000_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 20<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest016.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2676_85L<Cent>)
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
                PaymentCount = 24,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 5000_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 20<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 27<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest017.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -3176_85L<Cent>)
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
                PaymentCount = 24,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_89L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 20<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Pending 271_89L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 27<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Pending 271_89L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

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
                PaymentCount = 24,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 271_89L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 20<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Pending 271_89L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 27<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Pending 271_89L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

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
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 9, 5), 4, ValueNone)
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = [|
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 116_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 46<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 116_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 77<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 116_00L<Cent>; Metadata = Map.empty } }
            { PaymentDay = 107<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 116_00L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest020.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = ClosedBalance && si.GeneratedPayment = ValueSome -119_88L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``21) Late payment`` () =
        let sp = {
            AsOfDate = Date(2024, 7, 3)
            StartDate = Date(2024, 4, 12)
            Principal = 100_00L<Cent>
            PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 4, 19); PaymentCount = 4; PaymentAmount= 35_48L<Cent> } |]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = InterestMethod.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = [|
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 35_48L<Cent>; Metadata = Map.empty } }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest021.md")

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = OpenBalance && si.SettlementFigure = 135_59L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    // [<Fact>]
    // let ``22) Partial late payment`` () =
    //     let sp = {
    //         AsOfDate = Date(2024, 7, 3)
    //         StartDate = Date(2024, 4, 12)
    //         Principal = 100_00L<Cent>
    //         PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 4, 19); PaymentCount = 4; PaymentAmount= 35_48L<Cent> } |]
    //         PaymentOptions = {
    //             ScheduledPaymentOption = AsScheduled
    //             CloseBalanceOption = LeaveOpenBalance
    //         }
    //         FeesAndCharges = {
    //             Fees = [||]
    //             FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
    //             FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
    //             Charges = [||]
    //             ChargesHolidays = [||]
    //             ChargesGrouping = OneChargeTypePerDay
    //             LatePaymentGracePeriod = 0<DurationDay>
    //         }
    //         Interest = {
    //             Method = InterestMethod.Simple
    //             StandardRate = Interest.Rate.Daily (Percent 0.8m)
    //             Cap = interestCapExample
    //             InitialGracePeriod = 0<DurationDay>
    //             PromotionalRates = [||]
    //             RateOnNegativeBalance = ValueNone
    //         }
    //         Calculation = {
    //             AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
    //             RoundingOptions = RoundingOptions.recommended
    //             PaymentTimeout = 0<DurationDay>
    //             MinimumPayment = NoMinimumPayment
    //         }
    //     }

    //     let actualPayments = [|
    //         { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 20_00L<Cent>; Metadata = Map.empty } }
    //     |]

    //     let schedule =
    //         actualPayments
    //         |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

    //     schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/ActualPaymentTest022.md")

    //     let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> fun si -> si.BalanceStatus = OpenBalance && si.SettlementFigure = 158_40L<Cent>)
    //     let expected = ValueSome true
    //     actual |> should equal expected
