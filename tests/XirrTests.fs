namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit
open FSharp.Finance.Personal

module XirrTests =

    [<Fact>]
    let ``XIRR_basic_one_year should return approximately 10% for simple investment`` () =
        let cashflows = [
            DateTime(2024, 1, 1), -10000m  // Investment outflow
            DateTime(2025, 1, 1), 11000m   // Return inflow after 1 year
        ]
        let result = Xirr.xirr cashflows
        result |> should be (greaterThan 0.095m)
        result |> should be (lessThan 0.105m)

    [<Fact>]
    let ``XIRR_salary_advance_example should return approximately 30-40% for short term loan`` () =
        let cashflows = [
            DateTime(2024, 1, 1), 1000m    // Loan disbursement (inflow to borrower)
            DateTime(2024, 1, 31), -1030m  // Repayment after 30 days (outflow from borrower)
        ]
        let result = Xirr.xirr cashflows
        // Expected around 36% annually for this 3% monthly rate
        result |> should be (greaterThan 0.30m)
        result |> should be (lessThan 0.40m)

    [<Fact>]
    let ``XIRR_mixed_sign_validation should fail for single sign cashflows`` () =
        let positiveCashflows = [
            DateTime(2024, 1, 1), 1000m
            DateTime(2024, 2, 1), 500m
        ]
        let negativeCashflows = [
            DateTime(2024, 1, 1), -1000m
            DateTime(2024, 2, 1), -500m
        ]
        
        let positiveResult = Xirr.tryXirr positiveCashflows
        let negativeResult = Xirr.tryXirr negativeCashflows
        
        positiveResult |> should be (ofCase <@ Result<decimal, string>.Error @>)
        negativeResult |> should be (ofCase <@ Result<decimal, string>.Error @>)

    [<Fact>]
    let ``XIRR_guess_consistency should produce similar results for xirr and xirrG with 0.1`` () =
        let cashflows = [
            DateTime(2024, 1, 1), -10000m
            DateTime(2024, 6, 1), 5000m
            DateTime(2025, 1, 1), 6000m
        ]
        
        let resultDefault = Xirr.xirr cashflows
        let resultGuess = Xirr.xirrG 0.1m cashflows
        
        let difference = abs (resultDefault - resultGuess)
        difference |> should be (lessThan 1e-10m)

    [<Fact>]
    let ``XIRR should fail with insufficient cashflows`` () =
        let singleCashflow = [DateTime(2024, 1, 1), -1000m]
        let emptyCashflows = []
        
        (fun () -> Xirr.xirr singleCashflow |> ignore) |> should throw typeof<System.ArgumentException>
        (fun () -> Xirr.xirr emptyCashflows |> ignore) |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``XIRR should fail with identical dates`` () =
        let identicalDates = [
            DateTime(2024, 1, 1), -1000m
            DateTime(2024, 1, 1), 1100m
        ]
        
        (fun () -> Xirr.xirr identicalDates |> ignore) |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``tryXirr should return Ok for valid cashflows`` () =
        let cashflows = [
            DateTime(2024, 1, 1), -1000m
            DateTime(2025, 1, 1), 1100m
        ]
        
        let result = Xirr.tryXirr cashflows
        result |> should be (ofCase <@ Result<decimal, string>.Ok @>)
        
        match result with
        | Ok rate -> 
            rate |> should be (greaterThan 0.05m)
            rate |> should be (lessThan 0.15m)
        | Error _ -> failwith "Expected Ok result"