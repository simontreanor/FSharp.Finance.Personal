namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module EdgeCaseTests =

    open Amortisation
    open CustomerPayments
    open FeesAndCharges
    open PaymentSchedule
    open Quotes
    open Rescheduling

    [<Fact>]
    let ``1) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2023, 2, 9)
            Principal = 30000L<Cent>
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay = 15<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 137_40L<Cent>) }
                { PaymentDay = 43<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 137_40L<Cent>) }
                { PaymentDay = 74<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 137_40L<Cent>) }
                { PaymentDay = 104<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 137_40L<Cent>) }
            |]
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 5<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 31200L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest001.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (500_79L<Cent>, 177_81L<Cent>, 274_64L<Cent>, 48_34L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``2) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 2, 2)
            Principal = 25000L<Cent>
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay = 16<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11500L<Cent>) }
                { PaymentDay = 44<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11500L<Cent>) }
                { PaymentDay = 75<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11500L<Cent>) }
                { PaymentDay = 105<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 11500L<Cent>) }
            |]
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 5<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 26000L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest002.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (455_55L<Cent>, 148_17L<Cent>, 228_86L<Cent>, 78_52L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``3) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 12, 2)
            Principal = 75000L<Cent>
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay = 14<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 34350L<Cent>) }
                { PaymentDay = 45<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 34350L<Cent>) }
                { PaymentDay = 76<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 34350L<Cent>) }
                { PaymentDay = 104<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 34350L<Cent>) }
            |]
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 13<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 82800L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest003.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (1221_54L<Cent>, 427_28L<Cent>, 660_00L<Cent>, 134_26L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``4) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2020, 10, 8)
            Principal = 50000L<Cent>
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay = 8<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 39<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 69<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 100<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 214<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 25000L<Cent>) }
                { PaymentDay = 245<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 27600L<Cent>) }
            |]
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 22500L<Cent>) }
            { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (23700L<Cent>, [||])) }
            { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (23700L<Cent>, [||])) }
            { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (23700L<Cent>, [||])) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 24900L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 214<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 214<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 214<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 217<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 217<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 217<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 220<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 220<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 220<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 248<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 248<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 248<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 251<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 251<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 251<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 379<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17500L<Cent>, [||])) }
            { PaymentDay = 379<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17500L<Cent>, [||])) }
            { PaymentDay = 379<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17500L<Cent>, [||])) }
            { PaymentDay = 380<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17500L<Cent>) }
            { PaymentDay = 407<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17500L<Cent>) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 438<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 438<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 438<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 441<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 441<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 441<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 475<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17600L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest004.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (466_41L<Cent>, 151_32L<Cent>, 233_66L<Cent>, 81_43L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``4a) Only one insufficient funds charge per day`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2020, 10, 8)
            Principal = 50000L<Cent>
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay = 8<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 39<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 69<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 100<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 22500L<Cent>) }
                { PaymentDay = 214<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 25000L<Cent>) }
                { PaymentDay = 245<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Original 27600L<Cent>) }
            |]
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 154.47m, ValueNone, ValueSome RoundDown)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
            { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
            { PaymentDay = 8<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
            { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 11<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 39<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 69<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 22500L<Cent>) }
            { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 72<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 100<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (23700L<Cent>, [||])) }
            { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (23700L<Cent>, [||])) }
            { PaymentDay = 103<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (23700L<Cent>, [||])) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 24900L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (22500L<Cent>, [||])) }
            { PaymentDay = 214<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 214<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 214<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 217<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 217<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 217<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 220<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 220<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 220<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 245<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 248<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 248<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 248<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 251<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 251<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 251<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (25000L<Cent>, [||])) }
            { PaymentDay = 379<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17500L<Cent>, [||])) }
            { PaymentDay = 379<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17500L<Cent>, [||])) }
            { PaymentDay = 379<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17500L<Cent>, [||])) }
            { PaymentDay = 380<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17500L<Cent>) }
            { PaymentDay = 407<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17500L<Cent>) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 435<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 438<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 438<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 438<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 441<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 441<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 441<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (17600L<Cent>, [||])) }
            { PaymentDay = 475<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17600L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest004a.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (479_92L<Cent>, 155_70L<Cent>, 240_43L<Cent>, 83_79L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``5) Quote returning nothing`` () =
        let sp = {

            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 6, 22)
            Principal = 500_00L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2022, 7, 15), 6)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = Interest.Cap.example
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 23<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (166_67L<Cent>, [||])) }
            { PaymentDay = 23<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 23<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 26<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 26<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 26<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 29<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 29<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 29<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 54<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 54<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 54<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 57<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 57<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 57<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 60<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 60<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 60<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 85<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 85<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 85<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 88<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 91<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 91<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 91<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (66_67L<Cent>, [||])) }
            { PaymentDay = 135<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 83_33L<Cent>) }
            { PaymentDay = 165<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 83_33L<Cent>) }
            { PaymentDay = 196<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 196<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 196<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 199<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 199<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 199<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 202<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 202<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 202<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 227<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 227<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 227<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 230<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 230<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 230<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 233<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 233<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 233<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 255<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 255<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 255<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 258<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 258<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 258<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 261<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 261<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 261<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 286<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 286<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 286<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 289<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 289<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 289<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 292<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 292<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 292<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (83_33L<Cent>, [||])) }
            { PaymentDay = 322<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17_58L<Cent>) }
            { PaymentDay = 353<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17_58L<Cent>) }
            { PaymentDay = 384<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17_58L<Cent>) }
            { PaymentDay = 408<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17_58L<Cent>) }
            { PaymentDay = 449<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17_58L<Cent>) }
            { PaymentDay = 476<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 17_58L<Cent>) }
            { PaymentDay = 499<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 15_74L<Cent>) }
            { PaymentDay = 531<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 15_74L<Cent>) }
            { PaymentDay = 574<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 15_74L<Cent>) }
            { PaymentDay = 595<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 15_74L<Cent>) }
            { PaymentDay = 629<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 15_74L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest005.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (64916L<Cent>, 500_00L<Cent>, 0L<Cent>, 149_16L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``6) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 12)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2021, 12, 26)
            Principal = 150000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2022, 1, 7), 6)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = Interest.Cap.example
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 12<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (500_00L<Cent>, [||])) }
            { PaymentDay = 12<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (500_00L<Cent>, [||])) }
            { PaymentDay = 15<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (500_00L<Cent>, [||])) }
            { PaymentDay = 15<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (500_00L<Cent>, [||])) }
            { PaymentDay = 15<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 43<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (500_00L<Cent>, [||])) }
            { PaymentDay = 43<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (500_00L<Cent>, [||])) }
            { PaymentDay = 45<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 1540_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/EdgeCaseTest006.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-76_80L<Cent>, -76_80L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected


    [<Fact>]
    let ``7) Quote returning nothing`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2024, 2, 22), 4)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = Interest.Cap.example
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 6<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (2_00L<Cent>, [||])) }
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 97_01L<Cent>) }
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 97_01L<Cent>) }
        |]

        let (rp: RescheduleParameters) = {
            OriginalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>
            FeesSettlement = Fees.Settlement.ProRataRefund
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay =  58<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 5000L<Cent>) }
            |]
            NegativeInterestOption = DoNotApplyNegativeInterest
            InterestHolidays = [||]
            ChargesHolidays = [||]
            FutureSettlementDay = ValueSome 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments
        result |> ValueOption.iter(snd >> _.ScheduleItems >> Formatting.outputListToHtml "out/EdgeCaseTest007.md")

        let actual = result |> ValueOption.map (fun (_, s) -> s.ScheduleItems |> Array.last)

        let expected = ValueSome ({
            OffsetDate = Date(2024, 4, 30)
            OffsetDay = 88<OffsetDay>
            Advances = [||]
            ScheduledPayment = ScheduledPaymentType.None
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueSome 83_74L<Cent>
            NetEffect = 83_74L<Cent>
            PaymentStatus = Generated Settlement
            BalanceStatus = ClosedBalance
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
            ProRatedFees = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``8) Partial write-off`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 14)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2024, 2, 22), 4)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = Interest.Cap.example
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 6<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.WriteOff 42_00L<Cent>) }
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 97_01L<Cent>) }
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 97_01L<Cent>) }
        |]

        let (rp: RescheduleParameters) = {
            OriginalFinalPaymentDay = ((Date(2024, 5, 22) - Date(2024, 2, 2)).Days) * 1<OffsetDay>
            FeesSettlement = Fees.Settlement.ProRataRefund
            PaymentSchedule = IrregularSchedule [|
                { PaymentDay =  58<OffsetDay>; PaymentDetails = ScheduledPayment (ScheduledPaymentType.Rescheduled 5000L<Cent>) }
            |]
            NegativeInterestOption = DoNotApplyNegativeInterest
            InterestHolidays = [||]
            ChargesHolidays = [||]
            FutureSettlementDay = ValueSome 88<OffsetDay>
        }

        let result = reschedule sp rp actualPayments
        result |> ValueOption.iter(snd >> _.ScheduleItems >> Formatting.outputListToHtml "out/EdgeCaseTest008.md")

        let actual = result |> ValueOption.map (fun (_, s) -> s.ScheduleItems |> Array.last)

        let expected = ValueSome ({
            OffsetDate = Date(2024, 4, 30)
            OffsetDay = 88<OffsetDay>
            Advances = [||]
            ScheduledPayment = ScheduledPaymentType.None
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueSome 10_19L<Cent>
            NetEffect = 10_19L<Cent>
            PaymentStatus = Generated Settlement
            BalanceStatus = ClosedBalance
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
            ProRatedFees = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``9) Negative principal balance accruing interest`` () =
        let sp = {
            AsOfDate = Date(2024, 4, 5)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2023, 5, 5)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 5, 10), 4)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = Interest.Cap.example
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 5<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 111_00L<Cent>) }
            { PaymentDay = 21<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 181_01L<Cent>) }
        |]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduledPaymentType.Original

        schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/EdgeCaseTest009.md")

        let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
        let expected = ValueSome {
            OffsetDate = Date(2023, 8, 10)
            OffsetDay = 97<OffsetDay>
            Advances = [||]
            ScheduledPayment = ScheduledPaymentType.Original 87_67L<Cent>
            PaymentDue = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 0L<Cent>
            PaymentStatus = NoLongerRequired
            BalanceStatus = RefundDue
            NewInterest = -8.792109589041095890410958135m<Cent>
            NewCharges = [||]
            PrincipalPortion = 0L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = -12_94L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = -21.554849315068493150684929621m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            ProRatedFees = 0L<Cent>
        }
        actual |> should equal expected
