open System
open FSharp.Finance.Personal

(*

NEW FEATURES / TO DO:
---------------------

- add penalty charge types to statement ✔️ done
- return version of software used to generate figures
- move types / DUs to appropriate modules
- add [<RQA>] attribute
- payment needs a stub type when e.g. net-effect should not be set by the caller
- wrap applied payments in a type so we can bundle e.g. final APR
- check number of required decimal places in iterations for UK APR
- show iteration stage results for comparison
- add interest up-front option (for comparison)
- add function: if I pay $20 installments, how many payments / how long until settlement?
- add minimum payment option to prevent banking errors / unfavourable minimim transaction charges
- DU for manual intervention e.g. penalty charge refunds? (might be better just using zero fees and recalculating to avoid the effect of slower principal amortisation)
- add Excel calculation sheets?
- documentation

*)

open ScheduledPayment

let startDate = DateTime.Today.AddDays(-60.)

let sp = {
    AsOfDate = startDate
    StartDate = startDate
    Principal = 120000L<Cent>
    ProductFees = ValueSome <| ProductFees.Percentage (Percent 189.47m, ValueNone)
    ProductFeesSettlement = ProRataRefund
    InterestRate = AnnualInterestRate (Percent 9.95m)
    InterestCap = { TotalCap = ValueNone; DailyCap = ValueNone }
    InterestGracePeriod = 3<Days>
    InterestHolidays = [||]
    UnitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
    PaymentCount = 11
}

// calculateSchedule sp
// |> ValueOption.iter(
//     _.Items
//     >> Formatting.outputListToHtml "out/ScheduledPayment.md" (ValueSome 180)
// )

open ActualPayment

// let actualPayments =
//     [| 7 .. 7 .. 14 |]
//     |> Array.map(fun i ->
//         { Day = i * 1<Day>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     )

let actualPayments =
    [| 18 .. 7 .. 60 |]
    |> Array.map(fun i ->
        { Day = i * 1<OffsetDay>; ScheduledPayment = 0L<Cent>; ActualPayments = [| 2500L<Cent> |]; NetEffect = 0L<Cent>; PaymentStatus = ValueNone; PenaltyCharges = [||] }
    )

// let actualPayments = [|
//     { Day = 15<Day>; Adjustments = [| ActualPayment 32315L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 18<Day>; Adjustments = [| ActualPayment 10000L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 19<Day>; Adjustments = [| ActualPayment -10000L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
// |]

// let actualPayments = [|
//     { Day = 170<Day>; Adjustments = [| ActualPayment 300L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 177<Day>; Adjustments = [| ActualPayment 300L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 184<Day>; Adjustments = [| ActualPayment 300L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
//     { Day = 191<Day>; Adjustments = [| ActualPayment 300L<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] }
// |]

// actualPayments
// |> applyPayments sp
// |> ValueOption.iter(
//     Formatting.outputListToHtml "out/ActualPayment.md" (ValueSome 300)
// )

Settlement.getSettlement (DateTime.Today) sp actualPayments
|> fun s -> Console.WriteLine $"{s}"

exit 0
