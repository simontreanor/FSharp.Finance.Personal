namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module FeeAndChargesTests =

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    let interestCapExample: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    module ChargesTests =

        let folder = "Charges"

        let parameters: Parameters = {
            Basic = {
                EvaluationDate = Date(2023, 3, 31)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                        ScheduleLength = PaymentCount 5
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Daily(Percent 0.8m)
                    Cap = interestCapExample
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                }
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig =
                    Some {
                        ChargeTypes =
                            Map [
                                Charge.LatePayment,
                                {
                                    Value = 10_00L<Cent>
                                    ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                                    ChargeHolidays = [||]
                                }
                                Charge.InsufficientFunds,
                                {
                                    Value = 10_00L<Cent>
                                    ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                                    ChargeHolidays = [||]
                                }
                            ]
                    }
                InterestConfig = {
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = false
            }
        }

        [<Fact>]
        let ChargesTest000 () =
            let title = "ChargesTest000"
            let description = "One charge type per day"

            let actualPayments =
                Map [
                    4<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    35<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    36<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    40<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    66<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                        1, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    70<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
                    94<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    125<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
                ]

            let schedules = amortise parameters actualPayments

            Schedule.outputHtmlToFile folder title description parameters "" schedules

            let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

            let expected =
                125<OffsetDay>,
                {
                    OffsetDayType = OffsetDayType.EvaluationDay
                    OffsetDate = Date(2023, 3, 31)
                    Advances = [||]
                    ScheduledPayment = ScheduledPayment.quick (ValueSome 456_84L<Cent>) ValueNone
                    Window = 5
                    PaymentDue = 456_84L<Cent>
                    ActualPayments =
                        Map [
                            0,
                            {
                                ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>
                                Metadata = Map.empty
                                ScheduledPayments = Map.empty
                            }
                        ]
                    GeneratedPayment = NoGeneratedPayment
                    NetEffect = 456_84L<Cent>
                    PaymentStatus = PaymentMade
                    BalanceStatus = OpenBalance
                    ActuarialInterest = 11256.472m<Cent>
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

            let p = {
                parameters with
                    Advanced.ChargeConfig =
                        parameters.Advanced.ChargeConfig
                        |> Option.map (fun cc -> {
                            cc with
                                ChargeTypes =
                                    cc.ChargeTypes
                                    |> Map.map (fun _ ct -> {
                                        ct with
                                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerSchedule
                                    })
                        })
            }

            let actualPayments =
                Map [
                    4<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    35<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    36<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    40<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    66<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                        1, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    70<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
                    94<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    125<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
                ]

            let schedules = amortise p actualPayments

            Schedule.outputHtmlToFile folder title description p "" schedules

            let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

            let expected =
                125<OffsetDay>,
                {
                    OffsetDayType = OffsetDayType.EvaluationDay
                    OffsetDate = Date(2023, 3, 31)
                    Advances = [||]
                    ScheduledPayment = ScheduledPayment.quick (ValueSome 456_84L<Cent>) ValueNone
                    Window = 5
                    PaymentDue = 456_84L<Cent>
                    ActualPayments =
                        Map [
                            0,
                            {
                                ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>
                                Metadata = Map.empty
                                ScheduledPayments = Map.empty
                            }
                        ]
                    GeneratedPayment = NoGeneratedPayment
                    NetEffect = 456_84L<Cent>
                    PaymentStatus = PaymentMade
                    BalanceStatus = OpenBalance
                    ActuarialInterest = 10665.24m<Cent>
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

            let p = {
                parameters with
                    Advanced.ChargeConfig =
                        parameters.Advanced.ChargeConfig
                        |> Option.map (fun cc -> {
                            cc with
                                ChargeTypes =
                                    cc.ChargeTypes
                                    |> Map.map (fun _ ct -> {
                                        ct with
                                            ChargeGrouping = Charge.ChargeGrouping.AllChargesApplied
                                    })
                        })
            }

            let actualPayments =
                Map [
                    4<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    35<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    36<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    40<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    66<OffsetDay>,
                    Map [
                        0, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                        1, ActualPayment.quickFailed 456_88L<Cent> (ValueSome Charge.InsufficientFunds)
                    ]
                    70<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
                    94<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
                    125<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
                ]

            let schedules = amortise p actualPayments

            Schedule.outputHtmlToFile folder title description p "" schedules

            let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

            let expected =
                125<OffsetDay>,
                {
                    OffsetDayType = OffsetDayType.EvaluationDay
                    OffsetDate = Date(2023, 3, 31)
                    Advances = [||]
                    ScheduledPayment = ScheduledPayment.quick (ValueSome 456_84L<Cent>) ValueNone
                    Window = 5
                    PaymentDue = 456_84L<Cent>
                    ActualPayments =
                        Map [
                            0,
                            {
                                ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>
                                Metadata = Map.empty
                                ScheduledPayments = Map.empty
                            }
                        ]
                    GeneratedPayment = NoGeneratedPayment
                    NetEffect = 456_84L<Cent>
                    PaymentStatus = PaymentMade
                    BalanceStatus = OpenBalance
                    ActuarialInterest = 11552.088m<Cent>
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
