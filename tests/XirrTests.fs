namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit
open FSharp.Finance.Personal

module XirrTests =

    open Calculation
    open DateDay

    [<Fact>]
    let ``XIRR_basic_one_year should return approximately 10% for simple investment`` () =
        let cashflows = [
            Date(2024, 1, 1), -10000m  // Investment outflow
            Date(2025, 1, 1), 11000m   // Return inflow after 1 year
        ]
        let result = Xirr.xirr cashflows
        result |> should be (greaterThan 0.095m)
        result |> should be (lessThan 0.105m)

    [<Fact>]
    let ``XIRR_salary_advance_example should return approximately 30-40% for short term loan`` () =
        let cashflows = [
            Date(2024, 1, 1), 1000m    // Loan disbursement (inflow to borrower)
            Date(2024, 1, 31), -1030m  // Repayment after 30 days (outflow from borrower)
        ]
        let result = Xirr.xirr cashflows
        // Expected around 43% annually for this 3% monthly rate over 30 days
        result |> should be (greaterThan 0.30m)
        result |> should be (lessThan 0.50m)

    [<Fact>]
    let ``XIRR_mixed_sign_validation should fail for single sign cashflows`` () =
        let positiveCashflows = [
            Date(2024, 1, 1), 1000m
            Date(2024, 2, 1), 500m
        ]
        let negativeCashflows = [
            Date(2024, 1, 1), -1000m
            Date(2024, 2, 1), -500m
        ]
        
        let positiveResult = Xirr.tryXirr positiveCashflows
        let negativeResult = Xirr.tryXirr negativeCashflows
        
        match positiveResult with
        | Error _ -> () // Expected
        | Ok _ -> failwith "Expected Error for all positive cashflows"
        
        match negativeResult with
        | Error _ -> () // Expected
        | Ok _ -> failwith "Expected Error for all negative cashflows"

    [<Fact>]
    let ``XIRR_guess_consistency should converge to the same root from different guesses`` () =
        let cashflows = [
            Date(2024, 1, 1), -10000m
            Date(2024, 6, 1), 5000m
            Date(2025, 1, 1), 6000m
        ]

        let resultLowGuess = Xirr.xirrG 0.05m cashflows
        let resultHighGuess = Xirr.xirrG 0.3m cashflows

        let difference = abs (resultLowGuess - resultHighGuess)
        difference |> should be (lessThan 1e-6m)

    [<Fact>]
    let ``XIRR_unsorted_input should compute the same rate as sorted input`` () =
        let sorted = [
            Date(2024, 1, 1), -10000m
            Date(2024, 6, 1), 5000m
            Date(2025, 1, 1), 6000m
        ]
        let unsorted = [
            Date(2025, 1, 1), 6000m
            Date(2024, 1, 1), -10000m
            Date(2024, 6, 1), 5000m
        ]

        let resultSorted = Xirr.xirr sorted
        let resultUnsorted = Xirr.xirr unsorted

        resultUnsorted |> should equal resultSorted

    [<Fact>]
    let ``XIRR should fail with insufficient cashflows`` () =
        let singleCashflow = [Date(2024, 1, 1), -1000m]
        let emptyCashflows = []
        
        (fun () -> Xirr.xirr singleCashflow |> ignore) |> should throw typeof<System.ArgumentException>
        (fun () -> Xirr.xirr emptyCashflows |> ignore) |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``XIRR should fail with identical dates`` () =
        let identicalDates = [
            Date(2024, 1, 1), -1000m
            Date(2024, 1, 1), 1100m
        ]
        
        (fun () -> Xirr.xirr identicalDates |> ignore) |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``tryXirr should return Ok for valid cashflows`` () =
        let cashflows = [
            Date(2024, 1, 1), -1000m
            Date(2025, 1, 1), 1100m
        ]
        
        let result = Xirr.tryXirr cashflows
        
        match result with
        | Ok rate -> 
            rate |> should be (greaterThan 0.05m)
            rate |> should be (lessThan 0.15m)
        | Error msg -> failwith $"Expected Ok result but got Error: {msg}"

    [<Fact>]
    let ``tryXirrG should return Ok for valid cashflows and match xirrG`` () =
        let cashflows = [
            Date(2024, 1, 1), -1000m
            Date(2025, 1, 1), 1100m
        ]

        match Xirr.tryXirrG 0.05m cashflows with
        | Ok rate -> rate |> should equal (Xirr.xirrG 0.05m cashflows)
        | Error msg -> failwith $"Expected Ok result but got Error: {msg}"

    [<Fact>]
    let ``xirrG should fail with out-of-domain guess`` () =
        let cashflows = [
            Date(2024, 1, 1), -1000m
            Date(2025, 1, 1), 1100m
        ]

        (fun () -> Xirr.xirrG (-1m) cashflows |> ignore)
        |> should throw typeof<System.ArgumentException>

        match Xirr.tryXirrG (-1.5m) cashflows with
        | Error _ -> () // Expected
        | Ok _ -> failwith "Expected Error for out-of-domain guess"

    [<Fact>]
    let ``xirrCents should compute the same rate as decimal cashflows`` () =
        let centCashflows = [
            Date(2024, 1, 1), -1_000_000L<Cent>  // -10,000.00
            Date(2025, 1, 1), 1_100_000L<Cent>   //  11,000.00
        ]
        let decimalCashflows = [
            Date(2024, 1, 1), -10000m
            Date(2025, 1, 1), 11000m
        ]

        let centResult = Xirr.xirrCents centCashflows
        centResult |> should equal (Xirr.xirr decimalCashflows)

        match Xirr.tryXirrCents centCashflows with
        | Ok rate -> rate |> should equal centResult
        | Error msg -> failwith $"Expected Ok result but got Error: {msg}"
