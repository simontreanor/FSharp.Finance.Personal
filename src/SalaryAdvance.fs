namespace FSharp.Finance.Personal

open System
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Formatting

/// zero-interest salary advance / earned wage access modeling helper
module SalaryAdvance =

    /// represents different repayment modes for salary advances
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type RepaymentMode =
        /// repayment in full on the first payroll after advance
        | LumpOnFirstPayroll
        /// repayment evenly distributed across multiple payrolls
        | EvenlyProrated
        /// custom repayment over specified number of days
        | Custom of Days: int64

        /// HTML formatting to display the repayment mode in a readable format
        member rm.Html =
            match rm with
            | LumpOnFirstPayroll -> "lump sum on first payroll"
            | EvenlyProrated -> "evenly prorated"
            | Custom days -> $"custom over {days} days"

    /// represents a cashflow item for salary advance analysis
    [<Struct>]
    type CashflowItem = {
        /// the date of the cashflow
        Date: Date
        /// the amount of the cashflow from the provider perspective
        Amount: int64<Cent>
        /// description of the cashflow item
        Description: string
    }

    /// represents a fee applicable to salary advances
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type SalaryAdvanceFee =
        /// flat fee amount
        | FlatFee of Amount: int64<Cent>
        /// percentage-based fee expressed as a fraction (e.g. 0.02m = 2%)
        | PercentageFee of Percentage: decimal
        /// no fee
        | NoFee

        /// HTML formatting to display the fee in a readable format
        member sf.Html =
            match sf with
            | FlatFee amount -> $"flat fee {formatCent amount}"
            | PercentageFee pct -> $"percentage fee {pct:P2}"
            | NoFee -> "no fee"

    /// configuration for salary advance schedule
    [<Struct>]
    type ScheduleConfig = {
        /// the date the advance is made
        AdvanceDate: Date
        /// the amount of the advance
        AdvanceAmount: int64<Cent>
        /// the repayment mode
        RepaymentMode: RepaymentMode
        /// any applicable fees
        Fee: SalaryAdvanceFee
        /// expected payroll dates for repayment planning
        PayrollDates: Date array
    }

    /// represents a schedule item for salary advance repayment
    [<Struct>]
    type ScheduleItem = {
        /// the date of the payment
        PaymentDate: Date
        /// the amount to be repaid
        RepaymentAmount: int64<Cent>
        /// remaining balance after this payment
        RemainingBalance: int64<Cent>
        /// any fees included in this payment
        FeeAmount: int64<Cent>
    }

    /// calculates the total fee amount based on advance amount and fee configuration
    let calculateFeeAmount advanceAmount fee =
        match fee with
        | NoFee -> 0L<Cent>
        | FlatFee amount -> amount
        | PercentageFee pct ->
            let dcnt = decimal advanceAmount * pct * 1m<Cent>
            Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero) dcnt

    let private distributeEvenly (totalAmount: int64<Cent>) count =
        if count <= 0 then
            [||]
        else
            let totalValue = int64 totalAmount
            let divisor = int64 count
            let baseValue = totalValue / divisor
            let remainderValue = totalValue % divisor

            Array.init count (fun i ->
                let paymentValue =
                    if i = count - 1 then
                        baseValue + remainderValue
                    else
                        baseValue

                LanguagePrimitives.Int64WithMeasure<Cent> paymentValue)

    let private isStrictlyIncreasing dates =
        dates
        |> Array.pairwise
        |> Array.forall (fun (previousDate, nextDate) -> nextDate > previousDate)

    let private validateConfig config =
        let errors = ResizeArray<string>()

        if config.AdvanceAmount <= 0L<Cent> then
            errors.Add("Advance amount must be positive")

        match config.Fee with
        | FlatFee amount when amount < 0L<Cent> ->
            errors.Add("Flat fee cannot be negative")
        | PercentageFee pct when pct <= 0m || pct >= 1m ->
            errors.Add("Percentage fee must be greater than 0 and less than 1")
        | _ -> ()

        match config.RepaymentMode with
        | Custom days when days <= 0L ->
            errors.Add("Custom repayment days must be positive")
        | LumpOnFirstPayroll | EvenlyProrated when config.PayrollDates.Length = 0 ->
            errors.Add("Payroll dates required for this repayment mode")
        | _ -> ()

        if config.PayrollDates.Length > 1 && not (isStrictlyIncreasing config.PayrollDates) then
            errors.Add("Payroll dates must be strictly increasing")

        if config.PayrollDates |> Array.exists (fun d -> d <= config.AdvanceDate) then
            errors.Add("All payroll dates must be after advance date")

        errors.ToArray()

    /// creates a repayment schedule based on the configuration
    let createSchedule (config: ScheduleConfig) : ScheduleItem array =
        let errors = validateConfig config

        if errors.Length > 0 then
            let errorText = String.concat "; " errors
            invalidArg "config" errorText

        let totalFee = calculateFeeAmount config.AdvanceAmount config.Fee
        let totalAmount = config.AdvanceAmount + totalFee

        match config.RepaymentMode with
        | LumpOnFirstPayroll ->
            let firstPayroll = config.PayrollDates |> Array.head

            [| {
                PaymentDate = firstPayroll
                RepaymentAmount = totalAmount
                RemainingBalance = 0L<Cent>
                FeeAmount = totalFee
            } |]

        | EvenlyProrated ->
            let payrollCount = config.PayrollDates.Length
            let paymentAmounts = distributeEvenly totalAmount payrollCount
            let feeAmounts = distributeEvenly totalFee payrollCount
            let mutable remainingBalance: int64<Cent> = totalAmount

            config.PayrollDates
            |> Array.mapi (fun i payrollDate ->
                let paymentAmount = paymentAmounts.[i]
                remainingBalance <- remainingBalance - paymentAmount

                {
                    PaymentDate = payrollDate
                    RepaymentAmount = paymentAmount
                    RemainingBalance = remainingBalance
                    FeeAmount = feeAmounts.[i]
                })

        | Custom days ->
            let repaymentDate = config.AdvanceDate.AddDays(int days)
            [| {
                PaymentDate = repaymentDate
                RepaymentAmount = totalAmount
                RemainingBalance = 0L<Cent>
                FeeAmount = totalFee
            } |]

    /// exports the schedule as cashflow items for analytical use
    let exportCashflows (config: ScheduleConfig) : CashflowItem array =
        let schedule = createSchedule config

        // Provider perspective: disbursement out, repayments in.
        let advanceCashflow = {
            Date = config.AdvanceDate
            Amount = -config.AdvanceAmount
            Description = "Salary advance disbursement"
        }

        let repaymentCashflows =
            schedule
            |> Array.map (fun item -> {
                Date = item.PaymentDate
                Amount = item.RepaymentAmount
                Description = $"Repayment (principal: {formatCent (item.RepaymentAmount - item.FeeAmount)}, fee: {formatCent item.FeeAmount})"
            })

        Array.concat [| [| advanceCashflow |]; repaymentCashflows |]

    let borrowerCashflows (config: ScheduleConfig) : CashflowItem array =
        config
        |> exportCashflows
        |> Array.map (fun cashflow -> { cashflow with Amount = -cashflow.Amount })

    /// calculates summary statistics for the salary advance
    let calculateSummary (config: ScheduleConfig) =
        let schedule = createSchedule config
        let totalFee = calculateFeeAmount config.AdvanceAmount config.Fee
        
        let termInDays = 
            if schedule.Length > 0 then
                (schedule |> Array.last).PaymentDate - config.AdvanceDate
                |> fun ts -> ts.Days
            else 0

        {|
            AdvanceAmount = config.AdvanceAmount
            TotalFeeAmount = totalFee
            TotalRepaymentAmount = config.AdvanceAmount + totalFee
            TermInDays = termInDays
            NumberOfPayments = schedule.Length
            EffectiveFeeRate = if config.AdvanceAmount > 0L<Cent> then decimal totalFee / decimal config.AdvanceAmount * 100m else 0m
        |}

    /// module for working with schedule configurations
    module ScheduleConfig =

        /// creates a basic schedule configuration
        let create advanceDate advanceAmount repaymentMode payrollDates =
            {
                AdvanceDate = advanceDate
                AdvanceAmount = advanceAmount
                RepaymentMode = repaymentMode
                Fee = NoFee
                PayrollDates = payrollDates
            }

        /// adds a fee to the schedule configuration
        let withFee fee config =
            { config with Fee = fee }

        /// validates the schedule configuration
        let validate config =
            validateConfig config
