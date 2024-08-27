# F# Finance - Personal

FSharp.Finance.Personal is a set of functions for calculating Annual Percentage Rates (APRs) and amortisation schedules on products such as personal loans, mortgages, etc.

## APRs

An Annual Percentage Rate (APR) is a standardised way of calculating the cost of a financial product by extrapolating charges, interest and fees and annualising the results. The aim is to make it easier for customers to compare the cost of different products.

Calculating APRs is highly regulated. Initially this library supports two methods, the UK method devised by the Financial Conduct Authority (FCA) and the US Acturial method specified by the Consumer Financial Protection Bureau (CFPB).

Of the two, the US calculation method is the more complicated, and this library attempts to comply with the regulations as closely as possible, though sometimes the implementation requires a certain interpretation of the rules. No warranty is provided for the accuracy of the results but the library has been tested against all of the single-advance examples provided by the CFPB.

[UK APR calculation example](exampleAprUk.fsx)

[US APR calculation examples](exampleAprUs.fsx)

## Amortisation

Amortisation is the process of gradually paying down the principal of a lending product such as a mortgage or loan, taking into account any charges, interest and fees that may arise until the product is settled (fully paid off).

This library is able to generate amortisation schedules based on a highly customisable set of parameters, taking into account scheduled payments and actual payments, calculating charges, interest and fees, producing statements showing the current progress of a product or quotes for settlement of a product.

[Amortisation examples](exampleAmortisation.fsx)

## More about this library

[General design considerations](generalDesign.md)

[Algorithms in depth](algorithms.md)

## Future development

### The `FSharp.Finance` family

`FSharp.Finance.Personal` covers personal finance, whereas `FSharp.Finance.Corporate` and `FSharp.Finance.Public` could potentially cover other areas of finance - anyone interested!?