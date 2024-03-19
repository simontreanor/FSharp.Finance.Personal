namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementTests =

    open CustomerPayments
    open PaymentSchedule
    open Amortisation
    open Quotes

    [<Fact>]
    let ``1) Final payment due on Friday: what would I pay if I paid it today?`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 19)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 12, 22), 4)
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
                Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = {
                    InterestRounding = RoundDown
                    PaymentRounding = RoundUp
                }
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 24<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 55<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 86<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/SettlementTest001.md"
                let! scheduledItem = Array.vTryLastBut 1 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, scheduledItem
            }

        let expected = ValueSome (
            PaymentQuote (98_52L<Cent>, 81_56L<Cent>, 0L<Cent>, 16_96L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 3, 19)
                OffsetDay = 112<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 98_52L<Cent>
                NetEffect = 98_52L<Cent>
                PaymentStatus = Generated Settlement
                BalanceStatus = ClosedBalance
                NewInterest = 16_96L<Cent>
                NewCharges = [||]
                PrincipalPortion = 81_56L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 16_96L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0L<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 98_52L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``2) Final payment due on Friday: what would I pay if I paid it one week too late?`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 29)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 12, 22), 4)
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
                Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = {
                    InterestRounding = RoundDown
                    PaymentRounding = RoundUp
                }
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 24<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 55<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 86<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/SettlementTest002.md"
                let scheduledItem = Array.last quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, scheduledItem
            }

        let expected = ValueSome (
            PaymentQuote (105_04L<Cent>, 81_56L<Cent>, 0L<Cent>, 23_48L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 3, 29)
                OffsetDay = 122<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 105_04L<Cent>
                NetEffect = 105_04L<Cent>
                PaymentStatus = Generated Settlement
                BalanceStatus = ClosedBalance
                NewInterest = 4_56L<Cent>
                NewCharges = [||]
                PrincipalPortion = 81_56L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 23_48L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0L<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 105_04L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``3) Final payment due on Friday: what if I pay £50 on Friday and the rest next week one week too late?`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 29)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 12, 22), 4)
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
                Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = {
                    InterestRounding = RoundDown
                    PaymentRounding = RoundUp
                }
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 24<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 55<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 86<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 100_53L<Cent>) }
            { PaymentDay = 115<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 50_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/SettlementTest003.md"
                let scheduledItem = Array.last quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, scheduledItem
            }

        let expected = ValueSome (
            PaymentQuote (53_30L<Cent>, 50_48L<Cent>, 0L<Cent>, 2_82L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 3, 29)
                OffsetDay = 122<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 53_30L<Cent>
                NetEffect = 53_30L<Cent>
                PaymentStatus = Generated Settlement
                BalanceStatus = ClosedBalance
                NewInterest = 2_82L<Cent>
                NewCharges = [||]
                PrincipalPortion = 50_48L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 2_82L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0L<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 53_30L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

