namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module MortgageTests =

    open CustomerPayments
    open PaymentSchedule

    [<Fact>]
    let ``1) Standard mortgage in principal`` () =
    
        let startDate = Date(2024, 6, 1)

        let sp = {
            AsOfDate = Date(2024, 2, 29)
            StartDate = startDate
            Principal = 192_000_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 7, 1),
                PaymentCount = 240
            )
            FeesAndCharges = {
                Fees = [| |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [||]
                ChargesHolidays = [||]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 6.29m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                InitialGracePeriod = 0<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
                NegativeInterestOption = ApplyNegativeInterest
            }
        }

        let actual =
            voption {
                let! schedule = sp |> calculate AroundZero
                schedule.Items |> Formatting.outputListToHtml "out/MortgageTest001.md"
                return schedule.LevelPayment
            }

        let expected = ValueSome 1408_31L<Cent>
        actual |> should equal expected
