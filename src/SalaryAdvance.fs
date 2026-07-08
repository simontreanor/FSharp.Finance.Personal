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
        /// custom repayment amounts, one per payroll date: the amounts must all be positive and must sum to the total repayable
        | Custom of RepaymentAmounts: int64<Cent> list

        /// HTML formatting to display the repayment mode in a readable format
        member rm.Html =
            match rm with
            | LumpOnFirstPayroll -> "lump sum on first payroll"
            | EvenlyProrated -> "evenly prorated"
            | Custom amounts -> $"custom amounts over {List.length amounts} payroll dates"

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
        /// percentage-based fee expressed as a percentage of the advance amount (e.g. Percent 2m = 2%)
        | PercentageFee of Percentage: Percent
        /// no fee
        | NoFee

        /// HTML formatting to display the fee in a readable format
        member sf.Html =
            match sf with
            | FlatFee amount -> $"flat fee {formatCent amount}"
            | PercentageFee(Percent pct) -> $"percentage fee {pct} %%"
            | NoFee -> "no fee"

    /// how the fee is collected relative to the advance
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type FeeTreatment =
        /// the fee is deducted from the disbursed amount: net disbursed = advance − fee; total repayable = advance
        | NettedFromProceeds
        /// the fee is added to the amount repayable: net disbursed = advance; total repayable = advance + fee
        | AddedOnTop

        /// HTML formatting to display the fee treatment in a readable format
        member ft.Html =
            match ft with
            | NettedFromProceeds -> "fee netted from proceeds"
            | AddedOnTop -> "fee added on top"

    /// configuration for salary advance schedule
    /// > NOTE: this is deliberately a reference type: as a struct, default initialisation would produce a value
    /// > with a null PayrollDates array, which is a foot-gun for consumers
    type ScheduleConfig = {
        /// the date the advance is made
        AdvanceDate: Date
        /// the amount of the advance
        AdvanceAmount: int64<Cent>
        /// the repayment mode
        RepaymentMode: RepaymentMode
        /// any applicable fees
        Fee: SalaryAdvanceFee
        /// how the fee is collected: netted from the disbursed proceeds or added on top of the amount repayable
        FeeTreatment: FeeTreatment
        /// how to round a percentage-based fee to whole cents
        FeeRounding: Rounding
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
        /// the outstanding portion of the total repayable after this payment; note that the total repayable
        /// includes the fee when the fee treatment is AddedOnTop, whereas when the fee treatment is
        /// NettedFromProceeds the fee is collected at disbursement so the balance covers the advance only
        RemainingBalance: int64<Cent>
        /// the portion of this payment attributable to the fee, allocated in proportion to the payment amounts;
        /// always zero when the fee treatment is NettedFromProceeds, as the fee is then collected at disbursement
        FeeAmount: int64<Cent>
    }

    /// the largest advance or flat fee amount supported (int64.MaxValue / 10 cents), guarding against silent
    /// int64 overflow when the advance and the fee are summed
    let maxSupportedAmount = 922_337_203_685_477_580L<Cent>

    /// calculates the total fee amount based on the advance amount and fee configuration, rounding any
    /// percentage-based fee to whole cents using the given rounding method
    let calculateFeeAmount rounding advanceAmount fee =
        match fee with
        | NoFee -> 0L<Cent>
        | FlatFee amount -> amount
        | PercentageFee pct ->
            let dcnt = decimal advanceAmount * Percent.toDecimal pct * 1m<Cent>
            Cent.fromDecimalCent rounding dcnt

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

    /// allocates a total fee across payments in proportion to the payment amounts, so that the per-payment
    /// principal/fee decomposition (payment − fee) is coherent: fee shares are derived from cumulative payment
    /// shares (rounded down), which guarantees non-negative per-payment fees that sum exactly to the total fee,
    /// with any rounding remainder falling on the later payments
    let private allocateFeeByPaymentShare (totalFee: int64<Cent>) (paymentAmounts: int64<Cent> array) =
        let totalPayments = paymentAmounts |> Array.sumBy decimal

        if totalFee = 0L<Cent> || totalPayments = 0m then
            Array.create paymentAmounts.Length 0L<Cent>
        else
            let mutable cumulativePayment = 0m
            let mutable previousCumulativeFee = 0L

            paymentAmounts
            |> Array.map (fun paymentAmount ->
                cumulativePayment <- cumulativePayment + decimal paymentAmount

                let cumulativeFee =
                    decimal totalFee * cumulativePayment / totalPayments |> floor |> int64

                let feeValue = cumulativeFee - previousCumulativeFee
                previousCumulativeFee <- cumulativeFee
                LanguagePrimitives.Int64WithMeasure<Cent> feeValue)

    /// the total amount to be repaid across all scheduled repayments: the advance plus the fee when the fee is
    /// added on top, or the advance alone when the fee is netted from the disbursed proceeds
    let totalRepayable (config: ScheduleConfig) =
        let totalFee = calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee

        match config.FeeTreatment with
        | NettedFromProceeds -> config.AdvanceAmount
        | AddedOnTop -> config.AdvanceAmount + totalFee

    /// the amount actually paid out to the borrower on the advance date: the advance less the fee when the fee
    /// is netted from the proceeds, or the full advance when the fee is added on top
    let netDisbursedAmount (config: ScheduleConfig) =
        let totalFee = calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee

        match config.FeeTreatment with
        | NettedFromProceeds -> config.AdvanceAmount - totalFee
        | AddedOnTop -> config.AdvanceAmount

    let private isStrictlyIncreasing dates =
        dates
        |> Array.pairwise
        |> Array.forall (fun (previousDate, nextDate) -> nextDate > previousDate)

    let private validateConfig config =
        let errors = ResizeArray<string>()

        if config.AdvanceAmount <= 0L<Cent> then
            errors.Add("Advance amount must be positive")

        if config.AdvanceAmount > maxSupportedAmount then
            errors.Add($"Advance amount must not exceed {maxSupportedAmount} cents")

        match config.Fee with
        | FlatFee amount when amount < 0L<Cent> ->
            errors.Add("Flat fee cannot be negative")
        | FlatFee amount when amount > maxSupportedAmount ->
            errors.Add($"Flat fee must not exceed {maxSupportedAmount} cents")
        | PercentageFee(Percent pct) when pct < 0m || pct >= 100m ->
            // a zero percentage fee is accepted, consistent with a zero flat fee being accepted
            errors.Add("Percentage fee must be at least 0% and less than 100%")
        | _ -> ()

        if errors.Count = 0 then
            match config.FeeTreatment with
            | NettedFromProceeds when
                calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee >= config.AdvanceAmount
                ->
                errors.Add("Fee must be less than the advance amount when netted from proceeds")
            | _ -> ()

        if isNull config.PayrollDates then
            errors.Add("Payroll dates array must not be null")
        else
            if config.PayrollDates.Length = 0 then
                errors.Add("Payroll dates required for this repayment mode")

            if config.PayrollDates.Length > 1 && not (isStrictlyIncreasing config.PayrollDates) then
                errors.Add("Payroll dates must be strictly increasing")

            if config.PayrollDates |> Array.exists (fun d -> d <= config.AdvanceDate) then
                errors.Add("All payroll dates must be after advance date")

            match config.RepaymentMode with
            | Custom amounts ->
                if List.length amounts <> config.PayrollDates.Length then
                    errors.Add(
                        $"Custom repayment amounts count ({List.length amounts}) must match payroll dates count ({config.PayrollDates.Length})"
                    )

                if amounts |> List.exists (fun amount -> amount <= 0L<Cent>) then
                    errors.Add("Custom repayment amounts must all be positive")

                if errors.Count = 0 then
                    // sum in decimal so that pathological inputs cannot silently overflow int64
                    let expectedTotal =
                        match config.FeeTreatment with
                        | NettedFromProceeds -> decimal config.AdvanceAmount
                        | AddedOnTop ->
                            decimal config.AdvanceAmount
                            + decimal (calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee)

                    let actualTotal = amounts |> List.sumBy decimal

                    if actualTotal <> expectedTotal then
                        errors.Add(
                            $"Custom repayment amounts must sum to the total repayable ({expectedTotal} cents) but sum to {actualTotal} cents"
                        )
            | LumpOnFirstPayroll
            | EvenlyProrated -> ()

        errors.ToArray()

    /// creates a repayment schedule based on the configuration
    let createSchedule (config: ScheduleConfig) : ScheduleItem array =
        let errors = validateConfig config

        if errors.Length > 0 then
            let errorText = String.concat "; " errors
            invalidArg "config" errorText

        let totalFee = calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee
        let totalAmount = totalRepayable config

        // the portion of the fee collected through the repayments: when the fee is netted from the proceeds it
        // is collected at disbursement instead, so none of it is carried by the repayments
        let feeInRepayments =
            match config.FeeTreatment with
            | NettedFromProceeds -> 0L<Cent>
            | AddedOnTop -> totalFee

        let paymentDates, paymentAmounts =
            match config.RepaymentMode with
            | LumpOnFirstPayroll -> [| Array.head config.PayrollDates |], [| totalAmount |]
            | EvenlyProrated -> config.PayrollDates, distributeEvenly totalAmount config.PayrollDates.Length
            | Custom amounts -> config.PayrollDates, List.toArray amounts

        let feeAmounts = allocateFeeByPaymentShare feeInRepayments paymentAmounts
        let mutable remainingBalance: int64<Cent> = totalAmount

        Array.map2
            (fun paymentDate (paymentAmount: int64<Cent>, feeAmount) ->
                remainingBalance <- remainingBalance - paymentAmount

                {
                    PaymentDate = paymentDate
                    RepaymentAmount = paymentAmount
                    RemainingBalance = remainingBalance
                    FeeAmount = feeAmount
                })
            paymentDates
            (Array.zip paymentAmounts feeAmounts)

    /// exports the schedule as cashflow items for analytical use
    let exportCashflows (config: ScheduleConfig) : CashflowItem array =
        let schedule = createSchedule config

        // Provider perspective: disbursement out, repayments in. The disbursement is the net amount actually
        // paid out, i.e. the advance less any fee netted from the proceeds.
        let netDisbursed = netDisbursedAmount config

        let advanceCashflow = {
            Date = config.AdvanceDate
            Amount = -netDisbursed
            Description =
                match config.FeeTreatment with
                | NettedFromProceeds ->
                    let totalFee = calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee
                    $"Salary advance disbursement (advance {formatCent config.AdvanceAmount} less fee {formatCent totalFee})"
                | AddedOnTop -> "Salary advance disbursement"
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

    /// summary statistics for a salary advance
    type Summary = {
        /// the gross advance amount
        AdvanceAmount: int64<Cent>
        /// the amount actually paid out to the borrower on the advance date (the advance less any fee netted from the proceeds)
        NetDisbursedAmount: int64<Cent>
        /// the total fee amount
        TotalFeeAmount: int64<Cent>
        /// the total amount repayable across all scheduled repayments
        TotalRepaymentAmount: int64<Cent>
        /// the number of days from the advance date to the final scheduled repayment
        TermInDays: int
        /// the number of scheduled repayments
        NumberOfPayments: int
        /// the total fee as a percentage of the advance amount over the whole term: note that this is a flat
        /// rate with no time dimension, so it must not be mistaken for an annual cost such as an APR
        EffectiveFeeRate: Percent
        /// the effective fee rate annualized on a simple basis (fee rate × 365 / term in days), useful for
        /// comparing advances of different terms; this is not compounded and is not a regulatory APR; None when
        /// the term is zero days
        AnnualizedEffectiveFeeRate: Percent option
    }

    /// calculates summary statistics for the salary advance
    let calculateSummary (config: ScheduleConfig) : Summary =
        let schedule = createSchedule config
        let totalFee = calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee

        let termInDays =
            if schedule.Length > 0 then
                (schedule |> Array.last).PaymentDate - config.AdvanceDate
                |> fun ts -> ts.Days
            else
                0

        let effectiveFeeRate =
            if config.AdvanceAmount > 0L<Cent> then
                decimal totalFee / decimal config.AdvanceAmount * 100m
            else
                0m

        {
            AdvanceAmount = config.AdvanceAmount
            NetDisbursedAmount = netDisbursedAmount config
            TotalFeeAmount = totalFee
            TotalRepaymentAmount = totalRepayable config
            TermInDays = termInDays
            NumberOfPayments = schedule.Length
            EffectiveFeeRate = Percent effectiveFeeRate
            AnnualizedEffectiveFeeRate =
                if termInDays > 0 then
                    Some(Percent(effectiveFeeRate * 365m / decimal termInDays))
                else
                    None
        }

    /// module for working with schedule configurations
    module ScheduleConfig =

        /// creates a basic schedule configuration with no fee, defaulting to the fee added on top of the amount
        /// repayable and percentage-based fees rounded to whole cents with midpoints away from zero
        let create advanceDate advanceAmount repaymentMode payrollDates =
            {
                AdvanceDate = advanceDate
                AdvanceAmount = advanceAmount
                RepaymentMode = repaymentMode
                Fee = NoFee
                FeeTreatment = AddedOnTop
                FeeRounding = RoundWith MidpointRounding.AwayFromZero
                PayrollDates = payrollDates
            }

        /// adds a fee to the schedule configuration
        let withFee fee config =
            { config with Fee = fee }

        /// sets how the fee is collected: netted from the disbursed proceeds or added on top of the amount repayable
        let withFeeTreatment feeTreatment config =
            { config with FeeTreatment = feeTreatment }

        /// sets how a percentage-based fee is rounded to whole cents
        let withFeeRounding feeRounding config =
            { config with FeeRounding = feeRounding }

        /// validates the schedule configuration
        let validate config =
            validateConfig config
