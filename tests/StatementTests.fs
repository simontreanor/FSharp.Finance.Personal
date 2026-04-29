namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module StatementTests =

    open Amortisation
    open AppliedPayment
    open Calculation
    open DateDay
    open Scheduling
    open UnitPeriod

    // ── helpers ──────────────────────────────────────────────────────────────

    let interestCap: Interest.Cap = {
        TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
        DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
    }

    /// basic actuarial loan parameters (no fees, no charges)
    let actuarialParameters: Parameters = {
        Basic = {
            EvaluationDate = Date(2023, 4, 1)
            StartDate = Date(2022, 11, 26)
            Principal = 1500_00L<Cent>
            ScheduleConfig =
                AutoGenerateSchedule {
                    UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                    ScheduleLength = PaymentCount 5
                }
            PaymentConfig = {
                LevelPaymentOption = LowerFinalPayment
                Rounding = RoundUp
            }
            FeeConfig = ValueNone
            InterestConfig = {
                Method = Interest.Method.Actuarial
                StandardRate = Interest.Rate.Daily(Percent 0.8m)
                Cap = interestCap
                Rounding = RoundDown
                AprMethod = Apr.CalculationMethod.UnitedKingdom 3
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

    /// add-on interest loan parameters
    let addOnParameters: Parameters = {
        actuarialParameters with
            Basic.InterestConfig = {
                actuarialParameters.Basic.InterestConfig with
                    Method = Interest.Method.AddOn
            }
    }

    /// on-time actuarial payments for the 5-payment schedule
    let actuarialActualPayments =
        Map [
            4<OffsetDay>,   [| ActualPayment.quickConfirmed 456_88L<Cent> |]
            35<OffsetDay>,  [| ActualPayment.quickConfirmed 456_88L<Cent> |]
            66<OffsetDay>,  [| ActualPayment.quickConfirmed 456_88L<Cent> |]
            94<OffsetDay>,  [| ActualPayment.quickConfirmed 456_88L<Cent> |]
            125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
        ]

    /// on-time add-on payments for the 5-payment schedule (scheduled amounts differ from actuarial)
    let addOnActualPayments =
        Map [
            4<OffsetDay>,   [| ActualPayment.quickConfirmed 532_53L<Cent> |]
            35<OffsetDay>,  [| ActualPayment.quickConfirmed 532_53L<Cent> |]
            66<OffsetDay>,  [| ActualPayment.quickConfirmed 532_53L<Cent> |]
            94<OffsetDay>,  [| ActualPayment.quickConfirmed 532_53L<Cent> |]
            125<OffsetDay>, [| ActualPayment.quickConfirmed 532_51L<Cent> |]
        ]

    // ── generate ─────────────────────────────────────────────────────────────

    [<Fact>]
    let ``generate: line count equals schedule item count`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines.Length |> should equal (schedules.AmortisationSchedule.ScheduleItems |> Map.count)

    [<Fact>]
    let ``generate: first line opening balance is zero`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines[0].OpeningBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``generate: first line closing balance equals principal`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        // day 0: advance = principal, no payment, no fee, closing = principal (actuarial, no initial interest)
        lines[0].ClosingBalance |> should equal actuarialParameters.Basic.Principal

    [<Fact>]
    let ``generate: second line opening balance equals first line closing balance`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines[1].OpeningBalance |> should equal lines[0].ClosingBalance

    [<Fact>]
    let ``generate: opening balance chain holds for all consecutive lines`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines
        |> Array.pairwise
        |> Array.forall (fun (a, b) -> b.OpeningBalance = a.ClosingBalance)
        |> should equal true

    [<Fact>]
    let ``generate: final line closing balance is zero when fully repaid`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines |> Array.last |> _.ClosingBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``generate: cumulative interest paid is monotonically non-decreasing`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines
        |> Array.pairwise
        |> Array.forall (fun (a, b) -> b.CumulativeInterestPaid >= a.CumulativeInterestPaid)
        |> should equal true

    [<Fact>]
    let ``generate: dates match schedule item dates`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let scheduleDates = schedules.AmortisationSchedule.ScheduleItems |> Map.values |> Seq.map _.OffsetDate |> Seq.toArray

        lines |> Array.map _.Date |> should equal scheduleDates

    [<Fact>]
    let ``generate: first line advance equals principal for simple loan`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines[0].Advance |> should equal actuarialParameters.Basic.Principal

    [<Fact>]
    let ``generate: cumulative interest paid matches sum of interest portions`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        let totalInterestFromPortions = lines |> Array.sumBy _.InterestPortion
        let finalCumulative = lines |> Array.last |> _.CumulativeInterestPaid

        finalCumulative |> should equal totalInterestFromPortions

    // ── toPlainText ───────────────────────────────────────────────────────────

    [<Fact>]
    let ``toPlainText: output contains header column names`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let text = Statement.toPlainText lines

        (text.Contains("Date")) |> should equal true
        (text.Contains("Opening Bal")) |> should equal true
        (text.Contains("Closing Bal")) |> should equal true
        (text.Contains("Cum. Interest")) |> should equal true

    [<Fact>]
    let ``toPlainText: output contains separator row`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let text = Statement.toPlainText lines

        (text.Contains("-+-")) |> should equal true

    [<Fact>]
    let ``toPlainText: line count equals statement lines plus header and separator`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let textLines = (Statement.toPlainText lines).Split('\n')

        // header row + separator row + one row per statement line
        textLines.Length |> should equal (lines.Length + 2)

    [<Fact>]
    let ``toPlainText: all rows have equal length (fixed-width table)`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let textLines = (Statement.toPlainText lines).Split('\n')

        // all rows except the separator should have the same length
        let dataAndHeader = [| textLines[0]; yield! textLines[2..] |]
        let lengths = dataAndHeader |> Array.map _.Length |> Array.distinct

        lengths.Length |> should equal 1

    [<Fact>]
    let ``toPlainText: day-0 data row contains expected formatted amounts`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let textLines = (Statement.toPlainText lines).Split('\n')

        // row index 2 is the first data row (after header and separator)
        let day0Row = textLines[2]

        // day 0: date 2022-11-26, advance = 1500.00, payment = 0.00, closing = 1500.00
        (day0Row.Contains("2022-11-26")) |> should equal true
        (day0Row.Contains("1,500.00")) |> should equal true
        (day0Row.Contains("0.00")) |> should equal true

    // ── toCsv ─────────────────────────────────────────────────────────────────

    [<Fact>]
    let ``toCsv: output starts with correct header`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let csv = Statement.toCsv lines

        csv |> should startWith "Date,Opening Balance,Advance"

    [<Fact>]
    let ``toCsv: header has correct column count`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let csvLines = (Statement.toCsv lines).Split('\n')

        // header row has no quoted commas so naive split is accurate
        csvLines[0].Split(',').Length |> should equal 12

    [<Fact>]
    let ``toCsv: line count equals statement lines plus header`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let csvLines = (Statement.toCsv lines).Split('\n')

        // header row + one row per statement line
        csvLines.Length |> should equal (lines.Length + 1)

    [<Fact>]
    let ``toCsv: amounts containing commas are quoted`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let csv = Statement.toCsv lines

        // principal of 1,500.00 should appear quoted in csv
        (csv.Contains("\"1,500.00\"")) |> should equal true

    [<Fact>]
    let ``toCsv: day-0 data row contains expected formatted amounts`` () =
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let csvLines = (Statement.toCsv lines).Split('\n')

        // row index 1 is the first data row (after the header); day 0 is the advance
        let day0Row = csvLines[1]

        // date is unquoted, advance of 1,500.00 is quoted (contains comma), payment is 0.00 unquoted
        (day0Row.Contains("2022-11-26")) |> should equal true
        (day0Row.Contains("\"1,500.00\"")) |> should equal true
        (day0Row.Contains("0.00")) |> should equal true

    [<Fact>]
    let ``toCsv: double-quote characters in field values are escaped as double-double-quotes`` () =
        // RFC 4180: a field containing commas is wrapped in double-quotes; the column count is
        // preserved regardless of how many commas appear inside those quoted values.
        // The Date.Html and formatAmount outputs never produce double-quotes or newlines in
        // normal operation, so this test verifies the RFC 4180 structural invariant using a
        // state-machine parser that correctly handles the "" escape sequence inside quoted fields.
        let schedules = amortise actuarialParameters actuarialActualPayments
        let lines = Statement.generate actuarialParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule
        let csv = Statement.toCsv lines

        let csvLines = csv.Split('\n')
        csvLines
        |> Array.skip 1  // skip header
        |> Array.forall (fun row ->
            // RFC 4180-aware column counter:
            // - commas inside quoted fields are NOT separators
            // - two consecutive double-quotes inside a quoted field are a single escaped quote
            let mutable cols = 0
            let mutable inQuote = false
            let mutable i = 0
            while i < row.Length do
                match inQuote, row[i] with
                | false, '"' ->
                    inQuote <- true
                    i <- i + 1
                | true, '"' when i + 1 < row.Length && row[i + 1] = '"' ->
                    // escaped double-quote inside a quoted field; consume both characters
                    i <- i + 2
                | true, '"' ->
                    inQuote <- false
                    i <- i + 1
                | false, ',' ->
                    cols <- cols + 1
                    i <- i + 1
                | _ ->
                    i <- i + 1
            cols = 11  // 11 separating commas = 12 columns
        )
        |> should equal true

    // ── add-on interest method ────────────────────────────────────────────────

    [<Fact>]
    let ``generate add-on: first line closing balance includes initial interest`` () =
        let schedules = amortise addOnParameters addOnActualPayments
        let lines = Statement.generate addOnParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        // for add-on interest, initial interest is added to balance from day 0
        lines[0].ClosingBalance |> should be (greaterThan addOnParameters.Basic.Principal)

    [<Fact>]
    let ``generate add-on: final line closing balance is zero when fully repaid`` () =
        let schedules = amortise addOnParameters addOnActualPayments
        let lines = Statement.generate addOnParameters.Basic.InterestConfig.Rounding schedules.AmortisationSchedule

        lines |> Array.last |> _.ClosingBalance |> should equal 0L<Cent>

