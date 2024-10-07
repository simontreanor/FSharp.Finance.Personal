namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTests =

    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open Formatting
    open PaymentSchedule
    open Percentages
    open ValueOptionCE

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    let quickActualPayments (days: int array) levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last |> Array.map(fun d -> (d * 1<OffsetDay>), [| ActualPayment.QuickConfirmed finalPayment |])
            rest |> Array.map(fun d -> (d * 1<OffsetDay>), [| ActualPayment.QuickConfirmed levelPayment |])
        |]
        |> Array.concat
        |> Array.rev
        |> Map.ofArray

    let quickExpectedFinalItem date offsetDay paymentValue contractualInterest interestAdjustment interestPortion principalPortion = ValueSome (offsetDay, {
        OffsetDate = date
        Advances = [||]
        ScheduledPayment = ScheduledPayment.Quick (ValueSome paymentValue) ValueNone
        Window = 5
        PaymentDue = paymentValue
        ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentValue; Metadata = Map.empty } |]
        GeneratedPayment = ValueNone
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
        SettlementFigure = 0L<Cent>
        FeesRefundIfSettled = 0L<Cent>
    })

    [<Fact>]
    let ``1) Standard schedule with month-end payments from 4 days and paid off on time`` () =
        let sp = {
            AsOfDate = Date(2023, 4, 1)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter (_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest001.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 125<OffsetDay> 456_84L<Cent> 0m<Cent> 90_78.288m<Cent> 90_78L<Cent> 366_06L<Cent>
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest002.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)
        let expected = quickExpectedFinalItem (Date(2023, 3, 31)) 153<OffsetDay> 556_00L<Cent> 0m<Cent> 110_48.896m<Cent> 110_48L<Cent> 445_52L<Cent>
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest003.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)
        let expected = quickExpectedFinalItem (Date(2023, 3, 15)) 134<OffsetDay> 491_53L<Cent> 0m<Cent> 89_95.392m<Cent> 89_95L<Cent> 401_58L<Cent>
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest004.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)
        let expected = ValueSome (140<OffsetDay>, {
            OffsetDate = Date(2023, 3, 21)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1193_95L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest005.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)
        let expected = ValueSome (140<OffsetDay>, {
            OffsetDate = Date(2023, 3, 21)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1474_59L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
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
            SettlementFigure = -280_64L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                2<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                4<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                140<OffsetDay>, [| ActualPayment.QuickConfirmed (491_53L<Cent> * 3L) |]
                143<OffsetDay>, [| ActualPayment.QuickConfirmed -280_64L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest006.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)
        let expected = ValueSome (143<OffsetDay>, {
            OffsetDate = Date(2023, 3, 24)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_64L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                0<OffsetDay>, [| ActualPayment.QuickConfirmed 1500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest007.md" false))

        let actual = schedule |> ValueOption.bind (_.ScheduleItems >> Map.tryFind 0<OffsetDay> >> toValueOption)

        let expected = ValueSome ({
            OffsetDate = Date(2022, 11, 1)
            Advances = [| 1500_00L<Cent> |]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 0
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 1500_00L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 243_86L<Cent> |]
                28<OffsetDay>, [| ActualPayment.QuickConfirmed 243_86L<Cent> |]
                42<OffsetDay>, [| ActualPayment.QuickConfirmed 243_86L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest008.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (154<OffsetDay>, {
            OffsetDate = startDate.AddDays 154
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 243_66L<Cent>) ValueNone
            Window = 11
            PaymentDue = 243_66L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 243_66L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })

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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                2<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                4<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                140<OffsetDay>, [| ActualPayment.QuickConfirmed (491_53L<Cent> * 3L) |]
                143<OffsetDay>, [| ActualPayment.QuickConfirmed -280_83L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest009.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (143<OffsetDay>, {
            OffsetDate = Date(2023, 3, 24)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 5
            PaymentDue = 0L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_83L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 400_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest010.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (134<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 491_53L<Cent>) ValueNone
            Window = 5
            PaymentDue = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 491_53L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })

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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 491_53L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 400_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest011.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (134<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 491_53L<Cent>) ValueNone
            Window = 5
            PaymentDue = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
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
            SettlementFigure = 646_62L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })

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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest012.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (134<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 491_53L<Cent>) ValueNone
            Window = 5
            PaymentDue = 457_65L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
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
            SettlementFigure = -67_93L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                0<OffsetDay>, [| ActualPayment.QuickConfirmed 97_01L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest013.md" false))

        let actual = schedule |> ValueOption.map (fun s -> (s.ScheduleItems |> Map.values |> Seq.sumBy _.NetEffect) >= sp.Principal)

        let expected = ValueSome true
        
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                23<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                31<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                38<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                42<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                58<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                67<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                73<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                79<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                86<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                93<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |]
                100<OffsetDay>, [| ActualPayment.QuickConfirmed 276_37L<Cent> |]
                107<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                115<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                122<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                129<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                137<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                143<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                149<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                156<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                166<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                171<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                177<OffsetDay>, [| ActualPayment.QuickConfirmed 278_38L<Cent> |]
                185<OffsetDay>, [| ActualPayment.QuickConfirmed 278_33L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest014.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -61_27L<Cent>)
        let expected = ValueSome true
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.QuickConfirmed 5000_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest015.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2176_85L<Cent>)
        let expected = ValueSome true
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.QuickConfirmed 5000_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest016.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2676_85L<Cent>)
        let expected = ValueSome true
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.QuickConfirmed 5000_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                27<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest017.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -3176_85L<Cent>)
        let expected = ValueSome true
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.QuickConfirmed 271_89L<Cent> |]
                20<OffsetDay>, [| ActualPayment.QuickPending 271_89L<Cent> |]
                27<OffsetDay>, [| ActualPayment.QuickPending 271_89L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest018.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 111_50L<Cent>)
        let expected = ValueSome true
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
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.QuickConfirmed 271_89L<Cent> |]
                20<OffsetDay>, [| ActualPayment.QuickPending 271_89L<Cent> |]
                27<OffsetDay>, [| ActualPayment.QuickPending 271_89L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest019.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 222_71L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``20) Generated settlement figure is correct`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 2)
            StartDate = Date(2023, 8, 20)
            Principal = 250_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 5); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                16<OffsetDay>, [| ActualPayment.QuickConfirmed 116_00L<Cent> |]
                46<OffsetDay>, [| ActualPayment.QuickConfirmed 116_00L<Cent> |]
                77<OffsetDay>, [| ActualPayment.QuickConfirmed 116_00L<Cent> |]
                107<OffsetDay>, [| ActualPayment.QuickConfirmed 116_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest020.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = ClosedBalance && si.GeneratedPayment = ValueSome -119_88L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected

    [<Fact>]
    let ``21) Late payment`` () =
        let sp = {
            AsOfDate = Date(2024, 7, 3)
            StartDate = Date(2024, 4, 12)
            Principal = 100_00L<Cent>
            ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 4, 19); PaymentCount = 4; PaymentValue= 35_48L<Cent>; ScheduleType = ScheduleType.Original } |]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
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

        let actualPayments =
            Map [
                28<OffsetDay>, [| ActualPayment.QuickConfirmed 35_48L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/ActualPaymentTest021.md" false))

        let actual = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.SettlementFigure = 135_59L<Cent>)
        let expected = ValueSome true
        actual |> should equal expected
