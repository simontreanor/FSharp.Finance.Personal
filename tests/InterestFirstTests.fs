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
    let scheduleParameters interestMethod =
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
                Method = interestMethod
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = { Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); Daily = ValueSome <| Amount.Percentage (Percent 0.8m, ValueNone, ValueNone) }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
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
        let sp = scheduleParameters Interest.Method.Simple

        let actual =
            voption {
                let! schedule = sp |> PaymentSchedule.calculate BelowZero
                schedule.Items |> Formatting.outputListToHtml "out/InterestFirstTest001.md"
                return schedule.LevelPayment, schedule.FinalPayment
            }

        let expected = ValueSome (319_26L<Cent>, 319_23L<Cent>)
        actual |> should equal expected

    [<Fact>]
    let ``2) Simple interest method`` () =
        let sp = scheduleParameters Interest.Method.Simple

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest002.md")

        let interestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.InterestBalance) |> ValueOption.defaultValue 0m<Cent>
        interestBalance |> should equal 1000_00m<Cent>

    [<Fact>]
    let ``3) Add-on interest method initial schedule`` () =
        let sp = scheduleParameters Interest.Method.AddOn

        let actual =
            voption {
                let! schedule = sp |> PaymentSchedule.calculate BelowZero
                schedule.Items |> Formatting.outputListToHtml "out/InterestFirstTest003.md"
                return schedule.LevelPayment, schedule.FinalPayment
            }

        let expected = ValueSome (367_73L<Cent>, 367_72L<Cent>)
        actual |> should equal expected

    [<Fact>]
    let ``4) Add-on interest method`` () =
        let sp = scheduleParameters Interest.Method.AddOn

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest004.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 2000_00L<Cent>

    [<Fact>]
    let ``5) Add-on interest method with early repayment`` () =
        let sp = 
            { scheduleParameters Interest.Method.AddOn with
                AsOfDate = Date(2024, 8, 9)
            }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed 10<OffsetDay> 271_37L<Cent> //normal
                CustomerPayment.ActualConfirmed 17<OffsetDay> 271_37L<Cent> //all
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest005.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 838_64L<Cent>

    [<Fact>]
    let ``6) Add-on interest method with normal but very early repayments`` () =
        let sp = scheduleParameters Interest.Method.AddOn

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

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest006.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 24_00L<Cent>

    [<Fact>]
    let ``7) Add-on interest method with normal but with erratic payment timings`` () =
        let startDate = Date(2022, 1, 10)
        let sp =
            { scheduleParameters Interest.Method.AddOn with
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

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest007.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.PrincipalBalance) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 0L<Cent>

    [<Fact>]
    let ``8) Add-on interest method with normal but with erratic payment timings expecting settlement figure on final day`` () =
        let startDate = Date(2022, 1, 10)
        let sp =
            { scheduleParameters Interest.Method.AddOn with
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

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest008.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 650_63L<Cent>

    [<Fact>]
    let ``9) Add-on interest method with normal repayments`` () =
        let sp = scheduleParameters Interest.Method.AddOn

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

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest009.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 838_64L<Cent>

    [<Fact>]
    let ``10) Add-on interest method with single early repayment`` () =
        let sp = { scheduleParameters Interest.Method.AddOn with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed   1<OffsetDay> 1007_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest010.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.last |> _.SettlementFigure) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 737_36L<Cent>

    [<Fact>]
    let ``11) Add-on interest method with single early repayment then a quote one day later`` () =
        let sp = { scheduleParameters Interest.Method.AddOn with AsOfDate = startDate.AddDays 2 }

        let actualPayments =
            [|
                CustomerPayment.ActualConfirmed   1<OffsetDay> 1007_00L<Cent>
            |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp (IntendedPurpose.Settlement ValueNone) ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest011.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 14_65L<Cent>

