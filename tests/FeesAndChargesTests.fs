namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module FeeAndChargesTests =

    open Amortisation
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    let interestCapExample : Interest.Cap = {
        TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
    }

    module ChargesTests =

        let folder = "Charges"

        [<Fact>]
        let ChargesTest000 () =
            let title = "ChargesTest000"
            let description = "One charge type per day"
            let sp = {
                EvaluationDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    Rounding = RoundUp
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = None
                ChargeConfig = Some {
                    ChargeTypes = Map [
                        Charge.LatePayment, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                            ChargeHolidays = [||]
                        }
                        Charge.InsufficientFunds, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                            ChargeHolidays = [||]
                        }
                    ]
                }
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                }
            }

            let actualPayments =
                Map [
                    4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    36<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    40<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    66<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds); ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    70<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                ]

            let schedules =
                actualPayments
                |> Amortisation.generate sp SettlementDay.NoSettlement false

            Schedule.outputHtmlToFile folder title description sp schedules

            let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

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
                FeePortion = 0L<Cent>
                InterestPortion = 112_56L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 109_61L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 109_61L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

            actual |> should equal expected

        [<Fact>]
        let ChargesTest001 () =
            let title = "ChargesTest001"
            let description = "One charge type per schedule"
            let sp = {
                EvaluationDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    Rounding = RoundUp
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = None
                ChargeConfig = Some {
                    ChargeTypes = Map [
                        Charge.LatePayment, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerSchedule
                            ChargeHolidays = [||]
                        }
                        Charge.InsufficientFunds, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerSchedule
                            ChargeHolidays = [||]
                        }
                    ]
                }
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                }
            }

            let actualPayments =
                Map [
                    4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    36<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    40<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    66<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds); ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    70<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                ]

            let schedules =
                actualPayments
                |> Amortisation.generate sp SettlementDay.NoSettlement false

            Schedule.outputHtmlToFile folder title description sp schedules

            let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

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
                FeePortion = 0L<Cent>
                InterestPortion = 106_65L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 79_86L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 79_86L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

            actual |> should equal expected

        [<Fact>]
        let ChargesTest002 () =
            let title = "ChargesTest002"
            let description = "All charges applied"
            let sp = {
                EvaluationDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    ScheduledPaymentOption = AsScheduled
                    Rounding = RoundUp
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = None
                ChargeConfig = Some {
                    ChargeTypes = Map [
                        Charge.LatePayment, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.AllChargesApplied
                            ChargeHolidays = [||]
                        }
                        Charge.InsufficientFunds, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.AllChargesApplied
                            ChargeHolidays = [||]
                        }
                    ]
                }
                InterestConfig = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                }
            }

            let actualPayments =
                Map [
                    4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    35<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    36<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    40<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    66<OffsetDay>, [| ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds); ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds) |]
                    70<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                    94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
                    125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
                ]

            let schedules =
                actualPayments
                |> Amortisation.generate sp SettlementDay.NoSettlement false

            Schedule.outputHtmlToFile folder title description sp schedules

            let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue
            
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
                FeePortion = 0L<Cent>
                InterestPortion = 115_52L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 124_49L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 124_49L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

            actual |> should equal expected
