namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementTests =

    open CustomerPayments
    open PaymentSchedule
    open Amortisation

    [<Fact>]
    let ``1) Settlement falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (2500L<Cent>, [||]) }
            )

        let actual =
            voption{
                let! settlement = Settlement.getSettlement (Date(2024, 10, 1).AddDays -3) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement001.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (196970L<Cent>, {
            OffsetDate = (Date(2024, 10, 1).AddDays -3)
            OffsetDay = 57<OffsetDay>
            Advances = [||]
            ScheduledPayment = 32315L<Cent>
            ActualPayments = [| 196970L<Cent> |]
            NetEffect = 196970L<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = Settlement
            CumulativeInterest = 5359L<Cent>
            NewInterest = 371L<Cent>
            NewCharges = [||]
            PrincipalPortion = 117580L<Cent>
            FeesPortion = 79019L<Cent>
            InterestPortion = 371L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 143753L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``2) Settlement not falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (2500L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2024, 10, 1)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement002.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (202648L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 60<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 202648L<Cent> |]
            NetEffect = 202648L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 5637L<Cent>
            NewInterest = 278L<Cent>
            NewCharges = [||]
            PrincipalPortion = 117580L<Cent>
            FeesPortion = 83419L<Cent>
            InterestPortion = 649L<Cent>
            ChargesPortion = 1000L<Cent>
            FeesRefund = 139353L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``3) Settlement not falling on a scheduled payment date but having an actual payment already made on the same day`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (2500L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2024, 10, 1)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement003.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (200148L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 60<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 2500L<Cent>; 200148L<Cent> |]
            NetEffect = 202648L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 5637L<Cent>
            NewInterest = 278L<Cent>
            NewCharges = [||]
            PrincipalPortion = 117580L<Cent>
            FeesPortion = 83419L<Cent>
            InterestPortion = 649L<Cent>
            ChargesPortion = 1000L<Cent>
            FeesRefund = 139353L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``4) Settlement within interest grace period should not accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -3

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2024, 10, 1)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement004.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (120000L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 3<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 120000L<Cent> |]
            NetEffect = 120000L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 0L<Cent>
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = 120000L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``5) Settlement just outside interest grace period should accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -4

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2024, 10, 1)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement005.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (123840L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 4<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 123840L<Cent> |]
            NetEffect = 123840L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 3840L<Cent>
            NewInterest = 3840L<Cent>
            NewCharges = [||]
            PrincipalPortion = 120000L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 3840L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``6) Settlement when fee is due in full`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (2500L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2024, 10, 1)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement006.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (342001L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 60<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 342001L<Cent> |]
            NetEffect = 342001L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 5637L<Cent>
            NewInterest = 278L<Cent>
            NewCharges = [||]
            PrincipalPortion = 117580L<Cent>
            FeesPortion = 222772L<Cent>
            InterestPortion = 649L<Cent>
            ChargesPortion = 1000L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``7) Get next scheduled payment`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            StartDate = startDate
            AsOfDate = Date(2024, 10, 1)
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (32315L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getNextScheduled (Date(2024, 10, 1)) sp actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement007.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (32315L<Cent>, {
            OffsetDate = startDate.AddDays 155
            OffsetDay = 155<OffsetDay>
            Advances = [||]
            ScheduledPayment = 32310L<Cent>
            ActualPayments = [||]
            NetEffect = 32310L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = OpenBalance
            CumulativeInterest = 10003L<Cent>
            NewInterest = 383L<Cent>
            NewCharges = [||]
            PrincipalPortion = 11029L<Cent>
            FeesPortion = 20898L<Cent>
            InterestPortion = 383L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 23682L<Cent>
            FeesBalance = 44855L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``8) Get payment to cover all overdue amounts`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 750L<Cent>); Charge.LatePayment (Amount.Simple 1000L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (32315L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getAllOverdue (Date(2024, 10, 1)) sp actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement008.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (68808L<Cent>, {
            OffsetDate = startDate.AddDays 155
            OffsetDay = 155<OffsetDay>
            Advances = [||]
            ScheduledPayment = 30250L<Cent>
            ActualPayments = [||]
            NetEffect = 30250L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = Settlement
            CumulativeInterest = 8214L<Cent>
            NewInterest = 115L<Cent>
            NewCharges = [||]
            PrincipalPortion = 10416L<Cent>
            FeesPortion = 19719L<Cent>
            InterestPortion = 115L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``9) Verified example`` () =
        let startDate = Date(2023, 6, 23)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            StartDate = startDate
            Principal = 500_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2023, 6, 30))
            PaymentCount = 10
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2023, 12, 21)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement009.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1311_66L<Cent>, {
            OffsetDate = startDate.AddDays 181
            OffsetDay = 181<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 1311_66L<Cent>|]
            NetEffect = 1311_66L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 61_66L<Cent>
            NewInterest = 16_35L<Cent>
            NewCharges = [||]
            PrincipalPortion = 500_00L<Cent>
            FeesPortion = 750_00L<Cent>
            InterestPortion = 61_66L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``10) Verified example`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments = [|
            { PaymentDay = 70<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
            { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
            { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
            { PaymentDay = 85<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
            { PaymentDay = 98<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
            { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
            { PaymentDay = 126<OffsetDay>; PaymentDetails = ActualPayment (272_84L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2023, 12, 21)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement010.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1261_68L<Cent>, {
            OffsetDate = startDate.AddDays 388
            OffsetDay = 388<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 1261_68L<Cent>|]
            NetEffect = 1261_68L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 171_56L<Cent>
            NewInterest = 75_11L<Cent>
            NewCharges = [||]
            PrincipalPortion = 471_06L<Cent>
            FeesPortion = 706_53L<Cent>
            InterestPortion = 84_09L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``11) When settling a loan with 3-day late-payment grace period, scheduled payments within the grace period should be treated as missed payments, otherwise the settlement balance is too low`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (279_01L<Cent>, [||]) }
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (279_01L<Cent>, [||]) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (279_01L<Cent>, [||]) }
            { PaymentDay = 56<OffsetDay>; PaymentDetails = ActualPayment (279_01L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement (Date(2023, 2, 8)) sp true actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement011.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (973_52L<Cent>, {
            OffsetDate = startDate.AddDays 72
            OffsetDay = 72<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 973_52L<Cent>|]
            NetEffect = 973_52L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settlement
            CumulativeInterest = 48_01L<Cent>
            NewInterest = 1_04L<Cent>
            NewCharges = [||]
            PrincipalPortion = 769_46L<Cent>
            FeesPortion = 195_68L<Cent>
            InterestPortion = 8_38L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 958_45L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected
