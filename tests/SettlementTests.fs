namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementTests =

    open Amortisation
    open Calculation
    open DateDay
    open Formatting
    open Scheduling
    open Quotes

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit))
    }

    [<Fact>]
    let SettlementTest001 () =
        let title = "SettlementTest001"
        let description = "Final payment due on Friday: what would I pay if I paid it today?"
        let sp = {
            AsOfDate = Date(2024, 3, 19)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 12, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                Tolerance = LowerFinalPayment
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
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
            }
        }

        let actualPayments =
            Map [
                24<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                55<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                86<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
            ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
            let scheduledItem = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 112<OffsetDay>
            quote.QuoteResult, scheduledItem

        let paymentQuote =
            PaymentQuote {
                PaymentValue = 98_52L<Cent>
                Apportionment = {
                    PrincipalPortion = 81_56L<Cent>
                    FeesPortion = 0L<Cent>
                    InterestPortion = 16_96L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDate = Date(2024, 3, 19)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 3
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 98_52L<Cent>
                NetEffect = 98_52L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
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
                SettlementFigure = ValueSome 98_52L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let SettlementTest002 () =
        let title = "SettlementTest002"
        let description = "Final payment due on Friday: what would I pay if I paid it one week too late?"
        let sp = {
            AsOfDate = Date(2024, 3, 29)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 12, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                Tolerance = LowerFinalPayment
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
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
            }
        }

        let actualPayments =
            Map [
                24<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                55<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                86<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
            ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
            let scheduledItem = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 122<OffsetDay>
            quote.QuoteResult, scheduledItem

        let paymentQuote =
            PaymentQuote {
                PaymentValue = 105_04L<Cent>
                Apportionment = {
                    PrincipalPortion = 81_56L<Cent>
                    FeesPortion = 0L<Cent>
                    InterestPortion = 23_48L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDate = Date(2024, 3, 29)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 105_04L<Cent>
                NetEffect = 105_04L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
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
                SettlementFigure = ValueSome 105_04L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let SettlementTest003 () =
        let title = "SettlementTest003"
        let description = "Final payment due on Friday: what if I pay £50 on Friday and the rest next week one week too late?"
        let sp = {
            AsOfDate = Date(2024, 3, 29)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 12, 22); PaymentCount = 4; MaxDuration = Duration.Unlimited }
            PaymentConfig = {
                Tolerance = LowerFinalPayment
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
                InterestRounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
            }
        }

        let actualPayments =
            Map [
                24<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                55<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                86<OffsetDay>, [| ActualPayment.quickConfirmed 100_53L<Cent> |]
                115<OffsetDay>, [| ActualPayment.quickConfirmed 50_00L<Cent> |]
            ]

        let actual =
            let quote = getQuote SettlementDay.SettlementOnAsOfDay sp actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile title description sp
            let scheduledItem = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 122<OffsetDay>
            quote.QuoteResult, scheduledItem

        let paymentQuote =
            PaymentQuote {
                PaymentValue = 53_30L<Cent>
                Apportionment = {
                    PrincipalPortion = 50_48L<Cent>
                    FeesPortion = 0L<Cent>
                    InterestPortion = 2_82L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeesRefundIfSettled = 0L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDate = Date(2024, 3, 29)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 53_30L<Cent>
                NetEffect = 53_30L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
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
                SettlementFigure = ValueSome 53_30L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }

        actual |> should equal expected
