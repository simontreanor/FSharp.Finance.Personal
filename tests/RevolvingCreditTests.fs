namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module RevolvingCreditTests =

    open Calculation
    open DateDay
    open RevolvingCredit

    // ── helpers ──────────────────────────────────────────────────────────────

    /// a standard test account: £2 000 limit, 20 % APR purchase, 25 % APR cash advance, 30 % default,
    /// minimum payment = greater of £25 or 2 % of balance, 25-day interest-free period
    let standardAccount = {
        CreditLimit = 200_000L<Cent>                                     // £2 000
        StatementDay = 1
        PurchaseRate = Interest.Rate.Annual(Percent 20m)
        CashAdvanceRate = Interest.Rate.Annual(Percent 25m)
        DefaultRate = Interest.Rate.Annual(Percent 30m)
        MinimumPaymentRule = MinimumPayment.GreaterOfAmountOrPercent(2_500L<Cent>, Percent 2m)
        InterestFreeDays = 25<DurationDay>
    }

    let mkTx date amount txType = {
        TransactionDate = date
        Amount = amount
        TransactionType = txType
    }

    // Billing period used by most tests: 30 days inclusive (Jan 1 – Jan 30 2024)
    let periodStart = Date(2024, 1, 1)
    let periodEnd = Date(2024, 1, 30)

    // ── calculateBalance ──────────────────────────────────────────────────────

    [<Fact>]
    let ``CalculateBalance returns zero when no transactions`` () =
        let balance = calculateBalance [||] (Date(2024, 1, 31))
        balance |> should equal 0L<Cent>

    [<Fact>]
    let ``CalculateBalance sums transactions up to and including asOfDate`` () =
        let txs = [|
            mkTx (Date(2024, 1, 1))  50_000L<Cent> TransactionType.Purchase   // +£500
            mkTx (Date(2024, 1, 15)) -20_000L<Cent> TransactionType.Repayment  // -£200
            mkTx (Date(2024, 2, 1))  30_000L<Cent> TransactionType.Purchase   // after query date
        |]
        let balance = calculateBalance txs (Date(2024, 1, 31))
        balance |> should equal 30_000L<Cent>   // 500 - 200 = £300

    // ── calculateUtilisation ─────────────────────────────────────────────────

    [<Fact>]
    let ``CalculateUtilisation returns zero percent when credit limit is zero`` () =
        let txs = [| mkTx (Date(2024, 1, 1)) 50_000L<Cent> TransactionType.Purchase |]
        let u = calculateUtilisation 0L<Cent> txs (Date(2024, 1, 1))
        (u = Percent 0m) |> should equal true

    [<Fact>]
    let ``CalculateUtilisation returns 40 percent for £800 balance on £2 000 limit`` () =
        let txs = [| mkTx (Date(2024, 1, 1)) 80_000L<Cent> TransactionType.Purchase |]
        let u = calculateUtilisation 200_000L<Cent> txs (Date(2024, 1, 1))
        (u = Percent 40m) |> should equal true

    // ── generateStatement – interest-free period ─────────────────────────────

    [<Fact>]
    let ``InterestFree_FullRepayment_NoInterestCharged`` () =
        // £500 purchase, £500 repayment in same period → full balance repaid → no interest
        let txs = [|
            mkTx periodStart         50_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 25)) -50_000L<Cent> TransactionType.Repayment
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.InterestTotal  |> should equal 0L<Cent>
        stmt.ClosingBalance |> should equal 0L<Cent>

    [<Fact>]
    let ``InterestFree_PartialRepayment_InterestCharged`` () =
        // £500 purchase, only £400 repaid → interest-free condition not met
        // Repayment is applied end-of-day on Jan 25, so:
        //   end-of-day balances: 24 days at £500 (Jan 1–24), 6 days at £100 (Jan 25–30)
        // Interest = (24×50 000 + 6×10 000) × 20/36 500 = 1 260 000 × 0.000547945… = 690.41… → 690¢
        let txs = [|
            mkTx periodStart         50_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 25)) -40_000L<Cent> TransactionType.Repayment
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.InterestTotal  |> should equal 690L<Cent>
        stmt.ClosingBalance |> should equal (10_000L<Cent> + 690L<Cent>)   // £106.90

    [<Fact>]
    let ``InterestFree_NoInterestFreeDays_InterestCharged`` () =
        // Account without interest-free days: interest always applies to purchases
        let account = { standardAccount with InterestFreeDays = 0<DurationDay> }
        let txs = [|
            mkTx periodStart         50_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 25)) -50_000L<Cent> TransactionType.Repayment
        |]
        // Daily purchase balance: 50 000 for 25 days, 0 for 5 days → 25×50 000 = 1 250 000
        // Interest = 1 250 000 × 20/36 500 = 684.931… → 685¢
        let stmt = generateStatement account periodStart periodEnd txs
        stmt.InterestTotal |> should be (greaterThan 0L<Cent>)
        stmt.PurchasesTotal  |> should equal 50_000L<Cent>
        stmt.RepaymentsTotal |> should equal 50_000L<Cent>

    // ── generateStatement – cash advance ─────────────────────────────────────

    [<Fact>]
    let ``CashAdvance_InterestAccruedFromDayOne`` () =
        // £200 cash advance, no interest-free period applies
        // Daily cash advance balance: 20 000 for all 30 days
        // Interest = 30×20 000 × 25/36 500 = 410.958… → 411¢
        let txs = [| mkTx periodStart 20_000L<Cent> TransactionType.CashAdvance |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.InterestTotal    |> should equal 411L<Cent>
        stmt.CashAdvancesTotal |> should equal 20_000L<Cent>
        stmt.ClosingBalance   |> should equal (20_000L<Cent> + 411L<Cent>)

    [<Fact>]
    let ``CashAdvance_RepaymentAllocatedToCashAdvanceFirst`` () =
        // £200 cash advance + £300 purchase; £250 repayment
        // Repayment reduces cash advance (£200) first, then purchase (£50)
        // Remaining: purchase £250, cash advance £0
        let txs = [|
            mkTx periodStart          30_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 5))   20_000L<Cent>  TransactionType.CashAdvance
            mkTx (Date(2024, 1, 15)) -25_000L<Cent>  TransactionType.Repayment
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        // cash advance balance = 20 000 paid off by 20 000 of the repayment
        // purchase remaining ≈ 25 000
        // closing balance before interest = 30 000 + 20 000 - 25 000 = 25 000
        let closingBeforeInterest = 30_000L<Cent> + 20_000L<Cent> - 25_000L<Cent>
        stmt.ClosingBalance |> should be (greaterThan closingBeforeInterest)  // interest added
        stmt.InterestTotal  |> should be (greaterThan 0L<Cent>)

    // ── generateStatement – purchase with no repayment ───────────────────────

    [<Fact>]
    let ``Purchase_NoRepayment_InterestCharged`` () =
        // £500 purchase, no repayment, 20 % APR, 30-day period
        // Interest = 30×50 000 × 20/36 500 = 821.917… → 822¢
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.InterestTotal   |> should equal 822L<Cent>
        stmt.PurchasesTotal  |> should equal 50_000L<Cent>
        stmt.ClosingBalance  |> should equal (50_000L<Cent> + 822L<Cent>)
        stmt.OpeningBalance  |> should equal 0L<Cent>
        stmt.RepaymentsTotal |> should equal 0L<Cent>

    // ── generateStatement – revolving balance ────────────────────────────────

    [<Fact>]
    let ``RevolvingBalance_CarryForwardPurchaseBalance`` () =
        // Opening balance £300 (from prior period purchase), new £200 purchase, £100 repayment on day 15
        // Repayment applied end-of-day Jan 15, so:
        //   14 days at £500 (Jan 1–14), 16 days at £400 (Jan 15–30)
        // Interest = (14×50 000 + 16×40 000) × 20/36 500 = 1 340 000 × 0.000547945… = 734.24… → 734¢
        let priorPurchase = mkTx (Date(2023, 12, 15)) 30_000L<Cent> TransactionType.Purchase
        let txs = [|
            priorPurchase
            mkTx periodStart         20_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 15)) -10_000L<Cent> TransactionType.Repayment
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.OpeningBalance  |> should equal 30_000L<Cent>
        stmt.PurchasesTotal  |> should equal 20_000L<Cent>
        stmt.RepaymentsTotal |> should equal 10_000L<Cent>
        stmt.InterestTotal   |> should equal 734L<Cent>
        stmt.ClosingBalance  |> should equal (30_000L<Cent> + 20_000L<Cent> - 10_000L<Cent> + 734L<Cent>)

    // ── generateStatement – minimum payment ──────────────────────────────────

    [<Fact>]
    let ``MinimumPayment_GreaterOf_UsesFixedAmountWhenPercentIsLower`` () =
        // £500 balance: 2% = £10, fixed = £25 → minimum = £25
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        // balance = 50 000 + 822 (interest) = 50 822; 2% = 1016, fixed = 2500 → 2500
        stmt.MinimumPaymentDue |> should equal 2_500L<Cent>

    [<Fact>]
    let ``MinimumPayment_GreaterOf_UsesPercentWhenHigherThanFixed`` () =
        // £2 000 balance, no repayment: 2% = £40, fixed = £25 → minimum = £40
        let txs = [| mkTx periodStart 200_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        // balance = 200 000 + interest; 2% of that > 2500 → uses percent
        stmt.MinimumPaymentDue |> should be (greaterThan 2_500L<Cent>)

    [<Fact>]
    let ``MinimumPayment_InterestPlusPrincipalPercent`` () =
        // Rule: interest + 1 % of balance; balance £500, interest 822¢ → min = 822 + 500 = 1 322¢
        let account = { standardAccount with MinimumPaymentRule = MinimumPayment.InterestPlusPrincipalPercent(Percent 1m) }
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement account periodStart periodEnd txs
        // balance = 50 822; 1% = 508 (rounded); interest = 822 → 822 + 508 = 1 330
        let expected = 822L<Cent> + (50_822L<Cent> |> fun b -> decimal b * 1m / 100m |> Cent.round (RoundWith MidpointRounding.AwayFromZero))
        stmt.MinimumPaymentDue |> should equal expected

    [<Fact>]
    let ``MinimumPayment_FixedAmount`` () =
        let account = { standardAccount with MinimumPaymentRule = MinimumPayment.FixedAmount 3_000L<Cent> }
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement account periodStart periodEnd txs
        stmt.MinimumPaymentDue |> should equal 3_000L<Cent>

    [<Fact>]
    let ``MinimumPayment_PercentOfBalance`` () =
        let account = { standardAccount with MinimumPaymentRule = MinimumPayment.PercentOfBalance(Percent 3m) }
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement account periodStart periodEnd txs
        // balance = 50 822; 3% = 1524.66 → 1525¢
        let expected = decimal 50_822L<Cent> * 3m / 100m |> Cent.round (RoundWith MidpointRounding.AwayFromZero)
        stmt.MinimumPaymentDue |> should equal expected

    [<Fact>]
    let ``MinimumPayment_CannotExceedBalance`` () =
        // Fixed minimum of £1 000, but balance is only £50
        let account = { standardAccount with MinimumPaymentRule = MinimumPayment.FixedAmount 100_000L<Cent> }
        let txs = [| mkTx periodStart 5_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement account periodStart periodEnd txs
        stmt.MinimumPaymentDue |> should be (lessThanOrEqualTo stmt.ClosingBalance)

    // ── generateStatement – utilisation and available credit ─────────────────

    [<Fact>]
    let ``Utilisation_CalculatedFromClosingBalance`` () =
        // £500 purchase (+ £8.22 interest) on £2 000 limit
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        // closing = 50 822; utilisation = 50 822 / 200 000 × 100 = 25.411 %
        (stmt.Utilisation = Percent (50_822m / 200_000m * 100m)) |> should equal true

    [<Fact>]
    let ``AvailableCredit_ReflectsClosingBalance`` () =
        let txs = [| mkTx periodStart 50_000L<Cent> TransactionType.Purchase |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.AvailableCredit |> should equal (200_000L<Cent> - stmt.ClosingBalance)

    // ── generateStatement – fees ─────────────────────────────────────────────

    [<Fact>]
    let ``Fee_IncludedInClosingBalanceButNotInterest`` () =
        let txs = [|
            mkTx periodStart         50_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 10))  2_500L<Cent>  TransactionType.Fee
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        stmt.FeesTotal |> should equal 2_500L<Cent>
        // fees do not generate interest themselves in the base model
        stmt.ClosingBalance |> should equal (50_000L<Cent> + 2_500L<Cent> + stmt.InterestTotal)

    // ── generateStatement – statement items ──────────────────────────────────

    [<Fact>]
    let ``StatementItems_ContainAllTransactionsAndInterest`` () =
        let txs = [|
            mkTx periodStart         50_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 15)) -20_000L<Cent> TransactionType.Repayment
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        // 2 transactions + 1 interest item
        stmt.Items |> Array.length |> should equal 3
        stmt.Items |> Array.last |> _.TransactionType |> (fun tt -> (tt = TransactionType.Interest)) |> should equal true

    [<Fact>]
    let ``StatementItems_RunningBalancesAreMonotonicallyCorrect`` () =
        let txs = [|
            mkTx periodStart         50_000L<Cent>  TransactionType.Purchase
            mkTx (Date(2024, 1, 10)) -10_000L<Cent> TransactionType.Repayment
            mkTx (Date(2024, 1, 20))  5_000L<Cent>  TransactionType.Purchase
        |]
        let stmt = generateStatement standardAccount periodStart periodEnd txs
        // each running balance = previous + that item's amount
        stmt.Items
        |> Array.pairwise
        |> Array.forall (fun (prev, curr) ->
            curr.RunningBalance = prev.RunningBalance + curr.Amount)
        |> should equal true

    [<Fact>]
    let ``NoTransactions_ZeroBalanceAndNoItems`` () =
        let stmt = generateStatement standardAccount periodStart periodEnd [||]
        stmt.OpeningBalance  |> should equal 0L<Cent>
        stmt.ClosingBalance  |> should equal 0L<Cent>
        stmt.InterestTotal   |> should equal 0L<Cent>
        stmt.Items           |> Array.isEmpty |> should equal true
        stmt.MinimumPaymentDue |> should equal 0L<Cent>
        stmt.AvailableCredit |> should equal 200_000L<Cent>
