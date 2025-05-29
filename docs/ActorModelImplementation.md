# Actor Model Implementation - Amortisation3.fs

## Overview

The `Amortisation3.fs` module implements a reactive programming approach to financial amortisation calculations using the Actor Model pattern. This design transforms the imperative calculation logic found in the original `Amortisation.fs` into a message-driven, actor-based system that provides better separation of concerns, improved testability, and enhanced modularity.

## Architecture

### Core Design Principles

1. **Separation of Concerns**: Each financial component (principal, interest, fees, charges) is managed by a dedicated actor
2. **Message-Driven Communication**: Actors communicate exclusively through typed messages
3. **Isolated State Management**: Each actor maintains its own state independently
4. **Reactive Programming**: The system responds to events and state changes reactively
5. **Async/Await Pattern**: Non-blocking operations throughout the system

### Actor Hierarchy

```
CoordinatorActor (Orchestrator)
├── PrincipalActor (Principal balance management)
├── InterestActor (Interest calculation and accrual)
├── FeeActor (Fee balance and rebate calculations)
└── ChargesActor (Penalty charges management)
```

## Actor Types and Responsibilities

### 1. Principal Actor (`PrincipalMessage`)
**Responsibility**: Manages the principal balance throughout the loan lifecycle

**Messages**:
- `ApplyPrincipalPayment`: Reduces principal balance when payments are applied
- `GetPrincipalBalance`: Returns current principal balance
- `AddAdvance`: Adds new principal advances (typically day 0)

**State**: `PrincipalState`
- `Balance`: Current principal balance
- `LastUpdateDay`: Last day the balance was modified
- `TotalAdvances`: Total advances made to date

### 2. Interest Actor (`InterestMessage`)
**Responsibility**: Handles interest accrual, calculation, and payment application

**Messages**:
- `AccrueInterest`: Calculates and adds daily interest based on principal balance
- `ApplyInterestPayment`: Applies payment portions to interest balance
- `GetInterestBalance`: Returns current interest balance
- `CapInterest`: Applies interest cap limitations

**State**: `InterestState`
- `Balance`: Current interest balance (decimal for precision)
- `AccruedToday`: Interest accrued on current day
- `LastCalculationDay`: Last day interest was calculated
- `CumulativeInterest`: Total interest accrued to date
- `TotalCap`: Applied interest cap settings

### 3. Fee Actor (`FeeMessage`)
**Responsibility**: Manages fee balances and calculates rebates for early settlement

**Messages**:
- `ApplyFeePayment`: Applies payment portions to fee balance
- `GetFeeBalance`: Returns current fee balance
- `CalculateFeeRebate`: Computes rebate amount for settlement scenarios

**State**: `FeeState`
- `Balance`: Current fee balance
- `TotalFee`: Original total fee amount
- `PaidToDate`: Total fee payments made

### 4. Charges Actor (`ChargesMessage`)
**Responsibility**: Handles penalty charges and late payment fees

**Messages**:
- `AddCharge`: Adds new penalty charges to the balance
- `ApplyChargesPayment`: Applies payment portions to charges
- `GetChargesBalance`: Returns current charges balance

**State**: `ChargesState`
- `Balance`: Current charges balance
- `Charges`: List of all applied charges
- `TotalCharges`: Total charges applied to date

### 5. Coordinator Actor (`CoordinatorMessage`)
**Responsibility**: Orchestrates interactions between all actors and maintains the overall schedule

**Messages**:
- `ProcessPaymentDay`: Coordinates payment processing for a specific day
- `GetScheduleItem`: Retrieves schedule information for a specific day
- `GenerateSettlement`: Calculates settlement figures
- `GetFinalStats`: Computes final statistics for the schedule

## Key Features

### 1. Payment Apportionment
The system follows the standard payment waterfall:
1. **Charges** (highest priority)
2. **Interest** 
3. **Fees**
4. **Principal** (lowest priority)

This is implemented through the `calculatePaymentApportionment` function which coordinates between actors.

### 2. Reactive Extensions
The implementation includes optional reactive features:
- **State Change Monitoring**: Observable streams for tracking actor state changes
- **Event Notifications**: Real-time notifications of balance modifications
- **Debugging Support**: Enhanced observability for system behavior

### 3. Type Safety
All actor communications use F# discriminated unions, providing:
- Compile-time message validation
- Pattern matching for message handling
- Strong typing across the entire system

## Comparison with Original Implementation

| Aspect | Original (Amortisation.fs) | Actor Model (Amortisation3.fs) |
|--------|---------------------------|--------------------------------|
| **Architecture** | Imperative, procedural | Message-driven, actor-based |
| **State Management** | Mutable variables in scan function | Isolated actor state |
| **Concurrency** | Sequential processing | Potentially concurrent actors |
| **Testability** | Monolithic function testing | Individual actor unit testing |
| **Modularity** | Single large calculation function | Separate actors per concern |
| **Extensibility** | Requires modifying core function | Add new actors or messages |
| **Debugging** | Complex state tracking | Clear actor message flows |

## Usage Examples

### Basic Usage
```fsharp
open Amortisation3

let parameters = // ... parameter setup
let actualPayments = // ... payment data

// Async API
let result = amortise parameters actualPayments |> Async.RunSynchronously

// Sync API for compatibility
let result = amortiseSync parameters actualPayments
```

### Individual Actor Testing
```fsharp
// Create and test principal actor
let principalState = createPrincipalState 1500_00L<Cent>
let principalActor = createEnhancedPrincipalActor principalState

// Apply a payment
principalActor.Post(ApplyPrincipalPayment(100_00L<Cent>, 30<OffsetDay>))
```

### Reactive Monitoring
```fsharp
// Create observable actor with state change notifications
let stateChangeEvent = new Event<ActorStateChange<PrincipalState>>()
let observableActor = createObservableActor initialState "PrincipalActor" stateChangeEvent

// Subscribe to changes
stateChangeEvent.Publish.Add(fun change ->
    printfn "Balance changed from %A to %A" change.OldState.Balance change.NewState.Balance
)
```

## Implementation Benefits

### 1. **Improved Testability**
Each actor can be tested independently with specific message scenarios, making unit testing more focused and comprehensive.

### 2. **Better Separation of Concerns**
Financial calculations are cleanly separated by responsibility, making the codebase easier to understand and maintain.

### 3. **Enhanced Modularity**
New financial products or calculation methods can be added by creating new actors or extending existing message types.

### 4. **Potential for Concurrency**
While the current implementation is synchronous, the actor model provides a foundation for future concurrent processing.

### 5. **Reactive Programming Support**
Built-in support for reactive programming patterns enables real-time monitoring and event-driven extensions.

## Future Enhancements

1. **Persistence**: Add actor state persistence for long-running calculations
2. **Clustering**: Distribute actors across multiple processes for scalability
3. **Backpressure**: Implement flow control for high-throughput scenarios
4. **Saga Pattern**: Add transaction coordination for complex financial operations
5. **Actor Supervision**: Implement supervisor hierarchies for fault tolerance

## Conclusion

The Actor Model implementation in `Amortisation3.fs` demonstrates how reactive programming patterns can be applied to financial calculations. While maintaining compatibility with the existing API, it provides a more modular, testable, and extensible foundation for future development.

The transformation from imperative to reactive programming shows the benefits of applying modern software architecture patterns to financial domain problems, resulting in more maintainable and robust code.
