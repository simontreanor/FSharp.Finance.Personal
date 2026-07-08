namespace FSharp.Finance.Personal

open System

/// Excel-compatible XIRR (Extended Internal Rate of Return) calculations
/// using the ExcelFinancialFunctions library: same function semantics and default
/// guess (0.1) as Excel, though the underlying convergence implementation differs,
/// so results may occasionally differ from Excel in the last decimal places
[<RequireQualifiedAccess>]
module Xirr =

    open Calculation
    open DateDay

    /// Validates that the cashflow list meets XIRR calculation requirements
    let private validate (cashflows: (Date * decimal) list) =
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

    /// Validates that the initial guess is within the domain of the rate function (> -1)
    let private validateGuess (guess: decimal) =
        if guess <= -1m then
            invalidArg (nameof guess) "Guess must be greater than -1 (-100%)"

    /// Sorts cashflows by date (the underlying library requires the first cashflow
    /// to carry the earliest date) and converts them to float sequences for the
    /// ExcelFinancialFunctions library
    let private convertToSequences (cashflows: (Date * decimal) list) =
        let sorted = cashflows |> List.sortBy fst
        let dates = sorted |> List.map (fun (date, _) -> date.ToDateTime())
        let values = sorted |> List.map (fun (_, value) -> float value)
        (values, dates)

    /// <summary>
    /// Calculates the Extended Internal Rate of Return (XIRR) for a series of cashflows.
    /// Uses Excel-compatible function semantics with Excel's default guess of 0.1 (10%).
    /// </summary>
    /// <param name="cashflows">List of (date, cashflow) pairs, in any date order.
    /// Negative values represent outflows from the borrower perspective
    /// (for example, investments or loan payments made by the borrower).
    /// Positive values represent inflows from the borrower perspective
    /// (for example, returns or loan disbursements received by the borrower).</param>
    /// <returns>Annualized effective rate as decimal (e.g., 0.10 for 10%)</returns>
    /// <remarks>
    /// Sign convention follows Excel standard, expressed from the borrower perspective:
    /// - Negative cashflows: money going out (investments, loan payments)
    /// - Positive cashflows: money coming in (returns, loan disbursements)
    /// Cashflows are sorted by date internally, so input order does not matter.
    /// Results typically match Excel to high precision, but the underlying library uses
    /// a different convergence implementation than Excel, so small differences are possible.
    /// If the calculation fails to converge, the underlying library surfaces this as a raw
    /// System.Exception; use tryXirr to have such failures returned as a Result.Error instead.
    /// Precision may be limited by conversion from decimal to float for underlying calculation.
    /// </remarks>
    let xirr (cashflows: (Date * decimal) list) : decimal =
        validate cashflows
        let (values, dates) = convertToSequences cashflows
        let result = Excel.FinancialFunctions.Financial.XIrr(values, dates, 0.1)
        decimal result

    /// <summary>
    /// Calculates the Extended Internal Rate of Return (XIRR) for a series of cashflows
    /// with a custom initial guess.
    /// </summary>
    /// <param name="guess">Initial guess for the XIRR calculation (e.g., 0.1 for 10%);
    /// must be greater than -1 (-100%)</param>
    /// <param name="cashflows">List of date-only cashflow pairs using `DateDay.Date`,
    /// in any date order</param>
    /// <returns>Annualized effective rate as decimal</returns>
    /// <remarks>
    /// Same sign convention as xirr function. Custom guess may improve convergence
    /// for some cashflow patterns but should generally not be necessary.
    /// Throws System.ArgumentException if the guess is not greater than -1.
    /// If the calculation fails to converge, the underlying library surfaces this as a raw
    /// System.Exception; use tryXirrG to have such failures returned as a Result.Error instead.
    /// </remarks>
    let xirrG (guess: decimal) (cashflows: (Date * decimal) list) : decimal =
        validateGuess guess
        validate cashflows
        let (values, dates) = convertToSequences cashflows
        let result = Excel.FinancialFunctions.Financial.XIrr(values, dates, float guess)
        decimal result

    /// <summary>
    /// Attempts to calculate the Extended Internal Rate of Return (XIRR) for a series of cashflows,
    /// returning a Result type instead of throwing exceptions.
    /// </summary>
    /// <param name="cashflows">List of date-only cashflow pairs using `DateDay.Date`,
    /// in any date order</param>
    /// <returns>Result containing the XIRR rate on success, or error message on failure</returns>
    /// <remarks>
    /// Uses default guess of 0.1. Returns Result.Error for validation failures or
    /// convergence problems in the underlying calculation.
    /// </remarks>
    let tryXirr (cashflows: (Date * decimal) list) : Result<decimal, string> =
        try
            let result = xirr cashflows
            Ok result
        with
        | :? System.ArgumentException as ex -> Error ex.Message
        | ex -> Error $"XIRR calculation failed: {ex.Message}"

    /// <summary>
    /// Attempts to calculate the Extended Internal Rate of Return (XIRR) for a series of cashflows
    /// with a custom initial guess, returning a Result type instead of throwing exceptions.
    /// </summary>
    /// <param name="guess">Initial guess for the XIRR calculation (e.g., 0.1 for 10%);
    /// must be greater than -1 (-100%)</param>
    /// <param name="cashflows">List of date-only cashflow pairs using `DateDay.Date`,
    /// in any date order</param>
    /// <returns>Result containing the XIRR rate on success, or error message on failure</returns>
    /// <remarks>
    /// Same sign convention as xirr function. Returns Result.Error for validation failures
    /// (including an out-of-domain guess) or convergence problems in the underlying calculation.
    /// </remarks>
    let tryXirrG (guess: decimal) (cashflows: (Date * decimal) list) : Result<decimal, string> =
        try
            let result = xirrG guess cashflows
            Ok result
        with
        | :? System.ArgumentException as ex -> Error ex.Message
        | ex -> Error $"XIRR calculation failed: {ex.Message}"

    /// <summary>
    /// Calculates the Extended Internal Rate of Return (XIRR) for a series of cashflows
    /// expressed in the library's base currency unit (int64&lt;Cent&gt;).
    /// </summary>
    /// <param name="cashflows">List of (date, cashflow) pairs where cashflows are in cents,
    /// in any date order; same sign convention as xirr</param>
    /// <returns>Annualized effective rate as decimal (e.g., 0.10 for 10%)</returns>
    /// <remarks>
    /// Converts cent values to decimal via Cent.toDecimal and delegates to xirr,
    /// so the same behaviour, sign convention and exceptions apply.
    /// </remarks>
    let xirrCents (cashflows: (Date * int64<Cent>) list) : decimal =
        cashflows
        |> List.map (fun (date, value) -> date, Cent.toDecimal value)
        |> xirr

    /// <summary>
    /// Attempts to calculate the Extended Internal Rate of Return (XIRR) for a series of cashflows
    /// expressed in the library's base currency unit (int64&lt;Cent&gt;),
    /// returning a Result type instead of throwing exceptions.
    /// </summary>
    /// <param name="cashflows">List of (date, cashflow) pairs where cashflows are in cents,
    /// in any date order; same sign convention as xirr</param>
    /// <returns>Result containing the XIRR rate on success, or error message on failure</returns>
    /// <remarks>
    /// Converts cent values to decimal via Cent.toDecimal and delegates to tryXirr.
    /// </remarks>
    let tryXirrCents (cashflows: (Date * int64<Cent>) list) : Result<decimal, string> =
        cashflows
        |> List.map (fun (date, value) -> date, Cent.toDecimal value)
        |> tryXirr
