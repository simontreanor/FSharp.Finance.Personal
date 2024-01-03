namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementTests =

    open ActualPayment

    [<Fact>]
    let ``1) Settlement falling on a scheduled payment date`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; Charges = [||] }
            )

        let actual =
            voption{
                let! settlement = Settlement.getSettlement (DateTime.Today.AddDays -3.) sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement001.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (196970L<Cent>, {
            OffsetDate = (DateTime.Today.AddDays -3.)
            OffsetDay = 57<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 32315L<Cent>
            ActualPayments = [| 196970L<Cent> |]
            NetEffect = 196970L<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = Settled
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
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; Charges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement002.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (202648L<Cent>, {
            OffsetDate = DateTime.Today
            OffsetDay = 60<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 202648L<Cent> |]
            NetEffect = 202648L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
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
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; Charges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement003.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (200148L<Cent>, {
            OffsetDate = DateTime.Today
            OffsetDay = 60<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 2500L<Cent>; 200148L<Cent> |]
            NetEffect = 202648L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
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
        let startDate = DateTime.Today.AddDays -3.

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = startDate.AddDays 15. |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = ValueNone
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement004.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (120000L<Cent>, {
            OffsetDate = DateTime.Today
            OffsetDay = 3<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 120000L<Cent> |]
            NetEffect = 120000L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
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
        let startDate = DateTime.Today.AddDays -4.

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = startDate.AddDays 15. |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = ValueNone
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement005.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (123840L<Cent>, {
            OffsetDate = DateTime.Today
            OffsetDay = 4<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 123840L<Cent> |]
            NetEffect = 123840L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
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
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; Charges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement006.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (342001L<Cent>, {
            OffsetDate = DateTime.Today
            OffsetDay = 60<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 0L<Cent>
            ActualPayments = [| 342001L<Cent> |]
            NetEffect = 342001L<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
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
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            StartDate = startDate
            AsOfDate = DateTime.Today
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 32315L<Cent> |]; Charges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getNextScheduled DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement007.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (32315L<Cent>, {
            OffsetDate = startDate.AddDays(155.)
            OffsetDay = 155<OffsetDay>
            Advance = 0L<Cent>
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
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
            FeesAndCharges = {
                Fees = ValueSome <| Fees.Percentage (Percent 189.47m, ValueNone)
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDays>
                Holidays = [||]
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
                { PaymentDay = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 32315L<Cent> |]; Charges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getAllOverdue DateTime.Today sp actualPayments
                settlement.WhatIfSchedule.Items |> Formatting.outputListToHtml "out/Settlement008.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfSchedule.Items
            }

        let expected = ValueSome (68808L<Cent>, {
            OffsetDate = startDate.AddDays(155.)
            OffsetDay = 155<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 30250L<Cent>
            ActualPayments = [||]
            NetEffect = 30250L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = Settled
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
