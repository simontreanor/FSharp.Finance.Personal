namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module FeesAndChargesTests =

    open Amortisation
    open Calculation
    open CustomerPayments
    open Currency
    open DateDay
    open FeesAndCharges
    open Percentages
    open PaymentSchedule

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    module ChargesTests =

        [<Fact>]
        let ``One charge type per day`` () =
            let sp = {
                AsOfDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
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
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }

            let actualPayments = [|
                CustomerPayment.ActualConfirmed 4<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualFailed 35<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualFailed 36<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualConfirmed 40<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualFailed 66<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualFailed 66<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualConfirmed 70<OffsetDay> 456_84L<Cent>
                CustomerPayment.ActualConfirmed 94<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualConfirmed 125<OffsetDay> 456_84L<Cent>
            |]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> (Formatting.outputListToHtml "out/FeesAndChargesTest001.md" false))

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2023, 3, 31)
                OffsetDay = 125<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 456_84L<Cent>; Metadata = Map.empty }
                Window = 5
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueNone
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 11256.472m<Cent>
                NewInterest = 11256.472m<Cent>
                NewCharges = [||]
                PrincipalPortion = 344_28L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 112_56L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 109_61L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 109_61L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
            actual |> should equal expected

        [<Fact>]
        let ``One charge type per product`` () =
            let sp = {
                AsOfDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
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
                    ChargesGrouping = OneChargeTypePerProduct
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }

            let actualPayments = [|
                CustomerPayment.ActualConfirmed 4<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualFailed 35<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualFailed 36<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualConfirmed 40<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualFailed 66<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualFailed 66<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualConfirmed 70<OffsetDay> 456_84L<Cent>
                CustomerPayment.ActualConfirmed 94<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualConfirmed 125<OffsetDay> 456_84L<Cent>
            |]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> (Formatting.outputListToHtml "out/FeesAndChargesTest002.md" false))

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2023, 3, 31)
                OffsetDay = 125<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 456_84L<Cent>; Metadata = Map.empty }
                Window = 5
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueNone
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 10665.24m<Cent>
                NewInterest = 10665.24m<Cent>
                NewCharges = [||]
                PrincipalPortion = 350_19L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 106_65L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 79_86L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 79_86L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
            actual |> should equal expected

        [<Fact>]
        let ``All charges applied`` () =
            let sp = {
                AsOfDate = Date(2023, 4, 1)
                StartDate = Date(2022, 11, 26)
                Principal = 1500_00L<Cent>
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
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
                    ChargesGrouping = AllChargesApplied
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Method = Interest.Method.Simple
                    StandardRate = Interest.Rate.Daily (Percent 0.8m)
                    Cap = interestCapExample
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }

            let actualPayments = [|
                CustomerPayment.ActualConfirmed 4<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualFailed 35<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualFailed 36<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualConfirmed 40<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualFailed 66<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualFailed 66<OffsetDay> 456_88L<Cent> [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |]
                CustomerPayment.ActualConfirmed 70<OffsetDay> 456_84L<Cent>
                CustomerPayment.ActualConfirmed 94<OffsetDay> 456_88L<Cent>
                CustomerPayment.ActualConfirmed 125<OffsetDay> 456_84L<Cent>
            |]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> (Formatting.outputListToHtml "out/FeesAndChargesTest003.md" false))

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2023, 3, 31)
                OffsetDay = 125<OffsetDay>
                Advances = [||]
                ScheduledPayment = { ScheduledPaymentType = ScheduledPaymentType.Original 456_84L<Cent>; Metadata = Map.empty }
                Window = 5
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 456_84L<Cent>; Metadata = Map.empty } |]
                GeneratedPayment = ValueNone
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
                OriginalSimpleInterest = 0m<Cent>
                ContractualInterest = 0m<Cent>
                SimpleInterest = 11552.088m<Cent>
                NewInterest = 11552.088m<Cent>
                NewCharges = [||]
                PrincipalPortion = 341_32L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 115_52L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 124_49L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 124_49L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
            actual |> should equal expected
