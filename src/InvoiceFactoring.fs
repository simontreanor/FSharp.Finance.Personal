namespace FSharp.Finance.B2B
open FSharp.Finance.Personal

/// Invoice factoring parameter construction and modeling utilities
module InvoiceFactoring =

    open DateDay
    open Calculation
    open Scheduling
    open Interest
    open Apr

    /// An invoice for factoring analysis
    type Invoice = {
        /// Unique identifier for the invoice
        Id: string
        /// Face value of the invoice (amount owed by debtor)
        FaceValue: int64<Cent>
        /// Date the invoice was issued
        IssueDate: Date
        /// Date payment is due from the debtor
        DueDate: Date
        /// Debtor/customer identifier
        DebtorId: string
    }

    /// An invoice advance from a factoring arrangement
    type InvoiceAdvance = {
        /// Reference to the original invoice
        Invoice: Invoice
        /// Advance rate as a percentage of face value (e.g., 80% = 0.80m)
        AdvanceRate: decimal
        /// Factoring fee rate as a percentage of face value (e.g., 2% = 0.02m)
        FeeRate: decimal
        /// Reserve rate as a percentage of face value (remaining after advance and fee)
        ReserveRate: decimal
        /// Net advance amount (face value * advance rate - upfront fees)
        NetAdvance: int64<Cent>
        /// Upfront fee amount
        UpfrontFee: int64<Cent>
        /// Reserve amount held back
        ReserveAmount: int64<Cent>
    }

    /// Invoice helper functions
    module Invoice =
        
        /// Create a new invoice
        let create id faceValue issueDate dueDate debtorId =
            {
                Id = id
                FaceValue = faceValue
                IssueDate = issueDate
                DueDate = dueDate
                DebtorId = debtorId
            }

        /// Calculate the credit period in days for an invoice
        let creditPeriodDays (invoice: Invoice) =
            Apr.UsActuarial.daysBetween invoice.IssueDate invoice.DueDate

    /// Invoice advance helper functions
    module InvoiceAdvance =
        
        /// Derive an invoice advance from an invoice and factoring terms
        let derive (invoice: Invoice) (advanceRate: decimal) (feeRate: decimal) =
            if advanceRate < 0m || advanceRate > 1m then
                invalidArg (nameof advanceRate) "Advance rate must be between 0 and 1"
            if feeRate < 0m || feeRate > 1m then
                invalidArg (nameof feeRate) "Fee rate must be between 0 and 1"
            if advanceRate + feeRate > 1m then
                invalidArg "rates" "Advance rate plus fee rate cannot exceed 100%"

            let faceValueDecimal = Cent.toDecimal invoice.FaceValue
            let reserveRate = 1m - advanceRate - feeRate
            let upfrontFee = Cent.fromDecimal (faceValueDecimal * feeRate)
            let grossAdvance = Cent.fromDecimal (faceValueDecimal * advanceRate)
            let netAdvance = grossAdvance - upfrontFee
            let reserveAmount = Cent.fromDecimal (faceValueDecimal * reserveRate)

            {
                Invoice = invoice
                AdvanceRate = advanceRate
                FeeRate = feeRate
                ReserveRate = reserveRate
                NetAdvance = netAdvance
                UpfrontFee = upfrontFee
                ReserveAmount = reserveAmount
            }

    /// Factoring parameters builder for creating amortization parameters
    module FactoringParameters =
        
        /// Build Parameters for a set of invoice advances using the library's core types
        let build (advances: InvoiceAdvance array) (interestConfig: Interest.BasicConfig option) : Parameters =
            if Array.isEmpty advances then
                invalidArg (nameof advances) "At least one invoice advance is required"

            // Find the earliest issue date as the start date
            let startDate = 
                advances 
                |> Array.map (fun a -> a.Invoice.IssueDate)
                |> Array.min

            // Calculate total principal as sum of net advances
            let principal = 
                advances
                |> Array.sumBy (_.NetAdvance)

            // Create scheduled payments - one per invoice at its due date for full face value
            let scheduledPaymentMap =
                advances
                |> Array.map (fun advance ->
                    let offsetDay = OffsetDay.fromDate startDate advance.Invoice.DueDate
                    let scheduledPayment = ScheduledPayment.quick (ValueSome advance.Invoice.FaceValue) ValueNone
                    offsetDay, scheduledPayment
                )
                |> Map.ofArray

            // Create basic parameters
            let basicParams = {
                StartDate = startDate
                Principal = principal
                EvaluationDate = startDate // Default evaluation date to start date
                ScheduleConfig = CustomSchedule scheduledPaymentMap
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment // Use lowest option as default
                    Rounding = RoundDown
                }
                FeeConfig = ValueNone // Fees are handled as upfront deductions in net advance calculation
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Zero // Default to zero, can be overridden
                    Cap = Interest.Cap.zero
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 2 // Placeholder APR method
                } |> fun defaultConfig ->
                    interestConfig |> Option.defaultValue defaultConfig
            }

            // Create advanced parameters
            let advancedParams = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = NoMinimumPayment
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 0<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = true
            }

            // Return the complete parameters
            {
                Basic = basicParams
                Advanced = advancedParams
            }

        /// Calculate aggregate factoring statistics for a set of advances
        let calculateStatistics (advances: InvoiceAdvance array) =
            if Array.isEmpty advances then
                {| 
                    TotalFaceValue = 0L<Cent>
                    TotalNetAdvance = 0L<Cent>
                    TotalUpfrontFees = 0L<Cent>
                    TotalReserve = 0L<Cent>
                    WeightedAverageAdvanceRate = 0m
                    WeightedAverageFeeRate = 0m
                    AverageCreditPeriodDays = 0
                |}
            else
                let totalFaceValue = advances |> Array.sumBy (fun a -> a.Invoice.FaceValue)
                let totalNetAdvance = advances |> Array.sumBy (_.NetAdvance)
                let totalUpfrontFees = advances |> Array.sumBy (_.UpfrontFee)
                let totalReserve = advances |> Array.sumBy (_.ReserveAmount)
                
                let totalFaceValueDecimal = Cent.toDecimal totalFaceValue
                let weightedAvgAdvanceRate = 
                    if totalFaceValueDecimal = 0m then 0m
                    else (advances |> Array.sumBy (fun a -> Cent.toDecimal a.Invoice.FaceValue * a.AdvanceRate)) / totalFaceValueDecimal
                
                let weightedAvgFeeRate = 
                    if totalFaceValueDecimal = 0m then 0m
                    else (advances |> Array.sumBy (fun a -> Cent.toDecimal a.Invoice.FaceValue * a.FeeRate)) / totalFaceValueDecimal
                
                let avgCreditPeriod = 
                    if Array.isEmpty advances then 0
                    else 
                        advances 
                        |> Array.map (fun a -> Invoice.creditPeriodDays a.Invoice)
                        |> Array.averageBy float
                        |> int

                {| 
                    TotalFaceValue = totalFaceValue
                    TotalNetAdvance = totalNetAdvance
                    TotalUpfrontFees = totalUpfrontFees
                    TotalReserve = totalReserve
                    WeightedAverageAdvanceRate = weightedAvgAdvanceRate
                    WeightedAverageFeeRate = weightedAvgFeeRate
                    AverageCreditPeriodDays = avgCreditPeriod
                |}