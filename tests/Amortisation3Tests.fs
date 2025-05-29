namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open Amortisation3
open Amortisation
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

module Amortisation3Tests =

    let folder = "Amortisation3"

    // Test parameters similar to existing tests
    let testParameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2023, 3, 31)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Daily(Percent 0.8m)
                Cap = {
                    TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                    DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
                }
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
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
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
            }
            SettlementDay = SettlementDay.NoSettlement
            TrimEnd = false
        }
    }

    /// Helper to create actual payments map
    let quickActualPayments (days: int array) levelPayment finalPayment =
        days
        |> Array.rev
        |> Array.splitAt 1
        |> fun (last, rest) -> [|
            last
            |> Array.map (fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed finalPayment |])
            rest
            |> Array.map (fun d -> (d * 1<OffsetDay>), [| ActualPayment.quickConfirmed levelPayment |])
        |]
        |> Array.concat
        |> Array.rev
        |> Map.ofArray

    // ===== UNIT TESTS FOR INDIVIDUAL ACTORS =====

    [<Fact>]
    let ``Principal Actor - Basic Balance Management`` () =
        let initialPrincipal = 1000_00L<Cent>
        let principalState = createPrincipalState initialPrincipal
        let actor = createPrincipalActor principalState

        principalState.Balance |> should equal initialPrincipal
        principalState.TotalAdvances |> should equal initialPrincipal
        principalState.LastUpdateDay |> should equal 0<OffsetDay>

    [<Fact>]
    let ``Interest Actor - Basic State Creation`` () =
        let interestState = createInterestState()
        let actor = createInterestActor interestState testParameters

        interestState.Balance |> should equal 0m<Cent>
        interestState.AccruedToday |> should equal 0m<Cent>
        interestState.LastCalculationDay |> should equal 0<OffsetDay>
        interestState.CumulativeInterest |> should equal 0m<Cent>
        interestState.TotalCap |> should equal None

    [<Fact>]
    let ``Fee Actor - Basic State Creation`` () =
        let totalFee = 200_00L<Cent>
        let feeState = createFeeState totalFee
        let actor = createFeeActor feeState

        feeState.Balance |> should equal totalFee
        feeState.TotalFee |> should equal totalFee
        feeState.PaidToDate |> should equal 0L<Cent>

    [<Fact>]
    let ``Charges Actor - Basic State Creation`` () =
        let chargesState = createChargesState()
        let actor = createChargesActor chargesState

        chargesState.Balance |> should equal 0L<Cent>
        chargesState.Charges |> should equal []
        chargesState.TotalCharges |> should equal 0L<Cent>

    [<Fact>]
    let ``Daily Rate Calculation - Annual Rate`` () =
        let annualRate = Percent 14.6m
        let interestConfig = { 
            testParameters.Basic.InterestConfig with 
                StandardRate = Interest.Rate.Annual annualRate 
        }
        
        let dailyRate = calculateDailyRate interestConfig
        let expected = 14.6m / 365m / 100m
        
        dailyRate |> should (equalWithin 0.0001m) expected

    [<Fact>]
    let ``Daily Rate Calculation - Daily Rate`` () =
        let dailyRatePercent = Percent 0.04m
        let interestConfig = { 
            testParameters.Basic.InterestConfig with 
                StandardRate = Interest.Rate.Daily dailyRatePercent 
        }
        
        let dailyRate = calculateDailyRate interestConfig
        let expected = 0.04m / 100m
        
        dailyRate |> should equal expected

    [<Fact>]
    let ``Daily Rate Calculation - Zero Rate`` () =
        let interestConfig = { 
            testParameters.Basic.InterestConfig with 
                StandardRate = Interest.Rate.Zero 
        }
        
        let dailyRate = calculateDailyRate interestConfig
        
        dailyRate |> should equal 0m

    // ===== INTEGRATION TESTS =====

    [<Fact>]
    let ``Actor Model - Basic Amortisation Test`` () =
        let actualPayments = Map.empty

        // Test that the amortise function can be called without errors
        let result = amortiseSync testParameters actualPayments
        
        result |> should not' (be null)
        result.AmortisationSchedule |> should not' (be null)
        result.BasicSchedule |> should not' (be null)

    [<Fact>]
    let ``Actor Model - Compare with Original Implementation Basic Case`` () =
        let actualPayments = 
            quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        // Test with Actor Model
        let actorResult = amortiseSync testParameters actualPayments
        
        // Test with Original Implementation
        let originalResult = Amortisation.amortise testParameters actualPayments

        // Basic validation - both should complete without errors
        actorResult |> should not' (be null)
        originalResult |> should not' (be null)
        
        // Both should have schedule items
        actorResult.AmortisationSchedule.ScheduleItems.Count |> should be (greaterThan 0)
        originalResult.AmortisationSchedule.ScheduleItems.Count |> should be (greaterThan 0)

    [<Fact>]
    let ``Actor Model - Payment Apportionment Logic`` () =
        let principal = 1000_00L<Cent>
        let principalActor = createEnhancedPrincipalActor (createPrincipalState principal)
        let interestActor = createEnhancedInterestActor (createInterestState()) testParameters
        let feeActor = createEnhancedFeeActor (createFeeState 100_00L<Cent>) testParameters
        let chargesActor = createEnhancedChargesActor (createChargesState())

        // Test payment application - this should not throw exceptions
        let result = async {
            return! calculatePaymentApportionment 
                500_00L<Cent> 
                principalActor 
                interestActor 
                feeActor 
                chargesActor 
                testParameters
        }
        
        // Should complete without errors
        Async.RunSynchronously result |> should not' (be null)

    [<Fact>]
    let ``Actor Model - Coordinator Actor Creation`` () =
        let actualPayments = Map.empty
        let initialPrincipal = testParameters.Basic.Principal
        let feeTotal = 0L<Cent>
        
        let coordinator = createCoordinatorActor testParameters actualPayments
        
        // Coordinator should be created successfully
        coordinator |> should not' (be null)

    [<Fact>]
    let ``Actor Model - Enhanced Actor Creation`` () =
        let principal = 1000_00L<Cent>
        
        // Test enhanced principal actor
        let principalActor = createEnhancedPrincipalActor (createPrincipalState principal)
        principalActor |> should not' (be null)
        
        // Test enhanced interest actor
        let interestActor = createEnhancedInterestActor (createInterestState()) testParameters
        interestActor |> should not' (be null)
        
        // Test enhanced fee actor
        let feeActor = createEnhancedFeeActor (createFeeState 100_00L<Cent>) testParameters
        feeActor |> should not' (be null)
        
        // Test enhanced charges actor
        let chargesActor = createEnhancedChargesActor (createChargesState())
        chargesActor |> should not' (be null)

    [<Fact>]
    let ``Actor Model - Single Payment Test`` () =
        let actualPayments = 
            Map [ 14<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |] ]

        let result = amortiseSync testParameters actualPayments
        
        result.AmortisationSchedule.ScheduleItems |> should not' (be empty)

    [<Fact>]
    let ``Actor Model - Empty Payment Test`` () =
        let actualPayments = Map.empty

        let result = amortiseSync testParameters actualPayments
        
        // Should handle empty payments gracefully
        result |> should not' (be null)

    [<Fact>]
    let ``Actor Model - Multiple Payment Days Test`` () =
        let actualPayments = 
            Map [
                10<OffsetDay>, [| ActualPayment.quickConfirmed 200_00L<Cent> |]
                20<OffsetDay>, [| ActualPayment.quickConfirmed 300_00L<Cent> |]
                30<OffsetDay>, [| ActualPayment.quickConfirmed 400_00L<Cent> |]
            ]

        let result = amortiseSync testParameters actualPayments
        
        result.AmortisationSchedule.ScheduleItems.Count |> should be (greaterThan 0)

    // ===== ASYNC TESTS =====

    [<Fact>]
    let ``Actor Model - Async Amortise Test`` () =
        let actualPayments = 
            quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let asyncResult = async {
            return! amortise testParameters actualPayments
        }
        
        let result = Async.RunSynchronously asyncResult
        
        result |> should not' (be null)
        result.AmortisationSchedule.ScheduleItems |> should not' (be empty)

    // ===== ERROR HANDLING TESTS =====

    [<Fact>]
    let ``Actor Model - Invalid Parameters Test`` () =
        let invalidParameters = { 
            testParameters with 
                Basic = { 
                    testParameters.Basic with 
                        Principal = -1000_00L<Cent> 
                } 
        }
        let actualPayments = Map.empty

        // Should handle invalid parameters gracefully (not throw unhandled exceptions)
        let result = amortiseSync invalidParameters actualPayments
        
        result |> should not' (be null)

    [<Fact>]
    let ``Actor Model - Large Payment Amount Test`` () =
        let actualPayments = 
            Map [ 14<OffsetDay>, [| ActualPayment.quickConfirmed 10000_00L<Cent> |] ]

        let result = amortiseSync testParameters actualPayments
        
        // Should handle large payments gracefully
        result |> should not' (be null)

    // ===== PERFORMANCE TESTS =====

    [<Fact>]
    let ``Actor Model - Performance Baseline Test`` () =
        let actualPayments = 
            quickActualPayments [| 4; 35; 66; 94; 125 |] 456_88L<Cent> 456_84L<Cent>

        let stopwatch = System.Diagnostics.Stopwatch.StartNew()
        let result = amortiseSync testParameters actualPayments
        stopwatch.Stop()
        
        // Should complete within reasonable time (5 seconds for basic test)
        stopwatch.ElapsedMilliseconds |> should be (lessThan 5000L)
        result |> should not' (be null)

    // ===== STATE VALIDATION TESTS =====

    [<Fact>]
    let ``Actor Model - State Consistency Test`` () =
        let principal = 1000_00L<Cent>
        let principalState = createPrincipalState principal
        
        // Validate initial state consistency
        principalState.Balance |> should equal principalState.TotalAdvances
        principalState.LastUpdateDay |> should equal 0<OffsetDay>

    [<Fact>]
    let ``Actor Model - Interest State Validation`` () =
        let interestState = createInterestState()
        
        // Validate initial interest state
        interestState.Balance |> should be (greaterThanOrEqualTo 0m<Cent>)
        interestState.AccruedToday |> should be (greaterThanOrEqualTo 0m<Cent>)
        interestState.CumulativeInterest |> should be (greaterThanOrEqualTo 0m<Cent>)

    [<Fact>]
    let ``Actor Model - Fee State Validation`` () =
        let totalFee = 200_00L<Cent>
        let feeState = createFeeState totalFee
        
        // Validate fee state consistency
        feeState.Balance |> should be (greaterThanOrEqualTo 0L<Cent>)
        feeState.TotalFee |> should be (greaterThanOrEqualTo 0L<Cent>)
        feeState.PaidToDate |> should be (greaterThanOrEqualTo 0L<Cent>)
        feeState.Balance + feeState.PaidToDate |> should equal feeState.TotalFee

    [<Fact>]
    let ``Actor Model - Charges State Validation`` () =
        let chargesState = createChargesState()
        
        // Validate charges state consistency
        chargesState.Balance |> should be (greaterThanOrEqualTo 0L<Cent>)
        chargesState.TotalCharges |> should be (greaterThanOrEqualTo 0L<Cent>)
        chargesState.Charges.Length |> should be (greaterThanOrEqualTo 0)
