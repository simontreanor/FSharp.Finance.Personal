namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module PaymentMapTests =

    open ArrayExtension
    open Calculation
    open Currency
    open CustomerPayments
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
            MaxDuration = ValueSome { Length = 180<DurationDay>; FromDate = startDate }
        }
        PaymentOptions = {
            ScheduledPaymentOption = AsScheduled
            CloseBalanceOption = LeaveOpenBalance
        }
        FeeConfig = Fee.Config.DefaultValue
        ChargeConfig = Charge.Config.DefaultValue
        Interest = {
            Method = interestMethod
            StandardRate = Interest.Rate.Daily (Percent 0.8m)
            Cap = {
                Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
                Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
            }
            InitialGracePeriod = 0<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = ValueNone
        }
        Calculation = {
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            RoundingOptions = RoundingOptions.recommended
            MinimumPayment = DeferOrWriteOff 50L<Cent>
            PaymentTimeout = 3<DurationDay>
        }
    }

    [<Fact>]
    let ``1) Basic scenario`` () =
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            voption {
                let! schedule = sp |> calculate BelowZero
                let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
                let actualPayments : PaymentMap.Payment array = [|
                    { Day = 10<OffsetDay>; Amount = 250_00L<Cent> }
                    { Day = 17<OffsetDay>; Amount = 250_00L<Cent> }
                |]
                let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments

                let title = "<h3>1) Basic scenario</h3>"
                let newHtml = pm |> generateHtmlFromArray None
                $"{title}<br />{newHtml}" |> outputToFile' @"out/PaymentMapTest001.md" false

                return pm
            }

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
            )

        let expected = ValueSome 934_35L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``2) Very early exact repayments`` () =
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            voption {
                let! schedule = sp |> calculate BelowZero
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
                $"{title}<br />{newHtml}" |> outputToFile' @"out/PaymentMapTest002.md" false

                return pm
            }

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
            )

        let expected = ValueSome -1003_16L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``3) Paid off but with erratic payment timings`` () =
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            voption {
                let! schedule = sp |> calculate BelowZero
                let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
                let actualPayments : PaymentMap.Payment array = [|
                    { Day = 18<OffsetDay>; Amount = 367_73L<Cent> }
                    { Day = 35<OffsetDay>; Amount = 367_73L<Cent> }
                    { Day = 168<OffsetDay>; Amount = 1103_18L<Cent> }
                |]
                let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments

                let title = "<h3>3) Paid off but with erratic payment timings</h3>"
                let newHtml = pm |> generateHtmlFromArray None
                $"{title}<br />{newHtml}" |> outputToFile' @"out/PaymentMapTest003.md" false

                return pm
            }

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
            )

        let expected = ValueSome 591_30L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``4) Erratic payment timings but not paid off`` () =
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            voption {
                let! schedule = sp |> calculate BelowZero
                let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
                let actualPayments : PaymentMap.Payment array = [|
                    { Day =  18<OffsetDay>; Amount = 367_73L<Cent> }
                    { Day =  35<OffsetDay>; Amount = 367_73L<Cent> }
                |]
                let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments

                let title = "<h3>4) Erratic payment timings but not paid off</h3>"
                let newHtml = pm |> generateHtmlFromArray None
                $"{title}<br />{newHtml}" |> outputToFile' @"out/PaymentMapTest004.md" false

                return pm
            }

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
            )

        let expected = ValueSome 697_21L<Cent>
        actual |> should equal expected

    [<Fact>]
    let ``5) No payments at all`` () =
        let startDate = Date(2024, 8, 5)
        let asOfDate = startDate.AddDays 180
        let unitPeriodConfig = UnitPeriod.Config.Monthly(1, 2024, 8, 15)
        let sp = exampleParametersUk asOfDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn
        let paymentMap =
            voption {
                let! schedule = sp |> calculate BelowZero
                let scheduledPayments = schedule.Items |> Array.choose(fun i -> if i.Payment.IsSome then Some ({ Day = i.Day; Amount = i.Payment.Value } : PaymentMap.Payment) else None)
                let actualPayments = Array.empty<PaymentMap.Payment>
                let pm = PaymentMap.create asOfDate startDate scheduledPayments actualPayments

                let title = "<h3>5) No payments at all</h3>"
                let newHtml = pm |> generateHtmlFromArray None
                $"{title}<br />{newHtml}" |> outputToFile' @"out/PaymentMapTest005.md" false

                return pm
            }

        let dailyInterestRate = sp.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal
        let actual =
            paymentMap
            |> ValueOption.map
                (Array.sumBy(fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent (ValueSome sp.Calculation.RoundingOptions.InterestRounding)
            )

        let expected = ValueSome 1600_35L<Cent>
        actual |> should equal expected
