namespace FSharp.Finance.Personal

/// Amortisation calculation module using Functional Reactive Programming with F# Streams
/// This module replicates the functionality of Amortisation.fs using reactive stream processing
module Amortisation4 =

    open System
    open Calculation
    open Currency
    open DateDay
    open Scheduling
    open Util
    
    /// Stream processing utilities for reactive amortisation calculations
    module Streams =
        
        /// A simple reactive stream implementation for F#
        type Stream<'T> = {
            Subscribe: ('T -> unit) -> IDisposable
        }
        
        /// Create a stream from a sequence
        let fromSeq (seq: 'T seq) : Stream<'T> =
            { Subscribe = fun observer ->
                let cancelled = ref false
                let disposable = { new IDisposable with member _.Dispose() = cancelled := true }
                async {
                    for item in seq do
                        if not !cancelled then
                            observer item
                } |> Async.Start
                disposable
            }
        
        /// Map a function over a stream
        let map (f: 'T -> 'U) (stream: Stream<'T>) : Stream<'U> =
            { Subscribe = fun observer ->
                stream.Subscribe(f >> observer)
            }
        
        /// Filter a stream
        let filter (predicate: 'T -> bool) (stream: Stream<'T>) : Stream<'T> =
            { Subscribe = fun observer ->
                stream.Subscribe(fun x -> if predicate x then observer x)
            }
        
        /// Scan (fold with intermediate results) over a stream
        let scan (f: 'State -> 'T -> 'State) (initial: 'State) (stream: Stream<'T>) : Stream<'State> =
            { Subscribe = fun observer ->
                let state = ref initial
                observer !state  // Emit initial state
                stream.Subscribe(fun x ->
                    state := f !state x
                    observer !state
                )
            }
        
        /// Combine two streams using a function
        let combine (f: 'T -> 'U -> 'V) (stream1: Stream<'T>) (stream2: Stream<'U>) : Stream<'V> =
            { Subscribe = fun observer ->
                let latest1 = ref None
                let latest2 = ref None
                let checkEmit () =
                    match !latest1, !latest2 with
                    | Some v1, Some v2 -> observer (f v1 v2)
                    | _ -> ()
                
                let disp1 = stream1.Subscribe(fun x ->
                    latest1 := Some x
                    checkEmit()
                )
                let disp2 = stream2.Subscribe(fun x ->
                    latest2 := Some x
                    checkEmit()
                )
                
                { new IDisposable with 
                    member _.Dispose() = 
                        disp1.Dispose()
                        disp2.Dispose()
                }
            }
        
        /// Collect stream values into a list
        let toList (stream: Stream<'T>) : 'T list =
            let results = ResizeArray<'T>()
            let mutable completed = false
            use subscription = stream.Subscribe(fun x ->
                results.Add(x)
            )
            // For synchronous streams, we assume immediate completion
            results |> List.ofSeq

    /// Reactive types for amortisation calculations
    type AmortisationState = {
        OffsetDay: int<OffsetDay>
        Principal: decimal<Currency>
        Interest: decimal<Currency>
        Fees: decimal<Currency>
        TotalBalance: decimal<Currency>
        ScheduleItems: Map<int<OffsetDay>, ScheduleItem>
    }
    
    /// Events that can occur during amortisation
    type AmortisationEvent =
        | PaymentDue of ScheduledPayment
        | InterestAccrued of decimal<Currency>
        | FeeApplied of Fee
        | BalanceUpdated of decimal<Currency>
    
    /// Stream-based payment processor
    let processPaymentStream (parameters: Parameters) (initialState: AmortisationState) : Streams.Stream<AmortisationEvent> -> Streams.Stream<AmortisationState> =
        fun eventStream ->
            eventStream
            |> Streams.scan (fun state event ->
                match event with
                | PaymentDue payment ->
                    // Process scheduled payment
                    let appliedPayment = AppliedPayment.generate parameters state.OffsetDay payment
                    let newPrincipal = state.Principal - appliedPayment.PrincipalPortion
                    let newInterest = state.Interest - appliedPayment.InterestPortion
                    let newFees = state.Fees - appliedPayment.FeesPortion
                    
                    { state with 
                        Principal = newPrincipal
                        Interest = newInterest  
                        Fees = newFees
                        TotalBalance = newPrincipal + newInterest + newFees
                    }
                
                | InterestAccrued amount ->
                    { state with 
                        Interest = state.Interest + amount
                        TotalBalance = state.TotalBalance + amount
                    }
                
                | FeeApplied fee ->
                    let feeAmount = Fee.calculate parameters.FeeTypes fee
                    { state with 
                        Fees = state.Fees + feeAmount
                        TotalBalance = state.TotalBalance + feeAmount
                    }
                
                | BalanceUpdated newBalance ->
                    { state with TotalBalance = newBalance }
            ) initialState

    /// Generate interest accrual events stream
    let generateInterestStream (parameters: Parameters) (balanceStream: Streams.Stream<AmortisationState>) : Streams.Stream<AmortisationEvent> =
        balanceStream
        |> Streams.map (fun state ->
            let dailyRate = Interest.Daily.calculate parameters.Interest state.Principal
            InterestAccrued dailyRate
        )

    /// Generate fee events stream  
    let generateFeeStream (parameters: Parameters) (dayStream: Streams.Stream<int<OffsetDay>>) : Streams.Stream<AmortisationEvent> =
        dayStream
        |> Streams.filter (fun day -> 
            parameters.FeeTypes |> List.exists (fun feeType ->
                Fee.isApplicableOnDay feeType day
            ))
        |> Streams.map (fun day ->
            // Find applicable fee for this day
            parameters.FeeTypes 
            |> List.tryFind (fun feeType -> Fee.isApplicableOnDay feeType day)
            |> Option.map FeeApplied
            |> Option.defaultValue (BalanceUpdated 0m<Currency>)
        )

    /// Generate payment due events stream
    let generatePaymentStream (scheduledPayments: ScheduledPayment array) : Streams.Stream<AmortisationEvent> =
        scheduledPayments
        |> Streams.fromSeq
        |> Streams.map PaymentDue

    /// Calculate basic schedule using streams
    let calculateBasicScheduleStream (parameters: Parameters) : Streams.Stream<int<OffsetDay>> =
        let sp = Scheduling.generate parameters
        [1<OffsetDay> .. sp.FinalPaymentDay]
        |> Streams.fromSeq

    /// Main reactive amortisation calculation
    let amortiseReactive (parameters: Parameters) (scheduledPayments: ScheduledPayment array) : ScheduleItem array =
        
        // Initialize state
        let initialState = {
            OffsetDay = 0<OffsetDay>
            Principal = parameters.Principal
            Interest = 0m<Currency>
            Fees = 0m<Currency>
            TotalBalance = parameters.Principal
            ScheduleItems = Map.empty
        }
        
        // Generate event streams
        let dayStream = calculateBasicScheduleStream parameters
        let paymentEventStream = generatePaymentStream scheduledPayments
        let interestEventStream = generateInterestStream parameters (Streams.fromSeq [initialState])
        let feeEventStream = generateFeeStream parameters dayStream
        
        // Combine all event streams
        let combinedEventStream = 
            [paymentEventStream; interestEventStream; feeEventStream]
            |> List.fold (fun acc stream ->
                Streams.combine (fun _ event -> event) acc stream
            ) paymentEventStream
        
        // Process events through the payment processor
        let stateStream = processPaymentStream parameters initialState combinedEventStream
        
        // Convert final states to schedule items
        let finalStates = Streams.toList stateStream
        
        finalStates
        |> List.mapi (fun i state ->
            let offsetDay = (i + 1) * 1<OffsetDay>
            ScheduleItem.create offsetDay state.Principal state.Interest state.Fees
        )
        |> Array.ofList

    /// Stream-based amortisation with enhanced reactive patterns
    let amortiseWithStreams (parameters: Parameters) (scheduledPayments: ScheduledPayment array) : ScheduleItem array =
        
        // Create observable sequences for different aspects of the calculation
        let daySequence = 
            let sp = Scheduling.generate parameters
            [1<OffsetDay> .. sp.FinalPaymentDay]
        
        // Convert to reactive streams and process
        let dayStream = Streams.fromSeq daySequence
        
        // Calculate running balances using scan
        let balanceStream = 
            dayStream
            |> Streams.scan (fun (prevBalance, prevInterest, prevFees) currentDay ->
                // Find any scheduled payment for this day
                let paymentForDay = 
                    scheduledPayments 
                    |> Array.tryFind (fun sp -> sp.OffsetDay = currentDay)
                
                // Calculate daily interest on remaining principal
                let dailyInterest = Interest.Daily.calculate parameters.Interest prevBalance
                let newInterest = prevInterest + dailyInterest
                
                // Apply any fees for this day
                let applicableFees = 
                    parameters.FeeTypes
                    |> List.filter (fun feeType -> Fee.isApplicableOnDay feeType currentDay)
                    |> List.sumBy (Fee.calculate parameters.FeeTypes)
                let newFees = prevFees + applicableFees
                
                // Apply payment if present
                match paymentForDay with
                | Some payment ->
                    let appliedPayment = AppliedPayment.generate parameters currentDay payment
                    let newBalance = prevBalance - appliedPayment.PrincipalPortion
                    let finalInterest = newInterest - appliedPayment.InterestPortion  
                    let finalFees = newFees - appliedPayment.FeesPortion
                    (newBalance, finalInterest, finalFees)
                | None ->
                    (prevBalance, newInterest, newFees)
            ) (parameters.Principal, 0m<Currency>, 0m<Currency>)
        
        // Convert balance stream to schedule items
        let scheduleItemStream =
            Streams.combine (fun day (principal, interest, fees) ->
                ScheduleItem.create day principal interest fees
            ) dayStream balanceStream
        
        // Collect results
        Streams.toList scheduleItemStream
        |> Array.ofList

    /// Simplified stream-based amortisation that mirrors the original implementation structure
    let amortise (parameters: Parameters) (scheduledPayments: ScheduledPayment array) : ScheduleItem array =
        
        // Generate basic schedule (equivalent to calculateBasicSchedule)
        let basicSchedule = 
            let sp = Scheduling.generate parameters
            [|1<OffsetDay> .. sp.FinalPaymentDay|]
        
        // Create payment map for efficient lookup
        let paymentMap = 
            scheduledPayments 
            |> Array.map (fun sp -> sp.OffsetDay, sp)
            |> Map.ofArray
        
        // Process schedule using streams with state accumulation
        let scheduleStream = Streams.fromSeq basicSchedule
        
        let finalScheduleItems =
            scheduleStream
            |> Streams.scan (fun (scheduleMap, currentPrincipal, currentInterest, currentFees) offsetDay ->
                
                // Calculate daily interest
                let dailyInterest = Interest.Daily.calculate parameters.Interest currentPrincipal
                let newInterest = currentInterest + dailyInterest
                
                // Apply any fees for this day
                let dailyFees = 
                    parameters.FeeTypes
                    |> List.filter (fun feeType -> Fee.isApplicableOnDay feeType offsetDay)
                    |> List.sumBy (Fee.calculate parameters.FeeTypes)
                let newFees = currentFees + dailyFees
                
                // Check for scheduled payment
                let (finalPrincipal, finalInterest, finalFees) =
                    match Map.tryFind offsetDay paymentMap with
                    | Some payment ->
                        let appliedPayment = AppliedPayment.generate parameters offsetDay payment
                        (currentPrincipal - appliedPayment.PrincipalPortion,
                         newInterest - appliedPayment.InterestPortion,
                         newFees - appliedPayment.FeesPortion)
                    | None ->
                        (currentPrincipal, newInterest, newFees)
                
                // Create schedule item
                let scheduleItem = ScheduleItem.create offsetDay finalPrincipal finalInterest finalFees
                let updatedMap = Map.add offsetDay scheduleItem scheduleMap
                
                (updatedMap, finalPrincipal, finalInterest, finalFees)
                
            ) (Map.empty, parameters.Principal, 0m<Currency>, 0m<Currency>)
            |> Streams.toList
            |> List.map (fun (scheduleMap, _, _, _) -> scheduleMap)
            |> List.last
        
        // Convert map to array, sorted by offset day
        finalScheduleItems
        |> Map.toArray
        |> Array.map snd
        |> Array.sortBy (fun si -> si.OffsetDay)
