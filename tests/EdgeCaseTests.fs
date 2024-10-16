namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EdgeCaseTests =

    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open Formatting
    open MapExtension
    open PaymentSchedule
    open Util
    open Quotes
    open Rescheduling

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding))
    }

    [<Fact>]
    let ``1) Quote returning nothing`` () =
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 31200L<Cent> |]
        ]

        let actual =
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest001.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 500_79L<Cent>
                Apportionment = {
                    PrincipalPortion = 177_81L<Cent>
                    FeesPortion = 274_64L<Cent>
                    InterestPortion = 48_34L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``2) Quote returning nothing`` () =
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 26000L<Cent> |]
        ]

        let actual =
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest002.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 455_55L<Cent>
                Apportionment = {
                    PrincipalPortion = 148_17L<Cent>
                    FeesPortion = 228_86L<Cent>
                    InterestPortion = 78_52L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``3) Quote returning nothing`` () =
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            13<OffsetDay>, [| ActualPayment.quickConfirmed 82800L<Cent> |]
        ]

        let actual =
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest003.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 1221_54L<Cent>
                Apportionment = {
                    PrincipalPortion = 427_28L<Cent>
                    FeesPortion = 660_00L<Cent>
                    InterestPortion = 134_26L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``4) Quote returning nothing`` () =
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map.ofArrayWithMerge [|
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.quickConfirmed 22500L<Cent> |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.quickConfirmed 24900L<Cent> |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> [||] |]
            380<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            407<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            475<OffsetDay>, [| ActualPayment.quickConfirmed 17600L<Cent> |]
        |]

        let actual =
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest004.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 466_41L<Cent>
                Apportionment = {
                    PrincipalPortion = 151_32L<Cent>
                    FeesPortion = 233_66L<Cent>
                    InterestPortion = 81_43L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``4a) Only one insufficient funds charge per day`` () =
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
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 154.47m, Restriction.NoLimit, RoundDown)) |]
                Rounding = RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata
            }
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.zero
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map.ofArrayWithMerge [|
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
            8<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.quickConfirmed 22500L<Cent> |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.quickFailed 23700L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.quickConfirmed 24900L<Cent> |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.quickFailed 22500L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.quickFailed 25000L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.quickFailed 17500L<Cent> [||] |]
            380<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            407<OffsetDay>, [| ActualPayment.quickConfirmed 17500L<Cent> |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.quickFailed 17600L<Cent> [||] |]
            475<OffsetDay>, [| ActualPayment.quickConfirmed 17600L<Cent> |]
        |]

        let actual =
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest004a.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 479_92L<Cent>
                Apportionment = {
                    PrincipalPortion = 155_70L<Cent>
                    FeesPortion = 240_43L<Cent>
                    InterestPortion = 83_79L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``5) Quote returning nothing`` () =
        let sp = {

            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 6, 22)
            Principal = 500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2022, 7, 15); PaymentCount = 6; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map.ofArrayWithMerge [|
            23<OffsetDay>, [| ActualPayment.quickFailed 166_67L<Cent> [||] |]
            23<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            23<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            26<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            26<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            26<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            29<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            29<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            29<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            54<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            54<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            54<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            57<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            57<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            57<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            60<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            60<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            60<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            85<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            85<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            85<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            88<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            88<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            88<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            91<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            91<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            91<OffsetDay>, [| ActualPayment.quickFailed 66_67L<Cent> [||] |]
            135<OffsetDay>, [| ActualPayment.quickConfirmed 83_33L<Cent> |]
            165<OffsetDay>, [| ActualPayment.quickConfirmed 83_33L<Cent> |]
            196<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            196<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            196<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            199<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            199<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            199<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            202<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            202<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            202<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            227<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            227<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            227<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            230<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            230<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            230<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            233<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            233<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            233<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            255<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            255<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            255<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            258<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            258<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            258<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            261<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            261<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            261<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            286<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            286<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            286<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            289<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            289<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            289<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            292<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            292<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
            292<OffsetDay>, [| ActualPayment.quickFailed 83_33L<Cent> [||] |]
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
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest005.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 64916L<Cent>
                Apportionment = {
                    PrincipalPortion = 500_00L<Cent>
                    FeesPortion = 0L<Cent>
                    InterestPortion = 149_16L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ``6) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2021, 12, 26)
            Principal = 150000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2022, 1, 7); PaymentCount = 6; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            12<OffsetDay>, [| ActualPayment.quickFailed 500_00L<Cent> [||]; ActualPayment.quickFailed 500_00L<Cent> [||] |]
            15<OffsetDay>, [| ActualPayment.quickFailed 500_00L<Cent> [||]; ActualPayment.quickFailed 500_00L<Cent> [||]; ActualPayment.quickConfirmed 500_00L<Cent> |]
            43<OffsetDay>, [| ActualPayment.quickFailed 500_00L<Cent> [||]; ActualPayment.quickFailed 500_00L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.quickConfirmed 1540_00L<Cent> |]
        ]

        let actual =
            let quote = getQuote IntendedPurpose.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest006.md" false
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = -76_80L<Cent>
                Apportionment = {
                    PrincipalPortion = -76_80L<Cent>
                    FeesPortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected


    [<Fact>]
    let ``7) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            6<OffsetDay>, [| ActualPayment.quickFailed 2_00L<Cent> [||] |]
            16<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent>; ActualPayment.quickConfirmed 97_01L<Cent> |]
        ]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let rescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate

        let (rp: RescheduleParameters) = {
            FeeSettlementRefund = Fee.SettlementRefund.ProRataRescheduled originalFinalPaymentDay
            PaymentSchedule = CustomSchedule <| Map [
                58<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = rescheduleDay })
            ]
            RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
            PromotionalInterestRates = [||]
            ChargeHolidays = [||]
            IntendedPurpose = IntendedPurpose.SettlementOn 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments

        result |> snd |> _.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest007.md" false

        let actual = result |> snd |> _.ScheduleItems |> Map.maxKeyValue

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
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 4_32.256m<Cent>
            NewInterest = 4_32.256m<Cent>
            NewCharges = [||]
            PrincipalPortion = 67_54L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 16_20L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 83_74L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``8) Partial write-off`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            6<OffsetDay>, [| ActualPayment.quickWriteOff 42_00L<Cent> |]
            16<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent>; ActualPayment.quickConfirmed 97_01L<Cent> |]
        ]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let rescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate

        let (rp: RescheduleParameters) = {
            FeeSettlementRefund = Fee.SettlementRefund.ProRataRescheduled originalFinalPaymentDay
            PaymentSchedule = CustomSchedule <| Map [
                58<OffsetDay>, ScheduledPayment.quick ValueNone (ValueSome { Value = 5000L<Cent>; RescheduleDay = rescheduleDay })
            ]
            RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
            PromotionalInterestRates = [||]
            ChargeHolidays = [||]
            IntendedPurpose = IntendedPurpose.SettlementOn 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments

        result |> snd |> _.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest008.md" false

        let actual = result |> snd |> _.ScheduleItems |> Map.maxKeyValue

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
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 52.608m<Cent>
            NewInterest = 52.608m<Cent>
            NewCharges = [||]
            PrincipalPortion = 8_22L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 1_97L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 10_19L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ``9) Negative principal balance accruing interest`` () =
        let sp = {
            AsOfDate = Date(2024, 4, 5)
            StartDate = Date(2023, 5, 5)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 5, 10); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = Charge.Config.initialRecommended
            InterestConfig = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Annual (Percent 8m)
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                InterestRounding = RoundDown
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 111_00L<Cent> |]
            21<OffsetDay>, [| ActualPayment.quickConfirmed 181_01L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest009.md" false

        let actual = schedule.ScheduleItems |> Map.maxKeyValue

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
            BalanceStatus = RefundDue
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = -8.79210959m<Cent>
            NewInterest = -8.79210959m<Cent>
            NewCharges = [||]
            PrincipalPortion = 0L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -12_94L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = -21.55484933m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = -13_16L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }

        actual |> should equal expected
