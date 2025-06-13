namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestFirstTests =

    let folder = "InterestFirst"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Refinancing
    open Scheduling
    open UnitPeriod

    let startDate = Date(2024, 7, 23)

    let parameters: Parameters = {
        Basic = {
            EvaluationDate = startDate.AddDays 180
            StartDate = startDate
            Principal = 1000_00uL<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1u, 2024, 8, 2)
                    ScheduleLength = PaymentCount 5u
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
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
                RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }

    }

    [<Fact>]
    let InterestFirstTest000 () =
        let title = "InterestFirstTest000"
        let description = "Actuarial interest method basic schedule"

        let p = {
            parameters with
                Parameters.Basic.InterestConfig.Method = Interest.Method.Actuarial
        }

        let actual =
            let schedule = calculateBasicSchedule p.Basic
            BasicSchedule.outputHtmlToFile folder title description p.Basic schedule
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 319_26L<Cent>, 319_23L<Cent>

        actual |> should equal expected

    [<Fact>]
    let InterestFirstTest001 () =
        let title = "InterestFirstTest001"
        let description = "Actuarial interest method"

        let p = {
            parameters with
                Parameters.Basic.InterestConfig.Method = Interest.Method.Actuarial
        }

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalInterestBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.InterestBalance

        finalInterestBalance |> should equal 1000_00m<Cent>

    [<Fact>]
    let InterestFirstTest002 () =
        let title = "InterestFirstTest002"
        let description = "Add-on interest method basic schedule"
        let p = parameters

        let actual =
            let schedule = calculateBasicSchedule p.Basic
            BasicSchedule.outputHtmlToFile folder title description p.Basic schedule
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 367_73L<Cent>, 367_71L<Cent>
        actual |> should equal expected

    [<Fact>]
    let InterestFirstTest003 () =
        let title = "InterestFirstTest003"
        let description = "Add-on interest method"
        let p = parameters

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.SettlementFigure

        finalSettlementFigure |> should equal 2000_00L<Cent>

    [<Fact>]
    let InterestFirstTest004 () =
        let title = "InterestFirstTest004"
        let description = "Add-on interest method with early repayment"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 8, 9)
        }

        let actualPayments =
            Map [
                10u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 271_37uL<Cent> ] //normal
                17u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 271_37uL<Cent> ] //all
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterest =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 838_63L<Cent>

    [<Fact>]
    let InterestFirstTest005 () =
        let title = "InterestFirstTest005"
        let description = "Add-on interest method with normal but very early repayments"
        let p = parameters

        let actualPayments =
            Map [
                1u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                2u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                3u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                4u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                5u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_72uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterest =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 23_88L<Cent>

    [<Fact>]
    let InterestFirstTest006 () =
        let title = "InterestFirstTest006"

        let description =
            "Add-on interest method with normal but with erratic payment timings"

        let startDate = Date(2022, 1, 10)

        let p = {
            parameters with
                Basic.StartDate = startDate
                Basic.Principal = 700_00uL<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2022, 1, 28)
                        ScheduleLength = PaymentCount 4u
                    }
        }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            Map [
                18u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 294_91uL<Cent> ]
                35u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 294_91uL<Cent> ]
                168u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 810_18uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalPrincipalBalance =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.PrincipalBalance

        finalPrincipalBalance |> should equal 0L<Cent>

    [<Fact>]
    let InterestFirstTest007 () =
        let title = "InterestFirstTest007"

        let description =
            "Add-on interest method with normal but with erratic payment timings expecting settlement figure on final day"

        let startDate = Date(2022, 1, 10)

        let p = {
            parameters with
                Basic.StartDate = startDate
                Basic.Principal = 700_00uL<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2022, 1, 28)
                        ScheduleLength = PaymentCount 4u
                    }
        }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            Map [
                18u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 294_91uL<Cent> ]
                35u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 294_91uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.SettlementFigure

        finalSettlementFigure |> should equal 810_18L<Cent>

    [<Fact>]
    let InterestFirstTest008 () =
        let title = "InterestFirstTest008"
        let description = "Add-on interest method with normal repayments"
        let p = parameters

        let actualPayments =
            Map [
                10u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                41u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                71u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                102u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_73uL<Cent> ]
                132u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 367_72uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterest =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 838_63L<Cent>

    [<Fact>]
    let InterestFirstTest009 () =
        let title = "InterestFirstTest009"
        let description = "Add-on interest method with single early repayment"

        let p = {
            parameters with
                Basic.EvaluationDate = startDate.AddDays 2
        }

        let actualPayments =
            Map [ 1u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 1007_00uL<Cent> ] ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.SettlementFigure

        finalSettlementFigure |> should equal 0L<Cent>

    [<Fact>]
    let InterestFirstTest010 () =
        let title = "InterestFirstTest010"

        let description =
            "Add-on interest method with single early repayment then a quote one day later"

        let p = {
            parameters with
                Basic.EvaluationDate = startDate.AddDays 2
        }

        let actualPayments =
            Map [ 1u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 1007_00uL<Cent> ] ]

        let schedules =
            actualPayments
            |> amortise {
                p with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterest =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 14_65L<Cent>

    [<Fact>]
    let InterestFirstTest011 () =
        let title = "InterestFirstTest011"

        let description =
            "Add-on interest method with small loan and massive payment leading to a refund needed"

        let p = {
            parameters with
                Basic.EvaluationDate = startDate.AddDays 180
                Basic.Principal = 100_00uL<Cent>
        }

        let actualPayments =
            Map [ 10u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 1000_00uL<Cent> ] ]

        let schedules =
            actualPayments
            |> amortise {
                p with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterest =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal -25_24L<Cent>

    [<Fact>]
    let InterestFirstTest012 () =
        let title = "InterestFirstTest012"
        let description = "Realistic example 501ac58e62a5"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 8, 9)
                Basic.StartDate = Date(2022, 2, 28)
                Basic.Principal = 400_00uL<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2022, 4, 1)
                        ScheduleLength = PaymentCount 4u
                    }
        }

        let actualPayments =
            Map [
                Date(2022, 4, 10) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 198_40uL<Cent> ]
                Date(2022, 5, 14) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 198_40uL<Cent> ]
                Date(2022, 6, 10) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 198_40uL<Cent> ]
                Date(2022, 6, 17) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 198_40uL<Cent> ]
                Date(2022, 7, 15) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 204_80uL<Cent> ]
            ]

        let schedules =
            actualPayments
            |> amortise {
                p with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.SettlementFigure

        finalSettlementFigure |> should equal 0L<Cent>

    [<Fact>]
    let InterestFirstTest013 () =
        let title = "InterestFirstTest013"
        let description = "Realistic example 0004ffd74fbb"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 8, 9)
                Basic.StartDate = Date(2023, 6, 7)
                Basic.Principal = 200_00uL<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2023, 6, 10)
                        ScheduleLength = PaymentCount 4u
                    }
        }

        let actualPayments =
            Map [
                Date(2023, 7, 16) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 88_00uL<Cent> ]
                Date(2023, 10, 13) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 126_00uL<Cent> ]
                Date(2023, 10, 17) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 98_00uL<Cent> ]
                Date(2023, 10, 18) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 88_00uL<Cent> ]
            ]

        let schedules =
            actualPayments
            |> amortise {
                p with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.SettlementFigure

        finalSettlementFigure |> should equal 0L<Cent>

    [<Fact>]
    let ``InterestFirstTest014`` () =
        let title = "InterestFirstTests014"
        let description = "Realistic example 0004ffd74fbb with overpayment"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 8, 9)
                Basic.StartDate = Date(2023, 6, 7)
                Basic.Principal = 200_00uL<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1u, 2023, 6, 10)
                        ScheduleLength = PaymentCount 4u
                    }
        }

        let actualPayments =
            Map [
                Date(2023, 7, 16) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 88_00uL<Cent> ]
                Date(2023, 10, 13) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 126_00uL<Cent> ]
                Date(2023, 10, 17) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 98_00uL<Cent> ]
                Date(2023, 10, 18) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0u, ActualPayment.quickConfirmed 100_00uL<Cent> ]
            ]

        let schedules =
            actualPayments
            |> amortise {
                p with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> (fun asi -> asi.InterestPortion, asi.PrincipalPortion, asi.SettlementFigure)

        finalSettlementFigure |> should equal (-78L<Cent>, -12_00L<Cent>, 0L<Cent>)

    [<Fact>]
    let InterestFirstTest015 () =
        let title = "InterestFirstTest015"

        let description =
            "Add-on interest method with big early repayment followed by tiny overpayment"

        let p = {
            parameters with
                Basic.EvaluationDate = startDate.AddDays 1000
        }

        let actualPayments =
            Map [
                1u<OffsetDay>,
                Map [
                    091612u, ActualPayment.quickConfirmed 1007_00uL<Cent>
                    093146u, ActualPayment.quickConfirmed 2_00uL<Cent>
                ]
            ]

        let schedules =
            actualPayments
            |> amortise {
                p with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

        Schedule.outputHtmlToFile folder title description p "" schedules

        let finalSettlementFigure =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd
            |> _.SettlementFigure

        finalSettlementFigure |> should equal 0L<Cent>

    [<Fact>]
    let InterestFirstTest016 () =
        let title = "InterestFirstTest016"

        let description =
            "Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (no cap)"

        let p = {
            parameters with
                Basic.EvaluationDate = startDate
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.4m
                Basic.InterestConfig.Cap = Interest.Cap.zero
        }

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let initialInterestBalance =
            schedules.AmortisationSchedule.ScheduleItems[0u<OffsetDay>].InterestBalance

        initialInterestBalance |> should equal 362_35m<Cent>

    [<Fact>]
    let InterestFirstTest017 () =
        let title = "InterestFirstTest017"

        let description =
            "Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (cap in place)"

        let p = {
            parameters with
                Basic.EvaluationDate = startDate
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.4m
        }

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let initialInterestBalance =
            schedules.AmortisationSchedule.ScheduleItems[0u<OffsetDay>].InterestBalance

        initialInterestBalance |> should equal 362_35m<Cent>

    [<Fact>]
    let InterestFirstTest018 () =
        let title = "InterestFirstTest018"
        let description = "Realistic test 6045bd0ffc0f with correction on final day"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2023, 9, 22)
                Basic.Principal = 740_00uL<Cent>
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Zero
                Basic.ScheduleConfig =
                    [|
                        14u<OffsetDay>, ScheduledPayment.quick (ValueSome 33004uL<Cent>) ValueNone
                        37u<OffsetDay>, ScheduledPayment.quick (ValueSome 33004uL<Cent>) ValueNone
                        68u<OffsetDay>, ScheduledPayment.quick (ValueSome 33004uL<Cent>) ValueNone
                        98u<OffsetDay>, ScheduledPayment.quick (ValueSome 33004uL<Cent>) ValueNone
                        42u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        49u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        56u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        63u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        70u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        77u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        84u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        91u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 8000uL<Cent>
                                RescheduleDay = 19u<OffsetDay>
                            })
                        56u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        86u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        117u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        148u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        177u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        208u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        238u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        269u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        299u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        330u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        361u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 18000uL<Cent>
                                RescheduleDay = 47u<OffsetDay>
                            })
                        119u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        126u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        133u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        140u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        147u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        154u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        161u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        168u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        175u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        182u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        189u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        196u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        203u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        210u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        217u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        224u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        231u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        238u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        245u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        252u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        259u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        266u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        273u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        280u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        287u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        294u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        301u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        308u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        315u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        322u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        329u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        336u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        343u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        350u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                        357u<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 4000uL<Cent>
                                RescheduleDay = 115u<OffsetDay>
                            })
                    |]
                    |> mergeScheduledPayments
                    |> CustomSchedule
        }

        let actualPayments =
            [|
                14u<OffsetDay>, [| 0u, ActualPayment.quickFailed 33004uL<Cent> ValueNone |]
                14u<OffsetDay>, [| 1u, ActualPayment.quickFailed 33004uL<Cent> ValueNone |]
                14u<OffsetDay>, [| 2u, ActualPayment.quickFailed 33004uL<Cent> ValueNone |]
                42u<OffsetDay>, [| 0u, ActualPayment.quickFailed 20000uL<Cent> ValueNone |]
                42u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 20000uL<Cent> |]
                56u<OffsetDay>, [| 0u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                56u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 5000uL<Cent> |]
                86u<OffsetDay>, [| 0u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                86u<OffsetDay>, [| 1u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                86u<OffsetDay>, [| 2u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                89u<OffsetDay>, [| 0u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                89u<OffsetDay>, [| 1u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                89u<OffsetDay>, [| 2u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                92u<OffsetDay>, [| 0u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                92u<OffsetDay>, [| 1u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                92u<OffsetDay>, [| 2u, ActualPayment.quickFailed 5000uL<Cent> ValueNone |]
                119u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                119u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                119u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                122u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                122u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                122u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                125u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                125u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                125u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                126u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                126u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                126u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                129u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                129u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                129u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                132u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                132u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                132u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                132u<OffsetDay>, [| 3u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                133u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                133u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                133u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                136u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                136u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                136u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                139u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                139u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                139u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                140u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                140u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                140u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                143u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                143u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                143u<OffsetDay>, [| 2u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                146u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                146u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                146u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                147u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                147u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                147u<OffsetDay>, [| 2u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                150u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                150u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                150u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                153u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                153u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                153u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                154u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                154u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                154u<OffsetDay>, [| 2u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                161u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                161u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                161u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                164u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                168u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                168u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                168u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                171u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                171u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                171u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                174u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                174u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                174u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                175u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                175u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                175u<OffsetDay>, [| 2u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                182u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                182u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                182u<OffsetDay>, [| 2u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                189u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                189u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                196u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                196u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                196u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                199u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                199u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                199u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                202u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                202u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                202u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                203u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                203u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                203u<OffsetDay>, [| 2u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                206u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                206u<OffsetDay>, [| 1u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                206u<OffsetDay>, [| 2u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                210u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                217u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                217u<OffsetDay>, [| 1u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                224u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                231u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                238u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                245u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                252u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                259u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                266u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                273u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                280u<OffsetDay>, [| 0u, ActualPayment.quickFailed 3500uL<Cent> ValueNone |]
                280u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                287u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                294u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                301u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                308u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 3500uL<Cent> |]
                314u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 25000uL<Cent> |]
                315u<OffsetDay>, [| 0u, ActualPayment.quickConfirmed 1uL<Cent> |]
            |]
            |> Map.merge

        let schedules = amortise p actualPayments

        schedules |> Schedule.outputHtmlToFile folder title description p ""

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 740_00L<Cent>

    [<Fact>]
    let InterestFirstTest019 () =
        let title = "InterestFirstTest019"
        let description = "Realistic test 6045bd0ffc0f"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2023, 9, 22)
                Basic.Principal = 740_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2023, 9, 29)
                            PaymentCount = 4u
                            PaymentValue = 293_82uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                42u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                56u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 5000uL<Cent> ]
                133u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                143u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickConfirmed 3500uL<Cent>
                    1u, ActualPayment.quickConfirmed 3500uL<Cent>
                ]
                147u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                154u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickConfirmed 3500uL<Cent>
                    1u, ActualPayment.quickConfirmed 3500uL<Cent>
                ]
                164u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                175u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickConfirmed 3500uL<Cent>
                    1u, ActualPayment.quickConfirmed 3500uL<Cent>
                ]
                182u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                189u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                203u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                206u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                210u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                217u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                224u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                231u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                238u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                245u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                252u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                259u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                266u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                273u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                280u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                287u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                294u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                301u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                308u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 3500uL<Cent> ]
                314u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 25000uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 740_00L<Cent>

    [<Fact>]
    let InterestFirstTest020 () =
        let title = "InterestFirstTest020"
        let description = "Realistic test 6045bd123363 with correction on final day"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2023, 1, 14)
                Basic.Principal = 100_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2023, 2, 3)
                            PaymentCount = 4u
                            PaymentValue = 42_40uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [ 20u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 116_00uL<Cent> ] ]

        let schedules = amortise p actualPayments

        schedules |> Schedule.outputHtmlToFile folder title description p ""

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 16_00L<Cent>

    [<Fact>]
    let InterestFirstTest021 () =
        let title = "InterestFirstTest021"
        let description = "Realistic test 6045bd123363"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2023, 1, 14)
                Basic.Principal = 100_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2023, 2, 3)
                            PaymentCount = 4u
                            PaymentValue = 42_40uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [ 20u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 116_00uL<Cent> ] ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 16_00L<Cent>

    [<Fact>]
    let InterestFirstTest022 () =
        let title = "InterestFirstTest022"
        let description = "Realistic test 0004ffd74fbbn"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2018, 1, 26)
                Basic.Principal = 340_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2018, 3, 1)
                            PaymentCount = 11u
                            PaymentValue = 55_60uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                        {
                            UnitPeriodConfig = Monthly(1u, 2019, 2, 1)
                            PaymentCount = 1u
                            PaymentValue = 55_58uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                34u<OffsetDay>, Map [ 0u, ActualPayment.quickFailed 5560uL<Cent> ValueNone ]
                35u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 5571uL<Cent> ]
                60u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 5560uL<Cent> ]
                90u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 5560uL<Cent> ]
                119u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 5560uL<Cent> ]
                152u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 5560uL<Cent> ]
                214u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickConfirmed 5857uL<Cent>
                    1u, ActualPayment.quickConfirmed 5560uL<Cent>
                ]
                273u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickConfirmed 5835uL<Cent>
                    1u, ActualPayment.quickConfirmed 5560uL<Cent>
                ]
                305u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 16678uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 340_00L<Cent>

    [<Fact>]
    let InterestFirstTest023 () =
        let title = "InterestFirstTest023"
        let description = "Realistic test 0003ff008ae5"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2022, 12, 1)
                Basic.Principal = 1_500_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2023, 1, 2)
                            PaymentCount = 6u
                            PaymentValue = 500_00uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                32u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 50000uL<Cent> ]
                63u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 50000uL<Cent> ]
                148u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickFailed 20000uL<Cent> ValueNone
                    1u, ActualPayment.quickConfirmed 20000uL<Cent>
                ]
                181u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                209u<OffsetDay>, Map [ 0u, ActualPayment.quickFailed 20000uL<Cent> ValueNone ]
                212u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                242u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickFailed 20000uL<Cent> ValueNone
                    1u, ActualPayment.quickConfirmed 20000uL<Cent>
                ]
                273u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                304u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                334u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickFailed 20000uL<Cent> ValueNone
                    1u, ActualPayment.quickConfirmed 20000uL<Cent>
                ]
                365u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                395u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 20000uL<Cent> ]
                426u<OffsetDay>,
                Map [
                    0u, ActualPayment.quickFailed 20000uL<Cent> ValueNone
                    1u, ActualPayment.quickConfirmed 20000uL<Cent>
                ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 1_500_00L<Cent>

    [<Fact>]
    let InterestFirstTest024 () =
        let title = "InterestFirstTest024"
        let description = "Realistic test 0003ff00bffb with actuarial method"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2021, 2, 2)
                Basic.Principal = 350_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2021, 2, 28)
                            PaymentCount = 4u
                            PaymentValue = 168_00uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
                Basic.InterestConfig.Method = Interest.Method.Actuarial
        }

        let actualPayments =
            Map [
                26u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 16800uL<Cent> ]
                85u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 8400uL<Cent> ]
                189u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                220u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                251u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                281u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                317u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                402u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                422u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                430u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 706uL<Cent> ]
                462u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                506u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                531u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                562u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                583u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 689uL<Cent> ]
                615u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                646u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                689u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                715u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                741u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                750u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                771u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                799u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 921uL<Cent> ]
                855u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 921uL<Cent> ]
                856u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                895u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                924u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                954u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                988u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                1039u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1073u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1106u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1141u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1169u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1192u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1224u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
                1253u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
                1290u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
                1316u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>

    [<Fact>]
    let InterestFirstTest025 () =
        let title = "InterestFirstTest025"
        let description = "Realistic test 0003ff00bffb with add-on method"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2021, 2, 2)
                Basic.Principal = 350_00uL<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1u, 2021, 2, 28)
                            PaymentCount = 4u
                            PaymentValue = 168_00uL<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                26u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 16800uL<Cent> ]
                85u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 8400uL<Cent> ]
                189u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                220u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                251u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                281u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                317u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                402u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                422u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 546uL<Cent> ]
                430u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 706uL<Cent> ]
                462u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                506u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                531u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                562u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 598uL<Cent> ]
                583u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 689uL<Cent> ]
                615u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                646u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                689u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                715u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                741u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                750u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                771u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 869uL<Cent> ]
                799u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 921uL<Cent> ]
                855u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 921uL<Cent> ]
                856u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                895u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                924u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                954u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                988u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 862uL<Cent> ]
                1039u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1073u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1106u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1141u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1169u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1192u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 883uL<Cent> ]
                1224u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
                1253u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
                1290u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
                1316u<OffsetDay>, Map [ 0u, ActualPayment.quickConfirmed 911uL<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>
