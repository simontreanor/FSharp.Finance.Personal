open System
open FSharp.Finance.Personal

open RegularPayment

let scheduleParameters =
    {
        StartDate = DateTime.Today
        Principal = 1200 * 100<Cent>
        ProductFees = Percentage (189.47m<Percent>, ValueNone)
        InterestRate = AnnualInterestRate 9.95m<Percent>
        UnitPeriodConfig = UnitPeriod.Weekly(2, DateTime.Today.AddDays(15.))
        PaymentCount = 11
        FinalPaymentTolerance = ValueNone
    }

let regularSchedule =
    scheduleParameters
    |> calculateSchedule

regularSchedule.Items
|> Formatting.outputListToHtml "RegularPayment.md" (ValueSome 180)

open IrregularPayment

let actualPayments = [|
    { Day = 18<Day>; Amounts = [| 10000<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
|]

let scheduledPayments =
    regularSchedule.Items
    |> Array.map(fun si ->
        { Day = si.Day; Amounts = [| si.Payment |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
    )

let mergedPayments =
    mergePayments 50<Day> 1000<Cent> scheduledPayments actualPayments

mergedPayments
|> calculateSchedule scheduleParameters
|> Formatting.outputListToHtml "IrregularPayment.md" (ValueSome 180)

exit 0
