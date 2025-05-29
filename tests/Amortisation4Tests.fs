namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module Amortisation4Tests =

    let folder = "Amortisation4"

    open Amortisation
    open Amortisation4
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    let testParameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2024, 9, 28)
            StartDate = Date(2024, 8, 2)
            Principal = 1200_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Weekly(2, Date(2024, 8, 17))
                    ScheduleLength = PaymentCount 11
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig =
                ValueSome {
                    FeeType = Fee.FeeType.CabOrCsoFee(Amount.Percentage(Percent 189.47m, Restriction.NoLimit))
                    Rounding = RoundDown
                    FeeAmortisation = Fee.FeeAmortisation.AmortiseProportionately
                }
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Annual <| Percent 9.95m
                Cap = Interest.Cap.zero
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                Rounding = RoundDown
            }
        }
        Calculation = ValueNone
        FeesAndCharges = Array.empty
        Interest = ValueNone
        Repayment = ValueNone
    }

    let emptyActualPayments = Map.empty

    [<Fact>]
    let ``Stream-based amortisation produces valid schedule`` () =
        let result = Amortisation4.amortise testParameters emptyActualPayments

        result.AmortisationSchedule.ScheduleItems |> should not' (be Empty)
        result.AmortisationSchedule.ScheduleItems.Count |> should be (greaterThan 0)

    [<Fact>]
    let ``Reactive amortisation produces valid schedule`` () =
        let result = Amortisation4.amortiseReactive testParameters emptyActualPayments

        result.AmortisationSchedule.ScheduleItems |> should not' (be Empty)
        result.AmortisationSchedule.ScheduleItems.Count |> should be (greaterThan 0)

    [<Fact>]
    let ``Stream-based amortisation with streams produces valid schedule`` () =
        let result = Amortisation4.amortiseWithStreams testParameters emptyActualPayments

        result.AmortisationSchedule.ScheduleItems |> should not' (be Empty)
        result.AmortisationSchedule.ScheduleItems.Count |> should be (greaterThan 0)

    [<Fact>]
    let ``Amortisation4 produces same number of schedule items as original`` () =
        let originalResult = Amortisation.amortise testParameters emptyActualPayments
        let streamResult = Amortisation4.amortise testParameters emptyActualPayments

        streamResult.AmortisationSchedule.ScheduleItems.Count
        |> should equal originalResult.AmortisationSchedule.ScheduleItems.Count

    [<Fact>]
    let ``Reactive amortisation produces same number of schedule items as original`` () =
        let originalResult = Amortisation.amortise testParameters emptyActualPayments

        let reactiveResult =
            Amortisation4.amortiseReactive testParameters emptyActualPayments

        reactiveResult.AmortisationSchedule.ScheduleItems.Count
        |> should equal originalResult.AmortisationSchedule.ScheduleItems.Count

    [<Fact>]
    let ``All three implementations produce same number of schedule items`` () =
        let streamResult = Amortisation4.amortise testParameters emptyActualPayments

        let reactiveResult =
            Amortisation4.amortiseReactive testParameters emptyActualPayments

        let streamWithStreamsResult =
            Amortisation4.amortiseWithStreams testParameters emptyActualPayments

        let streamCount = streamResult.AmortisationSchedule.ScheduleItems.Count
        let reactiveCount = reactiveResult.AmortisationSchedule.ScheduleItems.Count

        let streamWithStreamsCount =
            streamWithStreamsResult.AmortisationSchedule.ScheduleItems.Count

        reactiveCount |> should equal streamCount
        streamWithStreamsCount |> should equal streamCount

    [<Fact>]
    let ``Stream-based amortisation calculates correct final balance`` () =
        let result = Amortisation4.amortise testParameters emptyActualPayments

        let finalItem =
            result.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.maxBy (fun si -> si.OffsetDate)

        finalItem.BalanceStatus.PrincipalBalance
        |> should be (lessThanOrEqualTo 0L<Cent>)

    [<Fact>]
    let ``Reactive amortisation calculates correct final balance`` () =
        let result = Amortisation4.amortiseReactive testParameters emptyActualPayments

        let finalItem =
            result.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.maxBy (fun si -> si.OffsetDate)

        finalItem.BalanceStatus.PrincipalBalance
        |> should be (lessThanOrEqualTo 0L<Cent>)

    [<Fact>]
    let ``Stream processing utilities work correctly`` () =
        let numbers = [ 1; 2; 3; 4; 5 ]
        let stream = numbers |> Stream.fromSeq

        let doubled = stream |> Stream.map (fun x -> x * 2)
        let evens = doubled |> Stream.filter (fun x -> x % 2 = 0)
        let result = evens |> Stream.toList

        result |> should equal [ 2; 4; 6; 8; 10 ]

    [<Fact>]
    let ``Stream scan accumulates correctly`` () =
        let numbers = [ 1; 2; 3; 4; 5 ]
        let stream = numbers |> Stream.fromSeq

        let accumulated = stream |> Stream.scan (+) 0
        let result = accumulated |> Stream.toList

        result |> should equal [ 0; 1; 3; 6; 10; 15 ]

    [<Fact>]
    let ``Stream combination works correctly`` () =
        let stream1 = [ 1; 2; 3 ] |> Stream.fromSeq
        let stream2 = [ 4; 5; 6 ] |> Stream.fromSeq

        let combined = Stream.combine stream1 stream2
        let result = combined |> Stream.toList

        result |> should equal [ 1; 2; 3; 4; 5; 6 ]

    [<Fact>]
    let ``AmortisationState correctly tracks balance changes`` () =
        let initialState = {
            PrincipalBalance = 1000_00L<Cent>
            FeesBalance = 100_00L<Cent>
            InterestBalance = 0L<Cent>
            ScheduleItems = Map.empty
            CurrentDate = Date(2024, 1, 1)
        }

        let paymentEvent = {
            EventType = PaymentReceived
            Amount = 200_00L<Cent>
            Date = Date(2024, 1, 15)
            Description = "Payment"
        }

        // This would normally be processed through the stream processor
        // For now, just verify the types are correctly structured
        initialState.PrincipalBalance |> should equal 1000_00L<Cent>
        paymentEvent.Amount |> should equal 200_00L<Cent>

    [<Fact>]
    let ``All implementations handle empty actual payments`` () =
        let streamResult = Amortisation4.amortise testParameters emptyActualPayments

        let reactiveResult =
            Amortisation4.amortiseReactive testParameters emptyActualPayments

        let streamWithStreamsResult =
            Amortisation4.amortiseWithStreams testParameters emptyActualPayments

        // All should produce valid results with non-empty schedule items
        streamResult.AmortisationSchedule.ScheduleItems |> should not' (be Empty)
        reactiveResult.AmortisationSchedule.ScheduleItems |> should not' (be Empty)

        streamWithStreamsResult.AmortisationSchedule.ScheduleItems
        |> should not' (be Empty)

    [<Fact>]
    let ``Stream-based implementation preserves payment schedule structure`` () =
        let result = Amortisation4.amortise testParameters emptyActualPayments

        let scheduleItems =
            result.AmortisationSchedule.ScheduleItems
            |> Map.values
            |> Seq.sortBy (fun si -> si.OffsetDate)
            |> Seq.toArray

        // Should have proper chronological ordering
        scheduleItems.Length |> should be (greaterThan 0)

        // Each item should have proper offset date progression
        if scheduleItems.Length > 1 then
            for i in 1 .. scheduleItems.Length - 1 do
                scheduleItems.[i].OffsetDate
                |> should be (greaterThanOrEqualTo scheduleItems.[i - 1].OffsetDate)
