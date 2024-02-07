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
            AsOfDate = Date(2024, 9, 28)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (25_00L<Cent>, [||]) }
            )

        let actual =
            voption{
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement001.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1969_70L<Cent>, {
            OffsetDate = (Date(2024, 10, 1).AddDays -3)
            OffsetDay = 57<OffsetDay>
            Advances = [||]
            ScheduledPayment = 323_15L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 1969_70L<Cent>
            NetEffect = 1969_70L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 53_59L<Cent>
            NewInterest = 3_71L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1175_80L<Cent>
            FeesPortion = 790_19L<Cent>
            InterestPortion = 3_71L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 1437_53L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (25_00L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement002.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (2026_48L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 60<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 2026_48L<Cent>
            NetEffect = 2026_48L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 56_37L<Cent>
            NewInterest = 2_78L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1175_80L<Cent>
            FeesPortion = 834_19L<Cent>
            InterestPortion = 6_49L<Cent>
            ChargesPortion = 10_00L<Cent>
            FeesRefund = 1393_53L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (25_00L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement003.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (2001_48L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 60<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 25_00L<Cent> |]
            GeneratedPayment = 2001_48L<Cent>
            NetEffect = 2026_48L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 56_37L<Cent>
            NewInterest = 2_78L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1175_80L<Cent>
            FeesPortion = 834_19L<Cent>
            InterestPortion = 6_49L<Cent>
            ChargesPortion = 10_00L<Cent>
            FeesRefund = 1393_53L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement004.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1200_00L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 3<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 1200_00L<Cent>
            NetEffect = 1200_00L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 0L<Cent>
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1200_00L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement005.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1238_40L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 4<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 1238_40L<Cent>
            NetEffect = 1238_40L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 38_40L<Cent>
            NewInterest = 38_40L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1200_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 38_40L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (25_00L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement006.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (3420_01L<Cent>, {
            OffsetDate = Date(2024, 10, 1)
            OffsetDay = 60<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 3420_01L<Cent>
            NetEffect = 3420_01L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 56_37L<Cent>
            NewInterest = 2_78L<Cent>
            NewCharges = [||]
            PrincipalPortion = 1175_80L<Cent>
            FeesPortion = 2227_72L<Cent>
            InterestPortion = 6_49L<Cent>
            ChargesPortion = 10_00L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (323_15L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getNextScheduled sp actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement007.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (323_15L<Cent>, {
            OffsetDate = startDate.AddDays 155
            OffsetDay = 155<OffsetDay>
            Advances = [||]
            ScheduledPayment = 323_10L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 0L<Cent>
            NetEffect = 323_10L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = OpenBalance
            CumulativeInterest = 100_03L<Cent>
            NewInterest = 3_83L<Cent>
            NewCharges = [||]
            PrincipalPortion = 110_29L<Cent>
            FeesPortion = 208_98L<Cent>
            InterestPortion = 3_83L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 236_82L<Cent>
            FeesBalance = 448_55L<Cent>
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
            Principal = 1200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
            PaymentCount = 11
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (323_15L<Cent>, [||]) }
            )

        let actual =
            voption {
                let! settlement = Settlement.getAllOverdue sp actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement008.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (688_08L<Cent>, {
            OffsetDate = startDate.AddDays 155
            OffsetDay = 155<OffsetDay>
            Advances = [||]
            ScheduledPayment = 302_50L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 0L<Cent>
            NetEffect = 302_50L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = ClosedBalance
            CumulativeInterest = 82_14L<Cent>
            NewInterest = 1_15L<Cent>
            NewCharges = [||]
            PrincipalPortion = 104_16L<Cent>
            FeesPortion = 197_19L<Cent>
            InterestPortion = 1_15L<Cent>
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
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement009.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1311_66L<Cent>, {
            OffsetDate = startDate.AddDays 181
            OffsetDay = 181<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 1311_66L<Cent>
            NetEffect = 1311_66L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
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
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement010.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (1261_68L<Cent>, {
            OffsetDate = startDate.AddDays 388
            OffsetDay = 388<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 1261_68L<Cent>
            NetEffect = 1261_68L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
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
            AsOfDate = Date(2023, 2, 8)
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
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement011.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (973_52L<Cent>, {
            OffsetDate = startDate.AddDays 72
            OffsetDay = 72<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 973_52L<Cent>
            NetEffect = 973_52L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
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

    [<Fact>]
    let ``12) Settlement figure should not be lower than principal`` () =
        let startDate = Date(2024, 1, 29)

        let sp = {
            AsOfDate = Date(2024, 2, 28)
            StartDate = startDate
            Principal = 400_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 28)
            PaymentCount = 4
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.798m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                GracePeriod = 1<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement012.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (495_76L<Cent>, {
            OffsetDate = startDate.AddDays 30
            OffsetDay = 30<OffsetDay>
            Advances = [||]
            ScheduledPayment = 165_90L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 495_76L<Cent>
            NetEffect = 495_76L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 95_76L<Cent>
            NewInterest = 95_76L<Cent>
            NewCharges = [||]
            PrincipalPortion = 400_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 95_76L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``13a) Loan is settled the day before the last scheduled payment is due`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 14)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement013a.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (429_24L<Cent>, {
            OffsetDate = Date(2023, 3, 14)
            OffsetDay = 133<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 429_24L<Cent>
            NetEffect = 429_24L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 929_24L<Cent>
            NewInterest = 76_24L<Cent>
            NewCharges = [||]
            PrincipalPortion = 353_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 76_24L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``13b) Loan is settled on the same day as the last scheduled payment is due (but which has not yet been made)`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 15)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement013b.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (432_07L<Cent>, {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = 491_53L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 432_07L<Cent>
            NetEffect = 432_07L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 932_07L<Cent>
            NewInterest = 79_07L<Cent>
            NewCharges = [||]
            PrincipalPortion = 353_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 79_07L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``13c) Loan is settled the day after the final schedule payment was due (and which was not made) but is within grace period so does not incur a late-payment fee`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 16)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement013c.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (434_89L<Cent>, {
            OffsetDate = Date(2023, 3, 16)
            OffsetDay = 135<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 434_89L<Cent>
            NetEffect = 434_89L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 934_89L<Cent>
            NewInterest = 2_82L<Cent>
            NewCharges = [||]
            PrincipalPortion = 353_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 81_89L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``13d) Loan is settled four days after the final schedule payment was due (and which was not made) and is outside grace period so incurs a late-payment fee`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 19)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement013d.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (453_36L<Cent>, {
            OffsetDate = Date(2023, 3, 19)
            OffsetDay = 138<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 453_36L<Cent>
            NetEffect = 453_36L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 943_36L<Cent>
            NewInterest = 11_29L<Cent>
            NewCharges = [||]
            PrincipalPortion = 353_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 90_36L<Cent>
            ChargesPortion = 10_00L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``14a) Loan is settled the day before an overpayment (note: if looked at from a later date the overpayment will cause a refund to be due)`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 14)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement014a.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (429_24L<Cent>, {
            OffsetDate = Date(2023, 3, 14)
            OffsetDay = 133<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = 429_24L<Cent>
            NetEffect = 429_24L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 929_24L<Cent>
            NewInterest = 76_24L<Cent>
            NewCharges = [||]
            PrincipalPortion = 353_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 76_24L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``14b) Loan is settled the same day as an overpayment`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 15)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement014b.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (-67_93L<Cent>, {
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 134<OffsetDay>
            Advances = [||]
            ScheduledPayment = 491_53L<Cent>
            ActualPayments = [| 500_00L<Cent> |]
            GeneratedPayment = -67_93L<Cent>
            NetEffect = 432_07L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 932_07L<Cent>
            NewInterest = 79_07L<Cent>
            NewCharges = [||]
            PrincipalPortion = 353_00L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 79_07L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``14c) Loan is settled the day after an overpayment`` () =
        let sp =
            {
                AsOfDate = Date(2023, 3, 16)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement014c.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (-67_95L<Cent>, {
            OffsetDate = Date(2023, 3, 16)
            OffsetDay = 135<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = -67_95L<Cent>
            NetEffect = -67_95L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 932_05L<Cent>
            NewInterest = -2L<Cent>
            NewCharges = [||]
            PrincipalPortion = -67_93L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = -2L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``15) Loan refund due for a long time, showing interest owed back`` () =
        let sp =
            {
                AsOfDate = Date(2024, 2, 5)
                StartDate = Date(2022, 11, 1)
                Principal = 1500_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 3<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  14<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  44<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay =  75<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment ( 500_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement015.md"
                return settlement.PaymentAmount, Array.last settlement.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (-72_80L<Cent>, {
            OffsetDate = Date(2024, 2, 5)
            OffsetDay = 461<OffsetDay>
            Advances = [||]
            ScheduledPayment = 0L<Cent>
            ActualPayments = [||]
            GeneratedPayment = -72_80L<Cent>
            NetEffect = -72_80L<Cent>
            PaymentStatus = ValueSome (Generated SettlementPayment)
            BalanceStatus = ClosedBalance
            CumulativeInterest = 927_20L<Cent>
            NewInterest = -4_87L<Cent>
            NewCharges = [||]
            PrincipalPortion = -67_93L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = -4_87L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``16) Settlement quote on the same day a loan is closed has zero payment and zero principal and interest components`` () =
        let sp =
            {
                AsOfDate = Date(2022, 12, 20)
                StartDate = Date(2022, 12, 19)
                Principal = 250_00L<Cent>
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 1, 20)
                PaymentCount = 4
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                    LatePaymentGracePeriod = 3<DurationDay>
                }
                Interest = {
                    Rate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                    GracePeriod = 0<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                    FinalPaymentAdjustment = AdjustFinalPayment
                }
            }
        let actualPayments = [|
            { PaymentDay =  1<OffsetDay>; PaymentDetails = ActualPayment ( 252_00L<Cent>, [||]) }
        |]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement sp ApplyNegativeInterest actualPayments
                settlement.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Settlement016.md"
                return settlement.PaymentAmount, settlement.OfWhichPrincipal, settlement.OfWhichInterest
            }

        let expected = ValueSome (0L<Cent>, 0L<Cent>, 0L<Cent>)
        actual |> should equal expected
