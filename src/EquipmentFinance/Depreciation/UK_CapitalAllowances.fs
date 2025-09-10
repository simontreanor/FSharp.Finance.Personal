namespace FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation

/// UK Capital Allowances module for equipment finance depreciation calculations.
/// 
/// IMPORTANT DISCLAIMER: This module is for educational and analytical purposes only.
/// It is NOT tax advice and should not be used for actual tax calculations without
/// validation by qualified tax professionals. The implementation includes several
/// simplifications that may not reflect real-world tax scenarios.
///
/// Key simplifications:
/// - Single asset addition per year only
/// - No disposals or part-exchanges
/// - No special rate pool transfers
/// - Simplified AIA application (full amount in year 1 only)
/// - No consideration of accounting periods vs tax years
/// - No integration with other allowances or reliefs
/// - Rounding uses midpoint-away-from-zero only
/// - No handling of short periods or cessation

/// UK Capital Allowances types and configuration
module Types =
    
    /// Represents the type of capital allowances pool
    [<Struct; RequireQualifiedAccess>]
    type Pool =
        | Main          // 18% writing down allowance
        | SpecialRate   // 6% writing down allowance

    /// Configuration for capital allowances calculations
    type CapitalAllowanceConfig = {
        /// Annual Investment Allowance limit (£1,000,000 in recent years)
        AnnualInvestmentAllowanceLimit: decimal
        /// Writing down allowance rate for main pool (typically 18%)
        MainPoolRate: decimal
        /// Writing down allowance rate for special rate pool (typically 6%)
        SpecialRatePoolRate: decimal
        /// Maximum number of years to calculate
        MaxYears: int
    }

    /// Default configuration based on common UK rates
    let Default: CapitalAllowanceConfig = {
        AnnualInvestmentAllowanceLimit = 1_000_000m
        MainPoolRate = 0.18m
        SpecialRatePoolRate = 0.06m
        MaxYears = 10
    }

    /// Represents an expenditure item
    type Expenditure = {
        /// Cost of the asset in pounds
        Amount: decimal
        /// Pool classification
        Pool: Pool
        /// Description of the asset
        Description: string
    }

    /// Represents allowances for a particular year
    type YearAllowance = {
        /// Year number (1-based)
        Year: int
        /// Annual Investment Allowance claimed
        AnnualInvestmentAllowance: decimal
        /// Writing Down Allowance claimed
        WritingDownAllowance: decimal
        /// Total allowances for the year
        TotalAllowances: decimal
        /// Remaining pool value at year end
        PoolValueEndOfYear: decimal
    }

/// UK Capital Allowances calculation functions
module Calculations =
    
    open Types

    /// Rounds a decimal value using midpoint-away-from-zero rounding
    let roundAwayFromZero (value: decimal) =
        DepreciationCommon.Rounding.roundCurrency value

    /// Generates a capital allowances schedule for a single expenditure
    let generateSchedule (config: CapitalAllowanceConfig) (expenditure: Expenditure) : YearAllowance list =
        
        let rec calculateYears (year: int) (poolValue: decimal) (remainingAIA: decimal) (acc: YearAllowance list) =
            if year > config.MaxYears || poolValue <= 0m then
                List.rev acc
            else
                // Calculate AIA for this year (only available in year 1 for single addition)
                let aiaThisYear = 
                    if year = 1 then
                        min poolValue remainingAIA
                    else
                        0m

                // Remaining value after AIA
                let valueAfterAIA = poolValue - aiaThisYear

                // Calculate WDA rate based on pool type
                let wdaRate = 
                    match expenditure.Pool with
                    | Pool.Main -> config.MainPoolRate
                    | Pool.SpecialRate -> config.SpecialRatePoolRate

                // Calculate WDA
                let wda = roundAwayFromZero (valueAfterAIA * wdaRate)

                // Total allowances for this year
                let totalAllowances = aiaThisYear + wda

                // Pool value at end of year
                let poolValueEOY = valueAfterAIA - wda

                let yearAllowance = {
                    Year = year
                    AnnualInvestmentAllowance = roundAwayFromZero aiaThisYear
                    WritingDownAllowance = roundAwayFromZero wda
                    TotalAllowances = roundAwayFromZero totalAllowances
                    PoolValueEndOfYear = roundAwayFromZero poolValueEOY
                }

                calculateYears (year + 1) poolValueEOY (remainingAIA - aiaThisYear) (yearAllowance :: acc)

        calculateYears 1 expenditure.Amount config.AnnualInvestmentAllowanceLimit []

    /// Generates a schedule using default configuration
    let scheduleDefault (expenditure: Expenditure) : YearAllowance list =
        generateSchedule Default expenditure

/// Example configurations and usage
module Examples =
    
    open Types
    open Calculations

    /// Example: Machinery costing £50,000 in main pool
    let exampleMachinery = {
        Amount = 50_000m
        Pool = Pool.Main
        Description = "Manufacturing equipment"
    }

    /// Example: Vehicle costing £30,000 in special rate pool  
    let exampleVehicle = {
        Amount = 30_000m
        Pool = Pool.SpecialRate
        Description = "Company vehicle"
    }

    /// Generate example schedule for machinery
    let exampleMachinerySchedule () = scheduleDefault exampleMachinery

    /// Generate example schedule for vehicle
    let exampleVehicleSchedule () = scheduleDefault exampleVehicle