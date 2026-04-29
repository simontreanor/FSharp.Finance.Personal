namespace FSharp.Finance.Personal

open System

/// tools for modelling savings and investment projections, fixed-term deposits, and Premium Bonds
module Savings =

    open Calculation

    // ============================================================
    // Shared helpers
    // ============================================================

    /// raise a decimal to a decimal power
    let private powm (power: decimal) (base': decimal) =
        Math.Pow(double base', double power) |> decimal

    /// raise a decimal to an int power
    let private powi (power: int) (base': decimal) =
        Math.Pow(double base', double power) |> decimal

    // ============================================================
    // 1. Regular savings / ISA projections
    // ============================================================

    /// how interest is compounded
    [<RequireQualifiedAccess; Struct>]
    type CompoundingFrequency =
        /// interest is compounded once per year
        | Annually
        /// interest is compounded four times per year
        | Quarterly
        /// interest is compounded twelve times per year
        | Monthly
        /// interest is compounded every day
        | Daily

    /// number of compounding periods per year for each frequency
    module CompoundingFrequency =
        /// the number of compounding periods per year
        let periodsPerYear =
            function
            | CompoundingFrequency.Daily -> 365
            | CompoundingFrequency.Monthly -> 12
            | CompoundingFrequency.Quarterly -> 4
            | CompoundingFrequency.Annually -> 1

    /// the tax treatment applied to a savings product
    [<RequireQualifiedAccess; Struct>]
    type TaxWrapper =
        /// no special tax treatment; interest is subject to income tax in the normal way
        | None
        /// Individual Savings Account — interest and growth are tax-free
        | Isa
        /// Lifetime ISA — 25% government bonus on contributions up to £4,000 per tax year;
        /// withdrawals before age 60 (except first-home purchase or terminal illness) incur a 25% penalty
        | Lisa
        /// pension via salary sacrifice — contributions are made gross of income tax and NI,
        /// modelled here with an optional employer top-up rate on the employee contribution
        | PensionSalarySacrifice of EmployerMatchRate: Percent

    /// an interest-rate schedule entry, pairing a one-based start month with the annual rate applicable from that month onwards
    [<Struct>]
    type RateScheduleEntry = {
        /// one-based month number from which this rate becomes effective
        FromMonth: int
        /// the gross annual interest rate applicable from the given month
        AnnualRate: Percent
    }

    /// the interest rate applied to a regular savings product
    [<RequireQualifiedAccess>]
    type SavingsRate =
        /// a single fixed annual interest rate for the entire term
        | Fixed of Percent
        /// a schedule of rates that change at defined months during the term
        | Stepped of RateScheduleEntry array

    /// module for working with savings rate schedules
    module SavingsRate =
        /// returns the annual rate applicable in the given one-based month
        let rateForMonth month =
            function
            | SavingsRate.Fixed rate -> rate
            | SavingsRate.Stepped schedule ->
                schedule
                |> Array.filter (fun entry -> entry.FromMonth <= month)
                |> Array.sortByDescending (fun entry -> entry.FromMonth)
                |> Array.tryHead
                |> Option.map (fun entry -> entry.AnnualRate)
                |> Option.defaultValue (Percent 0m)

    /// configuration for a regular-savings or ISA projection
    [<Struct>]
    type RegularSavingsConfig = {
        /// lump sum deposited at the start (month 0)
        InitialDeposit: int64<Cent>
        /// fixed amount deposited at the start of each month
        MonthlyContribution: int64<Cent>
        /// the interest rate(s) applicable over the term
        SavingsRate: SavingsRate
        /// how interest is compounded
        CompoundingFrequency: CompoundingFrequency
        /// the tax treatment applied to this account
        TaxWrapper: TaxWrapper
        /// the length of the savings term in months
        TermMonths: int
    }

    /// the projected state of a savings account at the end of a given month
    [<Struct>]
    type SavingsProjectionEntry = {
        /// the one-based month number
        Month: int
        /// balance at the start of the month, before contribution and interest
        OpeningBalance: int64<Cent>
        /// contribution deposited this month (including any government bonus for LISA, or employer match for pension)
        Contribution: int64<Cent>
        /// interest credited this month
        InterestEarned: int64<Cent>
        /// balance at the end of the month
        ClosingBalance: int64<Cent>
        /// total of all contributions made so far (including bonuses/employer match)
        TotalContributions: int64<Cent>
        /// total interest earned so far
        TotalInterestEarned: int64<Cent>
    }

    /// compute the monthly interest factor from an annual rate and compounding frequency
    let private monthlyGrowthFactor (Percent annualRate) (freq: CompoundingFrequency) =
        let n = CompoundingFrequency.periodsPerYear freq |> decimal
        let r = annualRate / 100m
        match freq with
        | CompoundingFrequency.Monthly ->
            // one compounding period = one month
            1m + r / n
        | _ ->
            // convert to effective monthly rate via compound-equivalent
            (1m + r / n |> powm (n / 12m))

    /// project a regular savings account over its full term, returning one entry per month
    let project (config: RegularSavingsConfig) : SavingsProjectionEntry array =
        let rounding = RoundWith MidpointRounding.AwayFromZero

        /// effective monthly contribution after applying tax-wrapper bonuses / employer match
        let effectiveContribution (monthlyContrib: int64<Cent>) =
            match config.TaxWrapper with
            | TaxWrapper.Lisa ->
                // 25% government bonus on employee contribution (bonus applied monthly)
                let bonus = decimal monthlyContrib * 0.25m |> Cent.round rounding
                monthlyContrib + bonus
            | TaxWrapper.PensionSalarySacrifice(Percent matchRate) ->
                let employerMatch = decimal monthlyContrib * matchRate / 100m |> Cent.round rounding
                monthlyContrib + employerMatch
            | TaxWrapper.None | TaxWrapper.Isa ->
                monthlyContrib

        let initialEffective =
            match config.TaxWrapper with
            | TaxWrapper.Lisa ->
                let bonus = decimal config.InitialDeposit * 0.25m |> Cent.round rounding
                config.InitialDeposit + bonus
            | TaxWrapper.PensionSalarySacrifice(Percent matchRate) ->
                let employerMatch = decimal config.InitialDeposit * matchRate / 100m |> Cent.round rounding
                config.InitialDeposit + employerMatch
            | TaxWrapper.None | TaxWrapper.Isa ->
                config.InitialDeposit

        let monthlyContrib = effectiveContribution config.MonthlyContribution

        Array.init config.TermMonths (fun i ->
            let month = i + 1
            i, month
        )
        |> Array.scan
            (fun (entry: SavingsProjectionEntry) (_, month) ->
                let opening = entry.ClosingBalance
                let rate = SavingsRate.rateForMonth month config.SavingsRate
                let factor = monthlyGrowthFactor rate config.CompoundingFrequency
                let balanceAfterContrib = opening + monthlyContrib
                let closing = decimal balanceAfterContrib * factor |> Cent.round rounding
                let interest = closing - balanceAfterContrib
                {
                    Month = month
                    OpeningBalance = opening
                    Contribution = monthlyContrib
                    InterestEarned = interest
                    ClosingBalance = closing
                    TotalContributions = entry.TotalContributions + monthlyContrib
                    TotalInterestEarned = entry.TotalInterestEarned + interest
                }
            )
            {
                Month = 0
                OpeningBalance = 0L<Cent>
                Contribution = initialEffective
                InterestEarned = 0L<Cent>
                ClosingBalance = initialEffective
                TotalContributions = initialEffective
                TotalInterestEarned = 0L<Cent>
            }
        |> Array.tail // drop the seed entry

    // ============================================================
    // 2. Fixed-term deposits
    // ============================================================

    /// when earned interest is credited to the depositor
    [<RequireQualifiedAccess; Struct>]
    type InterestPaymentTiming =
        /// all interest is paid at the end of the term in a single lump sum
        | AtMaturity
        /// interest is credited and (optionally) paid out each month
        | Monthly
        /// interest is credited and (optionally) paid out each year
        | Annually

    /// configuration for a fixed-term deposit account
    [<Struct>]
    type FixedTermDepositConfig = {
        /// the amount placed on deposit
        Principal: int64<Cent>
        /// the gross annual interest rate as quoted by the provider
        GrossAnnualRate: Percent
        /// the length of the deposit term in months
        TermMonths: int
        /// when interest is credited / paid
        InterestPaymentTiming: InterestPaymentTiming
        /// number of months of interest forfeited as an early-withdrawal penalty (0 = no penalty)
        EarlyWithdrawalPenaltyMonths: int
    }

    /// the projected state of a fixed-term deposit at the end of a given month
    [<Struct>]
    type DepositProjectionEntry = {
        /// the one-based month number
        Month: int
        /// balance at the start of the month
        OpeningBalance: int64<Cent>
        /// interest accrued this month (may not yet be credited)
        InterestAccrued: int64<Cent>
        /// interest credited (paid) this month
        InterestCredited: int64<Cent>
        /// balance at the end of the month (excluding uncredited accrued interest)
        ClosingBalance: int64<Cent>
        /// cumulative accrued interest not yet credited
        AccruedNotYetCredited: int64<Cent>
        /// total interest credited so far
        TotalInterestCredited: int64<Cent>
    }

    /// the overall result when a fixed-term deposit reaches maturity
    [<Struct>]
    type DepositMaturityResult = {
        /// the original principal
        Principal: int64<Cent>
        /// total interest earned over the full term
        TotalInterest: int64<Cent>
        /// the total amount returned to the depositor (principal + interest)
        MaturityValue: int64<Cent>
        /// the AER (Annual Equivalent Rate) corresponding to the gross rate and payment timing
        Aer: Percent
        /// the month-by-month projection
        Schedule: DepositProjectionEntry array
    }

    /// the result when a fixed-term deposit is withdrawn before maturity
    [<Struct>]
    type EarlyWithdrawalResult = {
        /// the original principal
        Principal: int64<Cent>
        /// gross interest that would have been earned up to the withdrawal date
        GrossInterestToDate: int64<Cent>
        /// the penalty (interest forfeited) expressed in currency
        PenaltyAmount: int64<Cent>
        /// net interest received after applying the penalty
        NetInterestReceived: int64<Cent>
        /// the total amount returned to the depositor
        AmountReturned: int64<Cent>
    }

    /// convert a gross annual rate and compounding frequency to AER
    /// AER = (1 + grossRate / n) ^ n − 1
    let grossToAer (Percent grossRate) (periodsPerYear: int) : Percent =
        let r = grossRate / 100m
        let n = decimal periodsPerYear
        ((1m + r / n |> powm n) - 1m) * 100m |> Percent

    /// convert an AER back to a gross annual rate for the given compounding frequency
    /// grossRate = n * ((1 + AER) ^ (1/n) − 1)
    let aerToGross (Percent aer) (periodsPerYear: int) : Percent =
        let a = aer / 100m
        let n = decimal periodsPerYear
        n * ((1m + a |> powm (1m / n)) - 1m) * 100m |> Percent

    /// compute the AER for a fixed-term deposit given its gross rate and payment timing
    let depositAer (config: FixedTermDepositConfig) : Percent =
        let periodsPerYear =
            match config.InterestPaymentTiming with
            | InterestPaymentTiming.Monthly -> 12
            | InterestPaymentTiming.Annually -> 1
            | InterestPaymentTiming.AtMaturity ->
                // compounding at maturity = simple interest for sub-annual terms,
                // or annual compounding otherwise; we use months as periods
                12
        grossToAer config.GrossAnnualRate periodsPerYear

    /// project a fixed-term deposit over its full term, returning one entry per month and overall results
    let projectDeposit (config: FixedTermDepositConfig) : DepositMaturityResult =
        let rounding = RoundWith MidpointRounding.AwayFromZero
        let monthlyRate = Percent.toDecimal config.GrossAnnualRate / 12m

        // For AtMaturity deposits the interest compounds (balance grows each month).
        // For Monthly/Annually payout products the principal stays fixed; interest is paid out
        // and does not feed back into subsequent months' accrual calculations.
        let isCompounding =
            match config.InterestPaymentTiming with
            | InterestPaymentTiming.AtMaturity -> true
            | _ -> false

        let schedule =
            Array.init config.TermMonths (fun i -> i + 1)
            |> Array.scan
                (fun (entry: DepositProjectionEntry) month ->
                    // For compounding products the accrual base is the running balance;
                    // for payout products it is always the original principal.
                    let accrualBase =
                        if isCompounding then entry.ClosingBalance else config.Principal
                    let opening = entry.ClosingBalance
                    let accrued = decimal accrualBase * monthlyRate |> Cent.round rounding
                    let isCreditMonth =
                        match config.InterestPaymentTiming with
                        | InterestPaymentTiming.AtMaturity -> month = config.TermMonths
                        | InterestPaymentTiming.Monthly -> true
                        | InterestPaymentTiming.Annually -> month % 12 = 0 || month = config.TermMonths
                    let totalAccrued = entry.AccruedNotYetCredited + accrued
                    let credited = if isCreditMonth then totalAccrued else 0L<Cent>
                    let remainingAccrued = if isCreditMonth then 0L<Cent> else totalAccrued
                    // For payout products credited interest is removed from the account, so the
                    // deposit balance returns to the original principal.  For compounding products
                    // the accrued interest stays and the balance grows.
                    let newClosing =
                        if isCompounding then opening + accrued
                        else config.Principal
                    {
                        Month = month
                        OpeningBalance = opening
                        InterestAccrued = accrued
                        InterestCredited = credited
                        ClosingBalance = newClosing
                        AccruedNotYetCredited = remainingAccrued
                        TotalInterestCredited = entry.TotalInterestCredited + credited
                    }
                )
                {
                    Month = 0
                    OpeningBalance = config.Principal
                    InterestAccrued = 0L<Cent>
                    InterestCredited = 0L<Cent>
                    ClosingBalance = config.Principal
                    AccruedNotYetCredited = 0L<Cent>
                    TotalInterestCredited = 0L<Cent>
                }
            |> Array.tail

        let lastEntry = schedule |> Array.last
        let totalInterest = lastEntry.TotalInterestCredited
        let aer = depositAer config

        {
            Principal = config.Principal
            TotalInterest = totalInterest
            MaturityValue = config.Principal + totalInterest
            Aer = aer
            Schedule = schedule
        }

    /// calculate the amount returned when a fixed-term deposit is withdrawn before maturity
    let earlyWithdrawal (config: FixedTermDepositConfig) (withdrawalMonth: int) : EarlyWithdrawalResult =
        let rounding = RoundWith MidpointRounding.AwayFromZero
        let monthlyRate = Percent.toDecimal config.GrossAnnualRate / 12m

        let grossInterest =
            decimal config.Principal * monthlyRate * decimal withdrawalMonth
            |> Cent.round rounding

        let penaltyMonths = min config.EarlyWithdrawalPenaltyMonths withdrawalMonth
        let penalty =
            decimal config.Principal * monthlyRate * decimal penaltyMonths
            |> Cent.round rounding

        let netInterest = grossInterest - penalty

        {
            Principal = config.Principal
            GrossInterestToDate = grossInterest
            PenaltyAmount = penalty
            NetInterestReceived = netInterest
            AmountReturned = config.Principal + netInterest
        }

    // ============================================================
    // 3. Premium Bonds expected return (stretch goal)
    // ============================================================

    /// configuration for a Premium Bonds holding
    [<Struct>]
    type PremiumBondsConfig = {
        /// the total face value of bonds held (£1 per bond; minimum £25, maximum £50,000)
        HoldingAmount: int64<Cent>
        /// the annual prize fund rate published by NS&I (equivalent to an interest rate for comparison purposes)
        PrizeFundRate: Percent
        /// the holder's marginal income tax rate (used for after-tax comparisons)
        MarginalIncomeTaxRate: Percent
    }

    /// expected annual return from a Premium Bonds holding, expressed in currency
    let premiumBondsExpectedReturn (config: PremiumBondsConfig) : int64<Cent> =
        let rounding = RoundWith MidpointRounding.AwayFromZero
        decimal config.HoldingAmount * Percent.toDecimal config.PrizeFundRate
        |> Cent.round rounding

    /// comparison of Premium Bonds against a cash ISA and a taxable savings account
    [<Struct>]
    type PremiumBondsComparison = {
        /// expected annual prize winnings from the Premium Bonds holding
        PremiumBondsExpectedReturn: int64<Cent>
        /// equivalent annual return rate needed from a taxable account to match Premium Bonds after tax
        EquivalentGrossRate: Percent
        /// after-tax interest from a taxable cash savings account at the same gross rate as the prize fund rate
        TaxableSavingsAfterTax: int64<Cent>
        /// interest from a cash ISA at the same gross rate as the prize fund rate (tax-free)
        CashIsaReturn: int64<Cent>
    }

    /// compare the expected return from Premium Bonds against cash ISA and taxable savings equivalents
    let comparePremiumBonds (config: PremiumBondsConfig) (cashIsaRate: Percent) : PremiumBondsComparison =
        let rounding = RoundWith MidpointRounding.AwayFromZero

        let pbReturn = premiumBondsExpectedReturn config

        // gross rate needed on a taxable account to equal the tax-free Premium Bonds return
        // pbReturn = grossInterest * (1 − taxRate)  ⟹  grossRate = prizeFundRate / (1 − taxRate)
        let (Percent taxRate) = config.MarginalIncomeTaxRate
        let (Percent pfRate) = config.PrizeFundRate
        let equivalentGrossRate =
            if taxRate >= 100m then Percent 0m
            else pfRate / (1m - taxRate / 100m) |> Percent

        let taxableSavingsGross =
            decimal config.HoldingAmount * pfRate / 100m |> Cent.round rounding

        let taxableSavingsAfterTax =
            decimal taxableSavingsGross * (1m - taxRate / 100m) |> Cent.round rounding

        let (Percent isaRate) = cashIsaRate
        let cashIsaReturn =
            decimal config.HoldingAmount * isaRate / 100m |> Cent.round rounding

        {
            PremiumBondsExpectedReturn = pbReturn
            EquivalentGrossRate = equivalentGrossRate
            TaxableSavingsAfterTax = taxableSavingsAfterTax
            CashIsaReturn = cashIsaReturn
        }
