namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementTests =

    let folder = "Settlement"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open Quotes
    open UnitPeriod

    let interestCapExample: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    let parameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 3, 19)
            StartDate = Date(2023, 11, 28)
            Principal = 25000L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2023, 12, 22)
                    ScheduleLength = PaymentCount 4
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Daily(Percent 0.8m)
                Cap = interestCapExample
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Timeout = 0<DurationDay>
                Minimum = NoMinimumPayment
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
    let SettlementTest001 () =
        let title = "SettlementTest001"

        let description =
            "Final payment due on Friday: what would I pay if I paid it today?"

        let actualPayments =
            Map [
                24<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                55<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                86<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
            ]

        let actual =
            let quote = getQuote parameters actualPayments

            quote.Schedules
            |> Schedule.outputHtmlToFile folder title description parameters ""

            let scheduledItem =
                quote.Schedules.AmortisationSchedule.ScheduleItems |> Map.find 112<OffsetDay>

            quote.QuoteResult, scheduledItem

        let paymentQuote =
            PaymentQuote {
                PaymentValue = 98_52L<Cent>
                Apportionment = {
                    PrincipalPortion = 81_56L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 16_96L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDayType = OffsetDayType.SettlementDay
                OffsetDate = Date(2024, 3, 19)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 3
                PaymentDue = 0L<Cent>
                ActualPayments = Map.empty
                PaidBy = Map.empty
                GeneratedPayment = GeneratedValue 98_52L<Cent>
                NetEffect = 98_52L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 16_96.448m<Cent>
                NewInterest = 16_96.448m<Cent>
                NewCharges = [||]
                PrincipalPortion = 81_56L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 16_96L<Cent>
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

    [<Fact>]
    let SettlementTest002 () =
        let title = "SettlementTest002"

        let description =
            "Final payment due on Friday: what would I pay if I paid it one week too late?"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 3, 29)
        }

        let actualPayments =
            Map [
                24<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                55<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                86<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
            ]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            let scheduledItem =
                quote.Schedules.AmortisationSchedule.ScheduleItems |> Map.find 122<OffsetDay>

            quote.QuoteResult, scheduledItem

        let paymentQuote =
            PaymentQuote {
                PaymentValue = 105_04L<Cent>
                Apportionment = {
                    PrincipalPortion = 81_56L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 23_48L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDayType = OffsetDayType.SettlementDay
                OffsetDate = Date(2024, 3, 29)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = Map.empty
                PaidBy = Map.empty
                GeneratedPayment = GeneratedValue 105_04L<Cent>
                NetEffect = 105_04L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 4_56.736m<Cent>
                NewInterest = 4_56.736m<Cent>
                NewCharges = [||]
                PrincipalPortion = 81_56L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 23_48L<Cent>
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

    [<Fact>]
    let SettlementTest003 () =
        let title = "SettlementTest003"

        let description =
            "Final payment due on Friday: what if I pay £50 on Friday and the rest next week one week too late?"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2024, 3, 29)
        }

        let actualPayments =
            Map [
                24<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                55<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                86<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 100_53L<Cent> ]
                115<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 50_00L<Cent> ]
            ]

        let actual =
            let quote = getQuote p actualPayments

            quote.Schedules |> Schedule.outputHtmlToFile folder title description p ""

            let scheduledItem =
                quote.Schedules.AmortisationSchedule.ScheduleItems |> Map.find 122<OffsetDay>

            quote.QuoteResult, scheduledItem

        let paymentQuote =
            PaymentQuote {
                PaymentValue = 53_30L<Cent>
                Apportionment = {
                    PrincipalPortion = 50_48L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 2_82L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDayType = OffsetDayType.SettlementDay
                OffsetDate = Date(2024, 3, 29)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = Map.empty
                PaidBy = Map.empty
                GeneratedPayment = GeneratedValue 53_30L<Cent>
                NetEffect = 53_30L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 2_82.688m<Cent>
                NewInterest = 2_82.688m<Cent>
                NewCharges = [||]
                PrincipalPortion = 50_48L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 2_82L<Cent>
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
