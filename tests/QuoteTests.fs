namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module QuoteTests =

    open ArrayExtension
    open Amortisation
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open PaymentSchedule
    open Percentages
    open Quotes
    open ValueOptionCE

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    [<Fact>]
    let ``1) Settlement falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 9, 28)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i -> CustomerPayment.ActualConfirmed (i * 1<OffsetDay>) 25_00L<Cent>)

        let actual =
            voption{
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest001.md" false
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1969_72L<Cent>, 1175_80L<Cent>, 790_21L<Cent>, 3_71L<Cent>, 0L<Cent>, 1437_53L<Cent>),
            {
                OffsetDate = (Date(2024, 10, 1).AddDays -3)
                OffsetDay = 57<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 323_15L<Cent>; Metadata = Map.empty }
                Window = 4
                PaymentDue = 323_15L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1969_72L<Cent>
                NetEffect = 1969_72L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 3_71.12573150m<Cent>
                NewInterest = 3_71.12573150m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeesPortion = 790_21L<Cent>
                InterestPortion = 3_71L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 1437_53L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1969_72L<Cent>
                FeesRefundIfSettled = 1437_53L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``2) Settlement not falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i -> CustomerPayment.ActualConfirmed (i * 1<OffsetDay>) 25_00L<Cent>)

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest002.md" false
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (2026_50L<Cent>, 1175_80L<Cent>, 834_21L<Cent>, 6_49L<Cent>, 10_00L<Cent>, 1393_53L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 60<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 2026_50L<Cent>
                NetEffect = 2026_50L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 2_78.34429863m<Cent>
                NewInterest = 2_78.34429863m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeesPortion = 834_21L<Cent>
                InterestPortion = 6_49L<Cent>
                ChargesPortion = 10_00L<Cent>
                FeesRefund = 1393_53L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 2026_50L<Cent>
                FeesRefundIfSettled = 1393_53L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``3) Settlement not falling on a scheduled payment date but having an actual payment already made on the same day`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i -> CustomerPayment.ActualConfirmed (i * 1<OffsetDay>) 25_00L<Cent>)

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest003.md" false
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (2001_50L<Cent>, 1175_80L<Cent>, 825_70L<Cent>, 0L<Cent>, 0L<Cent>, 1393_53L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 60<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 25_00L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueSome 2001_50L<Cent>
                NetEffect = 2026_50L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 2_78.34429863m<Cent>
                NewInterest = 2_78.34429863m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeesPortion = 834_21L<Cent>
                InterestPortion = 6_49L<Cent>
                ChargesPortion = 10_00L<Cent>
                FeesRefund = 1393_53L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 2001_50L<Cent>
                FeesRefundIfSettled = 1393_53L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``4) Settlement within interest grace period should not accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -3

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest004.md" false
                let! item = Array.vTryLastBut 5 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1200_00L<Cent>, 1200_00L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 3<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 0
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1200_00L<Cent>
                NetEffect = 1200_00L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 0m<Cent>
                NewInterest = 0m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1200_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 0L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1200_00L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``5) Settlement just outside interest grace period should accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -4

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest005.md" false
                let! item = Array.vTryLastBut 5 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1238_40L<Cent>, 1200_00L<Cent>, 0L<Cent>, 38_40L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 4<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 0
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1238_40L<Cent>
                NetEffect = 1238_40L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 38_40m<Cent>
                NewInterest = 38_40m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1200_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 38_40L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1238_40L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``6) Settlement when fee is due in full`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.None
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i -> CustomerPayment.ActualConfirmed (i * 1<OffsetDay>) 25_00L<Cent>)

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest006.md" false
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (3420_03L<Cent>, 1175_80L<Cent>, 2227_74L<Cent>, 6_49L<Cent>, 10_00L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 60<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 3420_03L<Cent>
                NetEffect = 3420_03L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 2_78.34429863m<Cent>
                NewInterest = 2_78.34429863m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1175_80L<Cent>
                FeesPortion = 2227_74L<Cent>
                InterestPortion = 6_49L<Cent>
                ChargesPortion = 10_00L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 3420_03L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    // [<Fact>]
    // let ``7) Get next scheduled payment`` () =
    //     let startDate = Date(2024, 10, 1).AddDays(-60)

    //     let sp = {
    //         StartDate = startDate
    //         AsOfDate = Date(2024, 10, 1)
    //         Principal = 1200_00L<Cent>
    //         PaymentSchedule = RegularSchedule (
    //             UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
    //             PaymentCount = 11,
    //             MaxDuration = ValueNone
    //         )
    //         PaymentOptions = {
    //             ScheduledPaymentOption = AsScheduled
    //             CloseBalanceOption = LeaveOpenBalance
    //         }
    //         FeesAndCharges = {
    //             Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
    //             FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
    //             FeesSettlementRefund = Fees.SettlementRefund.None
    //             Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
    //             ChargesHolidays = [||]
    //             ChargesGrouping = OneChargeTypePerDay
    //             LatePaymentGracePeriod = 0<DurationDay>
    //         }
    //         Interest = {
    //             Method = InterestMethod.Simple
    //             StandardRate = Interest.Rate.Annual (Percent 9.95m)
    //             Cap = Interest.Cap.none
    //             InitialGracePeriod = 3<DurationDay>
    //             PromotionalRates = [||]
    //             RateOnNegativeBalance = ValueNone
    //         }
    //         Calculation = {
    //             AprMethod = Apr.CalculationMethod.UsActuarial 8
    //             RoundingOptions = RoundingOptions.recommended
    //             MinimumPayment = DeferOrWriteOff 50L<Cent>
    //             PaymentTimeout = 3<DurationDay>
    //         }
    //     }

    //     let actualPayments =
    //         [| 15 .. 14 .. 29 |]
    //         |> Array.map(fun i ->
    //             { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 323_15L<Cent>; Metadata = Map.empty } }
    //         )

    //     let actual =
    //         voption {
    //             let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
    //             quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest007.md" false
    //             return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
    //         }

    //     let expected = ValueSome (
    //         PaymentQuote (323_15L<Cent>, 96_39L<Cent>, 182_65L<Cent>, 24_11L<Cent>, 20_00L<Cent>, 0L<Cent>), {
    //             OffsetDate = startDate.AddDays 155
    //             OffsetDay = 155<OffsetDay>
    //             Advances = [||]
    //             ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 323_10L<Cent>; Metadata = Map.empty }
    //             Window = 11
    //             PaymentDue = 323_10L<Cent>
    //             ActualPayments = [||]
    //             GeneratedPayment = ValueNone
    //             NetEffect = 323_10L<Cent>
    //             PaymentStatus = NotYetDue
    //             BalanceStatus = OpenBalance
    //             ContractualInterest = 0m<Cent>
    //             InterestAdjustment = 2_57.39205205m<Cent>
    //             NewCharges = [||]
    //             PrincipalPortion = 110_72L<Cent>
    //             FeesPortion = 209_81L<Cent>
    //             InterestPortion = 2_57L<Cent>
    //             ChargesPortion = 0L<Cent>
    //             FeesRefund = 0L<Cent>
    //             PrincipalBalance = 122_33L<Cent>
    //             FeesBalance = 231_57L<Cent>
    //             InterestBalance = 0m<Cent>
    //             ChargesBalance = 0L<Cent>
    //             SettlementFigure = 677_00L<Cent>
    //             FeesRefundIfSettled = 0L<Cent>
    //         }
    //     )

    //     actual |> should equal expected

    // [<Fact>]
    // let ``8) Get payment to cover all overdue amounts`` () =
    //     let startDate = Date(2024, 10, 1).AddDays(-60)

    //     let sp = {
    //         AsOfDate = Date(2024, 10, 1)
    //         StartDate = startDate
    //         Principal = 1200_00L<Cent>
    //         PaymentSchedule = RegularSchedule (
    //             UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
    //             PaymentCount = 11,
    //             MaxDuration = ValueNone
    //         )
    //         PaymentOptions = {
    //             ScheduledPaymentOption = AsScheduled
    //             CloseBalanceOption = LeaveOpenBalance
    //         }
    //         FeesAndCharges = {
    //             Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
    //             FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
    //             FeesSettlementRefund = Fees.SettlementRefund.None
    //             Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
    //             ChargesHolidays = [||]
    //             ChargesGrouping = OneChargeTypePerDay
    //             LatePaymentGracePeriod = 0<DurationDay>
    //         }
    //         Interest = {
    //             Method = InterestMethod.Simple
    //             StandardRate = Interest.Rate.Annual (Percent 9.95m)
    //             Cap = Interest.Cap.none
    //             InitialGracePeriod = 3<DurationDay>
    //             PromotionalRates = [||]
    //             RateOnNegativeBalance = ValueNone
    //         }
    //         Calculation = {
    //             AprMethod = Apr.CalculationMethod.UsActuarial 8
    //             RoundingOptions = RoundingOptions.recommended
    //             MinimumPayment = DeferOrWriteOff 50L<Cent>
    //             PaymentTimeout = 3<DurationDay>
    //         }
    //     }

    //     let actualPayments =
    //         [| 15 .. 14 .. 29 |]
    //         |> Array.map(fun i ->
    //             { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment { ActualPaymentStatus = ActualPaymentStatus.Confirmed 323_15L<Cent>; Metadata = Map.empty } }
    //         )

    //     let actual =
    //         voption {
    //             let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
    //             quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest008.md" false
    //             return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
    //         }

    //     let expected = ValueSome (
    //         PaymentQuote (690_41L<Cent>, 223_27L<Cent>, 423_03L<Cent>, 24_11L<Cent>, 20_00L<Cent>, 0L<Cent>),
    //         {
    //             OffsetDate = startDate.AddDays 155
    //             OffsetDay = 155<OffsetDay>
    //             Advances = [||]
    //             ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 323_10L<Cent>; Metadata = Map.empty }
    //             Window = 11
    //             PaymentDue = 300_11L<Cent>
    //             ActualPayments = [||]
    //             GeneratedPayment = ValueNone
    //             NetEffect = 300_11L<Cent>
    //             PaymentStatus = NotYetDue
    //             BalanceStatus = ClosedBalance
    //             ContractualInterest = 0m<Cent>
    //             InterestAdjustment = 1_14.10005753m<Cent>
    //             NewCharges = [||]
    //             PrincipalPortion = 103_33L<Cent>
    //             FeesPortion = 195_64L<Cent>
    //             InterestPortion = 1_14L<Cent>
    //             ChargesPortion = 0L<Cent>
    //             FeesRefund = 0L<Cent>
    //             PrincipalBalance = 0L<Cent>
    //             FeesBalance = 0L<Cent>
    //             InterestBalance = 0m<Cent>
    //             ChargesBalance = 0L<Cent>
    //             SettlementFigure = 300_11L<Cent>
    //             FeesRefundIfSettled = 0L<Cent>
    //         }
    //     )

    //     actual |> should equal expected

    [<Fact>]
    let ``9) Verified example`` () =
        let startDate = Date(2023, 6, 23)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            StartDate = startDate
            Principal = 500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2023, 6, 30)),
                PaymentCount = 10,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest009.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (1311_67L<Cent>, 500_00L<Cent>, 750_00L<Cent>, 61_67L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = startDate.AddDays 181
                OffsetDay = 181<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 10
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1311_67L<Cent>
                NetEffect = 1311_67L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 16_35.61643835m<Cent>
                NewInterest = 16_35.61643835m<Cent>
                NewCharges = [||]
                PrincipalPortion = 500_00L<Cent>
                FeesPortion = 750_00L<Cent>
                InterestPortion = 61_67L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1311_67L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``10) Verified example`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12)),
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 70<OffsetDay> 272_84L<Cent>
            CustomerPayment.ActualConfirmed 84<OffsetDay> 272_84L<Cent>
            CustomerPayment.ActualConfirmed 84<OffsetDay> 272_84L<Cent>
            CustomerPayment.ActualConfirmed 85<OffsetDay> 272_84L<Cent>
            CustomerPayment.ActualConfirmed 98<OffsetDay> 272_84L<Cent>
            CustomerPayment.ActualConfirmed 112<OffsetDay> 272_84L<Cent>
            CustomerPayment.ActualConfirmed 126<OffsetDay> 272_84L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest010.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (1261_73L<Cent>, 471_07L<Cent>, 706_56L<Cent>, 84_10L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = startDate.AddDays 388
                OffsetDay = 388<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 11
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1261_73L<Cent>
                NetEffect = 1261_73L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 75_11.98884657m<Cent>
                NewInterest = 75_11.98884657m<Cent>
                NewCharges = [||]
                PrincipalPortion = 471_07L<Cent>
                FeesPortion = 706_56L<Cent>
                InterestPortion = 84_10L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1261_73L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``11) When settling a loan with 3-day late-payment grace period, scheduled payments within the grace period should be treated as missed payments, otherwise the quote balance is too low`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 2, 8)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12)),
                PaymentCount = 11,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 279_01L<Cent>
            CustomerPayment.ActualConfirmed 28<OffsetDay> 279_01L<Cent>
            CustomerPayment.ActualConfirmed 42<OffsetDay> 279_01L<Cent>
            CustomerPayment.ActualConfirmed 56<OffsetDay> 279_01L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest011.md" false
                let! item = Array.vTryLastBut 6 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (973_53L<Cent>, 769_46L<Cent>, 195_68L<Cent>, 8_39L<Cent>, 0L<Cent>, 958_45L<Cent>),
            {
                OffsetDate = startDate.AddDays 72
                OffsetDay = 72<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 973_53L<Cent>
                NetEffect = 973_53L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 1_04.87518082m<Cent>
                NewInterest = 1_04.87518082m<Cent>
                NewCharges = [||]
                PrincipalPortion = 769_46L<Cent>
                FeesPortion = 195_68L<Cent>
                InterestPortion = 8_39L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 958_45L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 973_53L<Cent>
                FeesRefundIfSettled = 958_45L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``12) Settlement figure should not be lower than principal`` () =
        let startDate = Date(2024, 1, 29)

        let sp = {
            AsOfDate = Date(2024, 2, 28)
            StartDate = startDate
            Principal = 400_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 28),
                PaymentCount = 4,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.798m)
                Cap = interestCapExample
                InitialGracePeriod = 1<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest012.md" false
                let! item = Array.vTryLastBut 3 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (495_76L<Cent>, 400_00L<Cent>, 0L<Cent>, 95_76L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = startDate.AddDays 30
                OffsetDay = 30<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 165_90L<Cent>; Metadata = Map.empty }
                Window = 1
                PaymentDue = 165_90L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 495_76L<Cent>
                NetEffect = 495_76L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 95_76m<Cent>
                NewInterest = 95_76m<Cent>
                NewCharges = [||]
                PrincipalPortion = 400_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 95_76L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 495_76L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13a) Loan is settled the day before the last scheduled payment is due`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 14)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest013a.md" false
                let! item = Array.vTryLastBut 1 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (429_24L<Cent>, 353_00L<Cent>, 0L<Cent>, 76_24L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 14)
                OffsetDay = 133<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 76_24.800m<Cent>
                NewInterest = 76_24.800m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 76_24L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 429_24L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13b) Loan is settled on the same day as the last scheduled payment is due (but which has not yet been made)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 15)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest013b.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (432_07L<Cent>, 353_00L<Cent>, 0L<Cent>, 79_07L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 15)
                OffsetDay = 134<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 491_53L<Cent>; Metadata = Map.empty }
                Window = 5
                PaymentDue = 457_65L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 432_07L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 79_07.200m<Cent>
                NewInterest = 79_07.200m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 79_07L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 432_07L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13c) Loan is settled the day after the final schedule payment was due (and which was not made) but is within grace period so does not incur a late-payment fee`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest013c.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (434_89L<Cent>, 353_00L<Cent>, 0L<Cent>, 81_89L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 16)
                OffsetDay = 135<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 434_89L<Cent>
                NetEffect = 434_89L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 2_82.400m<Cent>
                NewInterest = 2_82.400m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 81_89L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 434_89L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13d) Loan is settled four days after the final schedule payment was due (and which was not made) and is outside grace period so incurs a late-payment fee`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 19)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest013d.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (453_36L<Cent>, 353_00L<Cent>, 0L<Cent>, 90_36L<Cent>, 10_00L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 19)
                OffsetDay = 138<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 453_36L<Cent>
                NetEffect = 453_36L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 11_29.600m<Cent>
                NewInterest = 11_29.600m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 90_36L<Cent>
                ChargesPortion = 10_00L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 453_36L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``14a) Loan is settled the day before an overpayment (note: if looked at from a later date the overpayment will cause a refund to be due)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 14)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }
        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 134<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest014a.md" false
                let! item = Array.vTryLastBut 1 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (429_24L<Cent>, 353_00L<Cent>, 0L<Cent>, 76_24L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 14)
                OffsetDay = 133<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 76_24.800m<Cent>
                NewInterest = 76_24.800m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 76_24L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 429_24L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``14b) Loan is settled the same day as an overpayment`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 15)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 134<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest014b.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (-67_93L<Cent>, -67_93L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 15)
                OffsetDay = 134<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 491_53L<Cent>; Metadata = Map.empty }
                Window = 5
                PaymentDue = 457_65L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueSome -67_93L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 79_07.200m<Cent>
                NewInterest = 79_07.200m<Cent>
                NewCharges = [||]
                PrincipalPortion = 353_00L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 79_07L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = -67_93L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``14c) Loan is settled the day after an overpayment`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 134<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest014c.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (-67_95L<Cent>, -67_93L<Cent>, 0L<Cent>, -2L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 16)
                OffsetDay = 135<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome -67_95L<Cent>
                NetEffect = -67_95L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = -1.48887672M<Cent>
                NewInterest = -1.48887672M<Cent>
                NewCharges = [||]
                PrincipalPortion = -67_93L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = -2L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = -67_95L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``15) Loan refund due for a long time, showing interest owed back`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 5)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 14<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 44<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 75<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 106<OffsetDay> 500_00L<Cent>
            CustomerPayment.ActualConfirmed 134<OffsetDay> 500_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest015.md" false
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (-72_80L<Cent>, -67_93L<Cent>, 0L<Cent>, -4_87L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 2, 5)
                OffsetDay = 461<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.None; Metadata = Map.empty }
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome -72_80L<Cent>
                NetEffect = -72_80L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = -4_86.86268494M<Cent>
                NewInterest = -4_86.86268494M<Cent>
                NewCharges = [||]
                PrincipalPortion = -67_93L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = -4_87L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = -72_80L<Cent>
                FeesRefundIfSettled = 0L<Cent>
                OriginalSimpleInterestToDate = 0m<Cent>
                SimpleInterestToDate = 0m<Cent>
                ChargesToDate = 0L<Cent>
                InterestToDate = 0L<Cent>
                FeesToDate = 0L<Cent>
                PrincipalToDate = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``16) Settlement quote on the same day a loan is closed has 0L<Cent> payment and 0L<Cent> principal and interest components`` () =
        let sp = {
            AsOfDate = Date(2022, 12, 20)
            StartDate = Date(2022, 12, 19)
            Principal = 250_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 1, 20),
                PaymentCount = 4,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 1<OffsetDay> 252_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest016.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``17) Generated settlement figure is correct`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 4)
            StartDate = Date(2018, 2, 3)
            Principal = 230_00L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2018, 2, 28), 3, ValueNone)
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }
        let actualPayments = [|
            CustomerPayment.ActualConfirmed 25<OffsetDay> 72_54L<Cent>
            CustomerPayment.ActualFailed 53<OffsetDay> 72_54L<Cent> [||]
            CustomerPayment.ActualConfirmed 53<OffsetDay> 72_54L<Cent>
            CustomerPayment.ActualConfirmed 78<OffsetDay> 72_54L<Cent>
            CustomerPayment.ActualConfirmed 78<OffsetDay> 145_07L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest017.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-5_83L<Cent>, -5_83L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``18) Generated settlement figure is correct when an insufficient funds penalty is charged for a failed payment`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 4)
            StartDate = Date(2018, 2, 3)
            Principal = 230_00L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2018, 2, 28), 3, ValueNone)
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }
        let actualPayments = [|
            CustomerPayment.ActualConfirmed 25<OffsetDay> 72_54L<Cent>
            CustomerPayment.ActualFailed 53<OffsetDay> 72_54L<Cent> [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>) |]
            CustomerPayment.ActualConfirmed 53<OffsetDay> 72_54L<Cent>
            CustomerPayment.ActualConfirmed 78<OffsetDay> 72_54L<Cent>
            CustomerPayment.ActualConfirmed 78<OffsetDay> 145_07L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest018.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (57_51L<Cent>, 3_17L<Cent>, 0L<Cent>, 54_34L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``19) Curveball`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 7)
            StartDate = Date(2024, 2, 2)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2024, 2, 22), 4, ValueNone)
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 5<OffsetDay> -5_10L<Cent>
            CustomerPayment.ActualConfirmed 6<OffsetDay> 2_00L<Cent>
            CustomerPayment.ActualConfirmed 16<OffsetDay> 97_01L<Cent>
            CustomerPayment.ActualConfirmed 16<OffsetDay> 97_01L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest019.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (104_69L<Cent>, 91_52L<Cent>, 0L<Cent>, 13_17L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``20) Negative interest should accrue to interest balance not principal balance`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 7)
            StartDate = Date(2023, 9, 2)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 9, 22), 4, ValueNone)
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 20<OffsetDay> 200_00L<Cent>
            CustomerPayment.ActualConfirmed 50<OffsetDay> 200_00L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest020.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-91_06L<Cent>, -88_40L<Cent>, 0L<Cent>, -2_66L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``21) Quote with long period of negative interest accruing`` () =
        let sp = {
            AsOfDate = Date(2024, 4, 5)
            StartDate = Date(2023, 5, 5)
            Principal = 25000L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2023, 5, 10), 4, ValueNone)
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = RoundingOptions.recommended
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
            }
        }

        let actualPayments = [|
            CustomerPayment.ActualConfirmed 5<OffsetDay> 111_00L<Cent>
            CustomerPayment.ActualConfirmed 21<OffsetDay> 181_01L<Cent>
        |]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/QuoteTest021.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-13_84L<Cent>, -12_94L<Cent>, 0L<Cent>, -90L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected
