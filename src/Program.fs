open System
open FSharp.Finance.Personal

open RegularPayment

let startDate = DateTime.Today.AddDays(-421.)

let rpsp =
    {
        StartDate = startDate
        Principal = 1200 * 100<Cent>
        ProductFees = Percentage (189.47m<Percent>, ValueNone)
        InterestRate = AnnualInterestRate 9.95m<Percent>
        UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
        PaymentCount = 11
        FinalPaymentTolerance = ValueNone
    }

let regularSchedule =
    rpsp
    |> calculateSchedule

regularSchedule.Items
|> Formatting.outputListToHtml "RegularPayment.md" (ValueSome 180)

open IrregularPayment

let actualPayments =
    [| 180 .. 7 .. 2098 |]
    |> Array.map(fun i ->
        { Day = i * 1<Day>; Adjustments = [| ActualPayment 2500<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
    )

// let actualPayments = [|
//     { Day = 15<Day>; Adjustments = [| ActualPayment 32315<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 18<Day>; Adjustments = [| ActualPayment 10000<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 19<Day>; Adjustments = [| ActualPayment -10000<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
// |]

// let actualPayments = [|
//     { Day = 170<Day>; Adjustments = [| ActualPayment 300<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 177<Day>; Adjustments = [| ActualPayment 300<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 184<Day>; Adjustments = [| ActualPayment 300<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 191<Day>; Adjustments = [| ActualPayment 300<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
// |]

let scheduledPayments =
    regularSchedule.Items
    |> Array.map(fun si ->
        { Day = si.Day; Adjustments = (match si.Payment with ValueSome p -> [| ScheduledPayment p |] | _ -> [||]); PaymentStatus = ValueNone; PenaltyCharges = [||] }
    )

let mergedPayments =
    let today = int (DateTime.Today - rpsp.StartDate).TotalDays * 1<Day>
    mergePayments today 1000<Cent> scheduledPayments actualPayments

let sp = {
    RegularPaymentScheduleParameters = rpsp
    RegularPaymentSchedule = regularSchedule
    Payments = mergedPayments
    InterestCap = PercentageOfPrincipal 100m<Percent>
}

sp
|> calculateSchedule
|> Formatting.outputListToHtml "IrregularPayment.md" (ValueSome 300)

exit 0
