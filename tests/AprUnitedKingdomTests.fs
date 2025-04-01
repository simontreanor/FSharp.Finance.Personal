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

    let getScheduleParameters (startDate: Date) paymentCount firstPaymentDay interestMethod =
        let firstPaymentDate = startDate.AddDays firstPaymentDay
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
                Cap = {
                    TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit, RoundDown))
                    DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit, NoRounding))
                }
                InitialGracePeriod = 3<DurationDay>
                PromotionalRates = [||]
                RateOnNegativeBalance = Interest.Rate.Zero
                InterestRounding = RoundDown
                AprMethod = CalculationMethod.UnitedKingdom 3
            }
        }

    let outputHtmlToFile title description tableRows =
        let htmlTitle = $"<h2>{title}</h2>"
        let htmlTable = $"<table><tr><th>First payment day</th><th>4 payments</th><th>5 payments</th><th>6 payments</th></tr>{tableRows}</table>"
        let htmlDescription = $"<p><h4>Description</h4><i>{description}</i></p>"

        let generalisedParams =
            Parameters.toHtmlTable (getScheduleParameters (Date(2025, 4, 1)) 4 3 Interest.Method.Simple)
            |> fun s -> Regex.Replace(s, "payment count: <i>4</i>", "payment count: <i>{4 to 6}</i>")
            |> fun s -> Regex.Replace(s, "unit-period config: <i>monthly from 2025-04 on 04</i>", "unit-period config: <i>monthly from {2025-04 on 04} to {2025-05 on 02}</i>")

        let htmlParams = $"<h4>Parameters</h4>{generalisedParams}"
        let htmlDatestamp = $"""<p>Generated: <i>{DateTime.Now.ToString "yyyy-MM-dd 'at' HH:mm:ss"}</i></p>"""
        let filename = $"out/{title}.md"
        $"{htmlTitle}{htmlTable}{htmlDescription}{htmlDatestamp}{htmlParams}"
        |> outputToFile' filename false

    [<Fact>] 
    let AprSpreadsheetSimple () =
        let title = "AprSpreadsheetSimple"
        let description = "Range of APRs for different payment counts and first payment days, using the simple interest method"
        let startDate = Date(2025, 4, 1)
        let paymentCounts = [| 4 .. 6 |]
        let firstPaymentDays = [| 3 .. 32 |]

        let tableCells firstPaymentDay =
            paymentCounts
            |> Array.map(fun paymentCount ->
                let schedule = calculate (getScheduleParameters startDate paymentCount firstPaymentDay Interest.Method.Simple) BelowZero
                $"<td>{snd schedule.Apr}</td>"
            )
            |> String.concat ""
            |> fun s -> $"<td>{firstPaymentDay}</td>{s}"

        let tableRows =
            firstPaymentDays
            |> Array.map(fun firstPaymentDay ->
                $"<tr>{tableCells firstPaymentDay}</tr>"
            )
            |> String.concat ""
        
        outputHtmlToFile title description tableRows

    [<Fact>] 
    let AprSpreadsheetAddOn () =
        let title = "AprSpreadsheetAddOn"
        let description = "Range of APRs for different payment counts and first payment days, using the add-on interest method"
        let startDate = Date(2025, 4, 1)

        let paymentCounts = [| 4 .. 6 |]
        let firstPaymentDays = [| 3 .. 32 |]

        let tableCells firstPaymentDay =
            paymentCounts
            |> Array.map(fun paymentCount ->
                let schedule = calculate (getScheduleParameters startDate paymentCount firstPaymentDay Interest.Method.AddOn) BelowZero
                $"<td>{snd schedule.Apr}</td>"
            )
            |> String.concat ""
            |> fun s -> $"<td>{firstPaymentDay}</td>{s}"

        let tableRows =
            firstPaymentDays
            |> Array.map(fun firstPaymentDay ->
                $"<tr>{tableCells firstPaymentDay}</tr>"
            )
            |> String.concat ""
        
        outputHtmlToFile title description tableRows
