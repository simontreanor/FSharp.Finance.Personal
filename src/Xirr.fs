namespace FSharp.Finance.Personal

open System

/// Excel-compatible XIRR (Extended Internal Rate of Return) calculations
/// using ExcelFinancialFunctions library to ensure parity with Excel results
[<RequireQualifiedAccess>]
module Xirr =

    /// Validates that the cashflow list meets XIRR calculation requirements
    let private validate (cashflows: (DateTime * decimal) list) =
        if cashflows.Length < 2 then
            invalidArg (nameof cashflows) "At least two cashflows are required for XIRR calculation"
        
        let values = cashflows |> List.map snd
        let hasPositive = values |> List.exists (fun v -> v > 0m)
        let hasNegative = values |> List.exists (fun v -> v < 0m)
        
        if not hasPositive then
            invalidArg (nameof cashflows) "At least one positive cashflow is required"
        if not hasNegative then
            invalidArg (nameof cashflows) "At least one negative cashflow is required"
        
        let dates = cashflows |> List.map fst
        let uniqueDates = dates |> List.distinct
        if uniqueDates.Length = 1 then
            invalidArg (nameof cashflows) "Cashflows cannot all have identical dates"

    /// Converts decimal cashflows to float sequences for ExcelFinancialFunctions library
    let private convertToSequences (cashflows: (DateTime * decimal) list) =
        let dates = cashflows |> List.map fst
        let values = cashflows |> List.map (fun (_, value) -> float value)
        (values, dates)

    /// <summary>
    /// Calculates the Extended Internal Rate of Return (XIRR) for a series of cashflows.
    /// Uses Excel-compatible calculation with default guess of 0.1 (10%).
    /// </summary>
    /// <param name="cashflows">List of (date, cashflow) pairs. 
    /// Negative values represent outflows (investments, payments from borrower perspective).
    /// Positive values represent inflows (returns, receipts from borrower perspective).</param>
    /// <returns>Annualized effective rate as decimal (e.g., 0.10 for 10%)</returns>
    /// <remarks>
    /// Sign convention follows Excel standard:
    /// - Negative cashflows: money going out (investments, loan disbursements)  
    /// - Positive cashflows: money coming in (returns, loan payments)
    /// Default guess of 0.1 ensures Excel compatibility.
    /// Precision may be limited by conversion from decimal to float for underlying calculation.
    /// </remarks>
    let xirr (cashflows: (DateTime * decimal) list) : decimal =
        validate cashflows
        let (values, dates) = convertToSequences cashflows
        let result = Excel.FinancialFunctions.Financial.XIrr(values, dates, 0.1)
        decimal result

    /// <summary>
    /// Calculates the Extended Internal Rate of Return (XIRR) for a series of cashflows
    /// with a custom initial guess.
    /// </summary>
    /// <param name="guess">Initial guess for the XIRR calculation (e.g., 0.1 for 10%)</param>
    /// <param name="cashflows">List of (date, cashflow) pairs</param>
    /// <returns>Annualized effective rate as decimal</returns>
    /// <remarks>
    /// Same sign convention as xirr function. Custom guess may improve convergence
    /// for some cashflow patterns but should generally not be necessary.
    /// </remarks>
    let xirrG (guess: decimal) (cashflows: (DateTime * decimal) list) : decimal =
        validate cashflows
        let (values, dates) = convertToSequences cashflows
        let result = Excel.FinancialFunctions.Financial.XIrr(values, dates, float guess)
        decimal result

    /// <summary>
    /// Attempts to calculate the Extended Internal Rate of Return (XIRR) for a series of cashflows,
    /// returning a Result type instead of throwing exceptions.
    /// </summary>
    /// <param name="cashflows">List of (date, cashflow) pairs</param>
    /// <returns>Result containing the XIRR rate on success, or error message on failure</returns>
    /// <remarks>
    /// Uses default guess of 0.1. Returns Result.Error for validation failures or
    /// convergence problems in the underlying calculation.
    /// </remarks>
    let tryXirr (cashflows: (DateTime * decimal) list) : Result<decimal, string> =
        try
            let result = xirr cashflows
            Ok result
        with
        | :? System.ArgumentException as ex -> Error ex.Message
        | ex -> Error $"XIRR calculation failed: {ex.Message}"