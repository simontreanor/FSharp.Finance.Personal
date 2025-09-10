(**
---
title: Trade Credit and Invoice Factoring Example
category: Business (B2B) Analytical Extensions
categoryindex: 4
index: 9
description: B2B analytics for trade credit discount rates and invoice factoring
keywords: trade credit, factoring, early payment discount, B2B finance
---

# Trade Credit and Invoice Factoring Example

This example demonstrates the use of the B2B analytical extensions for trade credit early-payment discount calculations 
and invoice factoring parameter construction.

**IMPORTANT DISCLAIMERS:**
- These modules are for analytical purposes only and do NOT determine regulatory status
- Results should not be used for regulatory compliance without proper legal and financial advice
- Always consult qualified professionals for actual business decisions

## Trade Credit Early-Payment Discount Analysis

Trade credit terms like "2/10 net 30" offer early-payment discounts. Let's analyze the implied annual cost of not taking the discount.

*)

#r "nuget: FSharp.Finance.Personal"

open FSharp.Finance.Personal
open FSharp.Finance.Personal.TradeCredit

// Create standard "2/10 net 30" discount terms
let terms2_10 = DiscountTerms.createTerms 2m 10 30

// Calculate the implied annual rate (simple)
let impliedRateSimple = DiscountTerms.impliedAnnualRateSimple terms2_10
printfn "2/10 net 30 - Simple implied annual rate: %.2f%%" (impliedRateSimple * 100m)

// Calculate the compounded annual rate
let impliedRateCompounded = DiscountTerms.impliedAnnualRateCompounded terms2_10  
printfn "2/10 net 30 - Compounded annual rate: %.2f%%" (impliedRateCompounded * 100m)

// This shows that not taking a 2% discount to extend payment from 10 to 30 days
// is equivalent to borrowing at approximately 37.24% simple annual rate!

(**

### Interpreting the Results

The calculation shows:
- **Simple Annual Rate**: ~37.24% - This is the effective annual interest rate for forgoing the discount
- **Compounded Rate**: ~44.59% - This accounts for multiple discount opportunities throughout the year

The formula for the simple rate is: `(Discount% / (100% - Discount%)) * (365 / (Net Days - Discount Days))`

For 2/10 net 30: `(2% / 98%) * (365 / 20) = 0.0204 * 18.25 = 37.24%`

## Invoice Factoring Analysis

Now let's model an invoice factoring scenario where a business sells invoices to improve cash flow.

*)

open FSharp.Finance.Personal.InvoiceFactoring
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Calculation

// Create sample invoices
let invoice1 = Invoice.create "INV-001" 10000_00L<Cent> (Date(2024, 1, 15)) (Date(2024, 2, 14)) "Customer-A"
let invoice2 = Invoice.create "INV-002" 15000_00L<Cent> (Date(2024, 1, 20)) (Date(2024, 2, 19)) "Customer-B"

// Create invoice advances with 80% advance rate and 2% factoring fee
let advance1 = InvoiceAdvance.derive invoice1 0.80m 0.02m
let advance2 = InvoiceAdvance.derive invoice2 0.80m 0.02m

printfn "\n=== Invoice Factoring Analysis ==="
printfn "Invoice 1: Face Value $%.2f, Net Advance $%.2f, Fee $%.2f, Reserve $%.2f" 
    (Cent.toDecimal invoice1.FaceValue) 
    (Cent.toDecimal advance1.NetAdvance) 
    (Cent.toDecimal advance1.UpfrontFee) 
    (Cent.toDecimal advance1.ReserveAmount)

printfn "Invoice 2: Face Value $%.2f, Net Advance $%.2f, Fee $%.2f, Reserve $%.2f" 
    (Cent.toDecimal invoice2.FaceValue) 
    (Cent.toDecimal advance2.NetAdvance) 
    (Cent.toDecimal advance2.UpfrontFee) 
    (Cent.toDecimal advance2.ReserveAmount)

// Calculate aggregate statistics
let advances = [| advance1; advance2 |]
let stats = FactoringParameters.calculateStatistics advances

printfn "\n=== Aggregate Statistics ==="
printfn "Total Face Value: $%.2f" (Cent.toDecimal stats.TotalFaceValue)
printfn "Total Net Advance: $%.2f" (Cent.toDecimal stats.TotalNetAdvance)
printfn "Total Fees: $%.2f" (Cent.toDecimal stats.TotalUpfrontFees)
printfn "Total Reserve: $%.2f" (Cent.toDecimal stats.TotalReserve)
printfn "Weighted Average Advance Rate: %.1f%%" (stats.WeightedAverageAdvanceRate * 100m)
printfn "Weighted Average Fee Rate: %.1f%%" (stats.WeightedAverageFeeRate * 100m)
printfn "Average Credit Period: %d days" stats.AverageCreditPeriodDays

// Build amortization parameters for the factoring arrangement
let factoringParams = FactoringParameters.build advances None

printfn "\n=== Factoring Parameters Built ==="
printfn "Start Date: %s" (factoringParams.Basic.StartDate.ToString())
printfn "Principal (Total Net Advance): $%.2f" (Cent.toDecimal factoringParams.Basic.Principal)
printfn "Number of Scheduled Payments: %d" 
    (match factoringParams.Basic.ScheduleConfig with 
     | Scheduling.CustomSchedule payments -> Map.count payments 
     | _ -> 0)

(**

## Understanding the Economics

### Trade Credit Analysis
- The 37.24% implied rate shows the high cost of not taking early payment discounts
- Businesses should compare this rate to their borrowing costs to make optimal decisions
- If you can borrow at less than 37.24%, take the discount and pay early

### Invoice Factoring Analysis  
- Factoring provides immediate cash flow at the cost of fees and reduced collections
- The 80% advance rate means immediate access to 80% of invoice value
- The 2% fee is charged upfront, reducing the net advance
- The remaining 18% is held as reserve until customer payment

### Integration with Core Library
The factoring parameters can be used with the main amortization engine:

```fsharp
// This would run a full amortization schedule (if no existing build issues)
// let schedule = Amortisation.generate factoringParams Map.empty
```

## Additional Trade Credit Scenarios

Let's look at other common trade credit terms:

*)

// 1/15 net 45 terms
let terms1_15 = DiscountTerms.createTerms 1m 15 45
let rate1_15 = DiscountTerms.impliedAnnualRateSimple terms1_15
printfn "\n1/15 net 45 - Simple implied annual rate: %.2f%%" (rate1_15 * 100m)

// 3/7 net 21 terms (more aggressive)
let terms3_7 = DiscountTerms.createTerms 3m 7 21  
let rate3_7 = DiscountTerms.impliedAnnualRateSimple terms3_7
printfn "3/7 net 21 - Simple implied annual rate: %.2f%%" (rate3_7 * 100m)

// Use the analysis functions
let costOfNotTaking = Analysis.costOfNotTakingDiscount terms2_10
let effectiveRate = Analysis.effectiveAnnualRate terms2_10
let breakEvenRate = Analysis.breakEvenBorrowingRate terms2_10

printfn "\n=== Analysis Functions for 2/10 net 30 ==="
printfn "Cost of not taking discount: %.2f%%" (costOfNotTaking * 100m)
printfn "Effective annual rate: %.2f%%" (effectiveRate * 100m)  
printfn "Break-even borrowing rate: %.2f%%" (breakEvenRate * 100m)

(**

## Summary

The B2B analytical extensions provide powerful tools for:

1. **Trade Credit Analysis**: Calculate the true cost of payment terms to make informed decisions
2. **Invoice Factoring**: Model factoring arrangements and integrate with cash flow analysis
3. **Product Classification**: Organize financial products for analytical purposes

These tools complement the core personal finance calculations while providing specialized B2B analytical capabilities.

**Remember**: These are analytical tools only. Always consult financial and legal professionals for actual business decisions.

*)

printfn "\n=== Example completed successfully ==="