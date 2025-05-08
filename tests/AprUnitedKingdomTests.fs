namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open System
open System.Text.RegularExpressions

open FSharp.Finance.Personal

module AprUnitedKingdomTests =

    let folder = "AprUnitedKingdom"

    open Amortisation
    open AppliedPayment
    open Apr
    open Calculation
    open DateDay
    open Formatting
    open Scheduling
    open UnitPeriod

    module Quirky =

        [<Fact>]
        let ``APR calculation 1 payment 0L<Cent>`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 10); Value = 500_00L<Cent> } |]
            |> getAprOr -1m
            |> should (equalWithin 0.001) 0m

        [<Fact>] 
        let ``APR calculation 1 payment`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 10, 15); Value = 510_00L<Cent> } |]
            |> getAprOr 0m
            |> should (equalWithin 0.001) (Percent 324.436m |> Percent.toDecimal)

        [<Fact>] 
        let ``APR calculation 2 payments`` () =
            UnitedKingdom.calculateApr (Date(2012, 10, 10)) 500_00L<Cent> [| { TransferType = Payment; TransferDate = Date(2012, 11, 10); Value = 270_00L<Cent> }; { TransferType = Payment; TransferDate = Date(2012, 12, 10); Value = 270_00L<Cent> } |]
            |> getAprOr 0m
            |> should (equalWithin 0.001) (Percent 84.63m |> Percent.toDecimal)

    let getParameters (startDate: Date) paymentCount firstPaymentDay interestMethod applyInterestCap : Parameters =
        let firstPaymentDate = startDate.AddDays firstPaymentDay
        let interestCap =
            if applyInterestCap then
                {
                    TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
                    DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
                } : Interest.Cap
            else
                Interest.Cap.Zero
        {
            Basic = {
                EvaluationDate = startDate
                StartDate = startDate
                Principal = 317_26L<Cent>
                ScheduleConfig = AutoGenerateSchedule {
                    UnitPeriodConfig = Config.defaultMonthly 1 firstPaymentDate
                    ScheduleLength = PaymentCount paymentCount
                }
                PaymentConfig = {
                    LevelPaymentOption = LowerFinalPayment
                    Rounding = RoundUp
                }
                FeeConfig = ValueNone
                InterestConfig = {
                    Method = interestMethod
                    StandardRate = Interest.Rate.Daily (Percent 0.798m)
                    Cap = interestCap
                    Rounding = RoundDown
                    AprMethod = CalculationMethod.UnitedKingdom 3
                }
            }
            Advanced = {
                PaymentConfig = {
                    ScheduledPaymentOption = AsScheduled
                    Minimum = DeferOrWriteOff 50L<Cent>
                    Timeout = 3<DurationDay>
                }
                FeeConfig = ValueNone
                ChargeConfig = None
                InterestConfig = {
                    InitialGracePeriod = 3<DurationDay>
                    PromotionalRates = [||]
                    RateOnNegativeBalance = Interest.Rate.Zero
                }
                SettlementDay = SettlementDay.NoSettlement
                TrimEnd = false
            }
        }

    let outputHtmlToFile folder startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description =
        let tableCells firstPaymentDay =
            paymentCounts
            |> Array.map(fun paymentCount ->
                let p = getParameters startDate paymentCount firstPaymentDay interestMethod applyInterestCap
                let basicSchedule = calculateBasicSchedule p.Basic

                basicSchedule |> BasicSchedule.outputHtmlToFile folder $"""AprUkTest_fp{firstPaymentDay.ToString "00"}_pc{paymentCount}""" $"UK APR test amortisation schedule, first payment day {firstPaymentDay}, payment count {paymentCount}" p.Basic

                $"""
        <td>{basicSchedule.Stats.InitialApr}</td>"""
            )
            |> String.concat ""
            |> fun s -> $"""
        <td>{firstPaymentDay}</td>{s}"""

        let tableRows =
            firstPaymentDays
            |> Array.map(fun firstPaymentDay ->
                $"""
    <tr>{tableCells firstPaymentDay}
    </tr>"""
            )
            |> String.concat ""

        let htmlTitle = $"<h2>{title}</h2>"
        let htmlTable =
            $"""
<table>
    <tr>
        <th>First payment day</th>
        <th>4 payments</th>
        <th>5 payments</th>
        <th>6 payments</th>
    </tr>{tableRows}
</table>"""
        let htmlDescription = $"""
<h4>Description</h4>
<p><i>{description}</i></p>"""

        let parameters = getParameters (Date(2025, 4, 1)) 4 3 interestMethod applyInterestCap

        let generalisedBasicParams =
            BasicParameters.toHtmlTable parameters.Basic
            |> fun s -> Regex.Replace(s, "payment count: <i>4</i>", "payment count: <i>{4 to 6}</i>")
            |> fun s -> Regex.Replace(s, "unit-period config: <i>monthly from 2025-04 on 04</i>", "unit-period config: <i>monthly from {2025-04 on 04} to {2025-05 on 02}</i>")

        let htmlBasicParams = $"""
<h4>Parameters</h4>{generalisedBasicParams}"""
        let htmlAdvancedParams = $"""
<h4>Parameters</h4>{AdvancedParameters.toHtmlTable parameters.Advanced}"""

        let htmlDatestamp = $"""
<p>Generated: <i>{DateTime.Now.ToString "yyyy-MM-dd"} using library version {libraryVersion}</i></p>"""
        let filename = $"out/{folder}/{title}.md"
        $"{htmlTitle}{htmlTable}{htmlDescription}{htmlDatestamp}{htmlBasicParams}{htmlAdvancedParams}"
        |> outputToFile' filename false

    let startDate = Date(2025, 4, 1)
    let paymentCounts = [| 4 .. 6 |]
    let firstPaymentDays = [| 3 .. 32 |]

    [<Fact>] 
    let AprSpreadsheetActuarial () =
        let title = "AprSpreadsheetActuarial"
        let description = "Range of APRs for different payment counts and first payment days, using the actuarial interest method"
        let interestMethod = Interest.Method.Actuarial
        let applyInterestCap = true
       
        outputHtmlToFile folder startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let AprSpreadsheetAddOn () =
        let title = "AprSpreadsheetAddOn"
        let description = "Range of APRs for different payment counts and first payment days, using the add-on interest method"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = true

        outputHtmlToFile folder startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let AprSpreadsheetActuarialNoInterestCap () =
        let title = "AprSpreadsheetActuarialNoInterestCap"
        let description = "Range of APRs for different payment counts and first payment days, using the actuarial interest method with no interest cap"
        let interestMethod = Interest.Method.Actuarial
        let applyInterestCap = false

        outputHtmlToFile folder startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let AprSpreadsheetAddOnNoInterestCap () =
        let title = "AprSpreadsheetAddOnNoInterestCap"
        let description = "Range of APRs for different payment counts and first payment days, using the add-on interest method with no interest cap"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = false

        outputHtmlToFile folder startDate paymentCounts firstPaymentDays interestMethod applyInterestCap title description

    [<Fact>] 
    let Amortisation_p6_fp23_BeforeAprJump () =
        let title = "Amortisation_p6_fp23_BeforeAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 23, using the add-on interest method (just before APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = true
        let p = getParameters startDate 6 23 interestMethod applyInterestCap
        let schedules = amortise p Map.empty
        Amortisation.Schedule.outputHtmlToFile folder title description p schedules

    [<Fact>] 
    let Amortisation_p6_fp24_AfterAprJump () =
        let title = "Amortisation_p6_fp24_AfterAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 24, using the add-on interest method (just after APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = true
        let p = getParameters startDate 6 24 interestMethod applyInterestCap
        let schedules = amortise p Map.empty
        Amortisation.Schedule.outputHtmlToFile folder title description p schedules

    [<Fact>] 
    let AmortisationNoInterestCap_p6_fp23_BeforeAprJump () =
        let title = "AmortisationNoInterestCap_p6_fp23_BeforeAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 23, using the add-on interest method with no interest cap (just before APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = false
        let p = getParameters startDate 6 23 interestMethod applyInterestCap
        let schedules = amortise p Map.empty
        Amortisation.Schedule.outputHtmlToFile folder title description p schedules

    [<Fact>] 
    let AmortisationNoInterestCap_p6_fp24_AfterAprJump () =
        let title = "AmortisationNoInterestCap_p6_fp24_AfterAprJump"
        let description = "Amortisation schedule, 6 payments, first payment on day 24, using the add-on interest method with no interest cap (just after APR jump)"
        let interestMethod = Interest.Method.AddOn
        let applyInterestCap = false
        let p = getParameters startDate 6 24 interestMethod applyInterestCap
        let schedules = amortise p Map.empty
        Amortisation.Schedule.outputHtmlToFile folder title description p schedules
