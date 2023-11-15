namespace FSharp.Finance.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance

/// https://www.consumerfinance.gov/rules-policy/regulations/1026/j/
/// 
/// Appendix J to Part 1026 — Annual Percentage Rate Computations for Closed-End Credit Transactions
///
/// (c) Examples for the actuarial method
module AprUsActuarialTests =

    open Apr
    open UnitPeriod

    let roundTo (dp: int) m =
        let x = 10m ** decimal dp
        divRem (m * x) 1m
        |> fun dr -> if dr.Remainder <= 0.5m then dr.Quotient else dr.Quotient + 1m
        |> fun m -> m / x

    /// (c)(1) Single advance transaction, with or without an odd first period, and otherwise regular
    let calculate1 advanceAmount payment paymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; Date = consummationDate; Amount = advanceAmount } |]
        let payments = intervalSchedule |> Schedule.generate paymentCount Schedule.Forward |> Array.map(fun dt -> { TransferType = Payment; Date = dt; Amount = payment })
        generalEquation consummationDate firstFinanceChargeEarnedDate advances payments
        |> (*) 100m |> roundTo 2

    [<Fact>]
    let ``Example (c)(1)(i): Monthly payments (regular first period)`` () =
        let actual = calculate1 5000m 230m 24 (Monthly (1, MonthlyConfig (1978, 2, TrackingDay 10))) (DateTime(1978, 1, 10))
        let expected = 9.69m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(ii): Monthly payments (long first period)`` () =
        let actual = calculate1 6000m 200m 36 (Monthly (1, MonthlyConfig (1978, 4, TrackingDay 1))) (DateTime(1978, 2, 10))
        let expected = 11.82m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(iii): Semimonthly payments (short first period)`` () =
        let actual = calculate1 5000m 219.17m 24 (SemiMonthly (SemiMonthlyConfig (1978, 3, TrackingDay 1, TrackingDay 16))) (DateTime(1978, 2, 23))
        let expected = 10.34m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(iv): Quarterly payments (long first period)`` () =
        let actual = calculate1 10000m 385m 40 (Monthly (3, MonthlyConfig (1978, 10, TrackingDay 1))) (DateTime(1978, 5, 23))
        let expected = 8.97m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(1)(v): Weekly payments (long first period)`` () =
        let actual = calculate1 500m 17.60m 30 (Weekly (1, DateTime(1978, 4, 21))) (DateTime(1978, 3, 20))
        let expected = 14.96m
        actual |> should equal expected

    /// (c)(2) Single advance transaction, with an odd first payment, with or without an odd first period, and otherwise regular
    let calculate2 advanceAmount firstPayment regularPaymentAmount regularPaymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; Date = consummationDate; Amount = advanceAmount } |]
        let payments = intervalSchedule |> Schedule.generate regularPaymentCount Schedule.Forward |> Array.map(fun dt -> { TransferType = Payment; Date = dt; Amount = regularPaymentAmount })
        let payments' = Array.concat [| [| firstPayment |]; payments |]
        generalEquation consummationDate firstFinanceChargeEarnedDate advances payments' |> fun apr -> Decimal.Round(apr, 10)
        |> (*) 100m |> roundTo 2

    [<Fact>]
    let ``Example (c)(2)(i): Monthly payments (regular first period and irregular first payment)`` () =
        let firstPayment = { TransferType = Advance; Amount = 250m; Date = DateTime(1978, 2, 10) } 
        let actual = calculate2 5000m firstPayment 230m 23 (Monthly (1, MonthlyConfig (1978, 3, TrackingDay 10))) (DateTime(1978, 1, 10))
        let expected = 10.08m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(2)(ii): Payments every 4 weeks (long first period and irregular first payment)`` () =
        let firstPayment = { TransferType = Advance; Amount = 39.50m; Date = DateTime(1978, 4, 20) }
        let actual = calculate2 400m firstPayment 38.31m 11 (Weekly (4, DateTime(1978, 5, 18))) (DateTime(1978, 3, 18))
        let expected = 28.50m
        actual |> should equal expected

    /// (c)(3) Single advance transaction, with an odd final payment, with or without an odd first period, and otherwise regular
    let calculate3 advanceAmount lastPayment regularPaymentAmount regularPaymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; Date = consummationDate; Amount = advanceAmount } |]
        let payments = intervalSchedule |> Schedule.generate regularPaymentCount Schedule.Forward |> Array.map(fun dt -> { TransferType = Payment; Date = dt; Amount = regularPaymentAmount })
        let payments' = Array.concat [| payments; [| lastPayment |] |]
        generalEquation consummationDate firstFinanceChargeEarnedDate advances payments'
        |> (*) 100m |> roundTo 2

    [<Fact>]
    let ``Example (c)(3)(i): Monthly payments (regular first period and irregular final payment)`` () =
        let lastPayment = { TransferType = Advance; Amount = 280m; Date = DateTime(1978, 2, 10).AddMonths(23) }
        let actual = calculate3 5000m lastPayment 230m 23 (Monthly (1, MonthlyConfig(1978, 2, TrackingDay 10))) (DateTime(1978, 1, 10))
        let expected = 10.50m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(3)(ii): Payments every 2 weeks (short first period and irregular final payment)`` () =
        let lastPayment = { TransferType = Advance; Amount = 30m; Date = DateTime(1978, 4, 11).AddDays(14.*19.) }
        let actual = calculate3 200m lastPayment 9.50m 19 (Weekly (2, DateTime(1978, 4, 11))) (DateTime(1978, 4, 3))
        let expected = 12.22m
        actual |> should equal expected

     /// (c)(4) Single advance transaction, with an odd first payment, odd final payment, with or without an odd first period, and otherwise regular
    let calculate4 advanceAmount firstPayment lastPayment regularPaymentAmount regularPaymentCount intervalSchedule advanceDate =
        let consummationDate = advanceDate
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| { TransferType = Advance; Date = consummationDate; Amount = advanceAmount } |]
        let payments = intervalSchedule |> Schedule.generate regularPaymentCount Schedule.Forward |> Array.map(fun dt -> { TransferType = Payment; Date = dt; Amount = regularPaymentAmount })
        let payments' = Array.concat [| [| firstPayment |]; payments; [| lastPayment |] |]
        generalEquation consummationDate firstFinanceChargeEarnedDate advances payments'
        |> (*) 100m |> roundTo 2

    [<Fact>]
    let ``Example (c)(4)(i): Monthly payments (regular first period, irregular first payment, and irregular final payment)`` () =
        let firstPayment = { TransferType = Payment; Amount = 250m; Date = DateTime(1978, 2, 10) }
        let lastPayment = { TransferType = Payment; Amount = 280m; Date = DateTime(1978, 3, 10).AddMonths(22) }
        let actual = calculate4 5000m firstPayment lastPayment 230m 22 (Monthly (1, MonthlyConfig(1978, 3, TrackingDay 10))) (DateTime(1978, 1, 10))
        let expected = 10.90m
        actual |> should equal expected

    [<Fact>]
    let ``Example (c)(4)(ii): Payments every two months (short first period, irregular first payment, and irregular final payment)`` () =
        let firstPayment = { TransferType = Payment; Amount = 449.36m; Date = DateTime(1978, 3, 1) }
        let lastPayment = { TransferType = Payment; Amount = 200m; Date = DateTime(1978, 5, 1).AddMonths(36) }
        let actual = calculate4 8000m firstPayment lastPayment 465m 18 (Monthly (2, MonthlyConfig(1978, 5, TrackingDay 1))) (DateTime(1978, 1, 10))
        let expected = 7.30m
        actual |> should equal expected

    /// (c)(5) Single advance, single payment transaction
    let calculate5 advance payment =
        let consummationDate = advance.Date
        let firstFinanceChargeEarnedDate = consummationDate
        let advances = [| advance |]
        let payments = [| payment |]
        generalEquation consummationDate firstFinanceChargeEarnedDate advances payments
        |> (*) 100m |> roundTo 2

    [<Fact>]
    let ``Example (c)(5)(i): Single advance, single payment (term of less than 1 year, measured in days)`` () =
        let advance = { TransferType = Payment; Date = DateTime(1978, 1, 3); Amount = 1000m }
        let payment = { TransferType = Payment; Date = DateTime(1978, 9, 15); Amount = 1080m }
        let actual = calculate5 advance payment
        let expected = 11.45m
        actual |> should equal expected

    /// examples created while debugging the notebook, used to test edge-cases, all confirmed using Excel
    module ExtraExamples =

        [<Fact>]
        let ``Example (c)(1)(iv) [modified]: Quarterly payments (shorter first period)`` () =
            let actual = calculate1 10000m 385m 40 (Monthly (3, MonthlyConfig (1978, 10, TrackingDay 1))) (DateTime(1978, 6, 23))
            let expected = 9.15m
            actual |> should equal expected

        [<Fact>]
        let ``Example (c)(1)(iv) [modified]: Quarterly payments (shorter first period: less than unit-period)`` () =
            let actual = calculate1 10000m 385m 40 (Monthly (3, MonthlyConfig (1978, 10, TrackingDay 1))) (DateTime(1978, 7, 23))
            let expected = 9.32m
            actual |> should equal expected

        [<Fact>]
        let ``Daily payments`` () =
            let actual = calculate1 1000m 220m 5 (Daily (DateTime(2023,11,30))) (DateTime(2023,10,26))
            let expected = 94.15m
            actual |> should equal expected

        [<Fact>]
        let ``Weekly payments with long first period`` () =
            let actual = calculate1 1000m 250m 5 (Weekly(1, DateTime(2023,11,30))) (DateTime(2023,10,28))
            let expected = 176.52m
            actual |> should equal expected

        [<Fact>]
        let ``Weekly payments with first period equal to unit-period`` () =
            let actual = calculate1 1000m 250m 5 (Weekly(1, DateTime(2023,11,30))) (DateTime(2023,11,23))
            let expected = 412.40m
            actual |> should equal expected

        [<Fact>]
        let ``Weekly payments with first period shorter than unit-period`` () =
            let actual = calculate1 1000m 250m 5 (Weekly(1, DateTime(2023,11,30))) (DateTime(2023,11,24))
            let expected = 434.30m
            actual |> should equal expected

        [<Fact>]
        let ``Yearly payments`` () =
            let actual = calculate1 1000m 500m 5 (Monthly (12, MonthlyConfig (2023, 11, TrackingDay 30))) (DateTime(2023, 10, 26))
            let expected = 78.34m
            actual |> should equal expected
