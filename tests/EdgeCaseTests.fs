namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EdgeCaseTests =

    open Amortisation
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
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
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledOriginal 15<OffsetDay> 137_40L<Cent>
                CustomerPayment.ScheduledOriginal 43<OffsetDay> 137_40L<Cent>
                CustomerPayment.ScheduledOriginal 74<OffsetDay> 137_40L<Cent>
                CustomerPayment.ScheduledOriginal 104<OffsetDay> 137_40L<Cent>
            |]
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

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 5<OffsetDay> 31200L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest001.md"
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
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledOriginal 16<OffsetDay> 11500L<Cent>
                CustomerPayment.ScheduledOriginal 44<OffsetDay> 11500L<Cent>
                CustomerPayment.ScheduledOriginal 75<OffsetDay> 11500L<Cent>
                CustomerPayment.ScheduledOriginal 105<OffsetDay> 11500L<Cent>
            |]
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

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 5<OffsetDay> 26000L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest002.md"
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
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledOriginal 14<OffsetDay> 34350L<Cent>
                CustomerPayment.ScheduledOriginal 45<OffsetDay> 34350L<Cent>
                CustomerPayment.ScheduledOriginal 76<OffsetDay> 34350L<Cent>
                CustomerPayment.ScheduledOriginal 104<OffsetDay> 34350L<Cent>
            |]
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

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 13<OffsetDay> 82800L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest003.md"
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
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledOriginal 8<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 39<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 69<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 100<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 214<OffsetDay> 25000L<Cent>
                CustomerPayment.ScheduledOriginal 245<OffsetDay> 27600L<Cent>
            |]
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

        let actualPayments = [|
            CustomerPayment.ActualFailed 8<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 8<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 8<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 11<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 11<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 11<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 14<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 14<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 14<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 39<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 39<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 39<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 42<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 42<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 42<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 45<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 45<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 45<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 69<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 69<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 69<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualConfirmed 72<OffsetDay> 22500L<Cent>
            CustomerPayment.ActualFailed 72<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 72<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 75<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 75<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 75<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 100<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 100<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 100<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 103<OffsetDay> 23700L<Cent> [||]
            CustomerPayment.ActualFailed 103<OffsetDay> 23700L<Cent> [||]
            CustomerPayment.ActualFailed 103<OffsetDay> 23700L<Cent> [||]
            CustomerPayment.ActualConfirmed 106<OffsetDay> 24900L<Cent>
            CustomerPayment.ActualFailed 106<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 106<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 214<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 214<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 214<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 217<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 217<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 217<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 220<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 220<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 220<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 248<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 248<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 248<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 251<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 251<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 251<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 379<OffsetDay> 17500L<Cent> [||]
            CustomerPayment.ActualFailed 379<OffsetDay> 17500L<Cent> [||]
            CustomerPayment.ActualFailed 379<OffsetDay> 17500L<Cent> [||]
            CustomerPayment.ActualConfirmed 380<OffsetDay> 17500L<Cent>
            CustomerPayment.ActualConfirmed 407<OffsetDay> 17500L<Cent>
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 438<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 438<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 438<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 441<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 441<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 441<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualConfirmed 475<OffsetDay> 17600L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest004.md"
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
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledOriginal 8<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 39<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 69<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 100<OffsetDay> 22500L<Cent>
                CustomerPayment.ScheduledOriginal 214<OffsetDay> 25000L<Cent>
                CustomerPayment.ScheduledOriginal 245<OffsetDay> 27600L<Cent>
            |]
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

        let actualPayments = [|
            CustomerPayment.ActualFailed 8<OffsetDay> 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
            CustomerPayment.ActualFailed 8<OffsetDay> 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
            CustomerPayment.ActualFailed 8<OffsetDay> 22500L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
            CustomerPayment.ActualFailed 11<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 11<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 11<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 14<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 14<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 14<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 39<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 39<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 39<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 42<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 42<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 42<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 45<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 45<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 45<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 69<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 69<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 69<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualConfirmed 72<OffsetDay> 22500L<Cent>
            CustomerPayment.ActualFailed 72<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 72<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 75<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 75<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 75<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 100<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 100<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 100<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 103<OffsetDay> 23700L<Cent> [||]
            CustomerPayment.ActualFailed 103<OffsetDay> 23700L<Cent> [||]
            CustomerPayment.ActualFailed 103<OffsetDay> 23700L<Cent> [||]
            CustomerPayment.ActualConfirmed 106<OffsetDay> 24900L<Cent>
            CustomerPayment.ActualFailed 106<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 106<OffsetDay> 22500L<Cent> [||]
            CustomerPayment.ActualFailed 214<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 214<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 214<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 217<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 217<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 217<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 220<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 220<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 220<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 245<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 248<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 248<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 248<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 251<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 251<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 251<OffsetDay> 25000L<Cent> [||]
            CustomerPayment.ActualFailed 379<OffsetDay> 17500L<Cent> [||]
            CustomerPayment.ActualFailed 379<OffsetDay> 17500L<Cent> [||]
            CustomerPayment.ActualFailed 379<OffsetDay> 17500L<Cent> [||]
            CustomerPayment.ActualConfirmed 380<OffsetDay> 17500L<Cent>
            CustomerPayment.ActualConfirmed 407<OffsetDay> 17500L<Cent>
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 435<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 438<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 438<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 438<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 441<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 441<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualFailed 441<OffsetDay> 17600L<Cent> [||]
            CustomerPayment.ActualConfirmed 475<OffsetDay> 17600L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest004a.md"
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
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2022, 7, 15), 6, ValueNone)
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

        let actualPayments = [|
            CustomerPayment.ActualFailed 23<OffsetDay> 166_67L<Cent> [||]
            CustomerPayment.ActualFailed 23<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 23<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 26<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 26<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 26<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 29<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 29<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 29<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 54<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 54<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 54<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 57<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 57<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 57<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 60<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 60<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 60<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 85<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 85<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 85<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 88<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 88<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 88<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 91<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 91<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualFailed 91<OffsetDay> 66_67L<Cent> [||]
            CustomerPayment.ActualConfirmed 135<OffsetDay> 83_33L<Cent>
            CustomerPayment.ActualConfirmed 165<OffsetDay> 83_33L<Cent>
            CustomerPayment.ActualFailed 196<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 196<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 196<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 199<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 199<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 199<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 202<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 202<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 202<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 227<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 227<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 227<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 230<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 230<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 230<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 233<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 233<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 233<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 255<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 255<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 255<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 258<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 258<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 258<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 261<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 261<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 261<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 286<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 286<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 286<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 289<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 289<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 289<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 292<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 292<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualFailed 292<OffsetDay> 83_33L<Cent> [||]
            CustomerPayment.ActualConfirmed 322<OffsetDay> 17_58L<Cent>
            CustomerPayment.ActualConfirmed 353<OffsetDay> 17_58L<Cent>
            CustomerPayment.ActualConfirmed 384<OffsetDay> 17_58L<Cent>
            CustomerPayment.ActualConfirmed 408<OffsetDay> 17_58L<Cent>
            CustomerPayment.ActualConfirmed 449<OffsetDay> 17_58L<Cent>
            CustomerPayment.ActualConfirmed 476<OffsetDay> 17_58L<Cent>
            CustomerPayment.ActualConfirmed 499<OffsetDay> 15_74L<Cent>
            CustomerPayment.ActualConfirmed 531<OffsetDay> 15_74L<Cent>
            CustomerPayment.ActualConfirmed 574<OffsetDay> 15_74L<Cent>
            CustomerPayment.ActualConfirmed 595<OffsetDay> 15_74L<Cent>
            CustomerPayment.ActualConfirmed 629<OffsetDay> 15_74L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest005.md"
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
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2022, 1, 7), 6, ValueNone)
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

        let actualPayments = [|
            CustomerPayment.ActualFailed 12<OffsetDay> 500_00L<Cent> [||]
            CustomerPayment.ActualFailed 12<OffsetDay> 500_00L<Cent> [||]
            CustomerPayment.ActualFailed 15<OffsetDay> 500_00L<Cent> [||]
            CustomerPayment.ActualFailed 15<OffsetDay> 500_00L<Cent> [||]
            CustomerPayment.ActualConfirmed 15<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualFailed 43<OffsetDay> 500_00L<Cent> [||]
            CustomerPayment.ActualFailed 43<OffsetDay> 500_00L<Cent> [||]
            CustomerPayment.ActualConfirmed 45<OffsetDay> 1540_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest006.md"
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
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2024, 2, 22), 4, ValueNone)
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

        let actualPayments = [|
            CustomerPayment.ActualFailed 6<OffsetDay> 2_00L<Cent> [||]
            CustomerPayment.ActualConfirmed 16<OffsetDay> 97_01L<Cent>
            CustomerPayment.ActualConfirmed 16<OffsetDay> 97_01L<Cent>
        |]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let (rp: RescheduleParameters) = {
            FeesSettlementRefund = Fees.SettlementRefund.ProRata (ValueSome originalFinalPaymentDay)
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledRescheduled 58<OffsetDay> 5000L<Cent>
            |]
            RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            PromotionalInterestRates = [||]
            ChargesHolidays = [||]
            IntendedPurpose = IntendedPurpose.Settlement <| ValueSome 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments
        result |> ValueOption.iter(snd >> _.ScheduleItems >> Formatting.outputListToHtml "out/EdgeCaseTest007.md")

        let actual = result |> ValueOption.map (fun (_, s) -> s.ScheduleItems |> Array.last)

        let expected = ValueSome ({
            OffsetDate = Date(2024, 4, 30)
            OffsetDay = 88<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 2
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueSome 83_74L<Cent>
            NetEffect = 83_74L<Cent>
            PaymentStatus = Generated
            BalanceStatus = ClosedBalance
            ContractualInterest = 0m<Cent>
            SimpleInterest = 16_20.960m<Cent>
            NewInterest = 16_20.960m<Cent>
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
        })
        actual |> should equal expected

    [<Fact>]
    let ``8) Partial write-off`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2024, 2, 22), 4, ValueNone)
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

        let actualPayments = [|
            CustomerPayment.ActualWriteOff 6<OffsetDay> 42_00L<Cent>
            CustomerPayment.ActualConfirmed 16<OffsetDay> 97_01L<Cent>
            CustomerPayment.ActualConfirmed 16<OffsetDay> 97_01L<Cent>
        |]

        let originalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>

        let (rp: RescheduleParameters) = {
            FeesSettlementRefund = Fees.SettlementRefund.ProRata (ValueSome originalFinalPaymentDay)
            PaymentSchedule = IrregularSchedule [|
                CustomerPayment.ScheduledRescheduled 58<OffsetDay> 5000L<Cent>
            |]
            RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            PromotionalInterestRates = [||]
            ChargesHolidays = [||]
            IntendedPurpose = IntendedPurpose.Settlement <| ValueSome 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments
        result |> ValueOption.iter(snd >> _.ScheduleItems >> Formatting.outputListToHtml "out/EdgeCaseTest008.md")

        let actual = result |> ValueOption.map (fun (_, s) -> s.ScheduleItems |> Array.last)

        let expected = ValueSome ({
            OffsetDate = Date(2024, 4, 30)
            OffsetDay = 88<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType= ScheduledPaymentType.None; Metadata = Map.empty }
            Window = 2
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueSome 10_19L<Cent>
            NetEffect = 10_19L<Cent>
            PaymentStatus = Generated
            BalanceStatus = ClosedBalance
            ContractualInterest = 0m<Cent>
            SimpleInterest = 1_97.280m<Cent>
            NewInterest = 1_97.280m<Cent>
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
        })
        actual |> should equal expected

    [<Fact>]
    let ``9) Negative principal balance accruing interest`` () =
        let sp = {
            AsOfDate = Date(2024, 4, 5)
            StartDate = Date(2023, 5, 5)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 5, 10), 4, ValueNone)
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

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 5<OffsetDay> 111_00L<Cent>
            CustomerPayment.ActualConfirmed 21<OffsetDay> 181_01L<Cent>
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/EdgeCaseTest009.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 8, 10)
            OffsetDay = 97<OffsetDay>
            Advances = [||]
            ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 87_67L<Cent>; Metadata = Map.empty }
            Window = 4
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 0L<Cent>
            PaymentStatus = NoLongerRequired
            BalanceStatus = RefundDue
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
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        }
        actual |> should equal expected
