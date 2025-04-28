namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ComplianceTests =

    let folder = "Compliance"

    open Amortisation
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    let interestCapExample : Interest.Cap = {
        TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
    }

    let startDate1 = Date(2023, 11, 6)
    let scheduleParameters1 =
        {
            EvaluationDate = startDate1.AddDays 180
            StartDate = startDate1
            Principal = 1000_00L<Cent>
            ScheduleConfig =
                CustomSchedule <| Map [
                    31<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46L<Cent>) ValueNone
                    61<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46L<Cent>) ValueNone
                    90<OffsetDay>, ScheduledPayment.quick (ValueSome 451_46L<Cent>) ValueNone
                    120<OffsetDay>, ScheduledPayment.quick (ValueSome 451_43L<Cent>) ValueNone
                ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                Rounding = RoundUp
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

    [<Fact>]
    let ComplianceTest000 () =
        let title = "ComplianceTest000"
        let description = "Repayments made on time"
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            90<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            120<OffsetDay>, [| ActualPayment.quickConfirmed 451_43L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters1 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters1 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest001 () =
        let title = "ComplianceTest001"
        let description = "Repayments made early"
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            81<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            101<OffsetDay>, [| ActualPayment.quickConfirmed 350_31L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters1 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters1 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest002 () =
        let title = "ComplianceTest002"
        let description = "Full repayment made on repayment 3"
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            90<OffsetDay>, [| ActualPayment.quickConfirmed 794_55L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters1 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters1 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest003 () =
        let title = "ComplianceTest003"
        let description = "Repayments made late - 3 and 4"
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            95<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            130<OffsetDay>, [| ActualPayment.quickConfirmed 505_60L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters1 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters1 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let startDate2 = Date(2021, 12, 14)
    let scheduleParameters2 =
        {
            EvaluationDate = startDate2.AddDays 180
            StartDate = startDate2
            Principal = 500_00L<Cent>
            ScheduleConfig =
                CustomSchedule <| Map [
                    17<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45L<Cent>) ValueNone
                    48<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45L<Cent>) ValueNone
                    76<OffsetDay>, ScheduledPayment.quick (ValueSome 209_45L<Cent>) ValueNone
                    107<OffsetDay>, ScheduledPayment.quick (ValueSome 209_40L<Cent>) ValueNone
                ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                Rounding = NoRounding
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = {
                    TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
                    DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
                }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

    [<Fact>]
    let ComplianceTest004 () =
        let title = "ComplianceTest004"
        let description = "Repayments made on time"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters2 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters2 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest005 () =
        let title = "ComplianceTest005"
        let description = "Early repayment"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 160_81L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters2 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters2 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest006 () =
        let title = "ComplianceTest006"
        let description = "Late repayment"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 234_52L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters2 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters2 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters3 =
        { scheduleParameters2 with
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2021, 12, 31)
                ScheduleLength = PaymentCount 4
            }
            Parameters.PaymentConfig.Rounding = RoundUp
            InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 1.2m
            InterestConfig.Cap = Interest.Cap.Zero
        }

    [<Fact>]
    let ComplianceTest007 () =
        let title = "ComplianceTest007"
        let description = "Repayments made on time - no interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 263_48L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters3 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters3 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest008 () =
        let title = "ComplianceTest008"
        let description = "Early repayment - no interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 175_99L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters3 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters3 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest009 () =
        let title = "ComplianceTest009"
        let description = "Late repayment - no interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 263_51L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 310_90L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters3 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters3 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters4 =
        { scheduleParameters3 with
            InterestConfig.Cap.DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
        }

    [<Fact>]
    let ComplianceTest010 () =
        let title = "ComplianceTest010"
        let description = "Repayments made on time - 0.8% daily interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 209_42L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters4 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters4 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest011 () =
        let title = "ComplianceTest011"
        let description = "Early repayment - 0.8% daily interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 160_82L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters4 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters4 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest012 () =
        let title = "ComplianceTest012"
        let description = "Late repayment - 0.8% daily interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 234_54L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters4 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters4 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters5 =
        { scheduleParameters3 with
            InterestConfig.Cap.TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        }

    [<Fact>]
    let ComplianceTest013 () =
        let title = "ComplianceTest013"
        let description = "Repayments made on time - 100% total interest cap - autogenerated schedule"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters5 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters5 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest014 () =
        let title = "ComplianceTest014"
        let description = "Early repayment - 100% total interest cap - autogenerated schedule"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters5 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters5 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal -38_00L<Cent>

    [<Fact>]
    let ComplianceTest015 () =
        let title = "ComplianceTest015"
        let description = "Late repayment - 100% total interest cap - autogenerated schedule"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters5 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters5 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters6 =
        { scheduleParameters5 with
            ScheduleConfig =
                CustomSchedule <| Map [
                    17<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                    48<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                    76<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                    107<OffsetDay>, ScheduledPayment.quick (ValueSome 250_00L<Cent>) ValueNone
                ]
        }

    [<Fact>]
    let ComplianceTest016 () =
        let title = "ComplianceTest016"
        let description = "Repayments made on time - 100% total interest cap - custom schedule"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters6 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters6 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest017 () =
        let title = "ComplianceTest017"
        let description = "Early repayment - 100% total interest cap - custom schedule"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 212_00L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters6 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters6 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest018 () =
        let title = "ComplianceTest018"
        let description = "Late repayment - 100% total interest cap - custom schedule"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 250_00L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate scheduleParameters6 SettlementDay.NoSettlement false

        Schedule.outputHtmlToFile folder title description scheduleParameters6 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters7 =
        { scheduleParameters1 with
            EvaluationDate = Date(2025, 4, 1)
            StartDate = Date(2025, 4, 1)
            Principal = 317_26L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2025, 4, 20)
                ScheduleLength = PaymentCount 4
            }
            InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.798m
        }

    [<Fact>]
    let ComplianceTest019 () =
        let title = "ComplianceTest019"
        let description = "Total repayable on interest-first loan of €317.26 with repayments starting on day 19 and total loan length 110 days"

        let schedules = Amortisation.generate scheduleParameters7 SettlementDay.NoSettlement false Map.empty

        Schedule.outputHtmlToFile folder title description scheduleParameters7 schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest020 () =
        let title = "ComplianceTest020"
        let description = "Total repayable on interest-first loan of £300 with repayments starting on day 19 and total loan length 110 days"
        let sp = { scheduleParameters7 with Principal = 300_00L<Cent> }
        let schedules = Amortisation.generate sp SettlementDay.NoSettlement false Map.empty

        Schedule.outputHtmlToFile folder title description sp schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest021 () =
        let title = "ComplianceTest021"
        let description = "Total repayable on interest-first loan of £250 with repayments starting on day 19 and total loan length 110 days"
        let sp = { scheduleParameters7 with Principal = 250_00L<Cent> }
        let schedules = Amortisation.generate sp SettlementDay.NoSettlement false Map.empty

        Schedule.outputHtmlToFile folder title description sp schedules

        let principalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters8 = {
        EvaluationDate = Date(2025, 4, 22)
        StartDate = Date(2025, 4, 22)
        Principal = 1000_00L<Cent>
        ScheduleConfig = AutoGenerateSchedule {
            UnitPeriodConfig = Monthly(1, 2025, 5, 22)
            ScheduleLength = PaymentCount 4
        }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            ScheduledPaymentOption = AsScheduled
            Rounding = RoundUp
            Minimum = DeferOrWriteOff 50L<Cent>
            Timeout = 3<DurationDay>
        }
        FeeConfig = None
        ChargeConfig = None
        InterestConfig = {
            Method = Interest.Method.AddOn
            StandardRate = Interest.Rate.Daily (Percent 0.798m)
            Cap = {
                TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
                DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
            }
            InitialGracePeriod = 3<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
            Rounding = RoundDown
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }
    }

    [<Fact>]
    let ComplianceTest022 () =
        let title = "ComplianceTest022"
        let description = "Add-on-interest loan of $1000 with payments starting after one month and 4 payments in total, for documentation purposes"
        let schedules = Scheduling.calculate scheduleParameters8

        BasicSchedule.outputHtmlToFile folder title description scheduleParameters8 schedules

        let principalBalance = schedules.Items |> Array.last |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest023 () =
        let title = "ComplianceTest023"
        let description = "Simple-interest loan of $1000 with payments starting after one month and 4 payments in total, for documentation purposes"
        let schedules = Scheduling.calculate { scheduleParameters8 with InterestConfig.Method = Interest.Method.Simple }

        BasicSchedule.outputHtmlToFile folder title description scheduleParameters8 schedules

        let principalBalance = schedules.Items |> Array.last |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>
