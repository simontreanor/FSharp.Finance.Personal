namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

/// https://www.consumerfinance.gov/rules-policy/regulations/1026/j/
/// 
/// Appendix J to Part 1026 — Annual Percentage Rate Computations for Closed-End Credit Transactions
///
/// (c) Examples for the actuarial method
module AprUsActuarialTests =

    open Apr
    open Calculation
    open DateDay
    open UnitPeriod

    /// (c)(1) Single advance transaction, with or without an odd first period, and otherwise regular
    let calculate1 advanceValue paymentValue paymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; TransferDate = consummationDate; Value = advanceValue } |]
        let payments = intervalSchedule |> UnitPeriod.generatePaymentSchedule paymentCount Duration.Unlimited Direction.Forward |> Array.map(fun d -> { TransferType = Payment; TransferDate = d; Value = paymentValue })
        UsActuarial.generalEquation consummationDate firstFinanceChargeEarnedDate advances payments
        |> Util.getAprOr 0m |> Percent.fromDecimal |> Percent.round 2

    [<Fact>]
    let ``Example (c)(1)(i): Monthly payments (regular first period)`` () =
        let actual = calculate1 5000_00L<Cent> 230_00L<Cent> 24 (Monthly (1, 1978, 2, 10)) (Date(1978, 1, 10))
        let expected = Percent 9.69m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(ii): Monthly payments (long first period)`` () =
        let actual = calculate1 6000_00L<Cent> 200_00L<Cent> 36 (Monthly (1, 1978, 4, 1)) (Date(1978, 2, 10))
        let expected = Percent 11.82m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(iii): Semimonthly payments (short first period)`` () =
        let actual = calculate1 5000_00L<Cent> 219_17L<Cent> 24 (SemiMonthly (1978, 3, 1, 16)) (Date(1978, 2, 23))
        let expected = Percent 10.34m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(iv): Quarterly payments (long first period)`` () =
        let actual = calculate1 10000_00L<Cent> 385_00L<Cent> 40 (Monthly (3, 1978, 10, 1)) (Date(1978, 5, 23))
        let expected = Percent 8.97m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(v): Weekly payments (long first period)`` () =
        let actual = calculate1 500_00L<Cent> 17_60L<Cent> 30 (Weekly (1, Date(1978, 4, 21))) (Date(1978, 3, 20))
        let expected = Percent 14.96m
        actual |> should equal expected

    /// (c)(2) Single advance transaction, with an odd first payment, with or without an odd first period, and otherwise regular
    let calculate2 advanceValue firstPayment regularPaymentValue regularPaymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; TransferDate = consummationDate; Value = advanceValue } |]
        let payments = intervalSchedule |> generatePaymentSchedule regularPaymentCount Duration.Unlimited Direction.Forward |> Array.map(fun d -> { TransferType = Payment; TransferDate = d; Value = regularPaymentValue })
        let payments' = Array.concat [| [| firstPayment |]; payments |]
        UsActuarial.generalEquation consummationDate firstFinanceChargeEarnedDate advances payments'
        |> Util.getAprOr 0m |> Percent.fromDecimal |> Percent.round 2

    [<Fact>]
    let ``Example (c)(2)(i): Monthly payments (regular first period and irregular first payment)`` () =
        let firstPayment = { TransferType = Advance; Value = 250_00L<Cent>; TransferDate = Date(1978, 2, 10) } 
        let actual = calculate2 5000_00L<Cent> firstPayment 230_00L<Cent> 23 (Monthly (1, 1978, 3, 10)) (Date(1978, 1, 10))
        let expected = Percent 10.08m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(2)(ii): Payments every 4 weeks (long first period and irregular first payment)`` () =
        let firstPayment = { TransferType = Advance; Value = 39_50L<Cent>; TransferDate = Date(1978, 4, 20) }
        let actual = calculate2 400_00L<Cent> firstPayment 38_31L<Cent> 11 (Weekly (4, Date(1978, 5, 18))) (Date(1978, 3, 18))
        let expected = Percent 28.50m
        actual |> should equal expected

    /// (c)(3) Single advance transaction, with an odd final payment, with or without an odd first period, and otherwise regular
    let calculate3 advanceValue lastPayment regularPaymentValue regularPaymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; TransferDate = consummationDate; Value = advanceValue } |]
        let payments = intervalSchedule |> generatePaymentSchedule regularPaymentCount Duration.Unlimited Direction.Forward |> Array.map(fun d -> { TransferType = Payment; TransferDate = d; Value = regularPaymentValue })
        let payments' = Array.concat [| payments; [| lastPayment |] |]
        UsActuarial.generalEquation consummationDate firstFinanceChargeEarnedDate advances payments'
        |> Util.getAprOr 0m |> Percent.fromDecimal |> Percent.round 2

    [<Fact>]
    let ``Example (c)(3)(i): Monthly payments (regular first period and irregular final payment)`` () =
        let lastPayment = { TransferType = Advance; Value = 280_00L<Cent>; TransferDate = Date(1978, 2, 10).AddMonths(23) }
        let actual = calculate3 5000_00L<Cent> lastPayment 230_00L<Cent> 23 (Monthly (1, 1978, 2, 10)) (Date(1978, 1, 10))
        let expected = Percent 10.50m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(3)(ii): Payments every 2 weeks (short first period and irregular final payment)`` () =
        let lastPayment = { TransferType = Advance; Value = 30_00L<Cent>; TransferDate = Date(1978, 4, 11).AddDays(14 * 19) }
        let actual = calculate3 200_00L<Cent> lastPayment 9_50L<Cent> 19 (Weekly (2, Date(1978, 4, 11))) (Date(1978, 4, 3))
        let expected = Percent 12.22m
        actual |> should equal expected

     /// (c)(4) Single advance transaction, with an odd first payment, odd final payment, with or without an odd first period, and otherwise regular
    let calculate4 advanceValue firstPayment lastPayment regularPaymentValue regularPaymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; TransferDate = consummationDate; Value = advanceValue } |]
        let payments = intervalSchedule |> generatePaymentSchedule regularPaymentCount Duration.Unlimited Direction.Forward |> Array.map(fun d -> { TransferType = Payment; TransferDate = d; Value = regularPaymentValue })
        let payments' = Array.concat [| [| firstPayment |]; payments; [| lastPayment |] |]
        UsActuarial.generalEquation consummationDate firstFinanceChargeEarnedDate advances payments'
        |> Util.getAprOr 0m |> Percent.fromDecimal |> Percent.round 2

    [<Fact>]
    let ``Example (c)(4)(i): Monthly payments (regular first period, irregular first payment, and irregular final payment)`` () =
        let firstPayment = { TransferType = Payment; Value = 250_00L<Cent>; TransferDate = Date(1978, 2, 10) }
        let lastPayment = { TransferType = Payment; Value = 280_00L<Cent>; TransferDate = Date(1978, 3, 10).AddMonths(22) }
        let actual = calculate4 5000_00L<Cent> firstPayment lastPayment 230_00L<Cent> 22 (Monthly (1, 1978, 3, 10)) (Date(1978, 1, 10))
        let expected = Percent 10.90m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(4)(ii): Payments every two months (short first period, irregular first payment, and irregular final payment)`` () =
        let firstPayment = { TransferType = Payment; Value = 449_36L<Cent>; TransferDate = Date(1978, 3, 1) }
        let lastPayment = { TransferType = Payment; Value = 200_00L<Cent>; TransferDate = Date(1978, 5, 1).AddMonths(36) }
        let actual = calculate4 8000_00L<Cent> firstPayment lastPayment 465_00L<Cent> 18 (Monthly (2, 1978, 5, 1)) (Date(1978, 1, 10))
        let expected = Percent 7.30m
        actual |> should equal expected

    /// (c)(5) Single advance, single payment transaction
    let calculate5 advance payment =
        let consummationDate = advance.TransferDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| advance |]
        let payments = [| payment |]
        UsActuarial.generalEquation consummationDate firstFinanceChargeEarnedDate advances payments
        |> Util.getAprOr 0m |> Percent.fromDecimal |> Percent.round 2

    [<Fact>]
    let ``Example (c)(5)(i): Single advance, single payment (term of less than 1 year, measured in days)`` () =
        let advance = { TransferType = Payment; TransferDate = Date(1978, 1, 3); Value = 1000_00L<Cent> }
        let payment = { TransferType = Payment; TransferDate = Date(1978, 9, 15); Value = 1080_00L<Cent> }
        let actual = calculate5 advance payment
        let expected = Percent 11.45m
        actual |> should equal expected

    /// examples created while debugging the notebook, used to test edge-cases, all confirmed using Excel
    module ExtraExamples =

        [<Fact>]
        let ``Example (c)(1)(iv) [modified]: Quarterly payments (shorter first period)`` () =
            let actual = calculate1 10000_00L<Cent> 385_00L<Cent> 40 (Monthly (3, 1978, 10, 1)) (Date(1978, 6, 23))
            let expected = Percent 9.15m
            actual |> should equal expected

        [<Fact>]
        let ``Example (c)(1)(iv) [modified]: Quarterly payments (shorter first period: less than unit-period)`` () =
            let actual = calculate1 10000_00L<Cent> 385_00L<Cent> 40 (Monthly (3, 1978, 10, 1)) (Date(1978, 7, 23))
            let expected = Percent 9.32m
            actual |> should equal expected

        [<Fact>]
        let ``Daily payments`` () =
            let actual = calculate1 1000_00L<Cent> 220_00L<Cent> 5 (Daily (Date(2023,11,30))) (Date(2023,10,26))
            let expected = Percent 94.15m
            actual |> should equal expected

        [<Fact>]
        let ``Weekly payments with long first period`` () =
            let actual = calculate1 1000_00L<Cent> 250_00L<Cent> 5 (Weekly(1, Date(2023,11,30))) (Date(2023,10,28))
            let expected = Percent 176.52m
            actual |> should equal expected

        [<Fact>]
        let ``Weekly payments with first period equal to unit-period`` () =
            let actual = calculate1 1000_00L<Cent> 250_00L<Cent> 5 (Weekly(1, Date(2023,11,30))) (Date(2023,11,23))
            let expected = Percent 412.40m
            actual |> should equal expected

        [<Fact>]
        let ``Weekly payments with first period shorter than unit-period`` () =
            let actual = calculate1 1000_00L<Cent> 250_00L<Cent> 5 (Weekly(1, Date(2023,11,30))) (Date(2023,11,24))
            let expected = Percent 434.30m
            actual |> should equal expected

        [<Fact>]
        let ``Yearly payments`` () =
            let actual = calculate1 1000_00L<Cent> 500_00L<Cent> 5 (Monthly (12, 2023, 11, 30)) (Date(2023, 10, 26))
            let expected = Percent 78.34m
            actual |> should equal expected
