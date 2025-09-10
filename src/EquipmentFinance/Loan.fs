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
        /// The principal amount of the loan
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

    /// Calculate monthly payment for an equipment loan
    let calculateMonthlyPayment (terms: EquipmentLoanTerms) : int64<Cent> =
        match terms.MonthlyPayment with
        | Some payment -> payment
        | None ->
            let monthlyRate = 
                match terms.InterestRate with
                | Interest.Rate.Zero -> 0m
                | Interest.Rate.Annual (Percent rate) -> rate / 100m / 12m
                | Interest.Rate.Daily (Percent rate) -> rate / 100m * 365m / 12m
            
            if monthlyRate = 0m then
                // No interest, just divide principal by term
                terms.Principal / int64 terms.TermMonths
            else
                let loanAmount = terms.Principal - terms.ResidualValue
                let numerator = decimal loanAmount * monthlyRate * pow (1m + monthlyRate) (decimal terms.TermMonths)
                let denominator = pow (1m + monthlyRate) (decimal terms.TermMonths) - 1m
                let payment = numerator / denominator
                payment * 1m<Cent> |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

    /// Calculate payment details for an equipment loan
    let calculatePaymentDetails (terms: EquipmentLoanTerms) : PaymentCalculation =
        let monthlyPayment = calculateMonthlyPayment terms
        let totalPayments = monthlyPayment * int64 terms.TermMonths + terms.ResidualValue
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
        let monthlyPayment = calculateMonthlyPayment terms
        let monthlyRate = 
            match terms.InterestRate with
            | Interest.Rate.Zero -> 0m
            | Interest.Rate.Annual (Percent rate) -> rate / 100m / 12m
            | Interest.Rate.Daily (Percent rate) -> rate / 100m * 365m / 12m
        
        let rec generateSchedule paymentNum currentDate balance acc =
            if paymentNum > terms.TermMonths then
                acc |> List.rev |> Array.ofList
            else
                let interestPayment = 
                    decimal balance * monthlyRate * 1m<Cent>
                    |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)
                
                let principalPayment = 
                    if paymentNum = terms.TermMonths then
                        // Final payment: pay remaining balance minus residual
                        balance - terms.ResidualValue
                    else
                        monthlyPayment - interestPayment
                
                let newBalance = balance - principalPayment
                let paymentDate = startDate.AddMonths(paymentNum)
                
                let item = {
                    PaymentNumber = paymentNum
                    PaymentDate = paymentDate
                    PaymentAmount = if paymentNum = terms.TermMonths then principalPayment + interestPayment else monthlyPayment
                    PrincipalPayment = principalPayment
                    InterestPayment = interestPayment
                    RemainingBalance = newBalance
                }
                
                generateSchedule (paymentNum + 1) paymentDate newBalance (item :: acc)
        
        generateSchedule 1 startDate terms.Principal []

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