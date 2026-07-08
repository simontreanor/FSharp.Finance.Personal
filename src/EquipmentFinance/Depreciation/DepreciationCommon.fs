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

    /// Per‑period depreciation result
    type DepreciationPeriod = {
        Period: int
        Depreciation: int64<Cent>
        Accumulated: int64<Cent>
        BookValue: int64<Cent>
        /// "SL", "DB", "DB (final adjustment)", or "DB->SL".
        /// Pure declining balance never reaches the salvage value by itself, so its final
        /// period is a plug that lands exactly on salvage; that period is labelled
        /// "DB (final adjustment)" to distinguish it from rate-based periods.
        Method: string
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

        // Private validation helper
        let private validateInputs (cost: int64<Cent>) (salvage: int64<Cent>) (life: int) =
            if life <= 0 then invalidArg (nameof life) "Life must be > 0."
            if cost <= 0L<Cent> then invalidArg (nameof cost) "Cost must be > 0."
            if salvage < 0L<Cent> then invalidArg (nameof salvage) "Salvage must be >= 0."
            if salvage >= cost then invalidArg (nameof salvage) "Salvage must be less than cost."

        // Enforce final salvage exactly by reconstructing the last period if needed.
        let private enforceSalvage (cost: int64<Cent>) (salvage: int64<Cent>) (schedule: DepreciationPeriod array) =
            if schedule.Length = 0 then schedule
            else
                let lastIndex = schedule.Length - 1
                let last = schedule[lastIndex]
                if last.BookValue = salvage then schedule
                else
                    let life = schedule.Length
                    let prevAccum, prevBookValue =
                        if life = 1 then
                            0L<Cent>, cost
                        else
                            let prev = schedule.[lastIndex - 1]
                            prev.Accumulated, prev.BookValue
                    let finalDepDec = Cent.toDecimal prevBookValue - Cent.toDecimal salvage
                    let finalDep = Cent.fromDecimal finalDepDec
                    let finalAccum = prevAccum + finalDep
                    let adjustedLast = {
                        last with
                            Depreciation = finalDep
                            Accumulated  = finalAccum
                            BookValue    = salvage
                    }
                    let clone = Array.copy schedule
                    clone[life-1] <- adjustedLast
                    clone

        /// Straight-line depreciation schedule.
        /// cost      : original asset cost (in cents)
        /// salvage   : target final book value (in cents)
        /// life      : number of periods
        /// Returns an array of period records (1-based Period index).
        /// Rounding: Per-period depreciation is rounded to cents. The final period is adjusted
        /// to ensure book value equals salvage exactly (within rounding).
        let straightLine (cost: int64<Cent>) (salvage: int64<Cent>) (life: int) : DepreciationPeriod array =
            validateInputs cost salvage life

            let costDec    = Cent.toDecimal cost
            let salvageDec = Cent.toDecimal salvage
            let totalDepDec = costDec - salvageDec
            let rawPerPeriod = totalDepDec / decimal life

            // Recursive builder
            let rec build p bookValueDec accumulated (acc: DepreciationPeriod list) =
                if p > life then
                    acc |> List.rev |> List.toArray
                else
                    let isLast = p = life
                    let depDecCandidate =
                        if isLast then
                            bookValueDec - salvageDec
                        else
                            rawPerPeriod

                    // Round candidate
                    let depCentsInitial =
                        if depDecCandidate <= 0m then 0L<Cent>
                        else Cent.fromDecimal depDecCandidate

                    // Ensure we don't breach salvage before the last period
                    let bookValueAfter =
                        bookValueDec - Cent.toDecimal depCentsInitial

                    let depCents =
                        if not isLast && bookValueAfter < salvageDec then
                            // Clamp to exactly reach salvage at this point
                            let neededDec = bookValueDec - salvageDec
                            if neededDec <= 0m then 0L<Cent> else Cent.fromDecimal neededDec
                        else
                            depCentsInitial

                    let newBookValueDec = bookValueDec - Cent.toDecimal depCents
                    let newAccumulated = accumulated + depCents
                    let periodRecord = {
                        Period       = p
                        Depreciation = depCents
                        Accumulated  = newAccumulated
                        BookValue    = Cent.fromDecimal newBookValueDec
                        Method       = "SL"
                    }

                    build (p+1) newBookValueDec newAccumulated (periodRecord :: acc)

            build 1 costDec 0L<Cent> []
            |> enforceSalvage cost salvage


        /// Declining balance depreciation schedule (e.g. rateFactor = 2.0 for 200% / double declining).
        /// Automatically switches to straight-line when switchToStraightLine = true AND
        /// the straight-line remainder for the period exceeds the declining balance amount.
        ///
        /// cost                : original asset cost (cents)
        /// salvage             : target final residual book value (cents)
        /// life                : total number of periods
        /// rateFactor          : acceleration multiple (1.0 = standard, 2.0 = double, 1.5 = 150%, etc.)
        /// switchToStraightLine: if true, permanently switches to SL once advantageous
        ///
        /// Rounding: Per-period depreciation is rounded to cents; final period adjusted for exact salvage.
        ///
        /// Note: without the straight-line switch, a declining balance never reaches the salvage
        /// value by itself, so the final period is a plug (book value minus salvage) rather than a
        /// rate-based amount; it is labelled "DB (final adjustment)" to make this explicit.
        let decliningBalance
            (cost: int64<Cent>)
            (salvage: int64<Cent>)
            (life: int)
            (rateFactor: decimal)
            (switchToStraightLine: bool)
            : DepreciationPeriod array =

            validateInputs cost salvage life
            if rateFactor <= 0m then invalidArg (nameof rateFactor) "Rate factor must be > 0."

            let costDec    = Cent.toDecimal cost
            let salvageDec = Cent.toDecimal salvage

            // Recursive builder
            let rec build p bookValueDec accumulated inStraight (acc: DepreciationPeriod list) =
                if p > life then
                    acc |> List.rev |> List.toArray
                else
                    let remaining = life - p + 1
                    let isLast = p = life

                    let straightLineRemainderDec =
                        if remaining > 0 then (bookValueDec - salvageDec) / decimal remaining
                        else 0m

                    let decliningDec =
                        if isLast then
                            bookValueDec - salvageDec
                        else
                            (rateFactor / decimal life) * bookValueDec

                    // Decide switching
                    let (chosenDec, inSLNow, tag) =
                        if inStraight then
                            (straightLineRemainderDec, true, "DB->SL")
                        elif switchToStraightLine && straightLineRemainderDec > decliningDec then
                            (straightLineRemainderDec, true, "DB->SL")
                        elif isLast then
                            // pure DB never lands on salvage by itself: the final period is a
                            // plug to exactly reach the salvage value, labelled distinctly
                            (decliningDec, false, "DB (final adjustment)")
                        else
                            (decliningDec, false, "DB")

                    // Prevent going below salvage before last period
                    let chosenDecClamped =
                        if not isLast && (bookValueDec - chosenDec) < salvageDec then
                            bookValueDec - salvageDec
                        else
                            chosenDec

                    let depCents =
                        if chosenDecClamped <= 0m then 0L<Cent>
                        else Cent.fromDecimal chosenDecClamped

                    let newBookValueDec = bookValueDec - Cent.toDecimal depCents
                    let newAccumulated = accumulated + depCents

                    let methodFinal =
                        if inSLNow then "DB->SL" else tag

                    let periodRecord = {
                        Period       = p
                        Depreciation = depCents
                        Accumulated  = newAccumulated
                        BookValue    = Cent.fromDecimal newBookValueDec
                        Method       = methodFinal
                    }

                    build (p+1) newBookValueDec newAccumulated inSLNow (periodRecord :: acc)

            build 1 costDec 0L<Cent> false []
            |> enforceSalvage cost salvage


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