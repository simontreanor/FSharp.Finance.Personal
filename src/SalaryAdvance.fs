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
        /// the amount of the cashflow (positive for advances, negative for repayments)
        Amount: int64<Cent>
        /// description of the cashflow item
        Description: string
    }

    /// represents a fee applicable to salary advances
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type SalaryAdvanceFee =
        /// flat fee amount
        | FlatFee of Amount: int64<Cent>
        /// percentage-based fee
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
            let dcnt = (decimal advanceAmount * pct / 100m) * 1m<Cent>
            Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero) dcnt

    /// creates a repayment schedule based on the configuration
    let createSchedule (config: ScheduleConfig) : ScheduleItem array =
        let totalFee = calculateFeeAmount config.AdvanceAmount config.Fee
        let totalAmount = config.AdvanceAmount + totalFee

        match config.RepaymentMode with
        | LumpOnFirstPayroll ->
            match config.PayrollDates |> Array.tryHead with
            | Some firstPayroll ->
                [| {
                    PaymentDate = firstPayroll
                    RepaymentAmount = totalAmount
                    RemainingBalance = 0L<Cent>
                    FeeAmount = totalFee
                } |]
            | None -> [||]

        | EvenlyProrated ->
            let payrollCount = config.PayrollDates.Length
            if payrollCount = 0 then [||]
            else
                let basePayment = totalAmount / (int64 payrollCount)
                let remainder = totalAmount % (int64 payrollCount * 1L<Cent>)
                
                config.PayrollDates
                |> Array.mapi (fun i payrollDate ->
                    let isLastPayment = i = payrollCount - 1
                    let paymentAmount = 
                        if isLastPayment then basePayment + remainder
                        else basePayment
                    
                    let remainingPayments = payrollCount - i - 1
                    let remainingBalance = (int64 remainingPayments) * basePayment
                    
                    {
                        PaymentDate = payrollDate
                        RepaymentAmount = paymentAmount
                        RemainingBalance = if isLastPayment then 0L<Cent> else remainingBalance
                        FeeAmount = if i = 0 then totalFee else 0L<Cent>
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
        
        // Initial advance (positive cashflow)
        let advanceCashflow = {
            Date = config.AdvanceDate
            Amount = config.AdvanceAmount
            Description = "Salary advance disbursement"
        }
        
        // Repayment cashflows (negative amounts)
        let repaymentCashflows =
            schedule
            |> Array.map (fun item -> {
                Date = item.PaymentDate
                Amount = -item.RepaymentAmount
                Description = $"Repayment (principal: {formatCent (item.RepaymentAmount - item.FeeAmount)}, fee: {formatCent item.FeeAmount})"
            })
        
        Array.concat [| [| advanceCashflow |]; repaymentCashflows |]

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
            let errors = ResizeArray<string>()
            
            if config.AdvanceAmount <= 0L<Cent> then
                errors.Add("Advance amount must be positive")
            
            match config.RepaymentMode with
            | Custom days when days <= 0L ->
                errors.Add("Custom repayment days must be positive")
            | LumpOnFirstPayroll | EvenlyProrated when config.PayrollDates.Length = 0 ->
                errors.Add("Payroll dates required for this repayment mode")
            | _ -> ()
                
            if config.PayrollDates |> Array.exists (fun d -> d <= config.AdvanceDate) then
                errors.Add("All payroll dates must be after advance date")
                
            errors.ToArray()