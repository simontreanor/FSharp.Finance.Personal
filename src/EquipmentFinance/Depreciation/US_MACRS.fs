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
        // 20-year (IRS Table A-1, three-decimal percentages summing to exactly 100.000)
        [| 3.750m; 7.219m; 6.677m; 6.177m; 5.713m; 5.285m; 4.888m; 4.522m; 4.462m; 4.461m; 4.462m; 4.461m; 4.462m; 4.461m; 4.462m; 4.461m; 4.462m; 4.461m; 4.462m; 4.461m; 2.231m |]
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
        if asset.CostBasis <= 0L<Cent> then invalidArg (nameof asset.CostBasis) "CostBasis must be > 0."
        if asset.Convention <> Convention.HalfYear then
            invalidArg (nameof asset.Convention) "Only HalfYear convention is currently supported."

        let percentages = getDepreciationPercentages asset.PropertyClass
        
        let rec calculateYears (year: int) (accumulatedDep: int64<Cent>) (acc: DepreciationYear list) =
            if year > percentages.Length then
                List.rev acc
            else
                let rate = percentages.[year - 1] / 100m // Convert percentage to decimal
                let rawDepreciationAmount = 
                    decimal asset.CostBasis * rate * 1m<Cent>
                    |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
                let remainingBasis = asset.CostBasis - accumulatedDep
                let depreciationAmount = min rawDepreciationAmount remainingBasis
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
        if asset.Convention <> Convention.HalfYear then
            invalidArg (nameof asset.Convention) "Only HalfYear convention is currently supported."
        if year <= 0 then invalidArg (nameof year) "Year must be > 0."

        let percentage = getMacrsPercentage asset.PropertyClass year
        let schedule = generateSchedule asset
        let matchedYear = schedule |> List.tryFind (fun depreciationYear -> depreciationYear.Year = year)

        let depreciationAmount = 
            decimal asset.CostBasis * (percentage / 100m) * 1m<Cent>
            |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
        
        // Calculate cumulative depreciation through this year
        match matchedYear with
        | Some depreciationYear -> depreciationYear
        | None ->
            {
                Year = year
                DepreciationRate = percentage / 100m
                DepreciationAmount = depreciationAmount
                AccumulatedDepreciation = asset.CostBasis
                BookValue = 0L<Cent>
            }

    /// Attempt to determine the MACRS property class from an asset description.
    /// Returns None when no keyword matches, so callers can surface the fact that an
    /// asset class was assumed rather than recognized.
    /// Real-property descriptions (buildings etc.) are rejected: under MACRS, buildings are
    /// 27.5-year (residential) / 39-year (nonresidential) straight-line REAL property, which
    /// this simplified module does not model.
    let tryClassifyAsset (assetDescription: string) : AssetClass option =
        let desc = assetDescription.ToLowerInvariant()
        let realPropertyKeywords = [ "building"; "warehouse"; "real property"; "real estate"; "residential"; "nonresidential"; "apartment"; "office block" ]
        if realPropertyKeywords |> List.exists desc.Contains then
            invalidArg (nameof assetDescription)
                "Real property (27.5-year residential / 39-year nonresidential straight-line) is not supported by this simplified MACRS module."
        elif desc.Contains "computer" || desc.Contains "car" || desc.Contains "truck" || desc.Contains "vehicle" then
            Some AssetClass.FiveYear
        elif desc.Contains "furniture" || desc.Contains "manufacturing" then
            Some AssetClass.SevenYear
        elif desc.Contains "boat" || desc.Contains "barge" then
            Some AssetClass.TenYear
        elif desc.Contains "land improvement" || desc.Contains "parking lot" || desc.Contains "fence" || desc.Contains "landscaping" || desc.Contains "gas station" || desc.Contains "billboard" then
            Some AssetClass.FifteenYear
        else
            None

    /// Determine the MACRS property class based on asset description, defaulting to
    /// FiveYear (most business equipment) when no keyword matches.
    /// Use tryClassifyAsset to detect whether the default assumption was applied.
    let classifyAsset (assetDescription: string) : AssetClass =
        tryClassifyAsset assetDescription |> Option.defaultValue AssetClass.FiveYear

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
