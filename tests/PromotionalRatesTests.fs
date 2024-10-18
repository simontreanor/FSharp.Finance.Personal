namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PromotionalRatesTests =

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding))
    }

    let startDate = Date(2024, 8, 23)
    let scheduleParameters promotionalRates =
        {
            AsOfDate = startDate.AddDays 180
            StartDate = startDate
            Principal = 400_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 9, 2)
                PaymentCount = 4
                MaxDuration = Duration.Maximum (180<DurationDay>, startDate)
            }
            PaymentConfig = { 
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = {
                    TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown)
                    DailyAmount = ValueSome <| Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding)
                }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = promotionalRates |> ValueOption.defaultValue [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }

    [<Fact>]
    let ``1) Baseline with no promotional rates`` () =
        let sp = scheduleParameters ValueNone

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/PromotionalRatesTest001.md" false

        let interestBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance
        interestBalance |> should equal 323_20m<Cent>

    [<Fact>]
    let ``2) Interest-free October should reduce total interest by 31 days`` () =
        let promotionalRates : Interest.PromotionalRate array = [|
            { DateRange = { Start = Date(2024, 10, 1); End = Date(2024, 10, 31) }; Rate = Interest.Rate.Zero }
        |]

        let sp = scheduleParameters (ValueSome promotionalRates)

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/PromotionalRatesTest002.md" false

        let interestBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance
        interestBalance |> should equal 224_00m<Cent>

    [<Fact>]
    let ``3) Low-interest December should reduce all interest during December`` () =
        let promotionalRates : Interest.PromotionalRate array = [|
            { DateRange = { Start = Date(2024, 12, 1); End = Date(2024, 12, 31) }; Rate = Interest.Rate.Annual <| Percent 20.24m }
        |]

        let sp = scheduleParameters (ValueSome promotionalRates)

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/PromotionalRatesTest003.md" false

        let interestBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance
        interestBalance |> should equal 317_24.36164383m<Cent>

