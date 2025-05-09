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
            Principal = 1000_00L<Cent>
            ScheduleConfig =
                CustomSchedule
                <| Map [
                    31<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46L<Cent>) ValueNone
                    61<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46L<Cent>) ValueNone
                    90<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46L<Cent>) ValueNone
                    120<OffsetDay>, ScheduledPayment.quick (ValueSome 451_43L<Cent>) ValueNone
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
            ChargeConfig = None
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
    let ComplianceTest000 () =
        let title = "ComplianceTest000"
        let description = "Repayments made on time"

        let actualPayments =
            Map [
                31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                90<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                120<OffsetDay>, [| ActualPayment.quickConfirmed 451_43L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 schedules

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
                31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                81<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                101<OffsetDay>, [| ActualPayment.quickConfirmed 350_31L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 schedules

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
                31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                90<OffsetDay>, [| ActualPayment.quickConfirmed 794_55L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 schedules

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
                31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                95<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
                130<OffsetDay>, [| ActualPayment.quickConfirmed 505_60L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters1

        Schedule.outputHtmlToFile folder title description parameters1 schedules

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
            Principal = 500_00L<Cent>
            ScheduleConfig =
                CustomSchedule
                <| Map [
                    17<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45L<Cent>) ValueNone
                    48<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45L<Cent>) ValueNone
                    76<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45L<Cent>) ValueNone
                    107<OffsetDay>, ScheduledPayment.quick (ValueSome 209_40L<Cent>) ValueNone
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
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 0<DurationDay>
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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters2

        Schedule.outputHtmlToFile folder title description parameters2 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                63<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                91<OffsetDay>, [| ActualPayment.quickConfirmed 160_81L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters2

        Schedule.outputHtmlToFile folder title description parameters2 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 234_52L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters2

        Schedule.outputHtmlToFile folder title description parameters2 schedules

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
                    UnitPeriodConfig = Monthly(1, 2021, 12, 31)
                    ScheduleLength = PaymentCount 4
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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 263_48L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters3

        Schedule.outputHtmlToFile folder title description parameters3 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                63<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                91<OffsetDay>, [| ActualPayment.quickConfirmed 175_99L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters3

        Schedule.outputHtmlToFile folder title description parameters3 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 310_90L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters3

        Schedule.outputHtmlToFile folder title description parameters3 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 209_42L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters4

        Schedule.outputHtmlToFile folder title description parameters4 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                63<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                91<OffsetDay>, [| ActualPayment.quickConfirmed 160_82L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters4

        Schedule.outputHtmlToFile folder title description parameters4 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 234_54L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters4

        Schedule.outputHtmlToFile folder title description parameters4 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters5

        Schedule.outputHtmlToFile folder title description parameters5 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                63<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                91<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters5

        Schedule.outputHtmlToFile folder title description parameters5 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters5

        Schedule.outputHtmlToFile folder title description parameters5 schedules

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
                    17<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                    48<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                    76<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                    107<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                ]
    }

    [<Fact>]
    let ComplianceTest016 () =
        let title = "ComplianceTest016"

        let description =
            "Repayments made on time - 100% total interest cap - custom schedule"

        let actualPayments =
            Map [
                17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters6

        Schedule.outputHtmlToFile folder title description parameters6 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                63<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                91<OffsetDay>, [| ActualPayment.quickConfirmed 212_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters6

        Schedule.outputHtmlToFile folder title description parameters6 schedules

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
                17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise parameters6

        Schedule.outputHtmlToFile folder title description parameters6 schedules

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
            Basic.Principal = 317_26L<Cent>
            Basic.ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2025, 4, 20)
                    ScheduleLength = PaymentCount 4
                }
            Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.798m
    }

    [<Fact>]
    let ComplianceTest019 () =
        let title = "ComplianceTest019"

        let description =
            "Total repayable on interest-first loan of €317.26 with repayments starting on day 19 and total loan length 110 days"

        let schedules = amortise parameters7 Map.empty

        Schedule.outputHtmlToFile folder title description parameters7 schedules

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
                Basic.Principal = 300_00L<Cent>
        }

        let schedules = amortise p Map.empty

        Schedule.outputHtmlToFile folder title description p schedules

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
                Basic.Principal = 250_00L<Cent>
        }

        let schedules = amortise p Map.empty

        Schedule.outputHtmlToFile folder title description p schedules

        let principalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        principalBalance |> should equal 0L<Cent>

    let basicParameters = {
        EvaluationDate = Date(2025, 4, 22)
        StartDate = Date(2025, 4, 22)
        Principal = 1000_00L<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2025, 5, 22)
                ScheduleLength = PaymentCount 4
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
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
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
