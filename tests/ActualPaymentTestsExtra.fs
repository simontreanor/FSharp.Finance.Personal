namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module ActualPaymentTestsExtra =

    open CustomerPayments
    open PaymentSchedule
    open Amortisation

    let asOfDate = Date(2023, 12, 1)
    let startDates = [| -90 .. 5 .. 90 |] |> Array.map (asOfDate.AddDays)
    let advanceAmounts = [| 100_00L<Cent> .. 50_00L<Cent> .. 2500_00L<Cent> |]
    let fees =
        let none = [||]
        let percentage = [| 1m .. 200m |] |> Array.map(fun m -> Amount.Percentage(Percent m, ValueNone) |> Fee.FacilitationFee)
        let percentageCapped = [| 1m .. 200m |] |> Array.map(fun m -> Amount.Percentage(Percent m, ValueSome (UpperLimit 50_00L<Cent>)) |> Fee.FacilitationFee)
        let simple = [| 10_00L<Cent> .. 10_00L<Cent> .. 100_00L<Cent> |] |> Array.map (Amount.Simple >> Fee.FacilitationFee)
        [| none; percentage; percentageCapped; simple |]
    let feesSettlements = [| Fees.Settlement.DueInFull; Fees.Settlement.ProRataRefund |]
    let interestRates =
        let daily = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (Percent >> Interest.Rate.Daily)
        let annual = [| 1m .. 20m |] |> Array.map (Percent >> Interest.Rate.Annual)
        [| daily; annual |] |> Array.concat
    let totalInterestCaps =
        let none = [| ValueNone |]
        let totalFixed = [| 100_00L<Cent> .. 100_00L<Cent> .. 500_00L<Cent> |] |> Array.map (Interest.TotalFixedCap >> ValueSome)
        let totalPercentageCap = [| 50m .. 50m .. 200m |] |> Array.map (Percent >> Interest.TotalPercentageCap >> ValueSome)
        [| none; totalFixed; totalPercentageCap |] |> Array.concat
    let dailyInterestCaps =
        let none = [| ValueNone |]
        let dailyFixed = [| 1_00L<Cent> .. 1_00L<Cent> .. 10_00L<Cent> |] |> Array.map (Interest.DailyFixedCap >> ValueSome)
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
        let insufficientFundses = [| 0L<Cent>; 7_50L<Cent> |] |> Array.map (Amount.Simple >> Charge.InsufficientFunds)
        let latePayments = [| 0L<Cent>; 10_00L<Cent> |] |> Array.map (Amount.Simple >> Charge.LatePayment)
        [| insufficientFundses; latePayments |]
    let roundingOptions =
        let interestRoundings = [| RoundDown; RoundUp; Round MidpointRounding.ToEven; Round MidpointRounding.AwayFromZero; |]
        let paymentRoundings = [| RoundDown; RoundUp; Round MidpointRounding.ToEven; Round MidpointRounding.AwayFromZero; |]
        interestRoundings
        |> Array.collect(fun ir -> paymentRoundings |> Array.map(fun pr -> { InterestRounding = ir; PaymentRounding = pr }))
    let finalPaymentAdjustments = [| AdjustFinalPayment; SpreadOverLevelPayments |]

    type ScheduledPaymentTestItem = {
        TestId: string
        FinalPayment: int64<Cent>
        LevelPayment: int64<Cent>
        PrincipalBalance: int64<Cent>
        FeesBalance: int64<Cent>
        InterestBalance: int64<Cent>
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
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> Array.filter _.Payment.IsSome |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ActualPayment (si.Payment.Value, [||]) })
                return
                    scheduleItems
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
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
            let advanceTotal = aa |> Array.collect _.Advances |> Array.sum
            let principalPortionTotal = aa |> Array.sumBy _.PrincipalPortion
            if finalPayment > levelPayment
                || (principalBalance, feesBalance, interestBalance, chargesBalance) <> (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>)
                || balanceStatus <> ClosedBalance
                || principalPortionTotal <> advanceTotal
                then
                    aa |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra_{testId}.md"
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
            }
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
                    LevelPayment = aa |> Array.countBy _.NetEffect |> Array.maxByOrDefault snd fst 0L<Cent>
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

    [<Fact>]
    let ``1) Simple schedule looked at from a date in the past showing projected to be fully settled on time`` () =
        let sp = ({
            AsOfDate = Date(2023, 7, 23)
            StartDate = Date(2023, 7, 23)
            Principal = 800_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Monthly(1, 2023, 8, 1)
            PaymentCount = 5
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> allPaidOnTime
                let amortisationSchedule =
                    scheduleItems
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra001.md"
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2023, 12, 1)
            OffsetDay = 131<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 407_64L<Cent>
            PaymentDue = 407_64L<Cent>
            ActualPayments = [| 407_64L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 407_64L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 3_30L<Cent>
            NewCharges = [||]
            PrincipalPortion = 161_76L<Cent>
            FeesPortion = 242_58L<Cent>
            InterestPortion = 3_30L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``2) Schedule with a payment on day 0L<Cent>, seen from a date before scheduled payments are due to start`` () =
        let sp = ({
            AsOfDate = Date(2022, 3, 25)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
            PaymentCount = 12
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = [|
                    ({ PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment (166_60L<Cent>, [||]) })
                |]
                let amortisationSchedule =
                    scheduleItems
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra002.md"
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2022, 8, 27)
            OffsetDay = 172<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 170_90L<Cent>
            PaymentDue = 170_03L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 170_03L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 64L<Cent>
            NewCharges = [||]
            PrincipalPortion = 67_79L<Cent>
            FeesPortion = 101_60L<Cent>
            InterestPortion = 64L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``3) Schedule with a payment on day 0L<Cent>, then all scheduled payments missed, seen from a date after the original settlement date, showing the effect of projected small payments until paid off`` () =
        let sp = ({
            AsOfDate = Date(2022, 8, 31)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
            PaymentCount = 12
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        })
        let actual =
            voption {
                // calculate schedule based on existing payments
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduledPayments =
                    schedule.Items
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                let actualPayments = [|
                    ({ PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment (166_60L<Cent>, [||]) })
                |]
                let amortisationSchedule =
                    scheduledPayments
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                // calculate revised schedule including new payment plan
                let amount = 20_00L<Cent>
                let paymentPlanStartDate = Date(2022, 9, 1)
                let unitPeriodConfig = UnitPeriod.Config.Weekly(2, paymentPlanStartDate)
                let outstandingBalance = amortisationSchedule |> Array.last |> fun asi -> asi.PrincipalBalance + asi.FeesBalance + asi.InterestBalance + asi.ChargesBalance
                let extraScheduledPayments = Rescheduling.createPaymentPlan amount unitPeriodConfig sp.Interest.Rate outstandingBalance sp.StartDate
                let payments = Array.concat [| actualPayments; extraScheduledPayments |]
                let amortisationSchedule' =
                    scheduledPayments
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) payments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                amortisationSchedule' |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra003.md"
                return amortisationSchedule'
            }
            |> ValueOption.bind (Array.lastBut 13)
        let expected = ValueSome ({
            OffsetDate = Date(2027, 7, 29)
            OffsetDay = 1969<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 20_00L<Cent>
            PaymentDue = 9_49L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 9_49L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 3L<Cent>
            NewCharges = [||]
            PrincipalPortion = 4_25L<Cent>
            FeesPortion = 5_21L<Cent>
            InterestPortion = 3L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``4) never settles down`` () =
        let sp = ({
            AsOfDate = Date(2023, 12, 1)
            StartDate = Date(2023, 11, 6)
            Principal = 800_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly (8, Date(2023, 11, 23))
            PaymentCount = 19
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 164m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.DueInFull
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
                LatePaymentGracePeriod = 0<DurationDay>
            }
            Interest = {
                Rate = Interest.Rate.Daily (Percent 0.12m)
                Cap = { Total = ValueSome <| Interest.TotalFixedCap 500_00L<Cent>; Daily = ValueNone }
                GracePeriod = 7<DurationDay>
                Holidays = [||]
                RateOnNegativeBalance = ValueNone
            }
            Calculation = {
                AprMethod = Apr.CalculationMethod.UsActuarial 8
                RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> allPaidOnTime
                let amortisationSchedule =
                    scheduleItems
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra004.md"
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2026, 8, 27)
            OffsetDay = 1025<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 137_36L<Cent>
            PaymentDue = 137_36L<Cent>
            ActualPayments = [| 137_36L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 137_36L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 0L<Cent>
            NewCharges = [||]
            PrincipalPortion = 52_14L<Cent>
            FeesPortion = 85_22L<Cent>
            InterestPortion = 0L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``5) large negative payment`` () =
        let sp = ({
            AsOfDate = Date(2023, 12, 11)
            StartDate = Date(2022, 9, 11)
            Principal = 200_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Monthly (1, 2022, 9, 15)
            PaymentCount = 7
            FeesAndCharges = {
                Fees = [||]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> allPaidOnTime
                let amortisationSchedule =
                    scheduleItems
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra005.md"
                return amortisationSchedule
            }
            |> ValueOption.map Array.last
        let expected = ValueSome ({
            OffsetDate = Date(2023, 3, 15)
            OffsetDay = 185<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 51_53L<Cent>
            PaymentDue = 51_53L<Cent>
            ActualPayments = [| 51_53L<Cent> |]
            GeneratedPayment = ValueNone
            NetEffect = 51_53L<Cent>
            PaymentStatus = PaymentMade
            BalanceStatus = ClosedBalance
            NewInterest = 9_43L<Cent>
            NewCharges = [||]
            PrincipalPortion = 42_10L<Cent>
            FeesPortion = 0L<Cent>
            InterestPortion = 9_43L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

    [<Fact>]
    let ``6) Schedule with a payment on day 0L<Cent>, seen from a date after the first unpaid scheduled payment, but within late-payment grace period`` () =
        let sp = ({
            AsOfDate = Date(2022, 4, 1)
            StartDate = Date(2022, 3, 8)
            Principal = 800_00L<Cent>
            UnitPeriodConfig = UnitPeriod.Weekly(2, Date(2022, 3, 26))
            PaymentCount = 12
            FeesAndCharges = {
                Fees = [| Fee.CabOrCsoFee (Amount.Percentage (Percent 150m, ValueNone)) |]
                FeesSettlement = Fees.Settlement.ProRataRefund
                Charges = [| Charge.InsufficientFunds (Amount.Simple 7_50L<Cent>); Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
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
                FinalPaymentAdjustment = AdjustFinalPayment
            }
        })
        let actual =
            voption {
                let! schedule = PaymentSchedule.calculate BelowZero sp
                let scheduleItems = schedule.Items
                let actualPayments = [|
                    ({ PaymentDay = 0<OffsetDay>; PaymentDetails = ActualPayment (166_60L<Cent>, [||]) })
                |]
                let amortisationSchedule =
                    scheduleItems
                    |> Array.filter _.Payment.IsSome
                    |> Array.map(fun si -> { PaymentDay = si.Day; PaymentDetails = ScheduledPayment si.Payment.Value })
                    |> AppliedPayment.applyPayments schedule.AsOfDay IntendedPurpose.Statement sp.FeesAndCharges.LatePaymentGracePeriod (ValueSome (Amount.Simple 10_00L<Cent>)) actualPayments
                    |> Amortisation.calculate sp IntendedPurpose.Statement schedule.FinalPaymentDay ApplyNegativeInterest
                amortisationSchedule |> Formatting.outputListToHtml $"out/ActualPaymentTestsExtra006.md"
                return amortisationSchedule
            }
            |> ValueOption.bind (Array.lastBut 1)
        let expected = ValueSome ({
            OffsetDate = Date(2022, 8, 13)
            OffsetDay = 158<OffsetDay>
            Advances = [||]
            ScheduledPayment = ValueSome 171_02L<Cent>
            PaymentDue = 167_36L<Cent>
            ActualPayments = [||]
            GeneratedPayment = ValueNone
            NetEffect = 167_36L<Cent>
            PaymentStatus = NotYetDue
            BalanceStatus = ClosedBalance
            NewInterest = 63L<Cent>
            NewCharges = [||]
            PrincipalPortion = 66_73L<Cent>
            FeesPortion = 100_00L<Cent>
            InterestPortion = 63L<Cent>
            ChargesPortion = 0L<Cent>
            FeesRefund = 0L<Cent>
            PrincipalBalance = 0L<Cent>
            FeesBalance = 0L<Cent>
            InterestBalance = 0L<Cent>
            ChargesBalance = 0L<Cent>
        })
        actual |> should equal expected

