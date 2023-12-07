namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit
open System.Text.Json

open FSharp.Finance.Personal

module ActualPaymentTestsExtra =

    let startDates = [| -90. .. 5. .. 90. |] |> Array.map (DateTime(2023, 12, 1).AddDays)
    let advanceAmounts = [| 10000L<Cent> .. 5000L<Cent> .. 250000L<Cent> |]
    let productFees =
        let none = [| ValueNone |]
        let percentage = [| 1m .. 200m |] |> Array.map(fun m -> ProductFees.Percentage(Percent m, ValueNone) |> ValueSome)
        let percentageCapped = [| 1m .. 200m |] |> Array.map(fun m -> ProductFees.Percentage(Percent m, ValueSome 5000L<Cent>) |> ValueSome)
        let simple = [| 1000L<Cent> .. 1000L<Cent> .. 10000L<Cent> |] |> Array.map (ProductFees.Simple >> ValueSome)
        [| none; percentage; percentageCapped; simple |] |> Array.concat
    let productFeesSettlements = [| DueInFull; ProRataRefund |]
    let interestRates =
        let daily = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (Percent >> DailyInterestRate)
        let annual = [| 1m .. 20m |] |> Array.map (Percent >> AnnualInterestRate)
        [| daily; annual |] |> Array.concat
    let interestCaps =
        let none = [| ValueNone |]
        let fixed' = [| 10000L<Cent> .. 10000L<Cent> .. 50000L<Cent> |] |> Array.map (InterestCap.Fixed >> ValueSome)
        let percentageOfPrincipal = [| 50m .. 50m .. 200m |] |> Array.map (Percent >> InterestCap.PercentageOfPrincipal >> ValueSome)
        [| none; fixed'; percentageOfPrincipal |] |> Array.concat
    let interestGracePeriods = [| 0<Duration> .. 1<Duration> .. 7<Duration> |]
    let interestHolidays =
        let none = [||]
        let some = [| { InterestHolidayStart = DateTime(2024, 3, 1); InterestHolidayEnd = DateTime(2024, 12, 31)} |]
        [| none; some |]
    let unitPeriodConfigs (startDate: DateTime) =
        let daily = [| 4. .. 32. |] |> Array.map (startDate.AddDays >> UnitPeriod.Config.Daily)
        let weekly = [| 1; 2; 4; 8 |] |> Array.collect(fun multiple -> [| 4. .. 32. |] |> Array.map(fun d -> UnitPeriod.Config.Weekly(multiple, (startDate.AddDays d))))
        let semiMonthly =
            [| 4. .. 32. |]
            |> Array.collect(fun d -> 
                startDate.AddDays d
                |> fun sd ->
                    if sd.Day < 15 then [| sd.Day + 15 |] elif sd.Day = 15 then [| 30; 31 |] elif sd.Day < 31 then [| sd.Day - 15 |] else [| 15 |]
                    |> Array.map(fun td2 -> 
                        UnitPeriod.Config.SemiMonthly(UnitPeriod.SemiMonthlyConfig(sd.Year, sd.Month, sd.Day * 1<TrackingDay>, td2 * 1<TrackingDay>))
                    )
            )
        let monthly =
            [| 1; 2; 3; 6 |]
            |> Array.collect(fun multiple ->
                [| 4. .. 32. |]
                |> Array.map(fun d ->
                    startDate.AddDays d
                    |> fun sd -> UnitPeriod.Config.Monthly(multiple, UnitPeriod.MonthlyConfig(sd.Year, sd.Month, sd.Day * 1<TrackingDay>))
                )
            )
        [| daily; weekly; semiMonthly; monthly |] |> Array.concat
    let paymentCounts = [| 1 .. 26 |]

    type ScheduledPaymentTestItem = {
        TestId: string
        FinalPayment: int64<Cent>
        LevelPayment: int64<Cent>
        PrincipalBalance: int64<Cent>
        ProductFeesBalance: int64<Cent>
        InterestBalance: int64<Cent>
        PenaltyChargesBalance: int64<Cent>
        BalanceStatus: ActualPayment.BalanceStatus
        CumulativeInterest: int64<Cent>
        InterestPortionTotal: int64<Cent>
        AdvanceTotal: int64<Cent>
        PrincipalPortionTotal: int64<Cent>
    }

    let applyPayments (sp: ScheduledPayment.ScheduleParameters) =
        let sd = sp.StartDate.ToString "yyyy-MM-dd"
        let p = sp.Principal
        let pf = sp.ProductFees
        let pfs = sp.ProductFeesSettlement
        let ir = InterestRate.serialise sp.InterestRate
        let ic = sp.InterestCap
        let igp = sp.InterestGracePeriod
        let ih = match sp.InterestHolidays with [||] -> "()" | ihh -> ihh |> Array.map(fun ih -> $"""({ih.InterestHolidayStart.ToString "yyyy-MM-dd"}-{ih.InterestHolidayEnd.ToString "yyyy-MM-dd"})""") |> String.concat ";" |> fun s -> $"({s})"
        let upc = UnitPeriod.Config.serialise sp.UnitPeriodConfig
        let pc = sp.PaymentCount
        let testId = $"sd{sd}_p{p}_pf{pf}_pfs{pfs}_ir{ir}_ic{ic}_igp{igp}_ih{ih}_upc{upc}_pc{pc}"
        let appliedPayments = 
            voption {
                let! schedule = ScheduledPayment.calculateSchedule sp
                let scheduleItems = schedule.Items
                let actualPayments = scheduleItems |> Array.map(fun si -> { Day = si.Day; ScheduledPayment = 0L<Cent>; ActualPayments = [| si.Payment |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] } : ActualPayment.Payment)
                return
                    scheduleItems
                    |> ActualPayment.mergePayments (Day.todayAsOffset sp.StartDate) 1000L<Cent> actualPayments
                    |> ActualPayment.calculateSchedule sp ValueNone
            }
        appliedPayments |> ValueOption.iter(fun aa -> 
            let a = Array.last aa
            let finalPayment = a.NetEffect
            let levelPayment = aa |> Array.filter(fun a -> a.NetEffect > 0L<Cent>) |> Array.countBy _.NetEffect |> Array.maxByOrDefault snd fst 0L<Cent>
            let principalBalance = a.PrincipalBalance 
            let productFeesBalance = a.ProductFeesBalance 
            let interestBalance = a.InterestBalance 
            let penaltyChargesBalance = a.PenaltyChargesBalance 
            let balanceStatus = a.BalanceStatus
            let cumulativeInterest = a.CumulativeInterest
            let interestPortionTotal = aa |> Array.sumBy _.InterestPortion
            let advanceTotal = aa |> Array.sumBy _.Advance
            let principalPortionTotal = aa |> Array.sumBy _.PrincipalPortion
            if finalPayment > levelPayment
                || (principalBalance, productFeesBalance, interestBalance, penaltyChargesBalance) <> (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>)
                || balanceStatus <> ActualPayment.BalanceStatus.Settled
                || cumulativeInterest <> interestPortionTotal
                || principalPortionTotal <> advanceTotal
                then
                    aa |> Formatting.outputListToHtml $"aptx/ActualPaymentTestsExtra_{testId}.md" (ValueSome 300)
            else
                ()
        )
        testId, appliedPayments

    let generateRandomAppliedPayments size =
        let rnd = Random()
        seq { 1 .. size }
        |> Seq.map(fun _ ->
            let takeRandomFrom (a: _ array) = a |> Array.length |> rnd.Next |> fun r -> Array.item r a
            let sd  = takeRandomFrom startDates
            let aa  = takeRandomFrom advanceAmounts
            let pf  = takeRandomFrom productFees
            let pfs = takeRandomFrom productFeesSettlements
            let ir  = takeRandomFrom interestRates
            let ic  = takeRandomFrom interestCaps
            let igp = takeRandomFrom interestGracePeriods
            let ih  = takeRandomFrom interestHolidays
            let upc = takeRandomFrom <| unitPeriodConfigs sd
            let pc  = takeRandomFrom paymentCounts
            { StartDate = sd; Principal = aa; ProductFees = pf; ProductFeesSettlement = pfs; InterestRate = ir; InterestCap = ic; InterestGracePeriod = igp; InterestHolidays = ih; UnitPeriodConfig = upc; PaymentCount = pc } : ScheduledPayment.ScheduleParameters
            |> applyPayments
        )

    let generateAllAppliedPayments () = // warning: very large seq (~168 trillion items)
        startDates |> Seq.collect(fun sd ->
        advanceAmounts |> Seq.collect(fun aa ->
        productFees |> Seq.collect(fun pf ->
        productFeesSettlements |> Seq.collect(fun pfs ->
        interestRates |> Seq.collect(fun ir ->
        interestCaps |> Seq.collect(fun ic ->
        interestGracePeriods |> Seq.collect(fun igp ->
        interestHolidays |> Seq.collect(fun ih ->
        unitPeriodConfigs sd |> Seq.collect(fun upc ->
        paymentCounts
        |> Seq.map(fun pc ->
            { StartDate = sd; Principal = aa; ProductFees = pf; ProductFeesSettlement = pfs; InterestRate = ir; InterestCap = ic; InterestGracePeriod = igp; InterestHolidays = ih; UnitPeriodConfig = upc; PaymentCount = pc } : ScheduledPayment.ScheduleParameters
        ))))))))))
        |> Seq.map applyPayments

    let generateRegularPaymentTestData (appliedPayments: (string * ActualPayment.Apportionment array voption) seq) =
        appliedPayments
        |> Seq.map(fun (testId, apportionments) ->
            match apportionments with
            | ValueSome aa ->
                let a = aa |> Array.last
                {
                    TestId = testId
                    FinalPayment = a |> _.NetEffect
                    LevelPayment = aa |> Array.countBy _.NetEffect |> Array.maxByOrDefault snd fst 0L<Cent>
                    PrincipalBalance = a.PrincipalBalance
                    ProductFeesBalance = a.ProductFeesBalance
                    InterestBalance = a.InterestBalance
                    PenaltyChargesBalance = a.PenaltyChargesBalance
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

    let randomAppliedPayments = generateRandomAppliedPayments 1000

    let regularPaymentTestData =
        generateRegularPaymentTestData randomAppliedPayments 

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Final payment should be less than level payment`` (testItem: ScheduledPaymentTestItem) =
        testItem.FinalPayment |> should be (lessThanOrEqualTo testItem.LevelPayment)

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Final balances should all be zero`` (testItem: ScheduledPaymentTestItem) =
        (testItem.PrincipalBalance, testItem.ProductFeesBalance, testItem.InterestBalance, testItem.PenaltyChargesBalance) |> should equal (0L<Cent>, 0L<Cent>, 0L<Cent>, 0L<Cent>)

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Final balance status should be settled`` (testItem: ScheduledPaymentTestItem) =
        (testItem.BalanceStatus) |> should equal ActualPayment.BalanceStatus.Settled

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Cumulative interest should be the sum of new interest`` (testItem: ScheduledPaymentTestItem) =
        testItem.CumulativeInterest |> should equal testItem.InterestPortionTotal

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``The sum of principal portions should equal the sum of advances`` (testItem: ScheduledPaymentTestItem) =
        testItem.PrincipalPortionTotal |> should equal testItem.AdvanceTotal

    // [<Fact>]
    // let ``Error #001`` () =
    //     let sp = ({
    //         StartDate = DateTime(2023, 9, 2)
    //         Principal = 10000L<Cent>
    //         ProductFees = ValueNone
    //         ProductFeesSettlement = DueInFull
    //         InterestRate = DailyInterestRate (Percent 0.02m)
    //         InterestCap = ValueNone
    //         InterestGracePeriod = 0<Duration>
    //         InterestHolidays = [||]
    //         UnitPeriodConfig = UnitPeriod.Daily(DateTime(2023, 9, 6))
    //         PaymentCount = 2
    //     } : ScheduledPayment.ScheduleParameters)
    //     voption {
    //         let! schedule = ScheduledPayment.calculateSchedule sp
    //         let scheduleItems = schedule.Items
    //         let actualPayments = scheduleItems |> ActualPayment.allPaidOnTime
    //         let appliedPayments =
    //             scheduleItems
    //             |> ActualPayment.mergePayments (Day.todayAsOffset sp.StartDate) 1000L<Cent> actualPayments
    //             |> ActualPayment.calculateSchedule sp ValueNone
    //         appliedPayments |> Formatting.outputListToHtml $"aptx/ActualPaymentTestsExtraError001.md" (ValueSome 300)
    //     }
