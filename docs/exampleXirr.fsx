(**
# XIRR (Extended Internal Rate of Return) Examples

This example demonstrates the Excel-compatible XIRR functionality provided by the `FSharp.Finance.Personal` library.

## Basic Investment Example

Consider a simple investment scenario where you invest $10,000 and receive $11,000 one year later:
*)

open System
open FSharp.Finance.Personal

// Basic investment: -$10,000 invested today, +$11,000 received in one year
let basicInvestment = [
    DateTime(2024, 1, 1), -10000m   // Investment outflow
    DateTime(2025, 1, 1), 11000m    // Return inflow
]

let basicRate = Xirr.xirr basicInvestment
printfn "Basic investment XIRR: %.2f%%" (basicRate * 100m)
// Output: approximately 10.00%

(**
## Salary Advance Example

A salary advance scenario where an employee receives $1,000 today and repays $1,030 in 30 days:
*)

// Salary advance: +$1,000 received today, -$1,030 repaid in 30 days
let salaryAdvance = [
    DateTime(2024, 1, 1), 1000m     // Advance received (inflow to borrower)
    DateTime(2024, 1, 31), -1030m   // Repayment (outflow from borrower)
]

let salaryAdvanceRate = Xirr.xirr salaryAdvance
printfn "Salary advance XIRR: %.2f%%" (salaryAdvanceRate * 100m)
// Output: approximately 43.28% (very high due to short term)

(**
## Trade Credit Cashflow Example

A simple trade credit scenario with multiple payments:
*)

// Trade credit: Invoice factoring with advance and final settlement
let tradeCreditCashflows = [
    DateTime(2024, 1, 1), -100000m   // Invoice amount (outflow to factor)
    DateTime(2024, 1, 2), 85000m    // Advance payment (inflow from factor)
    DateTime(2024, 3, 1), 14000m    // Final settlement minus fees (inflow from factor)
]

let tradeCreditRate = Xirr.xirr tradeCreditCashflows
printfn "Trade credit XIRR: %.2f%%" (tradeCreditRate * 100m)
// Output: effective rate for the factoring arrangement

(**
## Using Custom Guess

When the default guess of 0.1 (10%) might not converge well, you can provide a custom initial guess:
*)

// Using a custom guess of 5% instead of the default 10%
let customGuessRate = Xirr.xirrG 0.05m basicInvestment
printfn "Custom guess XIRR: %.2f%%" (customGuessRate * 100m)

(**
## Safe Error Handling

For production code, use `tryXirr` to handle potential calculation failures gracefully:
*)

let safeCalculation cashflows =
    match Xirr.tryXirr cashflows with
    | Ok rate -> 
        printfn "XIRR: %.2f%%" (rate * 100m)
    | Error message -> 
        printfn "XIRR calculation failed: %s" message

// Test with valid cashflows
safeCalculation basicInvestment

// Test with invalid cashflows (all positive)
let invalidCashflows = [
    DateTime(2024, 1, 1), 1000m
    DateTime(2024, 6, 1), 1100m
]
safeCalculation invalidCashflows

(**
## Sign Convention

**Important**: The XIRR calculation follows Excel's sign convention:

- **Negative values**: Money going out (investments, loan disbursements, payments made)
- **Positive values**: Money coming in (returns, loan payments received, income)

From a **borrower's perspective**:
- Loan disbursement: +1000m (money received)
- Loan repayment: -1030m (money paid out)

From an **investor's perspective**:
- Investment: -10000m (money invested)
- Return: +11000m (money received back)

## Excel Compatibility

This implementation uses the `ExcelFinancialFunctions` library to ensure complete compatibility with Excel's XIRR function, including:

- Default guess value of 0.1 (10%)
- Same convergence algorithm
- Identical precision and rounding behavior

The functions return annualized effective rates as decimal values (e.g., 0.10 for 10%).
*)