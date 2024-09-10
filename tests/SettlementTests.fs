namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementTests =

    open ArrayExtension
    open Amortisation
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open PaymentSchedule
    open Percentages
    open Quotes
    open ValueOptionCE

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    [<Fact>]
    let ``1) Final payment due on Friday: what would I pay if I paid it today?`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 19)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 12, 22), 4, ValueNone)
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
            CustomerPayment.ActualConfirmed 24<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 55<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 86<OffsetDay> 100_53L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/SettlementTest001.md" false
                let! scheduledItem = Array.vTryLastBut 1 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, scheduledItem
            }

        let expected = ValueSome (
            PaymentQuote (98_52L<Cent>, 81_56L<Cent>, 0L<Cent>, 16_96L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 3, 19)
                OffsetDay = 112<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 3
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 98_52L<Cent>
                NetEffect = 98_52L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ContractualInterest = 0m<Cent>
                SimpleInterest = 16_96.448m<Cent>
                NewInterest = 16_96.448m<Cent>
                NewCharges = [||]
                PrincipalPortion = 81_56L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 16_96L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 98_52L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``2) Final payment due on Friday: what would I pay if I paid it one week too late?`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 29)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 12, 22), 4, ValueNone)
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
            CustomerPayment.ActualConfirmed 24<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 55<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 86<OffsetDay> 100_53L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/SettlementTest002.md" false
                let scheduledItem = Array.last quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, scheduledItem
            }

        let expected = ValueSome (
            PaymentQuote (105_04L<Cent>, 81_56L<Cent>, 0L<Cent>, 23_48L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 3, 29)
                OffsetDay = 122<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 105_04L<Cent>
                NetEffect = 105_04L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ContractualInterest = 0m<Cent>
                SimpleInterest = 4_56.736m<Cent>
                NewInterest = 4_56.736m<Cent>
                NewCharges = [||]
                PrincipalPortion = 81_56L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 23_48L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 105_04L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``3) Final payment due on Friday: what if I pay £50 on Friday and the rest next week one week too late?`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 29)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 12, 22), 4, ValueNone)
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
            CustomerPayment.ActualConfirmed 24<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 55<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 86<OffsetDay> 100_53L<Cent>
            CustomerPayment.ActualConfirmed 115<OffsetDay> 50_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/SettlementTest003.md" false
                let scheduledItem = Array.last quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, scheduledItem
            }

        let expected = ValueSome (
            PaymentQuote (53_30L<Cent>, 50_48L<Cent>, 0L<Cent>, 2_82L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 3, 29)
                OffsetDay = 122<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 53_30L<Cent>
                NetEffect = 53_30L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ContractualInterest = 0m<Cent>
                SimpleInterest = 2_82.688m<Cent>
                NewInterest = 2_82.688m<Cent>
                NewCharges = [||]
                PrincipalPortion = 50_48L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 2_82L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 53_30L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
        )

        actual |> should equal expected

