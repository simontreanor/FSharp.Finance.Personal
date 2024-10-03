namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PromotionalRatesTests =

    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open FeesAndCharges
    open Formatting
    open PaymentSchedule
    open Percentages

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    let startDate = Date(2024, 8, 23)
    let scheduleParameters promotionalRates =
        {
            AsOfDate = startDate.AddDays 180
            StartDate = startDate
            Principal = 400_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 9, 2); PaymentCount = 4; MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> }}
            PaymentOptions = { ScheduledPaymentOption = AsScheduled; CloseBalanceOption = LeaveOpenBalance }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Cap = { Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); Daily = ValueSome <| Amount.Percentage (Percent 0.8m, ValueNone, ValueNone) }
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = promotionalRates |> ValueOption.defaultValue [||]
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
    let ``1) Baseline with no promotional rates`` () =
        let sp = scheduleParameters ValueNone

        let actualPayments = Map.empty

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter (_.ScheduleItems >> (outputMapToHtml "out/PromotionalRatesTest001.md" false))

        let interestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance) |> ValueOption.defaultValue 0m<Cent>
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter (_.ScheduleItems >> (outputMapToHtml "out/PromotionalRatesTest002.md" false))

        let interestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance) |> ValueOption.defaultValue 0m<Cent>
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
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter (_.ScheduleItems >> (outputMapToHtml "out/PromotionalRatesTest003.md" false))

        let interestBalance = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance) |> ValueOption.defaultValue 0m<Cent>
        interestBalance |> should equal 317_24.36164383m<Cent>

