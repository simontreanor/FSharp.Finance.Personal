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
    open Interest
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
                { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay =  35<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  36<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  40<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  70<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
                { PaymentDay =  94<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay = 125<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
            |]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/FeesAndChargesTest001.md")

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2023, 3, 31)
                OffsetDay = 125<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 456_84L<Cent>
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| ActualPaymentStatus.Confirmed 456_84L<Cent> |]
                GeneratedPayment = ValueNone
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
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
                { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay =  35<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  36<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  40<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  70<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
                { PaymentDay =  94<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay = 125<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
            |]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/FeesAndChargesTest002.md")

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2023, 3, 31)
                OffsetDay = 125<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 456_84L<Cent>
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| ActualPaymentStatus.Confirmed 456_84L<Cent> |]
                GeneratedPayment = ValueNone
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
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
                { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay =  35<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  36<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  40<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Failed (456_88L<Cent>, [| Charge.InsufficientFunds (Amount.Simple 10_00L<Cent>) |])) }
                { PaymentDay =  70<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
                { PaymentDay =  94<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
                { PaymentDay = 125<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
            |]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/FeesAndChargesTest003.md")

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2023, 3, 31)
                OffsetDay = 125<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 456_84L<Cent>
                PaymentDue = 456_84L<Cent>
                ActualPayments = [| ActualPaymentStatus.Confirmed 456_84L<Cent> |]
                GeneratedPayment = ValueNone
                NetEffect = 456_84L<Cent>
                PaymentStatus = PaymentMade
                BalanceStatus = OpenBalance
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
