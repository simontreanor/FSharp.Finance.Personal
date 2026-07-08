(**
---
title: Equipment Finance Examples
category: Examples
categoryindex: 2
index: 5
description: Examples of equipment finance calculations
keywords: equipment finance loan lease depreciation MACRS capital allowances
---
*)

(**
# Equipment Finance Examples

This script demonstrates the usage of the Equipment Finance modules
that consolidate and supersede PRs #5 and #9.

## Disclaimer

These examples are for educational and analytical purposes only.
They are NOT tax advice and should not be used for actual tax calculations
without validation by qualified tax professionals.
*)

// Once a release of FSharp.Finance.Personal including the EquipmentFinance modules is
// published, this can become: #r "nuget:FSharp.Finance.Personal"
// Until then, build the library first (dotnet build src) and reference it directly:
#r "../src/bin/Debug/netstandard2.1/FSharp.Finance.Personal.dll"

open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance

let money (c: int64<Cent>) = Cent.toDecimal c

(**
## US MACRS Depreciation Examples

### Example 1: Computer Equipment (5-Year Property)

A computer system costing $10,000, classified as 5-year property.
*)

module MACRS = FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS.Calculations
module MacrsTypes = FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS.Types

printfn "=== US MACRS: Computer Equipment (5-Year) ==="

let computer: MacrsTypes.MacrsAsset = {
    CostBasis = 10000_00L<Cent> // $10,000
    PlacedInServiceDate = DateDay.Date(2024, 1, 1)
    PropertyClass = MacrsTypes.AssetClass.FiveYear
    Convention = MacrsTypes.Convention.HalfYear
}

printfn "Cost: $%.2f (5-year property)" (money computer.CostBasis)
printfn ""
printfn "Year\tRate\t\tDepreciation\tAccumulated\tBook Value"

MACRS.generateSchedule computer
|> List.iter (fun year ->
    printfn "%d\t%.2f%%\t\t$%.2f\t\t$%.2f\t\t$%.2f"
        year.Year
        (year.DepreciationRate * 100m)
        (money year.DepreciationAmount)
        (money year.AccumulatedDepreciation)
        (money year.BookValue))

(**
### Example 2: Office Furniture (7-Year Property)

Office furniture costing $5,000, classified as 7-year property. Here the asset class is
derived from the description with `tryClassifyAsset`.
*)

printfn "\n=== US MACRS: Office Furniture (7-Year) ==="

let furnitureClass =
    MACRS.tryClassifyAsset "office furniture"
    |> Option.defaultValue MacrsTypes.AssetClass.FiveYear

let furniture: MacrsTypes.MacrsAsset = {
    CostBasis = 5000_00L<Cent> // $5,000
    PlacedInServiceDate = DateDay.Date(2024, 1, 1)
    PropertyClass = furnitureClass
    Convention = MacrsTypes.Convention.HalfYear
}

printfn "Cost: $%.2f (classified as %A)" (money furniture.CostBasis) furniture.PropertyClass
printfn ""
printfn "Year\tRate\t\tDepreciation\tAccumulated\tBook Value"

MACRS.generateSchedule furniture
|> List.iter (fun year ->
    printfn "%d\t%.2f%%\t\t$%.2f\t\t$%.2f\t\t$%.2f"
        year.Year
        (year.DepreciationRate * 100m)
        (money year.DepreciationAmount)
        (money year.AccumulatedDepreciation)
        (money year.BookValue))

(**
Note: real property is out of scope for this simplified MACRS module - descriptions such
as "building" are rejected with an error because buildings are 27.5/39-year straight-line
property, not equipment.
*)

(**
## UK Capital Allowances Examples

### Example 1: Machinery in Main Pool (Small Asset)

Manufacturing equipment costing 25,000 GBP in the main pool.
This is fully claimed via the Annual Investment Allowance in year 1.
*)

module UKCA = FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances.Calculations
module UKCATypes = FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances.Types

let printAllowances (schedule: UKCATypes.AllowanceSchedule) =
    printfn "Year\tAIA\t\tWDA\t\tTotal\t\tPool EOY"
    schedule.Years
    |> List.iter (fun year ->
        printfn "%d\t£%.2f\t£%.2f\t£%.2f\t£%.2f"
            year.Year
            (money year.AnnualInvestmentAllowance)
            (money year.WritingDownAllowance)
            (money year.TotalAllowances)
            (money year.PoolValueEndOfYear))
    if schedule.UnclaimedPool > 0L<Cent> then
        printfn "Unclaimed pool after final year: £%.2f" (money schedule.UnclaimedPool)

printfn "\n=== UK Capital Allowances: Small Machinery ==="

let smallMachinery: UKCATypes.Expenditure = {
    Amount = 25_000_00L<Cent>
    Pool = UKCATypes.Pool.Main
    Description = "Manufacturing equipment"
}

printfn "Cost: £%.2f in main pool" (money smallMachinery.Amount)
printfn ""
UKCA.scheduleDefault smallMachinery |> printAllowances

(**
### Example 2: Large Equipment in Main Pool

Equipment costing 150,000 GBP against a reduced AIA limit of 100,000 GBP, so it uses both
AIA and writing down allowances. Any pool value left when the calculation stops at
`MaxYears` is reported as `UnclaimedPool` rather than silently dropped.
*)

printfn "\n=== UK Capital Allowances: Large Equipment ==="

let largeEquipment: UKCATypes.Expenditure = {
    Amount = 150_000_00L<Cent>
    Pool = UKCATypes.Pool.Main
    Description = "Production line"
}

let largeConfig = {
    UKCATypes.Default with
        AnnualInvestmentAllowanceLimit = 100_000_00L<Cent>
        MaxYears = 4
}

printfn "Cost: £%.2f in main pool, AIA limit £%.2f" (money largeEquipment.Amount) (money largeConfig.AnnualInvestmentAllowanceLimit)
printfn ""
UKCA.generateSchedule largeConfig largeEquipment |> printAllowances

(**
### Example 3: Vehicle in Special Rate Pool

A company vehicle costing 35,000 GBP in the special rate pool (6% WDA) against a reduced
AIA limit of 25,000 GBP. When the pool balance drops to 1,000 GBP or below, the HMRC small
pools allowance writes off the remainder in full.
*)

printfn "\n=== UK Capital Allowances: Vehicle (Special Rate Pool) ==="

let vehicle: UKCATypes.Expenditure = {
    Amount = 35_000_00L<Cent>
    Pool = UKCATypes.Pool.SpecialRate
    Description = "Company vehicle"
}

let vehicleConfig = {
    UKCATypes.Default with
        AnnualInvestmentAllowanceLimit = 25_000_00L<Cent>
        MaxYears = 3
}

printfn "Cost: £%.2f in special rate pool (6%% WDA), AIA limit £%.2f" (money vehicle.Amount) (money vehicleConfig.AnnualInvestmentAllowanceLimit)
printfn ""
UKCA.generateSchedule vehicleConfig vehicle |> printAllowances

(**
## Equipment Loan Examples

### Example 1: Basic Equipment Loan

A loan for manufacturing equipment with a 6% annual interest rate and a monthly service fee.
*)

printfn "\n=== Equipment Loan: Manufacturing Equipment ==="

let loanTerms: Loan.EquipmentLoanTerms = {
    Principal = 10000_00L<Cent> // $10,000 financed
    InterestRate = Interest.Rate.Annual (Percent 6.0m)
    TermMonths = 36
    MonthlyPayment = None // calculated
    MonthlyFee = Some 5_00L<Cent> // $5/month service fee
    EquipmentDescription = "Manufacturing equipment"
    EquipmentCost = 12000_00L<Cent>
    DownPayment = 2000_00L<Cent>
    ResidualValue = 0L<Cent>
}

let loanDetails = Loan.calculatePaymentDetails loanTerms

printfn "Principal: $%.2f" (money loanTerms.Principal)
printfn "Interest Rate: %O nominal annual" loanDetails.NominalAnnualRate
printfn "Term: %d months" loanTerms.TermMonths
printfn "Down Payment: $%.2f" (money loanTerms.DownPayment)
printfn ""
printfn "Monthly Payment: $%.2f (+ $%.2f fee)" (money loanDetails.MonthlyPayment) (money (loanTerms.MonthlyFee |> Option.defaultValue 0L<Cent>))
printfn "Total Payments: $%.2f" (money loanDetails.TotalPayments)
printfn "Total Interest: $%.2f" (money loanDetails.TotalInterest)
printfn "Total Fees: $%.2f" (money loanDetails.TotalFees)

let loanSchedule = Loan.generateAmortizationSchedule loanTerms (DateDay.Date(2024, 1, 1))

printfn ""
printfn "First three payments and the final payment:"
printfn "#\tPayment\t\tPrincipal\tInterest\tFee\tBalance"

Array.append (loanSchedule |> Array.take 3) [| loanSchedule |> Array.last |]
|> Array.iter (fun item ->
    printfn "%d\t$%.2f\t\t$%.2f\t\t$%.2f\t\t$%.2f\t$%.2f"
        item.PaymentNumber
        (money item.PaymentAmount)
        (money item.PrincipalPayment)
        (money item.InterestPayment)
        (money item.FeePayment)
        (money item.RemainingBalance))

(**
### Example 2: Equipment Lease vs Buy Analysis

Comparing leasing vs buying manufacturing equipment. Equipment leases are usually paid in
advance (at the start of each period), and the analysis discounts both alternatives at a
supplied discount rate to compute the net advantage to leasing.
*)

printfn "\n=== Equipment Lease vs Buy Analysis ==="

let leaseTerms: Lease.EquipmentLeaseTerms = {
    EquipmentDescription = "Manufacturing equipment"
    FairMarketValue = 10000_00L<Cent> // $10,000
    TermMonths = 36
    LeaseType = Lease.LeaseType.FinanceLease
    PaymentFrequency = Lease.PaymentFrequency.Monthly
    PaymentTiming = Lease.PaymentTiming.InAdvance
    LeasePayment = 0L<Cent> // calculated
    PeriodicFee = None
    UpfrontPayment = 1000_00L<Cent>
    ResidualValue = 2000_00L<Cent>
    PurchaseOption = Some 2000_00L<Cent>
    ImplicitRate = Interest.Rate.Annual (Percent 5.0m)
}

let discountRate = Percent 8.0m
let leaseAnalysis = Lease.analyzeLeaseVsBuy leaseTerms (DateDay.Date(2024, 1, 1)) discountRate
let leaseDetails = leaseAnalysis.LeaseDetails

printfn "Equipment Fair Market Value: $%.2f" (money leaseTerms.FairMarketValue)
printfn "Lease Term: %d months, paid in advance" leaseTerms.TermMonths
printfn "Residual Value: $%.2f" (money leaseTerms.ResidualValue)
printfn "Purchase Option: $%.2f" (money (leaseTerms.PurchaseOption |> Option.defaultValue 0L<Cent>))
printfn ""
printfn "Lease Analysis:"
printfn "  Monthly Lease Payment: $%.2f" (money leaseDetails.LeasePayment)
printfn "  Total Lease Payments: $%.2f" (money leaseDetails.TotalPayments)
printfn "  Total Cost (with purchase): $%.2f" (money leaseDetails.TotalCost)
printfn "  Present Value of Payments: $%.2f" (money leaseDetails.PresentValue)
printfn "  Net Advantage to Leasing (at %O discount): $%.2f" discountRate (money leaseAnalysis.NetAdvantageToLeasing)
printfn ""
printfn "Purchase Analysis:"
printfn "  Purchase Price: $%.2f" (money leaseTerms.FairMarketValue)

let purchaseYear1 = leaseAnalysis.PurchaseDepreciation |> List.head
printfn "  Year 1 MACRS Depreciation: $%.2f (%.2f%%)" (money purchaseYear1.DepreciationAmount) (purchaseYear1.DepreciationRate * 100m)

(**
## Integration Examples

### Example 1: Loan with Depreciation Analysis

Showing how equipment loans integrate with depreciation calculations.
*)

printfn "\n=== Loan with Depreciation Analysis ==="

let integratedTerms: Loan.EquipmentLoanTerms = {
    Principal = 8000_00L<Cent> // $8,000 financed
    InterestRate = Interest.Rate.Annual (Percent 6.0m)
    TermMonths = 36
    MonthlyPayment = None
    MonthlyFee = None
    EquipmentDescription = "Manufacturing equipment"
    EquipmentCost = 10000_00L<Cent>
    DownPayment = 2000_00L<Cent>
    ResidualValue = 0L<Cent>
}

let loanAnalysis = Loan.analyzeLoan integratedTerms (DateDay.Date(2024, 1, 1)) (Percent 8.0m)

printfn "Equipment: %s ($%.2f)" integratedTerms.EquipmentDescription (money integratedTerms.EquipmentCost)
printfn "Loan: $%.2f at %O for %d months" (money integratedTerms.Principal) loanAnalysis.PaymentDetails.NominalAnnualRate integratedTerms.TermMonths
printfn "Classification assumed: %b" loanAnalysis.AssetClassAssumed
printfn ""
printfn "Financial Analysis:"
printfn "  Monthly Payment: $%.2f" (money loanAnalysis.PaymentDetails.MonthlyPayment)
printfn "  Total Interest: $%.2f" (money loanAnalysis.PaymentDetails.TotalInterest)
printfn "  NPV of loan cash flows at 8%%: $%.2f" (money loanAnalysis.NetPresentValue)
printfn ""

let year1Depreciation = loanAnalysis.DepreciationSchedule |> List.head
let year1Interest =
    loanAnalysis.AmortizationSchedule
    |> Array.filter (fun item -> item.PaymentNumber <= 12)
    |> Array.sumBy (fun item -> item.InterestPayment)

printfn "Tax Analysis (Year 1):"
printfn "  MACRS Depreciation: $%.2f (%.2f%%)" (money year1Depreciation.DepreciationAmount) (year1Depreciation.DepreciationRate * 100m)
printfn "  Interest Paid: $%.2f" (money year1Interest)

(**
## Summary

This script demonstrates the key features of the consolidated Equipment Finance modules:

### US MACRS Depreciation
- Different asset classes with varying recovery periods
- Half-year convention application
- IRS Table A-1 percentage schedules
- Complete depreciation over the recovery period
- Keyword-based classification that rejects unsupported real property

### UK Capital Allowances
- Different treatment for main pool (18%) vs special rate pool (6%)
- Annual Investment Allowance application in year 1
- Writing Down Allowances for remaining value
- HMRC small pools allowance and explicit unclaimed pool reporting

### Equipment Financing
- Loan calculations with amortization schedules, residual balloons and recurring fees
- Lease schedules honouring the contractual rental, in advance or in arrears
- Lease vs buy comparison with net advantage to leasing at a supplied discount rate
- Integration with depreciation calculations for complete analysis

All modules provide educational implementations of complex financial and tax
calculations and should be validated with professionals for actual use.

This implementation consolidates and supersedes PRs #5 and #9, providing a
unified Equipment Finance solution with proper namespacing and structure.
*)

printfn "\n=== Examples completed ==="
