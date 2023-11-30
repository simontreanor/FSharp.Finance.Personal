open System
open FSharp.Finance.Personal

open RegularPayment

let startDate = DateTime.Today.AddDays(-60.)

let sp = {
    StartDate = startDate
    Principal = 1200 * 100<Cent>
    ProductFees = ValueSome <| Percentage (Percent 189.47m, ValueNone)
    ProductFeesSettlement = ProRataRefund
    InterestRate = AnnualInterestRate (Percent 9.95m)
    InterestCap = ValueNone
    InterestGracePeriod = 3<Duration>
    InterestHolidays = [||]
    UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
    PaymentCount = 11
}

// calculateSchedule sp
// |> ValueOption.iter(
//     _.Items
//     >> Formatting.outputListToHtml "RegularPayment.md" (ValueSome 180)
// )

open IrregularPayment

// let actualPayments =
//     [| 7 .. 7 .. 14 |]
//     |> Array.map(fun i ->
//         { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     )

let actualPayments =
    [| 18 .. 7 .. 60 |]
    |> Array.map(fun i ->
        { Day = i * 1<Day>; ScheduledPayment = 0<Cent>; ActualPayments = [| 2500<Cent> |]; NetEffect = 0<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
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

// actualPayments
// |> applyPayments sp
// |> ValueOption.iter(
//     Formatting.outputListToHtml "IrregularPayment.md" (ValueSome 300)
// )

Settlement.getSettlement (DateTime.Today) sp actualPayments
|> fun s -> Console.WriteLine $"{s}"

exit 0
