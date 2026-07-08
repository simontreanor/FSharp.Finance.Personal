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
    let ``Invoice advance decomposes face value exactly into net advance, fee and reserve`` () =
        let invoice = Invoice.create "INV-001" 10000_00L<Cent> (Date(2024, 1, 15)) (Date(2024, 2, 14)) "Customer-A"

        let advance = InvoiceAdvance.derive invoice 0.80m 0.02m

        advance.GrossAdvance |> should equal 8000_00L<Cent>
        advance.NetAdvance |> should equal 7800_00L<Cent>
        advance.UpfrontFee |> should equal 200_00L<Cent>
        advance.ReserveAmount |> should equal 2000_00L<Cent>
        advance.NetAdvance + advance.UpfrontFee + advance.ReserveAmount |> should equal invoice.FaceValue

    [<Theory>]
    [<InlineData(9999_99L)>]
    [<InlineData(33_33L)>]
    [<InlineData(1L)>]
    [<InlineData(12345_67L)>]
    let ``Invoice advance decomposition invariant holds to the cent for awkward amounts`` (faceValue: int64) =
        let invoice = Invoice.create "INV-001" (faceValue * 1L<Cent>) (Date(2024, 1, 15)) (Date(2024, 2, 14)) "Customer-A"

        let advance = InvoiceAdvance.derive invoice 0.80m 0.02m

        advance.NetAdvance + advance.UpfrontFee + advance.ReserveAmount |> should equal invoice.FaceValue
        advance.GrossAdvance |> should equal (advance.NetAdvance + advance.UpfrontFee)

    [<Fact>]
    let ``Invoice advance rejects fee rate exceeding advance rate`` () =
        let invoice = Invoice.create "INV-001" 10000_00L<Cent> (Date(2024, 1, 15)) (Date(2024, 2, 14)) "Customer-A"

        (fun () -> InvoiceAdvance.derive invoice 0.02m 0.80m |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Invoice create rejects non-positive face values`` () =
        (fun () -> Invoice.create "INV-001" 0L<Cent> (Date(2024, 1, 1)) (Date(2024, 2, 1)) "Customer-A" |> ignore)
        |> should throw typeof<System.ArgumentException>

        (fun () -> Invoice.create "INV-001" -100_00L<Cent> (Date(2024, 1, 1)) (Date(2024, 2, 1)) "Customer-A" |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Invoice create rejects due date before issue date`` () =
        (fun () -> Invoice.create "INV-001" 100_00L<Cent> (Date(2024, 2, 1)) (Date(2024, 1, 1)) "Customer-A" |> ignore)
        |> should throw typeof<System.ArgumentException>

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

        totalScheduledAmount |> should equal (advance1.GrossAdvance + advance2.GrossAdvance)

    [<Fact>]
    let ``FactoringParameters build produces a schedule that closes with a zero principal balance`` () =
        let invoice1 = Invoice.create "INV-001" 10000_00L<Cent> (Date(2024, 1, 15)) (Date(2024, 2, 14)) "Customer-A"
        let invoice2 = Invoice.create "INV-002" 15000_00L<Cent> (Date(2024, 1, 20)) (Date(2024, 2, 19)) "Customer-B"

        let advance1 = InvoiceAdvance.derive invoice1 0.80m 0.02m
        let advance2 = InvoiceAdvance.derive invoice2 0.80m 0.02m

        let parameters = FactoringParameters.build [| advance1; advance2 |] None
        let schedule = calculateBasicSchedule parameters.Basic

        let closingBalance = schedule.Items |> Array.last |> _.PrincipalBalance
        closingBalance |> should equal 0L<Cent>

        // the opening balance is the principal (net advances) plus the fees, i.e. the total gross advance,
        // and the scheduled payments repay exactly that
        parameters.Basic.Principal |> should equal (advance1.NetAdvance + advance2.NetAdvance)
        schedule.Stats.ScheduledPaymentTotal |> should equal (advance1.GrossAdvance + advance2.GrossAdvance)

    [<Fact>]
    let ``FactoringParameters build closes to zero for awkward face values`` () =
        let invoice = Invoice.create "INV-001" 9999_99L<Cent> (Date(2024, 1, 15)) (Date(2024, 2, 14)) "Customer-A"
        let advance = InvoiceAdvance.derive invoice 0.80m 0.02m

        let parameters = FactoringParameters.build [| advance |] None
        let schedule = calculateBasicSchedule parameters.Basic

        schedule.Items |> Array.last |> _.PrincipalBalance |> should equal 0L<Cent>
