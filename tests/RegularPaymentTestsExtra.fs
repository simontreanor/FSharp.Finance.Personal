namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit
open System.Text.Json

open FSharp.Finance.Personal

module RegularPaymentTestsExtra =

    let startDates = [| -90. .. 5. .. 90. |] |> Array.map (DateTime(2023, 12, 1).AddDays)
    let advanceAmounts = [| 10000<Cent> .. 5000<Cent> .. 250000<Cent> |]
    let productFees =
        let none = [| ValueNone |]
        let percentage = [| 1m .. 200m |] |> Array.map(fun m -> ProductFees.Percentage(Percent m, ValueNone) |> ValueSome)
        let percentageCapped = [| 1m .. 200m |] |> Array.map(fun m -> ProductFees.Percentage(Percent m, ValueSome 5000<Cent>) |> ValueSome)
        let simple = [| 1000<Cent> .. 1000<Cent> .. 10000<Cent> |] |> Array.map (ProductFees.Simple >> ValueSome)
        [| none; percentage; percentageCapped; simple |] |> Array.concat
    let productFeesSettlements = [| DueInFull; ProRataRefund |]
    let interestRates =
        let daily = [| 0.02m .. 0.02m .. 0.2m |] |> Array.map (Percent >> DailyInterestRate)
        let annual = [| 1m .. 20m |] |> Array.map (Percent >> AnnualInterestRate)
        [| daily; annual |] |> Array.concat
    let interestCaps =
        let none = [| ValueNone |]
        let fixed' = [| 10000<Cent> .. 10000<Cent> .. 50000<Cent> |] |> Array.map (InterestCap.Fixed >> ValueSome)
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

    type RegularPaymentTestItemDto = {
        FinalPayment: int
        LevelPayment: int
        PrincipalBalance: int
        ProductFeesBalance: int
        InterestBalance: int
        PenaltyChargesBalance: int
        BalanceStatusString: string
        CumulativeInterest: int
        InterestPortionTotal: int
        AdvanceTotal: int
        PrincipalPortionTotal: int
    }

    type RegularPaymentTestItem = {
        FinalPayment: int<Cent>
        LevelPayment: int<Cent>
        PrincipalBalance: int<Cent>
        ProductFeesBalance: int<Cent>
        InterestBalance: int<Cent>
        PenaltyChargesBalance: int<Cent>
        BalanceStatus: IrregularPayment.BalanceStatus
        CumulativeInterest: int<Cent>
        InterestPortionTotal: int<Cent>
        AdvanceTotal: int<Cent>
        PrincipalPortionTotal: int<Cent>
    }


    let appliedPayments =
        startDates |> Seq.collect(fun sd ->
        advanceAmounts |> Seq.collect(fun aa ->
        productFees |> Seq.collect(fun pf ->
        productFeesSettlements |> Seq.collect(fun pfs ->
        interestRates |> Seq.collect(fun ir ->
        interestCaps |> Seq.collect(fun ic ->
        interestGracePeriods |> Seq.collect(fun igp ->
        interestHolidays |> Seq.collect(fun ih ->
        unitPeriodConfigs sd |> Seq.collect(fun upc ->
        paymentCounts |> Seq.map(fun pc ->
            {
                StartDate = sd
                Principal = aa
                ProductFees = pf
                ProductFeesSettlement = pfs
                InterestRate = ir
                InterestCap = ic
                InterestGracePeriod = igp
                InterestHolidays = ih
                UnitPeriodConfig = upc
                PaymentCount = pc
            } : RegularPayment.ScheduleParameters
        ))))))))))
        |> Seq.map(fun sp ->
            let appliedPayments = 
                voption {
                    let! schedule = RegularPayment.calculateSchedule sp
                    let scheduleItems = schedule.Items
                    let actualPayments = scheduleItems |> Array.map(fun si -> { Day = si.Day; ScheduledPayment = 0<Cent>; ActualPayments = [| si.Payment |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] } : IrregularPayment.Payment)
                    return
                        scheduleItems
                        |> IrregularPayment.mergePayments (Day.todayAsOffset sp.StartDate) 1000<Cent> actualPayments
                        |> IrregularPayment.calculateSchedule sp ValueNone
                }
            let sd = sp.StartDate.ToString "yyyy-MM-dd"
            let aa = sp.Principal
            let pf = sp.ProductFees
            let pfs = sp.ProductFeesSettlement
            let ir = InterestRate.serialise sp.InterestRate
            let ic = sp.InterestCap
            let igp = sp.InterestGracePeriod
            let ih = match sp.InterestHolidays with [||] -> "()" | ihh -> ihh |> Array.map(fun ih -> $"""({ih.InterestHolidayStart.ToString "yyyy-MM-dd"}-{ih.InterestHolidayEnd.ToString "yyyy-MM-dd"})""") |> String.concat "+" |> fun s -> $"({s})"
            let upc = UnitPeriod.Config.serialise sp.UnitPeriodConfig
            let pc = sp.PaymentCount
            appliedPayments |> ValueOption.iter(Formatting.outputListToHtml $"IrregularPaymentTestExtra_sd{sd}_aa{aa}_pf{pf}_pfs{pfs}_ir{ir}_ic{ic}_igp{igp}_ih{ih}_upc{upc}_pc{pc}.md" (ValueSome 300))
            appliedPayments
        )

    let writeTestData (apportionmentsSets: IrregularPayment.Apportionment array voption seq) =
        for apportionments in apportionmentsSets do
            voption {
                let! aa = apportionments
                let finalApportionment = Array.last aa
                aa
                |> Array.map(fun a -> {
                    FinalPayment = int finalApportionment.NetEffect
                    LevelPayment = aa |> Array.countBy _.NetEffect |> Array.maxByOrDefault snd fst 0<Cent> |> int
                    PrincipalBalance = int a.PrincipalBalance 
                    ProductFeesBalance = int a.ProductFeesBalance 
                    InterestBalance = int a.InterestBalance 
                    PenaltyChargesBalance = int a.PenaltyChargesBalance 
                    BalanceStatusString = string a.BalanceStatus
                    CumulativeInterest = int a.CumulativeInterest
                    InterestPortionTotal = aa |> Array.sumBy _.InterestPortion |> int
                    AdvanceTotal = aa |> Array.sumBy _.Advance |> int
                    PrincipalPortionTotal = aa |> Array.sumBy _.PrincipalPortion |> int
                })
                |> JsonSerializer.Serialize
                |> fun s -> IO.File.WriteAllText($"{__SOURCE_DIRECTORY__}/../tests/io/RegularPaymentTestData.json", s)
            } |> ignore

    let regularPaymentTestData =
        let fileName = $"{__SOURCE_DIRECTORY__}/../tests/io/RegularPaymentTestData.json"
        if not <| IO.File.Exists fileName then writeTestData (appliedPayments |> Seq.take 1000) else ()
        IO.File.ReadAllText fileName
        |> JsonSerializer.Deserialize<RegularPaymentTestItemDto seq>
        |> Seq.map(fun i -> {
            FinalPayment = i.FinalPayment * 1<Cent>
            LevelPayment = i.LevelPayment * 1<Cent>
            PrincipalBalance = i.PrincipalBalance * 1<Cent>
            ProductFeesBalance = i.ProductFeesBalance * 1<Cent>
            InterestBalance = i.InterestBalance * 1<Cent>
            PenaltyChargesBalance = i.PenaltyChargesBalance * 1<Cent>
            BalanceStatus = IrregularPayment.BalanceStatus.FromString i.BalanceStatusString
            CumulativeInterest = i.CumulativeInterest * 1<Cent>
            InterestPortionTotal = i.InterestPortionTotal * 1<Cent>
            AdvanceTotal = i.AdvanceTotal * 1<Cent>
            PrincipalPortionTotal = i.PrincipalPortionTotal * 1<Cent>
        })
        |> Seq.toArray
        |> toMemberData

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Final payment should be less than level payment`` (testItem: RegularPaymentTestItem) =
        testItem.FinalPayment |> should be (lessThan testItem.LevelPayment)

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Final balances should all be zero`` (testItem: RegularPaymentTestItem) =
        (testItem.PrincipalBalance, testItem.ProductFeesBalance, testItem.InterestBalance, testItem.PenaltyChargesBalance) |> should equal (0<Cent>, 0<Cent>, 0<Cent>, 0<Cent>)

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Final balance status should be settled`` (testItem: RegularPaymentTestItem) =
        (testItem.BalanceStatus) |> should equal IrregularPayment.BalanceStatus.Settled

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``Cumulative interest should be the sum of new interest`` (testItem: RegularPaymentTestItem) =
        testItem.CumulativeInterest |> should equal testItem.InterestPortionTotal

    [<Theory>]
    [<MemberData(nameof(regularPaymentTestData))>]
    let ``The sum of principal portions should equal the sum of advances`` (testItem: RegularPaymentTestItem) =
        testItem.PrincipalPortionTotal |> should equal testItem.AdvanceTotal
