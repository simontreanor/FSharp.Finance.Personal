namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module QuoteTests =

    open CustomerPayments
    open PaymentSchedule
    open Amortisation
    open Quotes

    [<Fact>]
    let ``1) Settlement falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 9, 28)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 25_00L<Cent>) }
            )

        let actual =
            voption{
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote001.md"
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1969_70L<Cent>, 1175_80L<Cent>, 790_19L<Cent>, 3_71L<Cent>, 0L<Cent>, 1437_53L<Cent>),
            {
                OffsetDate = (Date(2024, 10, 1).AddDays -3)
                OffsetDay = 57<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 323_15L<Cent>
                PaymentDue = 323_15L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1969_70L<Cent>
                NetEffect = 1969_70L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 1969_70L<Cent>
                ProRatedFees = 1437_53L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``2) Settlement not falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 25_00L<Cent>) }
            )

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote002.md"
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (2026_48L<Cent>, 1175_80L<Cent>, 834_19L<Cent>, 6_49L<Cent>, 10_00L<Cent>, 1393_53L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 60<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 2026_48L<Cent>
                NetEffect = 2026_48L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 2026_48L<Cent>
                ProRatedFees = 1393_53L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``3) Settlement not falling on a scheduled payment date but having an actual payment already made on the same day`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 60 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 25_00L<Cent>) }
            )

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote003.md"
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (2001_48L<Cent>, 1175_80L<Cent>, 825_68L<Cent>, 0L<Cent>, 0L<Cent>, 1393_53L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 60<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [| ActualPaymentStatus.Confirmed 25_00L<Cent> |]
                GeneratedPayment = ValueSome 2001_48L<Cent>
                NetEffect = 2026_48L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 2001_48L<Cent>
                ProRatedFees = 1393_53L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``4) Settlement within interest grace period should not accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -3

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote004.md"
                let! item = Array.vTryLastBut 5 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1200_00L<Cent>, 1200_00L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 3<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1200_00L<Cent>
                NetEffect = 1200_00L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 1200_00L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``5) Settlement just outside interest grace period should accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -4

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1)),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote005.md"
                let! item = Array.vTryLastBut 5 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1238_40L<Cent>, 1200_00L<Cent>, 0L<Cent>, 38_40L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 4<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1238_40L<Cent>
                NetEffect = 1238_40L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 1238_40L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``6) Settlement when fee is due in full`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments =
            [| 18 .. 7 .. 53 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 25_00L<Cent>) }
            )

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote006.md"
                let! item = Array.vTryLastBut 7 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (3420_01L<Cent>, 1175_80L<Cent>, 2227_72L<Cent>, 6_49L<Cent>, 10_00L<Cent>, 2227_72L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                OffsetDay = 60<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 3420_01L<Cent>
                NetEffect = 3420_01L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 3420_01L<Cent>
                ProRatedFees = 2227_72L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``7) Get next scheduled payment`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            AsOfDate = Date(2024, 10, 1)
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = DoNotApplyNegativeInterest
            }
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 323_15L<Cent>) }
            )

        let actual =
            voption {
                let! quote = getQuote FirstOutstanding sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote007.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (323_15L<Cent>, 96_39L<Cent>, 182_65L<Cent>, 24_11L<Cent>, 20_00L<Cent>, 1685_14L<Cent>), {
                OffsetDate = startDate.AddDays 155
                OffsetDay = 155<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 323_10L<Cent>
                PaymentDue = 323_10L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueNone
                NetEffect = 323_10L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = OpenBalance
                NewInterest = 2_57L<Cent>
                NewCharges = [||]
                PrincipalPortion = 110_72L<Cent>
                FeesPortion = 209_81L<Cent>
                InterestPortion = 2_57L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 122_33L<Cent>
                FeesBalance = 231_57L<Cent>
                InterestBalance = 0L<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 677_00L<Cent>
                ProRatedFees = 231_57L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``8) Get payment to cover all overdue amounts`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = DoNotApplyNegativeInterest
            }
        }

        let actualPayments =
            [| 15 .. 14 .. 29 |]
            |> Array.map(fun i ->
                { PaymentDay = i * 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 323_15L<Cent>) }
            )

        let actual =
            voption {
                let! quote = getQuote AllOverdue sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote008.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (690_41L<Cent>, 223_27L<Cent>, 423_03L<Cent>, 24_11L<Cent>, 20_00L<Cent>, 1444_76L<Cent>),
            {
                OffsetDate = startDate.AddDays 155
                OffsetDay = 155<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 323_10L<Cent>
                PaymentDue = 300_11L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueNone
                NetEffect = 300_11L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                NewInterest = 1_14L<Cent>
                NewCharges = [||]
                PrincipalPortion = 103_33L<Cent>
                FeesPortion = 195_64L<Cent>
                InterestPortion = 1_14L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0L<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 300_11L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``9) Verified example`` () =
        let startDate = Date(2023, 6, 23)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2023, 6, 30)),
                PaymentCount = 10
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote009.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (1311_66L<Cent>, 500_00L<Cent>, 750_00L<Cent>, 61_66L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = startDate.AddDays 181
                OffsetDay = 181<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1311_66L<Cent>
                NetEffect = 1311_66L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 1311_66L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``10) Verified example`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12)),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 70<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
            { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
            { PaymentDay = 84<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
            { PaymentDay = 85<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
            { PaymentDay = 98<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
            { PaymentDay = 112<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
            { PaymentDay = 126<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 272_84L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote010.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (1261_68L<Cent>, 471_06L<Cent>, 706_53L<Cent>, 84_09L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = startDate.AddDays 388
                OffsetDay = 388<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1261_68L<Cent>
                NetEffect = 1261_68L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 1261_68L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``11) When settling a loan with 3-day late-payment grace period, scheduled payments within the grace period should be treated as missed payments, otherwise the quote balance is too low`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 2, 8)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 1200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12)),
                PaymentCount = 11
            )
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 5
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 279_01L<Cent>) }
            { PaymentDay = 28<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 279_01L<Cent>) }
            { PaymentDay = 42<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 279_01L<Cent>) }
            { PaymentDay = 56<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 279_01L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote011.md"
                let! item = Array.vTryLastBut 6 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (973_52L<Cent>, 769_46L<Cent>, 195_68L<Cent>, 8_38L<Cent>, 0L<Cent>, 958_45L<Cent>),
            {
                OffsetDate = startDate.AddDays 72
                OffsetDay = 72<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 973_52L<Cent>
                NetEffect = 973_52L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 973_52L<Cent>
                ProRatedFees = 958_45L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``12) Settlement figure should not be lower than principal`` () =
        let startDate = Date(2024, 1, 29)

        let sp = {
            AsOfDate = Date(2024, 2, 28)
            ScheduleType = ScheduleType.Original
            StartDate = startDate
            Principal = 400_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 28),
                PaymentCount = 4
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.798m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 1<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome (Interest.Rate.Annual (Percent 8m))
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [||]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote012.md"
                let! item = Array.vTryLastBut 3 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (495_76L<Cent>, 400_00L<Cent>, 0L<Cent>, 95_76L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = startDate.AddDays 30
                OffsetDay = 30<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 165_90L<Cent>
                PaymentDue = 165_90L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 495_76L<Cent>
                NetEffect = 495_76L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 495_76L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13a) Loan is settled the day before the last scheduled payment is due`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 14)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote013a.md"
                let! item = Array.vTryLastBut 1 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (429_24L<Cent>, 353_00L<Cent>, 0L<Cent>, 76_24L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 14)
                OffsetDay = 133<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 429_24L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13b) Loan is settled on the same day as the last scheduled payment is due (but which has not yet been made)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 15)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote013b.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (432_07L<Cent>, 353_00L<Cent>, 0L<Cent>, 79_07L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 15)
                OffsetDay = 134<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 491_53L<Cent>
                PaymentDue = 457_65L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 432_07L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 432_07L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13c) Loan is settled the day after the final schedule payment was due (and which was not made) but is within grace period so does not incur a late-payment fee`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote013c.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (434_89L<Cent>, 353_00L<Cent>, 0L<Cent>, 81_89L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 16)
                OffsetDay = 135<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 434_89L<Cent>
                NetEffect = 434_89L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 434_89L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``13d) Loan is settled four days after the final schedule payment was due (and which was not made) and is outside grace period so incurs a late-payment fee`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 19)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote013d.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (453_36L<Cent>, 353_00L<Cent>, 0L<Cent>, 90_36L<Cent>, 10_00L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 19)
                OffsetDay = 138<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 453_36L<Cent>
                NetEffect = 453_36L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 453_36L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``14a) Loan is settled the day before an overpayment (note: if looked at from a later date the overpayment will cause a refund to be due)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 14)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }
        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote014a.md"
                let! item = Array.vTryLastBut 1 quote.RevisedSchedule.ScheduleItems
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (429_24L<Cent>, 353_00L<Cent>, 0L<Cent>, 76_24L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 14)
                OffsetDay = 133<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = 429_24L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``14b) Loan is settled the same day as an overpayment`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 15)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote014b.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (-67_93L<Cent>, -67_93L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 15)
                OffsetDay = 134<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 491_53L<Cent>
                PaymentDue = 457_65L<Cent>
                ActualPayments = [| ActualPaymentStatus.Confirmed 500_00L<Cent> |]
                GeneratedPayment = ValueSome -67_93L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = -67_93L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``14c) Loan is settled the day after an overpayment`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote014c.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (-67_95L<Cent>, -67_93L<Cent>, 0L<Cent>, -2L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2023, 3, 16)
                OffsetDay = 135<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome -67_95L<Cent>
                NetEffect = -67_95L<Cent>
                PaymentStatus = (Generated Settlement)
                BalanceStatus = ClosedBalance
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
                SettlementFigure = -67_95L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``15) Loan refund due for a long time, showing interest owed back`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 5)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15),
                PaymentCount = 5
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 14<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 44<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 75<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 106<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
            { PaymentDay = 134<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 500_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote015.md"
                return quote.QuoteResult, Array.last quote.RevisedSchedule.ScheduleItems
            }

        let expected = ValueSome (
            PaymentQuote (-72_80L<Cent>, -67_93L<Cent>, 0L<Cent>, -4_87L<Cent>, 0L<Cent>, 0L<Cent>),
            {
                OffsetDate = Date(2024, 2, 5)
                OffsetDay = 461<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.None
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome -72_80L<Cent>
                NetEffect = -72_80L<Cent>
                PaymentStatus = Generated Settlement
                BalanceStatus = ClosedBalance
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
                SettlementFigure = -72_80L<Cent>
                ProRatedFees = 0L<Cent>
            }
        )

        actual |> should equal expected

    [<Fact>]
    let ``16) Settlement quote on the same day a loan is closed has 0L<Cent> payment and 0L<Cent> principal and interest components`` () =
        let sp = {
            AsOfDate = Date(2022, 12, 20)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2022, 12, 19)
            Principal = 250_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 1, 20),
                PaymentCount = 4
            )
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 3<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueSome <| Interest.Rate.Annual (Percent 8m)
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actualPayments = [|
            { PaymentDay = 1<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 252_00L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote016.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``17) Generated settlement figure is correct`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 4)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2018, 2, 3)
            Principal = 230_00L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2018, 2, 28), 3)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = {
                    InterestRounding = RoundDown
                    PaymentRounding = RoundUp
                }
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }
        let actualPayments = [|
            { PaymentDay = 25<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 72_54L<Cent>) }
            { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed(72_54L<Cent>, [||])) }
            { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 72_54L<Cent>) }
            { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 72_54L<Cent>) }
            { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 145_07L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote017.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-5_83L<Cent>, -5_83L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``18) Generated settlement figure is correct when an insufficient funds penalty is charged for a failed payment`` () =
        let sp = {
            AsOfDate = Date(2024, 3, 4)
            ScheduleType = ScheduleType.Original
            StartDate = Date(2018, 2, 3)
            Principal = 230_00L<Cent>
            PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2018, 2, 28), 3)
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Daily (Percent 0.8m)
                Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                RoundingOptions = {
                    InterestRounding = RoundDown
                    PaymentRounding = RoundUp
                }
                PaymentTimeout = 0<DurationDay>
                MinimumPayment = NoMinimumPayment
                NegativeInterestOption = ApplyNegativeInterest
            }
        }
        let actualPayments = [|
            { PaymentDay = 25<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 72_54L<Cent>) }
            { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed(72_54L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>) |])) }
            { PaymentDay = 53<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 72_54L<Cent>) }
            { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 72_54L<Cent>) }
            { PaymentDay = 78<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 145_07L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote018.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (57_51L<Cent>, 3_17L<Cent>, 0L<Cent>, 54_34L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected

    [<Fact>]
    let ``19) Curveball`` () =
        let sp = {
                AsOfDate = Date(2024, 3, 7)
                ScheduleType = ScheduleType.Original
                StartDate = Date(2024, 2, 2)
                Principal = 25000L<Cent>
                PaymentSchedule = RegularSchedule(UnitPeriod.Config.Monthly(1, 2024, 2, 22), 4)
                FeesAndCharges = {
                    Fees = [||]
                    FeesSettlement = Fees.Settlement.ProRataRefund
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = Interest.Daily (Percent 0.8m)
                    Cap = { Total = ValueSome (Interest.TotalPercentageCap (Percent 100m)) ; Daily = ValueSome (Interest.DailyPercentageCap(Percent 0.8m)) }
                    InitialGracePeriod = 0<DurationDay>
                    Holidays = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom(3)
                    RoundingOptions = {
                        InterestRounding = RoundDown
                        PaymentRounding = RoundUp
                    }
                    PaymentTimeout = 0<DurationDay>
                    MinimumPayment = NoMinimumPayment
                    NegativeInterestOption = ApplyNegativeInterest
                }
            }

        let actualPayments = [|
            { PaymentDay = 5<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed -5_10L<Cent>) }
            { PaymentDay = 6<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 2_00L<Cent>) }
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 97_01L<Cent>) }
            { PaymentDay = 16<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 97_01L<Cent>) }
        |]

        let actual =
            voption {
                let! quote = getQuote Settlement sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> Formatting.outputListToHtml "out/Quote019.md"
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (105_69L<Cent>, 92_40L<Cent>, 0L<Cent>, 13_29L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected
