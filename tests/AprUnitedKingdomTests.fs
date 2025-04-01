namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open System
open System.Text.RegularExpressions

open FSharp.Finance.Personal

module AprUnitedKingdomTests =

    open Apr
    open Calculation
    open DateDay
    open Formatting
    open Scheduling

    module Quirky =

        [<Fact>]
        let ``APR calculation 1 payment 0L<Cent>`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 10); Value = 500_00L<Cent> } |]
            |> Util.getAprOr -1m
            |> should (equalWithin 0.001) 0m

        [<Fact>] 
        let ``APR calculation 1 payment`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 15); Value = 510_00L<Cent> } |]
            |> Util.getAprOr 0m
            |> should (equalWithin 0.001) (Percent 324.436m |> Percent.toDecimal)

        [<Fact>] 
        let ``APR calculation 2 payments`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 11, 10); Value = 270_00L<Cent> }; { TransferType = Payment; TransferDate = Date(2012, 12, 10); Value = 270_00L<Cent> } |]
            |> Util.getAprOr 0m
            |> should (equalWithin 0.001) (Percent 84.63m |> Percent.toDecimal)

    let getScheduleParameters (startDate: Date) paymentCount firstPaymentDay interestMethod applyInterestCap =
        let firstPaymentDate = startDate.AddDays firstPaymentDay
        let interestCap =
            if applyInterestCap then
                {
                    TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
                    DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, RoundDown))
                } : Interest.Cap
            else
                Interest.Cap.Zero
        {
            AsOfDate = startDate
            StartDate = startDate
            Principal = 500_00L<Cent>
            ScheduleConfig = AutoGenerateSchedule {
                UnitPeriodConfig = UnitPeriod.Config.defaultMonthly 1 firstPaymentDate
                PaymentCount = paymentCount
                MaxDuration = Duration.Unlimited
            }
            PaymentConfig = {
                ScheduledPaymentOption = AsScheduled
                CloseBalanceOption = LeaveOpenBalance
                PaymentRounding = RoundUp
                MinimumPayment = DeferOrWriteOff 50L<Cent>
                PaymentTimeout = 3<DurationDay>
            }
            FeeConfig = Fee.Config.initialRecommended
            ChargeConfig = {
                ChargeTypes = [||]
                Rounding = RoundDown
                ChargeHolidays = [||]
                ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
                LatePaymentGracePeriod = 0<DurationDay>
            }
            InterestConfig = {
                Method = interestMethod
                StandardRate = Interest.Rate.Daily (Percent 0.8m)
                Cap = interestCap
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = CalculationMethod.UnitedKingdom 3
            }
        }

    let outputHtmlToFile startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description =
        let tableCells firstPaymentDay =
            paymentCounts
            |> Array.map(fun paymentCount ->
                let schedule = calculate (getScheduleParameters startDate paymentCount firstPaymentDay interestMethod applyInterestCap) BelowZero
                $"<td>{snd schedule.Stats.InitialApr}</td>"
            )
            |> String.concat ""
            |> fun s -> $"<td>{firstPaymentDay}</td>{s}"

        let tableRows =
            firstPaymentDays
            |> Array.map(fun firstPaymentDay ->
                $"<tr>{tableCells firstPaymentDay}</tr>"
            )
            |> String.concat ""

        let htmlTitle = $"<h2>{title}</h2>"
        let htmlTable = $"<table><tr><th>First payment day</th><th>4 payments</th><th>5 payments</th><th>6 payments</th></tr>{tableRows}</table>"
        let htmlDescription = $"<p><h4>Description</h4><i>{description}</i></p>"

        let generalisedParams =
            Parameters.toHtmlTable (getScheduleParameters (Date(2025, 4, 1)) 4 3 interestMethod applyInterestCap)
            |> fun s -> Regex.Replace(s, "payment count: <i>4</i>", "payment count: <i>{4 to 6}</i>")
            |> fun s -> Regex.Replace(s, "unit-period config: <i>monthly from 2025-04 on 04</i>", "unit-period config: <i>monthly from {2025-04 on 04} to {2025-05 on 02}</i>")

        let htmlParams = $"<h4>Parameters</h4>{generalisedParams}"
        let htmlDatestamp = $"""<p>Generated: <i>{DateTime.Now.ToString "yyyy-MM-dd 'at' HH:mm:ss"}</i></p>"""
        let filename = $"out/{title}.md"
        $"{htmlTitle}{htmlTable}{htmlDescription}{htmlDatestamp}{htmlParams}"
        |> outputToFile' filename false

    let startDate = Date(2025, 4, 1)
    let paymentCounts = [| 4 .. 6 |]
    let firstPaymentDays = [| 3 .. 32 |]

    [<Fact>] 
    let AprSpreadsheetSimple () =
        let title = "AprSpreadsheetSimple"
        let description = "Range of APRs for different payment counts and first payment days, using the simple interest method"
        let interestMethod = Interest.Method.Simple
        let applyInterestCap = true
       
        outputHtmlToFile startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let AprSpreadsheetAddOn () =
        let title = "AprSpreadsheetAddOn"
        let description = "Range of APRs for different payment counts and first payment days, using the add-on interest method"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = true

        outputHtmlToFile startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let AprSpreadsheetSimpleNoInterestCap () =
        let title = "AprSpreadsheetSimpleNoInterestCap"
        let description = "Range of APRs for different payment counts and first payment days, using the simple interest method with no interest cap"
        let interestMethod = Interest.Method.Simple
        let applyInterestCap = false

        outputHtmlToFile startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let AprSpreadsheetAddOnNoInterestCap () =
        let title = "AprSpreadsheetAddOnNoInterestCap"
        let description = "Range of APRs for different payment counts and first payment days, using the add-on interest method with no interest cap"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = false

        outputHtmlToFile startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let Amortisation_p6_fp23_BeforeAprJump () =
        let title = "Amortisation_p6_fp23_BeforeAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 23, using the add-on interest method (just before APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = true
        let sp = getScheduleParameters startDate 6 23 interestMethod applyInterestCap
        let schedule = Amortisation.generate sp ValueNone false Map.empty
        Amortisation.Schedule.outputHtmlToFile title description sp schedule

    [<Fact>] 
    let Amortisation_p6_fp24_AfterAprJump () =
        let title = "Amortisation_p6_fp24_AfterAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 24, using the add-on interest method (just after APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = true
        let sp = getScheduleParameters startDate 6 24 interestMethod applyInterestCap
        let schedule = Amortisation.generate sp ValueNone false Map.empty
        Amortisation.Schedule.outputHtmlToFile title description sp schedule

    [<Fact>] 
    let AmortisationNoInterestCap_p6_fp23_BeforeAprJump () =
        let title = "AmortisationNoInterestCap_p6_fp23_BeforeAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 23, using the add-on interest method with no interest cap (just before APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = false
        let sp = getScheduleParameters startDate 6 23 interestMethod applyInterestCap
        let schedule = Amortisation.generate sp ValueNone false Map.empty
        Amortisation.Schedule.outputHtmlToFile title description sp schedule

    [<Fact>] 
    let AmortisationNoInterestCap_p6_fp24_AfterAprJump () =
        let title = "AmortisationNoInterestCap_p6_fp24_AfterAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 24, using the add-on interest method with no interest cap (just after APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = false
        let sp = getScheduleParameters startDate 6 24 interestMethod applyInterestCap
        let schedule = Amortisation.generate sp ValueNone false Map.empty
        Amortisation.Schedule.outputHtmlToFile title description sp schedule
