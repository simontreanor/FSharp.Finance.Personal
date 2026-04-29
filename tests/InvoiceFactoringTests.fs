namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.Scheduling
open FSharp.Finance.B2B.InvoiceFactoring

module InvoiceFactoringTests =

    [<Fact>]
    let ``FactoringParameters build aggregates invoices due on same day`` () =
        let issueDate = Date(2024, 1, 1)
        let dueDate = Date(2024, 1, 31)

        let invoice1 = Invoice.create "INV-001" 10000_00L<Cent> issueDate dueDate "Customer-A"
        let invoice2 = Invoice.create "INV-002" 15000_00L<Cent> (Date(2024, 1, 5)) dueDate "Customer-B"

        let advance1 = InvoiceAdvance.derive invoice1 0.80m 0.02m
        let advance2 = InvoiceAdvance.derive invoice2 0.80m 0.02m

        let parameters = FactoringParameters.build [| advance1; advance2 |] None

        let scheduledPayments =
            match parameters.Basic.ScheduleConfig with
            | CustomSchedule payments -> payments
            | _ -> failwith "Expected custom schedule"

        Map.count scheduledPayments |> should equal 1

        let totalScheduledAmount =
            scheduledPayments
            |> Map.toSeq
            |> Seq.sumBy (snd >> ScheduledPayment.total)

        totalScheduledAmount |> should equal (invoice1.FaceValue + invoice2.FaceValue)
