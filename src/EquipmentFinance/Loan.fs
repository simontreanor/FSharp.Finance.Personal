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
        /// The monthly payment amount (if known)
        MonthlyPayment: int64<Cent> option
        /// The equipment being financed
        EquipmentDescription: string
        /// The cost of the equipment
        EquipmentCost: int64<Cent>
        /// Down payment made on the equipment
        DownPayment: int64<Cent>
        /// Residual value at end of loan term
        ResidualValue: int64<Cent>
    }

    /// Loan payment calculation result
    type PaymentCalculation = {
        /// The calculated monthly payment
        MonthlyPayment: int64<Cent>
        /// Total payments over the loan term
        TotalPayments: int64<Cent>
        /// Total interest paid over the loan term
        TotalInterest: int64<Cent>
        /// The annual percentage rate
        Apr: Percent
    }

    /// Loan amortization schedule item
    type AmortizationItem = {
        /// Payment number (1-based)
        PaymentNumber: int
        /// Payment due date
        PaymentDate: DateDay.Date
        /// The payment amount
        PaymentAmount: int64<Cent>
        /// Principal portion of payment
        PrincipalPayment: int64<Cent>
        /// Interest portion of payment
        InterestPayment: int64<Cent>
        /// Remaining principal balance
        RemainingBalance: int64<Cent>
    }

    let private validateTerms (terms: EquipmentLoanTerms) =
        if terms.Principal <= 0L<Cent> then invalidArg (nameof terms.Principal) "Principal must be > 0."
        if terms.TermMonths <= 0 then invalidArg (nameof terms.TermMonths) "TermMonths must be > 0."
        if terms.EquipmentCost <= 0L<Cent> then invalidArg (nameof terms.EquipmentCost) "EquipmentCost must be > 0."
        if terms.DownPayment < 0L<Cent> then invalidArg (nameof terms.DownPayment) "DownPayment must be >= 0."
        if terms.ResidualValue < 0L<Cent> then invalidArg (nameof terms.ResidualValue) "ResidualValue must be >= 0."
        if terms.ResidualValue >= terms.Principal then invalidArg (nameof terms.ResidualValue) "ResidualValue must be less than Principal."

        match terms.MonthlyPayment with
        | Some payment when payment <= 0L<Cent> -> invalidArg (nameof terms.MonthlyPayment) "MonthlyPayment must be > 0 when provided."
        | _ -> ()

    let private monthlyRate (interestRate: Interest.Rate) =
        match interestRate with
        | Interest.Rate.Zero -> 0m
        | Interest.Rate.Annual (Percent rate) -> rate / 100m / 12m
        | Interest.Rate.Daily (Percent rate) -> rate / 100m * 365m / 12m

    let private buildScheduleCore (terms: EquipmentLoanTerms) =
        validateTerms terms

        let payment =
            match terms.MonthlyPayment with
            | Some monthlyPayment -> monthlyPayment
            | None ->
                let rate = monthlyRate terms.InterestRate
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

        let rate = monthlyRate terms.InterestRate

        let rec generateSchedule paymentNum balance acc =
            if paymentNum > terms.TermMonths then
                acc |> List.rev |> Array.ofList
            else
                let interestPayment = 
                    decimal balance * rate * 1m<Cent>
                    |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

                let paymentAmount, principalPayment =
                    if paymentNum = terms.TermMonths then
                        let finalPayment = balance + interestPayment
                        finalPayment, balance
                    else
                        let principal = payment - interestPayment
                        if principal <= 0L<Cent> then
                            invalidArg (nameof terms.MonthlyPayment) "Monthly payment must exceed periodic interest."
                        payment, principal

                let newBalance = balance - principalPayment
                generateSchedule (paymentNum + 1) newBalance ((paymentNum, paymentAmount, principalPayment, interestPayment, newBalance) :: acc)

        payment, generateSchedule 1 terms.Principal []

    /// Calculate monthly payment for an equipment loan
    let calculateMonthlyPayment (terms: EquipmentLoanTerms) : int64<Cent> =
        buildScheduleCore terms |> fst

    /// Calculate payment details for an equipment loan
    let calculatePaymentDetails (terms: EquipmentLoanTerms) : PaymentCalculation =
        let monthlyPayment, schedule = buildScheduleCore terms
        let totalPayments = schedule |> Array.sumBy (fun (_, paymentAmount, _, _, _) -> paymentAmount)
        let totalInterest = totalPayments - terms.Principal
        
        // Simplified APR calculation (actual APR would require iterative calculation)
        let annualRate = 
            match terms.InterestRate with
            | Interest.Rate.Zero -> Percent 0m
            | Interest.Rate.Annual percent -> percent
            | Interest.Rate.Daily (Percent rate) -> Percent (rate * 365m)
        
        {
            MonthlyPayment = monthlyPayment
            TotalPayments = totalPayments
            TotalInterest = totalInterest
            Apr = annualRate
        }

    /// Generate loan amortization schedule
    let generateAmortizationSchedule (terms: EquipmentLoanTerms) (startDate: DateDay.Date) : AmortizationItem array =
        let _, schedule = buildScheduleCore terms

        schedule
        |> Array.map (fun (paymentNum, paymentAmount, principalPayment, interestPayment, remainingBalance) ->
            {
                PaymentNumber = paymentNum
                PaymentDate = startDate.AddMonths(paymentNum)
                PaymentAmount = paymentAmount
                PrincipalPayment = principalPayment
                InterestPayment = interestPayment
                RemainingBalance = remainingBalance
            })

    /// Analyze equipment loan with depreciation considerations
    type LoanAnalysis = {
        /// Loan payment calculation
        PaymentDetails: PaymentCalculation
        /// Depreciation schedule for the equipment
        DepreciationSchedule: Types.DepreciationYear list
        /// Loan amortization schedule
        AmortizationSchedule: AmortizationItem array
        /// Net present value analysis would go here in a full implementation
        NetPresentValue: int64<Cent> option
    }

    /// Perform comprehensive analysis of an equipment loan
    let analyzeLoan (terms: EquipmentLoanTerms) (startDate: DateDay.Date) : LoanAnalysis =
        let paymentDetails = calculatePaymentDetails terms
        let amortizationSchedule = generateAmortizationSchedule terms startDate
        
        // Create MACRS asset for depreciation analysis
        let macrsAsset = {
            Types.CostBasis = terms.EquipmentCost
            Types.PlacedInServiceDate = startDate
            Types.PropertyClass = Calculations.classifyAsset terms.EquipmentDescription
            Types.Convention = Types.Convention.HalfYear
        }
        
        let depreciationSchedule = Calculations.generateSchedule macrsAsset
        
        {
            PaymentDetails = paymentDetails
            DepreciationSchedule = depreciationSchedule
            AmortizationSchedule = amortizationSchedule
            NetPresentValue = None // Would implement NPV calculation in full version
        }
