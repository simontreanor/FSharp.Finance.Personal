namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module IrregularPaymentTests =

    open RegularPayment
    open IrregularPayment

    [<Fact>]
    let ``Non-payer who commits to a long-term payment plan and completes it`` () =
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
        let regularSchedule = rpsp |> RegularPayment.calculateSchedule
        let actualPayments =
            [| 180 .. 7 .. 2098 |]
            |> Array.map(fun i -> { Day = i * 1<Day>; Adjustments = [| ActualPayment 2500<Cent> |]; PaymentStatus = ValueNone; PenaltyCharges = [||] })
        let scheduledPayments =
            regularSchedule.Items
            |> Array.map(fun si -> { Day = si.Day; Adjustments = (match si.Payment with ValueSome p -> [| ScheduledPayment p |] | _ -> [||]); PaymentStatus = ValueNone; PenaltyCharges = [||] })
        let mergedPayments =
            let today = int (DateTime.Today - startDate).TotalDays * 1<Day>
            mergePayments today 1000<Cent> scheduledPayments actualPayments
        let irregularSchedule =
            {
                RegularPaymentScheduleParameters = rpsp
                RegularPaymentSchedule = regularSchedule
                Payments = mergedPayments
                InterestCap = PercentageOfPrincipal 100m<Percent>
            }
            |> calculateSchedule

        let actual = irregularSchedule |> Array.last

        let expected = {
            Date = DateTime(2026, 8, 9)
            TermDay = 1412<Day>
            Advance = 0<Cent>
            Adjustments = [| ActualPayment 2500<Cent> |] // to-do: limit this to the final payment amount
            Payments = [| 1264<Cent> |]
            PaymentStatus = ValueSome PaidInFull
            CumulativeInterest = 82900<Cent>
            NewInterest = 2<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 439<Cent>
            ProductFeesPortion = 823<Cent>
            InterestPortion = 2<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = 0<Cent>
            ProductFeesBalance = 0<Cent>
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }

        actual |> should equal expected
