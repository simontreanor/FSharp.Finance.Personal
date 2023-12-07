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
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption{
                let! settlement = Settlement.getSettlement (DateTime.Today.AddDays -3.) sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement001.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
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
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 117580L<Cent>
            ProductFeesPortion = 79019L<Cent>
            InterestPortion = 371L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 143753L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``2) Settlement not falling on a scheduled payment date`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement002.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
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
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 117580L<Cent>
            ProductFeesPortion = 83419L<Cent>
            InterestPortion = 649L<Cent>
            PenaltyChargesPortion = 1000L<Cent>
            ProductFeesRefund = 139353L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``3) Settlement not falling on a scheduled payment date but having an actual payment already made on the same day`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i ->
                { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement003.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
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
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 117580L<Cent>
            ProductFeesPortion = 83419L<Cent>
            InterestPortion = 649L<Cent>
            PenaltyChargesPortion = 1000L<Cent>
            ProductFeesRefund = 139353L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``4) Settlement within interest grace period should not accrue interest`` () =
        let startDate = DateTime.Today.AddDays -3.

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            ProductFees = ValueNone
            ProductFeesSettlement = ProRataRefund
            InterestRate = DailyInterestRate (Percent 0.8m)
            InterestCap = ValueSome (InterestCap.PercentageOfPrincipal (Percent 100m))
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Monthly(1, startDate.AddDays 15. |> fun sd -> UnitPeriod.MonthlyConfig(sd.Year, sd.Month, sd.Day * 1<TrackingDay>))
            PaymentCount = 5
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement004.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
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
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 120000L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 0L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``5) Settlement just outside interest grace period should accrue interest`` () =
        let startDate = DateTime.Today.AddDays -4.

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            ProductFees = ValueNone
            ProductFeesSettlement = ProRataRefund
            InterestRate = DailyInterestRate (Percent 0.8m)
            InterestCap = ValueSome (InterestCap.PercentageOfPrincipal (Percent 100m))
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Monthly(1, startDate.AddDays 15. |> fun sd -> UnitPeriod.MonthlyConfig(sd.Year, sd.Month, sd.Day * 1<TrackingDay>))
            PaymentCount = 5
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement005.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
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
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 120000L<Cent>
            ProductFeesPortion = 0L<Cent>
            InterestPortion = 3840L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``6) Settlement when product fee is due in full`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = DueInFull
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getSettlement DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement006.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
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
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 117580L<Cent>
            ProductFeesPortion = 222772L<Cent>
            InterestPortion = 649L<Cent>
            PenaltyChargesPortion = 1000L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``7) Get next scheduled payment`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            StartDate = startDate
            AsOfDate = DateTime.Today
            Principal = 120000L<Cent>
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = DueInFull
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 32315L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getNextScheduled DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement007.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
            }

        let expected = ValueSome (32315L<Cent>, {
            OffsetDate = startDate.AddDays(155.)
            OffsetDay = 155<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 32310L<Cent>
            ActualPayments = [| |]
            NetEffect = 32310L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = OpenBalance
            CumulativeInterest = 10003L<Cent>
            NewInterest = 383L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 11029L<Cent>
            ProductFeesPortion = 20898L<Cent>
            InterestPortion = 383L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 23682L<Cent>
            ProductFeesBalance = 44855L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``8) Get payment to cover all overdue amounts`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : ScheduledPayment.ScheduleParameters = {
            AsOfDate = DateTime.Today
            StartDate = startDate
            Principal = 120000L<Cent>
            ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = DueInFull
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Days>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 32315L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlement = Settlement.getAllOverdue DateTime.Today sp actualPayments
                settlement.WhatIfStatement |> Formatting.outputListToHtml "out/Settlement008.md" (ValueSome 300)
                return settlement.PaymentAmount, Array.last settlement.WhatIfStatement
            }

        let expected = ValueSome (68808L<Cent>, {
            OffsetDate = startDate.AddDays(155.)
            OffsetDay = 155<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 30250L<Cent>
            ActualPayments = [| |]
            NetEffect = 30250L<Cent>
            PaymentStatus = ValueSome NotYetDue
            BalanceStatus = Settled
            CumulativeInterest = 8214L<Cent>
            NewInterest = 115L<Cent>
            NewPenaltyCharges = 0L<Cent>
            PrincipalPortion = 10416L<Cent>
            ProductFeesPortion = 19719L<Cent>
            InterestPortion = 115L<Cent>
            PenaltyChargesPortion = 0L<Cent>
            ProductFeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            ProductFeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            PenaltyChargesBalance = 0L<Cent>
        })

        actual |> should equal expected
