namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTests =

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding))
    }

    let quickActualPayments (days: int array) levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last |> Array.map(fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed finalPayment |])
            rest |> Array.map(fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed levelPayment |])
        |]
        |> Array.concat
        |> Array.rev
        |> Map.ofArray

    let quickExpectedFinalItem date offsetDay paymentValue eventuallyPaid contractualInterest interestAdjustment interestPortion principalPortion =
        offsetDay,
        {
            OffsetDate = date
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome paymentValue) ValueNone
            Window = 5
            PaymentDue = paymentValue
            EventuallyPaid = eventuallyPaid
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentValue; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = paymentValue
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = contractualInterest
            SimpleInterest = interestAdjustment
            NewInterest = interestAdjustment
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
            SettlementFigure = ValueSome 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

    [<Fact>]
    let ``1) Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest001.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 125<OffsetDay> 456_84L<Cent> 0L<Cent> 0m<Cent> 90_78.288m<Cent> 90_78L<Cent> 366_06L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Standard schedule with month-end payments from 32 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 10, 29)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 32; 63; 94; 122; 153 |] 556_05L<Cent> 556_00L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest002.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 153<OffsetDay> 556_00L<Cent> 0L<Cent> 0m<Cent> 110_48.896m<Cent> 110_48L<Cent> 445_52L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Standard schedule with mid-monthly payments from 14 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 14; 44; 75; 106; 134 |] 491_53L<Cent> 491_53L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest003.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue
        let expected = quickExpectedFinalItem (Date(2023, 3, 15)) 134<OffsetDay> 491_53L<Cent> 0L<Cent> 0m<Cent> 89_95.392m<Cent> 89_95L<Cent> 401_58L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Made 2 payments on early repayment, then one single payment after the full balance is overdue`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 22)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }
 
        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> 1193_95L<Cent>

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest004.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 140<OffsetDay>, {
            OffsetDate = Date(2023, 3, 21)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.zero
            Window = 5
            PaymentDue = 0L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1193_95L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 1193_95L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 26_75.760m<Cent>
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
            SettlementFigure = ValueSome 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``5) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 22)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> (491_53L<Cent> * 3L)

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest005.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 140<OffsetDay>, {
            OffsetDate = Date(2023, 3, 21)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.zero
            Window = 5
            PaymentDue = 0L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1474_59L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 1474_59L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = RefundDue
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 26_75.760m<Cent>
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
            SettlementFigure = ValueSome -280_64L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``6) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 25)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                2<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                4<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                140<OffsetDay>, [| ActualPayment.quickConfirmed (491_53L<Cent> * 3L) |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed -280_64L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest006.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue
        let expected = 143<OffsetDay>, {
            OffsetDate = Date(2023, 3, 24)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.zero
            Window = 5
            PaymentDue = 0L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_64L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = -280_64L<Cent>
            PaymentStatus = Refunded
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 0m<Cent>
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
            SettlementFigure = ValueSome 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``7) 0L<Cent>-day loan`` () =
        let sp = {
            AsOfDate = Date(2022, 11, 1)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                0<OffsetDay>, [| ActualPayment.quickConfirmed 1500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest007.md" false

        let actual = schedule.ScheduleItems |> Map.find 0<OffsetDay>

        let expected = {
            OffsetDate = Date(2022, 11, 1)
            Advances = [| 1500_00L<Cent> |]
            ScheduledPayment = ScheduledPayment.zero
            Window = 0
            PaymentDue = 0L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1500_00L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 1500_00L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 0m<Cent>
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
            SettlementFigure = ValueSome 0L<Cent>
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
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 14)
                PaymentCount = 11
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 243_86L<Cent> |]
                28<OffsetDay>, [| ActualPayment.quickConfirmed 243_86L<Cent> |]
                42<OffsetDay>, [| ActualPayment.quickConfirmed 243_86L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest008.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 154<OffsetDay>, {
            OffsetDate = startDate.AddDays 154
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 243_66L<Cent>) ValueNone
            Window = 11
            PaymentDue = 243_66L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 243_66L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 24_54.144m<Cent>
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
            SettlementFigure = ValueSome 243_66L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``9) Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded (with interest due to the customer on the negative balance)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 25)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                2<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                4<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                140<OffsetDay>, [| ActualPayment.quickConfirmed (491_53L<Cent> * 3L) |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed -280_83L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest009.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 143<OffsetDay>, {
            OffsetDate = Date(2023, 3, 24)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.zero
            Window = 5
            PaymentDue = 0L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_83L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = -280_83L<Cent>
            PaymentStatus = Refunded
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = -18.45304110M<Cent>
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
            SettlementFigure = ValueSome 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``10) Underpayment made should show scheduled payment as net effect while in grace period`` () =
        let sp = {
            AsOfDate = Date(2023, 1, 18)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 400_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest010.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 134<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
            Window = 5
            PaymentDue = 491_53L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 491_53L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 89_95.392m<Cent>
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
            SettlementFigure = ValueSome 491_53L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``11) Underpayment made should show scheduled payment as underpayment after grace period has expired`` () =
        let sp = {
            AsOfDate = Date(2023, 1, 19)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 400_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest011.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 134<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
            Window = 5
            PaymentDue = 491_53L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 491_53L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = OpenBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 118_33.696m<Cent>
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
            SettlementFigure = ValueSome 646_62L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``12) Settled loan`` () =
        let sp = {
            AsOfDate = Date(2034, 1, 31)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest012.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

        let expected = 134<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
            Window = 5
            PaymentDue = 432_07L<Cent>
            EventuallyPaid = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 500_00L<Cent>
            PaymentStatus = Overpayment
            BalanceStatus = RefundDue
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 79_07.200m<Cent>
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
            SettlementFigure = ValueSome -67_93L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }
        
        actual |> should equal expected

    [<Fact>]
    let ``13) Scheduled payment total can be less than principal when early actual payments are made but net effect is never less`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 7)
            StartDate = Date(2024, 2, 2)
            Principal = 250_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 22)
                PaymentCount = 4
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments =
            Map [
                0<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest013.md" false

        let actual = (schedule.ScheduleItems |> Map.values |> Seq.sumBy _.NetEffect) >= sp.Principal

        let expected = true
        
        actual |> should equal expected

    [<Fact>]
    let ``14) Something TH spotted`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6))
                PaymentCount = 24
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                23<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                31<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                38<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                42<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                58<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                67<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                73<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                79<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                86<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                93<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                100<OffsetDay>, [| ActualPayment.quickConfirmed 276_37L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                115<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                129<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                137<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                149<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                156<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                166<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                171<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                177<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                185<OffsetDay>, [| ActualPayment.quickConfirmed 278_33L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest014.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -61_27L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``15) Large overpayment should not result in runaway fee refunds`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6))
                PaymentCount = 24
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 5000_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest015.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2176_85L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``16) Large overpayment should not result in runaway fee refunds (2 actual payments)`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6))
                PaymentCount = 24
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 5000_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest016.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2676_85L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``17) Large overpayment should not result in runaway fee refunds (3 actual payments)`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 13)
            StartDate = Date(2022, 4, 30)
            Principal = 2500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2022, 5, 6))
                PaymentCount = 24
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 5000_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                27<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest017.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -3176_85L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``18) Pending payments should only apply if not timed out`` () =
        let sp = {
            AsOfDate = Date(2024, 1, 30)
            StartDate = Date(2024, 1, 1)
            Principal = 2500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2024, 1, 14))
                PaymentCount = 24
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 271_89L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
                27<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest018.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 111_50L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``19) Pending payments should only apply if not timed out`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 1)
            StartDate = Date(2024, 1, 1)
            Principal = 2500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(1, Date(2024, 1, 14))
                PaymentCount = 24
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 271_89L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
                27<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest019.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 222_71L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``20) Generated settlement figure is correct`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 2)
            StartDate = Date(2023, 8, 20)
            Principal = 250_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 5); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                16<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
                46<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
                77<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest020.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = ClosedBalance && si.GeneratedPayment = GeneratedValue -119_88L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``21) Late payment`` () =
        let sp = {
            AsOfDate = Date(2024, 7, 3)
            StartDate = Date(2024, 4, 12)
            Principal = 100_00L<Cent>
            ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 4, 19); PaymentCount = 4; PaymentValue= 35_48L<Cent>; ScheduleType = ScheduleType.Original } |]
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                28<OffsetDay>, [| ActualPayment.quickConfirmed 35_48L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest021.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.SettlementFigure = ValueSome 135_59L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``22) Some refund`` () =
        let sp = {
            AsOfDate = Date(2025, 2, 19)
            StartDate = Date(2021, 12, 29)
            Principal = 650_00L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                30<OffsetDay>, ScheduledPayment.quick (ValueSome 318_50L<Cent>) ValueNone
                61<OffsetDay>, ScheduledPayment.quick (ValueSome 318_50L<Cent>) ValueNone
                89<OffsetDay>, ScheduledPayment.quick (ValueSome 318_50L<Cent>) ValueNone
                120<OffsetDay>, ScheduledPayment.quick (ValueSome 318_50L<Cent>) ValueNone
            ]
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments = //Map.empty
            Map [
                29<OffsetDay>, [| ActualPayment.quickConfirmed 318_50L<Cent> |]
                61<OffsetDay>, [| ActualPayment.quickConfirmed 318_50L<Cent> |]
                72<OffsetDay>, [| ActualPayment.quickConfirmed 387_40L<Cent> |]
                1062<OffsetDay>, [| ActualPayment.quickConfirmed -12_88L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest022.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.SettlementFigure = ValueSome 135_59L<Cent>
        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ``23) Interesting payment timings`` () =
        let rescheduledPayment1 = { Value = 30_00L<Cent>; RescheduleDay = 60<OffsetDay> }
        let rescheduledPayment2 = { Value = 40_18L<Cent>; RescheduleDay = 60<OffsetDay> }
        let sp = {
            AsOfDate = Date(2024, 7, 14)
            StartDate = Date(2023, 8, 6)
            Principal = 280_00L<Cent>
            ScheduleConfig = [|
                32<OffsetDay>, ScheduledPayment.quick (ValueSome 138_88L<Cent>) ValueNone
                62<OffsetDay>, ScheduledPayment.quick (ValueSome 138_88L<Cent>) ValueNone
                93<OffsetDay>, ScheduledPayment.quick (ValueSome 138_88L<Cent>) ValueNone
                123<OffsetDay>, ScheduledPayment.quick (ValueSome 138_88L<Cent>) ValueNone
                208<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment1)
                239<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment1)
                269<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment1)
                300<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment1)
                330<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment1)
                361<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment1)
                392<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment2)
                422<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment2)
                453<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment2)
                483<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment2)
                514<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment2)
                545<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome rescheduledPayment2)
            |]
            |> mergeScheduledPayments
            |> CustomSchedule
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                InterestRounding = RoundDown
            }
        }

        let actualPayments =
            Map [
                32<OffsetDay>, [| ActualPayment.quickConfirmed 138_88L<Cent> |]
                207<OffsetDay>, [| ActualPayment.quickConfirmed 30_00L<Cent> |]
                242<OffsetDay>, [| ActualPayment.quickConfirmed 30_04L<Cent> |]
                300<OffsetDay>, [| ActualPayment.quickConfirmed 30_04L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ActualPaymentTest023.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.SettlementFigure = ValueSome 135_59L<Cent>
        let expected = true
        actual |> should equal expected
