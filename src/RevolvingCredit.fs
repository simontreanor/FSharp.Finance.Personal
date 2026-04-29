namespace FSharp.Finance.Personal

open System

/// functions for modelling revolving credit products such as credit cards, overdrafts, and lines of credit
module RevolvingCredit =

    open Calculation
    open DateDay

    /// the type of a revolving credit transaction
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type TransactionType =
        /// a retail or service purchase; attracts no interest during the interest-free period if the full balance is repaid by the statement date
        | Purchase
        /// a cash advance; attracts interest from day one at the cash advance rate with no interest-free period
        | CashAdvance
        /// a repayment credited to the account
        | Repayment
        /// a fee charged to the account (e.g. annual fee, late payment fee)
        | Fee
        /// interest charged to the account at the end of a billing period
        | Interest

        /// HTML formatting to display the transaction type in a readable format
        member tt.Html =
            match tt with
            | Purchase -> "Purchase"
            | CashAdvance -> "Cash Advance"
            | Repayment -> "Repayment"
            | Fee -> "Fee"
            | Interest -> "Interest"

    /// a transaction on a revolving credit account
    [<Struct>]
    type Transaction = {
        /// the date on which the transaction occurred
        TransactionDate: Date
        /// the amount of the transaction: positive for debits (purchases, cash advances, fees, interest); negative for credits (repayments)
        Amount: int64<Cent>
        /// the type of transaction
        TransactionType: TransactionType
    }

    /// the rule used to calculate the minimum payment due on a revolving credit account
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type MinimumPayment =
        /// the greater of a fixed amount or a percentage of the outstanding balance (e.g. greater of £25 or 2% of balance)
        | GreaterOfAmountOrPercent of FixedAmount: int64<Cent> * BalancePercent: Percent
        /// the interest charged in the period plus a percentage of the principal balance (e.g. interest + 1% of principal)
        | InterestPlusPrincipalPercent of PrincipalPercent: Percent
        /// a fixed amount regardless of the balance
        | FixedAmount of int64<Cent>
        /// a fixed percentage of the outstanding balance
        | PercentOfBalance of BalancePercent: Percent

        /// HTML formatting to display the minimum payment rule in a readable format
        member mp.Html =
            match mp with
            | GreaterOfAmountOrPercent(amount, Percent pct) -> $"greater of {Cent.toDecimal amount:N2} or {pct} %%"
            | InterestPlusPrincipalPercent(Percent pct) -> $"interest + {pct} %% of principal"
            | FixedAmount amount -> $"fixed {Cent.toDecimal amount:N2}"
            | PercentOfBalance(Percent pct) -> $"{pct} %% of balance"

    /// a revolving credit account such as a credit card, overdraft, or line of credit
    [<Struct>]
    type CreditAccount = {
        /// the credit limit of the account
        CreditLimit: int64<Cent>
        /// the day of the month on which the monthly statement is issued (1-31)
        StatementDay: int
        /// the annual or daily interest rate applicable to purchase transactions
        PurchaseRate: Interest.Rate
        /// the annual or daily interest rate applicable to cash advance transactions
        CashAdvanceRate: Interest.Rate
        /// the annual or daily interest rate applicable when in default or over-limit
        DefaultRate: Interest.Rate
        /// the rule governing the minimum repayment due each period
        MinimumPaymentRule: MinimumPayment
        /// the number of days for which purchases are interest-free if the full balance is repaid by the statement date (0 = no interest-free period)
        InterestFreeDays: int<DurationDay>
    }

    /// a single line item in a revolving credit statement
    type StatementItem = {
        /// the date of this item
        ItemDate: Date
        /// a description of the item
        Description: string
        /// the monetary amount of the item (positive = debit, negative = credit)
        Amount: int64<Cent>
        /// the type of transaction
        TransactionType: TransactionType
        /// the running account balance after this item is applied
        RunningBalance: int64<Cent>
    }

    /// a revolving credit statement for a given billing period
    type Statement = {
        /// the first date of the billing period
        StatementStart: Date
        /// the last date of the billing period (the statement date)
        StatementEnd: Date
        /// the outstanding balance at the beginning of the billing period
        OpeningBalance: int64<Cent>
        /// all transactions and charges during the billing period, in date order
        Items: StatementItem array
        /// the outstanding balance at the end of the billing period
        ClosingBalance: int64<Cent>
        /// the total value of purchase transactions in the period
        PurchasesTotal: int64<Cent>
        /// the total value of cash advance transactions in the period
        CashAdvancesTotal: int64<Cent>
        /// the total interest charged in the period
        InterestTotal: int64<Cent>
        /// the total fees charged in the period
        FeesTotal: int64<Cent>
        /// the total repayments received in the period
        RepaymentsTotal: int64<Cent>
        /// the minimum repayment due by the next payment date
        MinimumPaymentDue: int64<Cent>
        /// the credit available to use
        AvailableCredit: int64<Cent>
        /// the utilisation ratio expressed as a percentage of the credit limit
        Utilisation: Percent
    }

    /// calculates the outstanding balance at a given date from a transaction history
    let calculateBalance (transactions: Transaction array) (asOfDate: Date) =
        transactions
        |> Array.filter (fun t -> t.TransactionDate <= asOfDate)
        |> Array.sumBy _.Amount

    /// calculates the utilisation ratio (balance as a percentage of the credit limit) at a given date
    let calculateUtilisation (creditLimit: int64<Cent>) (transactions: Transaction array) (asOfDate: Date) =
        if creditLimit = 0L<Cent> then
            Percent 0m
        else
            let balance = calculateBalance transactions asOfDate
            decimal (max 0L<Cent> balance) / decimal creditLimit * 100m |> Percent

    // builds an array of end-of-day balances over the given inclusive date range
    let private buildDailyBalances (openingBalance: int64<Cent>) (startDate: Date) (endDate: Date) (transactionsByDate: Map<Date, int64<Cent>>) =
        let days = (endDate - startDate).Days + 1  // inclusive of both start and end dates
        [| 0 .. days - 1 |]
        |> Array.scan
            (fun balance day ->
                let date = startDate.AddDays(day)
                let delta = transactionsByDate |> Map.tryFind date |> Option.defaultValue 0L<Cent>
                balance + delta
            )
            openingBalance
        |> Array.skip 1  // drop the initial opening balance; result is one end-of-day balance per day

    // calculates total interest accrued on a set of daily balances at the given rate, accumulating as decimal cents before rounding
    let private calculatePeriodInterest (rate: Interest.Rate) (dailyBalances: int64<Cent> array) =
        let dailyRate = rate |> Interest.Rate.daily |> Percent.toDecimal
        dailyBalances
        |> Array.sumBy (fun balance -> decimal (max 0L<Cent> balance) * dailyRate * 1m<Cent>)
        |> Cent.fromDecimalCent (RoundWith MidpointRounding.AwayFromZero)

    // adds a delta for a date to a running map, creating or summing as needed
    let private addDelta (date: Date) (amount: int64<Cent>) (m: Map<Date, int64<Cent>>) =
        m |> Map.change date (fun existing -> Some(Option.defaultValue 0L<Cent> existing + amount))

    /// generates a statement for a revolving credit account covering the given billing period
    let generateStatement (account: CreditAccount) (statementStart: Date) (statementEnd: Date) (transactions: Transaction array) : Statement =

        // split transactions into pre-period and in-period
        let prePeriodTransactions = transactions |> Array.filter (fun t -> t.TransactionDate < statementStart)

        let periodTransactions =
            transactions
            |> Array.filter (fun t -> t.TransactionDate >= statementStart && t.TransactionDate <= statementEnd)
            |> Array.sortBy _.TransactionDate

        // calculate the opening balance (sum of all prior transactions)
        let openingBalance = prePeriodTransactions |> Array.sumBy _.Amount

        // compute opening purchase and cash advance balances from prior transactions;
        // repayments are allocated to the cash advance balance first, then to the purchase balance
        let openingPurchaseBalance, openingCashAdvanceBalance =
            prePeriodTransactions
            |> Array.fold
                (fun (purchaseBal, cashBal) t ->
                    match t.TransactionType with
                    | TransactionType.Purchase -> purchaseBal + t.Amount, cashBal
                    | TransactionType.CashAdvance -> purchaseBal, cashBal + t.Amount
                    | TransactionType.Repayment ->
                        let repay = -t.Amount
                        let cashRepay = min repay (max 0L<Cent> cashBal)
                        let purchaseRepay = min (repay - cashRepay) (max 0L<Cent> purchaseBal)
                        purchaseBal - purchaseRepay, cashBal - cashRepay
                    | _ -> purchaseBal, cashBal
                )
                (0L<Cent>, 0L<Cent>)

        // process in-period transactions to build separate daily delta maps for purchase and cash advance balances;
        // repayments are allocated to the cash advance balance first, then the purchase balance
        let purchaseDeltasByDate, cashAdvanceDeltasByDate, _ =
            periodTransactions
            |> Array.fold
                (fun (purchaseMap, cashMap, (purchaseBal, cashBal)) t ->
                    match t.TransactionType with
                    | TransactionType.Purchase ->
                        addDelta t.TransactionDate t.Amount purchaseMap, cashMap, (purchaseBal + t.Amount, cashBal)
                    | TransactionType.CashAdvance ->
                        purchaseMap, addDelta t.TransactionDate t.Amount cashMap, (purchaseBal, cashBal + t.Amount)
                    | TransactionType.Repayment ->
                        let repay = -t.Amount
                        let cashRepay = min repay (max 0L<Cent> cashBal)
                        let purchaseRepay = min (repay - cashRepay) (max 0L<Cent> purchaseBal)
                        let cashMap' = if cashRepay > 0L<Cent> then addDelta t.TransactionDate (-cashRepay) cashMap else cashMap
                        let purchaseMap' = if purchaseRepay > 0L<Cent> then addDelta t.TransactionDate (-purchaseRepay) purchaseMap else purchaseMap
                        purchaseMap', cashMap', (purchaseBal - purchaseRepay, cashBal - cashRepay)
                    | _ ->
                        purchaseMap, cashMap, (purchaseBal, cashBal)
                )
                (Map.empty, Map.empty, (openingPurchaseBalance, openingCashAdvanceBalance))

        // build per-day balance arrays for interest calculation
        let purchaseDailyBalances = buildDailyBalances openingPurchaseBalance statementStart statementEnd purchaseDeltasByDate
        let cashAdvanceDailyBalances = buildDailyBalances openingCashAdvanceBalance statementStart statementEnd cashAdvanceDeltasByDate

        // aggregate period totals
        let purchasesInPeriod =
            periodTransactions
            |> Array.filter (fun t -> t.TransactionType = TransactionType.Purchase)
            |> Array.sumBy _.Amount

        let cashAdvancesInPeriod =
            periodTransactions
            |> Array.filter (fun t -> t.TransactionType = TransactionType.CashAdvance)
            |> Array.sumBy _.Amount

        let totalRepayments =
            periodTransactions
            |> Array.filter (fun t -> t.TransactionType = TransactionType.Repayment)
            |> Array.sumBy (fun t -> -t.Amount)

        let feesInPeriod =
            periodTransactions
            |> Array.filter (fun t -> t.TransactionType = TransactionType.Fee)
            |> Array.sumBy _.Amount

        // determine interest-free eligibility for purchases:
        // purchases are interest-free when the account has an interest-free period configured AND
        // repayments cover the opening purchase balance plus any new purchases made in the period
        let purchaseBalanceToRepay = max 0L<Cent> (openingPurchaseBalance + purchasesInPeriod)

        let isInterestFreeEligible =
            account.InterestFreeDays > 0<DurationDay> && totalRepayments >= purchaseBalanceToRepay

        // calculate interest for the period
        let purchaseInterest =
            if isInterestFreeEligible then 0L<Cent>
            else calculatePeriodInterest account.PurchaseRate purchaseDailyBalances

        let cashAdvanceInterest = calculatePeriodInterest account.CashAdvanceRate cashAdvanceDailyBalances
        let totalInterest = purchaseInterest + cashAdvanceInterest

        // build statement line items with running balances
        let transactionItems, balanceBeforeInterest =
            periodTransactions
            |> Array.mapFold
                (fun runBal t ->
                    let desc =
                        match t.TransactionType with
                        | TransactionType.Purchase -> "Purchase"
                        | TransactionType.CashAdvance -> "Cash Advance"
                        | TransactionType.Repayment -> "Repayment"
                        | TransactionType.Fee -> "Fee"
                        | TransactionType.Interest -> "Interest"
                    let item = {
                        ItemDate = t.TransactionDate
                        Description = desc
                        Amount = t.Amount
                        TransactionType = t.TransactionType
                        RunningBalance = runBal + t.Amount
                    }
                    item, runBal + t.Amount
                )
                openingBalance

        // append an interest line item if interest was charged in the period
        let interestItem =
            if totalInterest > 0L<Cent> then
                [|
                    {
                        ItemDate = statementEnd
                        Description = "Interest charged"
                        Amount = totalInterest
                        TransactionType = TransactionType.Interest
                        RunningBalance = balanceBeforeInterest + totalInterest
                    }
                |]
            else
                [||]

        let allItems = Array.append transactionItems interestItem

        let closingBalance =
            allItems
            |> Array.tryLast
            |> Option.map _.RunningBalance
            |> Option.defaultValue openingBalance

        // calculate the minimum payment due
        let balanceOwed = max 0L<Cent> closingBalance

        let minimumPayment =
            (match account.MinimumPaymentRule with
             | MinimumPayment.GreaterOfAmountOrPercent(fixedAmount, Percent pct) ->
                 let percentAmount = decimal balanceOwed * pct / 100m |> Cent.round (RoundWith MidpointRounding.AwayFromZero)
                 max fixedAmount percentAmount
             | MinimumPayment.InterestPlusPrincipalPercent(Percent pct) ->
                 let principalPortion = decimal balanceOwed * pct / 100m |> Cent.round (RoundWith MidpointRounding.AwayFromZero)
                 totalInterest + principalPortion
             | MinimumPayment.FixedAmount amount -> amount
             | MinimumPayment.PercentOfBalance(Percent pct) ->
                 decimal balanceOwed * pct / 100m |> Cent.round (RoundWith MidpointRounding.AwayFromZero))
            |> max 0L<Cent>
            |> min balanceOwed  // minimum payment cannot exceed what is owed

        let availableCredit = max 0L<Cent> (account.CreditLimit - closingBalance)

        let utilisation =
            if account.CreditLimit = 0L<Cent> then Percent 0m
            else decimal (max 0L<Cent> closingBalance) / decimal account.CreditLimit * 100m |> Percent

        {
            StatementStart = statementStart
            StatementEnd = statementEnd
            OpeningBalance = openingBalance
            Items = allItems
            ClosingBalance = closingBalance
            PurchasesTotal = purchasesInPeriod
            CashAdvancesTotal = cashAdvancesInPeriod
            InterestTotal = totalInterest
            FeesTotal = feesInPeriod
            RepaymentsTotal = totalRepayments
            MinimumPaymentDue = minimumPayment
            AvailableCredit = availableCredit
            Utilisation = utilisation
        }
