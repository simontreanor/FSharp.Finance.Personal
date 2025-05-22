namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTests =

    let folder = "ActualPayment"

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
            |> Array.map (fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed finalPayment |])
            rest
            |> Array.map (fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed levelPayment |])
        |]
        |> Array.concat
        |> Array.rev
        |> Map.ofArray

    let quickExpectedFinalItem date offsetDay paymentValue interestAdjustment interestPortion principalPortion =
        offsetDay,
        {
            OffsetDayType = OffsetDayType.EvaluationDay
            OffsetDate = date
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome paymentValue) ValueNone
            Window = 5
            PaymentDue = paymentValue
            ActualPayments = [|
                {
                    ActualPaymentStatus = ActualPaymentStatus.Confirmed paymentValue
                    Metadata = Map.empty
                }
            |]
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

    let parameters1: Parameters = {
        Basic = {
            EvaluationDate = Date(2023, 3, 31)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
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
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = ValueNone
            ChargeConfig =
                Some {
                    ChargeTypes =
                        Map [
                            Charge.LatePayment,
                            {
                                Value = 10_00L<Cent>
                                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                                ChargeHolidays = [||]
                            }
                        ]
                }
            InterestConfig = {
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    [<Fact>]
    let ActualPaymentTest000 () =
        let title = "ActualPaymentTest000"

        let description =
            "Standard schedule with month-end payments from 4 days and paid off on time"

        let actualPayments =
            quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let schedules = amortise parameters1 actualPayments

        Schedule.outputHtmlToFile folder title description parameters1 "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            quickExpectedFinalItem
                (Date(2023, 3, 31))
                125<OffsetDay>
                456_84L<Cent>
                90_78.288m<Cent>
                90_78L<Cent>
                366_06L<Cent>

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest001 () =
        let title = "ActualPaymentTest001"

        let description =
            "Standard schedule with month-end payments from 32 days and paid off on time"

        let p = {
            parameters1 with
                Basic.StartDate = Date(2022, 10, 29)
        }

        let actualPayments =
            quickActualPayments [| 32; 63; 94; 122; 153 |] 556_05L<Cent> 556_00L<Cent>

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            quickExpectedFinalItem
                (Date(2023, 3, 31))
                153<OffsetDay>
                556_00L<Cent>
                110_48.896m<Cent>
                110_48L<Cent>
                445_52L<Cent>

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest002 () =
        let title = "ActualPaymentTest002"

        let description =
            "Standard schedule with mid-monthly payments from 14 days and paid off on time"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 3, 15)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            quickActualPayments [| 14; 44; 75; 106; 134 |] 491_53L<Cent> 491_53L<Cent>

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            quickExpectedFinalItem
                (Date(2023, 3, 15))
                134<OffsetDay>
                491_53L<Cent>
                89_95.392m<Cent>
                89_95L<Cent>
                401_58L<Cent>

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest003 () =
        let title = "ActualPaymentTest003"

        let description =
            "Made 2 payments on early repayment, then one single payment after the full balance is overdue"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 3, 21)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> 1193_95L<Cent>

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            140<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2023, 3, 21)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed 1193_95L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 1193_95L<Cent>
                PaymentStatus = ExtraPayment
                BalanceStatus = ClosedBalance
                ActuarialInterest = 26_75.760m<Cent>
                NewInterest = 26_75.760m<Cent>
                NewCharges = [||]
                PrincipalPortion = 557_45L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 606_50L<Cent>
                ChargesPortion = 30_00L<Cent>
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
    let ActualPaymentTest004 () =
        let title = "ActualPaymentTest004"

        let description =
            "Made 2 payments on early repayment, then one single overpayment after the full balance is overdue"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 3, 21)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            quickActualPayments [| 2; 4; 140 |] 491_53L<Cent> (491_53L<Cent> * 3L)

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            140<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2023, 3, 21)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed 1474_59L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 1474_59L<Cent>
                PaymentStatus = ExtraPayment
                BalanceStatus = RefundDue
                ActuarialInterest = 26_75.760m<Cent>
                NewInterest = 26_75.760m<Cent>
                NewCharges = [||]
                PrincipalPortion = 838_09L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 606_50L<Cent>
                ChargesPortion = 30_00L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = -280_64L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = -280_64L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest005 () =
        let title = "ActualPaymentTest005"

        let description =
            "Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 3, 24)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            Map [
                2<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                4<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                140<OffsetDay>, [| ActualPayment.quickConfirmed (491_53L<Cent> * 3L) |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed -280_64L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            143<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2023, 3, 24)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_64L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = -280_64L<Cent>
                PaymentStatus = Refunded
                BalanceStatus = ClosedBalance
                ActuarialInterest = 0m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = -280_64L<Cent>
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
    let ActualPaymentTest006 () =
        let title = "ActualPaymentTest006"
        let description = "0L<Cent>-day loan"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2022, 11, 1)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 1500_00L<Cent> |] ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.find 0<OffsetDay>

        let expected = {
            OffsetDayType = OffsetDayType.EvaluationDay
            OffsetDate = Date(2022, 11, 1)
            Advances = [| 1500_00L<Cent> |]
            ScheduledPayment = ScheduledPayment.zero
            Window = 0
            PaymentDue = 0L<Cent>
            ActualPayments = [|
                {
                    ActualPaymentStatus = ActualPaymentStatus.Confirmed 1500_00L<Cent>
                    Metadata = Map.empty
                }
            |]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 1500_00L<Cent>
            PaymentStatus = ExtraPayment
            BalanceStatus = ClosedBalance
            ActuarialInterest = 0m<Cent>
            NewInterest = 0m<Cent>
            NewCharges = [||]
            PrincipalPortion = 1500_00L<Cent>
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
    let ActualPaymentTest007 () =
        let title = "ActualPaymentTest007"

        let description =
            "Check that charge for late payment is not applied on scheduled payment date when payment has not yet been made"

        let startDate = Date(2024, 10, 1).AddDays -56

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2, startDate.AddDays 14)
                        ScheduleLength = PaymentCount 11
                    }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 243_86L<Cent> |]
                28<OffsetDay>, [| ActualPayment.quickConfirmed 243_86L<Cent> |]
                42<OffsetDay>, [| ActualPayment.quickConfirmed 243_86L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            154<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.OffsetDay
                OffsetDate = startDate.AddDays 154
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 243_66L<Cent>) ValueNone
                Window = 11
                PaymentDue = 243_66L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 243_66L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                ActuarialInterest = 24_54.144m<Cent>
                NewInterest = 24_54.144m<Cent>
                NewCharges = [||]
                PrincipalPortion = 219_12L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 24_54L<Cent>
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
    let ActualPaymentTest008 () =
        let title = "ActualPaymentTest008"

        let description =
            "Made 2 payments on early repayment, then one single overpayment after the full balance is overdue, and this is then refunded (with interest due to the customer on the negative balance)"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 3, 24)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
        }

        let actualPayments =
            Map [
                2<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                4<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                140<OffsetDay>, [| ActualPayment.quickConfirmed (491_53L<Cent> * 3L) |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed -280_83L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            143<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2023, 3, 24)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed -280_83L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = -280_83L<Cent>
                PaymentStatus = Refunded
                BalanceStatus = ClosedBalance
                ActuarialInterest = -18.45304110M<Cent>
                NewInterest = -18.45304110M<Cent>
                NewCharges = [||]
                PrincipalPortion = -280_64L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = -19L<Cent>
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
    let ActualPaymentTest009 () =
        let title = "ActualPaymentTest009"

        let description =
            "Underpayment made should show scheduled payment as net effect while in grace period"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 1, 18)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 400_00L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            134<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.OffsetDay
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
                Window = 5
                PaymentDue = 491_53L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 491_53L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                ActuarialInterest = 89_95.392m<Cent>
                NewInterest = 89_95.392m<Cent>
                NewCharges = [||]
                PrincipalPortion = 401_58L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 89_95L<Cent>
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
    let ActualPaymentTest010 () =
        let title = "ActualPaymentTest010"

        let description =
            "Underpayment made should show scheduled payment as underpayment after grace period has expired"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2023, 1, 19)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 491_53L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 400_00L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            134<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.OffsetDay
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
                Window = 5
                PaymentDue = 491_53L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 491_53L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = OpenBalance
                ActuarialInterest = 118_33.696m<Cent>
                NewInterest = 118_33.696m<Cent>
                NewCharges = [||]
                PrincipalPortion = 373_20L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 118_33L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 155_09L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 155_09L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest011 () =
        let title = "ActualPaymentTest011"
        let description = "Settled loan"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2034, 1, 31)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                        ScheduleLength = PaymentCount 5
                    }
        }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual = schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            4109<OffsetDay>,
            {
                OffsetDayType = OffsetDayType.EvaluationDay
                OffsetDate = Date(2034, 1, 31)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 0L<Cent>
                PaymentStatus = InformationOnly
                BalanceStatus = RefundDue
                ActuarialInterest = 0m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 0L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 0L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = -67_93L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = -67_93L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest012 () =
        let title = "ActualPaymentTest012"

        let description =
            "Scheduled payment total can be less than principal when early actual payments are made but net effect is never less"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2024, 2, 7)
                Basic.StartDate = Date(2024, 2, 2)
                Basic.Principal = 250_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2024, 2, 22)
                        ScheduleLength = PaymentCount 4
                    }
                Advanced.ChargeConfig = None
        }

        let actualPayments =
            Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent> |] ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sumBy _.NetEffect
            >= p.Basic.Principal

        let expected = true

        actual |> should equal expected

    let parameters2 = {
        parameters1 with
            Basic.FeeConfig =
                ValueSome {
                    FeeType =
                        Fee.FeeType.CabOrCsoFee
                        <| Amount.Percentage(Percent 154.47m, Restriction.NoLimit)
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
            Basic.InterestConfig = {
                parameters1.Basic.InterestConfig with
                    StandardRate = Interest.Rate.Annual <| Percent 9.95m
                    Cap = Interest.Cap.zero
                    AprMethod = Apr.CalculationMethod.UsActuarial 5
            }
            Advanced.FeeConfig =
                ValueSome {
                    SettlementRebate = Fee.SettlementRebate.ProRata
                }
    }

    [<Fact>]
    let ActualPaymentTest013 () =
        let title = "ActualPaymentTest013"
        let description = "Something TH spotted"

        let p = {
            parameters2 with
                Basic.EvaluationDate = Date(2024, 2, 13)
                Basic.StartDate = Date(2022, 4, 30)
                Basic.Principal = 2500_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(1, Date(2022, 5, 6))
                        ScheduleLength = PaymentCount 24
                    }
                Advanced.ChargeConfig = None
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                23<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                31<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                38<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                42<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                58<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                67<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                73<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                79<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                86<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                93<OffsetDay>, [| ActualPayment.quickConfirmed 271_37L<Cent> |]
                100<OffsetDay>, [| ActualPayment.quickConfirmed 276_37L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                115<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                122<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                129<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                137<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                143<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                149<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                156<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                166<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                171<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                177<OffsetDay>, [| ActualPayment.quickConfirmed 278_38L<Cent> |]
                185<OffsetDay>, [| ActualPayment.quickConfirmed 278_33L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -61_27L<Cent>

        let expected = true
        actual |> should equal expected

    let parameters3 = {
        parameters2 with
            Basic.EvaluationDate = Date(2024, 2, 13)
            Basic.StartDate = Date(2022, 4, 30)
            Basic.Principal = 2500_00L<Cent>
            Basic.ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(1, Date(2022, 5, 6))
                    ScheduleLength = PaymentCount 24
                }
            Advanced.ChargeConfig = None
    }

    [<Fact>]
    let ActualPaymentTest014 () =
        let title = "ActualPaymentTest014"
        let description = "Large overpayment should not result in runaway fee rebates"

        let actualPayments =
            Map [ 13<OffsetDay>, [| ActualPayment.quickConfirmed 5000_00L<Cent> |] ]

        let schedules = amortise parameters3 actualPayments

        Schedule.outputHtmlToFile folder title description parameters3 "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2176_85L<Cent>

        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest015 () =
        let title = "ActualPaymentTest015"

        let description =
            "Large overpayment should not result in runaway fee rebates (2 actual payments)"

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 5000_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let schedules = amortise parameters3 actualPayments

        Schedule.outputHtmlToFile folder title description parameters3 "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -2676_85L<Cent>

        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest016 () =
        let title = "ActualPaymentTest016"

        let description =
            "Large overpayment should not result in runaway fee rebates (3 actual payments)"

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 5000_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                27<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let schedules = amortise parameters3 actualPayments

        Schedule.outputHtmlToFile folder title description parameters3 "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = RefundDue && si.PrincipalBalance = -3176_85L<Cent>

        let expected = true
        actual |> should equal expected

    let parameters4 = {
        parameters3 with
            Basic.EvaluationDate = Date(2024, 1, 30)
            Basic.StartDate = Date(2024, 1, 1)
            Basic.Principal = 2500_00L<Cent>
            Basic.ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(1, Date(2024, 1, 14))
                    ScheduleLength = PaymentCount 24
                }
            Advanced.ChargeConfig = None
    }

    [<Fact>]
    let ActualPaymentTest017 () =
        let title = "ActualPaymentTest017"
        let description = "Pending payments should only apply if not timed out"

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 271_89L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
                27<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
            ]

        let schedules = amortise parameters4 actualPayments

        Schedule.outputHtmlToFile folder title description parameters4 "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 111_50L<Cent>

        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest018 () =
        let title = "ActualPaymentTest018"
        let description = "Pending payments should only apply if not timed out"

        let p = {
            parameters4 with
                Basic.EvaluationDate = Date(2024, 2, 1)
                Advanced.ChargeConfig = None
        }

        let actualPayments =
            Map [
                13<OffsetDay>, [| ActualPayment.quickConfirmed 271_89L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
                27<OffsetDay>, [| ActualPayment.quickPending 271_89L<Cent> |]
            ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.PrincipalBalance = 222_71L<Cent>

        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest019 () =
        let title = "ActualPaymentTest019"
        let description = "Generated settlement figure is correct"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2024, 3, 2)
                Basic.StartDate = Date(2023, 8, 20)
                Basic.Principal = 250_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2023, 9, 5)
                        ScheduleLength = PaymentCount 4
                    }
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.ChargeConfig = None
                Advanced.SettlementDay = SettlementDay.SettlementOnEvaluationDay
        }

        let actualPayments =
            Map [
                16<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
                46<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
                77<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
                107<OffsetDay>, [| ActualPayment.quickConfirmed 116_00L<Cent> |]
            ]

        let schedules = actualPayments |> amortise p

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) ->
                si.BalanceStatus = ClosedBalance
                && si.GeneratedPayment = GeneratedValue -119_88L<Cent>

        let expected = true
        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTest020 () =
        let title = "ActualPaymentTest020"
        let description = "Late payment"

        let p = {
            parameters1 with
                Basic.EvaluationDate = Date(2024, 7, 3)
                Basic.StartDate = Date(2024, 4, 12)
                Basic.Principal = 100_00L<Cent>
                Basic.ScheduleConfig =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Monthly(1, 2024, 4, 19)
                            PaymentCount = 4
                            PaymentValue = 35_48L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.ChargeConfig = None
        }

        let actualPayments =
            Map [ 28<OffsetDay>, [| ActualPayment.quickConfirmed 35_48L<Cent> |] ]

        let schedules = amortise p actualPayments

        Schedule.outputHtmlToFile folder title description p "" schedules

        let actual =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> fun (_, si) -> si.BalanceStatus = OpenBalance && si.SettlementFigure = 100_11L<Cent>

        let expected = true
        actual |> should equal expected
