(**
---
title: XIRR Example
category: Examples
categoryindex: 2
index: 5
description: Example of Excel-compatible XIRR calculation
keywords: XIRR IRR cashflow
---

# XIRR (Extended Internal Rate of Return) Examples

This example demonstrates the Excel-compatible XIRR functionality provided by the `FSharp.Finance.Personal` library.

## Basic Investment Example

Consider a simple investment scenario where you invest $10,000 and receive $11,000 one year later:
*)

// After the next package release, this can use:
// #r "nuget:FSharp.Finance.Personal"
#r "nuget: ExcelFinancialFunctions, 3.2.0"
#r "../src/bin/Release/netstandard2.1/FSharp.Finance.Personal.dll"

open FSharp.Finance.Personal
open DateDay

// Basic investment: -$10,000 invested today, +$11,000 received in one year
let basicInvestment = [
    Date(2024, 1, 1), -10000m   // Investment outflow
    Date(2025, 1, 1), 11000m    // Return inflow
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
    Date(2024, 1, 1), 1000m     // Advance received (inflow to borrower)
    Date(2024, 1, 31), -1030m   // Repayment (outflow from borrower)
]

let salaryAdvanceRate = Xirr.xirr salaryAdvance
printfn "Salary advance XIRR: %.2f%%" (salaryAdvanceRate * 100m)
// Output: approximately 43.28% (very high due to short term)

(**
## Trade Credit Cashflow Example

An invoice factoring scenario from the factor's perspective: the factor advances $85,000
against a $100,000 invoice, later collects the invoice from the debtor, and remits the
remainder to the seller minus a $3,000 fee. The factor earns $3,000 on $85,000 over 60 days:
*)

// Invoice factoring from the factor's perspective
let tradeCreditCashflows = [
    Date(2024, 1, 1), -85000m   // Advance paid to the seller (outflow from factor)
    Date(2024, 3, 1), 100000m   // Invoice amount collected from the debtor (inflow to factor)
    Date(2024, 3, 1), -12000m   // Remainder remitted to the seller minus the fee (outflow from factor)
]

let tradeCreditRate = Xirr.xirr tradeCreditCashflows
printfn "Trade credit XIRR: %.2f%%" (tradeCreditRate * 100m)
// Output: approximately 23.49% (a $3,000 fee on an $85,000 advance over 60 days, annualized)

(**
## Using Custom Guess

When the default guess of 0.1 (10%) might not converge well, you can provide a custom initial guess:
*)

// Using a custom guess of 5% instead of the default 10%
let customGuessRate = Xirr.xirrG 0.05m basicInvestment
printfn "Custom guess XIRR: %.2f%%" (customGuessRate * 100m)

(**
## Working with Cent Values

The rest of the library represents money in the base currency unit as `int64<Cent>`
(see `Calculation.Cent`). The `xirrCents` and `tryXirrCents` functions accept such
cashflows directly:
*)

open Calculation

// The same basic investment expressed in cents
let basicInvestmentCents = [
    Date(2024, 1, 1), -1_000_000L<Cent>   // -$10,000.00
    Date(2025, 1, 1), 1_100_000L<Cent>    // +$11,000.00
]

let basicRateFromCents = Xirr.xirrCents basicInvestmentCents
printfn "Basic investment XIRR from cents: %.2f%%" (basicRateFromCents * 100m)
// Output: approximately 10.00%

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
    Date(2024, 1, 1), 1000m
    Date(2024, 6, 1), 1100m
]
safeCalculation invalidCashflows

(**
## Sign Convention

**Important**: The XIRR calculation follows Excel's sign convention:

- **Negative values**: Money going out from the borrower's perspective (investments, loan payments)
- **Positive values**: Money coming in from the borrower's perspective (returns, loan disbursements)

From a **borrower's perspective**:
- Loan disbursement: +1000m (money received)
- Loan repayment: -1030m (money paid out)

From an **investor's perspective**:
- Investment: -10000m (money invested)
- Return: +11000m (money received back)

## Excel Compatibility

This implementation uses the `ExcelFinancialFunctions` library, which provides Excel-compatible
function semantics for XIRR:

- Same default guess value of 0.1 (10%)
- Same sign convention and day-count treatment as Excel's XIRR
- Results typically match Excel to high precision

Note that the underlying library uses a different convergence implementation than Excel,
so results may occasionally differ from Excel in the last decimal places. Cashflows are
sorted by date internally, so they may be supplied in any order. If the calculation fails
to converge, `xirr` and `xirrG` surface this as a raw `System.Exception`; the `tryXirr`,
`tryXirrG` and `tryXirrCents` variants catch it and return a `Result.Error` instead.

The functions return annualized effective rates as decimal values (e.g., 0.10 for 10%).
*)
