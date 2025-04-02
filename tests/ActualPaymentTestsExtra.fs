namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTestsExtra =

    open Amortisation
    open Calculation
    open DateDay
    open Scheduling
    open Rescheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, RoundDown))
    }

    /// creates an array of actual payments made on time and in full according to an array of scheduled payments
    let allPaidOnTime (scheduleItems: SimpleItem array) =
        scheduleItems
        |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
        |> Array.map(fun si -> si.Day, [| ActualPayment.quickConfirmed <| ScheduledPayment.total si.ScheduledPayment |])
        |> Map.ofArray

    [<Fact>]
    let ActualPaymentTestExtra000 () =
        let title = "ActualPaymentTestExtra000"
        let description = "Simple schedule fully settled on time"
        let sp = {
            AsOfDate = Date(2023, 12, 1)
            StartDate = Date(2023, 7, 23)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 8, 1)
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
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let actual =
            let schedule = calculate sp BelowZero
            let scheduleItems = schedule.Items
            let actualPayments = scheduleItems |> allPaidOnTime
            let amortisationSchedule = Amortisation.generate sp ValueNone false actualPayments
            amortisationSchedule |> Schedule.outputHtmlToFile title description sp
            amortisationSchedule |> fst |> _.ScheduleItems |> Map.maxKeyValue

        let expected = 131<OffsetDay>, {
            OffsetDate = Date(2023, 12, 1)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 407_64L<Cent>) ValueNone
            Window = 5
            PaymentDue = 407_64L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 407_64L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 407_64L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 3_30.67257534m<Cent>
            NewInterest = 3_30.67257534m<Cent>
            NewCharges = [||]
            PrincipalPortion = 161_76L<Cent>
            FeesPortion = 242_58L<Cent>
            InterestPortion = 3_30L<Cent>
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
    let ActualPaymentTestExtra001 () =
        let title = "ActualPaymentTestExtra001"
        let description = "Schedule with a payment on day 0L<Cent>, seen from a date before scheduled payments are due to start"
        let sp = {
            AsOfDate = Date(2022, 3, 25)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
                PaymentCount = 12
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
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let actual =
            let actualPayments =
                Map [
                    0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |]
                ]
            let amortisationSchedule = Amortisation.generate sp ValueNone false actualPayments
            amortisationSchedule |> Schedule.outputHtmlToFile title description sp
            amortisationSchedule |> fst |> _.ScheduleItems |> Map.maxKeyValue

        let expected = 172<OffsetDay>, {
            OffsetDate = Date(2022, 8, 27)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 170_90L<Cent>) ValueNone
            Window = 12
            PaymentDue = 170_04L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 170_04L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 64.65046575m<Cent>
            NewInterest = 64.65046575m<Cent>
            NewCharges = [||]
            PrincipalPortion = 67_79L<Cent>
            FeesPortion = 101_61L<Cent>
            InterestPortion = 64L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 170_04L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTestExtra002 () =
        let title = "ActualPaymentTestExtra002"
        let description = "Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, seen from a date after the original settlement date, showing the effect of projected small payments until paid off"
        let sp = {
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
                PaymentCount = 12
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
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let originalFinalPaymentDay = generatePaymentMap sp.StartDate sp.ScheduleConfig |> Map.keys |> Seq.toArray |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        let originalFinalPaymentDay' = (int originalFinalPaymentDay - int (sp.AsOfDate - sp.StartDate).Days) * 1<OffsetDay>

        let actual =
            let actualPayments =
                Map [
                    0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |]
                ]
            let rescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate
            let rp : RescheduleParameters = {
                FeeSettlementRefund = Fee.SettlementRefund.ProRataRescheduled originalFinalPaymentDay'
                PaymentSchedule = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Weekly(2, Date(2022, 9, 1)); PaymentCount = 155; PaymentValue = 20_00L<Cent>; ScheduleType = ScheduleType.Rescheduled rescheduleDay } |]
                RateOnNegativeBalance = Interest.Rate.Zero
                PromotionalInterestRates = [||]
                ChargeHolidays = [||]
                SettlementDay = ValueNone
            }
            let oldSchedule, newSchedule = reschedule sp rp actualPayments
            newSchedule |> Schedule.outputHtmlToFile title description sp
            newSchedule |> fst |> _.ScheduleItems
            |> Map.maxKeyValue

        let expected = 1969<OffsetDay>, {
            OffsetDate = Date(2027, 7, 29)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick ValueNone (ValueSome { Value = 20_00L<Cent>; RescheduleDay = 176<OffsetDay> })
            Window = 141
            PaymentDue = 9_80L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 9_80L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 3.72866027m<Cent>
            NewInterest = 3.72866027m<Cent>
            NewCharges = [||]
            PrincipalPortion = 4_39L<Cent>
            FeesPortion = 5_38L<Cent>
            InterestPortion = 3L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 9_80L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTestExtra003 () =
        let title = "ActualPaymentTestExtra003"
        let description = "never settles down"
        let sp = {
            AsOfDate = Date(2026, 8, 27)
            StartDate = Date(2023, 11, 6)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly (8, Date(2023, 11, 23))
                PaymentCount = 19
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
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 164m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.Zero
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.12m)
                Cap = { TotalAmount = ValueSome <| Amount.Simple 500_00L<Cent>; DailyAmount = ValueNone }
                InitialGracePeriod = 7<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let actual =
            let schedule = Scheduling.calculate sp BelowZero
            let scheduleItems = schedule.Items
            let actualPayments = scheduleItems |> allPaidOnTime
            let amortisationSchedule = Amortisation.generate sp ValueNone false actualPayments
            amortisationSchedule |> Schedule.outputHtmlToFile title description sp
            amortisationSchedule |> fst |> _.ScheduleItems |> Map.maxKeyValue

        let expected = 1025<OffsetDay>, {
            OffsetDate = Date(2026, 8, 27)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 137_36L<Cent>) ValueNone
            Window = 19
            PaymentDue = 137_36L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 137_36L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 137_36L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 0m<Cent>
            NewInterest = 0m<Cent>
            NewCharges = [||]
            PrincipalPortion = 52_14L<Cent>
            FeesPortion = 85_22L<Cent>
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
    let ActualPaymentTestExtra004 () =
        let title = "ActualPaymentTestExtra004"
        let description = "large negative payment"
        let sp = {
            AsOfDate = Date(2023, 12, 11)
            StartDate = Date(2022, 9, 11)
            Principal = 200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly (1, 2022, 9, 15)
                PaymentCount = 7
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

        let actual =
            let schedule = Scheduling.calculate sp BelowZero
            let scheduleItems = schedule.Items
            let actualPayments = scheduleItems |> allPaidOnTime
            let amortisationSchedule = Amortisation.generate sp ValueNone false actualPayments
            amortisationSchedule |> Schedule.outputHtmlToFile title description sp
            amortisationSchedule |> fst |> _.ScheduleItems |> Map.maxKeyValue

        let expected = 185<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 51_53L<Cent>) ValueNone
            Window = 7
            PaymentDue = 51_53L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 51_53L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 51_53L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 9_43.040m<Cent>
            NewInterest = 9_43.040m<Cent>
            NewCharges = [||]
            PrincipalPortion = 42_10L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 9_43L<Cent>
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
    let ActualPaymentTestExtra005 () =
        let title = "ActualPaymentTestExtra005"
        let description = "Schedule with a payment on day 0L<Cent>, seen from a date after the first unpaid scheduled payment, but within late-payment grace period"
        let sp = {
            AsOfDate = Date(2022, 4, 1)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
                PaymentCount = 12
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
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 7<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let actual =
            let actualPayments = Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |] ]
            let amortisationSchedule = Amortisation.generate sp ValueNone false actualPayments
            amortisationSchedule |> Schedule.outputHtmlToFile title description sp
            amortisationSchedule |> fst |> _.ScheduleItems |> Map.find 144<OffsetDay>

        let expected = {
            OffsetDate = Date(2022, 7, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 171_02L<Cent>) ValueNone
            Window = 10
            PaymentDue = 142_40L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 142_40L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 1_28.41170136m<Cent>
            NewInterest = 1_28.41170136m<Cent>
            NewCharges = [||]
            PrincipalPortion = 134_62L<Cent>
            FeesPortion = 6_50L<Cent>
            InterestPortion = 1_28L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 195_35L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 142_40L<Cent>
            FeesRefundIfSettled = 195_35L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTestExtra006 () =
        let title = "ActualPaymentTestExtra006"
        let description = "Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fees rolled over)"
        let sp = {
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
                PaymentCount = 12
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
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let originalFinalPaymentDay = generatePaymentMap sp.StartDate sp.ScheduleConfig |> Map.keys |> Seq.toArray |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        let originalFinalPaymentDay' = (int originalFinalPaymentDay - int (sp.AsOfDate - sp.StartDate).Days) * 1<OffsetDay>

        let actual =
            let actualPayments =
                Map [
                    0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |]
                ]
            let rp : RolloverParameters = {
                OriginalFinalPaymentDay = originalFinalPaymentDay'
                PaymentSchedule = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Weekly(2, Date(2022, 9, 1)); PaymentCount = 155; PaymentValue = 20_00L<Cent>; ScheduleType = ScheduleType.Original } |]
                InterestConfig = sp.InterestConfig
                PaymentConfig = sp.PaymentConfig
                FeeHandling = Fee.FeeHandling.CarryOverAsIs
            }
            let oldSchedule, newSchedule = rollOver sp rp actualPayments
            newSchedule |> Schedule.outputHtmlToFile title description sp
            newSchedule |> fst |> _.ScheduleItems
            |> Map.maxKeyValue

        let expected = 1793<OffsetDay>, {
            OffsetDate = Date(2027, 7, 29)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 20_00L<Cent>) ValueNone
            Window = 129
            PaymentDue = 18_71L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 18_71L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 7.11384109m<Cent>
            NewInterest = 7.11384109m<Cent>
            NewCharges = [||]
            PrincipalPortion = 9_26L<Cent>
            FeesPortion = 9_38L<Cent>
            InterestPortion = 7L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 18_71L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTestExtra007 () =
        let title = "ActualPaymentTestExtra007"
        let description = "Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fees not rolled over)"
        let sp = {
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
                PaymentCount = 12
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
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                InterestRounding = RoundDown
            }
        }

        let originalFinalPaymentDay = generatePaymentMap sp.StartDate sp.ScheduleConfig |> Map.keys |> Seq.toArray |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        let originalFinalPaymentDay' = (int originalFinalPaymentDay - int (sp.AsOfDate - sp.StartDate).Days) * 1<OffsetDay>

        let actual =
            let actualPayments =
                Map [
                    0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |]
                ]
            let rp : RolloverParameters = {
                OriginalFinalPaymentDay = originalFinalPaymentDay'
                PaymentSchedule = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Weekly(2, Date(2022, 9, 1)); PaymentCount = 155; PaymentValue = 20_00L<Cent>; ScheduleType = ScheduleType.Original } |]
                InterestConfig = sp.InterestConfig
                PaymentConfig = sp.PaymentConfig
                FeeHandling = Fee.FeeHandling.CapitaliseAsPrincipal
            }
            let oldSchedule, newSchedule = rollOver sp rp actualPayments
            newSchedule |> Schedule.outputHtmlToFile title description sp
            newSchedule |> fst |> _.ScheduleItems
            |> Map.maxKeyValue

        let expected = 1793<OffsetDay>, {
            OffsetDate = Date(2027, 7, 29)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 20_00L<Cent>) ValueNone
            Window = 129
            PaymentDue = 18_71L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 18_71L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 7.11384109m<Cent>
            NewInterest = 7.11384109m<Cent>
            NewCharges = [||]
            PrincipalPortion = 18_64L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 7L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 18_71L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected
