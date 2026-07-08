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

## Business (B2B) Analytical Extensions (New)

The library now includes optional B2B analytical extensions for business-to-business finance scenarios:

All new business analytics live under the `FSharp.Finance.B2B` namespace.

### Trade Credit Analysis
- **Early-payment discount calculations**: Analyze terms like "2/10 net 30" to determine the implied annual cost of not taking discounts
- **Simple and compounded rate calculations**: Understand the true cost of extending payment terms
- **Standard terms support**: Built-in support for common trade credit arrangements

### Invoice Factoring
- **Parameter construction**: Build amortization parameters for factoring arrangements
- **Advance calculations**: Model invoice advances where the upfront fee is deducted from the gross advance and the rest of the face value is held in reserve, so net advance + fee + reserve = face value exactly
- **Integration ready**: Factoring parameters run through the core scheduling engine and close with a zero principal balance (the gross advances repaid on the due dates cover the net advances plus the upfront fees)

### Product Classification
- **Metadata support**: Classify financial products for organizational and analytical purposes
- **Specialized helpers**: `ProductMetadata.tradeCredit` and `ProductMetadata.invoiceFactoring`
- **Future-ready cashflow modeling**: Extensible types for advanced cashflow analysis (see the trade credit and factoring example for a demonstration)

**Important**: These B2B extensions are for analytical purposes only and do NOT determine regulatory status. They complement the core personal finance calculations while providing specialized business analytical capabilities.

NuGet package: https://www.nuget.org/packages/FSharp.Finance.Personal/

Documentation: https://simontreanor.dev/FSharp.Finance.Personal/
