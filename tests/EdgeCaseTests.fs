namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EdgeCaseTests =

    let folder = "EdgeCase"

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling
    open Quotes
    open Rescheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit))
    }

    [<Fact>]
    let EdgeCaseTest000 () =
        let title = "EdgeCaseTest000"
        let description = "Quote returning nothing"
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2023, 2, 9)
            Principal = 30000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                15<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                43<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                74<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
                104<OffsetDay>, ScheduledPayment.quick (ValueSome 137_40L<Cent>) ValueNone
            ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = Some {
                FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit))
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRebate = Fee.SettlementRebate.ProRata
            }
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 31200L<Cent> |]
        ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 500_79L<Cent>
                Apportionment = {
                    PrincipalPortion = 177_81L<Cent>
                    FeePortion = 274_64L<Cent>
                    InterestPortion = 48_34L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest001 () =
        let title = "EdgeCaseTest001"
        let description = "Quote returning nothing"
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                16<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                44<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                75<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
                105<OffsetDay>, ScheduledPayment.quick (ValueSome 11500L<Cent>) ValueNone
            ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = Some {
                FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit))
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRebate = Fee.SettlementRebate.ProRata
            }
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 26000L<Cent> |]
        ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 455_55L<Cent>
                Apportionment = {
                    PrincipalPortion = 148_17L<Cent>
                    FeePortion = 228_86L<Cent>
                    InterestPortion = 78_52L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest002 () =
        let title = "EdgeCaseTest002"
        let description = "Quote returning nothing"
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 12, 2)
            Principal = 75000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                14<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                45<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                76<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
                104<OffsetDay>, ScheduledPayment.quick (ValueSome 34350L<Cent>) ValueNone
            ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = Some {
                FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit))
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRebate = Fee.SettlementRebate.ProRata
            }
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            13<OffsetDay>, [| ActualPayment.quickConfirmed 82800L<Cent> |]
        ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 1221_54L<Cent>
                Apportionment = {
                    PrincipalPortion = 427_28L<Cent>
                    FeePortion = 660_00L<Cent>
                    InterestPortion = 134_26L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest003 () =
        let title = "EdgeCaseTest003"
        let description = "Quote returning nothing"
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2020, 10, 8)
            Principal = 50000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                8<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                39<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                69<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                100<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                214<OffsetDay>, ScheduledPayment.quick (ValueSome 25000L<Cent>) ValueNone
                245<OffsetDay>, ScheduledPayment.quick (ValueSome 27600L<Cent>) ValueNone
            ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = Some {
                FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit))
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRebate = Fee.SettlementRebate.ProRata
            }
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                Rounding = RoundDown
            }
        }

        let actualPayments = Map.ofArrayWithMerge [|
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            72<OffsetDay>, [| ActualPayment.quickConfirmed 22500L<Cent> |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> ValueNone |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> ValueNone |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> ValueNone |]
            106<OffsetDay>, [| ActualPayment.quickConfirmed 24900L<Cent> |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> ValueNone |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> ValueNone |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> ValueNone |]
            380<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            407<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            475<OffsetDay>, [| ActualPayment.quickConfirmed 17600L<Cent> |]
        |]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 466_41L<Cent>
                Apportionment = {
                    PrincipalPortion = 151_32L<Cent>
                    FeePortion = 233_66L<Cent>
                    InterestPortion = 81_43L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``EdgeCaseTest004`` () =
        let title = "EdgeCaseTest004"
        let description = "Only one insufficient funds charge per day"
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2020, 10, 8)
            Principal = 50000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                8<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                39<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                69<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                100<OffsetDay>, ScheduledPayment.quick (ValueSome 22500L<Cent>) ValueNone
                214<OffsetDay>, ScheduledPayment.quick (ValueSome 25000L<Cent>) ValueNone
                245<OffsetDay>, ScheduledPayment.quick (ValueSome 27600L<Cent>) ValueNone
            ]
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = Some {
                FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit))
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRebate = Fee.SettlementRebate.ProRata
            }
            ChargeConfig =
                Some {
                    ChargeTypes = Map [
                        Charge.InsufficientFunds, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                            ChargeHolidays = [||]
                        }
                    ]
                }
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.Zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                Rounding = RoundDown
            }
        }

        let actualPayments = Map.ofArrayWithMerge [|
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds) |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds) |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> (ValueSome Charge.InsufficientFunds) |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            72<OffsetDay>, [| ActualPayment.quickConfirmed 22500L<Cent> |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> ValueNone |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> ValueNone |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> ValueNone |]
            106<OffsetDay>, [| ActualPayment.quickConfirmed 24900L<Cent> |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> ValueNone |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> ValueNone |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> ValueNone |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> ValueNone |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> ValueNone |]
            380<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            407<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> ValueNone |]
            475<OffsetDay>, [| ActualPayment.quickConfirmed 17600L<Cent> |]
        |]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 479_92L<Cent>
                Apportionment = {
                    PrincipalPortion = 155_70L<Cent>
                    FeePortion = 240_43L<Cent>
                    InterestPortion = 83_79L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest005 () =
        let title = "EdgeCaseTest005"
        let description = "Quote returning nothing"
        let sp = {

            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 6, 22)
            Principal = 500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2022, 7, 15); PaymentCount = 6; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                Rounding = RoundDown
            }
        }

        let actualPayments = Map.ofArrayWithMerge [|
            23<OffsetDay>, [| ActualPayment.quickFailed 166_67L<Cent> ValueNone |]
            23<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            23<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            26<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            26<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            26<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            29<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            29<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            29<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            54<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            54<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            54<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            57<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            57<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            57<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            60<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            60<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            60<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            85<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            85<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            85<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            88<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            88<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            88<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            91<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            91<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            91<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> ValueNone |]
            135<OffsetDay>, [| ActualPayment.quickConfirmed 83_33L<Cent> |]
            165<OffsetDay>, [| ActualPayment.quickConfirmed 83_33L<Cent> |]
            196<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            196<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            196<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            199<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            199<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            199<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            202<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            202<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            202<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            227<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            227<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            227<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            230<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            230<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            230<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            233<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            233<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            233<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            255<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            255<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            255<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            258<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            258<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            258<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            261<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            261<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            261<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            286<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            286<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            286<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            289<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            289<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            289<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            292<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            292<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            292<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> ValueNone |]
            322<OffsetDay>, [| ActualPayment.quickConfirmed 17_58L<Cent> |]
            353<OffsetDay>, [| ActualPayment.quickConfirmed 17_58L<Cent> |]
            384<OffsetDay>, [| ActualPayment.quickConfirmed 17_58L<Cent> |]
            408<OffsetDay>, [| ActualPayment.quickConfirmed 17_58L<Cent> |]
            449<OffsetDay>, [| ActualPayment.quickConfirmed 17_58L<Cent> |]
            476<OffsetDay>, [| ActualPayment.quickConfirmed 17_58L<Cent> |]
            499<OffsetDay>, [| ActualPayment.quickConfirmed 15_74L<Cent> |]
            531<OffsetDay>, [| ActualPayment.quickConfirmed 15_74L<Cent> |]
            574<OffsetDay>, [| ActualPayment.quickConfirmed 15_74L<Cent> |]
            595<OffsetDay>, [| ActualPayment.quickConfirmed 15_74L<Cent> |]
            629<OffsetDay>, [| ActualPayment.quickConfirmed 15_74L<Cent> |]
        |]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 64916L<Cent>
                Apportionment = {
                    PrincipalPortion = 500_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 149_16L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest006 () =
        let title = "EdgeCaseTest006"
        let description = "Quote returning nothing"
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2021, 12, 26)
            Principal = 150000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2022, 1, 7); PaymentCount = 6; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            12<OffsetDay>, [| ActualPayment.quickFailed 500_00L<Cent> ValueNone; ActualPayment.quickFailed 500_00L<Cent> ValueNone |]
            15<OffsetDay>, [| ActualPayment.quickFailed 500_00L<Cent> ValueNone; ActualPayment.quickFailed 500_00L<Cent> ValueNone; ActualPayment.quickConfirmed 500_00L<Cent> |]
            43<OffsetDay>, [| ActualPayment.quickFailed 500_00L<Cent> ValueNone; ActualPayment.quickFailed 500_00L<Cent> ValueNone |]
            45<OffsetDay>, [| ActualPayment.quickConfirmed 1540_00L<Cent> |]
        ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description sp
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = -76_80L<Cent>
                Apportionment = {
                    PrincipalPortion = -76_80L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected


    [<Fact>]
    let EdgeCaseTest007 () =
        let title = "EdgeCaseTest007"
        let description = "Quote returning nothing"
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            6<OffsetDay>, [| ActualPayment.quickFailed 2_00L<Cent> ValueNone |]
            16<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent>; ActualPayment.quickConfirmed 97_01L<Cent> |]
        ]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let rescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate

        let (rp: RescheduleParameters) = {
            FeeettlementRebate = Fee.SettlementRebate.ProRataRescheduled originalFinalPaymentDay
            PaymentSchedule = CustomSchedule <| Map [
                58<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = rescheduleDay })
            ]
            RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
            PromotionalInterestRates = [||]
            SettlementDay = ValueSome <| SettlementDay.SettlementOn 88<OffsetDay>
        }

        let schedules = reschedule sp rp actualPayments

        schedules.NewSchedules |> Schedule.outputHtmlToFile folder title description sp

        let actual = schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected = 88<OffsetDay>, {
            OffsetDate = Date(2024, 4, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.zero
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = GeneratedValue 83_74L<Cent>
            NetEffect = 83_74L<Cent>
            PaymentStatus = Generated
            BalanceStatus = ClosedBalance
            SimpleInterest = 4_32.256m<Cent>
            NewInterest = 4_32.256m<Cent>
            NewCharges = [||]
            PrincipalPortion = 67_54L<Cent>
            FeePortion = 0L<Cent>
            InterestPortion = 16_20L<Cent>
            ChargesPortion = 0L<Cent>
            FeeRebate = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 0L<Cent>
            FeeRebateIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest008 () =
        let title = "EdgeCaseTest008"
        let description = "Partial write-off"
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            6<OffsetDay>, [| ActualPayment.quickWriteOff 42_00L<Cent> |]
            16<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent>; ActualPayment.quickConfirmed 97_01L<Cent> |]
        ]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let rescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate

        let (rp: RescheduleParameters) = {
            FeeettlementRebate = Fee.SettlementRebate.ProRataRescheduled originalFinalPaymentDay
            PaymentSchedule = CustomSchedule <| Map [
                58<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = rescheduleDay })
            ]
            RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
            PromotionalInterestRates = [||]
            SettlementDay = ValueSome <| SettlementDay.SettlementOn 88<OffsetDay>
        }

        let schedules = reschedule sp rp actualPayments

        schedules.NewSchedules |> Schedule.outputHtmlToFile folder title description sp

        let actual = schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected = 88<OffsetDay>, {
            OffsetDate = Date(2024, 4, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.zero
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = GeneratedValue 10_19L<Cent>
            NetEffect = 10_19L<Cent>
            PaymentStatus = Generated
            BalanceStatus = ClosedBalance
            SimpleInterest = 52.608m<Cent>
            NewInterest = 52.608m<Cent>
            NewCharges = [||]
            PrincipalPortion = 8_22L<Cent>
            FeePortion = 0L<Cent>
            InterestPortion = 1_97L<Cent>
            ChargesPortion = 0L<Cent>
            FeeRebate = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome 0L<Cent>
            FeeRebateIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let EdgeCaseTest009 () =
        let title = "EdgeCaseTest009"
        let description = "Negative principal balance accruing interest"
        let sp = {
            AsOfDate = Date(2024, 4, 5)
            StartDate = Date(2023, 5, 5)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 5, 10); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                Rounding = RoundUp
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
            }
            FeeConfig = None
            ChargeConfig = None
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                Rounding = RoundDown
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 111_00L<Cent> |]
            21<OffsetDay>, [| ActualPayment.quickConfirmed 181_01L<Cent> |]
        ]

        let schedules =
            actualPayments
            |> Amortisation.generate sp ValueNone false

        Schedule.outputHtmlToFile folder title description sp schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected = 97<OffsetDay>, {
            OffsetDate = Date(2023, 8, 10)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 87_67L<Cent>) ValueNone
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 0L<Cent>
            PaymentStatus = NoLongerRequired
            BalanceStatus = RebateDue
            SimpleInterest = -8.79210959m<Cent>
            NewInterest = -8.79210959m<Cent>
            NewCharges = [||]
            PrincipalPortion = 0L<Cent>
            FeePortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeeRebate = 0L<Cent>
            PrincipalBalance = -12_94L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = -21.55484933m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = ValueSome -13_16L<Cent>
            FeeRebateIfSettled = 0L<Cent>
        }

        actual |> should equal expected
