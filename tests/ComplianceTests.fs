namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ComplianceTests =

    let folder = "Compliance"

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

    let startDate1 = Date(2023, 11, 6)

    let parameters1: Parameters = {
        Basic = {
            EvaluationDate = startDate1.AddDays 180
            StartDate = startDate1
            Principal = 1000_00uL<Cent>
            ScheduleConfig =
                CustomSchedule
                <| Map [
                    31u<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46uL<Cent>) ValueNone
                    61u<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46uL<Cent>) ValueNone
                    90u<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46uL<Cent>) ValueNone
                    120u<OffsetDay>, ScheduledPayment.quick (ValueSome 451_43uL<Cent>) ValueNone
                ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = interestCapExample
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom
                AprPrecision = 3u
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50uL<Cent>
                Timeout = 3u<OffsetDay>
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 3u<OffsetDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    [<Fact>]
    let ComplianceTest000 () =
        let title = "ComplianceTest000"
        let description = "Repayments made on time"

        let actualPayments =
            Map [
                31u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                61u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                90u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                120u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_43uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest001 () =
        let title = "ComplianceTest001"
        let description = "Repayments made early"

        let actualPayments =
            Map [
                31u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                61u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                81u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                101u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 350_31uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest002 () =
        let title = "ComplianceTest002"
        let description = "Full repayment made on repayment 3"

        let actualPayments =
            Map [
                31u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                61u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                90u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 794_55uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest003 () =
        let title = "ComplianceTest003"
        let description = "Repayments made late - 3 and 4"

        let actualPayments =
            Map [
                31u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                61u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                95u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 451_46uL<Cent> ]
                130u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 505_60uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let startDate2 = Date(2021, 12, 14)

    let parameters2: Parameters = {
        Basic = {
            EvaluationDate = startDate2.AddDays 180
            StartDate = startDate2
            Principal = 500_00uL<Cent>
            ScheduleConfig =
                CustomSchedule
                <| Map [
                    17u<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45uL<Cent>) ValueNone
                    48u<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45uL<Cent>) ValueNone
                    76u<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45uL<Cent>) ValueNone
                    107u<OffsetDay>, ScheduledPayment.quick (ValueSome 209_40uL<Cent>) ValueNone
                ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = NoRounding
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = {
                    TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                    DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
                }
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom
                AprPrecision = 3u
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50uL<Cent>
                Timeout = 3u<OffsetDay>
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 0u<OffsetDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    [<Fact>]
    let ComplianceTest004 () =
        let title = "ComplianceTest004"
        let description = "Repayments made on time"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                107u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_40uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters2

        Schedule.outputHtmlToFile folder title description parameters2 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest005 () =
        let title = "ComplianceTest005"
        let description = "Early repayment"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                63u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                91u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 160_81uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters2

        Schedule.outputHtmlToFile folder title description parameters2 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest006 () =
        let title = "ComplianceTest006"
        let description = "Late repayment"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                122u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 234_52uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters2

        Schedule.outputHtmlToFile folder title description parameters2 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let parameters3 = {
        parameters2 with
            Basic.ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1u, 2021, 12, 31)
                    ScheduleLength = PaymentCount 4u
                }
            Basic.PaymentConfig.Rounding = RoundUp
            Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 1.2m
            Basic.InterestConfig.Cap = Interest.Cap.zero
    }

    [<Fact>]
    let ComplianceTest007 () =
        let title = "ComplianceTest007"
        let description = "Repayments made on time - no interest cap"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                107u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_48uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters3

        Schedule.outputHtmlToFile folder title description parameters3 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest008 () =
        let title = "ComplianceTest008"
        let description = "Early repayment - no interest cap"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                63u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                91u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 175_99uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters3

        Schedule.outputHtmlToFile folder title description parameters3 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest009 () =
        let title = "ComplianceTest009"
        let description = "Late repayment - no interest cap"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 263_51uL<Cent> ]
                122u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 310_90uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters3

        Schedule.outputHtmlToFile folder title description parameters3 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let parameters4 = {
        parameters3 with
            Basic.InterestConfig.Cap.DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    [<Fact>]
    let ComplianceTest010 () =
        let title = "ComplianceTest010"
        let description = "Repayments made on time - 0.8% daily interest cap"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                107u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_42uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters4

        Schedule.outputHtmlToFile folder title description parameters4 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest011 () =
        let title = "ComplianceTest011"
        let description = "Early repayment - 0.8% daily interest cap"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                63u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                91u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 160_82uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters4

        Schedule.outputHtmlToFile folder title description parameters4 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest012 () =
        let title = "ComplianceTest012"
        let description = "Late repayment - 0.8% daily interest cap"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 209_45uL<Cent> ]
                122u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 234_54uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters4

        Schedule.outputHtmlToFile folder title description parameters4 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let parameters5 = {
        parameters3 with
            Basic.InterestConfig.Cap.TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
    }

    [<Fact>]
    let ComplianceTest013 () =
        let title = "ComplianceTest013"

        let description =
            "Repayments made on time - 100% total interest cap - autogenerated schedule"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                107u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters5

        Schedule.outputHtmlToFile folder title description parameters5 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest014 () =
        let title = "ComplianceTest014"

        let description =
            "Early repayment - 100% total interest cap - autogenerated schedule"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                63u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                91u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters5

        Schedule.outputHtmlToFile folder title description parameters5 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal -38_00L<Cent>

    [<Fact>]
    let ComplianceTest015 () =
        let title = "ComplianceTest015"

        let description =
            "Late repayment - 100% total interest cap - autogenerated schedule"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                122u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters5

        Schedule.outputHtmlToFile folder title description parameters5 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let parameters6 = {
        parameters5 with
            Basic.ScheduleConfig =
                CustomSchedule
                <| Map [
                    17u<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00uL<Cent>) ValueNone
                    48u<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00uL<Cent>) ValueNone
                    76u<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00uL<Cent>) ValueNone
                    107u<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00uL<Cent>) ValueNone
                ]
    }

    [<Fact>]
    let ComplianceTest016 () =
        let title = "ComplianceTest016"

        let description =
            "Repayments made on time - 100% total interest cap - custom schedule"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                107u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters6

        Schedule.outputHtmlToFile folder title description parameters6 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest017 () =
        let title = "ComplianceTest017"
        let description = "Early repayment - 100% total interest cap - custom schedule"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                63u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                91u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 212_00uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters6

        Schedule.outputHtmlToFile folder title description parameters6 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest018 () =
        let title = "ComplianceTest018"
        let description = "Late repayment - 100% total interest cap - custom schedule"

        let actualPayments =
            Map [
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                48u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                76u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
                122u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 250_00uL<Cent> ]
            ]

        let schedules = actualPayments |> amortise parameters6

        Schedule.outputHtmlToFile folder title description parameters6 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let parameters7 = {
        parameters1 with
            Basic.EvaluationDate = Date(2025, 4, 1)
            Basic.StartDate = Date(2025, 4, 1)
            Basic.Principal = 317_26uL<Cent>
            Basic.ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1u, 2025, 4, 20)
                    ScheduleLength = PaymentCount 4u
                }
            Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.798m
    }

    [<Fact>]
    let ComplianceTest019 () =
        let title = "ComplianceTest019"

        let description =
            "Total repayable on interest-first loan of €317.26 with repayments starting on day 19 and total loan length 110 days"

        let schedules = amortise parameters7 Map.empty

        Schedule.outputHtmlToFile folder title description parameters7 "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest020 () =
        let title = "ComplianceTest020"

        let description =
            "Total repayable on interest-first loan of £300 with repayments starting on day 19 and total loan length 110 days"

        let p = {
            parameters7 with
                Basic.Principal = 300_00uL<Cent>
        }

        let schedules = amortise p Map.empty

        Schedule.outputHtmlToFile folder title description p "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest021 () =
        let title = "ComplianceTest021"

        let description =
            "Total repayable on interest-first loan of £250 with repayments starting on day 19 and total loan length 110 days"

        let p = {
            parameters7 with
                Basic.Principal = 250_00uL<Cent>
        }

        let schedules = amortise p Map.empty

        Schedule.outputHtmlToFile folder title description p "" schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let basicParameters = {
        EvaluationDate = Date(2025, 4, 22)
        StartDate = Date(2025, 4, 22)
        Principal = 1000_00uL<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1u, 2025, 5, 22)
                ScheduleLength = PaymentCount 4u
            }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.AddOn
            StandardRate = Interest.Rate.Daily(Percent 0.798m)
            Cap = {
                TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
            }
            Rounding = RoundDown
            AprMethod = Apr.CalculationMethod.UnitedKingdom
            AprPrecision = 3u
        }
    }

    [<Fact>]
    let ComplianceTest022 () =
        let title = "ComplianceTest022"

        let description =
            "Add-on-interest loan of $1000 with payments starting after one month and 4 payments in total, for documentation purposes"

        let basicSchedule = calculateBasicSchedule basicParameters

        BasicSchedule.outputHtmlToFile folder title description basicParameters basicSchedule

        let principalBalance = basicSchedule.Items |> Array.last |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest023 () =
        let title = "ComplianceTest023"

        let description =
            "Actuarial-interest loan of $1000 with payments starting after one month and 4 payments in total, for documentation purposes"

        let basicSchedule =
            calculateBasicSchedule {
                basicParameters with
                    InterestConfig.Method = Interest.Method.Actuarial
            }

        BasicSchedule.outputHtmlToFile folder title description basicParameters basicSchedule

        let principalBalance = basicSchedule.Items |> Array.last |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>
