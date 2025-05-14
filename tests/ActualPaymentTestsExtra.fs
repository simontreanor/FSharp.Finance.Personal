namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTestsExtra =

    let folder = "ActualPayment"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open Refinancing
    open UnitPeriod

    let interestCapExample: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    /// creates an array of actual payments made on time and in full according to an array of scheduled payments
    let allPaidOnTime (basicItems: BasicItem array) =
        basicItems
        |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
        |> Array.map (fun si ->
            si.Day, [| ActualPayment.quickConfirmed <| ScheduledPayment.total si.ScheduledPayment |]
        )
        |> Map.ofArray

    let parameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2023, 12, 1)
            StartDate = Date(2023, 7, 23)
            Principal = 800_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2023, 8, 1)
                    ScheduleLength = PaymentCount 5
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig =
                ValueSome {
                    FeeType = Fee.FeeType.CabOrCsoFee(Amount.Percentage(Percent 150m, Restriction.NoLimit))
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual <| Percent 9.95m
                Cap = Interest.Cap.zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                Rounding = RoundDown
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig =
                ValueSome {
                    SettlementRebate = Fee.SettlementRebate.ProRata
                }
            ChargeConfig =
                Some {
                    ChargeTypes =
                        Map [
                            Charge.InsufficientFunds,
                            {
                                Value = 7_50L<Cent>
                                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                                ChargeHolidays = [||]
                            }
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
    let ActualPaymentTestExtra000 () =
        let title = "ActualPaymentTestExtra000"
        let description = "Actuarial schedule fully settled on time"

        let actual =
            let schedule = calculateBasicSchedule parameters.Basic
            let scheduleItems = schedule.Items
            let actualPayments = scheduleItems |> allPaidOnTime
            let schedules = amortise parameters actualPayments
            schedules |> Schedule.outputHtmlToFile folder title description parameters ""
            schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            131<OffsetDay>,
            {
                OffsetDate = Date(2023, 12, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 407_64L<Cent>) ValueNone
                Window = 5
                PaymentDue = 407_64L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed 407_64L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 407_64L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                ActuarialInterest = 3_30.67257534m<Cent>
                NewInterest = 3_30.67257534m<Cent>
                NewCharges = [||]
                PrincipalPortion = 161_76L<Cent>
                FeePortion = 242_58L<Cent>
                InterestPortion = 3_30L<Cent>
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
    let ActualPaymentTestExtra001 () =
        let title = "ActualPaymentTestExtra001"

        let description =
            "Schedule with a payment on day 0L<Cent>, seen from a date before scheduled payments are due to start"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2022, 3, 25)
                Basic.StartDate = Date(2022, 3, 8)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2, Date(2022, 3, 26))
                        ScheduleLength = PaymentCount 12
                    }
        }

        let actual =
            let actualPayments =
                Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |] ]

            let schedules = amortise p actualPayments
            schedules |> Schedule.outputHtmlToFile folder title description p ""
            schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            172<OffsetDay>,
            {
                OffsetDate = Date(2022, 8, 27)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 170_90L<Cent>) ValueNone
                Window = 12
                PaymentDue = 170_04L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 170_04L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                ActuarialInterest = 64.65046575m<Cent>
                NewInterest = 64.65046575m<Cent>
                NewCharges = [||]
                PrincipalPortion = 67_79L<Cent>
                FeePortion = 101_61L<Cent>
                InterestPortion = 64L<Cent>
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
    let ActualPaymentTestExtra002 () =
        let title = "ActualPaymentTestExtra002"

        let description =
            "Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, seen from a date after the original settlement date, showing the effect of projected small payments until paid off"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2022, 8, 31)
                Basic.StartDate = Date(2022, 3, 8)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2, Date(2022, 3, 26))
                        ScheduleLength = PaymentCount 12
                    }
        }

        let originalFinalPaymentDay =
            generatePaymentMap p.Basic.StartDate p.Basic.ScheduleConfig
            |> Map.keys
            |> Seq.toArray
            |> Array.tryLast
            |> Option.defaultValue 0<OffsetDay>

        let originalFinalPaymentDay' =
            (int originalFinalPaymentDay
             - int (p.Basic.EvaluationDate - p.Basic.StartDate).Days)
            * 1<OffsetDay>

        let actual =
            let actualPayments =
                Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |] ]

            let rescheduleDay = p.Basic.EvaluationDate |> OffsetDay.fromDate p.Basic.StartDate

            let rp: RescheduleParameters = {
                FeeSettlementRebate = Fee.SettlementRebate.ProRataRescheduled originalFinalPaymentDay'
                PaymentSchedule =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Config.Weekly(2, Date(2022, 9, 1))
                            PaymentCount = 155
                            PaymentValue = 20_00L<Cent>
                            ScheduleType = ScheduleType.Rescheduled rescheduleDay
                        }
                    |]
                RateOnNegativeBalance = Interest.Rate.Zero
                PromotionalInterestRates = [||]
                SettlementDay = SettlementDay.NoSettlement
            }

            let schedules = reschedule p rp actualPayments

            schedules.NewSchedules
            |> Schedule.outputHtmlToFile folder title description p (RescheduleParameters.toHtmlTable rp)

            schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            1969<OffsetDay>,
            {
                OffsetDate = Date(2027, 7, 29)
                Advances = [||]
                ScheduledPayment =
                    ScheduledPayment.quick
                        ValueNone
                        (ValueSome {
                            Value = 20_00L<Cent>
                            RescheduleDay = 176<OffsetDay>
                        })
                Window = 141
                PaymentDue = 9_80L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 9_80L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                ActuarialInterest = 3.72866027m<Cent>
                NewInterest = 3.72866027m<Cent>
                NewCharges = [||]
                PrincipalPortion = 4_39L<Cent>
                FeePortion = 5_38L<Cent>
                InterestPortion = 3L<Cent>
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
    let ActualPaymentTestExtra003 () =
        let title = "ActualPaymentTestExtra003"
        let description = "never settles down"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2026, 8, 27)
                Basic.StartDate = Date(2023, 11, 6)
                Basic.Principal = 800_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(8, Date(2023, 11, 23))
                        ScheduleLength = PaymentCount 19
                    }
                Basic.FeeConfig =
                    ValueSome {
                        FeeType = Fee.FeeType.CabOrCsoFee(Amount.Percentage(Percent 164m, Restriction.NoLimit))
                        Rounding = RoundDown
                        FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                    }
                Basic.InterestConfig.Method = Interest.Method.Actuarial
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily(Percent 0.12m)
                Basic.InterestConfig.Cap = {
                    TotalAmount = Amount.Simple 500_00L<Cent>
                    DailyAmount = Amount.Unlimited
                }
                Advanced.FeeConfig =
                    ValueSome {
                        SettlementRebate = Fee.SettlementRebate.Zero
                    }
                Advanced.InterestConfig.InitialGracePeriod = 7<DurationDay>
        }

        let actual =
            let schedule = calculateBasicSchedule p.Basic
            let scheduleItems = schedule.Items
            let actualPayments = scheduleItems |> allPaidOnTime
            let schedules = amortise p actualPayments
            schedules |> Schedule.outputHtmlToFile folder title description p ""
            schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            1025<OffsetDay>,
            {
                OffsetDate = Date(2026, 8, 27)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 137_36L<Cent>) ValueNone
                Window = 19
                PaymentDue = 137_36L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed 137_36L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 137_36L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                ActuarialInterest = 0m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 52_14L<Cent>
                FeePortion = 85_22L<Cent>
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
    let ActualPaymentTestExtra004 () =
        let title = "ActualPaymentTestExtra004"
        let description = "large negative payment"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2023, 12, 11)
                Basic.StartDate = Date(2022, 9, 11)
                Basic.Principal = 200_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2022, 9, 15)
                        ScheduleLength = PaymentCount 7
                    }
                Basic.FeeConfig = ValueNone
                Basic.InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Daily(Percent 0.8m)
                    Cap = interestCapExample
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                }
                Advanced.FeeConfig = ValueNone
                Advanced.ChargeConfig =
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
        }

        let actual =
            let schedule = calculateBasicSchedule p.Basic
            let scheduleItems = schedule.Items
            let actualPayments = scheduleItems |> allPaidOnTime
            let schedules = amortise p actualPayments
            schedules |> Schedule.outputHtmlToFile folder title description p ""
            schedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            185<OffsetDay>,
            {
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 51_53L<Cent>) ValueNone
                Window = 7
                PaymentDue = 51_53L<Cent>
                ActualPayments = [|
                    {
                        ActualPaymentStatus = ActualPaymentStatus.Confirmed 51_53L<Cent>
                        Metadata = Map.empty
                    }
                |]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 51_53L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = ClosedBalance
                ActuarialInterest = 9_43.040m<Cent>
                NewInterest = 9_43.040m<Cent>
                NewCharges = [||]
                PrincipalPortion = 42_10L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 9_43L<Cent>
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
    let ActualPaymentTestExtra005 () =
        let title = "ActualPaymentTestExtra005"

        let description =
            "Schedule with a payment on day 0L<Cent>, seen from a date after the first unpaid scheduled payment, but within late-payment grace period"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2022, 4, 1)
                Basic.StartDate = Date(2022, 3, 8)
                Basic.Principal = 800_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2, Date(2022, 3, 26))
                        ScheduleLength = PaymentCount 12
                    }
                Advanced.PaymentConfig.Timeout = 7<DurationDay>
        }

        let actual =
            let actualPayments =
                Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |] ]

            let schedules = amortise p actualPayments
            schedules |> Schedule.outputHtmlToFile folder title description p ""
            schedules.AmortisationSchedule.ScheduleItems |> Map.find 144<OffsetDay>

        let expected = {
            OffsetDate = Date(2022, 7, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.quick (ValueSome 171_02L<Cent>) ValueNone
            Window = 10
            PaymentDue = 142_40L<Cent>
            ActualPayments = [||]
            GeneratedPayment = NoGeneratedPayment
            NetEffect = 142_40L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            ActuarialInterest = 1_28.41170136m<Cent>
            NewInterest = 1_28.41170136m<Cent>
            NewCharges = [||]
            PrincipalPortion = 134_62L<Cent>
            FeePortion = 6_50L<Cent>
            InterestPortion = 1_28L<Cent>
            ChargesPortion = 0L<Cent>
            FeeRebate = 195_35L<Cent>
            PrincipalBalance = 0L<Cent>
            FeeBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeeRebateIfSettled = 195_35L<Cent>
        }

        actual |> should equal expected

    [<Fact>]
    let ActualPaymentTestExtra006 () =
        let title = "ActualPaymentTestExtra006"

        let description =
            "Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fee rolled over)"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2022, 8, 31)
                Basic.StartDate = Date(2022, 3, 8)
                Basic.Principal = 800_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2, Date(2022, 3, 26))
                        ScheduleLength = PaymentCount 12
                    }
        }

        let originalFinalPaymentDay =
            generatePaymentMap p.Basic.StartDate p.Basic.ScheduleConfig
            |> Map.keys
            |> Seq.toArray
            |> Array.tryLast
            |> Option.defaultValue 0<OffsetDay>

        let originalFinalPaymentDay' =
            (int originalFinalPaymentDay
             - int (p.Basic.EvaluationDate - p.Basic.StartDate).Days)
            * 1<OffsetDay>

        let actual =
            let actualPayments =
                Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |] ]

            let rp: RolloverParameters = {
                OriginalFinalPaymentDay = originalFinalPaymentDay'
                PaymentSchedule =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Weekly(2, Date(2022, 9, 1))
                            PaymentCount = 155
                            PaymentValue = 20_00L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
                InterestConfig = p.Basic.InterestConfig
                PaymentConfig = p.Basic.PaymentConfig
                FeeHandling = Fee.FeeHandling.CarryOverAsIs
            }

            let schedules = rollOver p rp actualPayments

            schedules.NewSchedules
            |> Schedule.outputHtmlToFile folder title description p (RolloverParameters.toHtmlTable rp)

            schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            1793<OffsetDay>,
            {
                OffsetDate = Date(2027, 7, 29)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 20_00L<Cent>) ValueNone
                Window = 129
                PaymentDue = 18_71L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 18_71L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                ActuarialInterest = 7.11384109m<Cent>
                NewInterest = 7.11384109m<Cent>
                NewCharges = [||]
                PrincipalPortion = 9_26L<Cent>
                FeePortion = 9_38L<Cent>
                InterestPortion = 7L<Cent>
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
    let ActualPaymentTestExtra007 () =
        let title = "ActualPaymentTestExtra007"

        let description =
            "Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fee not rolled over)"

        let p = {
            parameters with
                Basic.EvaluationDate = Date(2022, 8, 31)
                Basic.StartDate = Date(2022, 3, 8)
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Weekly(2, Date(2022, 3, 26))
                        ScheduleLength = PaymentCount 12
                    }
        }

        let originalFinalPaymentDay =
            generatePaymentMap p.Basic.StartDate p.Basic.ScheduleConfig
            |> Map.keys
            |> Seq.toArray
            |> Array.tryLast
            |> Option.defaultValue 0<OffsetDay>

        let originalFinalPaymentDay' =
            (int originalFinalPaymentDay
             - int (p.Basic.EvaluationDate - p.Basic.StartDate).Days)
            * 1<OffsetDay>

        let actual =
            let actualPayments =
                Map [ 0<OffsetDay>, [| ActualPayment.quickConfirmed 166_60L<Cent> |] ]

            let rp: RolloverParameters = {
                OriginalFinalPaymentDay = originalFinalPaymentDay'
                PaymentSchedule =
                    FixedSchedules [|
                        {
                            UnitPeriodConfig = Config.Weekly(2, Date(2022, 9, 1))
                            PaymentCount = 155
                            PaymentValue = 20_00L<Cent>
                            ScheduleType = ScheduleType.Original
                        }
                    |]
                InterestConfig = p.Basic.InterestConfig
                PaymentConfig = p.Basic.PaymentConfig
                FeeHandling = Fee.FeeHandling.CapitaliseAsPrincipal
            }

            let schedules = rollOver p rp actualPayments

            schedules.NewSchedules
            |> Schedule.outputHtmlToFile folder title description p (RolloverParameters.toHtmlTable rp)

            schedules.NewSchedules.AmortisationSchedule.ScheduleItems |> Map.maxKeyValue

        let expected =
            1793<OffsetDay>,
            {
                OffsetDate = Date(2027, 7, 29)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 20_00L<Cent>) ValueNone
                Window = 129
                PaymentDue = 18_71L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 18_71L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                ActuarialInterest = 7.11384109m<Cent>
                NewInterest = 7.11384109m<Cent>
                NewCharges = [||]
                PrincipalPortion = 18_64L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 7L<Cent>
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
