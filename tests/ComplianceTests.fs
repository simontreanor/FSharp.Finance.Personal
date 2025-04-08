namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ComplianceTests =

    open Amortisation
    open Calculation
    open DateDay
    open Scheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding))
    }

    let startDate1 = Date(2023, 11, 6)
    let scheduleParameters1 =
        {
            AsOfDate = startDate1.AddDays 180
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = NoRounding
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters1 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters1 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters1 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters1 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let startDate2 = Date(2021, 12, 14)
    let scheduleParameters2 =
        {
            AsOfDate = startDate2.AddDays 180
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = NoRounding
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = {
                    TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown)
                    DailyAmount = ValueSome <| Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding)
                }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters2 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters2 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters2 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters2 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters2 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters2 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters3 =
        { scheduleParameters2 with
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2021, 12, 31)
                PaymentCount = 4
                MaxDuration = Duration.Unlimited
            }
            Parameters.PaymentConfig.PaymentRounding = RoundUp
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters3 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters3 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters3 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters3 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters3 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters3 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters4 =
        { scheduleParameters3 with
            InterestConfig.Cap.DailyAmount = ValueSome <| Amount.Percentage(Percent 0.8m, Restriction.NoLimit, RoundDown)
        }

    [<Fact>]
    let ComplianceTest010 () =
        let title = "ComplianceTest010"
        let description = "Repayments made on time - 0.8% daily interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 209_37L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters4 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters4 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest011 () =
        let title = "ComplianceTest011"
        let description = "Early repayment - 0.8% daily interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 160_81L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters4 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters4 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ComplianceTest012 () =
        let title = "ComplianceTest012"
        let description = "Late repayment - 0.8% daily interest cap"
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 234_42L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters4 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters4 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters5=
        { scheduleParameters3 with
            InterestConfig.Cap.TotalAmount = ValueSome <| Amount.Percentage(Percent 100m, Restriction.NoLimit, RoundDown)
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters5 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters5 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters5 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters5 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters5 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters5 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters6 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters6 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters6 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters6 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters6 ValueNone false

        Schedule.outputHtmlToFile title description scheduleParameters6 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    let scheduleParameters7 =
        { scheduleParameters1 with
            AsOfDate = Date(2025, 4, 1)
            StartDate = Date(2025, 4, 1)
            Principal = 317_26L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2025, 4, 20)
                PaymentCount = 4
                MaxDuration = Duration.Unlimited
            }
            InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.798m
        }

    [<Fact>]
    let ComplianceTest019 () =
        let title = "ComplianceTest019"
        let description = "Total repayable on interest-first loan with repayments starting on day 19 and total loan length 110 days"

        let schedule = Amortisation.generate scheduleParameters7 ValueNone false Map.empty

        Schedule.outputHtmlToFile title description scheduleParameters7 schedule

        let principalBalance = schedule |> fst |> _.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>
