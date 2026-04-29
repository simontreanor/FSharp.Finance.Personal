namespace FSharp.Finance.Personal

open System

/// functions for specialist mortgage product calculations: offset mortgages, equity release / lifetime mortgages, and shared ownership
module Mortgage =

    open Calculation
    open DateDay

    // -------------------------------------------------------------------------
    // Offset mortgage
    // -------------------------------------------------------------------------

    /// parameters for an offset mortgage calculation
    [<Struct>]
    type OffsetMortgageParameters = {
        /// the outstanding mortgage balance (in base currency units, e.g. cents / pence)
        MortgageBalance: int64<Cent>
        /// the total balance held in linked savings / current accounts that offset the mortgage balance
        LinkedSavingsBalance: int64<Cent>
        /// the annual interest rate on the mortgage
        AnnualInterestRate: Percent
        /// the remaining term of the mortgage in months
        RemainingTermMonths: int
        /// the monthly payment amount (if already known); if not provided, it is derived from the full mortgage balance
        MonthlyPayment: int64<Cent> voption
    }

    /// result of an offset mortgage calculation
    [<Struct>]
    type OffsetMortgageResult = {
        /// the net balance on which interest is charged (mortgage balance minus linked savings)
        NetInterestBearingBalance: int64<Cent>
        /// the monthly interest saving due to the offset
        MonthlyInterestSaving: int64<Cent>
        /// the monthly payment on the net (offset) balance
        EffectiveMonthlyPayment: int64<Cent>
        /// estimated months saved off the remaining term if monthly payment is kept the same
        EstimatedMonthsSaved: int
        /// total interest payable over the remaining term on the net (offset) balance
        TotalInterestOnNetBalance: int64<Cent>
        /// total interest that would have been payable on the full mortgage balance
        TotalInterestOnFullBalance: int64<Cent>
    }

    /// calculates the monthly payment for a capital-and-interest (annuity) loan
    let private calculateMonthlyPayment (principalCents: int64<Cent>) (annualRatePercent: Percent) (termMonths: int) : int64<Cent> =
        let r = Percent.toDecimal annualRatePercent / 12m
        if r = 0m then
            if termMonths = 0 then 0L<Cent>
            else Cent.round (RoundWith MidpointRounding.AwayFromZero) (decimal principalCents / decimal termMonths)
        else
            let decimalPow n x = decimal (Math.Pow(double x, double n))
            let factor = (1m + r) |> decimalPow termMonths
            let payment = decimal principalCents * r * factor / (factor - 1m)
            Cent.round (RoundWith MidpointRounding.AwayFromZero) payment

    /// estimates how many months a capital-and-interest loan with given balance, rate and payment will take to repay
    let private estimateTermMonths (principalCents: int64<Cent>) (annualRatePercent: Percent) (monthlyPaymentCents: int64<Cent>) : int =
        let r = Percent.toDecimal annualRatePercent / 12m
        if r = 0m then
            if monthlyPaymentCents = 0L<Cent> then Int32.MaxValue
            else int (Math.Ceiling(decimal principalCents / decimal monthlyPaymentCents))
        else
            let p = decimal principalCents
            let m = decimal monthlyPaymentCents
            if m <= p * r then
                // payment does not cover interest – loan never repays
                Int32.MaxValue
            else
                // n = -ln(1 - r*P/M) / ln(1+r)
                let n = -Math.Log(double (1m - r * p / m)) / Math.Log(double (1m + r))
                int (Math.Ceiling n)

    /// calculates total interest paid on a capital-and-interest loan
    let private totalInterest (principalCents: int64<Cent>) (annualRatePercent: Percent) (termMonths: int) : int64<Cent> =
        let payment = calculateMonthlyPayment principalCents annualRatePercent termMonths
        let total = decimal payment * decimal termMonths
        Cent.round (RoundWith MidpointRounding.AwayFromZero) (total - decimal principalCents)
        |> max 0L<Cent>

    /// calculates offset mortgage metrics, including net interest-bearing balance, payment savings and term reduction
    let calculateOffsetMortgage (p: OffsetMortgageParameters) : OffsetMortgageResult =
        let netBalance =
            (p.MortgageBalance - p.LinkedSavingsBalance)
            |> max 0L<Cent>

        let fullPayment =
            match p.MonthlyPayment with
            | ValueSome mp -> mp
            | ValueNone -> calculateMonthlyPayment p.MortgageBalance p.AnnualInterestRate p.RemainingTermMonths

        let netPayment = calculateMonthlyPayment netBalance p.AnnualInterestRate p.RemainingTermMonths

        let monthlySaving =
            Cent.round (RoundWith MidpointRounding.AwayFromZero) (decimal fullPayment - decimal netPayment)
            |> max 0L<Cent>

        let estimatedMonthsSaved =
            if netBalance = 0L<Cent> then
                p.RemainingTermMonths
            else
                let monthsAtFullPayment = estimateTermMonths netBalance p.AnnualInterestRate fullPayment
                p.RemainingTermMonths - monthsAtFullPayment |> max 0

        let totalInterestNet = totalInterest netBalance p.AnnualInterestRate p.RemainingTermMonths
        let totalInterestFull = totalInterest p.MortgageBalance p.AnnualInterestRate p.RemainingTermMonths

        {
            NetInterestBearingBalance = netBalance
            MonthlyInterestSaving = monthlySaving
            EffectiveMonthlyPayment = netPayment
            EstimatedMonthsSaved = estimatedMonthsSaved
            TotalInterestOnNetBalance = totalInterestNet
            TotalInterestOnFullBalance = totalInterestFull
        }

    // -------------------------------------------------------------------------
    // Equity release / lifetime mortgage
    // -------------------------------------------------------------------------

    /// a voluntary partial repayment made at a specific date
    [<Struct>]
    type PartialRepayment = {
        /// the date on which the partial repayment is made
        RepaymentDate: Date
        /// the amount repaid (in base currency units, e.g. cents / pence)
        RepaymentAmount: int64<Cent>
    }

    /// parameters for an equity release / lifetime mortgage calculation
    [<Struct>]
    type EquityReleaseParameters = {
        /// the initial advance (in base currency units)
        InitialAdvance: int64<Cent>
        /// the date on which the advance is made
        StartDate: Date
        /// the annual roll-up (compound) interest rate
        AnnualInterestRate: Percent
        /// the projected end date for balance calculation (e.g. expected sale or death)
        ProjectionDate: Date
        /// any voluntary partial repayments made during the term
        PartialRepayments: PartialRepayment array
        /// the maximum property value at the start, used to model the no-negative-equity guarantee
        PropertyValue: int64<Cent> voption
    }

    /// result of an equity release projection
    [<Struct>]
    type EquityReleaseResult = {
        /// projected outstanding balance at the projection date (before NNEG cap)
        ProjectedBalance: int64<Cent>
        /// total interest rolled up over the projection period
        TotalRolledUpInterest: int64<Cent>
        /// projected balance after applying the no-negative-equity guarantee (capped at property value)
        BalanceAfterNneg: int64<Cent>
        /// whether the no-negative-equity guarantee was triggered (i.e. projected balance exceeded property value)
        NnegTriggered: bool
    }

    /// projects the balance of an equity release / lifetime mortgage at a future date,
    /// accounting for daily compounding and any voluntary partial repayments
    let calculateEquityRelease (p: EquityReleaseParameters) : EquityReleaseResult =
        // materialise DateTime values once for efficient comparison
        let projectionDateTime = p.ProjectionDate.ToDateTime()

        // sort repayments by date, materialising DateTime values once
        let sortedRepayments =
            p.PartialRepayments
            |> Array.map (fun r -> r.RepaymentDate.ToDateTime(), r)
            |> Array.sortBy fst

        // compound the balance from the start date to the projection date,
        // applying any repayments on the relevant dates
        let dailyRate =
            Percent.toDecimal p.AnnualInterestRate / 365m

        let rec compound (currentDate: Date) (balance: decimal) (remainingRepayments: (DateTime * PartialRepayment) list) =
            let currentDateTime = currentDate.ToDateTime()
            if currentDateTime >= projectionDateTime then
                balance
            else
                // apply any repayments due today
                let repaidToday, remaining =
                    remainingRepayments
                    |> List.partition (fun (repaymentDt, _) -> repaymentDt <= currentDateTime)
                let repaymentAmount =
                    repaidToday |> List.sumBy (fun (_, r) -> decimal r.RepaymentAmount)
                let balanceAfterRepayment = max 0m (balance - repaymentAmount)
                // accrue one day of interest
                let newBalance = balanceAfterRepayment * (1m + dailyRate)
                compound (currentDate.AddDays 1) newBalance remaining

        let projectedDecimal =
            compound p.StartDate (decimal p.InitialAdvance) (sortedRepayments |> Array.toList)

        let projectedBalance =
            Cent.round (RoundWith MidpointRounding.AwayFromZero) projectedDecimal

        let totalInterest = projectedBalance - p.InitialAdvance |> max 0L<Cent>

        let nnegCap =
            match p.PropertyValue with
            | ValueSome pv -> pv
            | ValueNone -> Int64.MaxValue * 1L<Cent>

        let balanceAfterNneg = min projectedBalance nnegCap
        let nnegTriggered = projectedBalance > nnegCap

        {
            ProjectedBalance = projectedBalance
            TotalRolledUpInterest = totalInterest
            BalanceAfterNneg = balanceAfterNneg
            NnegTriggered = nnegTriggered
        }

    // -------------------------------------------------------------------------
    // Shared ownership
    // -------------------------------------------------------------------------

    /// a staircasing tranche: buying an additional share of the property
    [<Struct>]
    type StaircasingTranche = {
        /// the date on which the additional tranche is purchased
        PurchaseDate: Date
        /// the share being purchased (e.g. 0.25 for 25%)
        AdditionalShare: Percent
        /// the property value at the time of purchasing the additional tranche
        PropertyValueAtPurchase: int64<Cent>
    }

    /// parameters for a shared ownership calculation
    [<Struct>]
    type SharedOwnershipParameters = {
        /// the total property value at the start
        PropertyValue: int64<Cent>
        /// the share initially owned by the borrower (e.g. 0.25 for 25%)
        InitialShare: Percent
        /// the mortgage balance (covering the owned share) in base currency units
        MortgageBalance: int64<Cent>
        /// the annual mortgage interest rate
        MortgageAnnualRate: Percent
        /// the mortgage term in months
        MortgageTermMonths: int
        /// the annual rent charged on the unowned share (as a percentage of unowned share value)
        AnnualRentRate: Percent
        /// any planned staircasing tranches
        StaircasingTranches: StaircasingTranche array
        /// the evaluation horizon in months (used for total cost comparison)
        HorizonMonths: int
    }

    /// the blended monthly outgoing for a shared ownership product
    [<Struct>]
    type SharedOwnershipMonthlyOutgoing = {
        /// the monthly mortgage payment
        MonthlyMortgagePayment: int64<Cent>
        /// the monthly rent on the unowned share
        MonthlyRent: int64<Cent>
        /// the total blended monthly outgoing
        TotalMonthlyOutgoing: int64<Cent>
    }

    /// the result of a staircasing calculation
    [<Struct>]
    type StaircasingResult = {
        /// the cost of purchasing the additional tranche
        TrancheCost: int64<Cent>
        /// the owned share after purchasing the tranche
        NewOwnedShare: Percent
        /// the remaining unowned share
        NewUnownedShare: Percent
        /// the new monthly rent on the remaining unowned share (at the original rent rate)
        NewMonthlyRent: int64<Cent>
        /// the new monthly mortgage payment if the tranche is financed
        NewMonthlyMortgagePayment: int64<Cent>
        /// the new blended monthly outgoing
        NewTotalMonthlyOutgoing: int64<Cent>
    }

    /// the total-cost comparison result
    [<Struct>]
    type TotalCostComparison = {
        /// total cost over the horizon as a shared ownership occupier (mortgage payments + rent)
        TotalSharedOwnershipCost: int64<Cent>
        /// total cost over the horizon of an outright purchase (assumes mortgage payments only, no rent, same property value and rate)
        TotalOutrightPurchaseCost: int64<Cent>
        /// the difference (positive means shared ownership is more expensive over the horizon)
        CostDifference: int64<Cent>
    }

    /// calculates the blended monthly outgoing for a shared ownership product
    let calculateSharedOwnershipOutgoing (p: SharedOwnershipParameters) : SharedOwnershipMonthlyOutgoing =
        let mortgagePayment =
            calculateMonthlyPayment p.MortgageBalance p.MortgageAnnualRate p.MortgageTermMonths

        let unownedShare =
            let (Percent ownedShare) = p.InitialShare
            (100m - ownedShare) / 100m

        let annualRent =
            decimal p.PropertyValue * unownedShare * Percent.toDecimal p.AnnualRentRate

        let monthlyRent =
            Cent.round (RoundWith MidpointRounding.AwayFromZero) (annualRent / 12m)

        {
            MonthlyMortgagePayment = mortgagePayment
            MonthlyRent = monthlyRent
            TotalMonthlyOutgoing = mortgagePayment + monthlyRent
        }

    /// calculates the cost and impact of purchasing an additional tranche (staircasing)
    let calculateStaircasing
        (p: SharedOwnershipParameters)
        (tranche: StaircasingTranche)
        (existingMortgageBalance: int64<Cent>)
        (remainingMortgageTermMonths: int)
        : StaircasingResult =
        let (Percent additionalSharePct) = tranche.AdditionalShare
        let trancheCost =
            Cent.round
                (RoundWith MidpointRounding.AwayFromZero)
                (decimal tranche.PropertyValueAtPurchase * additionalSharePct / 100m)

        let (Percent ownedSharePct) = p.InitialShare
        let newOwnedSharePct = ownedSharePct + additionalSharePct |> min 100m
        let newOwnedShare = Percent newOwnedSharePct
        let newUnownedShare = Percent (100m - newOwnedSharePct)

        // new monthly rent on the remaining unowned share
        let newAnnualRent =
            decimal tranche.PropertyValueAtPurchase
            * ((100m - newOwnedSharePct) / 100m)
            * Percent.toDecimal p.AnnualRentRate

        let newMonthlyRent =
            Cent.round (RoundWith MidpointRounding.AwayFromZero) (newAnnualRent / 12m)

        // new mortgage = existing balance + tranche cost, financed over remaining term at same rate
        let newMortgageBalance = existingMortgageBalance + trancheCost
        let newMortgagePayment =
            calculateMonthlyPayment newMortgageBalance p.MortgageAnnualRate remainingMortgageTermMonths

        {
            TrancheCost = trancheCost
            NewOwnedShare = newOwnedShare
            NewUnownedShare = newUnownedShare
            NewMonthlyRent = newMonthlyRent
            NewMonthlyMortgagePayment = newMortgagePayment
            NewTotalMonthlyOutgoing = newMortgagePayment + newMonthlyRent
        }

    /// estimates the total cost of shared ownership vs. outright purchase over a given horizon
    let calculateTotalCostComparison (p: SharedOwnershipParameters) : TotalCostComparison =
        let outgoing = calculateSharedOwnershipOutgoing p

        // shared ownership: mortgage payments + rent for the horizon
        // (simplified: constant payment and rent throughout, ignoring staircasing)
        let totalSharedOwnershipCost =
            Cent.round
                (RoundWith MidpointRounding.AwayFromZero)
                (decimal (outgoing.TotalMonthlyOutgoing) * decimal p.HorizonMonths)

        // outright purchase comparison: mortgage payment on full property value, same rate and term
        let outrightPayment =
            calculateMonthlyPayment p.PropertyValue p.MortgageAnnualRate p.MortgageTermMonths

        let totalOutrightCost =
            Cent.round
                (RoundWith MidpointRounding.AwayFromZero)
                (decimal outrightPayment * decimal p.HorizonMonths)

        let costDifference = totalSharedOwnershipCost - totalOutrightCost

        {
            TotalSharedOwnershipCost = totalSharedOwnershipCost
            TotalOutrightPurchaseCost = totalOutrightCost
            CostDifference = costDifference
        }
