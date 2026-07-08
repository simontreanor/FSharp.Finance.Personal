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
        AnnualInvestmentAllowanceLimit: int64<Cent>
        /// Writing down allowance rate for main pool (typically 18%)
        MainPoolRate: decimal
        /// Writing down allowance rate for special rate pool (typically 6%)
        SpecialRatePoolRate: decimal
        /// Maximum number of years to calculate. Any pool value left after this many years
        /// is reported as UnclaimedPool on the schedule rather than silently dropped.
        MaxYears: int
        /// HMRC small pools allowance threshold: when the pool balance is at or below this
        /// value, the whole pool may be written off in that year (£1,000 under current rules)
        SmallPoolThreshold: int64<Cent>
    }

    /// Default configuration based on common UK rates
    let Default: CapitalAllowanceConfig = {
        AnnualInvestmentAllowanceLimit = 1_000_000_00L<Cent>
        MainPoolRate = 0.18m
        SpecialRatePoolRate = 0.06m
        MaxYears = 10
        SmallPoolThreshold = 1_000_00L<Cent>
    }

    /// Represents an expenditure item
    type Expenditure = {
        /// Cost of the asset in pounds
        Amount: int64<Cent>
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
        AnnualInvestmentAllowance: int64<Cent>
        /// Writing Down Allowance claimed
        WritingDownAllowance: int64<Cent>
        /// Total allowances for the year
        TotalAllowances: int64<Cent>
        /// Remaining pool value at year end
        PoolValueEndOfYear: int64<Cent>
    }

    /// A capital allowances schedule with any unclaimed residual made explicit
    type AllowanceSchedule = {
        /// Year-by-year allowances
        Years: YearAllowance list
        /// Pool value remaining unclaimed when the calculation stops at MaxYears.
        /// Zero when the pool is fully written off within the calculated years.
        UnclaimedPool: int64<Cent>
    }

/// UK Capital Allowances calculation functions
module Calculations =
    
    open Types

    /// Generates a capital allowances schedule for a single expenditure.
    /// Applies the HMRC small pools allowance: when the pool balance is at or below
    /// config.SmallPoolThreshold, the whole pool is written off in that year.
    /// Any pool value remaining after MaxYears is reported as UnclaimedPool.
    let generateSchedule (config: CapitalAllowanceConfig) (expenditure: Expenditure) : AllowanceSchedule =
        if expenditure.Amount <= 0L<Cent> then invalidArg (nameof expenditure.Amount) "Amount must be > 0."
        if config.MaxYears <= 0 then invalidArg (nameof config.MaxYears) "MaxYears must be > 0."
        if config.AnnualInvestmentAllowanceLimit < 0L<Cent> then invalidArg (nameof config.AnnualInvestmentAllowanceLimit) "AnnualInvestmentAllowanceLimit must be >= 0."
        if config.MainPoolRate < 0m then invalidArg (nameof config.MainPoolRate) "MainPoolRate must be >= 0."
        if config.SpecialRatePoolRate < 0m then invalidArg (nameof config.SpecialRatePoolRate) "SpecialRatePoolRate must be >= 0."
        if config.SmallPoolThreshold < 0L<Cent> then invalidArg (nameof config.SmallPoolThreshold) "SmallPoolThreshold must be >= 0."

        let rec calculateYears (year: int) (poolValue: int64<Cent>) (remainingAIA: int64<Cent>) (acc: YearAllowance list) =
            if year > config.MaxYears || poolValue <= 0L<Cent> then
                { Years = List.rev acc; UnclaimedPool = poolValue }
            else
                // Calculate AIA for this year (only available in year 1 for single addition)
                let aiaThisYear =
                    if year = 1 then
                        min poolValue remainingAIA
                    else
                        0L<Cent>

                // Remaining value after AIA
                let valueAfterAIA = poolValue - aiaThisYear

                // Calculate WDA rate based on pool type
                let wdaRate =
                    match expenditure.Pool with
                    | Pool.Main -> config.MainPoolRate
                    | Pool.SpecialRate -> config.SpecialRatePoolRate

                // Calculate Writing Down Allowance (WDA), applying the small pools allowance:
                // a pool at or below the threshold may be written off in full
                let wda =
                    if valueAfterAIA > 0L<Cent> && valueAfterAIA <= config.SmallPoolThreshold then
                        valueAfterAIA
                    else
                        let rawWda =
                            Cent.toDecimalCent valueAfterAIA * wdaRate
                            |> Cent.fromDecimalCent (Rounding.RoundWith MidpointRounding.AwayFromZero)
                        min rawWda valueAfterAIA

                // Total allowances for this year
                let totalAllowances = aiaThisYear + wda

                // Pool value at end of year
                let poolValueEOY = valueAfterAIA - wda

                let yearAllowance = {
                    Year = year
                    AnnualInvestmentAllowance = aiaThisYear
                    WritingDownAllowance = wda
                    TotalAllowances = totalAllowances
                    PoolValueEndOfYear = poolValueEOY
                }

                calculateYears (year + 1) poolValueEOY (remainingAIA - aiaThisYear) (yearAllowance :: acc)

        calculateYears 1 expenditure.Amount config.AnnualInvestmentAllowanceLimit []

    /// Generates a schedule using default configuration
    let scheduleDefault (expenditure: Expenditure) : AllowanceSchedule =
        generateSchedule Default expenditure

/// Example configurations and usage
module Examples =
    
    open Types
    open Calculations

    /// Example: Machinery costing £50,000 in main pool
    let exampleMachinery = {
        Amount = 50_000_00L<Cent>
        Pool = Pool.Main
        Description = "Manufacturing equipment"
    }

    /// Example: Vehicle costing £30,000 in special rate pool  
    let exampleVehicle = {
        Amount = 30_000_00L<Cent>
        Pool = Pool.SpecialRate
        Description = "Company vehicle"
    }

    /// Generate example schedule for machinery
    let exampleMachinerySchedule () = scheduleDefault exampleMachinery

    /// Generate example schedule for vehicle
    let exampleVehicleSchedule () = scheduleDefault exampleVehicle
