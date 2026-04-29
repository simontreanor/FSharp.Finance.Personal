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

Custom repayment over specified number of days.

*)

let customConfig = { lumpSumConfig with RepaymentMode = Custom 30L }
let customSchedule = SalaryAdvance.createSchedule customConfig

printfn "\nCustom 30-day Schedule:"
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

*)

let configWithPctFee = 
    lumpSumConfig 
    |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee 0.02m) // 2% fee

let scheduleWithPctFee = SalaryAdvance.createSchedule configWithPctFee

printfn "\nSchedule with 2%% Fee:"
scheduleWithPctFee |> Array.iter (fun item ->
    printfn "  Date: %A, Total: $%.2f (Principal: $%.2f, Fee: $%.2f)" 
        item.PaymentDate 
        (Cent.toDecimal item.RepaymentAmount)
        (Cent.toDecimal (item.RepaymentAmount - item.FeeAmount))
        (Cent.toDecimal item.FeeAmount))

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

printfn "\nSummary Statistics:"
printfn "  Advance Amount: $%.2f" (Cent.toDecimal summary.AdvanceAmount)
printfn "  Total Fee: $%.2f" (Cent.toDecimal summary.TotalFeeAmount)
printfn "  Total Repayment: $%.2f" (Cent.toDecimal summary.TotalRepaymentAmount)
printfn "  Term: %d days" summary.TermInDays
printfn "  Number of Payments: %d" summary.NumberOfPayments
printfn "  Effective Fee Rate: %.2f%%" summary.EffectiveFeeRate

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
