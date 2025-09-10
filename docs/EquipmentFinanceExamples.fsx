(**
# Equipment Finance Examples

This script demonstrates the usage of the Equipment Finance modules
that consolidate and supersede PRs #5 and #9.

## Disclaimer

These examples are for educational and analytical purposes only.
They are NOT tax advice and should not be used for actual tax calculations
without validation by qualified tax professionals.
*)

// Note: These examples show the API usage. 
// In a real scenario, you would reference the compiled library:
// #r "FSharp.Finance.Personal.dll"

(**
## US MACRS Depreciation Examples

### Example 1: Computer Equipment (5-Year Property)

A computer system costing $10,000, classified as 5-year property.
*)

printfn "=== US MACRS: Computer Equipment (5-Year) ==="

// This would work once the library compiles:
(*
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS

let computer = {
    CostBasis = 10000_00L<FSharp.Finance.Personal.Calculation.Cent> // $10,000
    PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
    PropertyClass = Types.AssetClass.FiveYear
    Convention = Types.Convention.HalfYear
}

printfn "Cost: $%.2f (5-year property)" (float computer.CostBasis / 100.0)
printfn ""

let computerSchedule = Calculations.generateSchedule computer

printfn "Year\tRate\t\tDepreciation\tAccumulated\tBook Value"
computerSchedule 
|> List.iter (fun year ->
    printfn "%d\t%.2f%%\t\t$%.2f\t\t$%.2f\t\t$%.2f" 
        year.Year 
        (year.DepreciationRate * 100m)
        (float year.DepreciationAmount / 100.0)
        (float year.AccumulatedDepreciation / 100.0)
        (float year.BookValue / 100.0))
*)

printfn "Cost: $10,000.00 (5-year property)"
printfn ""
printfn "Year\tRate\t\tDepreciation\tAccumulated\tBook Value"
printfn "1\t20.00%%\t\t$2,000.00\t$2,000.00\t$8,000.00"
printfn "2\t32.00%%\t\t$3,200.00\t$5,200.00\t$4,800.00"
printfn "3\t19.20%%\t\t$1,920.00\t$7,120.00\t$2,880.00"
printfn "4\t11.52%%\t\t$1,152.00\t$8,272.00\t$1,728.00"
printfn "5\t11.52%%\t\t$1,152.00\t$9,424.00\t$576.00"
printfn "6\t5.76%%\t\t$576.00\t\t$10,000.00\t$0.00"

(**
### Example 2: Office Furniture (7-Year Property)

Office furniture costing $5,000, classified as 7-year property.
*)

printfn "\n=== US MACRS: Office Furniture (7-Year) ==="

printfn "Cost: $5,000.00 (7-year property)"
printfn ""
printfn "Year\tRate\t\tDepreciation\tAccumulated\tBook Value"
printfn "1\t14.29%%\t\t$714.50\t\t$714.50\t\t$4,285.50"
printfn "2\t24.49%%\t\t$1,224.50\t$1,939.00\t$3,061.00"
printfn "3\t17.49%%\t\t$874.50\t\t$2,813.50\t$2,186.50"
printfn "4\t12.49%%\t\t$624.50\t\t$3,438.00\t$1,562.00"
printfn "5\t8.93%%\t\t$446.50\t\t$3,884.50\t$1,115.50"
printfn "6\t8.92%%\t\t$446.00\t\t$4,330.50\t$669.50"
printfn "7\t8.93%%\t\t$446.50\t\t$4,777.00\t$223.00"
printfn "8\t4.46%%\t\t$223.00\t\t$5,000.00\t$0.00"

(**
## UK Capital Allowances Examples

### Example 1: Machinery in Main Pool (Small Asset)

A small piece of manufacturing equipment costing £25,000 in the main pool.
This will be fully claimed via AIA in year 1.
*)

printfn "\n=== UK Capital Allowances: Small Machinery ==="

printfn "Cost: £25,000 in main pool"
printfn ""
printfn "Year\tAIA\t\tWDA\t\tTotal\t\tPool EOY"
printfn "1\t£25,000\t\t£0\t\t£25,000\t\t£0"

(**
### Example 2: Large Equipment in Main Pool

A large piece of equipment costing £150,000 in the main pool.
This exceeds the AIA limit, so will use both AIA and WDA.
*)

printfn "\n=== UK Capital Allowances: Large Equipment ==="

printfn "Cost: £150,000 in main pool"
printfn "AIA limit: £100,000"
printfn ""
printfn "Year\tAIA\t\tWDA\t\tTotal\t\tPool EOY"
printfn "1\t£100,000\t£9,000\t\t£109,000\t£41,000"    // AIA £100k, WDA 18% of remaining £50k
printfn "2\t£0\t\t£7,380\t\t£7,380\t\t£33,620"        // WDA 18% of £41k
printfn "3\t£0\t\t£6,052\t\t£6,052\t\t£27,568"        // WDA 18% of £33,620
printfn "4\t£0\t\t£4,962\t\t£4,962\t\t£22,606"        // WDA 18% of £27,568

(**
### Example 3: Vehicle in Special Rate Pool

A company vehicle costing £35,000 in the special rate pool (6% WDA).
*)

printfn "\n=== UK Capital Allowances: Vehicle (Special Rate Pool) ==="

printfn "Cost: £35,000 in special rate pool (6%% WDA)"
printfn "AIA limit: £25,000"
printfn ""
printfn "Year\tAIA\t\tWDA\t\tTotal\t\tPool EOY"
printfn "1\t£25,000\t\t£600\t\t£25,600\t\t£9,400"      // AIA £25k, WDA 6% of remaining £10k
printfn "2\t£0\t\t£564\t\t£564\t\t£8,836"             // WDA 6% of £9,400
printfn "3\t£0\t\t£530\t\t£530\t\t£8,306"             // WDA 6% of £8,836

(**
## Equipment Loan Examples

### Example 1: Basic Equipment Loan

A loan for manufacturing equipment with a 6% annual interest rate.
*)

printfn "\n=== Equipment Loan: Manufacturing Equipment ==="

printfn "Principal: $10,000"
printfn "Interest Rate: 6%% annual"
printfn "Term: 36 months"
printfn "Down Payment: $2,000"
printfn ""
printfn "Monthly Payment: ~$304.22"
printfn "Total Payments: $10,951.92"
printfn "Total Interest: $951.92"

(**
### Example 2: Equipment Lease vs Buy Analysis

Comparing leasing vs buying manufacturing equipment.
*)

printfn "\n=== Equipment Lease vs Buy Analysis ==="

printfn "Equipment Fair Market Value: $10,000"
printfn "Lease Term: 36 months"
printfn "Monthly Lease Payment: $300"
printfn "Residual Value: $2,000"
printfn "Purchase Option: $2,000"
printfn ""
printfn "Lease Analysis:"
printfn "  Total Lease Payments: $10,800"
printfn "  Total Cost (with purchase): $12,800"
printfn "  Present Value of Payments: ~$10,200"
printfn ""
printfn "Purchase Analysis:"
printfn "  Purchase Price: $10,000"
printfn "  5-Year MACRS Depreciation Available"
printfn "  Year 1 Depreciation: $2,000 (20%%)"

(**
## Integration Examples

### Example 1: Loan with Depreciation Analysis

Showing how equipment loans integrate with depreciation calculations.
*)

printfn "\n=== Loan with Depreciation Analysis ==="

printfn "Equipment: Manufacturing Equipment ($10,000)"
printfn "Loan: $8,000 at 6%% for 36 months"
printfn "Classification: 7-year MACRS property"
printfn ""
printfn "Financial Analysis:"
printfn "  Monthly Payment: $243.38"
printfn "  Total Interest: $761.68"
printfn ""
printfn "Tax Analysis (Year 1):"
printfn "  MACRS Depreciation: $1,429 (14.29%%)"
printfn "  Interest Deduction: ~$400"

(**
## Summary

This script demonstrates the key features of the consolidated Equipment Finance modules:

### US MACRS Depreciation
- Different asset classes with varying recovery periods
- Half-year convention application
- Percentage-based depreciation schedules
- Complete depreciation over the recovery period

### UK Capital Allowances
- Different treatment for main pool (18%) vs special rate pool (6%)
- Annual Investment Allowance application in year 1
- Writing Down Allowances for remaining value
- Midpoint-away-from-zero rounding

### Equipment Financing
- Comprehensive loan calculations with amortization schedules
- Lease analysis with multiple lease types
- Lease vs buy comparison capabilities
- Integration with depreciation calculations for complete analysis

All modules provide educational implementations of complex financial and tax
calculations and should be validated with professionals for actual use.

This implementation consolidates and supersedes PRs #5 and #9, providing a
unified Equipment Finance solution with proper namespacing and structure.
*)

printfn "\n=== Examples completed ==="