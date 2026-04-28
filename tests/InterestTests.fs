namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestTests =

    let folder = "Interest"

    open System

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Interest
    open Scheduling
    open UnitPeriod

    let interestCapExample: Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
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
        let ``Trying to add €150 interest to a €75 cumulative interest total with no cap on a principal of €200 allows the full €150``
            ()
            =
            let actual =
                Cap.cappedAddedValue Cap.zero.TotalAmount 200_00L<Cent> 75_00m<Cent> 150_00m<Cent>

            let expected = 150_00m<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``Trying to add €150 interest to a €75 cumulative interest total with a 100% total cap on a principal of €200 only allows €125``
            ()
            =
            let actual =
                Cap.cappedAddedValue interestCapExample.TotalAmount 200_00L<Cent> 75_00m<Cent> 150_00m<Cent>

            let expected = 125_00m<Cent>
            actual |> should equal expected

        let parameters1: Parameters = {
            Basic = {
                EvaluationDate = Date(2024, 4, 25)
                StartDate = Date(2023, 2, 9)
                Principal = 499_00L<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2023, 2, 14)
                        ScheduleLength = PaymentCount 4
                        RepaymentType = RepaymentType.CapitalAndInterest
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Method.Actuarial
                    StandardRate = Rate.Daily(Percent 0.8m)
                    Cap = interestCapExample
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RateSchedule = [||]
                    Rounding = RoundDown
                }
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Zero
                }
                SettlementDay = SettlementDay.SettlementOnEvaluationDay
                TrimEnd = false
            }
        }

        [<Fact>]
        let InterestCapTest000 () =
            let title = "InterestCapTest000"

            let description =
                "Total interest in amortised schedule does not exceed interest cap"

            let actualPayments = Map.empty

            let schedules = amortise parameters1 actualPayments

            Schedule.outputHtmlToFile folder title description parameters1 "" schedules

            let interestPortion =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.maxKeyValue
                |> snd
                |> _.InterestPortion

            interestPortion |> should be (lessThanOrEqualTo 499_00L<Cent>)

        [<Fact>]
        let InterestCapTest001 () =
            let title = "InterestCapTest001"

            let description =
                "Total interest in amortised schedule does not exceed interest cap, using unrounded percentages"

            let p = {
                parameters1 with
                    Basic.InterestConfig.StandardRate = Rate.Daily(Percent 0.876m)
                    Basic.InterestConfig.Cap = {
                        interestCapExample with
                            TotalAmount = Amount.Percentage(Percent 123.45m, Restriction.NoLimit)
                    }
            }

            let actualPayments = Map.empty

            let schedules = amortise p actualPayments

            Schedule.outputHtmlToFile folder title description p "" schedules

            let interestPortion =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.maxKeyValue
                |> snd
                |> _.InterestPortion

            interestPortion |> should be (lessThanOrEqualTo 616_01L<Cent>)

    module DailyRatesTests =

        [<Fact>]
        let ``Daily rates with no settlement inside the grace period or promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [||]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate false standardRate [||] promotionalRates fromDay toDay

            let expected =
                [| 1..10 |]
                |> Array.map (fun d -> {
                    RateDay = d * 1<OffsetDay>
                    InterestRate = Rate.Annual <| Percent 10m
                })

            actual |> should equal expected

        [<Fact>]
        let ``Daily rates with a settlement inside the grace period, but no promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [||]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate true standardRate [||] promotionalRates fromDay toDay

            let expected =
                [| 1..10 |]
                |> Array.map (fun d -> {
                    RateDay = d * 1<OffsetDay>
                    InterestRate = Rate.Zero
                })

            actual |> should equal expected

        [<Fact>]
        let ``Daily rates with no settlement inside the grace period but with promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m

            let promotionalRates = [|
                ({
                    DateRange = {
                        DateRangeStart = Date(2024, 4, 10)
                        DateRangeEnd = Date(2024, 4, 15)
                    }
                    Rate = Rate.Annual <| Percent 2m
                }
                : PromotionalRate)
            |]

            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate false standardRate [||] promotionalRates fromDay toDay

            let expected =
                [|
                    [| 1..5 |]
                    |> Array.map (fun d -> {
                        RateDay = d * 1<OffsetDay>
                        InterestRate = Rate.Annual <| Percent 2m
                    })
                    [| 6..10 |]
                    |> Array.map (fun d -> {
                        RateDay = d * 1<OffsetDay>
                        InterestRate = Rate.Annual <| Percent 10m
                    })
                |]
                |> Array.concat

            actual |> should equal expected

            let expected =
                [|
                    [| 1..5 |]
                    |> Array.map (fun d -> {
                        RateDay = d * 1<OffsetDay>
                        InterestRate = Rate.Annual <| Percent 2m
                    })
                    [| 6..10 |]
                    |> Array.map (fun d -> {
                        RateDay = d * 1<OffsetDay>
                        InterestRate = Rate.Annual <| Percent 10m
                    })
                |]
                |> Array.concat

            actual |> should equal expected

    module Cca2004Tests =

        [<Fact>]
        let Cca2004Test000 () =
            let title = "Cca2004Test000"
            let description = "UK rebate example 1"
            let principal = 5000_00L<Cent>
            let payments = [| 1..48 |] |> Array.map (fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 12
            let settlementPartPeriod = Fraction.Zero
            let unitPeriod = Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero

            let actual =
                calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding

            let expected = 860_52L<Cent>
            actual |> should equal expected

        [<Fact>]
        let Cca2004Test001 () =
            let title = "Cca2004Test001"
            let description = "UK rebate example 1a"
            let principal = 5000_00L<Cent>
            let payments = [| 1..48 |] |> Array.map (fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 12
            let settlementPartPeriod = Fraction.Simple(28, 30)
            let unitPeriod = Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero

            let actual =
                calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding

            let expected = 819_71L<Cent>
            actual |> should equal expected

        [<Fact>]
        let Cca2004Test002 () =
            let title = "Cca2004Test002"
            let description = "UK rebate example 1b"
            let principal = 5000_00L<Cent>
            let payments = [| 1..48 |] |> Array.map (fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 12
            let settlementPartPeriod = Fraction.Simple(28, 31)
            let unitPeriod = Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero

            let actual =
                calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding

            let expected = 821_03L<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``Cca2004Test003`` () =
            let title = "Cca2004Test003"
            let description = "UK rebate example 1c"
            let principal = 5000_00L<Cent>
            let payments = [| 1..48 |] |> Array.map (fun i -> i, 134_57L<Cent>)
            let apr = Percent 14m
            let settlementPeriod = 13
            let settlementPartPeriod = Fraction.Simple(28, 30)
            let unitPeriod = Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero

            let actual =
                calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding

            let expected = 776_90L<Cent>
            actual |> should equal expected

        [<Fact>]
        let Cca2004Test004 () =
            let title = "Cca2004Test004"
            let description = "UK rebate example 2"
            let principal = 10000_00L<Cent>
            let payments = [| 1..180 |] |> Array.map (fun i -> i, 139_51L<Cent>)
            let apr = Percent 16m
            let settlementPeriod = 73
            let settlementPartPeriod = Fraction.Zero
            let unitPeriod = Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero

            let actual =
                calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding

            let expected = 6702_45L<Cent>
            actual |> should equal expected

        [<Fact>]
        let Cca2004Test005 () =
            let title = "Cca2004Test005"
            let description = "UK rebate example 2a"
            let principal = 10000_00L<Cent>
            let payments = [| 1..180 |] |> Array.map (fun i -> i, 139_51L<Cent>)
            let apr = Percent 16m
            let settlementPeriod = 73
            let settlementPartPeriod = Fraction.Simple(28, 30)
            let unitPeriod = Month 1
            let paymentRounding = RoundWith MidpointRounding.AwayFromZero

            let actual =
                calculateRebate principal payments apr settlementPeriod settlementPartPeriod unitPeriod paymentRounding

            let expected = 6606_95L<Cent>
            actual |> should equal expected

        let parameters2: Parameters = {
            Basic = {
                StartDate = Date(2010, 3, 1)
                EvaluationDate = Date(2011, 3, 1)
                Principal = 5000_00L<Cent>
                ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2010, 4, 1)
                            PaymentCount = 48
                            PaymentValue = 134_57L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Method.Actuarial
                    StandardRate = Rate.Annual <| Percent 13.1475m
                    Cap = {
                        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
                    }
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RateSchedule = [||]
                    Rounding = RoundDown
                }
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 0<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Annual <| Percent 8m
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = true
            }
        }

        [<Fact>]
        let Cca2004Test006 () =
            let title = "Cca2004Test006"

            let description =
                "Initial statement (actuarial interest) matching total interest amount of £1459.36"

            let p = {
                parameters2 with
                    Basic.EvaluationDate = Date(2010, 3, 1)
            }

            let actualPayments = Map.empty

            let schedules = amortise p actualPayments

            Schedule.outputHtmlToFile folder title description p "" schedules

            let levelPayment =
                schedules.AmortisationSchedule.ScheduleItems[1433<OffsetDay>].ScheduledPayment
                |> ScheduledPayment.total

            let finalPayment =
                schedules.AmortisationSchedule.ScheduleItems[1461<OffsetDay>].ScheduledPayment
                |> ScheduledPayment.total

            let interestPortion =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion

            [ levelPayment; finalPayment; interestPortion ]
            |> should equal [ 134_57L<Cent>; 134_57L<Cent>; 1459_36L<Cent> ]

        [<Fact>]
        let Cca2004Test007 () =
            let title = "Cca2004Test007"

            let description =
                "Initial statement (actuarial interest, autogenerated payment amounts) matching level payment of £134.57"

            let p = {
                parameters2 with
                    Basic.EvaluationDate = Date(2010, 3, 1)
                    Basic.ScheduleConfig =
                        AutoGenerateSchedule {
                            UnitPeriodConfig = Monthly(1, 2010, 4, 1)
                            ScheduleLength = PaymentCount 48
                            RepaymentType = RepaymentType.CapitalAndInterest
                        }
            }

            let actualPayments = Map.empty

            let schedules = amortise p actualPayments

            Schedule.outputHtmlToFile folder title description p "" schedules

            let levelPayment =
                schedules.AmortisationSchedule.ScheduleItems[1433<OffsetDay>].ScheduledPayment
                |> ScheduledPayment.total

            let finalPayment =
                schedules.AmortisationSchedule.ScheduleItems[1461<OffsetDay>].ScheduledPayment
                |> ScheduledPayment.total

            let interestPortion =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion

            [ levelPayment; finalPayment; interestPortion ]
            |> should equal [ 134_57L<Cent>; 134_57L<Cent>; 1459_36L<Cent> ]

        [<Fact>]
        let Cca2004Test008 () =
            let title = "Cca2004Test008"

            let description =
                "CCA 2004 rebate example using library method (actuarial interest)"

            let p = {
                parameters2 with
                    Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
            }

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

            let schedules = amortise p actualPayments

            Schedule.outputHtmlToFile folder title description p "" schedules

            let interestPortion =
                schedules.AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion

            interestPortion |> should equal 598_08L<Cent>

    module RateScheduleTests =

        let startDate = Date(2024, 1, 1)

        let baseParameters: Parameters = {
            Basic = {
                EvaluationDate = startDate.AddMonths 12
                StartDate = startDate
                Principal = 100_000_00L<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2024, 2, 1)
                        ScheduleLength = PaymentCount 12
                        RepaymentType = RepaymentType.CapitalAndInterest
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Method.Actuarial
                    StandardRate = Rate.Annual <| Percent 5m
                    Cap = Cap.zero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RateSchedule = [||]
                    Rounding = RoundDown
                }
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = NoMinimumPayment
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 0<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = true
            }
        }

        [<Fact>]
        let ``Rate schedule with no entries behaves identically to standard rate`` () =
            let p1 = baseParameters
            let p2 = {
                baseParameters with
                    Basic.InterestConfig.RateSchedule = [||]
            }
            let schedules1 = amortise p1 Map.empty
            let schedules2 = amortise p2 Map.empty
            let totalInterest1 =
                schedules1.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion
            let totalInterest2 =
                schedules2.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.sumBy _.InterestPortion
            totalInterest1 |> should equal totalInterest2

        [<Fact>]
        let ``Rate schedule stepping up mid-term increases total interest compared to constant lower rate`` () =
            // baseline: 5% p.a. for the full term
            let pBaseline = baseParameters
            // stepped: 5% for 6 months, then 8% for the remaining 6 months
            let pStepped = {
                baseParameters with
                    Basic.InterestConfig.RateSchedule = [|
                        startDate.AddMonths 6, Rate.Annual(Percent 8m)
                    |]
            }
            let totalInterestBaseline =
                (amortise pBaseline Map.empty).AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion
            let totalInterestStepped =
                (amortise pStepped Map.empty).AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion
            totalInterestStepped |> should be (greaterThan totalInterestBaseline)

        [<Fact>]
        let ``Rate schedule stepping down mid-term decreases total interest compared to constant higher rate`` () =
            // baseline: 8% p.a. for the full term
            let pBaseline = {
                baseParameters with
                    Basic.InterestConfig.StandardRate = Rate.Annual(Percent 8m)
            }
            // stepped: 8% for 6 months, then 5% for the remaining 6 months
            let pStepped = {
                baseParameters with
                    Basic.InterestConfig.StandardRate = Rate.Annual(Percent 8m)
                    Basic.InterestConfig.RateSchedule = [|
                        startDate.AddMonths 6, Rate.Annual(Percent 5m)
                    |]
            }
            let totalInterestBaseline =
                (amortise pBaseline Map.empty).AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion
            let totalInterestStepped =
                (amortise pStepped Map.empty).AmortisationSchedule.ScheduleItems
                |> Map.values
                |> Seq.sumBy _.InterestPortion
            totalInterestStepped |> should be (lessThan totalInterestBaseline)

        [<Fact>]
        let ``dailyRates uses RateSchedule.effectiveRate correctly`` () =
            let rateSchedule: RateSchedule = [|
                Date(2024, 7, 1), Rate.Annual(Percent 8m)
            |]
            // day 1 (Jan 2) is before the effective date, should use standard rate (5%)
            let day1 = dailyRates startDate false (Rate.Annual(Percent 5m)) rateSchedule [||] 0<OffsetDay> 1<OffsetDay>
            // day 183 (July 1) is on the effective date, should use stepped rate (8%)
            let day183 = dailyRates startDate false (Rate.Annual(Percent 5m)) rateSchedule [||] 182<OffsetDay> 183<OffsetDay>
            // use daily rate to verify (avoids DU reflection issues in test framework)
            let toDailyPercent r = r |> Rate.daily |> Percent.toDecimal
            toDailyPercent day1[0].InterestRate |> should equal (Percent.toDecimal (Rate.daily (Rate.Annual(Percent 5m))))
            toDailyPercent day183[0].InterestRate |> should equal (Percent.toDecimal (Rate.daily (Rate.Annual(Percent 8m))))

    module RepaymentTypeTests =

        let startDate = Date(2024, 1, 1)

        let baseInterestConfig = {
            Method = Method.Actuarial
            StandardRate = Rate.Annual <| Percent 5m
            Cap = Cap.zero
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            RateSchedule = [||]
            Rounding = RoundDown
        }

        let makeParameters repaymentType : Parameters = {
            Basic = {
                EvaluationDate = startDate.AddMonths 12
                StartDate = startDate
                Principal = 100_000_00L<Cent>
                ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2024, 2, 1)
                        ScheduleLength = PaymentCount 12
                        RepaymentType = repaymentType
                    }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = baseInterestConfig
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = NoMinimumPayment
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 0<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = true
            }
        }

        [<Fact>]
        let ``InterestOnly: all period payments are interest only; final payment settles principal`` () =
            let p = makeParameters RepaymentType.InterestOnly
            let basicItems = (amortise p Map.empty).BasicSchedule.Items
            // skip first item (day 0 initial state)
            let paymentItems = basicItems |> Array.tail
            let allButLast = paymentItems |> Array.take (paymentItems.Length - 1)
            let lastItem = paymentItems |> Array.last
            // all payments except the last should have zero principal portion (interest-only)
            allButLast |> Array.forall (fun si -> si.PrincipalPortion = 0L<Cent>) |> should equal true
            // the final payment should settle the principal
            lastItem.PrincipalBalance |> should equal 0L<Cent>

        [<Fact>]
        let ``InterestOnly: total interest is higher than CapitalAndInterest`` () =
            let pIO = makeParameters RepaymentType.InterestOnly
            let pCI = makeParameters RepaymentType.CapitalAndInterest
            let totalInterestIO = (amortise pIO Map.empty).BasicSchedule.Stats.InterestTotal
            let totalInterestCI = (amortise pCI Map.empty).BasicSchedule.Stats.InterestTotal
            // Interest-only always accumulates more interest (principal is not reduced until the end)
            totalInterestIO |> should be (greaterThan totalInterestCI)

        [<Fact>]
        let ``Mixed: first N payments are interest only, remaining payments amortise the principal`` () =
            let interestOnlyPeriods = 6
            let p = makeParameters (RepaymentType.Mixed interestOnlyPeriods)
            let basicItems = (amortise p Map.empty).BasicSchedule.Items |> Array.tail
            let ioPhase = basicItems |> Array.take interestOnlyPeriods
            let ciPhase = basicItems |> Array.skip interestOnlyPeriods
            // first 6 payment items should have zero principal portion (interest-only)
            ioPhase |> Array.forall (fun si -> si.PrincipalPortion = 0L<Cent>) |> should equal true
            // remaining payment items should reduce the principal
            ciPhase |> Array.forall (fun si -> si.PrincipalPortion > 0L<Cent>) |> should equal true
            // the schedule should be fully settled
            (basicItems |> Array.last).PrincipalBalance |> should equal 0L<Cent>

        [<Fact>]
        let ``Mixed: total interest is between InterestOnly and CapitalAndInterest`` () =
            let pCI = makeParameters RepaymentType.CapitalAndInterest
            let pMixed = makeParameters (RepaymentType.Mixed 6)
            let pIO = makeParameters RepaymentType.InterestOnly
            let totalInterest p = (amortise p Map.empty).BasicSchedule.Stats.InterestTotal
            let interestCI = totalInterest pCI
            let interestMixed = totalInterest pMixed
            let interestIO = totalInterest pIO
            interestMixed |> should be (greaterThan interestCI)
            interestMixed |> should be (lessThan interestIO)

        [<Fact>]
        let ``Mixed with zero interest-only periods behaves identically to CapitalAndInterest`` () =
            let pCI = makeParameters RepaymentType.CapitalAndInterest
            let pMixed0 = makeParameters (RepaymentType.Mixed 0)
            let totalInterestCI = (amortise pCI Map.empty).BasicSchedule.Stats.InterestTotal
            let totalInterestMixed0 = (amortise pMixed0 Map.empty).BasicSchedule.Stats.InterestTotal
            totalInterestCI |> should equal totalInterestMixed0
