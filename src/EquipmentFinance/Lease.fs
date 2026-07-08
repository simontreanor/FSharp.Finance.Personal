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

    /// Timing of lease payments within each period
    [<Struct; RequireQualifiedAccess>]
    type PaymentTiming =
        | InArrears         // payment at the end of each period (annuity-immediate)
        | InAdvance         // payment at the start of each period (annuity-due; usual for equipment leases)

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
        /// Timing of payments within each period. Equipment leases are typically InAdvance;
        /// InArrears matches an ordinary loan-style annuity.
        PaymentTiming: PaymentTiming
        /// Lease payment amount excluding any PeriodicFee (0 to have it calculated from the other terms)
        LeasePayment: int64<Cent>
        /// Optional recurring fee (e.g. service or administration fee) charged on top of
        /// every payment; shown separately as FeePortion in the schedule
        PeriodicFee: int64<Cent> option
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
        /// The periodic lease payment (excluding any PeriodicFee)
        LeasePayment: int64<Cent>
        /// Total payments over the lease term, including any recurring fees
        TotalPayments: int64<Cent>
        /// Total recurring fees paid over the lease term
        TotalFees: int64<Cent>
        /// Total cost of leasing vs buying
        TotalCost: int64<Cent>
        /// The nominal annual rate implicit in the lease.
        /// Note: this is NOT an effective APR; no compounding or fee effects are included.
        NominalAnnualRate: Percent
        /// Present value of lease payments
        PresentValue: int64<Cent>
    }

    /// Lease schedule item
    type LeaseScheduleItem = {
        /// Payment number (1-based)
        PaymentNumber: int
        /// Payment due date
        PaymentDate: DateDay.Date
        /// The lease payment amount (rental + fee)
        PaymentAmount: int64<Cent>
        /// Principal portion (for finance leases)
        PrincipalPortion: int64<Cent>
        /// Interest portion (for finance leases)
        InterestPortion: int64<Cent>
        /// Recurring fee portion of payment
        FeePortion: int64<Cent>
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
        match terms.PeriodicFee with
        | Some fee when fee < 0L<Cent> -> invalidArg (nameof terms.PeriodicFee) "PeriodicFee must be >= 0 when provided."
        | _ -> ()

    let private periodRate (terms: EquipmentLeaseTerms) (periodsPerYear: int) =
        match terms.ImplicitRate with
        | Interest.Rate.Zero -> 0m
        | Interest.Rate.Annual (Percent rate) -> rate / 100m / decimal periodsPerYear
        | Interest.Rate.Daily (Percent rate) -> rate / 100m * 365m / decimal periodsPerYear

    /// The level rental that amortizes the financed principal exactly to the residual over the term
    let private levelPaymentDec (financedPrincipal: decimal) (residual: decimal) (rate: decimal) (totalPeriods: int) (timing: PaymentTiming) =
        if rate = 0m then
            (financedPrincipal - residual) / decimal totalPeriods
        else
            let growth = pow (1m + rate) (decimal totalPeriods)
            let pvResidual = residual / growth
            let baseAmount = financedPrincipal - pvResidual
            let denom = 1m - 1m / growth
            if denom = 0m then invalidOp "Denominator collapsed (rate too small / overflow)."
            let ordinary = baseAmount * rate / denom
            match timing with
            | PaymentTiming.InArrears -> ordinary
            | PaymentTiming.InAdvance -> ordinary / (1m + rate)

    let private buildScheduleCore (terms: EquipmentLeaseTerms) =
        validateTerms terms

        let periodsPerYear, monthsPerPeriod, totalPeriods = getScheduleShape terms
        let rate = periodRate terms periodsPerYear

        let financedPrincipal = Cent.toDecimal (terms.FairMarketValue - terms.UpfrontPayment)
        let residualDec = Cent.toDecimal terms.ResidualValue

        // the level rental consistent with the stated residual; used as the computed payment
        // and quoted in error messages when a stated rental cannot land on the residual
        let consistentRentalDec = levelPaymentDec financedPrincipal residualDec rate totalPeriods terms.PaymentTiming

        let leasePayment =
            if terms.LeasePayment > 0L<Cent> then terms.LeasePayment
            else
                if financedPrincipal <= 0m then invalidArg "terms.UpfrontPayment" "Upfront payment >= fair value."
                if residualDec >= financedPrincipal then invalidOp "Discounted residual >= financed principal."
                if consistentRentalDec <= 0m then invalidOp "Computed payment is non-positive."
                Cent.fromDecimal consistentRentalDec

        let residual = terms.ResidualValue
        let fee = terms.PeriodicFee |> Option.defaultValue 0L<Cent>
        let round' (d: decimal<Cent>) = Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero) d
        // allow small rounding drift (schedule-length cents) before declaring the contract inconsistent
        let floorTolerance = int64 totalPeriods * 1L<Cent>

        let failTooHigh () =
            invalidArg (nameof terms.LeasePayment)
                $"LeasePayment {Cent.toDecimal leasePayment:N2} is too high to amortize exactly to the stated residual by maturity: the liability would fall below the residual before the final period. A consistent level rental is approximately {consistentRentalDec:N2}."

        let failTooLow (plugPayment: int64<Cent>) =
            invalidArg (nameof terms.LeasePayment)
                $"LeasePayment {Cent.toDecimal leasePayment:N2} is too low to amortize to the stated residual by maturity: the final payment would be {Cent.toDecimal plugPayment:N2}. The minimum consistent level rental is approximately {consistentRentalDec:N2}."

        // The stated rental is charged verbatim every period; interest accrues on the outstanding
        // liability and the FINAL period is the plug that lands exactly on the residual
        // (final principal = liability - residual). Inconsistent contracts are rejected.
        let rec generateSchedule paymentNum liability acc =
            if paymentNum > totalPeriods then
                acc |> List.rev |> Array.ofList
            else
                let interestPortion, principalPortion, paymentAmount, newLiability =
                    if terms.LeaseType = LeaseType.OperatingLease then
                        0L<Cent>, 0L<Cent>, leasePayment, 0L<Cent>
                    else
                        let isFinal = paymentNum = totalPeriods
                        match terms.PaymentTiming with
                        | PaymentTiming.InArrears ->
                            let interest = round' (decimal liability * rate * 1m<Cent>)
                            if isFinal then
                                let principal = liability - residual
                                let payment = interest + principal
                                if payment < 0L<Cent> then failTooHigh ()
                                if terms.LeasePayment > 0L<Cent> && payment > leasePayment * 2L then failTooLow payment
                                interest, principal, payment, residual
                            else
                                let principal = leasePayment - interest
                                if principal < 0L<Cent> then
                                    invalidArg (nameof terms.LeasePayment) "Lease payment must cover at least the period interest."
                                let remaining = liability - principal
                                if remaining < residual - floorTolerance then failTooHigh ()
                                interest, principal, leasePayment, remaining
                        | PaymentTiming.InAdvance ->
                            // payment at the start of the period; interest accrues on the
                            // post-payment balance and is settled within the same row, so the
                            // liability evolves as L' = (L - payment) * (1 + rate)
                            if isFinal then
                                let principal = liability - residual
                                let interest = round' (residualDec * rate / (1m + rate) * 100m<Cent>)
                                let payment = interest + principal
                                if payment < 0L<Cent> then failTooHigh ()
                                if terms.LeasePayment > 0L<Cent> && payment > leasePayment * 2L then failTooLow payment
                                interest, principal, payment, residual
                            else
                                if leasePayment > liability then failTooHigh ()
                                let interest = round' (decimal (liability - leasePayment) * rate * 1m<Cent>)
                                let principal = leasePayment - interest
                                if principal < 0L<Cent> then
                                    invalidArg (nameof terms.LeasePayment) "Lease payment must cover at least the period interest."
                                let remaining = liability - principal
                                if remaining < residual - floorTolerance then failTooHigh ()
                                interest, principal, leasePayment, remaining

                let item = {
                    PaymentNumber = paymentNum
                    PaymentDate = DateDay.Date(2000, 1, 1).AddMonths(paymentNum * monthsPerPeriod)
                    PaymentAmount = paymentAmount + fee
                    PrincipalPortion = principalPortion
                    InterestPortion = interestPortion
                    FeePortion = fee
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
    /// - InAdvance payments use the annuity-due adjustment (ordinary payment / (1 + period rate))
    let calculateLeasePayment (terms: EquipmentLeaseTerms) : int64<Cent> =
        buildScheduleCore terms |> fun (leasePayment, _, _, _, _, _) -> leasePayment

    /// Calculate lease payment details.
    /// Totals and present value are computed from the contractual schedule: the stated rental
    /// every period plus the final plug payment that lands exactly on the residual.
    let calculateLeaseDetails (terms: EquipmentLeaseTerms) : LeaseCalculation =
        let leasePayment, _, _, _, rate, schedule = buildScheduleCore terms
        let totalPayments = (schedule |> Array.sumBy (fun item -> item.PaymentAmount)) + terms.UpfrontPayment
        let totalFees = schedule |> Array.sumBy (fun item -> item.FeePortion)

        let totalCost =
            match terms.PurchaseOption with
            | Some purchasePrice -> totalPayments + purchasePrice
            | None -> totalPayments

        // Calculate present value of lease payments (in-advance payments fall at the start of
        // each period, so they are discounted one period less)
        let presentValue =
            if rate = 0m then
                totalPayments
            else
                let discountPeriods (item: LeaseScheduleItem) =
                    match terms.PaymentTiming with
                    | PaymentTiming.InArrears -> decimal item.PaymentNumber
                    | PaymentTiming.InAdvance -> decimal (item.PaymentNumber - 1)
                let pv = schedule
                         |> Array.sumBy (fun item -> decimal item.PaymentAmount / pow (1m + rate) (discountPeriods item))
                         |> (+) (decimal terms.UpfrontPayment)
                         |> (*) 1m<Cent>
                Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero) pv

        let nominalAnnualRate =
            match terms.ImplicitRate with
            | Interest.Rate.Zero -> Percent 0m
            | Interest.Rate.Annual percent -> percent
            | Interest.Rate.Daily (Percent rate) -> Percent (rate * 365m)

        {
            LeasePayment = leasePayment
            TotalPayments = totalPayments
            TotalFees = totalFees
            TotalCost = totalCost
            NominalAnnualRate = nominalAnnualRate
            PresentValue = presentValue
        }

    /// Generate lease payment schedule
    let generateLeaseSchedule (terms: EquipmentLeaseTerms) (startDate: DateDay.Date) : LeaseScheduleItem array =
        let _, _, monthsPerPeriod, _, _, schedule = buildScheduleCore terms

        let paymentDate (paymentNumber: int) =
            match terms.PaymentTiming with
            | PaymentTiming.InArrears -> startDate.AddMonths(paymentNumber * monthsPerPeriod)
            | PaymentTiming.InAdvance -> startDate.AddMonths((paymentNumber - 1) * monthsPerPeriod)

        schedule
        |> Array.map (fun item -> { item with PaymentDate = paymentDate item.PaymentNumber })

    /// Analyze lease vs buy decision
    type LeaseVsBuyAnalysis = {
        /// Lease calculation details
        LeaseDetails: LeaseCalculation
        /// Depreciation schedule if equipment were purchased
        PurchaseDepreciation: Types.DepreciationYear list
        /// Lease payment schedule
        LeaseSchedule: LeaseScheduleItem array
        /// Net advantage to leasing: NPV(cost of buying) - NPV(cost of leasing), both
        /// discounted at the supplied annual discount rate (positive means leasing is better).
        /// Buying costs the fair market value; if the lease has no purchase option the buyer
        /// additionally retains the residual value at term end (credited at present value),
        /// whereas with a purchase option both scenarios end in ownership so the terminal
        /// value cancels and the option price is a leasing cost.
        /// A simple pre-tax NPV; no tax effects are modelled.
        NetAdvantageToLeasing: int64<Cent>
        /// True when the equipment description did not match any known asset-class keyword
        /// and the default FiveYear MACRS class was assumed for the depreciation schedule
        AssetClassAssumed: bool
    }

    /// Perform lease vs buy analysis.
    /// annualDiscountRate is the rate used to discount both alternatives for NetAdvantageToLeasing.
    let analyzeLeaseVsBuy (terms: EquipmentLeaseTerms) (startDate: DateDay.Date) (annualDiscountRate: Percent) : LeaseVsBuyAnalysis =
        let leaseDetails = calculateLeaseDetails terms
        let leaseSchedule = generateLeaseSchedule terms startDate

        let periodsPerYear, _, totalPeriods = getScheduleShape terms
        let periodDiscount = Percent.toDecimal annualDiscountRate / decimal periodsPerYear
        let discountFactor (periods: decimal) = pow (1m + periodDiscount) periods

        let netAdvantageToLeasing =
            let discountPeriods (item: LeaseScheduleItem) =
                match terms.PaymentTiming with
                | PaymentTiming.InArrears -> decimal item.PaymentNumber
                | PaymentTiming.InAdvance -> decimal (item.PaymentNumber - 1)
            let leasePaymentsPv =
                leaseSchedule
                |> Array.sumBy (fun item -> decimal item.PaymentAmount / discountFactor (discountPeriods item))
            let leaseCostPv =
                decimal terms.UpfrontPayment
                + leasePaymentsPv
                + (match terms.PurchaseOption with
                   | Some price -> decimal price / discountFactor (decimal totalPeriods)
                   | None -> 0m)
            let buyCostPv =
                match terms.PurchaseOption with
                // without a purchase option the lessee hands the equipment back, so buying
                // retains the residual value at term end; with one, both scenarios end in
                // ownership and the terminal value cancels
                | None -> decimal terms.FairMarketValue - decimal terms.ResidualValue / discountFactor (decimal totalPeriods)
                | Some _ -> decimal terms.FairMarketValue
            (buyCostPv - leaseCostPv) * 1m<Cent>
            |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

        // Create depreciation schedule for the purchase scenario; if the description is not
        // recognized, FiveYear is assumed and the assumption is surfaced via AssetClassAssumed
        let classified = Calculations.tryClassifyAsset terms.EquipmentDescription
        let macrsAsset = {
            Types.CostBasis = terms.FairMarketValue
            Types.PlacedInServiceDate = startDate
            Types.PropertyClass = classified |> Option.defaultValue Types.AssetClass.FiveYear
            Types.Convention = Types.Convention.HalfYear
        }

        let depreciationSchedule = Calculations.generateSchedule macrsAsset

        {
            LeaseDetails = leaseDetails
            PurchaseDepreciation = depreciationSchedule
            LeaseSchedule = leaseSchedule
            NetAdvantageToLeasing = netAdvantageToLeasing
            AssetClassAssumed = classified.IsNone
        }
