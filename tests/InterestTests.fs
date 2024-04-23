namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module InterestTests =

    open Calculation
    open Currency
    open DateDay
    open Interest
    open Percentages

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    module RateTests =

        [<Fact>]
        let ``Zero rate converted to annual yields 0%`` () =
            let actual = Rate.Zero |> Rate.annual
            let expected = Percent 0m
            actual |> should equal expected

        [<Fact>]
        let ``Zero rate converted to daily yields 0%`` () =
            let actual = Rate.Zero |> Rate.daily
            let expected = Percent 0m
            actual |> should equal expected

        [<Fact>]
        let ``36,5% annual converted to daily yields 0,1%`` () =
            let actual = Percent 36.5m |> Rate.Annual |> Rate.daily
            let expected = Percent 0.1m
            actual |> should equal expected

        [<Fact>]
        let ``10% daily converted to daily yields the same`` () =
            let actual = Percent 10m |> Rate.Daily |> Rate.daily
            let expected = Percent 10m
            actual |> should equal expected

        [<Fact>]
        let ``10% annual converted to annual yields the same`` () =
            let actual = Percent 10m |> Rate.Annual |> Rate.annual
            let expected = Percent 10m
            actual |> should equal expected

        [<Fact>]
        let ``0,1% daily converted to annual yields 36,5%`` () =
            let actual = Percent 0.1m |> Rate.Daily |> Rate.annual
            let expected = Percent 36.5m
            actual |> should equal expected

    module CapTests =

        [<Fact>]
        let ``No cap total on a €100 principal yields a very large number`` () =
            let actual = Cap.none.Total |> Cap.total 100_00L<Cent>
            let expected = 92_233_720_368_547_758_07m<Cent>
            actual |> should equal expected

        [<Fact>]
        let ``100% cap total on a €100 principal yields €100`` () =
            let actual = interestCapExample.Total |> Cap.total 100_00L<Cent>
            let expected = 100_00m<Cent>
            actual |> should equal expected

    module DailyRatesTests =

        [<Fact>]
        let ``Daily rates with no settlement inside the grace period or promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [||]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate false standardRate promotionalRates fromDay toDay
            let expected = [| 1 .. 10 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Annual (Percent 10m) })
            actual |> should equal expected

        [<Fact>]
        let ``Daily rates with a settlement inside the grace period, but no promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [||]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate true standardRate promotionalRates fromDay toDay
            let expected = [| 1 .. 10 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Zero })
            actual |> should equal expected

        [<Fact>]
        let ``Daily rates with no settlement inside the grace period but with promotional rates`` () =
            let startDate = Date(2024, 4, 10)
            let standardRate = Rate.Annual <| Percent 10m
            let promotionalRates = [|
                ({ DateRange = { Start = Date(2024, 4, 10); End = Date(2024, 4, 15) }; Rate = Rate.Annual (Percent 2m) } : Interest.PromotionalRate)
            |]
            let fromDay = 0<OffsetDay>
            let toDay = 10<OffsetDay>
            let actual = dailyRates startDate false standardRate promotionalRates fromDay toDay
            let expected =
                [|
                    [| 1 .. 5 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Annual (Percent 2m) })
                    [| 6 .. 10 |] |> Array.map(fun d -> { RateDay = d * 1<OffsetDay>; InterestRate = Rate.Annual (Percent 10m) })
                |]
                |> Array.concat
            actual |> should equal expected

    module PromotionalRatesTests =

        open Amortisation
        open Calculation
        open CustomerPayments
        open FeesAndCharges
        open PaymentSchedule

        [<Fact>]
        let ``Mortgage quote with a five-year fixed interest deal and a mortgage fee added to the loan`` () =
            let sp = {
                AsOfDate = Date(2024, 4, 11)
                StartDate = Date(2024, 4, 11)
                Principal = 192_000_00L<Cent>
                PaymentSchedule = RegularFixedSchedule [|
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 5, 11); PaymentCount = 60; PaymentAmount = 1225_86L<Cent> }
                    { UnitPeriodConfig = UnitPeriod.Config.Monthly(1, 2029, 5, 11); PaymentCount = 180; PaymentAmount = 1525_12L<Cent> }
                |]
                FeesAndCharges = {
                    Fees = [| Fee.MortageFee <| Amount.Simple 999_00L<Cent> |]
                    FeesAmortisation = Fees.FeeAmortisation.AmortiseBeforePrincipal
                    FeesSettlementRefund = Fees.SettlementRefund.None
                    Charges = [||]
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 1<DurationDay>
                }
                Interest = {
                    StandardRate = Rate.Annual <| Percent 7.985m
                    Cap = Cap.none
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [|
                        { DateRange = { Start = Date(2024, 4, 11); End = Date(2029, 4, 10) }; Rate = Rate.Annual <| Percent 4.535m }
                    |]
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                    RoundingOptions = RoundingOptions.recommended
                    MinimumPayment = NoMinimumPayment
                    PaymentTimeout = 3<DurationDay>
                }
            }

            let actualPayments = [||]

            let schedule =
                actualPayments
                |> Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false

            schedule |> ValueOption.iter(_.ScheduleItems >> Formatting.outputListToHtml "out/PromotionalRatesTest001.md")

            let actual = schedule |> ValueOption.map (_.ScheduleItems >> Array.last)
            let expected = ValueSome {
                OffsetDate = Date(2044, 4, 11)
                OffsetDay = 7305<OffsetDay>
                Advances = [||]
                ScheduledPayment = ScheduledPaymentType.Original 1525_12L<Cent>
                PaymentDue = 1523_25L<Cent>
                ActualPayments = [||]
                GeneratedPayment = ValueNone
                NetEffect = 1523_25L<Cent>
                PaymentStatus = NotYetDue
                BalanceStatus = ClosedBalance
                NewInterest = 10_26.07665657m<Cent>
                NewCharges = [||]
                PrincipalPortion = 1512_99L<Cent>
                FeesPortion = 0L<Cent>
                InterestPortion = 10_26L<Cent>
                ChargesPortion = 0L<Cent>
                FeesRefund = 0L<Cent>
                PrincipalBalance = 0L<Cent>
                FeesBalance = 0L<Cent>
                InterestBalance = 0m<Cent>
                ChargesBalance = 0L<Cent>
                SettlementFigure = 1523_25L<Cent>
                FeesRefundIfSettled = 0L<Cent>
            }
            actual |> should equal expected

