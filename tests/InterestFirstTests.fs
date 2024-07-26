namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestFirstTests =

    open Amortisation
    open Calculation
    open Currency
    open CustomerPayments
    open DateDay
    open FeesAndCharges
    open Interest
    open PaymentSchedule
    open Percentages

    [<Fact>]
    let ``1) Simple interest method`` () =
        let startDate = Date(2024, 7, 23)
        let sp = {
            AsOfDate = startDate
            StartDate = startDate
            Principal = 1000_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 8, 2),
                PaymentCount = 5,
                MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> }
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.None
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Rate.Daily <| Percent 0.8m
                Cap = {
                    Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown)
                    Daily = ValueSome <| Amount.Percentage (Percent 0.8m, ValueNone, ValueNone)
                }
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

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest001.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 596_27L<Cent>

    [<Fact>]
    let ``2) Add-on interest method`` () =
        let startDate = Date(2024, 7, 23)
        let sp = {
            AsOfDate = startDate
            StartDate = startDate
            Principal = 1000_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 8, 2),
                PaymentCount = 5,
                MaxDuration = ValueSome { FromDate = startDate; Length = 180<DurationDay> }
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.None
                Charges = [||]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.AddOn
                StandardRate = Rate.Daily <| Percent 0.8m
                Cap = {
                    Total = ValueSome <| Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown)
                    Daily = ValueSome <| Amount.Percentage (Percent 0.8m, ValueNone, ValueNone)
                }
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

        let actualPayments = [||]

        let schedule =
            actualPayments
            |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

        schedule |> ValueOption.iter (_.ScheduleItems >> Formatting.outputListToHtml "out/InterestFirstTest002.md")

        let interestPortion = schedule |> ValueOption.map (fun s -> s.ScheduleItems |> Array.sumBy _.InterestPortion) |> ValueOption.defaultValue 0L<Cent>
        interestPortion |> should equal 838_64L<Cent>
