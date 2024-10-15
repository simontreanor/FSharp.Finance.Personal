namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestTests =

    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open Formatting
    open Interest
    open PaymentSchedule
    open Util
    open System

    let interestCapExample : Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding))
    }

    module RateTests =

        [<Fact>]
        let ``Zero rate converted to annual yields 0%`` () =
            let actual = Rate.Zero |> Rate.annual
            let expected = Percent 0m
            actual |> should equal expected

        [<Fact>]
        let ``Zero rate converted to daily yields 0%`` () =
            let actual = Rate.Zero |> Rate.daily
            let expected = Percent 0m
            actual |> should equal expected

        [<Fact>]
        let ``36,5% annual converted to daily yields 0,1%`` () =
            let actual = Percent 36.5m |> Rate.Annual |> Rate.daily
            let expected = Percent 0.1m
            actual |> should equal expected

        [<Fact>]
        let ``10% daily converted to daily yields the same`` () =
            let actual = Percent 10m |> Rate.Daily |> Rate.daily
            let expected = Percent 10m
            actual |> should equal expected

        [<Fact>]
        let ``10% annual converted to annual yields the same`` () =
            let actual = Percent 10m |> Rate.Annual |> Rate.annual
            let expected = Percent 10m
            actual |> should equal expected

        [<Fact>]
        let ``0,1% daily converted to annual yields 36,5%`` () =
            let actual = Percent 0.1m |> Rate.Daily |> Rate.annual
            let expected = Percent 36.5m
            actual |> should equal expected

    module CapTests =

        [<Fact>]
        let ``No cap total on a €100 principal yields a very large number`` () =
            let actual = Cap.zero.TotalAmount |> Cap.total 100_00L<Cent>
            let expected = 92_233_720_368_547_758_07m<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``100% cap total on a €100 principal yields €100`` () =
            let actual = interestCapExample.TotalAmount |> Cap.total 100_00L<Cent>
            let expected = 100_00m<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``1) Total interest in amortised schedule does not exceed interest cap`` () =
            let sp = {
                AsOfDate = Date(2024, 4, 25)
                StartDate = Date(2023, 2, 9)
                Principal = 499_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 2, 14)
                    PaymentCount = 4
                    MaxDuration = Duration.Unlimited
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
                    Method = Method.Simple
                    StandardRate = Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Zero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    InterestRounding = RoundDown
                }
            }

            let actualPayments = Map.empty

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

            schedule.ScheduleItems |> outputMapToHtml "out/InterestCapTest001.md" false

            let interestPortion = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestPortion

            interestPortion |> should be (lessThanOrEqualTo 499_00L<Cent>)

        [<Fact>]
        let ``2) Total interest in amortised schedule does not exceed interest cap, using unrounded percentages`` () =
            let sp = {
                AsOfDate = Date(2024, 4, 25)
                StartDate = Date(2023, 2, 9)
                Principal = 499_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 2, 14)
                    PaymentCount = 4
                    MaxDuration = Duration.Unlimited
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
                    Method = Method.Simple
                    StandardRate = Rate.Daily (Percent 0.876m)
                    Cap = { interestCapExample with TotalAmount = ValueSome (Amount.Percentage (Percent 123.45m, Restriction.NoLimit, RoundDown)) }
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Zero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    InterestRounding = RoundDown
                }
            }

            let actualPayments = Map.empty

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay false

            schedule.ScheduleItems |> outputMapToHtml "out/InterestCapTest002.md" false

            let interestPortion = schedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestPortion

            interestPortion |> should be (lessThanOrEqualTo 616_01L<Cent>)

    module DailyRatesTests =

        [<Fact>]
        let ``Daily rates with no settlement inside the grace period or promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [||]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate false standardRate promotionalRates fromDay toDay
            let expected = [| 1 .. 10 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Annual (Percent 10m) })
            actual |> should equal expected

        [<Fact>]
        let ``Daily rates with a settlement inside the grace period, but no promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [||]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate true standardRate promotionalRates fromDay toDay
            let expected = [| 1 .. 10 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Zero })
            actual |> should equal expected

        [<Fact>]
        let ``Daily rates with no settlement inside the grace period but with promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [|
                ({ DateRange = { Start = Date(2024, 4, 10); End = Date(2024, 4, 15) }; Rate = Rate.Annual (Percent 2m) } : PromotionalRate)
            |]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate false standardRate promotionalRates fromDay toDay
            let expected =
                [|
                    [| 1 .. 5 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Annual (Percent 2m) })
                    [| 6 .. 10 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Annual (Percent 10m) })
                |]
                |> Array.concat
            actual |> should equal expected

    module PromotionalRatesTests =

        [<Fact>]
        let ``1) Mortgage quote with a five-year fixed interest deal and a mortgage fee added to the loan`` () =
            let sp = {
                AsOfDate = Date(2024, 4, 11)
                StartDate = Date(2024, 4, 11)
                Principal = 192_000_00L<Cent>
                ScheduleConfig = FixedSchedules [|
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 5, 11); PaymentCount = 60; PaymentValue = 1225_86L<Cent>; ScheduleType = ScheduleType.Original }
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2029, 5, 11); PaymentCount = 180; PaymentValue = 1525_12L<Cent>; ScheduleType = ScheduleType.Original }
                |]
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                    PaymentRounding = RoundUp
                    MinimumPayment = NoMinimumPayment
                    PaymentTimeout = 3<DurationDay>
                }
                FeeConfig = {
                    FeeTypes = [| Fee.FeeType.MortageFee <| Amount.Simple 999_00L<Cent> |]
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseBeforePrincipal
                    SettlementRefund = Fee.SettlementRefund.Zero
                }
                ChargeConfig = Charge.Config.initialRecommended
                InterestConfig = {
                    Method = Method.Simple
                    StandardRate = Rate.Annual <| Percent 7.985m
                    Cap = Cap.zero
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [|
                        { DateRange = { Start = Date(2024, 4, 11); End = Date(2029, 4, 10) }; Rate = Rate.Annual <| Percent 4.535m }
                    |]
                    RateOnNegativeBalance = Rate.Zero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    InterestRounding = RoundDown
                }
            }

            let actualPayments = Map.empty

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement false

            schedule.ScheduleItems |> outputMapToHtml "out/InterestTest001.md" false

            let actual = schedule.ScheduleItems |> Map.maxKeyValue

            let expected = 7305<OffsetDay>, {
                OffsetDate = Date(2044, 4, 11)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 1525_12L<Cent>) ValueNone
                Window = 240
                PaymentDue = 1523_25L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 1523_25L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 10_26.07665657m<Cent>
                NewInterest = 10_26.07665657m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1512_99L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 10_26L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1523_25L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

            actual |> should equal expected

    module Cca2004Tests =

        [<Fact>]
        let ``1) UK rebate example 1`` () =
            let principal = 5000_00L<Cent>
            let payments = [| 1 .. 48 |] |> Array.map(fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 12
            let settlementPartPeriod = Fraction.Zero
            let unitPeriod = UnitPeriod.Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero
            let actual = calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding
            let expected = 860_52L<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``1a) UK rebate example 1a`` () =
            let principal = 5000_00L<Cent>
            let payments = [| 1 .. 48 |] |> Array.map(fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 12
            let settlementPartPeriod = Fraction.Simple (28, 30)
            let unitPeriod = UnitPeriod.Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero
            let actual = calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding
            let expected = 819_71L<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``1b) UK rebate example 1b`` () =
            let principal = 5000_00L<Cent>
            let payments = [| 1 .. 48 |] |> Array.map(fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 12
            let settlementPartPeriod = Fraction.Simple (28, 31)
            let unitPeriod = UnitPeriod.Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero
            let actual = calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding
            let expected = 821_03L<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``1c) UK rebate example 1c`` () =
            let principal = 5000_00L<Cent>
            let payments = [| 1 .. 48 |] |> Array.map(fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 13
            let settlementPartPeriod = Fraction.Simple (28, 30)
            let unitPeriod = UnitPeriod.Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero
            let actual = calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding
            let expected = 776_90L<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``2) UK rebate example 2`` () =
            let principal = 10000_00L<Cent>
            let payments = [| 1 .. 180 |] |> Array.map(fun i -> i, 139_51L<Cent>)
            let apr = Percent 16m
            let settlementPeriod = 73
            let settlementPartPeriod = Fraction.Zero
            let unitPeriod = UnitPeriod.Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero
            let actual = calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding
            let expected = 6702_45L<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``2a) UK rebate example 2a`` () =
            let principal = 10000_00L<Cent>
            let payments = [| 1 .. 180 |] |> Array.map(fun i -> i, 139_51L<Cent>)
            let apr = Percent 16m
            let settlementPeriod = 73
            let settlementPartPeriod = Fraction.Simple (28, 30)
            let unitPeriod = UnitPeriod.Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero
            let actual = calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding
            let expected = 6606_95L<Cent>
            actual |> should equal expected

        let scheduleParameters2 =
            {
                StartDate = Date(2010, 3, 1)
                AsOfDate = Date(2011, 3, 1)
                Principal = 5000_00L<Cent>
                ScheduleConfig = FixedSchedules [| { UnitPeriodConfig = UnitPeriod.Monthly(1, 2010, 4, 1); PaymentCount = 48; PaymentValue = 134_57L<Cent>; ScheduleType = ScheduleType.Original } |]
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
                    Method = Method.Simple
                    StandardRate = Rate.Annual <| Percent 13.1475m
                    Cap = { TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown); DailyAmount = ValueSome <| Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding) }
                    InitialGracePeriod = 0<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Annual (Percent 8m)
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    InterestRounding = RoundDown
                }
            }

        [<Fact>]
        let ``3) Initial statement (simple interest) matching total interest amount of £1459.36`` () =
            let sp = { scheduleParameters2 with AsOfDate = Date(2010, 3, 1) }

            let actualPayments = Map.empty

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement true

            schedule.ScheduleItems |> outputMapToHtml "out/Cca2004Test003.md" false

            let levelPayment = schedule.ScheduleItems[1433<OffsetDay>].ScheduledPayment |> ScheduledPayment.total
            let finalPayment = schedule.ScheduleItems[1461<OffsetDay>].ScheduledPayment |> ScheduledPayment.total
            let interestPortion = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion
            [ levelPayment; finalPayment; interestPortion ] |> should equal [ 134_57L<Cent>; 134_57L<Cent>; 1459_36L<Cent> ]

        [<Fact>]
        let ``3a) Initial statement (simple interest, autogenerated payment amounts) matching level payment of £134.57`` () =
            let sp = { scheduleParameters2 with AsOfDate = Date(2010, 3, 1); ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Monthly(1, 2010, 4, 1); PaymentCount = 48; MaxDuration = Duration.Unlimited } }

            let actualPayments = Map.empty

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement true

            schedule.ScheduleItems |> outputMapToHtml "out/Cca2004Test003a.md" false

            let levelPayment = schedule.ScheduleItems[1433<OffsetDay>].ScheduledPayment |> ScheduledPayment.total
            let finalPayment = schedule.ScheduleItems[1461<OffsetDay>].ScheduledPayment |> ScheduledPayment.total
            let interestPortion = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion
            [ levelPayment; finalPayment; interestPortion ] |> should equal [ 134_57L<Cent>; 134_57L<Cent>; 1459_36L<Cent> ]

        [<Fact>]
        let ``3b) CCA 2004 rebate example using library method (simple interest)`` () =
            let sp = scheduleParameters2

            let actualPayments =
                Map [
                    31<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    61<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    92<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    122<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    153<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    184<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    214<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    245<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    275<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    306<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    337<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                    365<OffsetDay>, [| ActualPayment.quickConfirmed 134_57L<Cent> |]
                ]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.SettlementOnAsOfDay true

            schedule.ScheduleItems |> outputMapToHtml "out/Cca2004Test003b.md" false

            let interestPortion = schedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion

            interestPortion |> should equal 598_08L<Cent>
