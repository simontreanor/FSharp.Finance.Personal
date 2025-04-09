namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PaymentMapTests =

    open ArrayExtension
    open Calculation
    open Currency
    open DateDay
    open Formatting
    open PaymentSchedule
    open Percentages
    open ValueOptionCE

    let exampleParametersUk asOfDate startDate principal unitPeriodConfig paymentCount interestMethod = {
        AsOfDate = asOfDate
        StartDate = startDate
        Principal = principal
        PaymentSchedule = AutoGenerateSchedule {
            UnitPeriodConfig = unitPeriodConfig
            PaymentCount = paymentCount,
            MaxDuration = Duration.Maximum (180<DurationDay>, startDate)
        }
        PaymentOptions = {
            ScheduledPaymentOption = AsScheduled
            CloseBalanceOption = LeaveOpenBalance
        }
        FeeConfig = None
        ChargeConfig = None
        Interest = {
            Method = interestMethod
            StandardRate = Interest.Rate.Daily (Percent 0.8m)
            Cap = {
                Total = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit))
                Daily = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit))
            }
            InitialGracePeriod = 0<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
        }
        Calculation = {
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            InterestRounding = RoundDown
            MinimumPayment = DeferOrWriteOff 50L<Cent>
            PaymentTimeout = 3<DurationDay>
        }
    }

    [<Fact>]
    let PaymentMapTest001 () =
        let title = "PaymentMapTest001"
        let description = "Basic scenario"
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            let schedule = sp |> calculate BelowZero
            let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
            let actualPayments : PaymentMap.Payment array = [|
                { Day = 10<OffsetDay>; Amount = 250_00L<Cent> }
                { Day = 17<OffsetDay>; Amount = 250_00L<Cent> }
            |]
            let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments
            let title = "<h3>1) Basic scenario</h3>"
            let newHtml = pm |> generateHtmlFromArray None
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent sp.InterestConfig.InterestRounding
            )

        let expected = 934_35L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest002 () =
        let title = "PaymentMapTest002"
        let description = "Very early exact repayments"
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            let schedule = sp |> calculate BelowZero
            let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
            let actualPayments : PaymentMap.Payment array = [|
                { Day = 1<OffsetDay>; Amount = 367_73L<Cent> }
                { Day = 2<OffsetDay>; Amount = 367_73L<Cent> }
                { Day = 3<OffsetDay>; Amount = 367_73L<Cent> }
                { Day = 4<OffsetDay>; Amount = 367_73L<Cent> }
                { Day = 5<OffsetDay>; Amount = 367_72L<Cent> }
            |]
            let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments
            let title = "<h3>2) Very early exact repayments</h3>"
            let newHtml = pm |> generateHtmlFromArray None
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent sp.InterestConfig.InterestRounding
            )

        let expected = -1003_16L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest003 () =
        let title = "PaymentMapTest003"
        let description = "Paid off but with erratic payment timings"
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            let schedule = sp |> calculate BelowZero
            let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
            let actualPayments : PaymentMap.Payment array = [|
                { Day = 18<OffsetDay>; Amount = 367_73L<Cent> }
                { Day = 35<OffsetDay>; Amount = 367_73L<Cent> }
                { Day = 168<OffsetDay>; Amount = 1103_18L<Cent> }
            |]
            let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments
            let title = "<h3>3) Paid off but with erratic payment timings</h3>"
            let newHtml = pm |> generateHtmlFromArray None
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent sp.InterestConfig.InterestRounding
            )

        let expected = 591_30L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest004 () =
        let title = "PaymentMapTest004"
        let description = "Erratic payment timings but not paid off"
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            let schedule = sp |> calculate BelowZero
            let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
            let actualPayments : PaymentMap.Payment array = [|
                { Day =  18<OffsetDay>; Amount = 367_73L<Cent> }
                { Day =  35<OffsetDay>; Amount = 367_73L<Cent> }
            |]
            let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments
            let title = "<h3>4) Erratic payment timings but not paid off</h3>"
            let newHtml = pm |> generateHtmlFromArray None
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent sp.InterestConfig.InterestRounding
            )

        let expected = 697_21L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest005 () =
        let title = "PaymentMapTest005"
        let description = "No payments at all"
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            let schedule = sp |> calculate BelowZero
            let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
            let actualPayments = Array.empty<PaymentMap.Payment>
            let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments
            let title = "<h3>5) No payments at all</h3>"
            let newHtml = pm |> generateHtmlFromArray None
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent sp.InterestConfig.InterestRounding
            )

        let expected = 1600_35L<Cent>
        actual |> should equal expected
