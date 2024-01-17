namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open FSharp.Finance.Personal.ActualPayment

module ActualPaymentTestsExtra =

    let asOfDate = Date(2023, 12, 1)
    let startDates = [| -90 .. 5 .. 90 |] |> Array.map (asOfDate.AddDays)
    let advanceAmounts = [| 10000L<Cent> .. 5000L<Cent> .. 250000L<Cent> |]
    let fees =
        let none = [| ValueNone |]
        let percentage = [| 1m .. 200m |] |> Array.map(fun m -> Fees.Percentage(Percent m, ValueNone) |> ValueSome)
        let percentageCapped = [| 1m .. 200m |] |> Array.map(fun m -> Fees.Percentage(Percent m, ValueSome 5000L<Cent>) |> ValueSome)
        let simple = [| 1000L<Cent> .. 1000L<Cent> .. 10000L<Cent> |] |> Array.map (Fees.Simple >> ValueSome)
        [| none; percentage; percentageCapped; simple |] |> Array.concat
    let feesSettlements = [| Fees.Settlement.DueInFull; Fees.Settlement.ProRataRefund |]
    let interestRates =
        let daily = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (Percent >> Interest.Rate.Daily)
        let annual = [| 1m .. 20m |] |> Array.map (Percent >> Interest.Rate.Annual)
        [| daily; annual |] |> Array.concat
    let totalInterestCaps =
        let none = [| ValueNone |]
        let totalFixed = [| 10000L<Cent> .. 10000L<Cent> .. 50000L<Cent> |] |> Array.map (Interest.TotalFixedCap >> ValueSome)
        let totalPercentageCap = [| 50m .. 50m .. 200m |] |> Array.map (Percent >> Interest.TotalPercentageCap >> ValueSome)
        [| none; totalFixed; totalPercentageCap |] |> Array.concat
    let dailyInterestCaps =
        let none = [| ValueNone |]
        let dailyFixed = [| 100L<Cent> .. 100L<Cent> .. 1000L<Cent> |] |> Array.map (Interest.DailyFixedCap >> ValueSome)
        let dailyPercentageCap = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (Percent >> Interest.DailyPercentageCap >> ValueSome)
        [| none; dailyFixed; dailyPercentageCap |] |> Array.concat
    let interestGracePeriods = [| 0<DurationDay> .. 1<DurationDay> .. 7<DurationDay> |]
    let interestHolidays =
        let none = [||]
        let some = [| { Interest.Holiday.Start = Date(2024, 3, 1); Interest.Holiday.End = Date(2024, 12, 31)} |]
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
        let insufficientFundses = [| 0L<Cent>; 750L<Cent> |] |> Array.map Charge.InsufficientFunds
        let latePayments = [| 0L<Cent>; 1000L<Cent> |] |> Array.map Charge.LatePayment
        insufficientFundses |> Array.collect(fun if' -> latePayments |> Array.map(fun lp -> [| if'; lp |]))
    let roundingOptions =
        let interestRoundings = [| RoundDown; RoundUp; Round MidpointRounding.ToEven; Round MidpointRounding.AwayFromZero; |]
        let paymentRoundings = [| RoundDown; RoundUp; Round MidpointRounding.ToEven; Round MidpointRounding.AwayFromZero; |]
        interestRoundings
        |> Array.collect(fun ir -> paymentRoundings |> Array.map(fun pr -> { InterestRounding = ir; PaymentRounding = pr }))
    let finalPaymentAdjustments = [| ScheduledPayment.AdjustFinalPayment; ScheduledPayment.SpreadOverLevelPayments |]

    type ScheduledPaymentTestItem = {
        TestId: string
        FinalPayment: int64<Cent>
        LevelPayment: int64<Cent>
        PrincipalBalance: int64<Cent>
        FeesBalance: int64<Cent>
        InterestBalance: int64<Cent>
        ChargesBalance: int64<Cent>
        BalanceStatus: ActualPayment.BalanceStatus
        CumulativeInterest: int64<Cent>
        InterestPortionTotal: int64<Cent>
        AdvanceTotal: int64<Cent>
        PrincipalPortionTotal: int64<Cent>
    }

    let applyPayments (sp: ScheduledPayment.ScheduleParameters) =
        let aod = sp.AsOfDate.ToString()
        let sd = sp.StartDate.ToString()
        let p = sp.Principal
        let pf = sp.FeesAndCharges.Fees
        let pfs = sp.FeesAndCharges.FeesSettlement
        let ir = Interest.Rate.serialise sp.Interest.Rate
        let ic = sp.Interest.Cap
        let igp = sp.Interest.GracePeriod
        let ih = match sp.Interest.Holidays with [||] -> "()" | ihh -> ihh |> Array.map(fun ih -> $"""({ih.Start.ToString()}-{ih.End.ToString()})""") |> String.concat ";" |> fun s -> $"({s})"
        let upc = UnitPeriod.Config.serialise sp.UnitPeriodConfig
        let pc = sp.PaymentCount
        let acm = sp.Calculation.AprMethod
        let pcc = sp.FeesAndCharges.Charges
        let ro = sp.Calculation.RoundingOptions
        let fpa = sp.Calculation.FinalPaymentAdjustment
        let testId = $"""aod{aod}_sd{sd}_p{p}_pf{pf}_pfs{pfs}_ir{ir}_ic{ic}_igp{igp}_ih{ih}_upc{upc}_pc{pc}_acm{acm}_pcc{pcc}_ro{ro}_fpa{fpa}"""
        let amortisationSchedule = 
            voption {
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ActualPayments ([| si.Payment |], [||]) } : ActualPayment.Payment)
                return
                    scheduleItems
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
            }
        amortisationSchedule |> ValueOption.iter(fun aa -> 
            let a = Array.last aa
            let finalPayment = a.NetEffect
            let levelPayment = aa |> Array.filter(fun a -> a.NetEffect > 0L<Cent>) |> Array.countBy _.NetEffect |> Array.maxByOrDefault snd fst 0L<Cent>
            let principalBalance = a.PrincipalBalance 
            let feesBalance = a.FeesBalance 
            let interestBalance = a.InterestBalance 
            let chargesBalance = a.ChargesBalance 
            let balanceStatus = a.BalanceStatus
            let cumulativeInterest = a.CumulativeInterest
            let interestPortionTotal = aa |> Array.sumBy _.InterestPortion
            let advanceTotal = aa |> Array.sumBy _.Advance
            let principalPortionTotal = aa |> Array.sumBy _.PrincipalPortion
            if finalPayment > levelPayment
                || (principalBalance, feesBalance, interestBalance, chargesBalance) <> (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>)
                || balanceStatus <> ActualPayment.BalanceStatus.Settled
                || cumulativeInterest <> interestPortionTotal
                || principalPortionTotal <> advanceTotal
                then
                    aa |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra_{testId}.md" (ValueSome 300)
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
            let pf  = takeRandomFrom fees
            let pfs = takeRandomFrom feesSettlements
            let ir  = takeRandomFrom interestRates
            let tic  = takeRandomFrom totalInterestCaps
            let dic  = takeRandomFrom dailyInterestCaps
            let igp = takeRandomFrom interestGracePeriods
            let ih  = takeRandomFrom interestHolidays
            let upc = takeRandomFrom <| unitPeriodConfigs sd
            let pc  = takeRandomFrom paymentCounts
            let acm = takeRandomFrom aprCalculationMethods
            let pcc = takeRandomFrom charges
            let ro = takeRandomFrom roundingOptions
            let fpa = takeRandomFrom finalPaymentAdjustments
            {
                AsOfDate = asOfDate
                StartDate = sd
                Principal = aa
                UnitPeriodConfig = upc
                PaymentCount = pc
                FeesAndCharges = {
                    Fees = pf
                    FeesSettlement = pfs
                    Charges = pcc
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = ir
                    Cap = { Total = tic; Daily = dic }
                    GracePeriod = igp
                    Holidays = ih
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = acm
                    RoundingOptions = ro
                    FinalPaymentAdjustment = fpa
                }
            } : ScheduledPayment.ScheduleParameters
            |> applyPayments
        )

    let generateAllAppliedPayments asOfDate = // warning: very large seq
        startDates |> Seq.collect(fun sd ->
        advanceAmounts |> Seq.collect(fun aa ->
        fees |> Seq.collect(fun pf ->
        feesSettlements |> Seq.collect(fun pfs ->
        interestRates |> Seq.collect(fun ir ->
        totalInterestCaps |> Seq.collect(fun tic ->
        dailyInterestCaps |> Seq.collect(fun dic ->
        interestGracePeriods |> Seq.collect(fun igp ->
        interestHolidays |> Seq.collect(fun ih ->
        unitPeriodConfigs sd |> Seq.collect(fun upc ->
        paymentCounts |> Seq.collect(fun pc ->
        aprCalculationMethods |> Seq.collect(fun acm ->
        charges |> Seq.collect(fun pcc ->
        roundingOptions |> Seq.collect(fun ro ->
        finalPaymentAdjustments
        |> Seq.map(fun fpa ->
            {
                AsOfDate = asOfDate
                StartDate = sd
                Principal = aa
                UnitPeriodConfig = upc
                PaymentCount = pc
                FeesAndCharges = {
                    Fees = pf
                    FeesSettlement = pfs
                    Charges = pcc
                    LatePaymentGracePeriod = 0<DurationDay>
                }
                Interest = {
                    Rate = ir
                    Cap = { Total = tic; Daily = dic }
                    GracePeriod = igp
                    Holidays = ih
                    RateOnNegativeBalance = ValueNone
                }
                Calculation = {
                    AprMethod = acm
                    RoundingOptions = ro
                    FinalPaymentAdjustment = fpa
                }
            } : ScheduledPayment.ScheduleParameters
        )))))))))))))))
        |> Seq.map applyPayments

    let generateRegularPaymentTestData (amortisationSchedule: (string * ActualPayment.AmortisationScheduleItem array voption) seq) =
        amortisationSchedule
        |> Seq.map(fun (testId, items) ->
            match items with
            | ValueSome aa ->
                let a = aa |> Array.last
                {
                    TestId = testId
                    FinalPayment = a |> _.NetEffect
                    LevelPayment = aa |> Array.countBy _.NetEffect |> Array.maxByOrDefault snd fst 0L<Cent>
                    PrincipalBalance = a.PrincipalBalance
                    FeesBalance = a.FeesBalance
                    InterestBalance = a.InterestBalance
                    ChargesBalance = a.ChargesBalance
                    BalanceStatus = a.BalanceStatus
                    CumulativeInterest = a.CumulativeInterest
                    InterestPortionTotal = aa |> Array.sumBy _.InterestPortion
                    AdvanceTotal = aa |> Array.sumBy _.Advance
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
    // let ``Final balances should all be zero`` (testItem: ScheduledPaymentTestItem) =
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

    [<Fact>]
    let ``1) Simple schedule looked at from a date in the past showing projected to be fully settled on time`` () =
        let sp = ({
            AsOfDate = Date(2023, 7, 23)
            StartDate = Date(2023, 7, 23)
            Principal = 80000L<Cent>
            UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 8, 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = ValueSome(Fees.Percentage (Percent 150m, ValueNone))
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        } : ScheduledPayment.ScheduleParameters)
        let actual =
            voption {
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> ActualPayment.allPaidOnTime
                let amortisationSchedule =
                    scheduleItems
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra001.md" (ValueSome 300)
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2023, 12, 1)
            OffsetDay = 131<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 40764L<Cent>
            ActualPayments = [| 40764L<Cent> |]
            NetEffect = 40764L<Cent>
            PaymentStatus = ValueSome ActualPayment.NotYetDue
            BalanceStatus = ActualPayment.Settled
            CumulativeInterest = 3832L<Cent>
            NewInterest = 330L<Cent>
            NewCharges = [||]
            PrincipalPortion = 16176L<Cent>
            FeesPortion = 24258L<Cent>
            InterestPortion = 330L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        } : ActualPayment.AmortisationScheduleItem)
        actual |> should equal expected

    [<Fact>]
    let ``2) Schedule with a payment on day zero, seen from a date before scheduled payments are due to start`` () =
        let sp = ({
            AsOfDate = Date(2022, 3, 25)
            StartDate = Date(2022, 3, 8)
            Principal = 80000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
            PaymentCount = 12
            FeesAndCharges = {
                Fees = ValueSome(Fees.Percentage (Percent 150m, ValueNone))
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        } : ScheduledPayment.ScheduleParameters)
        let actual =
            voption {
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = [|
                    ({ PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayments ([| 16660L<Cent> |], [||]) } : ActualPayment.Payment)
                |]
                let amortisationSchedule =
                    scheduleItems
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra002.md" (ValueSome 300)
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2022, 7, 30)
            OffsetDay = 144<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 14240L<Cent>
            ActualPayments = [||]
            NetEffect = 14240L<Cent>
            PaymentStatus = ValueSome ActualPayment.NotYetDue
            BalanceStatus = ActualPayment.Settled
            CumulativeInterest = 4353L<Cent>
            NewInterest = 128L<Cent>
            NewCharges = [||]
            PrincipalPortion = 13462L<Cent>
            FeesPortion = 650L<Cent>
            InterestPortion = 128L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 19535L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        } : ActualPayment.AmortisationScheduleItem)
        actual |> should equal expected

    [<Fact>]
    let ``3) Schedule with a payment on day zero, then all scheduled payments missed, seen from a date after the original settlement date, showing the effect of projected small payments until paid off`` () =
        let sp = ({
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 80000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
            PaymentCount = 12
            FeesAndCharges = {
                Fees = ValueSome(Fees.Percentage (Percent 150m, ValueNone))
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        } : ScheduledPayment.ScheduleParameters)
        let actual =
            voption {
                // calculate schedule based on existing payments
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduledPayments =
                    schedule.Items
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                let actualPayments = [|
                    ({ PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayments ([| 16660L<Cent> |], [||]) } : ActualPayment.Payment)
                |]
                let amortisationSchedule =
                    scheduledPayments
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                // calculate revised schedule including new payment plan
                let amount = 20_00L<Cent>
                let paymentPlanStartDate = Date(2022, 9, 1)
                let unitPeriodConfig = UnitPeriod.Config.Weekly(2, paymentPlanStartDate)
                let outstandingBalance = amortisationSchedule |> Array.last |> fun asi -> asi.PrincipalBalance + asi.FeesBalance + asi.InterestBalance + asi.ChargesBalance
                let extraScheduledPayments = Rescheduling.createPaymentPlan amount unitPeriodConfig sp.Interest.Rate outstandingBalance sp.StartDate
                let payments = Array.concat [| actualPayments; extraScheduledPayments |]
                let amortisationSchedule' =
                    scheduledPayments
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> payments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                amortisationSchedule' |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra003.md" (ValueSome 300)
                return amortisationSchedule'
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2027, 7, 29)
            OffsetDay = 1969<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 949L<Cent>
            ActualPayments = [||]
            NetEffect = 949L<Cent>
            PaymentStatus = ValueSome ActualPayment.NotYetDue
            BalanceStatus = ActualPayment.Settled
            CumulativeInterest = 61609L<Cent>
            NewInterest = 3L<Cent>
            NewCharges = [||]
            PrincipalPortion = 425L<Cent>
            FeesPortion = 521L<Cent>
            InterestPortion = 3L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        } : ActualPayment.AmortisationScheduleItem)
        actual |> should equal expected

    [<Fact>]
    let ``4) never settles down`` () =
        let sp = ({
            AsOfDate = Date(2023, 12, 1)
            StartDate = Date(2023, 11, 6)
            Principal = 80000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly (8, Date(2023, 11, 23))
            PaymentCount = 19
            FeesAndCharges = {
                Fees = ValueSome (Fees.Percentage (Percent 164m, ValueNone))
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.12m)
                Cap = { Total = ValueSome <| Interest.TotalFixedCap 50000L<Cent>; Daily = ValueNone }
                GracePeriod = 7<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        } : ScheduledPayment.ScheduleParameters)
        let actual =
            voption {
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> ActualPayment.allPaidOnTime
                let amortisationSchedule =
                    scheduleItems
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra004.md" (ValueSome 300)
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2026, 8, 27)
            OffsetDay = 1025<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 13736L<Cent>
            ActualPayments = [| 13736L<Cent> |]
            NetEffect = 13736L<Cent>
            PaymentStatus = ValueSome ActualPayment.NotYetDue
            BalanceStatus = ActualPayment.Settled
            CumulativeInterest = 50000L<Cent>
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = 5214L<Cent>
            FeesPortion = 8522L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        } : ActualPayment.AmortisationScheduleItem)
        actual |> should equal expected

    [<Fact>]
    let ``5) large negative payment`` () =
        let sp = ({
            AsOfDate = Date(2023, 12, 11)
            StartDate = Date(2022, 9, 11)
            Principal = 20000L<Cent>
            UnitPeriodConfig = UnitPeriod.Monthly (1, 2022, 9, 15)
            PaymentCount = 7
            FeesAndCharges = {
                Fees = ValueNone
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment 1000L<Cent> |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.8m)
                Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        } : ScheduledPayment.ScheduleParameters)
        let actual =
            voption {
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> ActualPayment.allPaidOnTime
                let amortisationSchedule =
                    scheduleItems
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra005.md" (ValueSome 300)
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 185<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 5153L<Cent>
            ActualPayments = [| 5153L<Cent> |]
            NetEffect = 5153L<Cent>
            PaymentStatus = ValueSome ActualPayment.PaymentMade
            BalanceStatus = ActualPayment.Settled
            CumulativeInterest = 16119L<Cent>
            NewInterest = 943L<Cent>
            NewCharges = [||]
            PrincipalPortion = 4210L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 943L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        } : ActualPayment.AmortisationScheduleItem)
        actual |> should equal expected

    [<Fact>]
    let ``6) Schedule with a payment on day zero, seen from a date after the first unpaid scheduled payment, but within late-payment grace period`` () =
        let sp = ({
            AsOfDate = Date(2022, 4, 1)
            StartDate = Date(2022, 3, 8)
            Principal = 80000L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
            PaymentCount = 12
            FeesAndCharges = {
                Fees = ValueSome(Fees.Percentage (Percent 150m, ValueNone))
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds 750L<Cent>; Charge.LatePayment 1000L<Cent> |]
                LatePaymentGracePeriod = 7<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Annual (Percent 9.95m)
                Cap = { Total = ValueNone; Daily = ValueNone }
                GracePeriod = 3<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = ScheduledPayment.AdjustFinalPayment
            }
        } : ScheduledPayment.ScheduleParameters)
        let actual =
            voption {
                let! schedule = ScheduledPayment.calculateSchedule BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = [|
                    ({ PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayments ([| 16660L<Cent> |], [||]) } : ActualPayment.Payment)
                |]
                let amortisationSchedule =
                    scheduleItems
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment })
                    |> ActualPayment.applyPayments schedule.AsOfDay sp.FeesAndCharges.LatePaymentGracePeriod 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone schedule.FinalPaymentDay true
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra006.md" (ValueSome 300)
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2022, 7, 30)
            OffsetDay = 144<OffsetDay>
            Advance = 0L<Cent>
            ScheduledPayment = 14240L<Cent>
            ActualPayments = [||]
            NetEffect = 14240L<Cent>
            PaymentStatus = ValueSome ActualPayment.NotYetDue
            BalanceStatus = ActualPayment.Settled
            CumulativeInterest = 4353L<Cent>
            NewInterest = 128L<Cent>
            NewCharges = [||]
            PrincipalPortion = 13462L<Cent>
            FeesPortion = 650L<Cent>
            InterestPortion = 128L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 19535L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        } : ActualPayment.AmortisationScheduleItem)
        actual |> should equal expected

