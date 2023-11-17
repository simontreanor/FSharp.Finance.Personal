open System
open FSharp.Finance

open Amortisation

let amortisationScheduleInfo =
    let principal = 1200 * 100<Cent>
    let productFees = Percentage (189.47m<Percent>, ValueNone)
    let annualInterestRate = 9.95m<Percent>
    let startDate = DateTime.Today
    let unitPeriodConfig = UnitPeriod.Weekly(2, DateTime.Today.AddDays(15.))
    let maxLoanLength = 180<Duration>
    let paymentCount = UnitPeriod.maxPaymentCount maxLoanLength startDate unitPeriodConfig
    {
        Principal = principal
        ProductFees = productFees
        AnnualInterestRate = annualInterestRate
        InterestCapitalisation = OnPaymentDates
        StartDate = startDate
        UnitPeriodConfig = unitPeriodConfig
        PaymentCount = paymentCount
        Output = Full
    }
    |> createRegularScheduleInfo

amortisationScheduleInfo.Schedule.Items
|> Formatting.outputListToHtml "Output.md" (ValueSome 180)

exit 0
