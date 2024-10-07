namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module QuoteTests =

    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open Formatting
    open PaymentSchedule
    open Percentages
    open Quotes
    open ValueOptionCE

    let interestCapExample : Interest.Cap = {
        TotalAmount = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    [<Fact>]
    let ``1) Settlement falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 9, 28)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
                PaymentCount = 11
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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
            |> Array.map(fun i -> (i * 1<OffsetDay>), [| ActualPayment.QuickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            voption{
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest001.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 57<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1969_72L<Cent>, 1175_80L<Cent>, 790_21L<Cent>, 3_71L<Cent>, 0L<Cent>, 1437_53L<Cent>),
            ({
                OffsetDate = (Date(2024, 10, 1).AddDays -3)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.Quick (ValueSome 323_15L<Cent>) ValueNone
                Window = 4
                PaymentDue = 323_15L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1969_72L<Cent>
                NetEffect = 1969_72L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``2) Settlement not falling on a scheduled payment date`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
                PaymentCount = 11
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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
            |> Array.map(fun i -> (i * 1<OffsetDay>), [| ActualPayment.QuickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest002.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 60<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (2026_50L<Cent>, 1175_80L<Cent>, 834_21L<Cent>, 6_49L<Cent>, 10_00L<Cent>, 1393_53L<Cent>),
            {
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 2026_50L<Cent>
                NetEffect = 2026_50L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
                PaymentCount = 11
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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
            |> Array.map(fun i -> (i * 1<OffsetDay>), [| ActualPayment.QuickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest003.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 60<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (2001_50L<Cent>, 1175_80L<Cent>, 825_70L<Cent>, 0L<Cent>, 0L<Cent>, 1393_53L<Cent>),
            ({
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 25_00L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueSome 2001_50L<Cent>
                NetEffect = 2026_50L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``4) Settlement within interest grace period should not accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -3

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1))
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); DailyAmount = ValueNone }
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

        let actualPayments = Map.empty

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest004.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 3<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1200_00L<Cent>, 1200_00L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 0
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1200_00L<Cent>
                NetEffect = 1200_00L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``5) Settlement just outside interest grace period should accrue interest`` () =
        let startDate = Date(2024, 10, 1).AddDays -4

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = (startDate.AddDays 15 |> fun sd -> UnitPeriod.Monthly(1, sd.Year, sd.Month, sd.Day * 1))
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown); DailyAmount = ValueNone }
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

        let actualPayments = Map.empty

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest005.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 4<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1238_40L<Cent>, 1200_00L<Cent>, 0L<Cent>, 38_40L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 0
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1238_40L<Cent>
                NetEffect = 1238_40L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``6) Settlement when fee is due in full`` () =
        let startDate = Date(2024, 10, 1).AddDays(-60)

        let sp = {
            AsOfDate = Date(2024, 10, 1)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
                PaymentCount = 11
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.None
            }
            ChargeConfig = {
                ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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
            |> Array.map(fun i -> (i * 1<OffsetDay>), [| ActualPayment.QuickConfirmed 25_00L<Cent> |])
            |> Map.ofArray

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest006.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 60<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (3420_03L<Cent>, 1175_80L<Cent>, 2227_74L<Cent>, 6_49L<Cent>, 10_00L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2024, 10, 1)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 3420_03L<Cent>
                NetEffect = 3420_03L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    // [<Fact>]
    // let ``7) Get next scheduled payment`` () =
    //     let startDate = Date(2024, 10, 1).AddDays(-60)

    //     let sp = {
    //         StartDate = startDate
    //         AsOfDate = Date(2024, 10, 1)
    //         Principal = 1200_00L<Cent>
    //         PaymentSchedule = AutoGenerateSchedule {
    //             UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
    //             PaymentCount = 11
    //             MaxDuration = ValueNone
    //         )
    //         PaymentOptions = {
    //             ScheduledPaymentOption = AsScheduled
    //             CloseBalanceOption = LeaveOpenBalance
    //         }
    //         FeesAndCharges = {
    //             Fee.FeeTypes = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]             Fee.Rounding = ValueSome RoundDown
    //             Fee.FeeAmortisation = Fees.FeeAmortisation.AmortiseProportionately
    //             Fee.SettlementRefund = Fees.SettlementRefund.None
    //             ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]             Rounding = ValueSome RoundDown
    //             ChargeHolidays = [||]
    //             ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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
    //             ScheduledPayment = ScheduledPayment.Quick (ValueSome 323_10L<Cent>) ValueNone
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
    //         PaymentSchedule = AutoGenerateSchedule {
    //             UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays 15)
    //             PaymentCount = 11
    //             MaxDuration = ValueNone
    //         )
    //         PaymentOptions = {
    //             ScheduledPaymentOption = AsScheduled
    //             CloseBalanceOption = LeaveOpenBalance
    //         }
    //         FeesAndCharges = {
    //             Fee.FeeTypes = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 189.47m, ValueNone, ValueSome RoundDown)) |]             Fee.Rounding = ValueSome RoundDown
    //             Fee.FeeAmortisation = Fees.FeeAmortisation.AmortiseProportionately
    //             Fee.SettlementRefund = Fees.SettlementRefund.None
    //             ChargeTypes = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]             Rounding = ValueSome RoundDown
    //             ChargeHolidays = [||]
    //             ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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
    //             ScheduledPayment = ScheduledPayment.Quick (ValueSome 323_10L<Cent>) ValueNone
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
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2023, 6, 30))
                PaymentCount = 10
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments = Map.empty

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest009.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 181<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1311_67L<Cent>, 500_00L<Cent>, 750_00L<Cent>, 61_67L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = startDate.AddDays 181
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 10
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1311_67L<Cent>
                NetEffect = 1311_67L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``10) Verified example`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 12, 21)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12))
                PaymentCount = 11
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                70<OffsetDay>, [| ActualPayment.QuickConfirmed 272_84L<Cent> |]
                84<OffsetDay>, [| ActualPayment.QuickConfirmed 272_84L<Cent>; ActualPayment.QuickConfirmed 272_84L<Cent> |]
                85<OffsetDay>, [| ActualPayment.QuickConfirmed 272_84L<Cent> |]
                98<OffsetDay>, [| ActualPayment.QuickConfirmed 272_84L<Cent> |]
                112<OffsetDay>, [| ActualPayment.QuickConfirmed 272_84L<Cent> |]
                126<OffsetDay>, [| ActualPayment.QuickConfirmed 272_84L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest010.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 388<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (1261_73L<Cent>, 471_07L<Cent>, 706_56L<Cent>, 84_10L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = startDate.AddDays 388
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 11
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 1261_73L<Cent>
                NetEffect = 1261_73L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``11) When settling a loan with 3-day late-payment grace period, scheduled payments within the grace period should be treated as missed payments, otherwise the quote balance is too low`` () =
        let startDate = Date(2022, 11, 28)

        let sp = {
            AsOfDate = Date(2023, 2, 8)
            StartDate = startDate
            Principal = 1200_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 12, 12))
                PaymentCount = 11
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = {
                FeeTypes = [| Fee.FeeType.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                Rounding = ValueSome RoundDown
                FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                SettlementRefund = Fee.SettlementRefund.ProRata ValueNone
            }
            ChargeConfig = Charge.Config.DefaultValue
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.None
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 279_01L<Cent> |]
                28<OffsetDay>, [| ActualPayment.QuickConfirmed 279_01L<Cent> |]
                42<OffsetDay>, [| ActualPayment.QuickConfirmed 279_01L<Cent> |]
                56<OffsetDay>, [| ActualPayment.QuickConfirmed 279_01L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest011.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 72<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (973_53L<Cent>, 769_46L<Cent>, 195_68L<Cent>, 8_39L<Cent>, 0L<Cent>, 958_45L<Cent>),
            ({
                OffsetDate = startDate.AddDays 72
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 973_53L<Cent>
                NetEffect = 973_53L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``12) Settlement figure should not be lower than principal`` () =
        let startDate = Date(2024, 1, 29)

        let sp = {
            AsOfDate = Date(2024, 2, 28)
            StartDate = startDate
            Principal = 400_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 2, 28)
                PaymentCount = 4
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
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

        let actualPayments = Map.empty

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest012.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 30<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (495_76L<Cent>, 400_00L<Cent>, 0L<Cent>, 95_76L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = startDate.AddDays 30
                Advances = [||]
                ScheduledPayment = ScheduledPayment.Quick (ValueSome 165_90L<Cent>) ValueNone
                Window = 1
                PaymentDue = 165_90L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 495_76L<Cent>
                NetEffect = 495_76L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``13a) Loan is settled the day before the last scheduled payment is due`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 14)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest013a.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 133<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (429_24L<Cent>, 353_00L<Cent>, 0L<Cent>, 76_24L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 14)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``13b) Loan is settled on the same day as the last scheduled payment is due (but which has not yet been made)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 15)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest013b.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 134<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (432_07L<Cent>, 353_00L<Cent>, 0L<Cent>, 79_07L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.Quick (ValueSome 491_53L<Cent>) ValueNone
                Window = 5
                PaymentDue = 457_65L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 432_07L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``13c) Loan is settled the day after the final schedule payment was due (and which was not made) but is within grace period so does not incur a late-payment fee`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest013c.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 135<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (434_89L<Cent>, 353_00L<Cent>, 0L<Cent>, 81_89L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 16)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 434_89L<Cent>
                NetEffect = 434_89L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``13d) Loan is settled four days after the final schedule payment was due (and which was not made) and is outside grace period so incurs a late-payment fee`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 19)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest013d.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 138<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (453_36L<Cent>, 353_00L<Cent>, 0L<Cent>, 90_36L<Cent>, 10_00L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 19)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 453_36L<Cent>
                NetEffect = 453_36L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``14a) Loan is settled the day before an overpayment (note: if looked at from a later date the overpayment will cause a refund to be due)`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 14)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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
        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest014a.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 133<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (429_24L<Cent>, 353_00L<Cent>, 0L<Cent>, 76_24L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 14)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 4
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome 429_24L<Cent>
                NetEffect = 429_24L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``14b) Loan is settled the same day as an overpayment`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 15)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest014b.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 134<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (-67_93L<Cent>, -67_93L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 15)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.Quick (ValueSome 491_53L<Cent>) ValueNone
                Window = 5
                PaymentDue = 457_65L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 500_00L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueSome -67_93L<Cent>
                NetEffect = 432_07L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``14c) Loan is settled the day after an overpayment`` () =
        let sp = {
            AsOfDate = Date(2023, 3, 16)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest014c.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 135<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (-67_95L<Cent>, -67_93L<Cent>, 0L<Cent>, -2L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2023, 3, 16)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome -67_95L<Cent>
                NetEffect = -67_95L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``15) Loan refund due for a long time, showing interest owed back`` () =
        let sp = {
            AsOfDate = Date(2024, 2, 5)
            StartDate = Date(2022, 11, 1)
            Principal = 1500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 15)
                PaymentCount = 5
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                14<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                44<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                75<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                106<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
                134<OffsetDay>, [| ActualPayment.QuickConfirmed 500_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest015.md" false
                let! item = quote.RevisedSchedule.ScheduleItems |> Map.tryFind 461<OffsetDay> |> toValueOption
                return quote.QuoteResult, item
            }

        let expected = ValueSome (
            PaymentQuote (-72_80L<Cent>, -67_93L<Cent>, 0L<Cent>, -4_87L<Cent>, 0L<Cent>, 0L<Cent>),
            ({
                OffsetDate = Date(2024, 2, 5)
                Advances = [||]
                ScheduledPayment = ScheduledPayment.DefaultValue
                Window = 5
                PaymentDue = 0L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueSome -72_80L<Cent>
                NetEffect = -72_80L<Cent>
                PaymentStatus = Generated
                BalanceStatus = ClosedBalance
                OriginalSimpleInterest = 0L<Cent>
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
            })
        )

        actual |> should equal expected

    [<Fact>]
    let ``16) Settlement quote on the same day a loan is closed has 0L<Cent> payment and 0L<Cent> principal and interest components`` () =
        let sp = {
            AsOfDate = Date(2022, 12, 20)
            StartDate = Date(2022, 12, 19)
            Principal = 250_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 1, 20)
                PaymentCount = 4
                MaxDuration = ValueNone
            }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = {
                ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                Rounding = ValueSome RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
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

        let actualPayments =
            Map [
                1<OffsetDay>, [| ActualPayment.QuickConfirmed 252_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest016.md" false
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
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2018, 2, 28); PaymentCount = 3; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
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
        let actualPayments =
            Map [
                25<OffsetDay>, [| ActualPayment.QuickConfirmed 72_54L<Cent> |]
                53<OffsetDay>, [| ActualPayment.QuickFailed 72_54L<Cent> [||]; ActualPayment.QuickConfirmed 72_54L<Cent> |]
                78<OffsetDay>, [| ActualPayment.QuickConfirmed 72_54L<Cent>; ActualPayment.QuickConfirmed 145_07L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest017.md" false
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
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2018, 2, 28); PaymentCount = 3; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
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
        let actualPayments =
            Map [
                25<OffsetDay>, [| ActualPayment.QuickConfirmed 72_54L<Cent> |]
                53<OffsetDay>, [| ActualPayment.QuickFailed 72_54L<Cent> [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>) |]; ActualPayment.QuickConfirmed 72_54L<Cent> |]
                78<OffsetDay>, [| ActualPayment.QuickConfirmed 72_54L<Cent>; ActualPayment.QuickConfirmed 145_07L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest018.md" false
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
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 2, 22); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
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

        let actualPayments =
            Map [
                5<OffsetDay>, [| ActualPayment.QuickConfirmed -5_10L<Cent> |]
                6<OffsetDay>, [| ActualPayment.QuickConfirmed 2_00L<Cent> |]
                16<OffsetDay>, [| ActualPayment.QuickConfirmed 97_01L<Cent>; ActualPayment.QuickConfirmed 97_01L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest019.md" false
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
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 9, 22); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
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

        let actualPayments =
            Map [
                20<OffsetDay>, [| ActualPayment.QuickConfirmed 200_00L<Cent> |]
                50<OffsetDay>, [| ActualPayment.QuickConfirmed 200_00L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest020.md" false
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
            ScheduleConfig = AutoGenerateSchedule { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2023, 5, 10); PaymentCount = 4; MaxDuration = ValueNone }
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeeConfig = Fee.Config.DefaultValue
            ChargeConfig = Charge.Config.DefaultValue
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

        let actualPayments =
            Map [
                5<OffsetDay>, [| ActualPayment.QuickConfirmed 111_00L<Cent> |]
                21<OffsetDay>, [| ActualPayment.QuickConfirmed 181_01L<Cent> |]
            ]

        let actual =
            voption {
                let! quote = getQuote (IntendedPurpose.Settlement ValueNone) sp actualPayments
                quote.RevisedSchedule.ScheduleItems |> outputMapToHtml "out/QuoteTest021.md" false
                return quote.QuoteResult
            }

        let expected = ValueSome (PaymentQuote (-13_84L<Cent>, -12_94L<Cent>, 0L<Cent>, -90L<Cent>, 0L<Cent>, 0L<Cent>))
        actual |> should equal expected
