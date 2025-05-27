namespace FSharp.Finance.Personal

/// A declarative, event-driven approach to loan amortization schedule calculation
/// Each field in the schedule is computed via event subscriptions, making the
/// system more modular and easier to reason about than procedural approaches.
module Amortisation2 =

    open AppliedPayment
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    // ===== EVENT SYSTEM INFRASTRUCTURE =====
    /// Represents a specific event that can trigger field calculations

    type ScheduleEvent =
        | DayAdvanced of day: int<OffsetDay>
        | WindowAdvanced of window: int * day: int<OffsetDay>
        | PaymentApplied of payment: ActualPayment * day: int<OffsetDay>
        | InterestAccrued of amount: decimal<Cent> * day: int<OffsetDay>
        | ChargeApplied of charge: AppliedCharge * day: int<OffsetDay>
        | BalanceChanged of newBalance: int64<Cent> * day: int<OffsetDay>
        | ScheduleItemCreated of item: Amortisation.ScheduleItem * day: int<OffsetDay>

    /// A field subscription defines how to calculate a specific field based on events
    type FieldSubscription<'T> = {
        /// The name of the field for debugging/logging
        Name: string
        /// Dependencies on other field calculations
        Dependencies: string list
        /// The calculation function that responds to events
        Calculate: ScheduleEvent list -> ScheduleContext -> 'T
        /// Default value when no events have occurred
        Default: 'T
    }

    /// Context passed to field calculations, containing current state
    and ScheduleContext = {
        /// Current day being processed
        CurrentDay: int<OffsetDay>
        /// Previous schedule item (for carrying forward balances)
        PreviousItem: Amortisation.ScheduleItem option
        /// Parameters for the calculation
        Parameters: Parameters
        /// All actual payments made
        ActualPayments: Map<int<OffsetDay>, ActualPayment array>
        /// Scheduled payments
        ScheduledPayments: Map<int<OffsetDay>, ScheduledPayment>
        /// Current event history for this day
        EventHistory: ScheduleEvent list
        /// Previously calculated field values for current item
        CalculatedFields: Map<string, obj>
    }

    /// The declarative schedule configuration
    type ScheduleConfiguration = {
        /// Field subscriptions defining how each field is calculated
        FieldSubscriptions: Map<string, FieldSubscription<obj>>
        /// Event processors that can generate additional events
        EventProcessors: (ScheduleEvent -> ScheduleContext -> ScheduleEvent list) list
    } // ===== SCHEDULE ITEM STRUCTURE =====

    // ===== FIELD CALCULATION HELPERS =====

    /// Helper to get a calculated field value with type safety
    let getFieldValue<'T> (fieldName: string) (context: ScheduleContext) : 'T option =
        context.CalculatedFields
        |> Map.tryFind fieldName
        |> Option.map (fun obj -> obj :?> 'T)

    /// Helper to set a calculated field value
    let setFieldValue (fieldName: string) (value: obj) (context: ScheduleContext) : ScheduleContext = {
        context with
            CalculatedFields = context.CalculatedFields |> Map.add fieldName value
    }

    /// Get the previous balance for a specific field
    let getPreviousBalance<'T> (fieldName: string) (context: ScheduleContext) (defaultValue: 'T) : 'T =
        match context.PreviousItem with
        | Some item ->
            // Use reflection to get the field value from the previous item
            let fieldInfo = typeof<Amortisation.ScheduleItem>.GetProperty(fieldName)
            fieldInfo.GetValue(item) :?> 'T
        | None -> defaultValue

    // ===== FIELD SUBSCRIPTIONS =====    /// Calculate the offset date for the current day
    let offsetDateSubscription = {
        Name = "OffsetDate"
        Dependencies = []
        Calculate = fun events context -> context.Parameters.Basic.StartDate.AddDays(int context.CurrentDay) |> box
        Default = box (Date(1900, 1, 1))
    }

    /// Calculate advances for the current day
    let advancesSubscription = {
        Name = "Advances"
        Dependencies = []
        Calculate =
            fun events context ->
                // Advances typically only occur on day 0
                if context.CurrentDay = 0<OffsetDay> then
                    [| context.Parameters.Basic.Principal |] |> box
                else
                    [||] |> box
        Default = box [||]
    }

    /// Calculate scheduled payment for the current day
    let scheduledPaymentSubscription = {
        Name = "ScheduledPayment"
        Dependencies = []
        Calculate =
            fun events context ->
                context.ScheduledPayments
                |> Map.tryFind context.CurrentDay
                |> Option.defaultValue ScheduledPayment.zero
                |> box
        Default = box ScheduledPayment.zero
    }

    /// Calculate payment window
    let windowSubscription = {
        Name = "Window"
        Dependencies = [ "ScheduledPayment" ]
        Calculate =
            fun events context ->
                let window = context.PreviousItem |> Option.map _.Window |> Option.defaultValue 0
                let scheduledPayment = getFieldValue<ScheduledPayment> "ScheduledPayment" context

                match scheduledPayment with
                | Some _ -> window + 1
                | _ -> window
                |> box
        Default = box 0
    }

    /// Calculate payment due amount
    let paymentDueSubscription = {
        Name = "PaymentDue"
        Dependencies = [ "ScheduledPayment" ]
        Calculate =
            fun events context ->
                getFieldValue<ScheduledPayment> "ScheduledPayment" context
                |> Option.map ScheduledPayment.total
                |> Option.defaultValue 0L<Cent>
                |> box
        Default = box 0L<Cent>
    }

    /// Calculate actual payments for the current day
    let actualPaymentsSubscription = {
        Name = "ActualPayments"
        Dependencies = []
        Calculate =
            fun events context ->
                context.ActualPayments
                |> Map.tryFind context.CurrentDay
                |> Option.defaultValue [||]
                |> box
        Default = box [||]
    }

    /// Calculate net effect of payments
    let netEffectSubscription = {
        Name = "NetEffect"
        Dependencies = [ "ActualPayments"; "PaymentDue" ]
        Calculate =
            fun events context ->
                let actualPayments =
                    getFieldValue<ActualPayment array> "ActualPayments" context
                    |> Option.defaultValue [||]

                let actualTotal = actualPayments |> Array.sumBy ActualPayment.total

                let paymentDue =
                    getFieldValue<int64<Cent>> "PaymentDue" context |> Option.defaultValue 0L<Cent>

                // Net effect is actual payments minus any charges that might be applied
                actualTotal |> box
        Default = box 0L<Cent>
    }

    /// Calculate payment status
    let paymentStatusSubscription = {
        Name = "PaymentStatus"
        Dependencies = [ "ActualPayments"; "PaymentDue"; "NetEffect" ]
        Calculate =
            fun events context ->
                let actualPayments =
                    getFieldValue<ActualPayment array> "ActualPayments" context
                    |> Option.defaultValue [||]

                let paymentDue =
                    getFieldValue<int64<Cent>> "PaymentDue" context |> Option.defaultValue 0L<Cent>

                let netEffect =
                    getFieldValue<int64<Cent>> "NetEffect" context |> Option.defaultValue 0L<Cent>

                match paymentDue, actualPayments with
                | 0L<Cent>, [||] -> PaymentStatus.NoneScheduled
                | _, payments when
                    payments
                    |> Array.exists (fun p ->
                        match p.ActualPaymentStatus with
                        | ActualPaymentStatus.Pending _ -> true
                        | _ -> false
                    )
                    ->
                    PaymentStatus.PaymentPending
                | due, payments when
                    due > 0L<Cent>
                    && (payments |> Array.sumBy ActualPayment.totalConfirmedOrWrittenOff) >= due
                    ->
                    PaymentStatus.PaymentMade
                | due, payments when
                    due > 0L<Cent>
                    && (payments |> Array.sumBy ActualPayment.totalConfirmedOrWrittenOff) > 0L<Cent>
                    ->
                    PaymentStatus.Underpayment
                | 0L<Cent>, payments when (payments |> Array.sumBy ActualPayment.totalConfirmedOrWrittenOff) > 0L<Cent> ->
                    PaymentStatus.ExtraPayment
                | _ -> PaymentStatus.NotYetDue
                |> box
        Default = box PaymentStatus.NoneScheduled
    }

    /// Calculate actuarial interest
    let actuarialInterestSubscription = {
        Name = "ActuarialInterest"
        Dependencies = [ "PrincipalBalance" ]
        Calculate =
            fun events context ->
                let previousPrincipalBalance =
                    getPreviousBalance<int64<Cent>> "PrincipalBalance" context context.Parameters.Basic.Principal

                let daysDiff = 1 // assuming daily calculation

                // Simple interest calculation - in reality this would be more complex
                let dailyRate = 0.01m / 365m // Example daily rate
                let interest = decimal previousPrincipalBalance * dailyRate * decimal daysDiff
                interest * 1m<Cent> |> box
        Default = box 0m<Cent>
    }

    /// Calculate principal balance
    let principalBalanceSubscription = {
        Name = "PrincipalBalance"
        Dependencies = [ "PrincipalPortion" ]
        Calculate =
            fun events context ->
                let previousBalance =
                    getPreviousBalance<int64<Cent>> "PrincipalBalance" context context.Parameters.Basic.Principal

                let principalPortion =
                    getFieldValue<int64<Cent>> "PrincipalPortion" context
                    |> Option.defaultValue 0L<Cent>

                previousBalance - principalPortion |> box
        Default = box 0L<Cent>
    }

    /// Calculate principal portion
    let principalPortionSubscription = {
        Name = "PrincipalPortion"
        Dependencies = [ "NetEffect"; "InterestPortion"; "FeePortion"; "ChargesPortion" ]
        Calculate =
            fun events context ->
                let netEffect =
                    getFieldValue<int64<Cent>> "NetEffect" context |> Option.defaultValue 0L<Cent>

                let interestPortion =
                    getFieldValue<int64<Cent>> "InterestPortion" context
                    |> Option.defaultValue 0L<Cent>

                let feePortion =
                    getFieldValue<int64<Cent>> "FeePortion" context |> Option.defaultValue 0L<Cent>

                let chargesPortion =
                    getFieldValue<int64<Cent>> "ChargesPortion" context
                    |> Option.defaultValue 0L<Cent>

                // Principal portion is what remains after other portions are applied
                netEffect - interestPortion - feePortion - chargesPortion |> box
        Default = box 0L<Cent>
    }

    /// Calculate interest portion
    let interestPortionSubscription = {
        Name = "InterestPortion"
        Dependencies = [ "NetEffect"; "ActuarialInterest" ]
        Calculate =
            fun events context ->
                let actuarialInterest =
                    getFieldValue<decimal<Cent>> "ActuarialInterest" context
                    |> Option.defaultValue 0m<Cent>
                // Convert decimal to int64 for interest portion
                int64 (decimal actuarialInterest) * 1L<Cent> |> box
        Default = box 0L<Cent>
    }

    /// Calculate balance status
    let balanceStatusSubscription = {
        Name = "BalanceStatus"
        Dependencies = [ "PrincipalBalance"; "InterestBalance"; "FeeBalance"; "ChargesBalance" ]
        Calculate =
            fun events context ->
                let principalBalance =
                    getFieldValue<int64<Cent>> "PrincipalBalance" context
                    |> Option.defaultValue 0L<Cent>

                let interestBalance =
                    getFieldValue<decimal<Cent>> "InterestBalance" context
                    |> Option.defaultValue 0m<Cent>

                let feeBalance =
                    getFieldValue<int64<Cent>> "FeeBalance" context |> Option.defaultValue 0L<Cent>

                let chargesBalance =
                    getFieldValue<int64<Cent>> "ChargesBalance" context
                    |> Option.defaultValue 0L<Cent>

                let totalBalance =
                    principalBalance
                    + int64 (decimal interestBalance) * 1L<Cent>
                    + feeBalance
                    + chargesBalance

                match totalBalance with
                | balance when balance < 0L<Cent> -> Amortisation.BalanceStatus.RefundDue
                | 0L<Cent> -> Amortisation.BalanceStatus.ClosedBalance
                | _ -> Amortisation.BalanceStatus.OpenBalance
                |> box
        Default = box Amortisation.BalanceStatus.OpenBalance
    }

    // Default field subscriptions for fields not yet implemented
    let defaultSubscriptions = [
        "OffsetDayType",
        {
            Name = "OffsetDayType"
            Dependencies = []
            Calculate = (fun _ _ -> box Amortisation.OffsetDay)
            Default = box Amortisation.OffsetDay
        }
        "GeneratedPayment",
        {
            Name = "GeneratedPayment"
            Dependencies = []
            Calculate = (fun _ _ -> box NoGeneratedPayment)
            Default = box NoGeneratedPayment
        }
        "NewCharges",
        {
            Name = "NewCharges"
            Dependencies = []
            Calculate = (fun _ _ -> box [||])
            Default = box [||]
        }
        "ChargesPortion",
        {
            Name = "ChargesPortion"
            Dependencies = []
            Calculate = (fun _ _ -> box 0L<Cent>)
            Default = box 0L<Cent>
        }
        "NewInterest",
        {
            Name = "NewInterest"
            Dependencies = [ "ActuarialInterest" ]
            Calculate =
                (fun _ ctx ->
                    getFieldValue<decimal<Cent>> "ActuarialInterest" ctx
                    |> Option.defaultValue 0m<Cent>
                    |> box
                )
            Default = box 0m<Cent>
        }
        "FeeRebate",
        {
            Name = "FeeRebate"
            Dependencies = []
            Calculate = (fun _ _ -> box 0L<Cent>)
            Default = box 0L<Cent>
        }
        "FeePortion",
        {
            Name = "FeePortion"
            Dependencies = []
            Calculate = (fun _ _ -> box 0L<Cent>)
            Default = box 0L<Cent>
        }
        "ChargesBalance",
        {
            Name = "ChargesBalance"
            Dependencies = []
            Calculate = (fun _ _ -> box 0L<Cent>)
            Default = box 0L<Cent>
        }
        "InterestBalance",
        {
            Name = "InterestBalance"
            Dependencies = [ "ActuarialInterest" ]
            Calculate =
                (fun _ ctx ->
                    getFieldValue<decimal<Cent>> "ActuarialInterest" ctx
                    |> Option.defaultValue 0m<Cent>
                    |> box
                )
            Default = box 0m<Cent>
        }
        "FeeBalance",
        {
            Name = "FeeBalance"
            Dependencies = []
            Calculate = (fun _ _ -> box 0L<Cent>)
            Default = box 0L<Cent>
        }
        "SettlementFigure",
        {
            Name = "SettlementFigure"
            Dependencies = [ "PrincipalBalance"; "InterestBalance"; "FeeBalance"; "ChargesBalance" ]
            Calculate =
                (fun _ ctx ->
                    let principalBalance =
                        getFieldValue<int64<Cent>> "PrincipalBalance" ctx
                        |> Option.defaultValue 0L<Cent>

                    let interestBalance =
                        getFieldValue<decimal<Cent>> "InterestBalance" ctx
                        |> Option.defaultValue 0m<Cent>

                    let feeBalance =
                        getFieldValue<int64<Cent>> "FeeBalance" ctx |> Option.defaultValue 0L<Cent>

                    let chargesBalance =
                        getFieldValue<int64<Cent>> "ChargesBalance" ctx |> Option.defaultValue 0L<Cent>

                    principalBalance
                    + int64 (decimal interestBalance) * 1L<Cent>
                    + feeBalance
                    + chargesBalance
                    |> box
                )
            Default = box 0L<Cent>
        }
        "FeeRebateIfSettled",
        {
            Name = "FeeRebateIfSettled"
            Dependencies = []
            Calculate = (fun _ _ -> box 0L<Cent>)
            Default = box 0L<Cent>
        }
    ]

    /// Default schedule configuration with all field subscriptions
    let defaultScheduleConfiguration = {
        FieldSubscriptions =
            [
                "OffsetDate",
                {
                    offsetDateSubscription with
                        Calculate = fun events context -> offsetDateSubscription.Calculate events context
                }
                "Advances",
                {
                    advancesSubscription with
                        Calculate = fun events context -> advancesSubscription.Calculate events context
                }
                "ScheduledPayment",
                {
                    scheduledPaymentSubscription with
                        Calculate = fun events context -> scheduledPaymentSubscription.Calculate events context
                }
                "Window",
                {
                    windowSubscription with
                        Calculate = fun events context -> windowSubscription.Calculate events context
                }
                "PaymentDue",
                {
                    paymentDueSubscription with
                        Calculate = fun events context -> paymentDueSubscription.Calculate events context
                }
                "ActualPayments",
                {
                    actualPaymentsSubscription with
                        Calculate = fun events context -> actualPaymentsSubscription.Calculate events context
                }
                "NetEffect",
                {
                    netEffectSubscription with
                        Calculate = fun events context -> netEffectSubscription.Calculate events context
                }
                "PaymentStatus",
                {
                    paymentStatusSubscription with
                        Calculate = fun events context -> paymentStatusSubscription.Calculate events context
                }
                "ActuarialInterest",
                {
                    actuarialInterestSubscription with
                        Calculate = fun events context -> actuarialInterestSubscription.Calculate events context
                }
                "PrincipalBalance",
                {
                    principalBalanceSubscription with
                        Calculate = fun events context -> principalBalanceSubscription.Calculate events context
                }
                "PrincipalPortion",
                {
                    principalPortionSubscription with
                        Calculate = fun events context -> principalPortionSubscription.Calculate events context
                }
                "InterestPortion",
                {
                    interestPortionSubscription with
                        Calculate = fun events context -> interestPortionSubscription.Calculate events context
                }
                "BalanceStatus",
                {
                    balanceStatusSubscription with
                        Calculate = fun events context -> balanceStatusSubscription.Calculate events context
                }
            ]
            @ defaultSubscriptions
            |> Map.ofList
        EventProcessors = []
    }

    // ===== DEPENDENCY RESOLUTION =====    /// Topologically sort field subscriptions based on dependencies
    let resolveDependencyOrder (subscriptions: Map<string, FieldSubscription<obj>>) : string list =
        let rec resolve (resolved: Set<string>) (remaining: Map<string, FieldSubscription<obj>>) (order: string list) =
            if Map.isEmpty remaining then
                List.rev order
            else
                let readyFields =
                    remaining
                    |> Map.filter (fun _ subscription -> subscription.Dependencies |> List.forall resolved.Contains)
                    |> Map.keys
                    |> Seq.toList

                if List.isEmpty readyFields then
                    let remainingFieldNames = remaining |> Map.keys |> String.concat ", "
                    failwithf "Circular dependency detected in remaining fields: %s" remainingFieldNames

                let nextField = readyFields |> List.head
                let newResolved = resolved |> Set.add nextField
                let newRemaining = remaining |> Map.remove nextField
                let newOrder = nextField :: order

                resolve newResolved newRemaining newOrder

        resolve Set.empty subscriptions []

    // ===== SCHEDULE CALCULATION ENGINE =====    /// Calculate a single schedule item for a given day
    let calculateScheduleItem (config: ScheduleConfiguration) (context: ScheduleContext) : Amortisation.ScheduleItem =

        let dependencyOrder = resolveDependencyOrder config.FieldSubscriptions

        // Calculate fields in dependency order
        let finalContext =
            dependencyOrder
            |> List.fold
                (fun ctx fieldName ->
                    match config.FieldSubscriptions.TryFind fieldName with
                    | Some subscription ->
                        let value = subscription.Calculate context.EventHistory ctx
                        setFieldValue fieldName value ctx
                    | None -> ctx
                )
                context

        // Convert calculated fields back to ScheduleItem record
        {
            OffsetDayType =
                getFieldValue<Amortisation.OffsetDayType> "OffsetDayType" finalContext
                |> Option.defaultValue Amortisation.OffsetDay
            OffsetDate =
                getFieldValue<Date> "OffsetDate" finalContext
                |> Option.defaultValue (Date(1900, 1, 1))
            Advances =
                getFieldValue<int64<Cent> array> "Advances" finalContext
                |> Option.defaultValue [||]
            ScheduledPayment =
                getFieldValue<ScheduledPayment> "ScheduledPayment" finalContext
                |> Option.defaultValue ScheduledPayment.zero
            Window = getFieldValue<int> "Window" finalContext |> Option.defaultValue 0
            PaymentDue =
                getFieldValue<int64<Cent>> "PaymentDue" finalContext
                |> Option.defaultValue 0L<Cent>
            ActualPayments =
                getFieldValue<ActualPayment array> "ActualPayments" finalContext
                |> Option.defaultValue [||]
            GeneratedPayment =
                getFieldValue<GeneratedPayment> "GeneratedPayment" finalContext
                |> Option.defaultValue NoGeneratedPayment
            NetEffect =
                getFieldValue<int64<Cent>> "NetEffect" finalContext
                |> Option.defaultValue 0L<Cent>
            PaymentStatus =
                getFieldValue<PaymentStatus> "PaymentStatus" finalContext
                |> Option.defaultValue PaymentStatus.NoneScheduled
            BalanceStatus =
                getFieldValue<Amortisation.BalanceStatus> "BalanceStatus" finalContext
                |> Option.defaultValue Amortisation.BalanceStatus.OpenBalance
            NewCharges =
                getFieldValue<AppliedCharge array> "NewCharges" finalContext
                |> Option.defaultValue [||]
            ChargesPortion =
                getFieldValue<int64<Cent>> "ChargesPortion" finalContext
                |> Option.defaultValue 0L<Cent>
            ActuarialInterest =
                getFieldValue<decimal<Cent>> "ActuarialInterest" finalContext
                |> Option.defaultValue 0m<Cent>
            NewInterest =
                getFieldValue<decimal<Cent>> "NewInterest" finalContext
                |> Option.defaultValue 0m<Cent>
            InterestPortion =
                getFieldValue<int64<Cent>> "InterestPortion" finalContext
                |> Option.defaultValue 0L<Cent>
            FeeRebate =
                getFieldValue<int64<Cent>> "FeeRebate" finalContext
                |> Option.defaultValue 0L<Cent>
            FeePortion =
                getFieldValue<int64<Cent>> "FeePortion" finalContext
                |> Option.defaultValue 0L<Cent>
            PrincipalPortion =
                getFieldValue<int64<Cent>> "PrincipalPortion" finalContext
                |> Option.defaultValue 0L<Cent>
            ChargesBalance =
                getFieldValue<int64<Cent>> "ChargesBalance" finalContext
                |> Option.defaultValue 0L<Cent>
            InterestBalance =
                getFieldValue<decimal<Cent>> "InterestBalance" finalContext
                |> Option.defaultValue 0m<Cent>
            FeeBalance =
                getFieldValue<int64<Cent>> "FeeBalance" finalContext
                |> Option.defaultValue 0L<Cent>
            PrincipalBalance =
                getFieldValue<int64<Cent>> "PrincipalBalance" finalContext
                |> Option.defaultValue 0L<Cent>
            SettlementFigure =
                getFieldValue<int64<Cent>> "SettlementFigure" finalContext
                |> Option.defaultValue 0L<Cent>
            FeeRebateIfSettled =
                getFieldValue<int64<Cent>> "FeeRebateIfSettled" finalContext
                |> Option.defaultValue 0L<Cent>
        }

    /// Calculate the full amortization schedule
    let calculateSchedule
        (parameters: Parameters)
        (actualPayments: Map<int<OffsetDay>, ActualPayment array>)
        (scheduledPayments: Map<int<OffsetDay>, ScheduledPayment>)
        (config: ScheduleConfiguration option)
        : Map<int<OffsetDay>, Amortisation.ScheduleItem> =

        let config = config |> Option.defaultValue defaultScheduleConfiguration

        let evaluationDay =
            parameters.Basic.EvaluationDate |> OffsetDay.fromDate parameters.Basic.StartDate

        // Generate days from 0 to evaluation day
        let days = [ 0 .. int evaluationDay ] |> List.map (fun i -> i * 1<OffsetDay>)

        // Calculate schedule items day by day
        let (schedule, _) =
            days
            |> List.fold
                (fun (schedule, previousItem) day ->
                    let events = [ DayAdvanced day ]

                    let context = {
                        CurrentDay = day
                        PreviousItem = previousItem
                        Parameters = parameters
                        ActualPayments = actualPayments
                        ScheduledPayments = scheduledPayments
                        EventHistory = events
                        CalculatedFields = Map.empty
                    }

                    let item = calculateScheduleItem config context
                    let newSchedule = schedule |> Map.add day item
                    newSchedule, Some item
                )
                (Map.empty, None)

        schedule

    // ===== PUBLIC API =====

    /// Create a custom field subscription
    let createFieldSubscription<'T> name dependencies calculate defaultValue = {
        Name = name
        Dependencies = dependencies
        Calculate = fun events context -> calculate events context |> box
        Default = box defaultValue
    }

    /// Add a custom field subscription to a configuration
    let addFieldSubscription name subscription config = {
        config with
            FieldSubscriptions = config.FieldSubscriptions |> Map.add name subscription
    }

    /// Add an event processor to a configuration
    let addEventProcessor processor config = {
        config with
            EventProcessors = processor :: config.EventProcessors
    }
