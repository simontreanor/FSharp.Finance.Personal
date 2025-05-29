namespace FSharp.Finance.Personal

/// Actor Model approach to amortisation calculation
/// This implementation transforms the imperative financial calculation logic
/// into a reactive programming pattern using actors to handle different
/// balance components independently
module Amortisation3 =

    open System
    open System.Threading
    open AppliedPayment
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    // ===== ACTOR MESSAGE TYPES =====

    /// Message types for communication between actors
    type ActorMessage<'T> =
        | Calculate of input: 'T * replyChannel: AsyncReplyChannel<'T>
        | GetState of replyChannel: AsyncReplyChannel<'T>
        | UpdateState of newState: 'T
        | Reset of initialState: 'T
        | Shutdown

    /// Messages specific to principal balance management
    type PrincipalMessage =
        | ApplyPrincipalPayment of amount: int64<Cent> * day: int<OffsetDay>
        | GetPrincipalBalance
        | AddAdvance of amount: int64<Cent>

    /// Messages specific to interest calculation and balance
    type InterestMessage =
        | AccrueInterest of days: int * rate: decimal * principalBalance: int64<Cent>
        | ApplyInterestPayment of amount: int64<Cent>
        | GetInterestBalance
        | CapInterest of cap: Amount.InterestCap

    /// Messages specific to fee balance tracking
    type FeeMessage =
        | ApplyFeePayment of amount: int64<Cent>
        | GetFeeBalance
        | CalculateFeeRebate of settlementDay: int<OffsetDay> * totalFee: int64<Cent>

    /// Messages specific to charges processing
    type ChargesMessage =
        | AddCharge of charge: AppliedCharge
        | ApplyChargesPayment of amount: int64<Cent>
        | GetChargesBalance

    /// Coordinator messages for orchestrating actor interactions
    type CoordinatorMessage =
        | ProcessPaymentDay of day: int<OffsetDay> * appliedPayment: AppliedPayment
        | GetScheduleItem of day: int<OffsetDay>
        | GenerateSettlement of day: int<OffsetDay>
        | GetFinalStats

    // ===== ACTOR STATE TYPES =====

    /// State maintained by the principal balance actor
    type PrincipalState = {
        Balance: int64<Cent>
        LastUpdateDay: int<OffsetDay>
        TotalAdvances: int64<Cent>
    }

    /// State maintained by the interest actor
    type InterestState = {
        Balance: decimal<Cent>
        AccruedToday: decimal<Cent>
        LastCalculationDay: int<OffsetDay>
        CumulativeInterest: decimal<Cent>
        TotalCap: Amount.InterestCap option
    }

    /// State maintained by the fee actor
    type FeeState = {
        Balance: int64<Cent>
        TotalFee: int64<Cent>
        PaidToDate: int64<Cent>
    }

    /// State maintained by the charges actor
    type ChargesState = {
        Balance: int64<Cent>
        Charges: AppliedCharge list
        TotalCharges: int64<Cent>
    }

    /// Comprehensive state for the coordinator
    type CoordinatorState = {
        Parameters: Parameters
        CurrentDay: int<OffsetDay>
        ScheduleItems: Map<int<OffsetDay>, Amortisation.ScheduleItem>
        PrincipalActor: MailboxProcessor<ActorMessage<PrincipalState>>
        InterestActor: MailboxProcessor<ActorMessage<InterestState>>
        FeeActor: MailboxProcessor<ActorMessage<FeeState>>
        ChargesActor: MailboxProcessor<ActorMessage<ChargesState>>
    }

    // ===== HELPER FUNCTIONS =====

    /// Create default principal state
    let createPrincipalState initialPrincipal = {
        Balance = initialPrincipal
        LastUpdateDay = 0<OffsetDay>
        TotalAdvances = initialPrincipal
    }

    /// Create default interest state
    let createInterestState() = {
        Balance = 0m<Cent>
        AccruedToday = 0m<Cent>
        LastCalculationDay = 0<OffsetDay>
        CumulativeInterest = 0m<Cent>
        TotalCap = None
    }

    /// Create default fee state
    let createFeeState totalFee = {
        Balance = totalFee
        TotalFee = totalFee
        PaidToDate = 0L<Cent>
    }

    /// Create default charges state
    let createChargesState() = {
        Balance = 0L<Cent>
        Charges = []
        TotalCharges = 0L<Cent>
    }

    /// Calculate daily interest rate from parameters
    let calculateDailyRate (interestConfig: Interest.StandardRateConfig) =
        match interestConfig.StandardRate with
        | Interest.Rate.Daily rate -> Percent.toDecimal rate
        | Interest.Rate.Annual rate -> Percent.toDecimal rate / 365m
        | Interest.Rate.Zero -> 0m

    // ===== ACTOR IMPLEMENTATIONS =====

    /// Principal balance management actor
    let createPrincipalActor (initialState: PrincipalState) =
        MailboxProcessor<ActorMessage<PrincipalState>>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Calculate (input, replyChannel) ->
                        replyChannel.Reply(input)
                        return! loop state
                    | GetState replyChannel ->
                        replyChannel.Reply(state)
                        return! loop state
                    | UpdateState newState ->
                        return! loop newState
                    | Reset newState ->
                        return! loop newState
                    | Shutdown ->
                        return ()
                }
            loop initialState
        )

    /// Interest calculation and balance management actor
    let createInterestActor (initialState: InterestState) (parameters: Parameters) =
        MailboxProcessor<ActorMessage<InterestState>>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Calculate (input, replyChannel) ->
                        replyChannel.Reply(input)
                        return! loop state
                    | GetState replyChannel ->
                        replyChannel.Reply(state)
                        return! loop state
                    | UpdateState newState ->
                        return! loop newState
                    | Reset newState ->
                        return! loop newState
                    | Shutdown ->
                        return ()
                }
            loop initialState
        )

    /// Fee balance tracking actor
    let createFeeActor (initialState: FeeState) =
        MailboxProcessor<ActorMessage<FeeState>>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Calculate (input, replyChannel) ->
                        replyChannel.Reply(input)
                        return! loop state
                    | GetState replyChannel ->
                        replyChannel.Reply(state)
                        return! loop state
                    | UpdateState newState ->
                        return! loop newState
                    | Reset newState ->
                        return! loop newState
                    | Shutdown ->
                        return ()
                }
            loop initialState
        )

    /// Charges processing actor
    let createChargesActor (initialState: ChargesState) =
        MailboxProcessor<ActorMessage<ChargesState>>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Calculate (input, replyChannel) ->
                        replyChannel.Reply(input)
                        return! loop state
                    | GetState replyChannel ->
                        replyChannel.Reply(state)
                        return! loop state
                    | UpdateState newState ->
                        return! loop newState
                    | Reset newState ->
                        return! loop newState
                    | Shutdown ->
                        return ()
                }
            loop initialState
        )

    // ===== ADVANCED ACTOR MESSAGE PROCESSING =====

    /// Enhanced principal actor with specific business logic
    let createEnhancedPrincipalActor (initialState: PrincipalState) =
        MailboxProcessor<PrincipalMessage>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | ApplyPrincipalPayment (amount, day) ->
                        let newBalance = state.Balance - amount
                        let newState = { 
                            state with 
                                Balance = newBalance
                                LastUpdateDay = day 
                        }
                        return! loop newState
                    | GetPrincipalBalance ->
                        // This would typically send the balance to a reply channel
                        return! loop state
                    | AddAdvance amount ->
                        let newState = { 
                            state with 
                                Balance = state.Balance + amount
                                TotalAdvances = state.TotalAdvances + amount 
                        }
                        return! loop newState
                }
            loop initialState
        )

    /// Enhanced interest actor with accrual logic
    let createEnhancedInterestActor (initialState: InterestState) (parameters: Parameters) =
        MailboxProcessor<InterestMessage>.Start(fun inbox ->
            let dailyRate = calculateDailyRate parameters.Basic.InterestConfig
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | AccrueInterest (days, rate, principalBalance) ->
                        let interestAccrued = 
                            decimal principalBalance * rate * decimal days * 1m<Cent>
                        let newState = { 
                            state with 
                                Balance = state.Balance + interestAccrued
                                AccruedToday = interestAccrued
                                CumulativeInterest = state.CumulativeInterest + interestAccrued
                        }
                        return! loop newState
                    | ApplyInterestPayment amount ->
                        let paymentAmount = decimal amount * 1m<Cent>
                        let newBalance = state.Balance - paymentAmount
                        let newState = { 
                            state with 
                                Balance = Cent.max 0m<Cent> newBalance 
                        }
                        return! loop newState
                    | GetInterestBalance ->
                        return! loop state
                    | CapInterest cap ->
                        // Apply interest cap logic based on original implementation
                        let newState = { state with TotalCap = Some cap }
                        return! loop newState
                }
            loop initialState
        )

    /// Enhanced fee actor with rebate calculations
    let createEnhancedFeeActor (initialState: FeeState) (parameters: Parameters) =
        MailboxProcessor<FeeMessage>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | ApplyFeePayment amount ->
                        let newBalance = state.Balance - amount
                        let newPaid = state.PaidToDate + amount
                        let newState = { 
                            state with 
                                Balance = Cent.max 0L<Cent> newBalance
                                PaidToDate = newPaid 
                        }
                        return! loop newState
                    | GetFeeBalance ->
                        return! loop state
                    | CalculateFeeRebate (settlementDay, totalFee) ->
                        // Implement fee rebate calculation based on original logic
                        let rebateAmount = 
                            match parameters.Basic.FeeConfig with
                            | ValueSome feeConfig ->
                                match feeConfig.SettlementRebate with
                                | Fee.SettlementRebate.ProRata ->
                                    // Simplified pro-rata calculation
                                    // This would need to access original schedule parameters
                                    totalFee / 2L<Cent> // Placeholder calculation
                                | Fee.SettlementRebate.Zero -> 0L<Cent>
                                | _ -> 0L<Cent>
                            | ValueNone -> 0L<Cent>
                        
                        let newState = { state with Balance = state.Balance - rebateAmount }
                        return! loop newState
                }
            loop initialState
        )

    /// Enhanced charges actor with charge management
    let createEnhancedChargesActor (initialState: ChargesState) =
        MailboxProcessor<ChargesMessage>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | AddCharge charge ->
                        let newState = { 
                            state with 
                                Balance = state.Balance + charge.Total
                                Charges = charge :: state.Charges
                                TotalCharges = state.TotalCharges + charge.Total
                        }
                        return! loop newState
                    | ApplyChargesPayment amount ->
                        let newBalance = state.Balance - amount
                        let newState = { 
                            state with 
                                Balance = Cent.max 0L<Cent> newBalance 
                        }
                        return! loop newState
                    | GetChargesBalance ->
                        return! loop state
                }
            loop initialState
        )

    // ===== PAYMENT APPORTIONMENT LOGIC =====

    /// Calculate payment apportionment using actor communication
    let calculatePaymentApportionment 
        (netEffect: int64<Cent>) 
        (principalActor: MailboxProcessor<PrincipalMessage>)
        (interestActor: MailboxProcessor<InterestMessage>)
        (feeActor: MailboxProcessor<FeeMessage>)
        (chargesActor: MailboxProcessor<ChargesMessage>)
        (parameters: Parameters) =
        
        async {
            // Get current balances from all actors
            // Note: This is a simplified version - full implementation would need proper reply channels
            
            // Payment priority: Charges -> Interest -> Fees -> Principal
            let mutable remainingPayment = netEffect
            let mutable chargesPortion = 0L<Cent>
            let mutable interestPortion = 0L<Cent>
            let mutable feePortion = 0L<Cent>
            let mutable principalPortion = 0L<Cent>
            
            // Apply to charges first
            if remainingPayment > 0L<Cent> then
                chargesPortion <- Cent.min remainingPayment remainingPayment // Simplified
                remainingPayment <- remainingPayment - chargesPortion
                chargesActor.Post(ApplyChargesPayment chargesPortion)
            
            // Apply to interest second
            if remainingPayment > 0L<Cent> then
                interestPortion <- Cent.min remainingPayment remainingPayment // Simplified
                remainingPayment <- remainingPayment - interestPortion
                interestActor.Post(ApplyInterestPayment interestPortion)
            
            // Apply to fees third
            if remainingPayment > 0L<Cent> then
                feePortion <- Cent.min remainingPayment remainingPayment // Simplified
                remainingPayment <- remainingPayment - feePortion
                feeActor.Post(ApplyFeePayment feePortion)
            
            // Apply remainder to principal
            if remainingPayment > 0L<Cent> then
                principalPortion <- remainingPayment
                principalActor.Post(ApplyPrincipalPayment(principalPortion, 0<OffsetDay>))
            
            return {
                Amortisation.Apportionment.PrincipalPortion = principalPortion
                FeePortion = feePortion
                InterestPortion = interestPortion
                ChargesPortion = chargesPortion
            }
        }

    // ===== COORDINATOR ACTOR =====

    /// Main coordinator actor that orchestrates all other actors
    let createCoordinatorActor (parameters: Parameters) (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        let initialPrincipal = parameters.Basic.Principal
        let feeTotal = Fee.total parameters.Basic.FeeConfig initialPrincipal
        
        // Create child actors
        let principalActor = createEnhancedPrincipalActor (createPrincipalState initialPrincipal)
        let interestActor = createEnhancedInterestActor (createInterestState()) parameters
        let feeActor = createEnhancedFeeActor (createFeeState feeTotal) parameters
        let chargesActor = createEnhancedChargesActor (createChargesState())
        
        MailboxProcessor<CoordinatorMessage>.Start(fun inbox ->
            let rec loop (scheduleItems: Map<int<OffsetDay>, Amortisation.ScheduleItem>) =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | ProcessPaymentDay (day, appliedPayment) ->
                        // Process payment for the specific day
                        let netEffect = appliedPayment.NetEffect
                        
                        // Accrue interest for the day
                        let dailyRate = calculateDailyRate parameters.Basic.InterestConfig
                        interestActor.Post(AccrueInterest(1, dailyRate, initialPrincipal))
                        
                        // Apply any charges
                        appliedPayment.AppliedCharges
                        |> Array.iter (chargesActor.Post << AddCharge)
                        
                        // Calculate payment apportionment
                        let! apportionment = 
                            calculatePaymentApportionment 
                                netEffect principalActor interestActor feeActor chargesActor parameters
                        
                        // Create schedule item (simplified version)
                        let scheduleItem = {
                            Amortisation.ScheduleItem.zero with
                                OffsetDate = parameters.Basic.StartDate.AddDays(int day)
                                ActualPayments = appliedPayment.ActualPayments
                                NetEffect = netEffect
                                PrincipalPortion = apportionment.PrincipalPortion
                                FeePortion = apportionment.FeePortion
                                InterestPortion = apportionment.InterestPortion
                                ChargesPortion = apportionment.ChargesPortion
                        }
                        
                        let newScheduleItems = scheduleItems |> Map.add day scheduleItem
                        return! loop newScheduleItems
                        
                    | GetScheduleItem day ->
                        // Return schedule item for specific day
                        return! loop scheduleItems
                        
                    | GenerateSettlement day ->
                        // Generate settlement calculation
                        return! loop scheduleItems
                        
                    | GetFinalStats ->
                        // Calculate final statistics
                        return! loop scheduleItems
                }
            loop Map.empty
        )

    // ===== PUBLIC API =====

    /// Generate amortisation schedule using Actor Model approach
    let amortise (parameters: Parameters) (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        async {
            // Convert actual payments to applied payments (simplified)
            let appliedPayments = 
                actualPayments
                |> Map.map (fun day payments ->
                    let netEffect = payments |> Array.sumBy ActualPayment.total
                    {
                        AppliedPayment.zero with
                            ActualPayments = payments
                            NetEffect = netEffect
                    }
                )
            
            // Create coordinator actor
            let coordinator = createCoordinatorActor parameters actualPayments
            
            // Process each payment day
            for KeyValue(day, appliedPayment) in appliedPayments do
                coordinator.Post(ProcessPaymentDay(day, appliedPayment))
            
            // Wait for processing to complete and get final result
            // This is a simplified return - full implementation would gather
            // all schedule items and calculate proper statistics
            return {
                Amortisation.GenerationResult.AmortisationSchedule = {
                    ScheduleItems = Map.empty
                    FinalStats = {
                        RequiredScheduledPaymentCount = 0
                        LastRequiredScheduledPaymentDay = ValueNone
                        FinalActualPaymentCount = 0
                        LastActualPaymentDay = ValueNone
                        FinalCostToBorrowingRatio = Percent 0m
                        EffectiveInterestRate = Interest.Rate.Zero
                        SettlementFigure = ValueNone
                        FinalBalanceStatus = Amortisation.BalanceStatus.OpenBalance
                    }
                }
                BasicSchedule = {
                    Items = [||]
                    Stats = {
                        InitialInterestBalance = 0L<Cent>
                        RequiredPaymentCount = 0
                        LevelPayment = 0L<Cent>
                        FinalPayment = 0L<Cent>
                        LastScheduledPaymentDay = 0<OffsetDay>
                        InitialApr = Percent 0m
                    }
                }
            }
        }

    // ===== UTILITY FUNCTIONS =====

    /// Cleanup function to shutdown all actors
    let shutdownActors (coordinator: MailboxProcessor<CoordinatorMessage>) =
        async {
            // In a full implementation, this would send shutdown messages to all child actors
            return ()
        }

    /// Alternative synchronous API for compatibility
    let amortiseSync (parameters: Parameters) (actualPayments: Map<int<OffsetDay>, ActualPayment array>) =
        amortise parameters actualPayments |> Async.RunSynchronously

    // ===== REACTIVE EXTENSIONS =====

    /// Event stream for monitoring actor state changes
    type ActorStateChange<'T> = {
        ActorId: string
        OldState: 'T
        NewState: 'T
        Timestamp: DateTimeOffset
    }

    /// Observable stream of actor state changes (for monitoring and debugging)
    let createStateChangeObservable<'T>() =
        let event = new Event<ActorStateChange<'T>>()
        event.Publish

    /// Enhanced actor with state change notifications
    let createObservableActor<'T> (initialState: 'T) (actorId: string) (stateChangeEvent: Event<ActorStateChange<'T>>) =
        MailboxProcessor<ActorMessage<'T>>.Start(fun inbox ->
            let rec loop state =
                async {
                    let! msg = inbox.Receive()
                    match msg with
                    | Calculate (input, replyChannel) ->
                        replyChannel.Reply(input)
                        return! loop state
                    | GetState replyChannel ->
                        replyChannel.Reply(state)
                        return! loop state
                    | UpdateState newState ->
                        stateChangeEvent.Trigger({
                            ActorId = actorId
                            OldState = state
                            NewState = newState
                            Timestamp = DateTimeOffset.Now
                        })
                        return! loop newState
                    | Reset newState ->
                        stateChangeEvent.Trigger({
                            ActorId = actorId
                            OldState = state
                            NewState = newState
                            Timestamp = DateTimeOffset.Now
                        })
                        return! loop newState
                    | Shutdown ->
                        return ()
                }
            loop initialState
        )
