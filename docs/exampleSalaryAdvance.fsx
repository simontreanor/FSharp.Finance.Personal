(**
---
title: Salary Advance / Earned Wage Access Modeling
category: Examples
categoryindex: 2
index: 10
description: Zero-interest salary advance modeling with schedule construction and cashflow analysis
---

# Salary Advance / Earned Wage Access Modeling

This module provides zero-interest salary advance / earned wage access modeling capabilities under the `FSharp.Finance.Personal.SalaryAdvance` namespace.

## Key Features

- **RepaymentMode**: Discriminated union supporting multiple repayment strategies
- **Schedule Construction**: Generate repayment schedules based on payroll dates
- **Fee Handling**: Support for flat fees, percentage fees, or no fees
- **Cashflow Export**: Export provider or borrower cashflows for analytical use
- **Self-contained**: No dependencies on B2B modules

## RepaymentMode Options

The module supports three repayment modes:

*)

#r "../src/bin/Debug/netstandard2.1/FSharp.Finance.Personal.dll"

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.SalaryAdvance
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Calculation

(**

### 1. LumpOnFirstPayroll

Repayment in full on the first payroll after advance.

*)

let advanceDate = Date(2024, 1, 15)
let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15); Date(2024, 2, 29) |]

let lumpSumConfig = 
    SalaryAdvance.ScheduleConfig.create 
        advanceDate
        50000L<Cent>  // $500.00
        LumpOnFirstPayroll
        payrollDates

let lumpSumSchedule = SalaryAdvance.createSchedule lumpSumConfig

printfn "Lump Sum Schedule:"
lumpSumSchedule |> Array.iter (fun item ->
    printfn "  Date: %A, Amount: $%.2f, Remaining: $%.2f" 
        item.PaymentDate 
        (Cent.toDecimal item.RepaymentAmount)
        (Cent.toDecimal item.RemainingBalance))

(**

### 2. EvenlyProrated

Repayment evenly distributed across multiple payrolls.

*)

let proratedConfig = { lumpSumConfig with RepaymentMode = EvenlyProrated }
let proratedSchedule = SalaryAdvance.createSchedule proratedConfig

printfn "\nEvenly Prorated Schedule:"
proratedSchedule |> Array.iter (fun item ->
    printfn "  Date: %A, Amount: $%.2f, Remaining: $%.2f" 
        item.PaymentDate 
        (Cent.toDecimal item.RepaymentAmount)
        (Cent.toDecimal item.RemainingBalance))

(**

### 3. Custom

Custom repayment amounts, one per payroll date. The amounts must all be positive
and must sum to the total repayable (advance plus any fee added on top).

*)

let customConfig =
    { lumpSumConfig with
        RepaymentMode = Custom [ 25000L<Cent>; 15000L<Cent>; 10000L<Cent> ] }

let customSchedule = SalaryAdvance.createSchedule customConfig

printfn "\nCustom Amounts Schedule:"
customSchedule |> Array.iter (fun item ->
    printfn "  Date: %A, Amount: $%.2f, Remaining: $%.2f" 
        item.PaymentDate 
        (Cent.toDecimal item.RepaymentAmount)
        (Cent.toDecimal item.RemainingBalance))

(**

## Fee Handling

The module supports different fee structures:

### Flat Fee

*)

let configWithFlatFee = 
    lumpSumConfig 
    |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>) // $5.00 fee

let scheduleWithFlatFee = SalaryAdvance.createSchedule configWithFlatFee

printfn "\nSchedule with $5 Flat Fee:"
scheduleWithFlatFee |> Array.iter (fun item ->
    printfn "  Date: %A, Total: $%.2f (Principal: $%.2f, Fee: $%.2f)" 
        item.PaymentDate 
        (Cent.toDecimal item.RepaymentAmount)
        (Cent.toDecimal (item.RepaymentAmount - item.FeeAmount))
        (Cent.toDecimal item.FeeAmount))

(**

### Percentage Fee

Percentage fees use the library's `Percent` type (`Percent 2m` = 2%). How the fee is rounded
to whole cents is configurable via the library's `Rounding` type (`FeeRounding`, defaulting to
midpoint-away-from-zero).

*)

let configWithPctFee =
    lumpSumConfig
    |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee (Percent 2m)) // 2% fee

let scheduleWithPctFee = SalaryAdvance.createSchedule configWithPctFee

printfn "\nSchedule with 2%% Fee:"
scheduleWithPctFee |> Array.iter (fun item ->
    printfn "  Date: %A, Total: $%.2f (Principal: $%.2f, Fee: $%.2f)" 
        item.PaymentDate 
        (Cent.toDecimal item.RepaymentAmount)
        (Cent.toDecimal (item.RepaymentAmount - item.FeeAmount))
        (Cent.toDecimal item.FeeAmount))

(**

## Fee Treatment

The fee can either be added on top of the amount repayable (the default) or netted from the
disbursed proceeds:

- **AddedOnTop**: the borrower receives the full advance and repays advance + fee
- **NettedFromProceeds**: the borrower receives advance − fee and repays the advance alone

In both treatments the sum of the scheduled repayments equals the principal (total repayable).

*)

let nettedConfig =
    configWithFlatFee
    |> SalaryAdvance.ScheduleConfig.withFeeTreatment NettedFromProceeds

printfn "\nFee Added On Top:    disbursed $%.2f, total repayable $%.2f"
    (Cent.toDecimal (SalaryAdvance.netDisbursedAmount configWithFlatFee))
    (Cent.toDecimal (SalaryAdvance.totalRepayable configWithFlatFee))

printfn "Fee Netted From Proceeds: disbursed $%.2f, total repayable $%.2f"
    (Cent.toDecimal (SalaryAdvance.netDisbursedAmount nettedConfig))
    (Cent.toDecimal (SalaryAdvance.totalRepayable nettedConfig))

let nettedCashflows = SalaryAdvance.exportCashflows nettedConfig

printfn "\nProvider Cashflows (fee netted from proceeds):"
nettedCashflows |> Array.iter (fun cf ->
    printfn "  %A: $%.2f - %s"
        cf.Date
        (Cent.toDecimal cf.Amount)
        cf.Description)

(**

## Cashflow Export for Analysis

Export cashflows for use with analytical tools like XIRR. `exportCashflows`
returns provider-perspective cashflows, and `borrowerCashflows` flips the signs.

*)

let cashflows = SalaryAdvance.exportCashflows configWithFlatFee
let borrowerCashflows = SalaryAdvance.borrowerCashflows configWithFlatFee

printfn "\nProvider Cashflows for Analysis:"
cashflows |> Array.iter (fun cf ->
    printfn "  %A: $%.2f - %s" 
        cf.Date 
        (Cent.toDecimal cf.Amount)
        cf.Description)

printfn "\nBorrower Cashflows for Analysis:"
borrowerCashflows |> Array.iter (fun cf ->
    printfn "  %A: $%.2f - %s"
        cf.Date
        (Cent.toDecimal cf.Amount)
        cf.Description)

(**

## Summary Statistics

Generate summary statistics for the salary advance:

*)

let summary = SalaryAdvance.calculateSummary configWithFlatFee

let (Percent effectiveFeeRate) = summary.EffectiveFeeRate

printfn "\nSummary Statistics:"
printfn "  Advance Amount: $%.2f" (Cent.toDecimal summary.AdvanceAmount)
printfn "  Net Disbursed: $%.2f" (Cent.toDecimal summary.NetDisbursedAmount)
printfn "  Total Fee: $%.2f" (Cent.toDecimal summary.TotalFeeAmount)
printfn "  Total Repayment: $%.2f" (Cent.toDecimal summary.TotalRepaymentAmount)
printfn "  Term: %d days" summary.TermInDays
printfn "  Number of Payments: %d" summary.NumberOfPayments
// the effective fee rate is a flat rate over the term, with no time dimension
printfn "  Effective Fee Rate: %.2f%%" effectiveFeeRate

// the annualized companion (fee rate x 365 / term in days) makes advances of different terms
// comparable, but it is a simple annualization, not a compounded rate nor a regulatory APR
match summary.AnnualizedEffectiveFeeRate with
| Some(Percent annualizedRate) -> printfn "  Annualized Effective Fee Rate (simple): %.2f%%" annualizedRate
| None -> printfn "  Annualized Effective Fee Rate (simple): n/a (zero-day term)"

(**

## Configuration Validation

Validate configuration before processing:

*)

let validationErrors = SalaryAdvance.ScheduleConfig.validate configWithFlatFee

if validationErrors.Length = 0 then
    printfn "\n✓ Configuration is valid"
else
    printfn "\n✗ Configuration errors:"
    validationErrors |> Array.iter (printfn "  - %s")

(**

## Usage Notes

1. **Zero Interest**: This module is specifically designed for zero-interest salary advances
2. **Self-contained**: No dependencies on B2B modules, making it suitable for independent use
3. **Analytical Ready**: Cashflows are available from provider and borrower perspectives
4. **Flexible Fees**: Support for various fee structures (flat, percentage, or none)
5. **Validation**: Built-in configuration validation to prevent errors

*)
