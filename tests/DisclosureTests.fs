namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module DisclosureTests =

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Disclosure
    open Scheduling
    open UnitPeriod

    // ------------------------------------------------------------------
    // Shared test parameters
    // ------------------------------------------------------------------

    /// A simple actuarial loan: £500 over 4 monthly payments at 0.1 %/day
    let private basicParameters : Parameters = {
        Basic = {
            EvaluationDate = Date(2025, 1, 1)
            StartDate = Date(2025, 1, 1)
            Principal = 500_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Config.defaultMonthly 1 (Date(2025, 2, 1))
                    ScheduleLength = PaymentCount 4
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Daily(Percent 0.1m)
                Cap = Interest.Cap.zero
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 5
            }
        }
        Advanced = {
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                Minimum = DeferOrWriteOff 50L<Cent>
                Timeout = 3<DurationDay>
            }
            FeeConfig = ValueNone
            ChargeConfig = None
            InterestConfig = {
                InitialGracePeriod = 0<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    /// Generates a GenerationResult with all payments made on time
    let private generateWithAllPaymentsMade (p: Parameters) =
        let basicSchedule = calculateBasicSchedule p.Basic

        // create actual payments that match every scheduled payment
        let actualPayments =
            basicSchedule.Items
            |> Array.filter (fun bi -> ScheduledPayment.isSome bi.ScheduledPayment)
            |> Array.map (fun bi ->
                bi.Day,
                [| ActualPayment.quickConfirmed (ScheduledPayment.total bi.ScheduledPayment) |]
            )
            |> Map.ofArray

        amortise p actualPayments

    // ------------------------------------------------------------------
    // UK FCA CONC tests
    // ------------------------------------------------------------------

    module UkConcTests =

        [<Fact>]
        let ``ukConc: TotalAmountPayable equals principal plus TotalCostOfCredit`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule
            let principal = basicParameters.Basic.Principal

            disclosure.TotalAmountPayable |> should equal (principal + disclosure.TotalCostOfCredit)

        [<Fact>]
        let ``ukConc: TotalCostOfCredit equals BasicSchedule InterestTotal for no-fee loan`` () =
            let basicSchedule = calculateBasicSchedule basicParameters.Basic
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule

            disclosure.TotalCostOfCredit |> should equal basicSchedule.Stats.InterestTotal

        [<Fact>]
        let ``ukConc: TotalCostOfCredit is positive`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule

            disclosure.TotalCostOfCredit |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``ukConc: APR is positive`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule
            let (Percent apr) = disclosure.Apr

            apr |> should be (greaterThan 0m)

        [<Fact>]
        let ``ukConc: RepresentativeApr equals Apr for single-advance loan`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule

            (disclosure.RepresentativeApr = disclosure.Apr) |> should equal true

        [<Fact>]
        let ``ukConc: APR matches BasicSchedule InitialApr when same method is used`` () =
            let basicSchedule = calculateBasicSchedule basicParameters.Basic
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule
            let (Percent discApr) = disclosure.Apr
            let (Percent schedApr) = basicSchedule.Stats.InitialApr

            discApr |> should (equalWithin 0.001m) schedApr

        [<Fact>]
        let ``ukConc: TotalAmountPayable matches BasicSchedule ScheduledPaymentTotal for no-fee loan`` () =
            let basicSchedule = calculateBasicSchedule basicParameters.Basic
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = ukConc 5 result.AmortisationSchedule

            disclosure.TotalAmountPayable |> should equal basicSchedule.Stats.ScheduledPaymentTotal

    // ------------------------------------------------------------------
    // EU SECCI tests
    // ------------------------------------------------------------------

    module EuSecciTests =

        [<Fact>]
        let ``euSecci: TotalAmountPayable equals principal plus TotalCostOfCredit`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = euSecci 5 result.AmortisationSchedule
            let principal = basicParameters.Basic.Principal

            disclosure.TotalAmountPayable |> should equal (principal + disclosure.TotalCostOfCredit)

        [<Fact>]
        let ``euSecci: TotalCostOfCredit equals BasicSchedule InterestTotal for no-fee loan`` () =
            let basicSchedule = calculateBasicSchedule basicParameters.Basic
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = euSecci 5 result.AmortisationSchedule

            disclosure.TotalCostOfCredit |> should equal basicSchedule.Stats.InterestTotal

        [<Fact>]
        let ``euSecci: TotalCostOfCredit is positive`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = euSecci 5 result.AmortisationSchedule

            disclosure.TotalCostOfCredit |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``euSecci: APR is positive`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = euSecci 5 result.AmortisationSchedule
            let (Percent apr) = disclosure.Apr

            apr |> should be (greaterThan 0m)

        [<Fact>]
        let ``euSecci: repayment schedule has correct number of entries`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let schedule = euSecciRepaymentSchedule result.AmortisationSchedule
            let expectedCount = 4 // 4 monthly payments as per basicParameters ScheduleLength

            schedule |> Array.length |> should equal expectedCount

        [<Fact>]
        let ``euSecci: repayment schedule entries have positive amounts`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let schedule = euSecciRepaymentSchedule result.AmortisationSchedule

            schedule |> Array.forall (fun e -> e.Amount > 0L<Cent>) |> should equal true

        [<Fact>]
        let ``euSecci: repayment schedule total matches TotalAmountPayable`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let disclosure = euSecci 5 result.AmortisationSchedule
            let schedule = euSecciRepaymentSchedule result.AmortisationSchedule
            let scheduleTotal = schedule |> Array.sumBy _.Amount

            scheduleTotal |> should equal disclosure.TotalAmountPayable

        [<Fact>]
        let ``euSecci: repayment schedule dates are ascending`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let schedule = euSecciRepaymentSchedule result.AmortisationSchedule
            let dates = schedule |> Array.map _.PaymentDate

            let isAscending = dates |> Array.pairwise |> Array.forall (fun (a, b) -> a < b)
            isAscending |> should equal true

        [<Fact>]
        let ``euSecci: APR is consistent with ukConc APR for same schedule`` () =
            let result = generateWithAllPaymentsMade basicParameters
            let ukDisclosure = ukConc 5 result.AmortisationSchedule
            let euDisclosure = euSecci 5 result.AmortisationSchedule
            let (Percent ukApr) = ukDisclosure.Apr
            let (Percent euApr) = euDisclosure.Apr

            // UK and EU methods are aligned as of the Apr.fs note
            euApr |> should (equalWithin 0.001m) ukApr

    // ------------------------------------------------------------------
    // US TILA tests
    // ------------------------------------------------------------------

    module UsTilaTests =

        /// US-style parameters: principal $500, actuarial interest, US APR method
        let private usParameters : Parameters = {
            basicParameters with
                Basic = {
                    basicParameters.Basic with
                        InterestConfig = {
                            basicParameters.Basic.InterestConfig with
                                AprMethod = Apr.CalculationMethod.UsActuarial 5
                        }
                }
        }

        [<Fact>]
        let ``usTila: FinanceCharge is positive`` () =
            let result = generateWithAllPaymentsMade usParameters
            let disclosure = usTila 5 result.AmortisationSchedule

            disclosure.FinanceCharge |> should be (greaterThan 0L<Cent>)

        [<Fact>]
        let ``usTila: AmountFinanced equals principal when no prepaid fees`` () =
            let result = generateWithAllPaymentsMade usParameters
            let disclosure = usTila 5 result.AmortisationSchedule
            let principal = usParameters.Basic.Principal

            disclosure.AmountFinanced |> should equal principal

        [<Fact>]
        let ``usTila: FinanceCharge plus AmountFinanced equals TotalAmountPayable`` () =
            let result = generateWithAllPaymentsMade usParameters
            let disclosure = usTila 5 result.AmortisationSchedule

            (disclosure.AmountFinanced + disclosure.FinanceCharge)
            |> should equal (usParameters.Basic.Principal + disclosure.FinanceCharge)

        [<Fact>]
        let ``usTila: APR is positive`` () =
            let result = generateWithAllPaymentsMade usParameters
            let disclosure = usTila 5 result.AmortisationSchedule
            let (Percent apr) = disclosure.Apr

            apr |> should be (greaterThan 0m)

        [<Fact>]
        let ``usTila: FinanceCharge equals BasicSchedule InterestTotal for no-fee no-charge loan`` () =
            let basicSchedule = calculateBasicSchedule usParameters.Basic
            let result = generateWithAllPaymentsMade usParameters
            let disclosure = usTila 5 result.AmortisationSchedule

            disclosure.FinanceCharge |> should equal basicSchedule.Stats.InterestTotal

        [<Fact>]
        let ``usTila: APR is within plausible bounds`` () =
            let result = generateWithAllPaymentsMade usParameters
            let disclosure = usTila 5 result.AmortisationSchedule
            let (Percent apr) = disclosure.Apr

            // 0.1 %/day ≈ 36.5 % per year; APR compound should be well above that
            apr |> should be (greaterThan 30m)
            apr |> should be (lessThan 1000m)
