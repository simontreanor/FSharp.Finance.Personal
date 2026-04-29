# FSharp.Finance.Personal

Personal-finance functions written in F#

This library helps you verify your personal finance events, including verifying and creating payment plans, repayment schedules 
and interest calculations for things like mortgages, loans, hire purchases, debt consolidations, car loans, line of credit, etc. 

Initial features:

> APR calculation

> amortisation

This library operates partially in areas where business is regulated by various regulators.
Though every care has been taken to ensure the accuracy of the results, please independently validate the figures produced by it.
It is not audited or validated by any of the regulators.

If you have any suggestions or corrections, please feel free to comment or create a pull request.

For commercial use the user might need an operating license and to fulfil various statutory and regulatory requirements,
none of which are conferred by the use of this library.

NuGet package: https://www.nuget.org/packages/FSharp.Finance.Personal/

Documentation: https://simontreanor.dev/FSharp.Finance.Personal/

## Why choose FSharp.Finance.Personal?

| Feature | FSharp.Finance.Personal | ExcelFinancialFunctions | QuantLib |
|---|---|---|---|
| Language | Idiomatic F# | F# (Excel-compatible) | C++ / Python / C# (not F#-native) |
| APR calculation | EU / UK / US regulatory methods | No | No |
| Amortisation schedules | Full scheduling, actual vs scheduled payments | No | Partial |
| UK regulatory accuracy | Yes (FCA-aligned) | No | No |
| Fees, charges & interest caps | Yes | No | No |
| Settlement & refinancing quotes | Yes | No | Partial |
| Open development | Yes | Yes | Yes |

### ExcelFinancialFunctions

[ExcelFinancialFunctions](https://github.com/fsprojects/ExcelFinancialFunctions) is a lower-level library providing
Excel-compatible financial functions (NPV, IRR, RATE, etc.). It is ideal when you need to replicate spreadsheet
behaviour exactly, but it does not model full repayment schedules, does not handle regulatory APR calculations, and
provides no UK-specific consumer finance features.

### QuantLib

[QuantLib](https://www.quantlib.org/) is a comprehensive quantitative finance library covering derivatives, fixed-income
instruments and risk models. It is very broad in scope, not F#-native, and does not support UK consumer-finance
regulatory calculations such as FCA-style APR or the Consumer Credit Act schedule requirements.

### Why choose this library?

An F# developer working in UK consumer finance should choose `FSharp.Finance.Personal` because:

- **Idiomatic F# types** — discriminated unions, units of measure, and immutable records make it easy to model
  financial products accurately with the compiler as a safety net.
- **Regulatory accuracy** — APR calculations follow the EU Directive 2008/48/EC, the FCA's UK rules, and the CFPB's
  US Actuarial method; amortisation logic is designed to comply with consumer-credit regulations.
- **Full product lifecycle** — from generating an initial payment schedule through to handling missed payments,
  settlement quotes, and refinancing, covering the whole life of a consumer-credit product.
- **Open development** — the library is open-source, actively maintained, and welcomes contributions.
