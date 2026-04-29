namespace FSharp.Finance.Personal

/// higher-level decision-support functions built on top of the existing calculation modules
module Analytics =

    open Calculation
    open Scheduling

    // ── Loan Comparison ───────────────────────────────────────────────────────

    /// key metrics extracted from a basic schedule for comparison purposes
    [<Struct>]
    type LoanMetrics = {
        /// the total amount payable over the life of the loan (principal + fees + interest)
        TotalAmountPayable: int64<Cent>
        /// the total cost of credit (fees + interest = total amount payable minus the original principal advance)
        TotalCostOfCredit: int64<Cent>
        /// the typical level (regular) payment amount
        LevelPayment: int64<Cent>
        /// the Annual Percentage Rate
        Apr: Percent
    }

    /// a named loan offer with associated parameters and computed metrics
    type LoanOffer = {
        /// a human-readable name for the offer, e.g. "Lender A – 3-year fixed"
        Name: string
        /// the schedule parameters used to compute the offer
        Parameters: BasicParameters
        /// the key metrics derived from the basic schedule
        Metrics: LoanMetrics
    }

    /// ranked comparison of multiple loan offers
    type ComparisonResult = {
        /// all offers with their computed metrics
        Offers: LoanOffer array
        /// offers sorted by total cost of credit (ascending – cheapest first)
        RankedByTotalCostOfCredit: LoanOffer array
        /// offers sorted by level payment (ascending – lowest regular payment first)
        RankedByLevelPayment: LoanOffer array
        /// offers sorted by APR (ascending – lowest APR first)
        RankedByApr: LoanOffer array
        /// offers sorted by total amount payable (ascending – smallest total outlay first)
        RankedByTotalAmountPayable: LoanOffer array
    }

    /// functions for comparing multiple loan offers side-by-side
    module LoanComparison =

        /// compute key metrics for a single set of BasicParameters
        let private computeMetrics (bp: BasicParameters) =
            let schedule = calculateBasicSchedule bp
            {
                TotalAmountPayable = schedule.Stats.ScheduledPaymentTotal
                TotalCostOfCredit  = schedule.Stats.ScheduledPaymentTotal - bp.Principal
                LevelPayment       = schedule.Stats.LevelPayment
                Apr                = schedule.Stats.InitialApr
            }

        /// compare an array of named loan offers and return a ranked ComparisonResult;
        /// ties are broken by APR (ascending)
        let compare (offers: (string * BasicParameters) array) : ComparisonResult =
            let loanOffers =
                offers
                |> Array.map (fun (name, bp) -> {
                    Name       = name
                    Parameters = bp
                    Metrics    = computeMetrics bp
                })
            let aprKey (Percent a) = a
            {
                Offers                     = loanOffers
                RankedByTotalCostOfCredit  = loanOffers |> Array.sortBy (fun o -> o.Metrics.TotalCostOfCredit,  aprKey o.Metrics.Apr)
                RankedByLevelPayment       = loanOffers |> Array.sortBy (fun o -> o.Metrics.LevelPayment,       aprKey o.Metrics.Apr)
                RankedByApr                = loanOffers |> Array.sortBy (fun o -> aprKey o.Metrics.Apr,         o.Metrics.TotalCostOfCredit)
                RankedByTotalAmountPayable = loanOffers |> Array.sortBy (fun o -> o.Metrics.TotalAmountPayable, aprKey o.Metrics.Apr)
            }

    // ── Sensitivity Analysis ──────────────────────────────────────────────────

    /// a single sensitivity-analysis scenario showing the effect of a rate adjustment
    [<Struct>]
    type SensitivityScenario = {
        /// the rate adjustment applied relative to the base standard rate (in percent points)
        RateAdjustment: Percent
        /// the effective interest rate after applying the adjustment
        EffectiveRate: Interest.Rate
        /// the total amount payable under this scenario
        TotalAmountPayable: int64<Cent>
        /// the total cost of credit under this scenario
        TotalCostOfCredit: int64<Cent>
        /// the typical level payment under this scenario
        LevelPayment: int64<Cent>
        /// the APR under this scenario
        Apr: Percent
    }

    /// functions for performing sensitivity analysis on interest rate changes
    module SensitivityAnalysis =

        /// add a Percent adjustment to an existing Interest.Rate
        let private adjustRate (Percent adj) (rate: Interest.Rate) : Interest.Rate =
            match rate with
            | Interest.Rate.Zero ->
                if adj = 0m then Interest.Rate.Zero
                else Interest.Rate.Annual (Percent adj)
            | Interest.Rate.Annual (Percent r) -> Interest.Rate.Annual (Percent (r + adj))
            | Interest.Rate.Daily  (Percent r) -> Interest.Rate.Daily  (Percent (r + adj))

        /// analyse how total cost and level payment change across an array of interest-rate adjustments;
        /// each entry in the result corresponds to the scenario for the matching rateAdjustments element
        let analyse (baseParameters: BasicParameters) (rateAdjustments: Percent array) : SensitivityScenario array =
            rateAdjustments
            |> Array.map (fun adjustment ->
                let adjustedRate = adjustRate adjustment baseParameters.InterestConfig.StandardRate
                let adjustedBp   = {
                    baseParameters with
                        InterestConfig = { baseParameters.InterestConfig with StandardRate = adjustedRate }
                }
                let schedule = calculateBasicSchedule adjustedBp
                {
                    RateAdjustment     = adjustment
                    EffectiveRate      = adjustedRate
                    TotalAmountPayable = schedule.Stats.ScheduledPaymentTotal
                    TotalCostOfCredit  = schedule.Stats.ScheduledPaymentTotal - baseParameters.Principal
                    LevelPayment       = schedule.Stats.LevelPayment
                    Apr                = schedule.Stats.InitialApr
                }
            )
