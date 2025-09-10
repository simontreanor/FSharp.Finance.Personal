namespace FSharp.Finance.Personal.EquipmentFinance.Depreciation

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation

/// Common depreciation abstractions and utilities shared across depreciation modules.
/// 
/// This module provides common types, interfaces, and utility functions that can be
/// used across different depreciation calculation methods (UK Capital Allowances,
/// US MACRS, etc.).
module DepreciationCommon =

    /// Represents basic asset information
    type AssetInfo = {
        /// The original cost basis of the asset
        CostBasis: int64<Cent>
        /// Asset description
        Description: string
        /// Date asset was placed in service (optional)
        PlacedInServiceDate: DateDay.Date option
    }

    /// Common validation utilities
    module Validation =
        
        /// Validates that an amount is positive
        let validatePositiveAmount (amount: int64<Cent>) (fieldName: string) =
            if amount <= 0L<Cent> then
                failwith $"{fieldName} must be positive, got {amount}"
        
        /// Validates that a percentage is between 0 and 1
        let validatePercentage (percentage: decimal) (fieldName: string) =
            if percentage < 0m || percentage > 1m then
                failwith $"{fieldName} must be between 0 and 1, got {percentage}"
        
        /// Validates that a year count is positive
        let validateYearCount (years: int) (fieldName: string) =
            if years <= 0 then
                failwith $"{fieldName} must be positive, got {years}"

    /// Common calculation utilities
    module Calculations =
        
        /// Calculates the remaining value after depreciation
        let calculateRemainingValue (originalCost: int64<Cent>) (cumulativeDepreciation: int64<Cent>) =
            originalCost - cumulativeDepreciation
        
        /// Applies a percentage rate to a base amount
        let applyRate (baseAmount: int64<Cent>) (rate: decimal) =
            let result = decimal baseAmount * rate
            Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero) (result * 1m<Cent>)
        
        /// Ensures total depreciation does not exceed original cost
        let capDepreciationAtCost (originalCost: int64<Cent>) (proposedDepreciation: int64<Cent>) (cumulativeDepreciation: int64<Cent>) =
            let maxAllowable = originalCost - cumulativeDepreciation
            min proposedDepreciation maxAllowable

    /// Disclaimer text for educational use
    module Disclaimers =
        
        /// Standard educational disclaimer for all depreciation modules
        let EducationalDisclaimer = 
            "IMPORTANT DISCLAIMER: This module is for educational and analytical purposes only. " +
            "It is NOT tax advice and should not be used for actual tax calculations without " +
            "validation by qualified tax professionals. The implementation includes several " +
            "simplifications that may not reflect real-world tax scenarios."
        
        /// UK-specific disclaimer additions
        let UKSpecificDisclaimer =
            "This implementation is based on general UK capital allowances rules and may not " +
            "reflect recent changes, special circumstances, or specific industry rules. " +
            "Always consult HMRC guidance and qualified tax advisors."
        
        /// US-specific disclaimer additions
        let USSpecificDisclaimer =
            "This implementation is based on general US MACRS rules and may not reflect " +
            "recent tax law changes, special circumstances, or state-specific rules. " +
            "Always consult IRS publications and qualified tax professionals."