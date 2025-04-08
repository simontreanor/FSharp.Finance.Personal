namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module IllustrativeTests =

    open System
    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit))
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

    let quickExpectedFinalItem date offsetDay paymentValue window interestAdjustment interestPortion principalPortion =
        offsetDay,
        {
            OffsetDate = date
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome paymentValue) ValueNone
            Window = window
            PaymentDue = paymentValue
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentValue; Metadata = Map.empty } |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = paymentValue
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
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
    let IllustrativeTest000 () =
        let title = "IllustrativeTest000"
        let description = "Borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) - all paid on time"
        let sp = {
            AsOfDate = Date(2025, 7, 1)
            StartDate = Date(2025, 3, 1)
            Principal = 400_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2025, 3, 31)
                PaymentCount = 4
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [||]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 30; 60; 91; 121 |] 181_37L<Cent> 181_36L<Cent>

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue
        let expected =
            121<OffsetDay>,
            {
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_36L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_36L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_36L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_36L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                SimpleInterest = 43_52.64m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 181_36L<Cent>
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
    let IllustrativeTest001 () =
        let title = "IllustrativeTest001"
        let description = "Based on borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) - missed first repayment and then paid before second repayment due date (30/04/2025)"
        let sp = {
            AsOfDate = Date(2025, 7, 1)
            StartDate = Date(2025, 3, 1)
            Principal = 400_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2025, 3, 31)
                PaymentCount = 4
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [||]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 59; 60; 91; 121 |] 181_37L<Cent> 181_36L<Cent>

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue
        let expected =
            121<OffsetDay>,
            {
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_36L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_36L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_36L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_36L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                SimpleInterest = 43_52.64m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 181_36L<Cent>
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
    let IllustrativeTest002 () =
        let title = "IllustrativeTest002"
        let description = "Based on borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) - missed first repayment and did not pay before second repayment due date (30/04/2025)"
        let sp = {
            AsOfDate = Date(2025, 7, 1)
            StartDate = Date(2025, 3, 1)
            Principal = 400_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2025, 3, 31)
                PaymentCount = 4
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [||]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

        let actualPayments = quickActualPayments [| 60; 61; 91; 121 |] 181_37L<Cent> 181_36L<Cent>

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue
        let expected =
            121<OffsetDay>,
            {
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_36L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_36L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_36L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_36L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                SimpleInterest = 43_52.64m<Cent>
                NewInterest = 31.16m<Cent>
                NewCharges = [||]
                PrincipalPortion = 181_05L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 31L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 31L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = ValueSome 31L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
        actual |> should equal expected

