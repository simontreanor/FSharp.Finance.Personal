namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module FeesAndChargesTests =

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit))
    }

    module ChargesTests =

        [<Fact>]
        let ChargesTest000 () =
            let title = "ChargesTest000"
            let description = "One charge type per day"
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

            let actualPayments =
                Map [
                    4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    36<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    40<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    66<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]; ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    70<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                ]

            let schedule =
                actualPayments
                |> Amortisation.generate sp ValueNone false

            Schedule.outputHtmlToFile title description sp schedule

            let actual = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue

            let expected = 125<OffsetDay>, {
                OffsetDate = Date(2023, 3, 31)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 456_84L<Cent>) ValueNone
                Window = 5
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                SimpleInterest = 11256.472m<Cent>
                NewInterest = 11256.472m<Cent>
                NewCharges = [||]
                PrincipalPortion = 344_28L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 112_56L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 109_61L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = ValueSome 109_61L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

            actual |> should equal expected

        [<Fact>]
        let ChargesTest001 () =
            let title = "ChargesTest001"
            let description = "One charge type per product"
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
                    ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerProduct
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
                    4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    36<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    40<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    66<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]; ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    70<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                ]

            let schedule =
                actualPayments
                |> Amortisation.generate sp ValueNone false

            Schedule.outputHtmlToFile title description sp schedule

            let actual = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue

            let expected = 125<OffsetDay>, {
                OffsetDate = Date(2023, 3, 31)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 456_84L<Cent>) ValueNone
                Window = 5
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                SimpleInterest = 10665.24m<Cent>
                NewInterest = 10665.24m<Cent>
                NewCharges = [||]
                PrincipalPortion = 350_19L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 106_65L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 79_86L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = ValueSome 79_86L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

            actual |> should equal expected

        [<Fact>]
        let ChargesTest002 () =
            let title = "ChargesTest002"
            let description = "All charges applied"
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
                    ChargeGrouping = Charge.ChargeGrouping.AllChargesApplied
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
                    4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    36<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    40<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    66<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]; ActualPayment.quickFailed 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
                    70<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                ]

            let schedule =
                actualPayments
                |> Amortisation.generate sp ValueNone false

            Schedule.outputHtmlToFile title description sp schedule

            let actual = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue
            
            let expected = 125<OffsetDay>, {
                OffsetDate = Date(2023, 3, 31)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 456_84L<Cent>) ValueNone
                Window = 5
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                SimpleInterest = 11552.088m<Cent>
                NewInterest = 11552.088m<Cent>
                NewCharges = [||]
                PrincipalPortion = 341_32L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 115_52L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 124_49L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = ValueSome 124_49L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

            actual |> should equal expected
