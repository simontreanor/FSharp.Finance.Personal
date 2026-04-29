namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module BuyNowPayLaterTests =

    let folder = "BuyNowPayLater"

    open Amortisation
    open AppliedPayment
    open BuyNowPayLater
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    // ── helpers ──────────────────────────────────────────────────────────────

    let startDate = Date(2024, 3, 1)
    let principal = 300_00L<Cent>

    // ── Apr.CalculationMethod.Disabled ────────────────────────────────────────

    [<Fact>]
    let AprDisabledReturnsImpossible () =
        // Apr.calculate should return Solution.Impossible when Disabled
        let result =
            Apr.calculate
                Apr.CalculationMethod.Disabled
                principal
                startDate
                [|
                    {
                        Apr.TransferType = Apr.Payment
                        Apr.TransferDate = startDate.AddDays 30
                        Apr.Value = principal
                    }
                |]

        result |> should equal Solution.Impossible

    [<Fact>]
    let AprDisabledReturnsZeroPercent () =
        // Apr.toPercent should return Percent 0m when Disabled (solution is Impossible)
        let result = Apr.toPercent Apr.CalculationMethod.Disabled Solution.Impossible
        result |> should equal (Percent 0m)

    [<Fact>]
    let AprDisabledHtmlLabel () =
        // Apr.CalculationMethod.Disabled should render as "disabled"
        let html = Apr.CalculationMethod.Disabled.Html
        html |> should equal "disabled"

    // ── zero-interest instalment product ─────────────────────────────────────

    let payIn3Config: InstalmentConfig = {
        InstalmentCount = 3
        UnitPeriodConfig = Monthly(1, 2024, 4, 1)
        PenalInterestRate = Interest.Rate.Annual(Percent 39.99m)
        FundingModel = FundingModel.RetailerFunded
    }

    [<Fact>]
    let InstalmentBasicParameters_HasDisabledApr () =
        let bp = instalmentBasicParameters startDate principal payIn3Config
        bp.InterestConfig.AprMethod |> should equal Apr.CalculationMethod.Disabled

    [<Fact>]
    let InstalmentBasicParameters_HasZeroInterestRate () =
        let bp = instalmentBasicParameters startDate principal payIn3Config
        (bp.InterestConfig.StandardRate = Interest.Rate.Zero) |> should equal true

    [<Fact>]
    let InstalmentBasicParameters_CorrectPaymentCount () =
        let bp = instalmentBasicParameters startDate principal payIn3Config

        let schedule = calculateBasicSchedule bp

        schedule.Items
        |> Array.filter (fun i -> ScheduledPayment.isSome i.ScheduledPayment)
        |> Array.length
        |> should equal 3

    [<Fact>]
    let InstalmentBasicParameters_ZeroInterestTotal () =
        // zero-interest instalment: total interest must be zero
        let bp = instalmentBasicParameters startDate principal payIn3Config
        let schedule = calculateBasicSchedule bp
        schedule.Stats.InterestTotal |> should equal 0L<Cent>

    [<Fact>]
    let InstalmentBasicParameters_PrincipalMatchesAdvance () =
        // total principal paid must equal the original advance
        let bp = instalmentBasicParameters startDate principal payIn3Config
        let schedule = calculateBasicSchedule bp
        schedule.Stats.PrincipalTotal |> should equal principal

    [<Fact>]
    let InstalmentBasicParameters_InitialAprIsZero () =
        // with Disabled APR method, InitialApr must be Percent 0m
        let bp = instalmentBasicParameters startDate principal payIn3Config
        let schedule = calculateBasicSchedule bp
        schedule.Stats.InitialApr |> should equal (Percent 0m)

    [<Fact>]
    let InstalmentParameters_AdvancedHasNoPromotionalRates () =
        // default advanced parameters: no promotional rates
        instalmentAdvancedParameters.InterestConfig.PromotionalRates
        |> should equal [||]

    // ── "pay in 12" variant ───────────────────────────────────────────────────

    [<Fact>]
    let PayIn12_TwelveEqualPayments () =
        let config: InstalmentConfig = {
            InstalmentCount = 12
            UnitPeriodConfig = Monthly(1, 2024, 4, 1)
            PenalInterestRate = Interest.Rate.Annual(Percent 29.9m)
            FundingModel = FundingModel.LenderFunded
        }

        let bp = instalmentBasicParameters startDate principal config
        let schedule = calculateBasicSchedule bp

        let paymentCount =
            schedule.Items
            |> Array.filter (fun i -> ScheduledPayment.isSome i.ScheduledPayment)
            |> Array.length

        paymentCount |> should equal 12
        schedule.Stats.InterestTotal |> should equal 0L<Cent>
        schedule.Stats.PrincipalTotal |> should equal principal

    // ── deferred payment product (true zero-interest) ─────────────────────────

    let payLater30Config: DeferredConfig = {
        DeferralPeriodDays = 30<DurationDay>
        AccrueInterestDuringDeferral = false
        DeferredRate = Interest.Rate.Zero
        PostDeferralRate = Interest.Rate.Annual(Percent 39.99m)
        FundingModel = FundingModel.RetailerFunded
    }

    [<Fact>]
    let DeferredBasicParameters_HasDisabledApr () =
        let bp = deferredBasicParameters startDate principal payLater30Config
        bp.InterestConfig.AprMethod |> should equal Apr.CalculationMethod.Disabled

    [<Fact>]
    let DeferredBasicParameters_HasZeroStandardRate () =
        let bp = deferredBasicParameters startDate principal payLater30Config
        (bp.InterestConfig.StandardRate = Interest.Rate.Zero) |> should equal true

    [<Fact>]
    let DeferredBasicParameters_SinglePaymentOnDeferralDate () =
        let bp = deferredBasicParameters startDate principal payLater30Config
        let schedule = calculateBasicSchedule bp

        let payments =
            schedule.Items
            |> Array.filter (fun i -> ScheduledPayment.isSome i.ScheduledPayment)

        payments |> Array.length |> should equal 1
        payments[0].Day |> should equal 30<OffsetDay>

    [<Fact>]
    let DeferredBasicParameters_ZeroInterestNoAccrual () =
        let bp = deferredBasicParameters startDate principal payLater30Config
        let schedule = calculateBasicSchedule bp
        schedule.Stats.InterestTotal |> should equal 0L<Cent>
        schedule.Stats.PrincipalTotal |> should equal principal

    [<Fact>]
    let DeferredAdvancedParameters_NoPromotionalRatesForTrueZero () =
        // no promotional rates needed when AccrueInterestDuringDeferral = false
        let ap = deferredAdvancedParameters startDate payLater30Config
        ap.InterestConfig.PromotionalRates |> should equal [||]

    // ── deferred-interest product ─────────────────────────────────────────────

    let deferredInterest90Config: DeferredConfig = {
        DeferralPeriodDays = 90<DurationDay>
        AccrueInterestDuringDeferral = true
        DeferredRate = Interest.Rate.Annual(Percent 34.9m)
        PostDeferralRate = Interest.Rate.Annual(Percent 34.9m)
        FundingModel = FundingModel.LenderFunded
    }

    [<Fact>]
    let DeferredInterest_BasicParams_UsesDeferredRate () =
        let bp = deferredBasicParameters startDate principal deferredInterest90Config
        (bp.InterestConfig.StandardRate = Interest.Rate.Annual(Percent 34.9m)) |> should equal true

    [<Fact>]
    let DeferredInterest_BasicParams_HasDisabledApr () =
        let bp = deferredBasicParameters startDate principal deferredInterest90Config
        bp.InterestConfig.AprMethod |> should equal Apr.CalculationMethod.Disabled

    [<Fact>]
    let DeferredInterest_AdvancedParams_HasPromotionalZeroRate () =
        // deferred-interest product: promotional zero-rate covers the whole deferral period
        // (including the payment day, so the consumer pays exactly the purchase price on time)
        let ap = deferredAdvancedParameters startDate deferredInterest90Config

        ap.InterestConfig.PromotionalRates |> Array.length |> should equal 1

        let promo = ap.InterestConfig.PromotionalRates[0]
        (promo.Rate = Interest.Rate.Zero) |> should equal true
        promo.DateRange.DateRangeStart |> should equal startDate
        promo.DateRange.DateRangeEnd |> should equal (startDate.AddDays 90)

    [<Fact>]
    let DeferredInterest_AmortisedOnTime_ZeroInterest () =
        // when paid on time the amortisation schedule shows zero net interest (promotional rate covers deferral)
        let p = deferredParameters startDate principal deferredInterest90Config

        let actualPayments =
            Map.ofArray [|
                90<OffsetDay>, [| ActualPayment.quickConfirmed principal |]
            |]

        let schedules = amortise p actualPayments

        let lastItem =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd

        lastItem.PrincipalBalance |> should equal 0L<Cent>
        lastItem.InterestBalance |> should equal 0m<Cent>

    // ── FundingModel ─────────────────────────────────────────────────────────

    [<Fact>]
    let FundingModel_RetailerFunded_HtmlLabel () =
        FundingModel.RetailerFunded.Html |> should equal "retailer-funded"

    [<Fact>]
    let FundingModel_LenderFunded_HtmlLabel () =
        FundingModel.LenderFunded.Html |> should equal "lender-funded"

    // ── full round-trip: instalment schedule amortisation ────────────────────

    [<Fact>]
    let InstalmentAmortisation_AllPaymentsOnTime_ClosedBalance () =
        let p = instalmentParameters startDate principal payIn3Config

        let actualPayments =
            Map.ofArray [|
                31<OffsetDay>, [| ActualPayment.quickConfirmed 100_00L<Cent> |]
                62<OffsetDay>, [| ActualPayment.quickConfirmed 100_00L<Cent> |]
                92<OffsetDay>, [| ActualPayment.quickConfirmed 100_00L<Cent> |]
            |]

        let schedules = amortise p actualPayments

        let lastItem =
            schedules.AmortisationSchedule.ScheduleItems
            |> Map.maxKeyValue
            |> snd

        lastItem.PrincipalBalance |> should equal 0L<Cent>
        lastItem.BalanceStatus |> should equal ClosedBalance
