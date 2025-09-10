// Simple test script to verify SalaryAdvance module functionality
// Run with: dotnet fsi SalaryAdvanceTest.fsx

#r "bin/Debug/netstandard2.1/FSharp.Finance.Personal.dll"

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.SalaryAdvance
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Calculation

// Test basic functionality
let testBasicFunctionality () =
    let advanceDate = Date(2024, 1, 15)
    let payrollDates = [| 
        Date(2024, 1, 31)
        Date(2024, 2, 15)
        Date(2024, 2, 29)
    |]
    
    // Test LumpOnFirstPayroll mode
    let config1 = SalaryAdvance.ScheduleConfig.create 
                    advanceDate
                    50000L<Cent>  // $500.00
                    LumpOnFirstPayroll
                    payrollDates
    
    let schedule1 = SalaryAdvance.createSchedule config1
    printfn "Lump sum schedule: %A" schedule1
    
    // Test EvenlyProrated mode
    let config2 = { config1 with RepaymentMode = EvenlyProrated }
    let schedule2 = SalaryAdvance.createSchedule config2
    printfn "Evenly prorated schedule: %A" schedule2
    
    // Test with fee
    let config3 = config1 |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>)
    let schedule3 = SalaryAdvance.createSchedule config3
    printfn "Schedule with fee: %A" schedule3
    
    // Test cashflow export
    let cashflows = SalaryAdvance.exportCashflows config3
    printfn "Cashflows: %A" cashflows
    
    // Test summary
    let summary = SalaryAdvance.calculateSummary config3
    printfn "Summary: %A" summary

testBasicFunctionality ()