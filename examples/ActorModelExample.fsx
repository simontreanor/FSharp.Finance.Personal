// Simple test example for Amortisation3.fs (Actor Model implementation)
// This demonstrates how to use the new Actor Model approach

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Amortisation3
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

// Example parameters similar to the existing examples
let parameters: Parameters = {
    Basic = {
        EvaluationDate = Date(2023, 4, 1)
        StartDate = Date(2022, 11, 26)
        Principal = 1500_00L<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                PaymentCount = 5
                MaxDuration = Duration.Unlimited
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

// Example actual payments
let actualPayments =
    Map [
        4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        35<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        66<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
    ]

// Test the Actor Model implementation
let testActorModel () =
    async {
        printfn "Testing Actor Model Amortisation Implementation..."

        // Use the synchronous API for this example
        let result = amortiseSync parameters actualPayments

        printfn "Amortisation calculation completed using Actor Model approach!"
        printfn "Schedule items count: %d" (result.AmortisationSchedule.ScheduleItems.Count)
        printfn "Final balance status: %A" result.AmortisationSchedule.FinalStats.FinalBalanceStatus

        return result
    }

// Example of creating individual actors for testing
let testIndividualActors () =
    async {
        printfn "\nTesting Individual Actors..."

        // Create actor instances
        let principalState = createPrincipalState 1500_00L<Cent>
        let principalActor = createEnhancedPrincipalActor principalState

        let interestState = createInterestState ()
        let interestActor = createEnhancedInterestActor interestState parameters

        let feeState = createFeeState 0L<Cent>
        let feeActor = createEnhancedFeeActor feeState parameters

        let chargesState = createChargesState ()
        let chargesActor = createEnhancedChargesActor chargesState

        // Test principal payment
        principalActor.Post(ApplyPrincipalPayment(100_00L<Cent>, 30<OffsetDay>))

        // Test interest accrual
        interestActor.Post(AccrueInterest(30, 0.008m, 1500_00L<Cent>))

        // Test interest payment
        interestActor.Post(ApplyInterestPayment(50_00L<Cent>))

        printfn "Individual actor tests completed!"

        return ()
    }

// Demonstration of reactive features
let testReactiveFeatures () =
    async {
        printfn "\nTesting Reactive Features..."

        // Create state change observable
        let stateChangeEvent = new Event<ActorStateChange<PrincipalState>>()
        let stateChangeObservable = stateChangeEvent.Publish

        // Subscribe to state changes
        stateChangeObservable.Add(fun change ->
            printfn "Actor %s state changed at %A" change.ActorId change.Timestamp
            printfn "  Old balance: %A" change.OldState.Balance
            printfn "  New balance: %A" change.NewState.Balance
        )

        // Create observable actor
        let initialState = createPrincipalState 1000_00L<Cent>

        let observableActor =
            createObservableActor initialState "PrincipalActor" stateChangeEvent

        // Trigger state changes
        let newState = {
            initialState with
                Balance = 900_00L<Cent>
        }

        observableActor.Post(UpdateState newState)

        // Allow time for processing
        do! Async.Sleep(100)

        printfn "Reactive features test completed!"

        return ()
    }

// Main execution
let runTests () =
    async {
        do! testActorModel () |> Async.Ignore
        do! testIndividualActors ()
        do! testReactiveFeatures ()

        printfn "\nAll Actor Model tests completed successfully!"
        printfn "\nKey features of the Actor Model implementation:"
        printfn "1. Separate actors for Principal, Interest, Fees, and Charges"
        printfn "2. Message-based communication between actors"
        printfn "3. Isolated state management for each balance component"
        printfn "4. Coordinator actor for orchestrating complex operations"
        printfn "5. Reactive extensions for monitoring state changes"
        printfn "6. Async/await pattern for non-blocking operations"
        printfn "7. Type-safe message passing with F# discriminated unions"
    }

// Uncomment to run the tests
// runTests() |> Async.RunSynchronously
