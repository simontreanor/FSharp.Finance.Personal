namespace FSharp.Finance.B2B

open FSharp.Finance.Personal

/// Trade credit early-payment discount analysis utilities
module TradeCredit =

    open System
    open Calculation

    /// Trade credit discount terms (e.g., "2/10 net 30")
    type DiscountTerms = {
        /// Discount percentage (e.g., 2% = 0.02m)
        DiscountRate: decimal
        /// Number of days within which payment must be made to receive discount
        DiscountPeriodDays: int
        /// Total payment period in days (net terms)
        NetPeriodDays: int
    }

    /// Discount terms helper functions
    module DiscountTerms =
        
        /// Create discount terms from common "X/Y net Z" notation
        /// Example: createTerms 2m 10 30 creates "2/10 net 30" terms
        let createTerms (discountPercentage: decimal) (discountDays: int) (netDays: int) =
            if discountPercentage < 0m || discountPercentage >= 100m then
                invalidArg (nameof discountPercentage) "Discount percentage must be between 0 and less than 100"
            if discountDays < 0 then
                invalidArg (nameof discountDays) "Discount days must be non-negative"
            if netDays <= discountDays then
                invalidArg (nameof netDays) "Net days must be greater than discount days"
            
            {
                DiscountRate = discountPercentage / 100m
                DiscountPeriodDays = discountDays
                NetPeriodDays = netDays
            }

        /// Calculate the simple annual rate equivalent of the early payment discount
        /// Formula: (Discount% / (100% - Discount%)) * (365 / (Net Days - Discount Days))
        /// This represents the effective annual interest rate for forgoing the discount
        let impliedAnnualRateSimple (terms: DiscountTerms) =
            if terms.DiscountRate <= 0m then
                0m
            else
                let effectiveDiscountRate = terms.DiscountRate / (1m - terms.DiscountRate)
                let daysExtension = decimal (terms.NetPeriodDays - terms.DiscountPeriodDays)
                let annualizationFactor = 365m / daysExtension
                effectiveDiscountRate * annualizationFactor

        /// Calculate the compounded annual rate equivalent of the early payment discount
        /// Formula: ((100% / (100% - Discount%))^(365/(Net Days - Discount Days))) - 1
        /// This represents the compounded annual rate when discount opportunities occur multiple times per year
        let impliedAnnualRateCompounded (terms: DiscountTerms) =
            if terms.DiscountRate <= 0m then
                0m
            else
                let baseRate = 1m / (1m - terms.DiscountRate)
                let daysExtension = decimal (terms.NetPeriodDays - terms.DiscountPeriodDays)
                let exponent = 365m / daysExtension
                let compoundedRate = powm exponent baseRate |> decimal
                compoundedRate - 1m

        /// Create standard "2/10 net 30" discount terms
        let standard2_10Net30 = createTerms 2m 10 30

        /// Create standard "1/15 net 45" discount terms  
        let standard1_15Net45 = createTerms 1m 15 45

    /// Analysis functions for trade credit scenarios
    module Analysis =
        
        /// Calculate the cost of not taking the discount (simple annual rate)
        let costOfNotTakingDiscount (terms: DiscountTerms) =
            DiscountTerms.impliedAnnualRateSimple terms

        /// Calculate the effective annual rate if discount opportunities compound
        let effectiveAnnualRate (terms: DiscountTerms) =
            DiscountTerms.impliedAnnualRateCompounded terms

        /// Calculate the effective cost per period for the extended payment terms
        let costPerPeriod (terms: DiscountTerms) =
            terms.DiscountRate / (1m - terms.DiscountRate)

        /// Calculate the break-even borrowing rate (simple annual)
        /// This is the rate at which borrowing money to take the discount becomes neutral
        let breakEvenBorrowingRate (terms: DiscountTerms) =
            costOfNotTakingDiscount terms
