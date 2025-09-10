# FSharp.Finance.Personal

Personal-finance functions written in F#

This library helps you verify your personal finance events, including verifying and creating payment plans, repayment schedules 
and interest calculations for things like mortgages, loans, hire purchases, debt consolidations, car loans, line of credit, etc. 

Initial features:

> APR calculation

> amortisation

> XIRR (Excel-Compatible)

## XIRR (Excel-Compatible)

Calculate the Extended Internal Rate of Return (XIRR) for irregular cash flows with Excel compatibility.

```fsharp
open System
open FSharp.Finance.Personal

let cashflows = [ DateTime(2025,1,1), -10000m; DateTime(2026,1,1), 11000m ]
let r = Xirr.xirr cashflows
printfn "XIRR = %.4f%%" (r * 100m)
// Output: XIRR = 10.0000%
```

Features:
- **Excel compatibility**: Uses ExcelFinancialFunctions library with default guess=0.1
- **Multiple functions**: `xirr`, `xirrG` (custom guess), `tryXirr` (safe Result type)
- **Input validation**: Ensures mixed signs, sufficient data points, and non-identical dates
- **Decimal precision**: Returns rates as decimal values for financial calculations

The XIRR functions follow Excel's sign convention where negative values represent outflows (investments, payments) and positive values represent inflows (returns, receipts).

This library operates partially in areas where business is regulated by various regulators.
Though every care has been taken to ensure the accuracy of the results, please independently validate the figures produced by it.
It is not audited or validated by any of the regulators.

If you have any suggestions or corrections, please feel free to comment or create a pull request.

For commercial use the user might need an operating license and to fulfil various statutory and regulatory requirements,
none of which are conferred by the use of this library.

NuGet package: https://www.nuget.org/packages/FSharp.Finance.Personal/

Documentation: https://simontreanor.dev/FSharp.Finance.Personal/
