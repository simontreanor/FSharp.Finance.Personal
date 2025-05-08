namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PromotionalRatesTests =

    let folder = "PromotionalRates"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    let interestCapExample : Interest.Cap = {
        TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
    }

    let startDate = Date(2024, 8, 23)
    let parameters : Parameters =
        {
            Basic = {
                EvaluationDate = startDate.AddDays 180
                StartDate = startDate
                Principal = 400_00L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2024, 9, 2)
                    ScheduleLength = PaymentCount 4
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Daily <| Percent 0.8m
                    Cap = {
                        TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
                        DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
                    }
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
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
                    RateOnNegativeBalance = Interest.Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = true
            }
        }

    [<Fact>]
    let PromotionalRatesTest000 () =
        let title = "PromotionalRatesTest000"
        let description = "Baseline with no promotional rates"

        let actualPayments = Map.empty

        let schedules = amortise parameters actualPayments

        Schedule.outputHtmlToFile folder title description parameters schedules

        let interestBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance
        interestBalance |> should equal 323_20m<Cent>

    [<Fact>]
    let PromotionalRatesTest001 () =
        let title = "PromotionalRatesTest001"
        let description = "Interest-free October should reduce total interest by 31 days"

        let p =
            { parameters with
                Advanced.InterestConfig.PromotionalRates = [|
                    { DateRange = { Start = Date(2024, 10, 1); End = Date(2024, 10, 31) }; Rate = Interest.Rate.Zero }
                |]
            }

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p schedules

        let interestBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance
        interestBalance |> should equal 224_00m<Cent>

    [<Fact>]
    let PromotionalRatesTest002 () =
        let title = "PromotionalRatesTest002"
        let description = "Low-interest December should reduce all interest during December"

        let p =
            { parameters with
                Advanced.InterestConfig.PromotionalRates = [|
                    { DateRange = { Start = Date(2024, 12, 1); End = Date(2024, 12, 31) }; Rate = Interest.Rate.Annual <| Percent 20.24m }
                |]
            }

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p schedules

        let interestBalance = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue |> snd |> _.InterestBalance
        interestBalance |> should equal 317_24.36164383m<Cent>

    [<Fact>]
    let PromotionalRatesTest004 () =
        let title = "PromotionalRatesTest004"
        let description = "Mortgage quote with a five-year fixed interest deal and a mortgage fee added to the loan"
        let p : Parameters = {
            Basic = {
                EvaluationDate = Date(2024, 4, 11)
                StartDate = Date(2024, 4, 11)
                Principal = 192_000_00L<Cent>
                ScheduleConfig = FixedSchedules [|
                    { UnitPeriodConfig = Monthly(1, 2024, 5, 11); PaymentCount = 60; PaymentValue = 1225_86L<Cent>; ScheduleType = ScheduleType.Original }
                    { UnitPeriodConfig = Monthly(1, 2029, 5, 11); PaymentCount = 180; PaymentValue = 1525_12L<Cent>; ScheduleType = ScheduleType.Original }
                |]
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueSome {
                    FeeType = Fee.FeeType.MortageFee <| Amount.Simple 999_00L<Cent>
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseBeforePrincipal
                }
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Annual <| Percent 7.985m
                    Cap = Interest.Cap.Zero
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    Rounding = RoundDown
                }
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = NoMinimumPayment
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueSome {
                    SettlementRebate = Fee.SettlementRebate.Zero
                }
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [|
                        { DateRange = { Start = Date(2024, 4, 11); End = Date(2029, 4, 10) }; Rate = Interest.Rate.Annual <| Percent 4.535m }
                    |]
                    RateOnNegativeBalance = Interest.Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = false
            }
        }

        let actualPayments = Map.empty

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

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
            ActuarialInterest = 10_26.07665657m<Cent>
            NewInterest = 10_26.07665657m<Cent>
            NewCharges = [||]
            PrincipalPortion = 1512_99L<Cent>
            FeePortion = 0L<Cent>
            InterestPortion = 10_26L<Cent>
            ChargesPortion = 0L<Cent>
            FeeRebate = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeeRebateIfSettled = 0L<Cent>
        }

        actual |> should equal expected
