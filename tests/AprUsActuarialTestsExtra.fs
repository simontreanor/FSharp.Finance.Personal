namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit
open System.Text.Json

open FSharp.Finance.Personal

module AprActuarialTestsExtra =

    open Apr
    open Calculation
    open DateDay

    type AprUsActuarialTestItemDto = {
        StartDate: DateOnly
        Principal: decimal
        PaymentValue: decimal
        PaymentDates: DateOnly array
        ExpectedApr: decimal
        ActualApr: decimal
    }

    type AprUsActuarialTestItem = {
        StartDate: Date
        Principal: int64<Cent>
        PaymentValue: int64<Cent>
        PaymentDates: Date array
        ExpectedApr: Percent
        ActualApr: Percent
    }

    let aprUsActuarialTestData =
        IO.File.ReadAllText $"{__SOURCE_DIRECTORY__}/../tests/io/in/AprUsActuarialTestData.json"
        |> JsonSerializer.Deserialize<AprUsActuarialTestItemDto array>
        |> Array.choose (fun ssi ->
            let startDate = ssi.StartDate |> fun d -> Date(d.Year, d.Month, d.Day)
            let principal = Cent.fromDecimal ssi.Principal
            let paymentValue = Cent.fromDecimal ssi.PaymentValue

            let paymentDates =
                ssi.PaymentDates |> Array.map (fun d -> Date(d.Year, d.Month, d.Day))

            let payments =
                paymentDates
                |> Array.map (fun d -> {
                    TransferType = Payment
                    TransferDate = d
                    Value = paymentValue
                })

            let actualApr =
                calculate (CalculationMethod.UsActuarial 8) principal startDate payments

            match actualApr with
            | Solution.Found(apr, _, _) ->
                Some {
                    StartDate = startDate
                    Principal = principal
                    PaymentValue = paymentValue
                    PaymentDates = paymentDates
                    ExpectedApr = Percent ssi.ExpectedApr
                    ActualApr = apr |> Percent.fromDecimal |> Percent.round 4
                }
            | _ -> None
        )
        |> Util.toMemberData

    // [<Theory>]
    [<MemberData(nameof (aprUsActuarialTestData))>]
    let ``Actual APRs match expected APRs under the US actuarial method`` testItem =
        testItem.ActualApr |> should equal testItem.ExpectedApr

    /// regression tests for the unit-period detection and unit-period quotient calculations
    module RegressionExamples =

        let advance date value = {
            TransferType = Advance
            TransferDate = date
            Value = value
        }

        let payment date value = {
            TransferType = Payment
            TransferDate = date
            Value = value
        }

        let getApr solution =
            solution |> getAprOr 0m |> Percent.fromDecimal |> Percent.round 2

        [<Fact>]
        let ``Reg Z (b)(5)(vi): single advance, single payment, term of a whole month`` () =
            // $500 advanced on 2024-06-01 and repaid by a single $550 payment on 2024-07-01: the term (30 days) is equal
            // to a whole number of months (1), so there is 1 unit-period in the term and 12 unit-periods per year,
            // giving APR = 12 x (550/500 - 1) = 1.2 = 120.00%
            let actual =
                calculate (CalculationMethod.UsActuarial 5) 500_00L<Cent> (Date(2024, 6, 1)) [|
                    payment (Date(2024, 7, 1)) 550_00L<Cent>
                |]
                |> getApr

            let expected = Percent 120m
            actual |> should equal expected

        [<Fact>]
        let ``Reg Z (b)(5)(vii): single advance, single payment, term of 15 days`` () =
            // $500 advanced on 2024-06-01 and repaid by a single $550 payment on 2024-06-16: the term (15 days) is not
            // equal to a whole number of months, so there is 1 unit-period in the term and 365/15 unit-periods per year,
            // giving APR = 365/15 x (550/500 - 1) = 2.4333... = 243.33%
            let actual =
                calculate (CalculationMethod.UsActuarial 5) 500_00L<Cent> (Date(2024, 6, 1)) [|
                    payment (Date(2024, 6, 16)) 550_00L<Cent>
                |]
                |> getApr

            let expected = Percent 243.33m
            actual |> should equal expected

        [<Fact>]
        let ``Irregular two-payment schedule uses the average interval as the unit-period`` () =
            // $1000 advanced on 2024-01-01 with $550 payments at day 28 (2024-01-29) and day 64 (2024-03-05):
            // the intervals (28 days and 36 days) normalise to 4 weeks and 1 month; neither occurs more than once, so the
            // unit-period is the nearest to their average length ((28 + 30) / 2 = 29 days), i.e. 4 weeks (13 per year);
            // payment 1 falls at day 28 = exactly 1 unit-period and payment 2 at day 64 = 2 unit-periods + 8/28 remainder,
            // so per the general equation 1000 = 550/(1+i) + 550/((1 + (8/28)i)(1+i)^2), solved independently by bisection
            // to i = 0.0600756910, giving APR = 13 x 0.0600756910 = 0.78098398... = 78.10%
            let actual =
                UsActuarial.generalEquation (Date(2024, 1, 1)) (Date(2024, 1, 1)) [|
                    advance (Date(2024, 1, 1)) 1000_00L<Cent>
                |] [|
                    payment (Date(2024, 1, 29)) 550_00L<Cent>
                    payment (Date(2024, 3, 5)) 550_00L<Cent>
                |]
                |> getApr

            let expected = Percent 78.10m
            actual |> should equal expected

        /// the daily and weekly unit-period quotients must be calculated from the actual transfer dates, so moving a
        /// payment to a later date must lower the APR
        [<Fact>]
        let ``Daily unit-period quotients are date-sensitive`` () =
            // $300 advanced on 2024-01-01 with three payments of $102: with a daily unit-period, the quotient is the
            // actual number of days from the start of the term to each payment, so each variant solves a different equation
            let aprForDays days =
                UsActuarial.generalEquation (Date(2024, 1, 1)) (Date(2024, 1, 1)) [|
                    advance (Date(2024, 1, 1)) 300_00L<Cent>
                |] (days |> Array.map (fun d -> payment (Date(2024, 1, 1).AddDays d) 102_00L<Cent>))
                |> getApr

            let actual = [| 1, 2, 3; 1, 2, 4; 1, 2, 9 |] |> Array.map (fun (a, b, c) -> aprForDays [| a; b; c |])
            let expected = [| Percent 363.80m; Percent 311.98m; Percent 182.59m |]
            actual |> should equal expected

        [<Fact>]
        let ``Weekly unit-period quotients are date-sensitive`` () =
            // $300 advanced on 2024-01-01 with three roughly weekly payments of $102: the number of unit-periods and the
            // remaining fraction are determined by dividing the actual number of days to each payment by 7, so delaying
            // the final payment by a day must lower the APR
            let aprForDays days =
                UsActuarial.generalEquation (Date(2024, 1, 1)) (Date(2024, 1, 1)) [|
                    advance (Date(2024, 1, 1)) 300_00L<Cent>
                |] (days |> Array.map (fun d -> payment (Date(2024, 1, 1).AddDays d) 102_00L<Cent>))
                |> getApr

            let actual = aprForDays [| 7; 14; 21 |], aprForDays [| 7; 14; 22 |]
            let expected = Percent 51.83m, Percent 50.62m
            actual |> should equal expected
