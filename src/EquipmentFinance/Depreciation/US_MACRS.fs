namespace FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation

/// US MACRS (Modified Accelerated Cost Recovery System) module for equipment depreciation calculations.
/// 
/// IMPORTANT DISCLAIMER: This module is for educational and analytical purposes only.
/// It is NOT tax advice and should not be used for actual tax calculations without
/// validation by qualified tax professionals. The implementation includes several
/// simplifications that may not reflect real-world tax scenarios.
///
/// Key simplifications:
/// - Limited to half-year convention only
/// - Simplified asset class definitions
/// - No mid-quarter convention handling
/// - No Section 179 deduction integration
/// - No bonus depreciation considerations
/// - No averaging conventions for real property
/// - Simplified recovery period options
/// - No consideration of placed-in-service dates
/// - Educational percentage tables only

/// MACRS asset classes and configuration types
module Types =
    
    /// Represents MACRS asset classes with their typical recovery periods
    [<Struct; RequireQualifiedAccess>]
    type AssetClass =
        | ThreeYear     // Certain special tools, small manufacturing equipment
        | FiveYear      // Most equipment, computers, office machinery, vehicles
        | SevenYear     // Office furniture, equipment not in other classes
        | TenYear       // Boats, barges, single-purpose structures
        | FifteenYear   // Land improvements, gas stations, billboards
        | TwentyYear    // Farm buildings, utility property

    /// MACRS depreciation convention
    [<Struct; RequireQualifiedAccess>]
    type Convention =
        | HalfYear      // half-year convention (most common)
        | MidQuarter    // mid-quarter convention (when over 40% of assets placed in service in Q4)

    /// Represents a year in the depreciation schedule
    type DepreciationYear = {
        /// Year number (1-based)
        Year: int
        /// Depreciation percentage for this year
        DepreciationRate: decimal
        /// Annual depreciation amount
        DepreciationAmount: int64<Cent>
        /// Accumulated depreciation to date
        AccumulatedDepreciation: int64<Cent>
        /// Remaining book value
        BookValue: int64<Cent>
    }

    /// Configuration for MACRS calculations
    type MacrsAsset = {
        /// Original cost/basis of the asset
        CostBasis: int64<Cent>
        /// Date the asset was placed in service
        PlacedInServiceDate: DateDay.Date
        /// Asset classification
        PropertyClass: AssetClass
        /// Convention used
        Convention: Convention
    }

/// MACRS percentage tables and lookups
module Tables =
    
    open Types

    /// Half-year convention depreciation percentages by asset class and year
    /// These are simplified educational tables - actual IRS tables should be used for real calculations
    let private halfYearPercentages = [|
        // 3-year
        [| 33.33m; 44.45m; 14.81m; 7.41m |]
        // 5-year  
        [| 20.00m; 32.00m; 19.20m; 11.52m; 11.52m; 5.76m |]
        // 7-year
        [| 14.29m; 24.49m; 17.49m; 12.49m; 8.93m; 8.92m; 8.93m; 4.46m |]
        // 10-year
        [| 10.00m; 18.00m; 14.40m; 11.52m; 9.22m; 7.37m; 6.55m; 6.55m; 6.56m; 6.55m; 3.28m |]
        // 15-year
        [| 5.00m; 9.50m; 8.55m; 7.70m; 6.93m; 6.23m; 5.90m; 5.90m; 5.91m; 5.90m; 5.91m; 5.90m; 5.91m; 5.90m; 5.91m; 2.95m |]
        // 20-year
        [| 3.75m; 7.22m; 6.68m; 6.18m; 5.71m; 5.29m; 4.89m; 4.52m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 4.46m; 2.25m |]
    |]

    /// Get the depreciation percentages for a given asset class
    let getDepreciationPercentages (assetClass: AssetClass) : decimal array =
        let tableIndex = 
            match assetClass with
            | AssetClass.ThreeYear -> 0
            | AssetClass.FiveYear -> 1
            | AssetClass.SevenYear -> 2
            | AssetClass.TenYear -> 3
            | AssetClass.FifteenYear -> 4
            | AssetClass.TwentyYear -> 5
        
        halfYearPercentages.[tableIndex]

    /// Get the MACRS percentage for a given property class and year
    let getMacrsPercentage (propertyClass: AssetClass) (year: int) : decimal =
        let percentages = getDepreciationPercentages propertyClass
        if year > 0 && year <= percentages.Length then
            percentages.[year - 1]
        else
            0m

/// MACRS calculation functions
module Calculations =
    
    open Types
    open Tables

    /// Calculate the MACRS depreciation schedule for an asset
    let generateSchedule (asset: MacrsAsset) : DepreciationYear list =
        let percentages = getDepreciationPercentages asset.PropertyClass
        
        let rec calculateYears (year: int) (accumulatedDep: int64<Cent>) (acc: DepreciationYear list) =
            if year > percentages.Length then
                List.rev acc
            else
                let rate = percentages.[year - 1] / 100m // Convert percentage to decimal
                let depreciationAmount = 
                    decimal asset.CostBasis * rate * 1m<Cent>
                    |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
                let newAccumulated = accumulatedDep + depreciationAmount
                let bookValue = asset.CostBasis - newAccumulated

                let depreciationYear = {
                    Year = year
                    DepreciationRate = rate
                    DepreciationAmount = depreciationAmount
                    AccumulatedDepreciation = newAccumulated
                    BookValue = bookValue
                }

                calculateYears (year + 1) newAccumulated (depreciationYear :: acc)

        calculateYears 1 0L<Cent> []

    /// Get the recovery period for an asset class
    let getRecoveryPeriod (assetClass: AssetClass) : int =
        match assetClass with
        | AssetClass.ThreeYear -> 3
        | AssetClass.FiveYear -> 5
        | AssetClass.SevenYear -> 7
        | AssetClass.TenYear -> 10
        | AssetClass.FifteenYear -> 15
        | AssetClass.TwentyYear -> 20

    /// Calculate MACRS depreciation for a specific year
    let calculateMacrsDepreciation (asset: MacrsAsset) (year: int) : DepreciationYear =
        let percentage = getMacrsPercentage asset.PropertyClass year
        let depreciationAmount = 
            decimal asset.CostBasis * (percentage / 100m) * 1m<Cent>
            |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
        
        // Calculate cumulative depreciation through this year
        let cumulativeDepreciation = 
            [1..year]
            |> List.sumBy (fun y -> 
                let pct = getMacrsPercentage asset.PropertyClass y
                decimal asset.CostBasis * (pct / 100m))
            |> (*) 1m<Cent>
            |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
            |> min asset.CostBasis
        
        let bookValue = asset.CostBasis - cumulativeDepreciation
        
        {
            Year = year
            DepreciationRate = percentage / 100m
            DepreciationAmount = depreciationAmount
            AccumulatedDepreciation = cumulativeDepreciation
            BookValue = bookValue
        }

    /// Determine the MACRS property class based on asset description
    let classifyAsset (assetDescription: string) : AssetClass =
        let desc = assetDescription.ToLowerInvariant()
        if desc.Contains("computer") || desc.Contains("car") || desc.Contains("truck") then
            AssetClass.FiveYear
        elif desc.Contains("furniture") || desc.Contains("manufacturing") then
            AssetClass.SevenYear
        elif desc.Contains("building") then
            AssetClass.FifteenYear
        else
            AssetClass.FiveYear // default to 5-year for most business equipment

/// Example configurations and usage
module Examples =
    
    open Types
    open Calculations

    /// Example: Computer equipment costing $10,000 (5-year property)
    let exampleComputer = {
        CostBasis = 10000_00L<Cent>
        PlacedInServiceDate = DateDay.Date(2024, 1, 1)
        PropertyClass = AssetClass.FiveYear
        Convention = Convention.HalfYear
    }

    /// Example: Office furniture costing $5,000 (7-year property)
    let exampleFurniture = {
        CostBasis = 5000_00L<Cent>
        PlacedInServiceDate = DateDay.Date(2024, 1, 1)
        PropertyClass = AssetClass.SevenYear
        Convention = Convention.HalfYear
    }

    /// Example: Manufacturing equipment costing $25,000 (7-year property)
    let exampleEquipment = {
        CostBasis = 25000_00L<Cent>
        PlacedInServiceDate = DateDay.Date(2024, 1, 1)
        PropertyClass = AssetClass.SevenYear
        Convention = Convention.HalfYear
    }

    /// Generate example schedule for computer
    let exampleComputerSchedule () = generateSchedule exampleComputer

    /// Generate example schedule for furniture
    let exampleFurnitureSchedule () = generateSchedule exampleFurniture

    /// Generate example schedule for equipment
    let exampleEquipmentSchedule () = generateSchedule exampleEquipment