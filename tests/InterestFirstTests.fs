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
            Principal = 1000_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2024, 8, 2)
                    ScheduleLength = PaymentCount 5
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
                10<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 271_37L<Cent> ] //normal
                17<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 271_37L<Cent> ] //all
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
                1<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                2<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                3<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                4<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                5<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_72L<Cent> ]
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
                Basic.Principal = 700_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 1, 28)
                        ScheduleLength = PaymentCount 4
                    }
        }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            Map [
                18<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 294_91L<Cent> ]
                35<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 294_91L<Cent> ]
                168<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 810_18L<Cent> ]
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
                Basic.Principal = 700_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 1, 28)
                        ScheduleLength = PaymentCount 4
                    }
        }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            Map [
                18<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 294_91L<Cent> ]
                35<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 294_91L<Cent> ]
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
                10<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                41<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                71<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                102<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_73L<Cent> ]
                132<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 367_72L<Cent> ]
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
            Map [ 1<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 1007_00L<Cent> ] ]

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
            Map [ 1<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 1007_00L<Cent> ] ]

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
                Basic.Principal = 100_00L<Cent>
        }

        let actualPayments =
            Map [ 10<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 1000_00L<Cent> ] ]

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
                Basic.Principal = 400_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 4, 1)
                        ScheduleLength = PaymentCount 4
                    }
        }

        let actualPayments =
            Map [
                Date(2022, 4, 10) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 198_40L<Cent> ]
                Date(2022, 5, 14) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 198_40L<Cent> ]
                Date(2022, 6, 10) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 198_40L<Cent> ]
                Date(2022, 6, 17) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 198_40L<Cent> ]
                Date(2022, 7, 15) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 204_80L<Cent> ]
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
                Basic.Principal = 200_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2023, 6, 10)
                        ScheduleLength = PaymentCount 4
                    }
        }

        let actualPayments =
            Map [
                Date(2023, 7, 16) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 88_00L<Cent> ]
                Date(2023, 10, 13) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 126_00L<Cent> ]
                Date(2023, 10, 17) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 98_00L<Cent> ]
                Date(2023, 10, 18) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 88_00L<Cent> ]
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
                Basic.Principal = 200_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2023, 6, 10)
                        ScheduleLength = PaymentCount 4
                    }
        }

        let actualPayments =
            Map [
                Date(2023, 7, 16) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 88_00L<Cent> ]
                Date(2023, 10, 13) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 126_00L<Cent> ]
                Date(2023, 10, 17) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 98_00L<Cent> ]
                Date(2023, 10, 18) |> OffsetDay.fromDate p.Basic.StartDate,
                Map [ 0, ActualPayment.quickConfirmed 100_00L<Cent> ]
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
                1<OffsetDay>,
                Map [
                    091612, ActualPayment.quickConfirmed 1007_00L<Cent>
                    093146, ActualPayment.quickConfirmed 2_00L<Cent>
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
            schedules.AmortisationSchedule.ScheduleItems[0<OffsetDay>].InterestBalance

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
            schedules.AmortisationSchedule.ScheduleItems[0<OffsetDay>].InterestBalance

        initialInterestBalance |> should equal 362_35m<Cent>

    [<Fact>]
    let InterestFirstTest018 () =
        let title = "InterestFirstTest018"
        let description = "Realistic test 6045bd0ffc0f with correction on final day"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 9, 17)
                Basic.StartDate = Date(2023, 9, 22)
                Basic.Principal = 740_00L<Cent>
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Zero
                Basic.ScheduleConfig =
                    [|
                        14<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                        37<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                        68<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                        98<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                        42<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        49<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        56<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        63<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        70<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        77<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        84<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 20000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        91<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 8000L<Cent>
                                RescheduleDay = 19<OffsetDay>
                            })
                        56<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        86<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        117<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        148<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 5000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        177<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        208<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        238<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        269<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        299<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        330<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 15000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        361<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 18000L<Cent>
                                RescheduleDay = 47<OffsetDay>
                            })
                        119<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        126<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        133<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        140<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        147<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        154<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        161<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        168<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        175<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        182<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        189<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        196<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        203<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        210<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        217<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        224<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        231<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        238<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        245<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        252<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        259<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        266<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        273<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        280<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        287<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        294<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        301<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        308<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        315<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        322<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        329<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        336<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        343<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        350<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 3500L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                        357<OffsetDay>,
                        ScheduledPayment.quick
                            ValueNone
                            (ValueSome {
                                Value = 4000L<Cent>
                                RescheduleDay = 115<OffsetDay>
                            })
                    |]
                    |> mergeScheduledPayments
                    |> CustomSchedule
        }

        let actualPayments =
            [|
                14<OffsetDay>, [| 0, ActualPayment.quickFailed 33004L<Cent> ValueNone |]
                14<OffsetDay>, [| 1, ActualPayment.quickFailed 33004L<Cent> ValueNone |]
                14<OffsetDay>, [| 2, ActualPayment.quickFailed 33004L<Cent> ValueNone |]
                42<OffsetDay>, [| 0, ActualPayment.quickFailed 20000L<Cent> ValueNone |]
                42<OffsetDay>, [| 1, ActualPayment.quickConfirmed 20000L<Cent> |]
                56<OffsetDay>, [| 0, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                56<OffsetDay>, [| 1, ActualPayment.quickConfirmed 5000L<Cent> |]
                86<OffsetDay>, [| 0, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                86<OffsetDay>, [| 1, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                86<OffsetDay>, [| 2, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                89<OffsetDay>, [| 0, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                89<OffsetDay>, [| 1, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                89<OffsetDay>, [| 2, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                92<OffsetDay>, [| 0, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                92<OffsetDay>, [| 1, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                92<OffsetDay>, [| 2, ActualPayment.quickFailed 5000L<Cent> ValueNone |]
                119<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                119<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                119<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                122<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                122<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                122<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                125<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                125<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                125<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                126<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                126<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                126<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                129<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                129<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                129<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                132<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                132<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                132<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                132<OffsetDay>, [| 3, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                133<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                133<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                133<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                136<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                136<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                136<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                139<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                139<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                139<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                140<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                140<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                140<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                143<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                143<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                143<OffsetDay>, [| 2, ActualPayment.quickConfirmed 3500L<Cent> |]
                146<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                146<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                146<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                147<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                147<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                147<OffsetDay>, [| 2, ActualPayment.quickConfirmed 3500L<Cent> |]
                150<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                150<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                150<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                153<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                153<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                153<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                154<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                154<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                154<OffsetDay>, [| 2, ActualPayment.quickConfirmed 3500L<Cent> |]
                161<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                161<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                161<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                164<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                168<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                168<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                168<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                171<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                171<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                171<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                174<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                174<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                174<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                175<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                175<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                175<OffsetDay>, [| 2, ActualPayment.quickConfirmed 3500L<Cent> |]
                182<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                182<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                182<OffsetDay>, [| 2, ActualPayment.quickConfirmed 3500L<Cent> |]
                189<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                189<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                196<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                196<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                196<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                199<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                199<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                199<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                202<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                202<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                202<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                203<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                203<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                203<OffsetDay>, [| 2, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                206<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                206<OffsetDay>, [| 1, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                206<OffsetDay>, [| 2, ActualPayment.quickConfirmed 3500L<Cent> |]
                210<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                217<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                217<OffsetDay>, [| 1, ActualPayment.quickConfirmed 3500L<Cent> |]
                224<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                231<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                238<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                245<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                252<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                259<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                266<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                273<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                280<OffsetDay>, [| 0, ActualPayment.quickFailed 3500L<Cent> ValueNone |]
                280<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                287<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                294<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                301<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                308<OffsetDay>, [| 0, ActualPayment.quickConfirmed 3500L<Cent> |]
                314<OffsetDay>, [| 0, ActualPayment.quickConfirmed 25000L<Cent> |]
                315<OffsetDay>, [| 0, ActualPayment.quickConfirmed 1L<Cent> |]
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
                Basic.Principal = 740_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2023, 9, 29)
                            PaymentCount = 4
                            PaymentValue = 293_82L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                42<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                56<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 5000L<Cent> ]
                133<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                143<OffsetDay>,
                Map [
                    0, ActualPayment.quickConfirmed 3500L<Cent>
                    1, ActualPayment.quickConfirmed 3500L<Cent>
                ]
                147<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                154<OffsetDay>,
                Map [
                    0, ActualPayment.quickConfirmed 3500L<Cent>
                    1, ActualPayment.quickConfirmed 3500L<Cent>
                ]
                164<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                175<OffsetDay>,
                Map [
                    0, ActualPayment.quickConfirmed 3500L<Cent>
                    1, ActualPayment.quickConfirmed 3500L<Cent>
                ]
                182<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                189<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                203<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                206<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                210<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                217<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                224<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                231<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                238<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                245<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                252<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                259<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                266<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                273<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                280<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                287<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                294<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                301<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                308<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 3500L<Cent> ]
                314<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 25000L<Cent> ]
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
                Basic.Principal = 100_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2023, 2, 3)
                            PaymentCount = 4
                            PaymentValue = 42_40L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [ 20<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 116_00L<Cent> ] ]

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
                Basic.Principal = 100_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2023, 2, 3)
                            PaymentCount = 4
                            PaymentValue = 42_40L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [ 20<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 116_00L<Cent> ] ]

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
                Basic.Principal = 340_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2018, 3, 1)
                            PaymentCount = 11
                            PaymentValue = 55_60L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                        {
                            UnitPeriodConfig = Monthly(1, 2019, 2, 1)
                            PaymentCount = 1
                            PaymentValue = 55_58L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                34<OffsetDay>, Map [ 0, ActualPayment.quickFailed 5560L<Cent> ValueNone ]
                35<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 5571L<Cent> ]
                60<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 5560L<Cent> ]
                90<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 5560L<Cent> ]
                119<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 5560L<Cent> ]
                152<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 5560L<Cent> ]
                214<OffsetDay>,
                Map [
                    0, ActualPayment.quickConfirmed 5857L<Cent>
                    1, ActualPayment.quickConfirmed 5560L<Cent>
                ]
                273<OffsetDay>,
                Map [
                    0, ActualPayment.quickConfirmed 5835L<Cent>
                    1, ActualPayment.quickConfirmed 5560L<Cent>
                ]
                305<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 16678L<Cent> ]
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
                Basic.Principal = 1_500_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2023, 1, 2)
                            PaymentCount = 6
                            PaymentValue = 500_00L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                32<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 50000L<Cent> ]
                63<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 50000L<Cent> ]
                148<OffsetDay>,
                Map [
                    0, ActualPayment.quickFailed 20000L<Cent> ValueNone
                    1, ActualPayment.quickConfirmed 20000L<Cent>
                ]
                181<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                209<OffsetDay>, Map [ 0, ActualPayment.quickFailed 20000L<Cent> ValueNone ]
                212<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                242<OffsetDay>,
                Map [
                    0, ActualPayment.quickFailed 20000L<Cent> ValueNone
                    1, ActualPayment.quickConfirmed 20000L<Cent>
                ]
                273<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                304<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                334<OffsetDay>,
                Map [
                    0, ActualPayment.quickFailed 20000L<Cent> ValueNone
                    1, ActualPayment.quickConfirmed 20000L<Cent>
                ]
                365<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                395<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 20000L<Cent> ]
                426<OffsetDay>,
                Map [
                    0, ActualPayment.quickFailed 20000L<Cent> ValueNone
                    1, ActualPayment.quickConfirmed 20000L<Cent>
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
                Basic.Principal = 350_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2021, 2, 28)
                            PaymentCount = 4
                            PaymentValue = 168_00L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
                Basic.InterestConfig.Method = Interest.Method.Actuarial
        }

        let actualPayments =
            Map [
                26<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 16800L<Cent> ]
                85<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 8400L<Cent> ]
                189<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                220<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                251<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                281<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                317<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                402<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                422<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                430<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 706L<Cent> ]
                462<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                506<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                531<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                562<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                583<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 689L<Cent> ]
                615<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                646<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                689<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                715<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                741<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                750<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                771<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                799<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 921L<Cent> ]
                855<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 921L<Cent> ]
                856<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                895<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                924<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                954<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                988<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                1039<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1073<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1106<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1141<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1169<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1192<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1224<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
                1253<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
                1290<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
                1316<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
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
                Basic.Principal = 350_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2021, 2, 28)
                            PaymentCount = 4
                            PaymentValue = 168_00L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
        }

        let actualPayments =
            Map [
                26<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 16800L<Cent> ]
                85<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 8400L<Cent> ]
                189<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                220<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                251<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                281<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                317<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                402<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                422<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 546L<Cent> ]
                430<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 706L<Cent> ]
                462<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                506<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                531<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                562<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 598L<Cent> ]
                583<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 689L<Cent> ]
                615<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                646<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                689<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                715<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                741<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                750<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                771<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 869L<Cent> ]
                799<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 921L<Cent> ]
                855<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 921L<Cent> ]
                856<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                895<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                924<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                954<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                988<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 862L<Cent> ]
                1039<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1073<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1106<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1141<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1169<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1192<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 883L<Cent> ]
                1224<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
                1253<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
                1290<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
                1316<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 911L<Cent> ]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let totalInterestPortions =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>
