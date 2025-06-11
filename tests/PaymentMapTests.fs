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
    open UnitPeriod
    open ValueOptionCE

    let exampleParametersUk evaluationDate startDate principal unitPeriodConfig paymentCount interestMethod = {
        EvaluationDate = evaluationDate
        StartDate = startDate
        Principal = principal
        PaymentSchedule =
            AutoGenerateSchedule {
                UnitPeriodConfig = unitPeriodConfig
                ScheduleLength = PaymentCount paymentCount
            }
        PaymentOptions = { ScheduledPaymentOption = AsScheduled }
        FeeConfig = None
        ChargeConfig = None
        Interest = {
            Method = interestMethod
            StandardRate = Interest.Rate.Daily(Percent 0.8m)
            Cap = {
                Total = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                Daily = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
            }
            InitialGracePeriod = 0u<OffsetDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
        }
        Calculation = {
            AprMethod = Apr.CalculationMethod.UnitedKingdom
            AprPrecision = 3u
            AprPrecision = Apr.Precision 3
            InterestRounding = RoundDown
            MinimumPayment = DeferOrWriteOff 50L<Cent>
            PaymentTimeout = 3u<OffsetDay>
        }
    }

    [<Fact>]
    let PaymentMapTest001 () =
        let title = "PaymentMapTest001"
        let description = "Basic scenario"
        let startDate = Date(2024, 8, 5)
        let evaluationDate = startDate.AddDays 180
        let unitPeriodConfig = Monthly(1, 2024, 8, 15)

        let p =
            exampleParametersUk evaluationDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn

        let paymentMap =
            let schedule = p |> calculate BelowZero

            let scheduledPayments =
                schedule.Items
                |> Array.choose (fun i ->
                    if i.Payment.IsSome then
                        Some(
                            {
                                Day = i.Day
                                Amount = i.Payment.Value
                            }
                            : PaymentMap.Payment
                        )
                    else
                        None
                )

            let actualPayments: PaymentMap.Payment array = [|
                {
                    Day = 10u<OffsetDay>
                    Amount = 250_00L<Cent>
                }
                {
                    Day = 17u<OffsetDay>
                    Amount = 250_00L<Cent>
                }
            |]

            let pm = PaymentMap.create evaluationDate startDate scheduledPayments actualPayments
            let title = "<h3>1) Basic scenario</h3>"
            let newHtml = pm |> generateHtmlFromArray
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate =
            p.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let actual =
            paymentMap
            |> ValueOption.map (
                Array.sumBy (fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent p.InterestConfig.InterestRounding
            )

        let expected = 934_35L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest002 () =
        let title = "PaymentMapTest002"
        let description = "Very early exact repayments"
        let startDate = Date(2024, 8, 5)
        let evaluationDate = startDate.AddDays 180
        let unitPeriodConfig = Monthly(1, 2024, 8, 15)

        let p =
            exampleParametersUk evaluationDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn

        let paymentMap =
            let schedule = p |> calculate BelowZero

            let scheduledPayments =
                schedule.Items
                |> Array.choose (fun i ->
                    if i.Payment.IsSome then
                        Some(
                            {
                                Day = i.Day
                                Amount = i.Payment.Value
                            }
                            : PaymentMap.Payment
                        )
                    else
                        None
                )

            let actualPayments: PaymentMap.Payment array = [|
                {
                    Day = 1u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 2u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 3u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 4u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 5u<OffsetDay>
                    Amount = 367_72L<Cent>
                }
            |]

            let pm = PaymentMap.create evaluationDate startDate scheduledPayments actualPayments
            let title = "<h3>2) Very early exact repayments</h3>"
            let newHtml = pm |> generateHtmlFromArray
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate =
            p.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let actual =
            paymentMap
            |> ValueOption.map (
                Array.sumBy (fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent p.InterestConfig.InterestRounding
            )

        let expected = -1003_16L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest003 () =
        let title = "PaymentMapTest003"
        let description = "Paid off but with erratic payment timings"
        let startDate = Date(2024, 8, 5)
        let evaluationDate = startDate.AddDays 180
        let unitPeriodConfig = Monthly(1, 2024, 8, 15)

        let p =
            exampleParametersUk evaluationDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn

        let paymentMap =
            let schedule = p |> calculate BelowZero

            let scheduledPayments =
                schedule.Items
                |> Array.choose (fun i ->
                    if i.Payment.IsSome then
                        Some(
                            {
                                Day = i.Day
                                Amount = i.Payment.Value
                            }
                            : PaymentMap.Payment
                        )
                    else
                        None
                )

            let actualPayments: PaymentMap.Payment array = [|
                {
                    Day = 18u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 35u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 168u<OffsetDay>
                    Amount = 1103_18L<Cent>
                }
            |]

            let pm = PaymentMap.create evaluationDate startDate scheduledPayments actualPayments
            let title = "<h3>3) Paid off but with erratic payment timings</h3>"
            let newHtml = pm |> generateHtmlFromArray
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate =
            p.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let actual =
            paymentMap
            |> ValueOption.map (
                Array.sumBy (fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent p.InterestConfig.InterestRounding
            )

        let expected = 591_30L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest004 () =
        let title = "PaymentMapTest004"
        let description = "Erratic payment timings but not paid off"
        let startDate = Date(2024, 8, 5)
        let evaluationDate = startDate.AddDays 180
        let unitPeriodConfig = Monthly(1, 2024, 8, 15)

        let p =
            exampleParametersUk evaluationDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn

        let paymentMap =
            let schedule = p |> calculate BelowZero

            let scheduledPayments =
                schedule.Items
                |> Array.choose (fun i ->
                    if i.Payment.IsSome then
                        Some(
                            {
                                Day = i.Day
                                Amount = i.Payment.Value
                            }
                            : PaymentMap.Payment
                        )
                    else
                        None
                )

            let actualPayments: PaymentMap.Payment array = [|
                {
                    Day = 18u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
                {
                    Day = 35u<OffsetDay>
                    Amount = 367_73L<Cent>
                }
            |]

            let pm = PaymentMap.create evaluationDate startDate scheduledPayments actualPayments
            let title = "<h3>4) Erratic payment timings but not paid off</h3>"
            let newHtml = pm |> generateHtmlFromArray
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate =
            p.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let actual =
            paymentMap
            |> ValueOption.map (
                Array.sumBy (fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent p.InterestConfig.InterestRounding
            )

        let expected = 697_21L<Cent>
        actual |> should equal expected

    [<Fact>]
    let PaymentMapTest005 () =
        let title = "PaymentMapTest005"
        let description = "No payments at all"
        let startDate = Date(2024, 8, 5)
        let evaluationDate = startDate.AddDays 180
        let unitPeriodConfig = Monthly(1, 2024, 8, 15)

        let p =
            exampleParametersUk evaluationDate startDate 1000_00L<Cent> unitPeriodConfig 5 Interest.Method.AddOn

        let paymentMap =
            let schedule = p |> calculate BelowZero

            let scheduledPayments =
                schedule.Items
                |> Array.choose (fun i ->
                    if i.Payment.IsSome then
                        Some(
                            {
                                Day = i.Day
                                Amount = i.Payment.Value
                            }
                            : PaymentMap.Payment
                        )
                    else
                        None
                )

            let actualPayments = Array.empty<PaymentMap.Payment>
            let pm = PaymentMap.create evaluationDate startDate scheduledPayments actualPayments
            let title = "<h3>5) No payments at all</h3>"
            let newHtml = pm |> generateHtmlFromArray
            $"{title}<br />{newHtml}" |> outputToFile' @$"out/{title}.md" false
            pm

        let dailyInterestRate =
            p.Interest.StandardRate |> Interest.Rate.daily |> Percent.toDecimal

        let actual =
            paymentMap
            |> ValueOption.map (
                Array.sumBy (fun pm -> decimal pm.Amount * dailyInterestRate * decimal pm.VarianceDays * 1m<Cent>)
                >> Cent.fromDecimalCent p.InterestConfig.InterestRounding
            )

        let expected = 1600_35L<Cent>
        actual |> should equal expected
