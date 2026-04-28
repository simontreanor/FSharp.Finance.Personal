namespace FSharp.Finance.Personal

open System

/// modelling zero-interest instalment and deferred payment (Buy Now, Pay Later) products
///
/// BNPL products such as "pay in 3", "pay in 12", and "pay later" are characterised by:
/// - zero interest during the promotional period (if the consumer pays on time)
/// - penal interest or charges if a payment is missed
/// - no conventional APR (APR calculation is disabled for these products)
///
/// > **Regulatory note:** BNPL products in the UK are being brought into FCA regulation (CONC); disclosure
/// > calculations should be designed to accommodate future CONC rule requirements.
module BuyNowPayLater =

    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    /// identifies who bears the cost of offering a 0% product
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type FundingModel =
        /// the retailer bears the cost by paying a merchant service charge to the payment provider;
        /// the consumer pays exactly the purchase price in equal instalments
        | RetailerFunded
        /// the lender or payment provider absorbs the cost of the 0% offer
        | LenderFunded

        /// HTML formatting to display the funding model in a readable format
        member f.Html =
            match f with
            | RetailerFunded -> "retailer-funded"
            | LenderFunded -> "lender-funded"

    /// configuration for a zero-interest instalment product (e.g. "pay in 3" or "pay in 12")
    ///
    /// equal payments are spread over a fixed term; no interest accrues if payments are made on time;
    /// if a payment is missed, PenalInterestRate should be applied from the missed payment date
    [<Struct>]
    type InstalmentConfig = {
        /// the number of equal instalments
        InstalmentCount: int
        /// the unit-period config defining when each instalment is due (e.g. Monthly(1, year, month, day))
        UnitPeriodConfig: UnitPeriod.Config
        /// the interest rate applied if a payment is missed (penal rate);
        /// apply this via AdvancedParameters.InterestConfig.PromotionalRates when a missed payment is detected
        PenalInterestRate: Interest.Rate
        /// identifies who bears the cost of the 0% offer
        FundingModel: FundingModel
    }

    /// configuration for a deferred payment product (e.g. "pay later in 30 days" or "0% for 12 months")
    ///
    /// no payment is due during the deferral period; the full balance (plus any accrued interest)
    /// falls due on the deferral date
    [<Struct>]
    type DeferredConfig = {
        /// the number of days from the start date before any payment is due
        DeferralPeriodDays: int<DurationDay>
        /// whether interest accrues during the deferral period
        ///
        /// - false = true zero-interest deferred (no interest accrues; retailer or lender absorbs the cost)
        /// - true = deferred-interest product (interest accrues at DeferredRate; waived if settled on time
        ///          via a promotional zero-rate in AdvancedParameters; applied retroactively if not)
        AccrueInterestDuringDeferral: bool
        /// the interest rate applicable during the deferral period (only used when AccrueInterestDuringDeferral = true)
        DeferredRate: Interest.Rate
        /// the interest rate applied after the deferral period if the balance remains unpaid
        PostDeferralRate: Interest.Rate
        /// identifies who bears the cost of the 0% offer during the deferral period
        FundingModel: FundingModel
    }

    /// creates the basic parameters for a zero-interest instalment schedule
    ///
    /// the schedule shows equal payments with no interest; APR calculation is disabled
    let instalmentBasicParameters (startDate: Date) (principal: int64<Cent>) (config: InstalmentConfig) : BasicParameters =
        {
            EvaluationDate = startDate
            StartDate = startDate
            Principal = principal
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = config.UnitPeriodConfig
                    ScheduleLength = PaymentCount config.InstalmentCount
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundWith MidpointRounding.AwayFromZero
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Zero
                Cap = Interest.Cap.zero
                Rounding = RoundWith MidpointRounding.AwayFromZero
                AprMethod = Apr.CalculationMethod.Disabled
            }
        }

    /// creates the default advanced parameters for a zero-interest instalment schedule
    ///
    /// note: to apply PenalInterestRate after a missed payment, set a PromotionalRate from the missed
    /// payment date with the rate from InstalmentConfig.PenalInterestRate
    let instalmentAdvancedParameters : AdvancedParameters =
        {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = NoMinimumPayment
                Timeout = 3<DurationDay>
            }
            InterestConfig = {
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = true
        }

    /// creates the full parameters for a zero-interest instalment schedule
    let instalmentParameters (startDate: Date) (principal: int64<Cent>) (config: InstalmentConfig) : Parameters =
        {
            Basic = instalmentBasicParameters startDate principal config
            Advanced = instalmentAdvancedParameters
        }

    /// creates the basic parameters for a deferred payment schedule ("pay later")
    ///
    /// for a deferred-interest product (AccrueInterestDuringDeferral = true), the standard rate is DeferredRate
    /// and the basic schedule will show interest accruing; use the amortisation schedule with the corresponding
    /// advanced parameters (which include a promotional zero-rate during the deferral period) to model the
    /// on-time payment scenario correctly
    let deferredBasicParameters (startDate: Date) (principal: int64<Cent>) (config: DeferredConfig) : BasicParameters =
        let paymentDay = int config.DeferralPeriodDays * 1<OffsetDay>

        {
            EvaluationDate = startDate
            StartDate = startDate
            Principal = principal
            ScheduleConfig =
                CustomSchedule(
                    Map.ofArray [|
                        paymentDay, ScheduledPayment.quick (ValueSome principal) ValueNone
                    |]
                )
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundWith MidpointRounding.AwayFromZero
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate =
                    if config.AccrueInterestDuringDeferral then
                        config.DeferredRate
                    else
                        Interest.Rate.Zero
                Cap = Interest.Cap.zero
                Rounding = RoundWith MidpointRounding.AwayFromZero
                AprMethod = Apr.CalculationMethod.Disabled
            }
        }

    /// creates the advanced parameters for a deferred payment schedule ("pay later")
    ///
    /// for a deferred-interest product (AccrueInterestDuringDeferral = true), a promotional zero-rate is
    /// applied during the deferral period so that the amortisation schedule correctly shows no interest
    /// when the consumer pays on time; if the payment is missed, the DeferredRate applies after the deferral period
    let deferredAdvancedParameters (startDate: Date) (config: DeferredConfig) : AdvancedParameters =
        let promotionalRates =
            if not config.AccrueInterestDuringDeferral then
                [||]
            else
                [|
                    {
                        Interest.PromotionalRate.DateRange = {
                            DateRangeStart = startDate
                            DateRangeEnd = startDate.AddDays(int config.DeferralPeriodDays)
                        }
                        Interest.PromotionalRate.Rate = Interest.Rate.Zero
                    }
                |]

        {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = NoMinimumPayment
                Timeout = 3<DurationDay>
            }
            InterestConfig = {
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = promotionalRates
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = true
        }

    /// creates the full parameters for a deferred payment schedule ("pay later")
    let deferredParameters (startDate: Date) (principal: int64<Cent>) (config: DeferredConfig) : Parameters =
        {
            Basic = deferredBasicParameters startDate principal config
            Advanced = deferredAdvancedParameters startDate config
        }
