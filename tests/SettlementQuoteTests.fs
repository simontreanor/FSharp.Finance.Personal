namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module SettlementQuoteTests =

    open IrregularPayment

    [<Fact>]
    let ``1) Settlement falling on a scheduled payment date`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Duration>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption{
                let! settlementQuote = Settlement.getQuote (DateTime.Today.AddDays -3.) sp actualPayments
                settlementQuote.SettlementStatement |> Formatting.outputListToHtml "SettlementQuote001.md" (ValueSome 300)
                return settlementQuote.SettlementFigure, Array.last settlementQuote.SettlementStatement
            }

        let expected = ValueSome (196970<Cent>, {
            Date = (DateTime.Today.AddDays -3.)
            TermDay = 57<Day>
            Advance = 0<Cent>
            ScheduledPayment = 32315<Cent>
            ActualPayments = [| 196970<Cent> |]
            NetEffect = 196970<Cent>
            PaymentStatus = ValueSome Overpayment
            BalanceStatus = Settled
            CumulativeInterest = 5359<Cent>
            NewInterest = 371<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 79019<Cent>
            InterestPortion = 371<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 143753<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``2) Settlement not falling on a scheduled payment date`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Duration>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlementQuote = Settlement.getQuote DateTime.Today sp actualPayments
                settlementQuote.SettlementStatement |> Formatting.outputListToHtml "SettlementQuote002.md" (ValueSome 300)
                return settlementQuote.SettlementFigure, Array.last settlementQuote.SettlementStatement
            }

        let expected = ValueSome (202648<Cent>, {
            Date = DateTime.Today
            TermDay = 60<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 202648<Cent> |]
            NetEffect = 202648<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 5637<Cent>
            NewInterest = 278<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 83419<Cent>
            InterestPortion = 649<Cent>
            PenaltyChargesPortion = 1000<Cent>
            ProductFeesRefund = 139353<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``3) Settlement not falling on a scheduled payment date but having an actual payment already made on the same day`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = ProRataRefund
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Duration>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlementQuote = Settlement.getQuote DateTime.Today sp actualPayments
                settlementQuote.SettlementStatement |> Formatting.outputListToHtml "SettlementQuote003.md" (ValueSome 300)
                return settlementQuote.SettlementFigure, Array.last settlementQuote.SettlementStatement
            }

        let expected = ValueSome (200148<Cent>, {
            Date = DateTime.Today
            TermDay = 60<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 2500<Cent>; 200148<Cent> |]
            NetEffect = 202648<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 5637<Cent>
            NewInterest = 278<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 83419<Cent>
            InterestPortion = 649<Cent>
            PenaltyChargesPortion = 1000<Cent>
            ProductFeesRefund = 139353<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``4) Settlement within interest grace period should not accrue interest`` () =
        let startDate = DateTime.Today.AddDays -3.

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueNone
            ProductFeesSettlement = ProRataRefund
            InterestRate = DailyInterestRate (Percent 0.8m)
            InterestCap = ValueSome (PercentageOfPrincipal (Percent 100m))
            InterestGracePeriod = 3<Duration>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Monthly(1, startDate.AddDays 15. |> fun sd -> UnitPeriod.MonthlyConfig(sd.Year, sd.Month, sd.Day * 1<TrackingDay>))
            PaymentCount = 5
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlementQuote = Settlement.getQuote DateTime.Today sp actualPayments
                settlementQuote.SettlementStatement |> Formatting.outputListToHtml "SettlementQuote004.md" (ValueSome 300)
                return settlementQuote.SettlementFigure, Array.last settlementQuote.SettlementStatement
            }

        let expected = ValueSome (120000<Cent>, {
            Date = DateTime.Today
            TermDay = 3<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 120000<Cent> |]
            NetEffect = 120000<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 0<Cent>
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 120000<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``5) Settlement just outside interest grace period should accrue interest`` () =
        let startDate = DateTime.Today.AddDays -4.

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueNone
            ProductFeesSettlement = ProRataRefund
            InterestRate = DailyInterestRate (Percent 0.8m)
            InterestCap = ValueSome (PercentageOfPrincipal (Percent 100m))
            InterestGracePeriod = 3<Duration>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Monthly(1, startDate.AddDays 15. |> fun sd -> UnitPeriod.MonthlyConfig(sd.Year, sd.Month, sd.Day * 1<TrackingDay>))
            PaymentCount = 5
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! settlementQuote = Settlement.getQuote DateTime.Today sp actualPayments
                settlementQuote.SettlementStatement |> Formatting.outputListToHtml "SettlementQuote005.md" (ValueSome 300)
                return settlementQuote.SettlementFigure, Array.last settlementQuote.SettlementStatement
            }

        let expected = ValueSome (123840<Cent>, {
            Date = DateTime.Today
            TermDay = 4<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 123840<Cent> |]
            NetEffect = 123840<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 3840<Cent>
            NewInterest = 3840<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 120000<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 3840<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        })

        actual |> should equal expected

    [<Fact>]
    let ``6) Settlement when product fee is due in full`` () =
        let startDate = DateTime.Today.AddDays(-60.)

        let sp : RegularPayment.ScheduleParameters = {
            StartDate = startDate
            Principal = 1200 * 100<Cent>
            ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
            ProductFeesSettlement = DueInFull
            InterestRate = AnnualInterestRate (Percent 9.95m)
            InterestCap = ValueNone
            InterestGracePeriod = 3<Duration>
            InterestHolidays = [||]
            UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
            PaymentCount = 11
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
            )

        let actual =
            voption {
                let! settlementQuote = Settlement.getQuote DateTime.Today sp actualPayments
                settlementQuote.SettlementStatement |> Formatting.outputListToHtml "SettlementQuote006.md" (ValueSome 300)
                return settlementQuote.SettlementFigure, Array.last settlementQuote.SettlementStatement
            }

        let expected = ValueSome (342001<Cent>, {
            Date = DateTime.Today
            TermDay = 60<Day>
            Advance = 0<Cent>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| 342001<Cent> |]
            NetEffect = 342001<Cent>
            PaymentStatus = ValueSome ExtraPayment
            BalanceStatus = Settled
            CumulativeInterest = 5637<Cent>
            NewInterest = 278<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 117580<Cent>
            ProductFeesPortion = 222772<Cent>
            InterestPortion = 649<Cent>
            PenaltyChargesPortion = 1000<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        })

        actual |> should equal expected
