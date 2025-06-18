namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module IllustrativeTests =

    let folder = "Illustrative"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    let interestCapExample: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    let quickActualPayments (days: int array) levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last
            |> Array.map (fun d -> d * 1<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed finalPayment ])
            rest
            |> Array.map (fun d -> d * 1<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed levelPayment ])
        |]
        |> Array.concat
        |> Array.rev
        |> Map.ofArray

    let quickExpectedFinalItem date offsetDay paymentValue window interestAdjustment interestPortion principalPortion =
        offsetDay,
        {
            OffsetDayType = OffsetDayType.EvaluationDay
            OffsetDate = date
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome paymentValue) ValueNone
            Window = window
            PaymentDue = paymentValue
            ActualPayments =
                Map [
                    0,
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentValue
                        Metadata = Map.empty
                    // // ScheduledPayments = Map.empty
                    }
                ]
            PaidBy = Map.empty
            GeneratedPayment = NoGeneratedPayment
            NetEffect = paymentValue
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            ActuarialInterest = interestAdjustment
            NewInterest = interestAdjustment
            NewCharges = [||]
            PrincipalPortion = principalPortion
            FeePortion = 0L<Cent>
            InterestPortion = interestPortion
            ChargesPortion = 0L<Cent>
            FeeRebate = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeeRebateIfSettled = 0L<Cent>
        }

    let parameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2025, 6, 30)
            StartDate = Date(2025, 3, 1)
            Principal = 400_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2025, 3, 31)
                    ScheduleLength = PaymentCount 4
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.AddOn
                StandardRate = Interest.Rate.Daily(Percent 0.8m)
                Cap = interestCapExample
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom
                AprPrecision = 3
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<OffsetDay>
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 3<OffsetDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    [<Fact>]
    let IllustrativeTest000 () =
        let title = "IllustrativeTest000"

        let description =
            "Borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) - all paid on time"

        let actualPayments =
            quickActualPayments [| 30; 60; 91; 121 |] 181_38L<Cent> 181_34L<Cent>

        let schedules = amortise parameters actualPayments

        Schedule.outputHtmlToFile folder title description parameters "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            121<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_34L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_34L<Cent>
                ActualPayments =
                    Map [
                        0,
                        {
                            ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_34L<Cent>
                            Metadata = Map.empty
                        // // ScheduledPayments = Map.empty
                        }
                    ]
                PaidBy = Map [ (121<OffsetDay>, 0), 181_34L<Cent> ]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_34L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                ActuarialInterest = 43_52.16m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 181_34L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 0L<Cent>
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
    let IllustrativeTest001 () =
        let title = "IllustrativeTest001"

        let description =
            """Based on borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) - missed first repayment and then paid
            before second repayment due date (30/04/2025); this shows that early missed payments not not accrue extra interest because the principal balance is not decreasing while there is a
            positive interest balance"""

        let actualPayments =
            quickActualPayments [| 59; 60; 91; 121 |] 181_38L<Cent> 181_34L<Cent>

        let schedules = amortise parameters actualPayments

        Schedule.outputHtmlToFile folder title description parameters "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            121<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_34L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_34L<Cent>
                ActualPayments =
                    Map [
                        0,
                        {
                            ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_34L<Cent>
                            Metadata = Map.empty
                        // // ScheduledPayments = Map.empty
                        }
                    ]
                PaidBy = Map.empty
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_34L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                ActuarialInterest = 43_52.16m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 181_34L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 0L<Cent>
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
    let IllustrativeTest002 () =
        let title = "IllustrativeTest002"

        let description =
            """Based on borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) 
            - missed first repayment and did not pay before second repayment due date (30/04/2025); this shows a final open balance due the extra day's interest"""

        let actualPayments =
            quickActualPayments [| 60; 61; 91; 121 |] 181_38L<Cent> 181_34L<Cent>

        let schedules = amortise parameters actualPayments

        Schedule.outputHtmlToFile folder title description parameters "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            121<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_34L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_34L<Cent>
                ActualPayments =
                    Map [
                        0,
                        {
                            ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_34L<Cent>
                            Metadata = Map.empty
                        // // ScheduledPayments = Map.empty
                        }
                    ]
                PaidBy = Map [ (121<OffsetDay>, 0), 181_34L<Cent> ]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_34L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                ActuarialInterest = 43_52.16m<Cent>
                NewInterest = 29.44m<Cent>
                NewCharges = [||]
                PrincipalPortion = 181_05L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 29L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 29L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 29L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let IllustrativeTest003 () =
        let title = "IllustrativeTest003"

        let description =
            """Based on borrowing £400 over 4 months with the loan being taken on 01/03/2025 and the first repayment date/day being 31/03/2025 (30 days) - missed third repayment and then paid before 
            fourth repayment due date (30/06/2025); this shows (in contrast to test 001) that extra interest is in fact accrued on late payment because when there is no interest balance, the principal
            balance remains higher than it would have been if the payment had been made on time"""

        let actualPayments =
            quickActualPayments [| 30; 60; 120; 121 |] 181_38L<Cent> 181_34L<Cent>

        let schedules = amortise parameters actualPayments

        Schedule.outputHtmlToFile folder title description parameters "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            121<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2025, 6, 30)
                Advances = [||]
                ScheduledPayment = {
                    Original = ValueSome 181_34L<Cent>
                    Rescheduled = ValueNone
                    PreviousRescheduled = [||]
                    Adjustment = 0L<Cent>
                    Metadata = Map.empty
                }
                Window = 4
                PaymentDue = 181_34L<Cent>
                ActualPayments =
                    Map [
                        0,
                        {
                            ActualPaymentStatus = ActualPaymentStatus.Confirmed 181_34L<Cent>
                            Metadata = Map.empty
                        // // ScheduledPayments = Map.empty
                        }
                    ]
                PaidBy = Map [ (121<OffsetDay>, 0), 181_34L<Cent> ]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 181_34L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                ActuarialInterest = 1_77.568m<Cent>
                NewInterest = 1_77.568m<Cent>
                NewCharges = [||]
                PrincipalPortion = 179_57L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 1_77L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 42_39L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 42_39L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected
