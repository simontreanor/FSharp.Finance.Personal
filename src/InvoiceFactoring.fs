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
    ///
    /// The face value decomposes exactly: NetAdvance + UpfrontFee + ReserveAmount = FaceValue
    type InvoiceAdvance = {
        /// Reference to the original invoice
        Invoice: Invoice
        /// Advance rate as a percentage of face value (e.g., 80% = 0.80m)
        AdvanceRate: decimal
        /// Factoring fee rate as a percentage of face value (e.g., 2% = 0.02m)
        FeeRate: decimal
        /// Reserve rate as a percentage of face value (1 - advance rate)
        ReserveRate: decimal
        /// Gross advance amount (face value * advance rate) before the upfront fee is deducted
        GrossAdvance: int64<Cent>
        /// Net advance amount paid out to the seller (gross advance - upfront fee)
        NetAdvance: int64<Cent>
        /// Upfront fee amount, deducted from the gross advance
        UpfrontFee: int64<Cent>
        /// Reserve amount held back (face value - gross advance), rebated when the debtor pays
        ReserveAmount: int64<Cent>
    }

    /// Invoice helper functions
    module Invoice =
        
        /// Create a new invoice
        let create id (faceValue: int64<Cent>) (issueDate: Date) (dueDate: Date) debtorId =
            if faceValue <= 0L<Cent> then
                invalidArg (nameof faceValue) "Face value must be positive"

            if dueDate < issueDate then
                invalidArg (nameof dueDate) "Due date must not be before the issue date"

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
        ///
        /// The gross advance is the advance rate applied to the face value, the upfront fee is deducted from the
        /// gross advance to give the net advance paid out, and the remainder of the face value is held in reserve,
        /// so NetAdvance + UpfrontFee + ReserveAmount = FaceValue exactly
        let derive (invoice: Invoice) (advanceRate: decimal) (feeRate: decimal) =
            if advanceRate < 0m || advanceRate > 1m then
                invalidArg (nameof advanceRate) "Advance rate must be between 0 and 1"
            if feeRate < 0m || feeRate > 1m then
                invalidArg (nameof feeRate) "Fee rate must be between 0 and 1"
            if feeRate > advanceRate then
                invalidArg (nameof feeRate) "Fee rate cannot exceed advance rate, otherwise the net advance would be negative"

            // round the gross advance and fee once each, then derive the net advance and reserve by subtraction
            // so that the face-value decomposition holds exactly to the cent
            let faceValueDecimalCent = Cent.toDecimalCent invoice.FaceValue
            let rounding = RoundWith System.MidpointRounding.AwayFromZero
            let grossAdvance = faceValueDecimalCent * advanceRate |> Cent.fromDecimalCent rounding
            let upfrontFee = faceValueDecimalCent * feeRate |> Cent.fromDecimalCent rounding
            let netAdvance = grossAdvance - upfrontFee
            let reserveAmount = invoice.FaceValue - grossAdvance

            {
                Invoice = invoice
                AdvanceRate = advanceRate
                FeeRate = feeRate
                ReserveRate = 1m - advanceRate
                GrossAdvance = grossAdvance
                NetAdvance = netAdvance
                UpfrontFee = upfrontFee
                ReserveAmount = reserveAmount
            }

    /// Aggregate statistics for a set of invoice advances
    type FactoringStatistics = {
        /// Total face value of all invoices
        TotalFaceValue: int64<Cent>
        /// Total net advance paid out across all advances
        TotalNetAdvance: int64<Cent>
        /// Total upfront fees across all advances
        TotalUpfrontFees: int64<Cent>
        /// Total reserve held back across all advances
        TotalReserve: int64<Cent>
        /// Advance rate weighted by invoice face value
        WeightedAverageAdvanceRate: decimal
        /// Fee rate weighted by invoice face value
        WeightedAverageFeeRate: decimal
        /// Average credit period across the invoices, in days
        AverageCreditPeriodDays: decimal
    }

    /// Factoring parameters builder for creating amortization parameters
    module FactoringParameters =
        
        /// Build Parameters for a set of invoice advances using the library's core types
        ///
        /// The parameters model the seller's position in the factoring facility: the principal is the total net
        /// advance paid out, the upfront fees are carried as a fixed fee (so the opening balance is the total gross
        /// advance), and each due date schedules a payment of the gross advances due that day (the debtor pays the
        /// face value to the factor, who retains the gross advance plus fee and rebates the reserve to the seller);
        /// with the default zero interest rate the schedule closes with a zero principal balance
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

            // Total upfront fees, carried via the engine's fee config so they amortise alongside the principal
            let feeTotal =
                advances
                |> Array.sumBy (_.UpfrontFee)

            // Create scheduled payments - one per due date for the gross advances (net advances plus fees) due that day
            let scheduledPaymentMap =
                advances
                |> Array.groupBy (fun advance -> OffsetDay.fromDate startDate advance.Invoice.DueDate)
                |> Array.map (fun (offsetDay, sameDayAdvances) ->
                    let totalGrossAdvance =
                        sameDayAdvances
                        |> Array.sumBy (_.GrossAdvance)

                    let scheduledPayment = ScheduledPayment.quick (ValueSome totalGrossAdvance) ValueNone
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
                FeeConfig =
                    if feeTotal = 0L<Cent> then
                        ValueNone
                    else
                        ValueSome {
                            Fee.FeeType = Fee.CustomFee("factoring fee", Amount.Simple feeTotal)
                            Fee.Rounding = RoundDown
                            Fee.FeeAmortisation = Fee.AmortiseProportionately
                        }
                InterestConfig = {
                    Method = Interest.Method.Actuarial
                    StandardRate = Interest.Rate.Zero // Default to zero, can be overridden
                    Cap = Interest.Cap.zero
                    Rounding = RoundDown
                    AprMethod = Apr.CalculationMethod.UnitedKingdom 3 // UK FCA method to 1 d.p., consistent with the rest of the library; indicative only for factoring
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
                FeeConfig =
                    if feeTotal = 0L<Cent> then
                        ValueNone
                    else
                        ValueSome { Fee.SettlementRebate = Fee.SettlementRebate.Zero }
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
        let calculateStatistics (advances: InvoiceAdvance array) : FactoringStatistics =
            if Array.isEmpty advances then
                {
                    TotalFaceValue = 0L<Cent>
                    TotalNetAdvance = 0L<Cent>
                    TotalUpfrontFees = 0L<Cent>
                    TotalReserve = 0L<Cent>
                    WeightedAverageAdvanceRate = 0m
                    WeightedAverageFeeRate = 0m
                    AverageCreditPeriodDays = 0m
                }
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
                    advances
                    |> Array.averageBy (fun a -> decimal (Invoice.creditPeriodDays a.Invoice))

                {
                    TotalFaceValue = totalFaceValue
                    TotalNetAdvance = totalNetAdvance
                    TotalUpfrontFees = totalUpfrontFees
                    TotalReserve = totalReserve
                    WeightedAverageAdvanceRate = weightedAvgAdvanceRate
                    WeightedAverageFeeRate = weightedAvgFeeRate
                    AverageCreditPeriodDays = avgCreditPeriod
                }
