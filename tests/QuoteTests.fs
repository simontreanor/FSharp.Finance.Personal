namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module QuoteTests =

    let folder = "Quote"

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open Quotes
    open UnitPeriod

    let interestCapExample : Interest.Cap = {
        TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
    }

    let parameters1 : Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 9, 28)
            StartDate = Date(2024, 8, 2)
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = Weekly(2, Date(2024, 8, 17))
                ScheduleLength = PaymentCount 11
            }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueSome {
                FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 189.47m, Restriction.NoLimit))
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
            FeeConfig = ValueSome {
                SettlementRebate = Fee.SettlementRebate.ProRata
            }
            ChargeConfig = Some {
                ChargeTypes = Map [
                    Charge.InsufficientFunds, {
                        Value = 7_50L<Cent>
                        ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                        ChargeHolidays = [||]
                    }
                    Charge.LatePayment, {
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
            SettlementDay = SettlementDay.SettlementOnEvaluationDay
            TrimEnd = true
        }
    }

    [<Fact>]
    let QuoteTest000 () =
        let title = "QuoteTest000"
        let description = "Settlement falling on a scheduled payment date"

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i -> i * 1<OffsetDay>, [| ActualPayment.quickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            let quote = getQuote parameters1 actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description parameters1
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 57<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote = 
            PaymentQuote {
                PaymentValue = 1969_72L<Cent>
                Apportionment = {
                    PrincipalPortion = 1175_80L<Cent>
                    FeePortion = 790_21L<Cent>
                    InterestPortion = 3_71L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 1437_53L<Cent>
            }

        let expected =
            paymentQuote,
            {
                OffsetDate = (Date(2024, 10, 1).AddDays -3)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 323_15L<Cent>) ValueNone
                Window = 4
                PaymentDue = 323_15L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 1969_72L<Cent>
                NetEffect = 1969_72L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 3_71.12573150m<Cent>
                NewInterest = 3_71.12573150m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeePortion = 790_21L<Cent>
                InterestPortion = 3_71L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 1437_53L<Cent>
                PrincipalBalance = 0L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 0L<Cent>
                FeeRebateIfSettled = 1437_53L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest001 () =
        let title = "QuoteTest001"
        let description = "Settlement not falling on a scheduled payment date"
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, startDate.AddDays 15)
                    ScheduleLength = PaymentCount 11
                }
            }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i -> (i * 1<OffsetDay>), [| ActualPayment.quickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 60<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 2016_50L<Cent>
                Apportionment = {
                    PrincipalPortion = 1175_80L<Cent>
                    FeePortion = 834_21L<Cent>
                    InterestPortion = 6_49L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 1393_53L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 2016_50L<Cent>
                NetEffect = 2016_50L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 2_78.34429863m<Cent>
                NewInterest = 2_78.34429863m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeePortion = 834_21L<Cent>
                InterestPortion = 6_49L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 1393_53L<Cent>
                PrincipalBalance = 0L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 0L<Cent>
                FeeRebateIfSettled = 1393_53L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest002 () =
        let title = "QuoteTest002"
        let description = "Settlement not falling on a scheduled payment date but having an actual payment already made on the same day"
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, startDate.AddDays 15)
                    ScheduleLength = PaymentCount 11
                }
            }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i -> (i * 1<OffsetDay>), [| ActualPayment.quickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 60<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 1991_50L<Cent>
                Apportionment = {
                    PrincipalPortion = 1175_80L<Cent>
                    FeePortion = 815_70L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 1393_53L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 25_00L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = GeneratedValue 1991_50L<Cent>
                NetEffect = 2016_50L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 2_78.34429863m<Cent>
                NewInterest = 2_78.34429863m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeePortion = 834_21L<Cent>
                InterestPortion = 6_49L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 1393_53L<Cent>
                PrincipalBalance = 0L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 0L<Cent>
                FeeRebateIfSettled = 1393_53L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest003 () =
        let title = "QuoteTest003"
        let description = "Settlement within interest grace period should not accrue interest"
        let startDate = Date(2024, 10, 1).AddDays -3

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> Monthly(1, sd.Year, sd.Month, sd.Day * 1))
                    ScheduleLength = PaymentCount 5
                }
                Basic.FeeConfig = ValueNone
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Basic.InterestConfig.Cap = { TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit); DailyAmount = Amount.Unlimited }
                Basic.InterestConfig.AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                Advanced.FeeConfig = ValueNone
            }

        let actualPayments = Map.empty

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 3<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 1200_00L<Cent>
                Apportionment = {
                    PrincipalPortion = 1200_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 0
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 1200_00L<Cent>
                NetEffect = 1200_00L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 0m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1200_00L<Cent>
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
    let QuoteTest004 () =
        let title = "QuoteTest004"
        let description = "Settlement just outside interest grace period should accrue interest"
        let startDate = Date(2024, 10, 1).AddDays -4

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> Monthly(1, sd.Year, sd.Month, sd.Day * 1))
                    ScheduleLength = PaymentCount 5
                }
                Basic.FeeConfig = ValueNone
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Basic.InterestConfig.Cap = { TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit); DailyAmount = Amount.Unlimited }
                Basic.InterestConfig.AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                Advanced.FeeConfig = ValueNone
            }

        let actualPayments = Map.empty

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 4<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 1238_40L<Cent>
                Apportionment = {
                    PrincipalPortion = 1200_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 38_40L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 0
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 1238_40L<Cent>
                NetEffect = 1238_40L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 38_40m<Cent>
                NewInterest = 38_40m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1200_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 38_40L<Cent>
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
    let QuoteTest005 () =
        let title = "QuoteTest005"
        let description = "Settlement when fee is due in full"
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, startDate.AddDays 15)
                    ScheduleLength = PaymentCount 11
                }
                Advanced.FeeConfig = ValueSome { SettlementRebate = Fee.SettlementRebate.Zero }
            }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                i * 1<OffsetDay>, [| ActualPayment.quickConfirmed 25_00L<Cent> |]
            )
            |> Map.ofArray

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 60<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 3410_03L<Cent>
                Apportionment = {
                    PrincipalPortion = 1175_80L<Cent>
                    FeePortion = 2227_74L<Cent>
                    InterestPortion = 6_49L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 3410_03L<Cent>
                NetEffect = 3410_03L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 2_78.34429863m<Cent>
                NewInterest = 2_78.34429863m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeePortion = 2227_74L<Cent>
                InterestPortion = 6_49L<Cent>
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
    let QuoteTest006 () =
        let title = "QuoteTest006"
        let description = "Get next scheduled payment"
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let p =
            { parameters1 with
                Basic.StartDate = startDate
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, startDate.AddDays 15)
                    ScheduleLength = PaymentCount 11
                }
                Advanced.FeeConfig = ValueSome { SettlementRebate = Fee.SettlementRebate.Zero }
                Advanced.SettlementDay = SettlementDay.NoSettlement
            }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                i * 1<OffsetDay>, [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 323_15L<Cent>; Metadata = Map.empty } |]
            )
            |> Map.ofArray

        let actual =
            let schedules = amortise p actualPayments
            schedules |> Schedule.outputHtmlToFile folder title description p
            schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.find(fun si -> ScheduledPayment.isSome si.ScheduledPayment && si.OffsetDate >= p.Basic.EvaluationDate)

        let expected =
            {
                OffsetDate = startDate.AddDays 71
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 323_15L<Cent>) ValueNone
                Window = 5
                PaymentDue = 323_15L<Cent>
                ActualPayments = [||]
                GeneratedPayment = NoGeneratedPayment
                NetEffect = 323_15L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = OpenBalance
                ActuarialInterest = 7_68.32100821m<Cent>
                NewInterest = 7_68.32100821m<Cent>
                NewCharges = [||]
                PrincipalPortion = 108_25L<Cent>
                FeePortion = 205_13L<Cent>
                InterestPortion = 9_77L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 0L<Cent>
                PrincipalBalance = 776_92L<Cent>
                FeeBalance = 1471_94L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 2248_86L<Cent>
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest007 () =
        let title = "QuoteTest007"
        let description = "Get payment to cover all overdue amounts"
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2024, 10, 1)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, startDate.AddDays 15)
                    ScheduleLength = PaymentCount 11
                }
                Basic.PaymentConfig.Rounding = RoundDown
            }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                i * 1<OffsetDay>, [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 323_15L<Cent>; Metadata = Map.empty } |]
            )
            |> Map.ofArray

        let actual =
            let amortisationSchedule = amortise p actualPayments
            amortisationSchedule |> Schedule.outputHtmlToFile folder title description p
            // let quoteResult =
            //     quote
            //     |> ValueOption.map(fun q ->
            //         let paymentQuote = q.RevisedSchedule |> fst |> _.ScheduleItems |> Array.filter(fun si -> match si.PaymentStatus with MissedPayment -> si.ScheduledPayment.Total | PaidLaterOwing plo -> plo | _ -> 0L<Cent>))
            //         PaymentQuote (GeneratedPayment.Total si.GeneratedPayment, si.PrincipalPortion, si.FeePortion, si.InterestPortion, si.ChargesPortion, si.FeeRebateIfSettled)
            //     )
            ValueNone // not a single item to be returned, as the result is a sum of items

        let expected =
            ValueNone //ValueSome <| PaymentQuote (GeneratedPayment.Total si.GeneratedPayment, si.PrincipalPortion, si.FeePortion, si.InterestPortion, si.ChargesPortion, si.FeeRebateIfSettled),

        actual |> should equal expected

    [<Fact>]
    let QuoteTest008 () =
        let title = "QuoteTest008"
        let description = "Verified example"
        let startDate = Date(2023, 6, 23)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2023, 12, 21)
                Basic.StartDate = startDate
                Basic.Principal = 500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, Date(2023, 6, 30))
                    ScheduleLength = PaymentCount 10
                }
                Basic.FeeConfig = ValueSome {
                    FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit))
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
                Basic.InterestConfig.AprMethod = Apr.CalculationMethod.UsActuarial 5
                Advanced.ChargeConfig = None
            }

        let actualPayments = Map.empty

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 181<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 1311_67L<Cent>
                Apportionment = {
                    PrincipalPortion = 500_00L<Cent>
                    FeePortion = 750_00L<Cent>
                    InterestPortion = 61_67L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = startDate.AddDays 181
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 10
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 1311_67L<Cent>
                NetEffect = 1311_67L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 16_35.61643835m<Cent>
                NewInterest = 16_35.61643835m<Cent>
                NewCharges = [||]
                PrincipalPortion = 500_00L<Cent>
                FeePortion = 750_00L<Cent>
                InterestPortion = 61_67L<Cent>
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
    let QuoteTest009 () =
        let title = "QuoteTest009"
        let description = "Verified example"
        let startDate = Date(2022, 11, 28)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2023, 12, 21)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, Date(2022, 12, 12))
                    ScheduleLength = PaymentCount 11
                }
                Basic.FeeConfig = ValueSome {
                    FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit))
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
                Basic.InterestConfig.AprMethod = Apr.CalculationMethod.UsActuarial 5
                Advanced.ChargeConfig = None
            }

        let actualPayments =
            Map [
                70<OffsetDay>, [| ActualPayment.quickConfirmed 272_84L<Cent> |]
                84<OffsetDay>, [| ActualPayment.quickConfirmed 272_84L<Cent>; ActualPayment.quickConfirmed 272_84L<Cent> |]
                85<OffsetDay>, [| ActualPayment.quickConfirmed 272_84L<Cent> |]
                98<OffsetDay>, [| ActualPayment.quickConfirmed 272_84L<Cent> |]
                112<OffsetDay>, [| ActualPayment.quickConfirmed 272_84L<Cent> |]
                126<OffsetDay>, [| ActualPayment.quickConfirmed 272_84L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 388<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 1261_73L<Cent>
                Apportionment = {
                    PrincipalPortion = 471_07L<Cent>
                    FeePortion = 706_56L<Cent>
                    InterestPortion = 84_10L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = startDate.AddDays 388
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 11
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 1261_73L<Cent>
                NetEffect = 1261_73L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 75_11.98884657m<Cent>
                NewInterest = 75_11.98884657m<Cent>
                NewCharges = [||]
                PrincipalPortion = 471_07L<Cent>
                FeePortion = 706_56L<Cent>
                InterestPortion = 84_10L<Cent>
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
    let QuoteTest010 () =
        let title = "QuoteTest010"
        let description = "When settling a loan with 3-day late-payment grace period, scheduled payments within the grace period should be treated as missed payments, otherwise the quote balance is too low"
        let startDate = Date(2022, 11, 28)

        let p =
            { parameters1 with
                Basic.EvaluationDate = Date(2023, 2, 8)
                Basic.StartDate = startDate
                Basic.Principal = 1200_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, Date(2022, 12, 12))
                    ScheduleLength = PaymentCount 11
                }
                Basic.FeeConfig = ValueSome {
                    FeeType = Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, Restriction.NoLimit))
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
                Basic.InterestConfig.AprMethod = Apr.CalculationMethod.UsActuarial 5
                Advanced.ChargeConfig = None
            }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 279_01L<Cent> |]
                28<OffsetDay>, [| ActualPayment.quickConfirmed 279_01L<Cent> |]
                42<OffsetDay>, [| ActualPayment.quickConfirmed 279_01L<Cent> |]
                56<OffsetDay>, [| ActualPayment.quickConfirmed 279_01L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 72<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 973_53L<Cent>
                Apportionment = {
                    PrincipalPortion = 769_46L<Cent>
                    FeePortion = 195_68L<Cent>
                    InterestPortion = 8_39L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 958_45L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = startDate.AddDays 72
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 973_53L<Cent>
                NetEffect = 973_53L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 1_04.87518082m<Cent>
                NewInterest = 1_04.87518082m<Cent>
                NewCharges = [||]
                PrincipalPortion = 769_46L<Cent>
                FeePortion = 195_68L<Cent>
                InterestPortion = 8_39L<Cent>
                ChargesPortion = 0L<Cent>
                FeeRebate = 958_45L<Cent>
                PrincipalBalance = 0L<Cent>
                FeeBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 0L<Cent>
                FeeRebateIfSettled = 958_45L<Cent>
            }

        actual |> should equal expected

    let parameters2 =
            { parameters1 with
                Basic.FeeConfig = ValueNone
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.8m
                Basic.InterestConfig.Cap = interestCapExample
                Basic.InterestConfig.AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                Advanced.FeeConfig = ValueNone
                Advanced.ChargeConfig = None
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
            }

    [<Fact>]
    let QuoteTest011 () =
        let title = "QuoteTest011"
        let description = "Settlement figure should not be lower than principal"
        let startDate = Date(2024, 1, 29)

        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 2, 28)
                Basic.StartDate = startDate
                Basic.Principal = 400_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2024, 2, 28)
                    ScheduleLength = PaymentCount 4
                }
                Basic.InterestConfig.StandardRate = Interest.Rate.Daily <| Percent 0.798m
                Advanced.InterestConfig.InitialGracePeriod = 1<DurationDay>
            }

        let actualPayments = Map.empty

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 30<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 495_76L<Cent>
                Apportionment = {
                    PrincipalPortion = 400_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 95_76L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = startDate.AddDays 30
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 165_90L<Cent>) ValueNone
                Window = 1
                PaymentDue = 165_90L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 495_76L<Cent>
                NetEffect = 495_76L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 95_76m<Cent>
                NewInterest = 95_76m<Cent>
                NewCharges = [||]
                PrincipalPortion = 400_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 95_76L<Cent>
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
    let ``QuoteTest012`` () =
        let title = "QuoteTest012"
        let description = "Loan is settled the day before the last scheduled payment is due"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 14)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 133<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 429_24L<Cent>
                Apportionment = {
                    PrincipalPortion = 353_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 76_24L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 14)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 76_24.800m<Cent>
                NewInterest = 76_24.800m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 76_24L<Cent>
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
    let ``QuoteTest013`` () =
        let title = "QuoteTest013"
        let description = "Loan is settled on the same day as the last scheduled payment is due (but which has not yet been made)"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 15)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 134<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 432_07L<Cent>
                Apportionment = {
                    PrincipalPortion = 353_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 79_07L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
                Window = 5
                PaymentDue = 432_07L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 432_07L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 79_07.200m<Cent>
                NewInterest = 79_07.200m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 79_07L<Cent>
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
    let ``QuoteTest014`` () =
        let title = "QuoteTest014"
        let description = "Loan is settled the day after the final schedule payment was due (and which was not made) but is within grace period so does not incur a late-payment fee"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 16)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 135<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 434_89L<Cent>
                Apportionment = {
                    PrincipalPortion = 353_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 81_89L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 16)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 434_89L<Cent>
                NetEffect = 434_89L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 2_82.400m<Cent>
                NewInterest = 2_82.400m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 81_89L<Cent>
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
    let ``QuoteTest015`` () =
        let title = "QuoteTest015"
        let description = "Loan is settled four days after the final schedule payment was due (and which was not made) and is outside grace period so incurs a late-payment fee"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 19)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 15)
                    ScheduleLength = PaymentCount 5
                }
                Advanced.ChargeConfig = Some {
                    ChargeTypes = Map [
                        Charge.LatePayment, {
                            Value = 10_00L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                            ChargeHolidays = [||]
                        }
                    ]
                }
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Annual <| Percent 8m
            }

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.quickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 138<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 453_36L<Cent>
                Apportionment = {
                    PrincipalPortion = 353_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 90_36L<Cent>
                    ChargesPortion = 10_00L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 19)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 453_36L<Cent>
                NetEffect = 453_36L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 11_29.600m<Cent>
                NewInterest = 11_29.600m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 90_36L<Cent>
                ChargesPortion = 10_00L<Cent>
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
    let ``QuoteTest016`` () =
        let title = "QuoteTest016"
        let description = "Loan is settled the day before an overpayment (note: if looked at from a later date the overpayment will cause a refund to be due)"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 14)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 133<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = 429_24L<Cent>
                Apportionment = {
                    PrincipalPortion = 353_00L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 76_24L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 14)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 76_24.800m<Cent>
                NewInterest = 76_24.800m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 76_24L<Cent>
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
    let ``QuoteTest017`` () =
        let title = "QuoteTest017"
        let description = "Loan is settled the same day as an overpayment"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 15)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 134<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = -67_93L<Cent>
                Apportionment = {
                    PrincipalPortion = -67_93L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.quick (ValueSome 491_53L<Cent>) ValueNone
                Window = 5
                PaymentDue = 432_07L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = GeneratedValue -67_93L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = 79_07.200m<Cent>
                NewInterest = 79_07.200m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = 79_07L<Cent>
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
    let ``QuoteTest018`` () =
        let title = "QuoteTest018"
        let description = "Loan is settled the day after an overpayment"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2023, 3, 16)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 135<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = -67_95L<Cent>
                Apportionment = {
                    PrincipalPortion = -67_93L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = -2L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2023, 3, 16)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue -67_95L<Cent>
                NetEffect = -67_95L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = -1.48887672M<Cent>
                NewInterest = -1.48887672M<Cent>
                NewCharges = [||]
                PrincipalPortion = -67_93L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = -2L<Cent>
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
    let QuoteTest019 () =
        let title = "QuoteTest019"
        let description = "Loan refund due for a long time, showing interest owed back"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 2, 5)
                Basic.StartDate = Date(2022, 11, 1)
                Basic.Principal = 1500_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
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

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            let item = quote.RevisedSchedules.AmortisationSchedule.ScheduleItems |> Map.find 461<OffsetDay>
            quote.QuoteResult, item

        let paymentQuote =
            {
                PaymentValue = -72_80L<Cent>
                Apportionment = {
                    PrincipalPortion = -67_93L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = -4_87L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        let expected = PaymentQuote paymentQuote, {
                OffsetDate = Date(2024, 2, 5)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.zero
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = GeneratedValue -72_80L<Cent>
                NetEffect = -72_80L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                ActuarialInterest = -4_86.86268494M<Cent>
                NewInterest = -4_86.86268494M<Cent>
                NewCharges = [||]
                PrincipalPortion = -67_93L<Cent>
                FeePortion = 0L<Cent>
                InterestPortion = -4_87L<Cent>
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
    let QuoteTest020 () =
        let title = "QuoteTest020"
        let description = "Settlement quote on the same day a loan is closed has 0L<Cent> payment and 0L<Cent> principal and interest components"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2022, 12, 20)
                Basic.StartDate = Date(2022, 12, 19)
                Basic.Principal = 250_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2023, 1, 20)
                    ScheduleLength = PaymentCount 4
                }
                Advanced.InterestConfig.InitialGracePeriod = 0<DurationDay>
            }

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.quickConfirmed 252_00L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 0L<Cent>
                Apportionment = {
                    PrincipalPortion = 0L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest021 () =
        let title = "QuoteTest021"
        let description = "Generated settlement figure is correct"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 3, 4)
                Basic.StartDate = Date(2018, 2, 3)
                Basic.Principal = 230_00L<Cent>
                Basic.ScheduleConfig =
                    AutoGenerateSchedule {
                        UnitPeriodConfig = Monthly(1, 2018, 2, 28)
                        ScheduleLength = PaymentCount 3
                    }
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.InterestConfig.InitialGracePeriod = 0<DurationDay>
                Advanced.InterestConfig.RateOnNegativeBalance = Interest.Rate.Zero
            }
        let actualPayments =
            Map [
                25<OffsetDay>, [| ActualPayment.quickConfirmed 72_54L<Cent> |]
                53<OffsetDay>, [| ActualPayment.quickFailed 72_54L<Cent> ValueNone; ActualPayment.quickConfirmed 72_54L<Cent> |]
                78<OffsetDay>, [| ActualPayment.quickConfirmed 72_54L<Cent>; ActualPayment.quickConfirmed 145_07L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = -5_83L<Cent>
                Apportionment = {
                    PrincipalPortion = -5_83L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 0L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest022 () =
        let title = "QuoteTest022"
        let description = "Generated settlement figure is correct when an insufficient funds penalty is charged for a failed payment"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 3, 4)
                Basic.StartDate = Date(2018, 2, 3)
                Basic.Principal = 230_00L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = Monthly(1, 2018, 2, 28); ScheduleLength = PaymentCount 3 }
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.ChargeConfig = Some {
                    ChargeTypes = Map [
                        Charge.InsufficientFunds, {
                            Value = 7_50L<Cent>
                            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                            ChargeHolidays = [||]
                        }
                    ]
                }
                Advanced.InterestConfig.InitialGracePeriod = 0<DurationDay>
            }
        let actualPayments =
            Map [
                25<OffsetDay>, [| ActualPayment.quickConfirmed 72_54L<Cent> |]
                53<OffsetDay>, [| ActualPayment.quickFailed 72_54L<Cent> (ValueSome Charge.InsufficientFunds); ActualPayment.quickConfirmed 72_54L<Cent> |]
                78<OffsetDay>, [| ActualPayment.quickConfirmed 72_54L<Cent>; ActualPayment.quickConfirmed 145_07L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 57_51L<Cent>
                Apportionment = {
                    PrincipalPortion = 3_17L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 54_34L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest023 () =
        let title = "QuoteTest023"
        let description = "Curveball"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 3, 7)
                Basic.StartDate = Date(2024, 2, 2)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = Monthly(1, 2024, 2, 22); ScheduleLength = PaymentCount 4 }
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.InterestConfig.InitialGracePeriod = 0<DurationDay>
            }

        let actualPayments =
            Map [
                5<OffsetDay>, [| ActualPayment.quickConfirmed -5_10L<Cent> |]
                6<OffsetDay>, [| ActualPayment.quickConfirmed 2_00L<Cent> |]
                16<OffsetDay>, [| ActualPayment.quickConfirmed 97_01L<Cent>; ActualPayment.quickConfirmed 97_01L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = 104_69L<Cent>
                Apportionment = {
                    PrincipalPortion = 91_52L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = 13_17L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest024 () =
        let title = "QuoteTest024"
        let description = "Negative interest should accrue to interest balance not principal balance"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 3, 7)
                Basic.StartDate = Date(2023, 9, 2)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = Monthly(1, 2023, 9, 22); ScheduleLength = PaymentCount 4 }
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.InterestConfig.InitialGracePeriod = 0<DurationDay>
            }

        let actualPayments =
            Map [
                20<OffsetDay>, [| ActualPayment.quickConfirmed 200_00L<Cent> |]
                50<OffsetDay>, [| ActualPayment.quickConfirmed 200_00L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = -91_06L<Cent>
                Apportionment = {
                    PrincipalPortion = -88_40L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = -2_66L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected

    [<Fact>]
    let QuoteTest025 () =
        let title = "QuoteTest025"
        let description = "Quote with long period of negative interest accruing"
        let p =
            { parameters2 with
                Basic.EvaluationDate = Date(2024, 4, 5)
                Basic.StartDate = Date(2023, 5, 5)
                Basic.Principal = 25000L<Cent>
                Basic.ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = Monthly(1, 2023, 5, 10); ScheduleLength = PaymentCount 4 }
                Advanced.PaymentConfig.Timeout = 0<DurationDay>
                Advanced.PaymentConfig.Minimum = NoMinimumPayment
                Advanced.InterestConfig.InitialGracePeriod = 0<DurationDay>
            }

        let actualPayments =
            Map [
                5<OffsetDay>, [| ActualPayment.quickConfirmed 111_00L<Cent> |]
                21<OffsetDay>, [| ActualPayment.quickConfirmed 181_01L<Cent> |]
            ]

        let actual =
            let quote = getQuote p actualPayments
            quote.RevisedSchedules |> Schedule.outputHtmlToFile folder title description p
            quote.QuoteResult

        let expected =
            PaymentQuote {
                PaymentValue = -13_84L<Cent>
                Apportionment = {
                    PrincipalPortion = -12_94L<Cent>
                    FeePortion = 0L<Cent>
                    InterestPortion = -90L<Cent>
                    ChargesPortion = 0L<Cent>
                }
                FeeRebateIfSettled = 0L<Cent>
            }

        actual |> should equal expected
