# Equipment Finance Module Documentation

This document provides comprehensive documentation for the Equipment Finance modules in FSharp.Finance.Personal, which consolidate and supersede PRs #5 and #9.

## Overview

The Equipment Finance modules provide analytical capabilities for equipment financing scenarios including:

- **Equipment Loans**: Complete loan analysis with amortization schedules
- **Equipment Leases**: Lease calculations and lease vs. buy analysis
- **Depreciation**: Both US MACRS and UK Capital Allowances calculations

## Important Disclaimer

⚠️ **EDUCATIONAL PURPOSES ONLY**: These modules are for educational and analytical purposes only. They are NOT tax advice and should not be used for actual tax calculations without validation by qualified tax professionals.

## Module Structure

### Namespaces

- `FSharp.Finance.Personal.EquipmentFinance.Depreciation` - Common depreciation utilities
- `FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS` - US MACRS depreciation
- `FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances` - UK Capital Allowances
- `FSharp.Finance.Personal.EquipmentFinance.Loan` - Equipment loan calculations
- `FSharp.Finance.Personal.EquipmentFinance.Lease` - Equipment lease calculations

### File Organization

```
src/EquipmentFinance/
├── Depreciation/
│   ├── DepreciationCommon.fs      # Shared utilities and abstractions
│   ├── US_MACRS.fs                # US MACRS depreciation calculations
│   └── UK_CapitalAllowances.fs    # UK Capital Allowances calculations
├── Loan.fs                        # Equipment loan analysis
└── Lease.fs                       # Equipment lease analysis
```

## Depreciation Common Module

- `DepreciationCommon.Calculations.straightLine cost salvage life`
- `DepreciationCommon.Calculations.decliningBalance cost salvage life rateFactor switchToStraightLine`

## US MACRS Depreciation

### Asset Classes Supported

- **3-Year Property**: Special tools, small manufacturing equipment
- **5-Year Property**: Computers, office machinery, vehicles (default)
- **7-Year Property**: Office furniture, most equipment
- **10-Year Property**: Boats, barges, single-purpose structures
- **15-Year Property**: Land improvements, gas stations
- **20-Year Property**: Farm buildings, utility property

### Example Usage

```fsharp
open FSharp.Finance.Personal
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS

let computer = {
    CostBasis = 10000_00L<Cent> // $10,000
    PlacedInServiceDate = DateDay.Date(2024, 1, 1)
    PropertyClass = Types.AssetClass.FiveYear
    Convention = Types.Convention.HalfYear
}

let schedule = Calculations.generateSchedule computer
// Returns 6-year depreciation schedule with proper percentages
```

### Key Features

- Half-year convention implementation
- Educational percentage tables for all asset classes
- Complete year-by-year depreciation calculations
- Asset classification helper function

## UK Capital Allowances

### Pool Types Supported

- **Main Pool**: 18% Writing Down Allowance rate
- **Special Rate Pool**: 6% Writing Down Allowance rate (vehicles, etc.)

### Example Usage

```fsharp
open FSharp.Finance.Personal
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances

let machinery = {
    Amount = 50_000_00L<Cent>
    Pool = Types.Pool.Main
    Description = "Manufacturing equipment"
}

let schedule = Calculations.scheduleDefault machinery
// Returns schedule with AIA in year 1, then WDA in subsequent years
```

### Key Features

- Annual Investment Allowance (AIA) application
- Writing Down Allowances with proper rates
- Midpoint-away-from-zero rounding
- Configurable AIA limits and maximum years

## Equipment Loans

### Capabilities

- Monthly payment calculation with various interest rates
- Complete amortization schedule generation
- Integration with MACRS depreciation analysis
- Support for residual values; `Principal` is the financed amount after any down payment

### Example Usage

```fsharp
open FSharp.Finance.Personal.EquipmentFinance

let loanTerms = {
    Principal = 10000_00L<Cent> // $10,000
    InterestRate = Interest.Rate.Annual (Percent 6.0m)
    TermMonths = 36
    MonthlyPayment = None
    EquipmentDescription = "Manufacturing Equipment"
    EquipmentCost = 10000_00L<Cent>
    DownPayment = 2000_00L<Cent>
    ResidualValue = 1000_00L<Cent>
}

let analysis = Loan.analyzeLoan loanTerms startDate
// Returns comprehensive loan analysis including depreciation
```

## Equipment Leases

### Lease Types Supported

- **Operating Lease**: Off-balance-sheet, lessor retains ownership
- **Finance Lease**: On-balance-sheet, lessee assumes ownership risks
- **Capital Lease**: Similar to finance lease under older standards

### Example Usage

```fsharp
open FSharp.Finance.Personal.EquipmentFinance

let leaseTerms = {
    EquipmentDescription = "Manufacturing Equipment"
    FairMarketValue = 10000_00L<Cent>
    TermMonths = 36
    LeaseType = Lease.LeaseType.FinanceLease
    PaymentFrequency = Lease.PaymentFrequency.Monthly
    LeasePayment = 300_00L<Cent>
    UpfrontPayment = 1000_00L<Cent>
    ResidualValue = 2000_00L<Cent>
    PurchaseOption = Some 2000_00L<Cent>
    ImplicitRate = Interest.Rate.Annual (Percent 5.0m)
}

let analysis = Lease.analyzeLeaseVsBuy leaseTerms startDate
// Returns lease vs buy analysis with depreciation comparison
```

### Key Features

- Multiple payment frequencies (monthly, quarterly, etc.)
- Present value calculations
- Lease vs. buy analysis with depreciation integration
- Support for purchase options

## Common Features

### Shared Utilities

The `DepreciationCommon` module provides:

- **Rounding utilities**: Consistent midpoint-away-from-zero rounding
- **Validation functions**: Input validation for amounts and percentages
- **Calculation helpers**: Common depreciation calculations
- **Educational disclaimers**: Standardized disclaimer text

### Integration

All modules integrate seamlessly with the existing FSharp.Finance.Personal library:

- Uses `int64<Cent>` for monetary values
- Integrates with `Interest.Rate` and `Percent` types
- Uses `DateDay.Date` for date handling
- Follows existing functional programming patterns

The consolidated implementation provides:

- Consistent namespacing as specified in requirements
- Proper module organization and compilation order
- Comprehensive functionality from both PRs
- Enhanced documentation and examples
- Standardized coding patterns

## Testing

Comprehensive test suites are provided for all modules:

- `DepreciationCommonTests.fs`: Tests for shared utilities
- `USMacrsTests.fs`: Tests for US MACRS calculations
- `UKCapitalAllowancesTests.fs`: Tests for UK Capital Allowances
- `EquipmentLoanTests.fs`: Tests for loan calculations
- `EquipmentLeaseTests.fs`: Tests for lease calculations

## Future Enhancements

Potential areas for expansion:

- Additional depreciation methods (e.g., declining balance)
- More sophisticated lease vs. buy NPV analysis
- Integration with tax calculation modules
- Support for partial-year conventions
- Multiple asset management capabilities
