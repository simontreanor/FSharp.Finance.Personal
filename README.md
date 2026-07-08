# FSharp.Finance.Personal

Personal-finance functions written in F#

This library helps you verify your personal finance events, including verifying and creating payment plans, repayment schedules 
and interest calculations for things like mortgages, loans, hire purchases, debt consolidations, car loans, line of credit, etc. 

Initial features:

> APR calculation

> amortisation

> XIRR (Excel-Compatible)

## XIRR (Excel-Compatible)

Calculate the Extended Internal Rate of Return (XIRR) for irregular cash flows with Excel-compatible function semantics.

```fsharp
open FSharp.Finance.Personal
open DateDay

let cashflows = [ Date(2025, 1, 1), -10000m; Date(2026, 1, 1), 11000m ]
let r = Xirr.xirr cashflows
printfn "XIRR = %.4f%%" (r * 100m)
// Output: XIRR = 10.0000%
```

Features:
- **Excel-compatible semantics**: Uses the ExcelFinancialFunctions library with Excel's default guess of 0.1; results typically match Excel to high precision, though the underlying convergence implementation differs from Excel's, so small differences are possible
- **Multiple functions**: `xirr`, `xirrG` (custom guess), `tryXirr`/`tryXirrG` (safe Result type), and `xirrCents`/`tryXirrCents` for cashflows expressed in the library's `int64<Cent>` money representation
- **Input validation**: Ensures mixed signs, sufficient data points, non-identical dates and an in-domain guess; cashflows are sorted by date internally, so input order does not matter
- **Decimal return type**: Returns rates as `decimal` values, while the underlying XIRR calculation uses floating-point arithmetic

The XIRR functions follow Excel's sign convention from the borrower's perspective: negative values represent outflows such as investments or loan payments, and positive values represent inflows such as returns or loan disbursements.

This library operates partially in areas where business is regulated by various regulators.
Though every care has been taken to ensure the accuracy of the results, please independently validate the figures produced by it.
It is not audited or validated by any of the regulators.

If you have any suggestions or corrections, please feel free to comment or create a pull request.

For commercial use the user might need an operating license and to fulfil various statutory and regulatory requirements,
none of which are conferred by the use of this library.

NuGet package: https://www.nuget.org/packages/FSharp.Finance.Personal/

Documentation: https://simontreanor.dev/FSharp.Finance.Personal/
