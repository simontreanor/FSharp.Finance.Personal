namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestFirstTests =

    open Amortisation
    open ArrayExtension
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open PaymentSchedule
    open Percentages
    open ValueOptionCE

    let startDate = Date(2024, 7, 23)
    let scheduleParameters =
        {
            AsOfDate = startDate.AddDays 180
            StartDate = startDate
            Principal = 1000_00L<Cent>
            PaymentSchedule = RegularSchedule (UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 8, 2), PaymentCount = 5, MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> })
            PaymentOptions = { ScheduledPaymentOption = AsScheduled; CloseBalanceOption = LeaveOpenBalance }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.None
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.AddOn Interest.AddOnInterestCorrection.CorrectOnFinalDay
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = { Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); Daily = ValueSome <| Amount.Percentage (Percent 0.8m, ValueNone, ValueNone) }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

    [<Fact>]
    let ``1) Simple interest method initial schedule`` () =
        let sp = { scheduleParameters with Parameters.Interest.Method = Interest.Method.Simple }

        let actual =
            voption {
                let! schedule = sp |> PaymentSchedule.calculate BelowZero
                schedule.Items |> Formatting.outputListToHtml "out/InterestFirstTest001.md" false
                return schedule.LevelPayment, schedule.FinalPayment
            }

        let expected = ValueSome (319_26L<Cent>, 319_23L<Cent>)
        actual |> should equal expected

    [<Fact>]
    let ``2) Simple interest method`` () =
        let sp = { scheduleParameters with Parameters.Interest.Method = Interest.Method.Simple }

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest002.md" false))

        let finalInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.InterestBalance) |> ValueOption.defaultValue 0m<Cent>
        finalInterestBalance |> should equal 1000_00m<Cent>

    [<Fact>]
    let ``3) Add-on interest method initial schedule`` () =
        let sp = scheduleParameters

        let actual =
            voption {
                let! schedule = sp |> PaymentSchedule.calculate BelowZero
                schedule.Items |> Formatting.outputListToHtml "out/InterestFirstTest003.md" false
                return schedule.LevelPayment, schedule.FinalPayment
            }

        let expected = ValueSome (367_73L<Cent>, 367_72L<Cent>)
        actual |> should equal expected

    [<Fact>]
    let ``4) Add-on interest method`` () =
        let sp = scheduleParameters

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest004.md" false))

        let finalSettlementFigure = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        finalSettlementFigure |> should equal 2000_00L<Cent>

    [<Fact>]
    let ``5) Add-on interest method with early repayment`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9) }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed 10<OffsetDay> 271_37L<Cent> //normal
                CustomerPayment.ActualConfirmed 17<OffsetDay> 271_37L<Cent> //all
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest005.md" false))

        let totalInterest = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        totalInterest |> should equal 838_64L<Cent>

    [<Fact>]
    let ``6) Add-on interest method with normal but very early repayments`` () =
        let sp = scheduleParameters

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed 1<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed 2<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed 3<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed 4<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed 5<OffsetDay> 367_72L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest006.md" false))

        let totalInterest = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        totalInterest |> should equal 24_00L<Cent>

    [<Fact>]
    let ``7) Add-on interest method with normal but with erratic payment timings`` () =
        let startDate = Date(2022, 1, 10)
        let sp =
            { scheduleParameters with
                StartDate = startDate
                Principal = 700_00L<Cent>
                PaymentSchedule = RegularSchedule (UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 1, 28), PaymentCount = 4, MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> })
            }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed 18<OffsetDay> 294_91L<Cent>
                CustomerPayment.ActualConfirmed 35<OffsetDay> 294_91L<Cent>
                CustomerPayment.ActualConfirmed 168<OffsetDay> 810_18L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest007.md" false))

        let finalPrincipalBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.PrincipalBalance) |> ValueOption.defaultValue 0L<Cent>
        finalPrincipalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``8) Add-on interest method with normal but with erratic payment timings expecting settlement figure on final day`` () =
        let startDate = Date(2022, 1, 10)
        let sp =
            { scheduleParameters with
                StartDate = startDate
                Principal = 700_00L<Cent>
                PaymentSchedule = RegularSchedule (UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 1, 28), PaymentCount = 4, MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> })
            }

        // 700 over 108 days with 4 payments, paid on 1ot 2fewdayslate last2 in one go 2months late

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed 18<OffsetDay> 294_91L<Cent>
                CustomerPayment.ActualConfirmed 35<OffsetDay> 294_91L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest008.md" false))

        let finalSettlementFigure = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        finalSettlementFigure |> should equal 650_63L<Cent>

    [<Fact>]
    let ``9) Add-on interest method with normal repayments`` () =
        let sp = scheduleParameters

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed  10<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed  41<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed  71<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed 102<OffsetDay> 367_73L<Cent>
                CustomerPayment.ActualConfirmed 132<OffsetDay> 367_72L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest009.md" false))

        let totalInterest = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        totalInterest |> should equal 838_64L<Cent>

    [<Fact>]
    let ``10) Add-on interest method with single early repayment`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed   1<OffsetDay> 1007_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest010.md" false))

        let finalSettlementFigure = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        finalSettlementFigure |> should equal 737_36L<Cent>

    [<Fact>]
    let ``11) Add-on interest method with single early repayment then a quote one day later`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed   1<OffsetDay> 1007_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest011.md" false))

        let totalInterest = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        totalInterest |> should equal 14_65L<Cent>

    [<Fact>]
    let ``12) Add-on interest method with small loan and massive payment leading to a refund needed`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 180; Principal = 100_00L<Cent> }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed  10<OffsetDay> 1000_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest012.md" false))

        let totalInterest = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        totalInterest |> should equal -25_24L<Cent>

    [<Fact>]
    let ``13) Realistic example 501ac58e62a5`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2022, 2, 28); Principal = 400_00L<Cent>; PaymentSchedule = RegularSchedule(UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 4, 1), PaymentCount = 4, MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> }) }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed (Date(2022,  4, 10) |> OffsetDay.fromDate sp.StartDate)  198_40L<Cent>
                CustomerPayment.ActualConfirmed (Date(2022,  5, 14) |> OffsetDay.fromDate sp.StartDate)  198_40L<Cent>
                CustomerPayment.ActualConfirmed (Date(2022,  6, 10) |> OffsetDay.fromDate sp.StartDate)  198_40L<Cent>
                CustomerPayment.ActualConfirmed (Date(2022,  6, 17) |> OffsetDay.fromDate sp.StartDate)  198_40L<Cent>
                CustomerPayment.ActualConfirmed (Date(2022,  7, 15) |> OffsetDay.fromDate sp.StartDate)  204_80L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest013.md" false))

        let finalSettlementFigure = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        finalSettlementFigure |> should equal -324_49L<Cent>

    [<Fact>]
    let ``14) Realistic example 0004ffd74fbb`` () =
        let sp = { scheduleParameters with AsOfDate = Date(2024, 8, 9); StartDate = Date(2023, 6, 7); Principal = 200_00L<Cent>; PaymentSchedule = RegularSchedule(UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 6, 10), PaymentCount = 4, MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> }) }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed (Date(2023,  7, 16) |> OffsetDay.fromDate sp.StartDate)   88_00L<Cent>
                CustomerPayment.ActualConfirmed (Date(2023, 10, 13) |> OffsetDay.fromDate sp.StartDate)  126_00L<Cent>
                CustomerPayment.ActualConfirmed (Date(2023, 10, 17) |> OffsetDay.fromDate sp.StartDate)   98_00L<Cent>
                CustomerPayment.ActualConfirmed (Date(2023, 10, 18) |> OffsetDay.fromDate sp.StartDate)   88_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest014.md" false))

        let finalSettlementFigure = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        finalSettlementFigure |> should equal -5_81L<Cent>

    [<Fact>]
    let ``15) Add-on interest method with big early repayment followed by tiny overpayment`` () =
        let sp = { scheduleParameters with AsOfDate = startDate.AddDays 1000 }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed   1<OffsetDay> 1007_00L<Cent>
                CustomerPayment.ActualConfirmed   1<OffsetDay>    2_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> (Formatting.outputListToHtml "out/InterestFirstTest015.md" false))

        let finalSettlementFigure = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        finalSettlementFigure |> should equal -1_22L<Cent>

    [<Fact>]
    let ``16) Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (no cap)`` () =
        let sp = { scheduleParameters with AsOfDate = startDate; Parameters.Interest.StandardRate = Interest.Rate.Daily <| Percent 0.4m; Parameters.Interest.Cap = Interest.Cap.none }

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest016.md" false)

        let initialInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems.[0].InterestBalance) |> ValueOption.defaultValue 0m<Cent>
        initialInterestBalance |> should equal 362_34m<Cent>

    [<Fact>]
    let ``17) Add-on interest method with interest rate under the daily cap should have a lower initial interest balance than the cap (cap in place)`` () =
        let sp = { scheduleParameters with AsOfDate = startDate; Parameters.Interest.StandardRate = Interest.Rate.Daily <| Percent 0.4m }

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest017.md" false)

        let initialInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems.[0].InterestBalance) |> ValueOption.defaultValue 0m<Cent>
        initialInterestBalance |> should equal 362_34m<Cent>

    [<Fact>]
    let ``18) Realistic test 6045bd0ffc0f with correction on final day`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 9, 22)
                Principal = 740_00L<Cent>
                PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 29); PaymentCount = 4; PaymentAmount = 293_82L<Cent> } |]
                Parameters.Interest.RateOnNegativeBalance = ValueNone
                // PaymentSchedule = IrregularSchedule [|
                //     // CustomerPayment.ScheduledOriginal 14<OffsetDay> 33004L<Cent>
                //     // CustomerPayment.ScheduledOriginal 37<OffsetDay> 33004L<Cent>
                //     // CustomerPayment.ScheduledOriginal 68<OffsetDay> 33004L<Cent>
                //     // CustomerPayment.ScheduledOriginal 98<OffsetDay> 33004L<Cent>
                //     CustomerPayment.ScheduledRescheduled 42<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 49<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 56<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 63<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 70<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 77<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 84<OffsetDay> 20000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 91<OffsetDay> 8000L<Cent>
                //     CustomerPayment.ScheduledRescheduled 56<OffsetDay> 5000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 86<OffsetDay> 5000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 117<OffsetDay> 5000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 148<OffsetDay> 5000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 177<OffsetDay> 15000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 208<OffsetDay> 15000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 238<OffsetDay> 15000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 269<OffsetDay> 15000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 299<OffsetDay> 15000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 330<OffsetDay> 15000L<Cent>
                //     // CustomerPayment.ScheduledRescheduled 361<OffsetDay> 18000L<Cent>
                //     CustomerPayment.ScheduledRescheduled 119<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 126<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 133<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 140<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 147<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 154<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 161<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 168<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 175<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 182<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 189<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 196<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 203<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 210<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 217<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 224<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 231<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 238<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 245<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 252<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 259<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 266<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 273<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 280<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 287<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 294<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 301<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 308<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 315<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 322<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 329<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 336<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 343<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 350<OffsetDay> 3500L<Cent>
                //     CustomerPayment.ScheduledRescheduled 357<OffsetDay> 4000L<Cent>
                // |]
            }

        let actualPayments = [|
            // CustomerPayment.ActualFailed 14<OffsetDay> 33004L<Cent> [||]
            // CustomerPayment.ActualFailed 14<OffsetDay> 33004L<Cent> [||]
            // CustomerPayment.ActualFailed 14<OffsetDay> 33004L<Cent> [||]
            // CustomerPayment.ActualFailed 42<OffsetDay> 20000L<Cent> [||]
            CustomerPayment.ActualConfirmed 42<OffsetDay> 20000L<Cent>
            // CustomerPayment.ActualFailed 56<OffsetDay> 5000L<Cent> [||]
            CustomerPayment.ActualConfirmed 56<OffsetDay> 5000L<Cent>
            // CustomerPayment.ActualFailed 86<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 86<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 86<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 89<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 89<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 89<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 92<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 92<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 92<OffsetDay> 5000L<Cent> [||]
            // CustomerPayment.ActualFailed 119<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 119<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 119<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 122<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 122<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 122<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 125<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 125<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 125<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 126<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 126<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 126<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 129<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 129<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 129<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 132<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 132<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 132<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 132<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 133<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 133<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 133<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 136<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 136<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 136<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 139<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 139<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 139<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 140<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 140<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 140<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 143<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 143<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 143<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 146<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 146<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 146<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 147<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 147<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 147<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 150<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 150<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 150<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 153<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 153<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 153<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 154<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 154<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 154<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 161<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 161<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 161<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 164<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 168<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 168<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 168<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 171<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 171<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 171<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 174<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 174<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 174<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 175<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 175<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 175<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 182<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 182<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 182<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 189<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 189<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 196<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 196<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 196<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 199<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 199<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 199<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 202<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 202<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 202<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 203<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 203<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 203<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 206<OffsetDay> 3500L<Cent> [||]
            // CustomerPayment.ActualFailed 206<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 206<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 210<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 217<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 217<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 224<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 231<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 238<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 245<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 252<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 259<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 266<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 273<OffsetDay> 3500L<Cent>
            // CustomerPayment.ActualFailed 280<OffsetDay> 3500L<Cent> [||]
            CustomerPayment.ActualConfirmed 280<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 287<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 294<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 301<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 308<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 314<OffsetDay> 25000L<Cent>
            CustomerPayment.ActualConfirmed 315<OffsetDay> 1L<Cent>
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest018.md" false)

        let initialInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>

        initialInterestBalance |> should equal 740_00L<Cent>

    [<Fact>]
    let ``19) Realistic test 6045bd0ffc0f with continuous correction`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 9, 22)
                Principal = 740_00L<Cent>
                PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 29); PaymentCount = 4; PaymentAmount = 293_82L<Cent> } |]
                Parameters.Interest.Method = Interest.Method.AddOn Interest.AddOnInterestCorrection.CorrectOnDeviation
            }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 42<OffsetDay> 20000L<Cent>
            CustomerPayment.ActualConfirmed 56<OffsetDay> 5000L<Cent>
            CustomerPayment.ActualConfirmed 133<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 143<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 143<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 147<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 154<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 154<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 164<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 175<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 175<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 182<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 189<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 203<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 206<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 210<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 217<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 224<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 231<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 238<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 245<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 252<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 259<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 266<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 273<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 280<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 287<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 294<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 301<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 308<OffsetDay> 3500L<Cent>
            CustomerPayment.ActualConfirmed 314<OffsetDay> 25000L<Cent>
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest019.md" false)

        let initialInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>

        initialInterestBalance |> should equal 740_00L<Cent>

    [<Fact>]
    let ``20) Realistic test 6045bd123363 with correction on final day`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 1, 14)
                Principal = 100_00L<Cent>
                PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 2, 3); PaymentCount = 4; PaymentAmount = 42_40L<Cent> } |]
            }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 20<OffsetDay> 116_00L<Cent>
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest020.md" false)

        let initialInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>

        initialInterestBalance |> should equal 16_00L<Cent>

    [<Fact>]
    let ``21) Realistic test 6045bd123363 with continuous correction`` () =
        let sp =
            { scheduleParameters with
                AsOfDate = Date(2024, 9, 17)
                StartDate = Date(2023, 1, 14)
                Principal = 100_00L<Cent>
                PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 2, 3); PaymentCount = 4; PaymentAmount = 42_40L<Cent> } |]
                Parameters.Interest.Method = Interest.Method.AddOn Interest.AddOnInterestCorrection.CorrectOnDeviation
            }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 20<OffsetDay> 116_00L<Cent>
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest021.md" false)

        let initialInterestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>

        initialInterestBalance |> should equal 16_00L<Cent>
