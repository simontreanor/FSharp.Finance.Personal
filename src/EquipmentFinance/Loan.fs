namespace FSharp.Finance.Personal.EquipmentFinance

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS

/// Equipment loan calculations and analysis for equipment finance
module Loan =

    /// Simple power function for financial calculations
    let private pow (base': decimal) (power: decimal) =
        decimal (Math.Pow(double base', double power))

    /// Terms and conditions for an equipment loan
    type EquipmentLoanTerms = {
        /// The financed principal amount of the loan, net of any down payment
        Principal: int64<Cent>
        /// The annual interest rate
        InterestRate: Interest.Rate
        /// The loan term in months
        TermMonths: int
        /// The monthly payment amount excluding any MonthlyFee (if known)
        MonthlyPayment: int64<Cent> option
        /// Optional recurring fee (e.g. service or administration fee) charged on top of
        /// every payment; shown separately as FeePayment in the schedule
        MonthlyFee: int64<Cent> option
        /// The equipment being financed
        EquipmentDescription: string
        /// The cost of the equipment
        EquipmentCost: int64<Cent>
        /// Down payment made on the equipment
        DownPayment: int64<Cent>
        /// Residual value at end of loan term, payable as a balloon with the final payment.
        /// May equal Principal for an interest-only balloon structure.
        ResidualValue: int64<Cent>
    }

    /// Loan payment calculation result
    type PaymentCalculation = {
        /// The calculated monthly payment (principal + interest, excluding any MonthlyFee)
        MonthlyPayment: int64<Cent>
        /// Total payments over the loan term, including any recurring fees
        TotalPayments: int64<Cent>
        /// Total interest paid over the loan term
        TotalInterest: int64<Cent>
        /// Total recurring fees paid over the loan term
        TotalFees: int64<Cent>
        /// The nominal annual interest rate used for the schedule.
        /// Note: this is NOT an effective APR; no compounding or fee effects are included.
        NominalAnnualRate: Percent
    }

    /// Loan amortization schedule item
    type AmortizationItem = {
        /// Payment number (1-based)
        PaymentNumber: int
        /// Payment due date
        PaymentDate: DateDay.Date
        /// The payment amount (principal + interest + fee, including any balloon in the final payment)
        PaymentAmount: int64<Cent>
        /// Principal portion of payment (includes the balloon amount in the final payment)
        PrincipalPayment: int64<Cent>
        /// Interest portion of payment
        InterestPayment: int64<Cent>
        /// Recurring fee portion of payment
        FeePayment: int64<Cent>
        /// Remaining principal balance (includes the residual until the final payment clears it)
        RemainingBalance: int64<Cent>
        /// The residual (balloon) amount included in this payment. Zero except in the final
        /// payment of a loan with a residual value, where it distinguishes the balloon from
        /// ordinary amortization and rounding drift.
        BalloonAmount: int64<Cent>
    }

    let private validateTerms (terms: EquipmentLoanTerms) =
        if terms.Principal <= 0L<Cent> then invalidArg (nameof terms.Principal) "Principal must be > 0."
        if terms.TermMonths <= 0 then invalidArg (nameof terms.TermMonths) "TermMonths must be > 0."
        if terms.EquipmentCost <= 0L<Cent> then invalidArg (nameof terms.EquipmentCost) "EquipmentCost must be > 0."
        if terms.DownPayment < 0L<Cent> then invalidArg (nameof terms.DownPayment) "DownPayment must be >= 0."
        if terms.ResidualValue < 0L<Cent> then invalidArg (nameof terms.ResidualValue) "ResidualValue must be >= 0."
        // ResidualValue = Principal is a legitimate interest-only balloon structure
        if terms.ResidualValue > terms.Principal then invalidArg (nameof terms.ResidualValue) "ResidualValue must not exceed Principal."

        match terms.MonthlyPayment with
        | Some payment when payment <= 0L<Cent> -> invalidArg (nameof terms.MonthlyPayment) "MonthlyPayment must be > 0 when provided."
        | _ -> ()

        match terms.MonthlyFee with
        | Some fee when fee < 0L<Cent> -> invalidArg (nameof terms.MonthlyFee) "MonthlyFee must be >= 0 when provided."
        | _ -> ()

    let private monthlyRate (interestRate: Interest.Rate) =
        match interestRate with
        | Interest.Rate.Zero -> 0m
        | Interest.Rate.Annual (Percent rate) -> rate / 100m / 12m
        | Interest.Rate.Daily (Percent rate) -> rate / 100m * 365m / 12m

    let private buildScheduleCore (terms: EquipmentLoanTerms) =
        validateTerms terms

        let rate = monthlyRate terms.InterestRate

        let payment =
            match terms.MonthlyPayment with
            | Some monthlyPayment ->
                // reject negative amortization: the payment must cover at least the first-period
                // interest, which is the largest interest charge of the schedule
                let firstInterest =
                    decimal terms.Principal * rate * 1m<Cent>
                    |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
                if monthlyPayment < firstInterest then
                    invalidArg (nameof terms.MonthlyPayment)
                        $"MonthlyPayment must cover at least the first-period interest ({Cent.toDecimal firstInterest:N2}) to avoid negative amortization."
                monthlyPayment
            | None ->
                if rate = 0m then
                    let amortizedPrincipal = Cent.toDecimal (terms.Principal - terms.ResidualValue)
                    Cent.fromDecimal (amortizedPrincipal / decimal terms.TermMonths)
                else
                    let growthFactor = pow (1m + rate) (decimal terms.TermMonths)
                    let residualPresentValue = decimal terms.ResidualValue / growthFactor
                    let amortizedPrincipal = decimal terms.Principal - residualPresentValue
                    let numerator = amortizedPrincipal * rate * growthFactor
                    let denominator = growthFactor - 1m
                    let installment = numerator / denominator
                    installment * 1m<Cent> |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

        let fee = terms.MonthlyFee |> Option.defaultValue 0L<Cent>

        // The balance includes the residual until the final payment clears it as a balloon.
        // Per-period principal is clamped to the remaining amortizable balance (balance - residual);
        // if a supplied payment overpays, the schedule ends early rather than emitting
        // zero or negative rows.
        let rec generateSchedule paymentNum balance acc =
            let interestPayment =
                decimal balance * rate * 1m<Cent>
                |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

            let amortizable = balance - terms.ResidualValue
            let scheduledPrincipal = payment - interestPayment

            let isFinal =
                paymentNum = terms.TermMonths
                || (amortizable > 0L<Cent> && scheduledPrincipal >= amortizable)

            if isFinal then
                // final payment clears the remaining amortizable balance plus the residual balloon
                let principalPayment = balance
                let paymentAmount = interestPayment + principalPayment + fee
                let item = (paymentNum, paymentAmount, principalPayment, interestPayment, fee, 0L<Cent>, terms.ResidualValue)
                item :: acc |> List.rev |> Array.ofList
            else
                let principalPayment = max 0L<Cent> (min scheduledPrincipal amortizable)
                let newBalance = balance - principalPayment
                let paymentAmount = interestPayment + principalPayment + fee
                generateSchedule (paymentNum + 1) newBalance ((paymentNum, paymentAmount, principalPayment, interestPayment, fee, newBalance, 0L<Cent>) :: acc)

        payment, generateSchedule 1 terms.Principal []

    /// Calculate monthly payment for an equipment loan
    let calculateMonthlyPayment (terms: EquipmentLoanTerms) : int64<Cent> =
        buildScheduleCore terms |> fst

    /// Calculate payment details for an equipment loan
    let calculatePaymentDetails (terms: EquipmentLoanTerms) : PaymentCalculation =
        let monthlyPayment, schedule = buildScheduleCore terms
        let totalPayments = schedule |> Array.sumBy (fun (_, paymentAmount, _, _, _, _, _) -> paymentAmount)
        let totalInterest = schedule |> Array.sumBy (fun (_, _, _, interestPayment, _, _, _) -> interestPayment)
        let totalFees = schedule |> Array.sumBy (fun (_, _, _, _, feePayment, _, _) -> feePayment)

        let annualRate =
            match terms.InterestRate with
            | Interest.Rate.Zero -> Percent 0m
            | Interest.Rate.Annual percent -> percent
            | Interest.Rate.Daily (Percent rate) -> Percent (rate * 365m)

        {
            MonthlyPayment = monthlyPayment
            TotalPayments = totalPayments
            TotalInterest = totalInterest
            TotalFees = totalFees
            NominalAnnualRate = annualRate
        }

    /// Generate loan amortization schedule.
    /// Note: if a supplied MonthlyPayment overpays, the schedule terminates early
    /// (fewer periods than the nominal term) once the balance net of the residual is repaid.
    let generateAmortizationSchedule (terms: EquipmentLoanTerms) (startDate: DateDay.Date) : AmortizationItem array =
        let _, schedule = buildScheduleCore terms

        schedule
        |> Array.map (fun (paymentNum, paymentAmount, principalPayment, interestPayment, feePayment, remainingBalance, balloonAmount) ->
            {
                PaymentNumber = paymentNum
                PaymentDate = startDate.AddMonths(paymentNum)
                PaymentAmount = paymentAmount
                PrincipalPayment = principalPayment
                InterestPayment = interestPayment
                FeePayment = feePayment
                RemainingBalance = remainingBalance
                BalloonAmount = balloonAmount
            })

    /// Analyze equipment loan with depreciation considerations
    type LoanAnalysis = {
        /// Loan payment calculation
        PaymentDetails: PaymentCalculation
        /// Depreciation schedule for the equipment
        DepreciationSchedule: Types.DepreciationYear list
        /// Loan amortization schedule
        AmortizationSchedule: AmortizationItem array
        /// Present value of all loan cash outflows (down payment plus every scheduled
        /// payment, including any balloon and fees) discounted monthly at the supplied
        /// annual discount rate. A simple pre-tax NPV; no tax effects are modelled.
        NetPresentValue: int64<Cent>
        /// True when the equipment description did not match any known asset-class keyword
        /// and the default FiveYear MACRS class was assumed for the depreciation schedule
        AssetClassAssumed: bool
    }

    /// Perform comprehensive analysis of an equipment loan.
    /// annualDiscountRate is the rate used to discount the loan cash flows for NetPresentValue.
    let analyzeLoan (terms: EquipmentLoanTerms) (startDate: DateDay.Date) (annualDiscountRate: Percent) : LoanAnalysis =
        let paymentDetails = calculatePaymentDetails terms
        let amortizationSchedule = generateAmortizationSchedule terms startDate

        let monthlyDiscount = Percent.toDecimal annualDiscountRate / 12m
        let netPresentValue =
            let paymentsPv =
                amortizationSchedule
                |> Array.sumBy (fun item -> decimal item.PaymentAmount / pow (1m + monthlyDiscount) (decimal item.PaymentNumber))
            (decimal terms.DownPayment + paymentsPv) * 1m<Cent>
            |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

        // Create MACRS asset for depreciation analysis; if the description is not recognized,
        // FiveYear is assumed and the assumption is surfaced via AssetClassAssumed
        let classified = Calculations.tryClassifyAsset terms.EquipmentDescription
        let macrsAsset = {
            Types.CostBasis = terms.EquipmentCost
            Types.PlacedInServiceDate = startDate
            Types.PropertyClass = classified |> Option.defaultValue Types.AssetClass.FiveYear
            Types.Convention = Types.Convention.HalfYear
        }

        let depreciationSchedule = Calculations.generateSchedule macrsAsset

        {
            PaymentDetails = paymentDetails
            DepreciationSchedule = depreciationSchedule
            AmortizationSchedule = amortizationSchedule
            NetPresentValue = netPresentValue
            AssetClassAssumed = classified.IsNone
        }
