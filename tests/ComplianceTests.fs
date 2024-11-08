namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ComplianceTests =

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
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
                CloseBalanceOption = IncreaseFinalPayment
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
    let ``Example 1.1) Repayments made on time`` () =
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            90<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            120<OffsetDay>, [| ActualPayment.quickConfirmed 451_43L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest1.1.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Example 1.2) Repayments made early`` () =
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            81<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            101<OffsetDay>, [| ActualPayment.quickConfirmed 350_31L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest1.2.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Example 1.3) Full repayment made on repayment 3`` () =
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            90<OffsetDay>, [| ActualPayment.quickConfirmed 794_55L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest1.3.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Example 1.4) Repayments made late - 3 and 4`` () =
        let actualPayments = Map [
            31<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            95<OffsetDay>, [| ActualPayment.quickConfirmed 451_46L<Cent> |]
            130<OffsetDay>, [| ActualPayment.quickConfirmed 505_60L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters1 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest1.4.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
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
                CloseBalanceOption = IncreaseFinalPayment
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
    let ``Example 2.1) Repayments made on time`` () =
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            107<OffsetDay>, [| ActualPayment.quickConfirmed 209_40L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters2 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest2.1.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Example 2.2) Early repayment`` () =
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            63<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickConfirmed 160_81L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters2 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest2.2.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``Example 2.3) Late repayment`` () =
        let actualPayments = Map [
            17<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            48<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            76<OffsetDay>, [| ActualPayment.quickConfirmed 209_45L<Cent> |]
            122<OffsetDay>, [| ActualPayment.quickConfirmed 234_52L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate scheduleParameters2 ValueNone false

        schedule.ScheduleItems |> outputMapToHtml "out/ComplianceTest2.3.md" false

        let principalBalance = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.PrincipalBalance
        principalBalance |> should equal 0L<Cent>
