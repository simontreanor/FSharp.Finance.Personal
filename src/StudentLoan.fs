namespace FSharp.Finance.Personal

open System
open Calculation

/// functions for modelling income-contingent student loan repayments for UK plans (1, 2, 4, 5)
/// and the US Income-Based Repayment (IBR) scheme
module StudentLoan =

    /// the student loan repayment plan or scheme
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type Plan =
        /// UK Plan 1 — repayment at 9% above threshold; write-off after 25 years
        | UkPlan1
        /// UK Plan 2 — repayment at 9% above threshold; write-off after 30 years; RPI + up to 3% interest
        | UkPlan2
        /// UK Plan 4 (Scotland) — repayment at 9% above threshold; write-off after 30 years
        | UkPlan4
        /// UK Plan 5 — repayment at 9% above threshold; write-off after 40 years; interest capped at RPI
        | UkPlan5
        /// US Income-Based Repayment (IBR) — repayment at 10% of discretionary income; forgiveness after 20 or 25 years
        | UsIbr

        /// HTML formatting to display the plan in a readable format
        member p.Html =
            match p with
            | UkPlan1 -> "UK Plan 1"
            | UkPlan2 -> "UK Plan 2"
            | UkPlan4 -> "UK Plan 4"
            | UkPlan5 -> "UK Plan 5"
            | UsIbr -> "US IBR"

    /// the repayment rate expressed as a percentage of income above the relevant threshold
    [<Struct>]
    type RepaymentRate = {
        /// the percentage of income above the threshold that is repaid each year
        Rate: Percent
    }

    /// the mechanism by which the repayment threshold is uprated each year
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type ThresholdUprating =
        /// no annual uprating — threshold stays fixed
        | Fixed
        /// uprated by the Retail Price Index (RPI)
        | Rpi of RpiRate: Percent
        /// uprated by a fixed government policy percentage
        | GovernmentPolicy of PolicyRate: Percent

        /// HTML formatting to display the threshold uprating in a readable format
        member tu.Html =
            match tu with
            | Fixed -> "fixed"
            | Rpi(Percent r) -> $"RPI ({r} %%)"
            | GovernmentPolicy(Percent r) -> $"government policy ({r} %%)"

    /// the method used to calculate annual interest on the outstanding balance
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type InterestMethod =
        /// no interest charged on the outstanding balance
        | NoInterest
        /// interest at a fixed annual rate
        | Fixed of AnnualRate: Percent
        /// interest at RPI only (e.g. Plan 5 / Plan 1)
        | RpiOnly of RpiRate: Percent
        /// interest at RPI plus an additional margin of up to a given cap,
        /// scaled linearly between the lower and upper income thresholds (Plan 2)
        | RpiPlusScaledMargin of RpiRate: Percent * MaxMargin: Percent * LowerIncome: int64<Cent> * UpperIncome: int64<Cent>

        /// HTML formatting to display the interest method in a readable format
        member im.Html =
            match im with
            | NoInterest -> "none"
            | Fixed(Percent r) -> $"fixed {r} %%"
            | RpiOnly(Percent r) -> $"RPI ({r} %%)"
            | RpiPlusScaledMargin(Percent rpi, Percent maxMargin, lower, upper) ->
                let lowerDecimal = Cent.toDecimal lower
                let upperDecimal = Cent.toDecimal upper
                $"RPI ({rpi} %%) + up to {maxMargin} %% scaled between £{lowerDecimal:N0} and £{upperDecimal:N0}"

    /// the conditions under which the remaining balance is written off
    [<Struct>]
    type WriteOffCondition = {
        /// number of years after which any remaining balance is written off
        AfterYears: int
    }

    /// the result of a write-off estimation
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type WriteOffEstimate =
        /// the loan is projected to be fully repaid before the write-off date
        | FullyRepaid of YearsToRepay: int
        /// the loan is projected to be partially written off after the write-off term
        | PartialWriteOff of RemainingBalance: int64<Cent>

        /// HTML formatting to display the write-off estimate in a readable format
        member w.Html =
            match w with
            | FullyRepaid years -> $"fully repaid after {years} year(s)"
            | PartialWriteOff(balance) ->
                let balanceDecimal = Cent.toDecimal balance
                $"partial write-off of £{balanceDecimal:N2} remaining"

    /// a single year in a loan projection
    [<Struct>]
    type ProjectionYear = {
        /// the year number (1 = first year of repayment)
        Year: int
        /// gross annual income in cents for this year
        AnnualIncome: int64<Cent>
        /// the repayment threshold in cents for this year
        Threshold: int64<Cent>
        /// the annual repayment amount in cents
        AnnualRepayment: int64<Cent>
        /// the interest charged on the balance during this year in cents
        InterestCharged: int64<Cent>
        /// the outstanding balance at the end of this year in cents
        Balance: int64<Cent>
    }

    /// default plan parameters: repayment rate, write-off condition, and interest method
    let planDefaults plan rpiRate =
        match plan with
        | Plan.UkPlan1 ->
            { Rate = Percent 9m },
            { AfterYears = 25 },
            InterestMethod.RpiOnly(rpiRate)
        | Plan.UkPlan2 ->
            { Rate = Percent 9m },
            { AfterYears = 30 },
            // Plan 2 interest scales from RPI to RPI+3% between the repayment threshold (~£27,295) and the
            // upper income threshold (~£49,130). These values are set by UK government policy and are typically
            // reviewed annually; the figures here reflect approximate 2023/24 levels.
            InterestMethod.RpiPlusScaledMargin(rpiRate, Percent 3m, 2729_500L<Cent>, 4913_000L<Cent>)
        | Plan.UkPlan4 ->
            { Rate = Percent 9m },
            { AfterYears = 30 },
            InterestMethod.RpiOnly(rpiRate)
        | Plan.UkPlan5 ->
            { Rate = Percent 9m },
            { AfterYears = 40 },
            InterestMethod.RpiOnly(rpiRate)
        | Plan.UsIbr ->
            { Rate = Percent 10m },
            { AfterYears = 20 },
            InterestMethod.NoInterest

    /// calculates the annual interest rate applicable on the outstanding balance given the borrower's income
    let annualInterestRate (annualIncome: int64<Cent>) (interestMethod: InterestMethod) : Percent =
        match interestMethod with
        | InterestMethod.NoInterest -> Percent 0m
        | InterestMethod.Fixed(rate) -> rate
        | InterestMethod.RpiOnly(rpiRate) -> rpiRate
        | InterestMethod.RpiPlusScaledMargin(Percent rpi, Percent maxMargin, lowerIncome, upperIncome) ->
            if annualIncome <= lowerIncome then
                Percent rpi
            elif annualIncome >= upperIncome then
                Percent(rpi + maxMargin)
            else
                let incomeRange = decimal (upperIncome - lowerIncome)
                let incomeAboveLower = decimal (annualIncome - lowerIncome)
                let scaledMargin = maxMargin * incomeAboveLower / incomeRange
                Percent(rpi + scaledMargin)

    /// calculates the annual repayment amount given the borrower's income, the repayment threshold, and the repayment rate
    ///
    /// returns 0 if income is at or below the threshold
    let annualRepayment (annualIncome: int64<Cent>) (threshold: int64<Cent>) (repaymentRate: RepaymentRate) : int64<Cent> =
        if annualIncome <= threshold then
            0L<Cent>
        else
            let (Percent rate) = repaymentRate.Rate
            let incomeAboveThreshold = decimal (annualIncome - threshold)
            incomeAboveThreshold * rate / 100m
            |> Cent.round (RoundWith MidpointRounding.AwayFromZero)

    /// calculates the annual interest charged on the outstanding balance
    let annualInterestCharged (balance: int64<Cent>) (interestRate: Percent) : int64<Cent> =
        let (Percent rate) = interestRate
        decimal balance * rate / 100m
        |> Cent.round (RoundWith MidpointRounding.AwayFromZero)

    /// uprates a threshold by the given uprating method
    let uprateThreshold (threshold: int64<Cent>) (uprating: ThresholdUprating) : int64<Cent> =
        match uprating with
        | ThresholdUprating.Fixed -> threshold
        | ThresholdUprating.Rpi(Percent rpiRate) ->
            decimal threshold * (1m + rpiRate / 100m)
            |> Cent.round (RoundWith MidpointRounding.AwayFromZero)
        | ThresholdUprating.GovernmentPolicy(Percent policyRate) ->
            decimal threshold * (1m + policyRate / 100m)
            |> Cent.round (RoundWith MidpointRounding.AwayFromZero)

    /// projects the outstanding balance over time given initial parameters
    ///
    /// returns the list of projection years up to write-off or full repayment (whichever comes first)
    let projectBalance
        (initialBalance: int64<Cent>)
        (initialIncome: int64<Cent>)
        (initialThreshold: int64<Cent>)
        (annualIncomeGrowthRate: Percent)
        (thresholdUprating: ThresholdUprating)
        (repaymentRate: RepaymentRate)
        (interestMethod: InterestMethod)
        (writeOffCondition: WriteOffCondition)
        : ProjectionYear array =

        let (Percent incomeGrowth) = annualIncomeGrowthRate

        let rec loop year income threshold balance acc =
            if year > writeOffCondition.AfterYears || balance <= 0L<Cent> then
                acc |> List.rev |> List.toArray
            else
                let repayment = annualRepayment income threshold repaymentRate |> min balance
                let interestRate = annualInterestRate income interestMethod
                let interest = annualInterestCharged balance interestRate
                let newBalance = balance - repayment + interest |> max 0L<Cent>

                let projection = {
                    Year = year
                    AnnualIncome = income
                    Threshold = threshold
                    AnnualRepayment = repayment
                    InterestCharged = interest
                    Balance = newBalance
                }

                let newIncome = decimal income * (1m + incomeGrowth / 100m) |> Cent.round (RoundWith MidpointRounding.AwayFromZero)
                let newThreshold = uprateThreshold threshold thresholdUprating

                loop (year + 1) newIncome newThreshold newBalance (projection :: acc)

        loop 1 initialIncome initialThreshold initialBalance []

    /// estimates whether the loan is likely to be fully repaid or partially written off,
    /// given initial parameters and assumed annual income growth
    let estimatedWriteOff
        (initialBalance: int64<Cent>)
        (initialIncome: int64<Cent>)
        (initialThreshold: int64<Cent>)
        (annualIncomeGrowthRate: Percent)
        (thresholdUprating: ThresholdUprating)
        (repaymentRate: RepaymentRate)
        (interestMethod: InterestMethod)
        (writeOffCondition: WriteOffCondition)
        : WriteOffEstimate =

        let projection =
            projectBalance
                initialBalance
                initialIncome
                initialThreshold
                annualIncomeGrowthRate
                thresholdUprating
                repaymentRate
                interestMethod
                writeOffCondition

        match projection |> Array.tryFindIndex (fun p -> p.Balance = 0L<Cent>) with
        | Some idx -> WriteOffEstimate.FullyRepaid(projection[idx].Year)
        | None ->
            let finalBalance =
                projection
                |> Array.tryLast
                |> Option.map (fun p -> p.Balance)
                |> Option.defaultValue initialBalance

            WriteOffEstimate.PartialWriteOff(finalBalance)
