namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestFirstTests =

    open Amortisation
    open ArrayExtension
    open Calculation
    open Currency
    open DateDay
    open Formatting
    open MapExtension
    open PaymentSchedule
    open Util

    let startDate = Date(2024, 7, 23)
    let scheduleParameters =
        {
            AsOfDate = startDate.AddDays 180
            StartDate = startDate
            Principal = 1000_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 8, 2); PaymentCount = 5; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = { TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown); DailyAmount = ValueSome <| Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding) }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

    [<Fact>]
    let ``1) Simple interest method initial schedule`` () =
        let sp = { scheduleParameters with Parameters.InterestConfig.Method = Interest.Method.Simple }

        let actual =
            let schedule = sp |> PaymentSchedule.calculate BelowZero
            schedule.Items |> outputArrayToHtml "out/InterestFirstTest001.md" false
            schedule.LevelPayment, schedule.FinalPayment

        let expected = 319_26L<Cent>, 319_23L<Cent>

        actual |> should equal expected

    [<Fact>]
    let ``2) Simple interest method`` () =
        let sp = { scheduleParameters with Parameters.InterestConfig.Method = Interest.Method.Simple }

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest002.md" false

        let finalInterestBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance

        finalInterestBalance |> should equal 1000_00m<Cent>

    [<Fact>]
    let ``3) Add-on interest method initial schedule`` () =
        let sp = scheduleParameters

        let actual =
            let schedule = sp |> PaymentSchedule.calculate BelowZero
            schedule.Items |> outputArrayToHtml "out/InterestFirstTest003.md" false
            schedule.LevelPayment, schedule.FinalPayment

        let expected = 367_73L<Cent>, 367_72L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Add-on interest method`` () =
        let sp = scheduleParameters

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest004.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal 2000_00L<Cent>

    [<Fact>]
    let ``5) Add-on interest method with early repayment`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9) }

        let actualPayments =
            Map [
                10<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |] //normal
                17<OffsetDay>, [| ActualPayment.QuickConfirmed 271_37L<Cent> |] //all
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest005.md" false

        let totalInterest = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 838_64L<Cent>

    [<Fact>]
    let ``6) Add-on interest method with normal but very early repayments`` () =
        let sp = scheduleParameters

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                2<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                3<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                4<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                5<OffsetDay>, [| ActualPayment.QuickConfirmed 367_72L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest006.md" false

        let totalInterest = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 23_88L<Cent>

    [<Fact>]
    let ``7) Add-on interest method with normal but with erratic payment timings`` () =
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
                18<OffsetDay>, [| ActualPayment.QuickConfirmed 294_91L<Cent> |]
                35<OffsetDay>, [| ActualPayment.QuickConfirmed 294_91L<Cent> |]
                168<OffsetDay>, [| ActualPayment.QuickConfirmed 810_18L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest007.md" false

        let finalPrincipalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance

        finalPrincipalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``8) Add-on interest method with normal but with erratic payment timings expecting settlement figure on final day`` () =
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
                18<OffsetDay>, [| ActualPayment.QuickConfirmed 294_91L<Cent> |]
                35<OffsetDay>, [| ActualPayment.QuickConfirmed 294_91L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest008.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal 650_63L<Cent>

    [<Fact>]
    let ``9) Add-on interest method with normal repayments`` () =
        let sp = scheduleParameters

        let actualPayments =
            Map [
                10<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                41<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                71<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                102<OffsetDay>, [| ActualPayment.QuickConfirmed 367_73L<Cent> |]
                132<OffsetDay>, [| ActualPayment.QuickConfirmed 367_72L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest009.md" false

        let totalInterest = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 838_64L<Cent>

    [<Fact>]
    let ``10) Add-on interest method with single early repayment`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.QuickConfirmed 1007_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest010.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal 737_36L<Cent>

    [<Fact>]
    let ``11) Add-on interest method with single early repayment then a quote one day later`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.QuickConfirmed 1007_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest011.md" false

        let totalInterest = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterest |> should equal 14_65L<Cent>

    [<Fact>]
    let ``12) Add-on interest method with small loan and massive payment leading to a refund needed`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 180; Principal = 100_00L<Cent> }

        let actualPayments =
            Map [
                10<OffsetDay>, [| ActualPayment.QuickConfirmed 1000_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest012.md" false

        let totalInterest = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion
        totalInterest |> should equal -25_24L<Cent>

    [<Fact>]
    let ``13) Realistic example 501ac58e62a5`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2022, 2, 28); Principal = 400_00L<Cent>; ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 4, 1); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) } }

        let actualPayments =
            Map [
                (Date(2022,  4, 10) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 198_40L<Cent> |]
                (Date(2022,  5, 14) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 198_40L<Cent> |]
                (Date(2022,  6, 10) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 198_40L<Cent> |]
                (Date(2022,  6, 17) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 198_40L<Cent> |]
                (Date(2022,  7, 15) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 204_80L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest013.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal -324_57L<Cent>

    [<Fact>]
    let ``14) Realistic example 0004ffd74fbb`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2023, 6, 7); Principal = 200_00L<Cent>; ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 6, 10); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) } }

        let actualPayments =
            Map [
                (Date(2023,  7, 16) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed  88_00L<Cent> |]
                (Date(2023, 10, 13) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 126_00L<Cent> |]
                (Date(2023, 10, 17) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed  98_00L<Cent> |]
                (Date(2023, 10, 18) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed  88_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest014.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal 0L<Cent>

    [<Fact>]
    let ``14a) Realistic example 0004ffd74fbb with overpayment`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2023, 6, 7); Principal = 200_00L<Cent>; ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 6, 10); PaymentCount = 4; MaxDuration = Duration.Maximum (180<DurationDay>, startDate) } }

        let actualPayments =
            Map [
                (Date(2023,  7, 16) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed  88_00L<Cent> |]
                (Date(2023, 10, 13) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 126_00L<Cent> |]
                (Date(2023, 10, 17) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed  98_00L<Cent> |]
                (Date(2023, 10, 18) |> OffsetDay.fromDate sp.StartDate), [| ActualPayment.QuickConfirmed 100_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest014a.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> (fun asi -> asi.InterestPortion, asi.PrincipalPortion, asi.SettlementFigure)

        finalSettlementFigure |> should equal (-78L<Cent>, -12_00L<Cent>, -12_78L<Cent>)

    [<Fact>]
    let ``15) Add-on interest method with big early repayment followed by tiny overpayment`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 1000 }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.QuickConfirmed 1007_00L<Cent>; ActualPayment.QuickConfirmed 2_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest015.md" false

        let finalSettlementFigure = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.SettlementFigure

        finalSettlementFigure |> should equal -1_22L<Cent>

    [<Fact>]
    let ``16) Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (no cap)`` () =
        let sp = { scheduleParameters with AsOfDate = startDate; Parameters.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.4m; Parameters.InterestConfig.Cap = Interest.Cap.None }

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest016.md" false

        let initialInterestBalance = schedule.ScheduleItems[0<OffsetDay>].InterestBalance

        initialInterestBalance |> should equal 362_34m<Cent>

    [<Fact>]
    let ``17) Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (cap in place)`` () =
        let sp = { scheduleParameters with AsOfDate = startDate; Parameters.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.4m }

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest017.md" false

        let initialInterestBalance = schedule.ScheduleItems[0<OffsetDay>].InterestBalance
        
        initialInterestBalance |> should equal 362_34m<Cent>

    [<Fact>]
    let ``18) Realistic test 6045bd0ffc0f with correction on final day`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 9, 22)
                Principal = 740_00L<Cent>
                Parameters.InterestConfig.RateOnNegativeBalance = Interest.Rate.Zero
                ScheduleConfig = [|
                    14<OffsetDay>, ScheduledPayment.Quick (ValueSome 33004L<Cent>) ValueNone
                    37<OffsetDay>, ScheduledPayment.Quick (ValueSome 33004L<Cent>) ValueNone
                    68<OffsetDay>, ScheduledPayment.Quick (ValueSome 33004L<Cent>) ValueNone
                    98<OffsetDay>, ScheduledPayment.Quick (ValueSome 33004L<Cent>) ValueNone
                    42<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    49<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    56<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    63<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    70<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    77<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    84<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 20000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    91<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 8000L<Cent>; RescheduleDay = 19<OffsetDay> })
                    56<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    86<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    117<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    148<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    177<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    208<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    238<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    269<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    299<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    330<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 15000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    361<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 18000L<Cent>; RescheduleDay = 47<OffsetDay> })
                    119<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    126<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    133<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    140<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    147<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    154<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    161<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    168<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    175<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    182<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    189<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    196<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    203<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    210<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    217<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    224<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    231<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    238<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    245<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    252<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    259<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    266<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    273<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    280<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    287<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    294<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    301<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    308<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    315<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    322<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    329<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    336<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    343<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    350<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 3500L<Cent>; RescheduleDay = 115<OffsetDay> })
                    357<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome { Value = 4000L<Cent>; RescheduleDay = 115<OffsetDay> })
                |]
                |> mergeScheduledPayments
                |> CustomSchedule
            }

        let actualPayments =
            [|
                14<OffsetDay>, [| ActualPayment.QuickFailed 33004L<Cent> [||] |]
                14<OffsetDay>, [| ActualPayment.QuickFailed 33004L<Cent> [||] |]
                14<OffsetDay>, [| ActualPayment.QuickFailed 33004L<Cent> [||] |]
                42<OffsetDay>, [| ActualPayment.QuickFailed 20000L<Cent> [||] |]
                42<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                56<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                56<OffsetDay>, [| ActualPayment.QuickConfirmed 5000L<Cent> |]
                86<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                86<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                86<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                89<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                89<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                89<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                92<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                92<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                92<OffsetDay>, [| ActualPayment.QuickFailed 5000L<Cent> [||] |]
                119<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                119<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                119<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                122<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                122<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                122<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                125<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                125<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                125<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                126<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                126<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                126<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                129<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                129<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                129<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                132<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                133<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                133<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                133<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                136<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                136<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                136<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                139<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                139<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                139<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                140<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                140<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                140<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                143<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                143<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                143<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                146<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                146<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                146<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                147<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                147<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                147<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                150<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                150<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                150<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                153<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                153<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                153<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                154<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                154<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                154<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                161<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                161<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                161<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                164<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                168<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                168<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                168<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                171<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                171<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                171<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                174<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                174<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                174<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                175<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                175<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                175<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                182<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                182<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                182<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                189<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                189<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                196<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                196<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                196<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                199<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                199<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                199<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                202<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                202<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                202<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                203<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                203<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                203<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                206<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                206<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                206<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                210<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                217<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                217<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                224<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                231<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                238<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                245<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                252<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                259<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                266<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                273<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                280<OffsetDay>, [| ActualPayment.QuickFailed 3500L<Cent> [||] |]
                280<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                287<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                294<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                301<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                308<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                314<OffsetDay>, [| ActualPayment.QuickConfirmed 25000L<Cent> |]
                315<OffsetDay>, [| ActualPayment.QuickConfirmed 1L<Cent> |]
            |]
            |> Map.ofArrayWithArrayMerge

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> Formatting.outputMapToHtml "out/InterestFirstTest018.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 740_00L<Cent>

    [<Fact>]
    let ``19) Realistic test 6045bd0ffc0f`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 9, 22)
                Principal = 740_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 29); PaymentCount = 4; PaymentValue = 293_82L<Cent>; ScheduleType = ScheduleType.Original } |]
            }

        let actualPayments =
            Map [
                42<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                56<OffsetDay>, [| ActualPayment.QuickConfirmed 5000L<Cent> |]
                133<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                143<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent>; ActualPayment.QuickConfirmed 3500L<Cent> |]
                147<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                154<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent>; ActualPayment.QuickConfirmed 3500L<Cent> |]
                164<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                175<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent>; ActualPayment.QuickConfirmed 3500L<Cent> |]
                182<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                189<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                203<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                206<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                210<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                217<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                224<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                231<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                238<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                245<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                252<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                259<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                266<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                273<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                280<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                287<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                294<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                301<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                308<OffsetDay>, [| ActualPayment.QuickConfirmed 3500L<Cent> |]
                314<OffsetDay>, [| ActualPayment.QuickConfirmed 25000L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest019.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 740_00L<Cent>

    [<Fact>]
    let ``20) Realistic test 6045bd123363 with correction on final day`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 1, 14)
                Principal = 100_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 2, 3); PaymentCount = 4; PaymentValue = 42_40L<Cent>; ScheduleType = ScheduleType.Original } |]
            }

        let actualPayments =
            [|
                20<OffsetDay>, [| ActualPayment.QuickConfirmed 116_00L<Cent> |]
            |]
            |> Map.ofArray

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> Formatting.outputMapToHtml "out/InterestFirstTest020.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 16_00L<Cent>

    [<Fact>]
    let ``21) Realistic test 6045bd123363`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 1, 14)
                Principal = 100_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 2, 3); PaymentCount = 4; PaymentValue = 42_40L<Cent>; ScheduleType = ScheduleType.Original } |]
            }

        let actualPayments =
            Map [
                20<OffsetDay>, [| ActualPayment.QuickConfirmed 116_00L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest021.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 16_00L<Cent>

    [<Fact>]
    let ``22) Realistic test 0004ffd74fbbn`` () =
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
                34<OffsetDay>, [| ActualPayment.QuickFailed 5560L<Cent> [||] |]
                35<OffsetDay>, [| ActualPayment.QuickConfirmed 5571L<Cent> |]
                60<OffsetDay>, [| ActualPayment.QuickConfirmed 5560L<Cent> |]
                90<OffsetDay>, [| ActualPayment.QuickConfirmed 5560L<Cent> |]
                119<OffsetDay>, [| ActualPayment.QuickConfirmed 5560L<Cent> |]
                152<OffsetDay>, [| ActualPayment.QuickConfirmed 5560L<Cent> |]
                214<OffsetDay>, [| ActualPayment.QuickConfirmed 5857L<Cent>; ActualPayment.QuickConfirmed 5560L<Cent> |]
                273<OffsetDay>, [| ActualPayment.QuickConfirmed 5835L<Cent>; ActualPayment.QuickConfirmed 5560L<Cent> |]
                305<OffsetDay>, [| ActualPayment.QuickConfirmed 16678L<Cent>  |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest022.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 340_00L<Cent>

    [<Fact>]
    let ``23) Realistic test 0003ff008ae5`` () =
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
                32<OffsetDay>, [| ActualPayment.QuickConfirmed 50000L<Cent> |]
                63<OffsetDay>, [| ActualPayment.QuickConfirmed 50000L<Cent> |]
                148<OffsetDay>, [| ActualPayment.QuickFailed 20000L<Cent> [||]; ActualPayment.QuickConfirmed 20000L<Cent> |]
                181<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                209<OffsetDay>, [| ActualPayment.QuickFailed 20000L<Cent> [||] |]
                212<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                242<OffsetDay>, [| ActualPayment.QuickFailed 20000L<Cent> [||]; ActualPayment.QuickConfirmed 20000L<Cent> |]
                273<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                304<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                334<OffsetDay>, [| ActualPayment.QuickFailed 20000L<Cent> [||]; ActualPayment.QuickConfirmed 20000L<Cent> |]
                365<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                395<OffsetDay>, [| ActualPayment.QuickConfirmed 20000L<Cent> |]
                426<OffsetDay>, [| ActualPayment.QuickFailed 20000L<Cent> [||]; ActualPayment.QuickConfirmed 20000L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest023.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 1_500_00L<Cent>

    [<Fact>]
    let ``24) Realistic test 0003ff00bffb with actuarial method`` () =
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
                26<OffsetDay>, [| ActualPayment.QuickConfirmed 16800L<Cent> |]
                85<OffsetDay>, [| ActualPayment.QuickConfirmed 8400L<Cent> |]
                189<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                220<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                251<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                281<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                317<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                402<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                422<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                430<OffsetDay>, [| ActualPayment.QuickConfirmed 706L<Cent> |]
                462<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                506<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                531<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                562<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                583<OffsetDay>, [| ActualPayment.QuickConfirmed 689L<Cent> |]
                615<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                646<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                689<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                715<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                741<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                750<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                771<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                799<OffsetDay>, [| ActualPayment.QuickConfirmed 921L<Cent> |]
                855<OffsetDay>, [| ActualPayment.QuickConfirmed 921L<Cent> |]
                856<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                895<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                924<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                954<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                988<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                1039<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1073<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1106<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1141<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1169<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1192<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1224<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
                1253<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
                1290<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
                1316<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest024.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>

    [<Fact>]
    let ``25) Realistic test 0003ff00bffb with add-on method`` () =
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
                26<OffsetDay>, [| ActualPayment.QuickConfirmed 16800L<Cent> |]
                85<OffsetDay>, [| ActualPayment.QuickConfirmed 8400L<Cent> |]
                189<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                220<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                251<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                281<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                317<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                402<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                422<OffsetDay>, [| ActualPayment.QuickConfirmed 546L<Cent> |]
                430<OffsetDay>, [| ActualPayment.QuickConfirmed 706L<Cent> |]
                462<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                506<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                531<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                562<OffsetDay>, [| ActualPayment.QuickConfirmed 598L<Cent> |]
                583<OffsetDay>, [| ActualPayment.QuickConfirmed 689L<Cent> |]
                615<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                646<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                689<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                715<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                741<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                750<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                771<OffsetDay>, [| ActualPayment.QuickConfirmed 869L<Cent> |]
                799<OffsetDay>, [| ActualPayment.QuickConfirmed 921L<Cent> |]
                855<OffsetDay>, [| ActualPayment.QuickConfirmed 921L<Cent> |]
                856<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                895<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                924<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                954<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                988<OffsetDay>, [| ActualPayment.QuickConfirmed 862L<Cent> |]
                1039<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1073<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1106<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1141<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1169<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1192<OffsetDay>, [| ActualPayment.QuickConfirmed 883L<Cent> |]
                1224<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
                1253<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
                1290<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
                1316<OffsetDay>, [| ActualPayment.QuickConfirmed 911L<Cent> |]
            ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/InterestFirstTest025.md" false

        let totalInterestPortions = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

        totalInterestPortions |> should equal 350_00L<Cent>
