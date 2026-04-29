namespace FSharp.Finance.Personal.EquipmentFinance

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS

/// Equipment lease calculations and analysis for equipment finance
module Lease =

    /// Simple power function for financial calculations
    let private pow (base': decimal) (power: decimal) = 
        decimal (Math.Pow(double base', double power))

    /// Type of lease for equipment financing
    [<Struct; RequireQualifiedAccess>]
    type LeaseType =
        | OperatingLease    // Off-balance-sheet, lessor retains ownership
        | FinanceLease      // On-balance-sheet, lessee assumes ownership risks
        | CapitalLease      // Similar to finance lease under older accounting standards

    /// Lease payment frequency
    [<Struct; RequireQualifiedAccess>]
    type PaymentFrequency =
        | Monthly           // Monthly payments
        | Quarterly         // Quarterly payments
        | SemiAnnual        // Semi-annual payments
        | Annual            // Annual payments

        /// Convert frequency to number of payments per year
        member pf.PaymentsPerYear =
            match pf with
            | Monthly -> 12
            | Quarterly -> 4
            | SemiAnnual -> 2
            | Annual -> 1

    /// Terms and conditions for an equipment lease
    type EquipmentLeaseTerms = {
        /// The equipment being leased
        EquipmentDescription: string
        /// The fair market value of the equipment
        FairMarketValue: int64<Cent>
        /// The lease term in months
        TermMonths: int
        /// The type of lease
        LeaseType: LeaseType
        /// Payment frequency
        PaymentFrequency: PaymentFrequency
        /// Lease payment amount
        LeasePayment: int64<Cent>
        /// Any upfront payment or security deposit
        UpfrontPayment: int64<Cent>
        /// Residual value at end of lease term
        ResidualValue: int64<Cent>
        /// Purchase option at lease end
        PurchaseOption: int64<Cent> option
        /// Implicit interest rate in the lease
        ImplicitRate: Interest.Rate
    }

    /// Lease payment calculation result
    type LeaseCalculation = {
        /// The periodic lease payment
        LeasePayment: int64<Cent>
        /// Total payments over the lease term
        TotalPayments: int64<Cent>
        /// Total cost of leasing vs buying
        TotalCost: int64<Cent>
        /// The annual percentage rate equivalent
        AprEquivalent: Percent
        /// Present value of lease payments
        PresentValue: int64<Cent>
    }

    /// Lease schedule item
    type LeaseScheduleItem = {
        /// Payment number (1-based)
        PaymentNumber: int
        /// Payment due date
        PaymentDate: DateDay.Date
        /// The lease payment amount
        PaymentAmount: int64<Cent>
        /// Principal portion (for finance leases)
        PrincipalPortion: int64<Cent>
        /// Interest portion (for finance leases)
        InterestPortion: int64<Cent>
        /// Remaining lease liability (for finance leases)
        RemainingLiability: int64<Cent>
    }

    let private getScheduleShape (terms: EquipmentLeaseTerms) =
        let periodsPerYear = terms.PaymentFrequency.PaymentsPerYear
        if periodsPerYear <= 0 || 12 % periodsPerYear <> 0 then
            invalidArg (nameof terms.PaymentFrequency) "Payment frequency must correspond to a whole number of months per payment period."

        let monthsPerPeriod = 12 / periodsPerYear
        if terms.TermMonths <= 0 then invalidArg (nameof terms.TermMonths) "TermMonths must be > 0."
        if terms.TermMonths % monthsPerPeriod <> 0 then
            invalidArg (nameof terms.TermMonths) "TermMonths must be an exact multiple of the selected payment interval."

        periodsPerYear, monthsPerPeriod, terms.TermMonths / monthsPerPeriod

    let private validateTerms (terms: EquipmentLeaseTerms) =
        let _, _, _ = getScheduleShape terms
        if terms.FairMarketValue <= 0L<Cent> then invalidArg (nameof terms.FairMarketValue) "FairMarketValue must be > 0."
        if terms.UpfrontPayment < 0L<Cent> then invalidArg (nameof terms.UpfrontPayment) "UpfrontPayment must be >= 0."
        if terms.UpfrontPayment >= terms.FairMarketValue then invalidArg (nameof terms.UpfrontPayment) "UpfrontPayment must be < FairMarketValue."
        if terms.ResidualValue < 0L<Cent> then invalidArg (nameof terms.ResidualValue) "ResidualValue must be >= 0."
        if terms.ResidualValue >= terms.FairMarketValue then invalidArg (nameof terms.ResidualValue) "ResidualValue must be < FairMarketValue."
        if terms.LeasePayment < 0L<Cent> then invalidArg (nameof terms.LeasePayment) "LeasePayment must be >= 0."

    let private periodRate (terms: EquipmentLeaseTerms) (periodsPerYear: int) =
        match terms.ImplicitRate with
        | Interest.Rate.Zero -> 0m
        | Interest.Rate.Annual (Percent rate) -> rate / 100m / decimal periodsPerYear
        | Interest.Rate.Daily (Percent rate) -> rate / 100m * 365m / decimal periodsPerYear

    let private buildScheduleCore (terms: EquipmentLeaseTerms) =
        validateTerms terms

        let periodsPerYear, monthsPerPeriod, totalPeriods = getScheduleShape terms
        let rate = periodRate terms periodsPerYear

        let leasePayment =
            if terms.LeasePayment > 0L<Cent> then terms.LeasePayment
            else
                let fairValue = Cent.toDecimal terms.FairMarketValue
                let upfront = Cent.toDecimal terms.UpfrontPayment
                let residual = Cent.toDecimal terms.ResidualValue
                let financedPrincipal = fairValue - upfront
                if financedPrincipal <= 0m then invalidArg "terms.UpfrontPayment" "Upfront payment >= fair value."
                if residual >= fairValue then invalidArg "terms.ResidualValue" "Residual must be < fair value."

                if rate = 0m then
                    let paymentDec = (financedPrincipal - residual) / decimal totalPeriods
                    if paymentDec <= 0m then invalidOp "Non-positive payment under zero-rate scenario."
                    Cent.fromDecimal paymentDec
                else
                    let growth = pow (1m + rate) (decimal totalPeriods)
                    let pvResidual = residual / growth
                    let baseAmount = financedPrincipal - pvResidual
                    if baseAmount <= 0m then invalidOp "Discounted residual >= financed principal."
                    let denom = 1m - 1m / growth
                    if denom = 0m then invalidOp "Denominator collapsed (rate too small / overflow)."
                    let paymentDec = baseAmount * rate / denom
                    if paymentDec <= 0m then invalidOp "Computed payment is non-positive."
                    Cent.fromDecimal paymentDec

        let rec generateSchedule paymentNum liability acc =
            if paymentNum > totalPeriods then
                acc |> List.rev |> Array.ofList
            else
                let interestPortion, principalPortion, paymentAmount, newLiability =
                    if terms.LeaseType = LeaseType.OperatingLease then
                        0L<Cent>, 0L<Cent>, leasePayment, 0L<Cent>
                    else
                        let interest =
                            decimal liability * rate * 1m<Cent>
                            |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

                        let scheduledPrincipal = leasePayment - interest
                        if scheduledPrincipal < 0L<Cent> then
                            invalidArg (nameof terms.LeasePayment) "Lease payment must cover at least the period interest."
                        let remainingPrincipalBeforeResidual = max 0L<Cent> (liability - terms.ResidualValue)
                        let principal =
                            if paymentNum = totalPeriods then
                                remainingPrincipalBeforeResidual
                            else
                                min scheduledPrincipal remainingPrincipalBeforeResidual
                        if terms.LeasePayment > 0L<Cent> && principal > scheduledPrincipal then
                            invalidArg (nameof terms.LeasePayment) "LeasePayment is too low to amortize to the stated residual by maturity."
                        let payment = interest + principal
                        let remaining = liability - principal
                        interest, principal, payment, remaining

                let item = {
                    PaymentNumber = paymentNum
                    PaymentDate = DateDay.Date(2000, 1, 1).AddMonths(paymentNum * monthsPerPeriod)
                    PaymentAmount = paymentAmount
                    PrincipalPortion = principalPortion
                    InterestPortion = interestPortion
                    RemainingLiability = newLiability
                }

                generateSchedule (paymentNum + 1) newLiability (item :: acc)

        let initialLiability =
            if terms.LeaseType = LeaseType.OperatingLease then 0L<Cent>
            else terms.FairMarketValue - terms.UpfrontPayment

        leasePayment, periodsPerYear, monthsPerPeriod, totalPeriods, rate, generateSchedule 1 initialLiability []

    /// Calculate level lease payment with optional residual (balloon)
    /// Assumptions:
    /// - All incoming monetary values are int64<Cent>
    /// - UpfrontPayment reduces financed amount
    /// - ResidualValue discounted over total periods
    /// - Interest.Rate.Annual is nominal annual; divided by payments/year
    let calculateLeasePayment (terms: EquipmentLeaseTerms) : int64<Cent> =
        buildScheduleCore terms |> fun (leasePayment, _, _, _, _, _) -> leasePayment

    /// Calculate lease payment details
    let calculateLeaseDetails (terms: EquipmentLeaseTerms) : LeaseCalculation =
        validateTerms terms
        let leasePayment = 
            if terms.LeasePayment > 0L<Cent> then terms.LeasePayment 
            else calculateLeasePayment terms

        let _, periodsPerYear, _, _, rate, schedule = buildScheduleCore terms
        let totalPayments = (schedule |> Array.sumBy (fun item -> item.PaymentAmount)) + terms.UpfrontPayment
        
        let totalCost = 
            match terms.PurchaseOption with
            | Some purchasePrice -> totalPayments + purchasePrice
            | None -> totalPayments
        
        // Calculate present value of lease payments
        let presentValue = 
            if rate = 0m then
                totalPayments
            else
                let pv = schedule
                         |> Array.sumBy (fun item -> decimal item.PaymentAmount / pow (1m + rate) (decimal item.PaymentNumber))
                         |> (+) (decimal terms.UpfrontPayment)
                         |> (*) 1m<Cent>
                Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero) pv
        
        let aprEquivalent = 
            match terms.ImplicitRate with
            | Interest.Rate.Zero -> Percent 0m
            | Interest.Rate.Annual percent -> percent
            | Interest.Rate.Daily (Percent rate) -> Percent (rate * 365m)
        
        {
            LeasePayment = leasePayment
            TotalPayments = totalPayments
            TotalCost = totalCost
            AprEquivalent = aprEquivalent
            PresentValue = presentValue
        }

    /// Generate lease payment schedule
    let generateLeaseSchedule (terms: EquipmentLeaseTerms) (startDate: DateDay.Date) : LeaseScheduleItem array =
        let _, _, monthsPerPeriod, _, _, schedule = buildScheduleCore terms

        schedule
        |> Array.map (fun item ->
            { item with PaymentDate = startDate.AddMonths(item.PaymentNumber * monthsPerPeriod) })

    /// Analyze lease vs buy decision
    type LeaseVsBuyAnalysis = {
        /// Lease calculation details
        LeaseDetails: LeaseCalculation
        /// Depreciation schedule if equipment were purchased
        PurchaseDepreciation: Types.DepreciationYear list
        /// Lease payment schedule
        LeaseSchedule: LeaseScheduleItem array
        /// Net advantage to leasing (positive means leasing is better)
        NetAdvantageToLeasing: int64<Cent> option
    }

    /// Perform lease vs buy analysis
    let analyzeLeaseVsBuy (terms: EquipmentLeaseTerms) (startDate: DateDay.Date) : LeaseVsBuyAnalysis =
        let leaseDetails = calculateLeaseDetails terms
        let leaseSchedule = generateLeaseSchedule terms startDate
        
        // Create depreciation schedule for purchase scenario
        let macrsAsset = {
            Types.CostBasis = terms.FairMarketValue
            Types.PlacedInServiceDate = startDate
            Types.PropertyClass = Calculations.classifyAsset terms.EquipmentDescription
            Types.Convention = Types.Convention.HalfYear
        }
        
        let depreciationSchedule = Calculations.generateSchedule macrsAsset
        
        {
            LeaseDetails = leaseDetails
            PurchaseDepreciation = depreciationSchedule
            LeaseSchedule = leaseSchedule
            NetAdvantageToLeasing = None // Would implement full NPV analysis in complete version
        }
