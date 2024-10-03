namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EdgeCaseTests =

    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open FeesAndCharges
    open Formatting
    open MapExtension
    open PaymentSchedule
    open Percentages
    open Quotes
    open Rescheduling
    open ValueOptionCE

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    [<Fact>]
    let ``1) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2023, 2, 9)
            Principal = 30000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                15<OffsetDay>, ScheduledPayment.Quick (ValueSome 137_40L<Cent>) ValueNone
                43<OffsetDay>, ScheduledPayment.Quick (ValueSome 137_40L<Cent>) ValueNone
                74<OffsetDay>, ScheduledPayment.Quick (ValueSome 137_40L<Cent>) ValueNone
                104<OffsetDay>, ScheduledPayment.Quick (ValueSome 137_40L<Cent>) ValueNone
            ]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.QuickConfirmed 31200L<Cent> |]
        ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest001.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (500_79L<Cent>, 177_81L<Cent>, 274_64L<Cent>, 48_34L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``2) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                16<OffsetDay>, ScheduledPayment.Quick (ValueSome 11500L<Cent>) ValueNone
                44<OffsetDay>, ScheduledPayment.Quick (ValueSome 11500L<Cent>) ValueNone
                75<OffsetDay>, ScheduledPayment.Quick (ValueSome 11500L<Cent>) ValueNone
                105<OffsetDay>, ScheduledPayment.Quick (ValueSome 11500L<Cent>) ValueNone
            ]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.QuickConfirmed 26000L<Cent> |]
        ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest002.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (455_55L<Cent>, 148_17L<Cent>, 228_86L<Cent>, 78_52L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``3) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 12, 2)
            Principal = 75000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                14<OffsetDay>, ScheduledPayment.Quick (ValueSome 34350L<Cent>) ValueNone
                45<OffsetDay>, ScheduledPayment.Quick (ValueSome 34350L<Cent>) ValueNone
                76<OffsetDay>, ScheduledPayment.Quick (ValueSome 34350L<Cent>) ValueNone
                104<OffsetDay>, ScheduledPayment.Quick (ValueSome 34350L<Cent>) ValueNone
            ]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = Map [
            13<OffsetDay>, [| ActualPayment.QuickConfirmed 82800L<Cent> |]
        ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest003.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (1221_54L<Cent>, 427_28L<Cent>, 660_00L<Cent>, 134_26L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``4) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2020, 10, 8)
            Principal = 50000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                8<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                39<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                69<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                100<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                214<OffsetDay>, ScheduledPayment.Quick (ValueSome 25000L<Cent>) ValueNone
                245<OffsetDay>, ScheduledPayment.Quick (ValueSome 27600L<Cent>) ValueNone
            ]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = Map.ofArrayWithArrayMerge [|
            8<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            8<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            8<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.QuickConfirmed 22500L<Cent> |]
            72<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.QuickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.QuickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.QuickFailed 23700L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.QuickConfirmed 24900L<Cent> |]
            106<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.QuickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.QuickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.QuickFailed 17500L<Cent> [||] |]
            380<OffsetDay>, [| ActualPayment.QuickConfirmed 17500L<Cent> |]
            407<OffsetDay>, [| ActualPayment.QuickConfirmed 17500L<Cent> |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            475<OffsetDay>, [| ActualPayment.QuickConfirmed 17600L<Cent> |]
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest004.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (466_41L<Cent>, 151_32L<Cent>, 233_66L<Cent>, 81_43L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``4a) Only one insufficient funds charge per day`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2020, 10, 8)
            Principal = 50000L<Cent>
            ScheduleConfig = CustomSchedule <| Map [
                8<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                39<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                69<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                100<OffsetDay>, ScheduledPayment.Quick (ValueSome 22500L<Cent>) ValueNone
                214<OffsetDay>, ScheduledPayment.Quick (ValueSome 25000L<Cent>) ValueNone
                245<OffsetDay>, ScheduledPayment.Quick (ValueSome 27600L<Cent>) ValueNone
            ]
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = Map.ofArrayWithArrayMerge [|
            8<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
            8<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
            8<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |] |]
            11<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            11<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            14<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            39<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            42<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            69<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.QuickConfirmed 22500L<Cent> |]
            72<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            72<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            75<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            100<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.QuickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.QuickFailed 23700L<Cent> [||] |]
            103<OffsetDay>, [| ActualPayment.QuickFailed 23700L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.QuickConfirmed 24900L<Cent> |]
            106<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            106<OffsetDay>, [| ActualPayment.QuickFailed 22500L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            214<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            217<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            220<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            245<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            248<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            251<OffsetDay>, [| ActualPayment.QuickFailed 25000L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.QuickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.QuickFailed 17500L<Cent> [||] |]
            379<OffsetDay>, [| ActualPayment.QuickFailed 17500L<Cent> [||] |]
            380<OffsetDay>, [| ActualPayment.QuickConfirmed 17500L<Cent> |]
            407<OffsetDay>, [| ActualPayment.QuickConfirmed 17500L<Cent> |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            435<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            438<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            441<OffsetDay>, [| ActualPayment.QuickFailed 17600L<Cent> [||] |]
            475<OffsetDay>, [| ActualPayment.QuickConfirmed 17600L<Cent> |]
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest004a.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (479_92L<Cent>, 155_70L<Cent>, 240_43L<Cent>, 83_79L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``5) Quote returning nothing`` () =
        let sp = {

            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2022, 6, 22)
            Principal = 500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2022, 7, 15); PaymentCount = 6; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = Map.ofArrayWithArrayMerge [|
            23<OffsetDay>, [| ActualPayment.QuickFailed 166_67L<Cent> [||] |]
            23<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            23<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            26<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            26<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            26<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            29<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            29<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            29<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            54<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            54<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            54<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            57<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            57<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            57<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            60<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            60<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            60<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            85<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            85<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            85<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            88<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            88<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            88<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            91<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            91<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            91<OffsetDay>, [| ActualPayment.QuickFailed 66_67L<Cent> [||] |]
            135<OffsetDay>, [| ActualPayment.QuickConfirmed 83_33L<Cent> |]
            165<OffsetDay>, [| ActualPayment.QuickConfirmed 83_33L<Cent> |]
            196<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            196<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            196<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            199<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            199<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            199<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            202<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            202<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            202<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            227<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            227<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            227<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            230<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            230<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            230<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            233<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            233<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            233<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            255<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            255<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            255<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            258<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            258<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            258<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            261<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            261<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            261<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            286<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            286<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            286<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            289<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            289<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            289<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            292<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            292<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            292<OffsetDay>, [| ActualPayment.QuickFailed 83_33L<Cent> [||] |]
            322<OffsetDay>, [| ActualPayment.QuickConfirmed 17_58L<Cent> |]
            353<OffsetDay>, [| ActualPayment.QuickConfirmed 17_58L<Cent> |]
            384<OffsetDay>, [| ActualPayment.QuickConfirmed 17_58L<Cent> |]
            408<OffsetDay>, [| ActualPayment.QuickConfirmed 17_58L<Cent> |]
            449<OffsetDay>, [| ActualPayment.QuickConfirmed 17_58L<Cent> |]
            476<OffsetDay>, [| ActualPayment.QuickConfirmed 17_58L<Cent> |]
            499<OffsetDay>, [| ActualPayment.QuickConfirmed 15_74L<Cent> |]
            531<OffsetDay>, [| ActualPayment.QuickConfirmed 15_74L<Cent> |]
            574<OffsetDay>, [| ActualPayment.QuickConfirmed 15_74L<Cent> |]
            595<OffsetDay>, [| ActualPayment.QuickConfirmed 15_74L<Cent> |]
            629<OffsetDay>, [| ActualPayment.QuickConfirmed 15_74L<Cent> |]
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest005.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (64916L<Cent>, 500_00L<Cent>, 0L<Cent>, 149_16L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``6) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            StartDate = Date(2021, 12, 26)
            Principal = 150000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2022, 1, 7); PaymentCount = 6; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = Map [
            12<OffsetDay>, [| ActualPayment.QuickFailed 500_00L<Cent> [||]; ActualPayment.QuickFailed 500_00L<Cent> [||] |]
            15<OffsetDay>, [| ActualPayment.QuickFailed 500_00L<Cent> [||]; ActualPayment.QuickFailed 500_00L<Cent> [||]; ActualPayment.QuickConfirmed 500_00L<Cent> |]
            43<OffsetDay>, [| ActualPayment.QuickFailed 500_00L<Cent> [||]; ActualPayment.QuickFailed 500_00L<Cent> [||] |]
            45<OffsetDay>, [| ActualPayment.QuickConfirmed 1540_00L<Cent> |]
        ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/EdgeCaseTest006.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-76_80L<Cent>, -76_80L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected


    [<Fact>]
    let ``7) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = Map [
            6<OffsetDay>, [| ActualPayment.QuickFailed 2_00L<Cent> [||] |]
            16<OffsetDay>, [| ActualPayment.QuickConfirmed 97_01L<Cent>; ActualPayment.QuickConfirmed 97_01L<Cent> |]
        ]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let (rp: RescheduleParameters) = {
            RescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate
            FeesSettlementRefund = Fees.SettlementRefund.ProRata (ValueSome originalFinalPaymentDay)
            PaymentSchedule = CustomSchedule <| Map [
                58<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome 5000L<Cent>)
            ]
            RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            PromotionalInterestRates = [||]
            ChargesHolidays = [||]
            IntendedPurpose = IntendedPurpose.Settlement <| ValueSome 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments
        result |> ValueOption.iter(snd >> _.ScheduleItems >> (outputMapToHtml "out/EdgeCaseTest007.md" false))

        let actual = result |> ValueOption.map (snd >> _.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (88<OffsetDay>, {
            OffsetDate = Date(2024, 4, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueSome 138_65L<Cent>
            NetEffect = 138_65L<Cent>
            PaymentStatus = Generated
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 5_63.072m<Cent>
            NewInterest = 5_63.072m<Cent>
            NewCharges = [||]
            PrincipalPortion = 87_98L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 50_67L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 138_65L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``8) Partial write-off`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = Map [
            6<OffsetDay>, [| ActualPayment.QuickWriteOff 42_00L<Cent> |]
            16<OffsetDay>, [| ActualPayment.QuickConfirmed 97_01L<Cent>; ActualPayment.QuickConfirmed 97_01L<Cent> |]
        ]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let (rp: RescheduleParameters) = {
            RescheduleDay = sp.AsOfDate |> OffsetDay.fromDate sp.StartDate
            FeesSettlementRefund = Fees.SettlementRefund.ProRata (ValueSome originalFinalPaymentDay)
            PaymentSchedule = CustomSchedule <| Map [
                58<OffsetDay>, ScheduledPayment.Quick ValueNone (ValueSome 5000L<Cent>)
            ]
            RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            PromotionalInterestRates = [||]
            ChargesHolidays = [||]
            IntendedPurpose = IntendedPurpose.Settlement <| ValueSome 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments
        result |> ValueOption.iter(snd >> _.ScheduleItems >> (outputMapToHtml "out/EdgeCaseTest008.md" false))

        let actual = result |> ValueOption.map (snd >> _.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (88<OffsetDay>, {
            OffsetDate = Date(2024, 4, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.DefaultValue
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueSome 68_68L<Cent>
            NetEffect = 68_68L<Cent>
            PaymentStatus = Generated
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 2_78.912m<Cent>
            NewInterest = 2_78.912m<Cent>
            NewCharges = [||]
            PrincipalPortion = 43_58L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 25_10L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 68_68L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``9) Negative principal balance accruing interest`` () =
        let sp = {
            AsOfDate = Date(2024, 4, 5)
            StartDate = Date(2023, 5, 5)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 5, 10); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = Map [
            5<OffsetDay>, [| ActualPayment.QuickConfirmed 111_00L<Cent> |]
            21<OffsetDay>, [| ActualPayment.QuickConfirmed 181_01L<Cent> |]
        ]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement false

        schedule |> ValueOption.iter(_.ScheduleItems >> (outputMapToHtml "out/EdgeCaseTest009.md" false))

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Map.maxKeyValue)

        let expected = ValueSome (97<OffsetDay>, {
            OffsetDate = Date(2023, 8, 10)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 87_67L<Cent>) ValueNone
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
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
        })

        actual |> should equal expected
