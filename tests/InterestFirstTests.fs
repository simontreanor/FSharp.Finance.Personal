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
            AsOfDate = startDate
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
                InitialGracePeriod = 3<DurationDay>
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

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 596_27L<Cent>

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

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 838_64L<Cent>
