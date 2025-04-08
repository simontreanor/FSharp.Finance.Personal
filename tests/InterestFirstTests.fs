namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestFirstTests =

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    let startDate = Date(2024, 7, 23)
    let scheduleParameters =
        {
            AsOfDate = startDate.AddDays 180
            StartDate = startDate
            Principal = 1000_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 8, 2); PaymentCount = 5; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) }
            PaymentConfig = {
                Tolerance = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = { TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, Restriction.NoLimit); DailyAmount = ValueSome <| Amount.Percentage (Percent 0.8m, Restriction.NoLimit) }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

    [<Fact>]
    let InterestFirstTest000 () =
        let title = "InterestFirstTest000"
        let description = "Simple interest method initial schedule"
        let sp = { scheduleParameters with Parameters.InterestConfig.Method = Interest.Method.Simple }

        let actual =
            let schedule = calculate sp
            SimpleSchedule.outputHtmlToFile title description sp schedule
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 319_26L<Cent>, 319_23L<Cent>

        actual |> should equal expected

    [<Fact>]
    let InterestFirstTest001 () =
        let title = "InterestFirstTest001"
        let description = "Simple interest method"
        let sp = { scheduleParameters with Parameters.InterestConfig.Method = Interest.Method.Simple }

        let actualPayments = Map.empty

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let finalInterestBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance

        finalInterestBalance |> should equal 1000_00m<Cent>

    [<Fact>]
    let InterestFirstTest002 () =
        let title = "InterestFirstTest002"
        let description = "Add-on interest method initial schedule"
        let sp = scheduleParameters

        let actual =
            let schedule = calculate sp
            SimpleSchedule.outputHtmlToFile title description sp schedule
            schedule.Stats.LevelPayment, schedule.Stats.FinalPayment

        let expected = 367_73L<Cent>, 367_72L<Cent>
        actual |> should equal expected

    [<Fact>]
    let InterestFirstTest003 () =
        let title = "InterestFirstTest003"
        let description = "Add-on interest method"
        let sp = scheduleParameters

        let actualPayments = Map.empty

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal (ValueSome 2000_00L<Cent>)

    [<Fact>]
    let InterestFirstTest004 () =
        let title = "InterestFirstTest004"
        let description = "Add-on interest method with early repayment"
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9) }

        let actualPayments =
            Map [
                10<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |] //normal
                17<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |] //all
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterest = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 838_64L<Cent>

    [<Fact>]
    let InterestFirstTest005 () =
        let title = "InterestFirstTest005"
        let description = "Add-on interest method with normal but very early repayments"
        let sp = scheduleParameters

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                2<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                3<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                4<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                5<OffsetDay>, [| ActualPayment.quickConfirmed 367_72L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterest = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 23_88L<Cent>

    [<Fact>]
    let InterestFirstTest006 () =
        let title = "InterestFirstTest006"
        let description = "Add-on interest method with normal but with erratic payment timings"
        let startDate = Date(2022, 1, 10)
        let sp =
            { scheduleParameters with
                StartDate = startDate
                Principal = 700_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 1, 28); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) }
            }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            Map [
                18<OffsetDay>, [| ActualPayment.quickConfirmed 294_91L<Cent> |]
                35<OffsetDay>, [| ActualPayment.quickConfirmed 294_91L<Cent> |]
                168<OffsetDay>, [| ActualPayment.quickConfirmed 810_18L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let finalPrincipalBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance

        finalPrincipalBalance |> should equal 0L<Cent>

    [<Fact>]
    let InterestFirstTest007 () =
        let title = "InterestFirstTest007"
        let description = "Add-on interest method with normal but with erratic payment timings expecting settlement figure on final day"
        let startDate = Date(2022, 1, 10)
        let sp =
            { scheduleParameters with
                StartDate = startDate
                Principal = 700_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 1, 28); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) }
            }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            Map [
                18<OffsetDay>, [| ActualPayment.quickConfirmed 294_91L<Cent> |]
                35<OffsetDay>, [| ActualPayment.quickConfirmed 294_91L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal (ValueSome 650_63L<Cent>)

    [<Fact>]
    let InterestFirstTest008 () =
        let title = "InterestFirstTest008"
        let description = "Add-on interest method with normal repayments"
        let sp = scheduleParameters

        let actualPayments =
            Map [
                10<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                41<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                71<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                102<OffsetDay>, [| ActualPayment.quickConfirmed 367_73L<Cent> |]
                132<OffsetDay>, [| ActualPayment.quickConfirmed 367_72L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterest = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 838_64L<Cent>

    [<Fact>]
    let InterestFirstTest009 () =
        let title = "InterestFirstTest009"
        let description = "Add-on interest method with single early repayment"
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.quickConfirmed 1007_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal (ValueSome 737_36L<Cent>)

    [<Fact>]
    let InterestFirstTest010 () =
        let title = "InterestFirstTest010"
        let description = "Add-on interest method with single early repayment then a quote one day later"
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.quickConfirmed 1007_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterest = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 14_65L<Cent>

    [<Fact>]
    let InterestFirstTest011 () =
        let title = "InterestFirstTest011"
        let description = "Add-on interest method with small loan and massive payment leading to a refund needed"
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 180; Principal = 100_00L<Cent> }

        let actualPayments =
            Map [
                10<OffsetDay>, [| ActualPayment.quickConfirmed 1000_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterest = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion
        totalInterest |> should equal -25_24L<Cent>

    [<Fact>]
    let InterestFirstTest012 () =
        let title = "InterestFirstTest012"
        let description = "Realistic example 501ac58e62a5"
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2022, 2, 28); Principal = 400_00L<Cent>; ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 4, 1); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) } }

        let actualPayments =
            Map [
                (Date(2022,  4, 10) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 198_40L<Cent> |]
                (Date(2022,  5, 14) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 198_40L<Cent> |]
                (Date(2022,  6, 10) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 198_40L<Cent> |]
                (Date(2022,  6, 17) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 198_40L<Cent> |]
                (Date(2022,  7, 15) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 204_80L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal (ValueSome -324_57L<Cent>)

    [<Fact>]
    let InterestFirstTest013 () =
        let title = "InterestFirstTest013"
        let description = "Realistic example 0004ffd74fbb"
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2023, 6, 7); Principal = 200_00L<Cent>; ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 6, 10); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) } }

        let actualPayments =
            Map [
                (Date(2023,  7, 16) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed  88_00L<Cent> |]
                (Date(2023, 10, 13) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 126_00L<Cent> |]
                (Date(2023, 10, 17) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed  98_00L<Cent> |]
                (Date(2023, 10, 18) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed  88_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal (ValueSome 0L<Cent>)

    [<Fact>]
    let ``InterestFirstTest014`` () =
        let title = "InterestFirstTests014"
        let description = "Realistic example 0004ffd74fbb with overpayment"
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2023, 6, 7); Principal = 200_00L<Cent>; ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 6, 10); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) } }

        let actualPayments =
            Map [
                (Date(2023,  7, 16) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed  88_00L<Cent> |]
                (Date(2023, 10, 13) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 126_00L<Cent> |]
                (Date(2023, 10, 17) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed  98_00L<Cent> |]
                (Date(2023, 10, 18) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.quickConfirmed 100_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> (fun asi -> asi.InterestPortion, asi.PrincipalPortion, asi.SettlementFigure)

        finalSettlementFigure |> should equal (-78L<Cent>, -12_00L<Cent>, ValueSome -12_78L<Cent>)

    [<Fact>]
    let InterestFirstTest015 () =
        let title = "InterestFirstTest015"
        let description = "Add-on interest method with big early repayment followed by tiny overpayment"
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 1000 }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.quickConfirmed 1007_00L<Cent>; ActualPayment.quickConfirmed 2_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp (ValueSome SettlementDay.SettlementOnAsOfDay) false

        Schedule.outputHtmlToFile title description sp schedules

        let finalSettlementFigure = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal (ValueSome -1_22L<Cent>)

    [<Fact>]
    let InterestFirstTest016 () =
        let title = "InterestFirstTest016"
        let description = "Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (no cap)"
        let sp = { scheduleParameters with AsOfDate = startDate; Parameters.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.4m; Parameters.InterestConfig.Cap = Interest.Cap.Zero }

        let actualPayments = Map.empty

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let initialInterestBalance = schedules.AmortisationSchedule.ScheduleItems[0<OffsetDay>].InterestBalance

        initialInterestBalance |> should equal 362_34m<Cent>

    [<Fact>]
    let InterestFirstTest017 () =
        let title = "InterestFirstTest017"
        let description = "Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (cap in place)"
        let sp = { scheduleParameters with AsOfDate = startDate; Parameters.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.4m }

        let actualPayments = Map.empty

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let initialInterestBalance = schedules.AmortisationSchedule.ScheduleItems[0<OffsetDay>].InterestBalance
        
        initialInterestBalance |> should equal 362_34m<Cent>

    [<Fact>]
    let InterestFirstTest018 () =
        let title = "InterestFirstTest018"
        let description = "Realistic test 6045bd0ffc0f with correction on final day"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 9, 22)
                Principal = 740_00L<Cent>
                Parameters.InterestConfig.RateOnNegativeBalance = Interest.Rate.Zero
                ScheduleConfig = [|
                    14<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                    37<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                    68<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                    98<OffsetDay>, ScheduledPayment.quick (ValueSome 33004L<Cent>) ValueNone
                    42<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    49<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    56<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    63<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    70<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    77<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    84<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    91<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 8000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    56<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    86<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    117<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    148<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    177<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    208<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    238<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    269<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    299<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    330<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    361<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 18000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    119<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    126<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    133<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    140<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    147<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    154<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    161<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    168<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    175<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    182<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    189<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    196<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    203<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    210<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    217<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    224<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    231<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    238<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    245<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    252<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    259<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    266<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    273<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    280<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    287<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    294<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    301<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    308<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    315<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    322<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    329<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    336<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    343<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    350<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    357<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 4000L<Cent>; RescheduleDay = 115<OffsetDay> })
                |]
                |> mergeScheduledPayments
                |> CustomSchedule
            }

        let actualPayments =
            [|
                14<OffsetDay>, [| ActualPayment.quickFailed 33004L<Cent> [||] |]
                14<OffsetDay>, [| ActualPayment.quickFailed 33004L<Cent> [||] |]
                14<OffsetDay>, [| ActualPayment.quickFailed 33004L<Cent> [||] |]
                42<OffsetDay>, [| ActualPayment.quickFailed 20000L<Cent> [||] |]
                42<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                56<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                56<OffsetDay>, [| ActualPayment.quickConfirmed 5000L<Cent> |]
                86<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                86<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                86<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                89<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                89<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                89<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                92<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                92<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                92<OffsetDay>, [| ActualPayment.quickFailed 5000L<Cent> [||] |]
                119<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                119<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                119<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                122<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                122<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                122<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                125<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                125<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                125<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                126<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                126<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                126<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                129<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                129<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                129<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                133<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                133<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                133<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                136<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                136<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                136<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                139<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                139<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                139<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                140<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                140<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                140<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                143<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                146<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                146<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                146<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                147<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                147<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                147<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                150<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                150<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                150<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                153<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                153<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                153<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                154<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                154<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                154<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                161<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                161<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                161<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                164<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                168<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                168<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                168<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                171<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                171<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                171<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                174<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                174<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                174<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                175<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                175<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                175<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                182<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                182<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                182<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                189<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                189<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                196<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                196<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                196<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                199<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                199<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                199<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                202<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                202<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                202<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                203<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                203<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                203<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                206<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                206<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                206<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                210<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                217<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                217<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                224<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                231<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                238<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                245<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                252<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                259<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                266<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                273<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                280<OffsetDay>, [| ActualPayment.quickFailed 3500L<Cent> [||] |]
                280<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                287<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                294<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                301<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                308<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                314<OffsetDay>, [| ActualPayment.quickConfirmed 25000L<Cent> |]
                315<OffsetDay>, [| ActualPayment.quickConfirmed 1L<Cent> |]
            |]
            |> Map.ofArrayWithMerge

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedules |> Schedule.outputHtmlToFile title description sp

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 740_00L<Cent>

    [<Fact>]
    let InterestFirstTest019 () =
        let title = "InterestFirstTest019"
        let description = "Realistic test 6045bd0ffc0f"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 9, 22)
                Principal = 740_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 29); PaymentCount = 4; PaymentValue = 293_82L<Cent>; ScheduleType = ScheduleType.Original } |]
            }

        let actualPayments =
            Map [
                42<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                56<OffsetDay>, [| ActualPayment.quickConfirmed 5000L<Cent> |]
                133<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent>; ActualPayment.quickConfirmed 3500L<Cent> |]
                147<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                154<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent>; ActualPayment.quickConfirmed 3500L<Cent> |]
                164<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                175<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent>; ActualPayment.quickConfirmed 3500L<Cent> |]
                182<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                189<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                203<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                206<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                210<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                217<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                224<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                231<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                238<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                245<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                252<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                259<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                266<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                273<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                280<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                287<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                294<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                301<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                308<OffsetDay>, [| ActualPayment.quickConfirmed 3500L<Cent> |]
                314<OffsetDay>, [| ActualPayment.quickConfirmed 25000L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 740_00L<Cent>

    [<Fact>]
    let InterestFirstTest020 () =
        let title = "InterestFirstTest020"
        let description = "Realistic test 6045bd123363 with correction on final day"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 1, 14)
                Principal = 100_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 2, 3); PaymentCount = 4; PaymentValue = 42_40L<Cent>; ScheduleType = ScheduleType.Original } |]
            }

        let actualPayments =
            [|
                20<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
            |]
            |> Map.ofArray

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedules |> Schedule.outputHtmlToFile title description sp

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 16_00L<Cent>

    [<Fact>]
    let InterestFirstTest021 () =
        let title = "InterestFirstTest021"
        let description = "Realistic test 6045bd123363"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 1, 14)
                Principal = 100_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 2, 3); PaymentCount = 4; PaymentValue = 42_40L<Cent>; ScheduleType = ScheduleType.Original } |]
            }

        let actualPayments =
            Map [
                20<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 16_00L<Cent>

    [<Fact>]
    let InterestFirstTest022 () =
        let title = "InterestFirstTest022"
        let description = "Realistic test 0004ffd74fbbn"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2018, 1, 26)
                Principal = 340_00L<Cent>
                ScheduleConfig = FixedSchedules [|
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2018, 3, 1); PaymentCount = 11; PaymentValue = 55_60L<Cent>; ScheduleType = ScheduleType.Original }
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2019, 2, 1); PaymentCount = 1; PaymentValue = 55_58L<Cent>; ScheduleType = ScheduleType.Original }
                |]
            }

        let actualPayments =
            Map [
                34<OffsetDay>, [| ActualPayment.quickFailed 5560L<Cent> [||] |]
                35<OffsetDay>, [| ActualPayment.quickConfirmed 5571L<Cent> |]
                60<OffsetDay>, [| ActualPayment.quickConfirmed 5560L<Cent> |]
                90<OffsetDay>, [| ActualPayment.quickConfirmed 5560L<Cent> |]
                119<OffsetDay>, [| ActualPayment.quickConfirmed 5560L<Cent> |]
                152<OffsetDay>, [| ActualPayment.quickConfirmed 5560L<Cent> |]
                214<OffsetDay>, [| ActualPayment.quickConfirmed 5857L<Cent>; ActualPayment.quickConfirmed 5560L<Cent> |]
                273<OffsetDay>, [| ActualPayment.quickConfirmed 5835L<Cent>; ActualPayment.quickConfirmed 5560L<Cent> |]
                305<OffsetDay>, [| ActualPayment.quickConfirmed 16678L<Cent>  |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 340_00L<Cent>

    [<Fact>]
    let InterestFirstTest023 () =
        let title = "InterestFirstTest023"
        let description = "Realistic test 0003ff008ae5"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2022, 12, 1)
                Principal = 1_500_00L<Cent>
                ScheduleConfig = FixedSchedules [|
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 1, 2); PaymentCount = 6; PaymentValue = 500_00L<Cent>; ScheduleType = ScheduleType.Original }
                |]
            }

        let actualPayments =
            Map [
                32<OffsetDay>, [| ActualPayment.quickConfirmed 50000L<Cent> |]
                63<OffsetDay>, [| ActualPayment.quickConfirmed 50000L<Cent> |]
                148<OffsetDay>, [| ActualPayment.quickFailed 20000L<Cent> [||]; ActualPayment.quickConfirmed 20000L<Cent> |]
                181<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                209<OffsetDay>, [| ActualPayment.quickFailed 20000L<Cent> [||] |]
                212<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                242<OffsetDay>, [| ActualPayment.quickFailed 20000L<Cent> [||]; ActualPayment.quickConfirmed 20000L<Cent> |]
                273<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                304<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                334<OffsetDay>, [| ActualPayment.quickFailed 20000L<Cent> [||]; ActualPayment.quickConfirmed 20000L<Cent> |]
                365<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                395<OffsetDay>, [| ActualPayment.quickConfirmed 20000L<Cent> |]
                426<OffsetDay>, [| ActualPayment.quickFailed 20000L<Cent> [||]; ActualPayment.quickConfirmed 20000L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 1_500_00L<Cent>

    [<Fact>]
    let InterestFirstTest024 () =
        let title = "InterestFirstTest024"
        let description = "Realistic test 0003ff00bffb with actuarial method"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2021, 2, 2)
                Principal = 350_00L<Cent>
                ScheduleConfig = FixedSchedules [|
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2021, 2, 28); PaymentCount = 4; PaymentValue = 168_00L<Cent>; ScheduleType = ScheduleType.Original }
                |]
                Parameters.InterestConfig.Method = Interest.Method.Simple
            }

        let actualPayments =
            Map [
                26<OffsetDay>, [| ActualPayment.quickConfirmed 16800L<Cent> |]
                85<OffsetDay>, [| ActualPayment.quickConfirmed 8400L<Cent> |]
                189<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                220<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                251<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                281<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                317<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                402<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                422<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                430<OffsetDay>, [| ActualPayment.quickConfirmed 706L<Cent> |]
                462<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                506<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                531<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                562<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                583<OffsetDay>, [| ActualPayment.quickConfirmed 689L<Cent> |]
                615<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                646<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                689<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                715<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                741<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                750<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                771<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                799<OffsetDay>, [| ActualPayment.quickConfirmed 921L<Cent> |]
                855<OffsetDay>, [| ActualPayment.quickConfirmed 921L<Cent> |]
                856<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                895<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                924<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                954<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                988<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                1039<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1073<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1106<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1141<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1169<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1192<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1224<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
                1253<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
                1290<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
                1316<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>

    [<Fact>]
    let InterestFirstTest025 () =
        let title = "InterestFirstTest025"
        let description = "Realistic test 0003ff00bffb with add-on method"
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2021, 2, 2)
                Principal = 350_00L<Cent>
                ScheduleConfig = FixedSchedules [|
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2021, 2, 28); PaymentCount = 4; PaymentValue = 168_00L<Cent>; ScheduleType = ScheduleType.Original }
                |]
            }

        let actualPayments =
            Map [
                26<OffsetDay>, [| ActualPayment.quickConfirmed 16800L<Cent> |]
                85<OffsetDay>, [| ActualPayment.quickConfirmed 8400L<Cent> |]
                189<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                220<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                251<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                281<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                317<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                402<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                422<OffsetDay>, [| ActualPayment.quickConfirmed 546L<Cent> |]
                430<OffsetDay>, [| ActualPayment.quickConfirmed 706L<Cent> |]
                462<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                506<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                531<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                562<OffsetDay>, [| ActualPayment.quickConfirmed 598L<Cent> |]
                583<OffsetDay>, [| ActualPayment.quickConfirmed 689L<Cent> |]
                615<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                646<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                689<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                715<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                741<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                750<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                771<OffsetDay>, [| ActualPayment.quickConfirmed 869L<Cent> |]
                799<OffsetDay>, [| ActualPayment.quickConfirmed 921L<Cent> |]
                855<OffsetDay>, [| ActualPayment.quickConfirmed 921L<Cent> |]
                856<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                895<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                924<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                954<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                988<OffsetDay>, [| ActualPayment.quickConfirmed 862L<Cent> |]
                1039<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1073<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1106<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1141<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1169<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1192<OffsetDay>, [| ActualPayment.quickConfirmed 883L<Cent> |]
                1224<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
                1253<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
                1290<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
                1316<OffsetDay>, [| ActualPayment.quickConfirmed 911L<Cent> |]
            ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile title description sp schedules

        let totalInterestPortions = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>
