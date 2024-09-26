namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTestsExtra =

    open ArrayExtension
    open Amortisation
    open Calculation
    open Currency
    open DateDay
    open FeesAndCharges
    open Formatting
    open FormattingHelper
    open PaymentSchedule
    open Percentages
    open Rescheduling
    open Util
    open ValueOptionCE

    let interestCapExample : Interest.Cap = {
        Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
        Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
    }

    let asOfDate = Date(2023, 12, 1)
    let startDates = [| -90 .. 5 .. 90 |] |> Array.map (asOfDate.AddDays)
    let advanceAmounts = [| 100_00L<Cent> .. 50_00L<Cent> .. 2500_00L<Cent> |]
    let fees =
        let none = [||]
        let percentage = [| 1m .. 200m |] |> Array.map(fun m -> Amount.Percentage(Percent m, ValueNone, ValueSome RoundDown) |> Fee.FacilitationFee)
        let percentageCapped = [| 1m .. 200m |] |> Array.map(fun m -> Amount.Percentage(Percent m, ValueSome (Amount.Restriction.UpperLimit 50_00L<Cent>), ValueSome RoundDown) |> Fee.FacilitationFee)
        let simple = [| 10_00L<Cent> .. 10_00L<Cent> .. 100_00L<Cent> |] |> Array.map (Amount.Simple >> Fee.FacilitationFee)
        [| none; percentage; percentageCapped; simple |]
    let feesAmortisations = [| Fees.FeeAmortisation.AmortiseBeforePrincipal; Fees.FeeAmortisation.AmortiseProportionately |]
    let feesSettlements = [| Fees.SettlementRefund.None; Fees.SettlementRefund.ProRata ValueNone |]
    let interestRates =
        let daily = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (Percent >> Interest.Rate.Daily)
        let annual = [| 1m .. 20m |] |> Array.map (Percent >> Interest.Rate.Annual)
        [| daily; annual |] |> Array.concat
    let totalInterestCaps =
        let none = [| ValueNone |]
        let totalFixed = [| 100_00L<Cent> .. 100_00L<Cent> .. 500_00L<Cent> |] |> Array.map (Amount.Simple >> ValueSome)
        let totalPercentageCap = [| 50m .. 50m .. 200m |] |> Array.map (fun p -> Amount.Percentage(Percent p, ValueNone, ValueNone) |> ValueSome)
        [| none; totalFixed; totalPercentageCap |] |> Array.concat
    let dailyInterestCaps =
        let none = [| ValueNone |]
        let dailyFixed = [| 1_00L<Cent> .. 1_00L<Cent> .. 10_00L<Cent> |] |> Array.map (Amount.Simple >> ValueSome)
        let dailyPercentageCap = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (fun p -> Amount.Percentage(Percent p, ValueNone, ValueNone) |> ValueSome)
        [| none; dailyFixed; dailyPercentageCap |] |> Array.concat
    let interestGracePeriods = [| 0<DurationDay> .. 1<DurationDay> .. 7<DurationDay> |]
    let promotionalInterestRates =
        let none = [||]
        let some = [| ({ DateRange = { DateRange.Start = Date(2024, 3, 1); DateRange.End = Date(2024, 12, 31) }; Rate = Interest.Rate.Daily (Percent 0.02m) } : Interest.PromotionalRate) |]
        [| none; some |]
    let unitPeriodConfigs (startDate: Date) =
        let daily = [| 4 .. 32 |] |> Array.map (startDate.AddDays >> UnitPeriod.Config.Daily)
        let weekly = [| 1; 2; 4; 8 |] |> Array.collect(fun multiple -> [| 4 .. 32 |] |> Array.map(fun d -> UnitPeriod.Config.Weekly(multiple, (startDate.AddDays d))))
        let semiMonthly =
            [| 4 .. 32 |]
            |> Array.collect(fun d -> 
                startDate.AddDays d
                |> fun sd ->
                    if sd.Day < 15 then [| sd.Day + 15 |] elif sd.Day = 15 then [| 30; 31 |] elif sd.Day < 31 then [| sd.Day - 15 |] else [| 15 |]
                    |> Array.map(fun td2 -> 
                        UnitPeriod.Config.SemiMonthly(sd.Year, sd.Month, sd.Day * 1, td2 * 1)
                    )
            )
        let monthly =
            [| 1; 2; 3; 6 |]
            |> Array.collect(fun multiple ->
                [| 4 .. 32 |]
                |> Array.map(fun d ->
                    startDate.AddDays d
                    |> fun sd -> UnitPeriod.Config.Monthly(multiple, sd.Year, sd.Month, sd.Day * 1)
                )
            )
        [| daily; weekly; semiMonthly; monthly |] |> Array.concat
    let paymentCounts = [| 1 .. 26 |]
    let aprCalculationMethods = [| Apr.CalculationMethod.UnitedKingdom 3; Apr.CalculationMethod.UsActuarial 8 |]
    let charges =
        let insufficientFundses = [| 0L<Cent>; 7_50L<Cent> |] |> Array.map (Amount.Simple >> Charge.InsufficientFunds)
        let latePayments = [| 0L<Cent>; 10_00L<Cent> |] |> Array.map (Amount.Simple >> Charge.LatePayment)
        [| insufficientFundses; latePayments |]
    let roundingOptions =
        let interestRoundings = [| RoundDown; RoundUp; Round MidpointRounding.ToEven; Round MidpointRounding.AwayFromZero; |]
        let paymentRoundings = [| RoundDown; RoundUp; Round MidpointRounding.ToEven; Round MidpointRounding.AwayFromZero; |]
        interestRoundings
        |> Array.collect(fun ir -> paymentRoundings |> Array.map(fun pr -> { ChargesRounding = RoundDown; FeesRounding = RoundDown; InterestRounding = ir; PaymentRounding = pr } : RoundingOptions ))

    type ScheduledPaymentTestItem = {
        TestId: string
        FinalPayment: int64<Cent>
        LevelPayment: int64<Cent>
        PrincipalBalance: int64<Cent>
        FeesBalance: int64<Cent>
        InterestBalance: decimal<Cent>
        ChargesBalance: int64<Cent>
        BalanceStatus: BalanceStatus
        InterestPortionTotal: int64<Cent>
        AdvanceTotal: int64<Cent>
        PrincipalPortionTotal: int64<Cent>
    }

    let applyPayments sp =
        let aod = sp.AsOfDate.ToString()
        let sd = sp.StartDate.ToString()
        let p = sp.Principal
        let pf = sp.FeesAndCharges.Fees
        let pfs = sp.FeesAndCharges.FeesSettlementRefund
        let ir = Interest.Rate.serialise sp.Interest.StandardRate
        let ic = sp.Interest.Cap
        let igp = sp.Interest.InitialGracePeriod
        let pir = match sp.Interest.PromotionalRates with [||] -> "()" | pirr -> pirr |> Array.map(fun pr -> $"""({pr.DateRange.Start.ToString()}-{pr.DateRange.End.ToString()}__{Interest.Rate.serialise pr.Rate})""") |> String.concat ";" |> fun s -> $"({s})"
        let upc, pc =
            match sp.PaymentSchedule with
            | RegularSchedule(unitPeriodConfig, paymentCount, maxDuration) ->
                (UnitPeriod.Config.serialise unitPeriodConfig), paymentCount
            | _ -> failwith "Not implemented"
        let acm = sp.Calculation.AprMethod
        let pcc = sp.FeesAndCharges.Charges
        let ro = sp.Calculation.RoundingOptions
        let testId = $"""aod{aod}_sd{sd}_p{p}_pf{pf}_pfs{pfs}_ir{ir}_ic{ic}_igp{igp}_pir{pir}_upc{upc}_pc{pc}_acm{acm}_pcc{pcc}_ro{ro}"""
        let amortisationSchedule = 
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> Array.filter _.Payment.IsSome |> Array.map(fun si -> si.Day, [| ActualPayment.QuickConfirmed si.Payment.Value |]) |> Map.ofArray
                let! amortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
                return amortisationSchedule.ScheduleItems
            }
        amortisationSchedule |> ValueOption.iter(fun aa -> 
            let a = Map.maxKeyValue aa |> snd
            let finalPayment = a.NetEffect
            let levelPayment = aa |> Map.values |> Seq.filter(fun a -> a.NetEffect > 0L<Cent>) |> Seq.countBy _.NetEffect |> fun a -> (if Seq.isEmpty a then None else a |> Seq.maxBy snd |> fst |> Some) |> Option.defaultValue 0L<Cent>
            let principalBalance = a.PrincipalBalance 
            let feesBalance = a.FeesBalance 
            let interestBalance = a.InterestBalance 
            let chargesBalance = a.ChargesBalance 
            let balanceStatus = a.BalanceStatus
            let advanceTotal = aa |> Map.values |> Seq.collect _.Advances |> Seq.sum
            let principalPortionTotal = aa |> Map.values |> Seq.sumBy _.PrincipalPortion
            if finalPayment > levelPayment
                || (principalBalance, feesBalance, interestBalance, chargesBalance) <> (0L<Cent>, 0L<Cent>, 0m<Cent>, 0L<Cent>)
                || balanceStatus <> ClosedBalance
                || principalPortionTotal <> advanceTotal
                then
                    aa |> outputMapToHtml $"out/ActualPaymentTestsExtra_{testId}.md" false
            else
                ()
        )
        testId, amortisationSchedule

    let generateRandomAppliedPayments asOfDate size =
        let rnd = Random()
        seq { 1 .. size }
        |> Seq.map(fun _ ->
            let takeRandomFrom (a: _ array) = a |> Array.length |> rnd.Next |> fun r -> Array.item r a
            let sd  = takeRandomFrom startDates
            let aa  = takeRandomFrom advanceAmounts
            let f  = takeRandomFrom fees
            let fa = takeRandomFrom feesAmortisations
            let fs = takeRandomFrom feesSettlements
            let ir  = takeRandomFrom interestRates
            let tic  = takeRandomFrom totalInterestCaps
            let dic  = takeRandomFrom dailyInterestCaps
            let igp = takeRandomFrom interestGracePeriods
            let pir  = takeRandomFrom promotionalInterestRates
            let upc = takeRandomFrom <| unitPeriodConfigs sd
            let pc  = takeRandomFrom paymentCounts
            let acm = takeRandomFrom aprCalculationMethods
            let c = takeRandomFrom charges
            let ro = takeRandomFrom roundingOptions
            {
                AsOfDate = asOfDate
                StartDate = sd
                Principal = aa
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = upc,
                    PaymentCount = pc,
                    MaxDuration = ValueNone
                )
                PaymentOptions = {
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                }
                FeesAndCharges = {
                    Fees = f
                    FeesAmortisation = fa
                    FeesSettlementRefund = fs
                    Charges = c
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Method = Interest.Method.Simple
                    StandardRate = ir
                    Cap = { Total = tic; Daily = dic }
                    InitialGracePeriod = igp
                    PromotionalRates = pir
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = acm
                    RoundingOptions = ro
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }
            |> applyPayments
        )

    let generateAllAppliedPayments asOfDate = // warning: very large seq
        startDates |> Seq.collect(fun sd ->
        advanceAmounts |> Seq.collect(fun aa ->
        fees |> Seq.collect(fun f ->
        feesAmortisations |> Seq.collect(fun fa ->
        feesSettlements |> Seq.collect(fun fs ->
        interestRates |> Seq.collect(fun ir ->
        totalInterestCaps |> Seq.collect(fun tic ->
        dailyInterestCaps |> Seq.collect(fun dic ->
        interestGracePeriods |> Seq.collect(fun igp ->
        promotionalInterestRates |> Seq.collect(fun ih ->
        unitPeriodConfigs sd |> Seq.collect(fun upc ->
        paymentCounts |> Seq.collect(fun pc ->
        aprCalculationMethods |> Seq.collect(fun acm ->
        charges |> Seq.collect(fun c ->
        roundingOptions
        |> Seq.map(fun ro ->
            {
                AsOfDate = asOfDate
                StartDate = sd
                Principal = aa
                PaymentSchedule = RegularSchedule (
                    UnitPeriodConfig = upc,
                    PaymentCount = pc,
                    MaxDuration = ValueNone
                )
                PaymentOptions = {
                    ScheduledPaymentOption = AsScheduled
                    CloseBalanceOption = LeaveOpenBalance
                }
                FeesAndCharges = {
                    Fees = f
                    FeesAmortisation = fa
                    FeesSettlementRefund = fs
                    Charges = c
                    ChargesHolidays = [||]
                    ChargesGrouping = OneChargeTypePerDay
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Method = Interest.Method.Simple
                    StandardRate = ir
                    Cap = { Total = tic; Daily = dic }
                    InitialGracePeriod = igp
                    PromotionalRates = ih
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = acm
                    RoundingOptions = ro
                    MinimumPayment = DeferOrWriteOff 50L<Cent>
                    PaymentTimeout = 3<DurationDay>
                }
            }
        )))))))))))))))
        |> Seq.map applyPayments

    let generateRegularPaymentTestData (amortisationSchedule: (string * Amortisation.ScheduleItem array voption) seq) =
        amortisationSchedule
        |> Seq.map(fun (testId, items) ->
            match items with
            | ValueSome aa ->
                let a = aa |> Array.last
                {
                    TestId = testId
                    FinalPayment = a |> _.NetEffect
                    LevelPayment = aa |> Array.countBy _.NetEffect |> fun a -> (if Seq.isEmpty a then None else a |> Seq.maxBy snd |> fst |> Some) |> Option.defaultValue 0L<Cent>
                    PrincipalBalance = a.PrincipalBalance
                    FeesBalance = a.FeesBalance
                    InterestBalance = a.InterestBalance
                    ChargesBalance = a.ChargesBalance
                    BalanceStatus = a.BalanceStatus
                    InterestPortionTotal = aa |> Array.sumBy _.InterestPortion
                    AdvanceTotal = aa |> Array.collect _.Advances |> Array.sum
                    PrincipalPortionTotal = aa |> Array.sumBy _.PrincipalPortion
                }
            | ValueNone -> failwith $"Unable to calculate regular payment test data for test ID: {testId}"
        )
        |> Seq.toArray
        |> toMemberData

    // let randomAppliedPayments = generateRandomAppliedPayments asOfDate 1000

    // let regularPaymentTestData =
    //     generateRegularPaymentTestData randomAppliedPayments 

    // [<Theory>]
    // [<MemberData(nameof(regularPaymentTestData))>]
    // let ``Final payment should be less than level payment`` (testItem: ScheduledPaymentTestItem) =
    //     testItem.FinalPayment |> should be (lessThanOrEqualTo testItem.LevelPayment)

    // [<Theory>]
    // [<MemberData(nameof(regularPaymentTestData))>]
    // let ``Final balances should all be 0L<Cent>`` (testItem: ScheduledPaymentTestItem) =
    //     (testItem.PrincipalBalance, testItem.FeesBalance, testItem.InterestBalance, testItem.ChargesBalance) |> should equal (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>)

    // [<Theory>]
    // [<MemberData(nameof(regularPaymentTestData))>]
    // let ``Final balance status should be settled`` (testItem: ScheduledPaymentTestItem) =
    //     (testItem.BalanceStatus) |> should equal ActualPayment.BalanceStatus.Settled

    // [<Theory>]
    // [<MemberData(nameof(regularPaymentTestData))>]
    // let ``Cumulative interest should be the sum of new interest`` (testItem: ScheduledPaymentTestItem) =
    //     testItem.CumulativeInterest |> should equal testItem.InterestPortionTotal

    // [<Theory>]
    // [<MemberData(nameof(regularPaymentTestData))>]
    // let ``The sum of principal portions should equal the sum of advances`` (testItem: ScheduledPaymentTestItem) =
    //     testItem.PrincipalPortionTotal |> should equal testItem.AdvanceTotal

    /// creates an array of actual payments made on time and in full according to an array of scheduled payments
    let allPaidOnTime (scheduleItems: Item array) =
        scheduleItems
        |> Array.filter _.Payment.IsSome
        |> Array.map(fun si -> si.Day, [| ActualPayment.QuickConfirmed si.Payment.Value |])
        |> Map.ofArray

    [<Fact>]
    let ``1) Simple schedule fully settled on time`` () =
        let sp = ({
            AsOfDate = Date(2023, 12, 1)
            StartDate = Date(2023, 7, 23)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 8, 1),
                PaymentCount = 5,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> allPaidOnTime
                let! amortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
                amortisationSchedule.ScheduleItems |> outputMapToHtml $"out/ActualPaymentTestsExtra001.md" false
                return amortisationSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (131<OffsetDay>, {
            OffsetDate = Date(2023, 12, 1)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 407_64L<Cent>) ValueNone
            Window = 5
            PaymentDue = 407_64L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 407_64L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 407_64L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 3_30.67257534m<Cent>
            NewInterest = 3_30.67257534m<Cent>
            NewCharges = [||]
            PrincipalPortion = 161_76L<Cent>
            FeesPortion = 242_58L<Cent>
            InterestPortion = 3_30L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``2) Schedule with a payment on day 0L<Cent>, seen from a date before scheduled payments are due to start`` () =
        let sp = ({
            AsOfDate = Date(2022, 3, 25)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26)),
                PaymentCount = 12,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments =
                    [|
                        0<OffsetDay>, [| ActualPayment.QuickConfirmed 166_60L<Cent> |]
                    |]
                    |> Map.ofArray
                let! amortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
                amortisationSchedule.ScheduleItems |> outputMapToHtml $"out/ActualPaymentTestsExtra002.md" false
                return amortisationSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (172<OffsetDay>, {
            OffsetDate = Date(2022, 8, 27)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 170_90L<Cent>) ValueNone
            Window = 12
            PaymentDue = 170_04L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 170_04L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 64.65046575m<Cent>
            NewInterest = 64.65046575m<Cent>
            NewCharges = [||]
            PrincipalPortion = 67_79L<Cent>
            FeesPortion = 101_61L<Cent>
            InterestPortion = 64L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 170_04L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``3) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, seen from a date after the original settlement date, showing the effect of projected small payments until paid off`` () =
        let sp = ({
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26)),
                PaymentCount = 12,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let originalFinalPaymentDay = generatePaymentMap sp.StartDate sp.PaymentSchedule |> Map.keys |> Seq.toArray |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        let originalFinalPaymentDay' = (int originalFinalPaymentDay - int (sp.AsOfDate - sp.StartDate).Days) * 1<OffsetDay>
        let actual =
            voption {
                let actualPayments =
                    [|
                        0<OffsetDay>, [| ActualPayment.QuickConfirmed 166_60L<Cent> |]
                    |]
                    |> Map.ofArray
                let rp : RescheduleParameters = {
                    FeesSettlementRefund = Fees.SettlementRefund.ProRata (ValueSome originalFinalPaymentDay')
                    PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Weekly(2, Date(2022, 9, 1)); PaymentCount = 155; PaymentAmount = 20_00L<Cent>; ScheduleType = ScheduleType.Rescheduled } |]
                    RateOnNegativeBalance = ValueNone
                    PromotionalInterestRates = [||]
                    ChargesHolidays = [||]
                    IntendedPurpose = IntendedPurpose.Statement
                }
                let! newSchedule = reschedule sp rp actualPayments
                let title = "<h3>3) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, seen from a date after the original settlement date, showing the effect of projected small payments until paid off</h3>"
                let generationOptions = Some { GoParameters = sp; GoPurpose = IntendedPurpose.Statement; GoExtra = true } |> getHideProperties
                let newHtml = newSchedule.ScheduleItems |> generateHtmlFromMap generationOptions
                $"{title}<br />{newHtml}" |> outputToFile' $"out/ActualPaymentTestsExtra003.md" false
                return newSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (1969<OffsetDay>, {
            OffsetDate = Date(2027, 7, 29)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick ValueNone (ValueSome 20_00L<Cent>)
            Window = 141
            PaymentDue = 9_80L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 9_80L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 3.72866027m<Cent>
            NewInterest = 3.72866027m<Cent>
            NewCharges = [||]
            PrincipalPortion = 4_39L<Cent>
            FeesPortion = 5_38L<Cent>
            InterestPortion = 3L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 9_80L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``4) never settles down`` () =
        let sp = ({
            AsOfDate = Date(2026, 8, 27)
            StartDate = Date(2023, 11, 6)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly (8, Date(2023, 11, 23)),
                PaymentCount = 19,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 164m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.None
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.12m)
                Cap = { Total = ValueSome <| Amount.Simple 500_00L<Cent>; Daily = ValueNone }
                InitialGracePeriod = 7<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> allPaidOnTime
                let! amortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
                amortisationSchedule.ScheduleItems |> outputMapToHtml $"out/ActualPaymentTestsExtra004.md" false
                return amortisationSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (1025<OffsetDay>, {
            OffsetDate = Date(2026, 8, 27)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 137_36L<Cent>) ValueNone
            Window = 19
            PaymentDue = 137_36L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 137_36L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 137_36L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 0m<Cent>
            NewInterest = 0m<Cent>
            NewCharges = [||]
            PrincipalPortion = 52_14L<Cent>
            FeesPortion = 85_22L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``5) large negative payment`` () =
        let sp = ({
            AsOfDate = Date(2023, 12, 11)
            StartDate = Date(2022, 9, 11)
            Principal = 200_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Monthly (1, 2022, 9, 15),
                PaymentCount = 7,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [||]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCapExample
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> allPaidOnTime
                let! amortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
                amortisationSchedule.ScheduleItems |> outputMapToHtml $"out/ActualPaymentTestsExtra005.md" false
                return amortisationSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (185<OffsetDay>, {
            OffsetDate = Date(2023, 3, 15)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 51_53L<Cent>) ValueNone
            Window = 7
            PaymentDue = 51_53L<Cent>
            ActualPayments = [| { ActualPaymentStatus = ActualPaymentStatus.Confirmed 51_53L<Cent>; Metadata = Map.empty } |]
            GeneratedPayment = ValueNone
            NetEffect = 51_53L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 9_43.040m<Cent>
            NewInterest = 9_43.040m<Cent>
            NewCharges = [||]
            PrincipalPortion = 42_10L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 9_43L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 0L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``6) Schedule with a payment on day 0L<Cent>, seen from a date after the first unpaid scheduled payment, but within late-payment grace period`` () =
        let sp = ({
            AsOfDate = Date(2022, 4, 1)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26)),
                PaymentCount = 12,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 7<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let actual =
            voption {
                let actualPayments =
                    [|
                        0<OffsetDay>, [| ActualPayment.QuickConfirmed 166_60L<Cent> |]
                    |]
                    |> Map.ofArray
                let! amortisationSchedule = Amortisation.generate sp IntendedPurpose.Statement ScheduleType.Original false actualPayments
                amortisationSchedule.ScheduleItems |> outputMapToHtml $"out/ActualPaymentTestsExtra006.md" false
                return amortisationSchedule.ScheduleItems
            }
            |> ValueOption.bind (Map.tryFind 144<OffsetDay> >> toValueOption)
        let expected = ValueSome ({
            OffsetDate = Date(2022, 7, 30)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 171_02L<Cent>) ValueNone
            Window = 10
            PaymentDue = 142_40L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 142_40L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 1_28.41170136m<Cent>
            NewInterest = 1_28.41170136m<Cent>
            NewCharges = [||]
            PrincipalPortion = 134_62L<Cent>
            FeesPortion = 6_50L<Cent>
            InterestPortion = 1_28L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 195_35L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 142_40L<Cent>
            FeesRefundIfSettled = 195_35L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``7) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fees rolled over)`` () =
        let sp = ({
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26)),
                PaymentCount = 12,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let originalFinalPaymentDay = generatePaymentMap sp.StartDate sp.PaymentSchedule |> Map.keys |> Seq.toArray |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        let originalFinalPaymentDay' = (int originalFinalPaymentDay - int (sp.AsOfDate - sp.StartDate).Days) * 1<OffsetDay>
        let actual =
            voption {
                let actualPayments =
                    [|
                        0<OffsetDay>, [| ActualPayment.QuickConfirmed 166_60L<Cent> |]
                    |]
                    |> Map.ofArray
                let rp : RolloverParameters = {
                    OriginalFinalPaymentDay = originalFinalPaymentDay'
                    PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Weekly(2, Date(2022, 9, 1)); PaymentCount = 155; PaymentAmount = 20_00L<Cent>; ScheduleType = ScheduleType.Original } |]
                    FeesAndCharges = ValueNone
                    Interest = ValueNone
                    Calculation = ValueNone
                    FeeHandling = Fees.FeeHandling.CarryOverAsIs
                }
                let! newSchedule = rollOver sp rp actualPayments
                let title = "<h3>7) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fees rolled over)</h3>"
                let newHtml = newSchedule.ScheduleItems |> generateHtmlFromMap [||]
                $"{title}<br />{newHtml}" |> outputToFile' $"out/ActualPaymentTestsExtra007.md" false
                return newSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (1793<OffsetDay>, {
            OffsetDate = Date(2027, 7, 29)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 20_00L<Cent>) ValueNone
            Window = 129
            PaymentDue = 18_71L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 18_71L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 7.11384109m<Cent>
            NewInterest = 7.11384109m<Cent>
            NewCharges = [||]
            PrincipalPortion = 9_26L<Cent>
            FeesPortion = 9_38L<Cent>
            InterestPortion = 7L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 18_71L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``8) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fees not rolled over)`` () =
        let sp = ({
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            PaymentSchedule = RegularSchedule (
                UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26)),
                PaymentCount = 12,
                MaxDuration = ValueNone
            )
            PaymentOptions = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
            }
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone, ValueSome RoundDown)) |]
                FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
                FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                ChargesHolidays = [||]
                ChargesGrouping = OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Method = Interest.Method.Simple
                StandardRate = Interest.Rate.Annual (Percent 9.95m)
                Cap = Interest.Cap.none
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = RoundingOptions.recommended
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
        })
        let originalFinalPaymentDay = generatePaymentMap sp.StartDate sp.PaymentSchedule |> Map.keys |> Seq.toArray |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
        let originalFinalPaymentDay' = (int originalFinalPaymentDay - int (sp.AsOfDate - sp.StartDate).Days) * 1<OffsetDay>
        let actual =
            voption {
                let actualPayments =
                    [|
                        0<OffsetDay>, [| ActualPayment.QuickConfirmed 166_60L<Cent> |]
                    |]
                    |> Map.ofArray
                let rp : RolloverParameters = {
                    OriginalFinalPaymentDay = originalFinalPaymentDay'
                    PaymentSchedule = RegularFixedSchedule [| { UnitPeriodConfig = UnitPeriod.Config.Weekly(2, Date(2022, 9, 1)); PaymentCount = 155; PaymentAmount = 20_00L<Cent>; ScheduleType = ScheduleType.Original } |]
                    FeesAndCharges = ValueNone
                    Interest = ValueNone
                    Calculation = ValueNone
                    FeeHandling = Fees.FeeHandling.CapitaliseAsPrincipal
                }
                let! newSchedule = rollOver sp rp actualPayments
                let title = "<h3>8) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, then loan rolled over (fees not rolled over)</h3>"
                let newHtml = newSchedule.ScheduleItems |> generateHtmlFromMap [||]
                $"{title}<br />{newHtml}" |> outputToFile' $"out/ActualPaymentTestsExtra008.md" false
                return newSchedule.ScheduleItems
            }
            |> ValueOption.map Map.maxKeyValue
        let expected = ValueSome (1793<OffsetDay>, {
            OffsetDate = Date(2027, 7, 29)
            Advances = [||]
            ScheduledPayment = ScheduledPayment.Quick (ValueSome 20_00L<Cent>) ValueNone
            Window = 129
            PaymentDue = 18_71L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 18_71L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            OriginalSimpleInterest = 0L<Cent>
            ContractualInterest = 0m<Cent>
            SimpleInterest = 7.11384109m<Cent>
            NewInterest = 7.11384109m<Cent>
            NewCharges = [||]
            PrincipalPortion = 18_64L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 7L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0m<Cent>
            ChargesBalance = 0L<Cent>
            SettlementFigure = 18_71L<Cent>
            FeesRefundIfSettled = 0L<Cent>
        })
        actual |> should equal expected

