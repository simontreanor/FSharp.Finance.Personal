namespace FSharp.Finance.Personal

open System

/// calculating the APR according to various country-specific regulations
module Apr =

    open Calculation
    open DateDay

    /// the calculation method used to determine the APR
    [<RequireQualifiedAccess; Struct; StructuredFormatDisplay("{Html}")>]
    type CalculationMethod =
        /// calculates the APR according to EU Directive 2008/48/EC to the stated decimal precision (note that this is two places more than the percent precision)
        | EuropeanUnion of EuPrecision: int
        /// calculates the APR according to UK FCA rules to the stated decimal precision (note that this is two places more than the percent precision)
        | UnitedKingdom of UkPrecision: int
        /// calculates the APR according to the US CFPB actuarial method to the stated decimal precision (note that this is two places more than the percent precision)
        | UsActuarial of UsPrecision: int
        /// calculates the APR according to the United States rule (not yet implemented)
        | UnitedStatesRule

        /// HTML formatting to display the calculation method in a readable format
        member cm.Html =
            match cm with
            | EuropeanUnion precision -> $"EU to {precision - 2} d.p."
            | UnitedKingdom precision -> $"UK FCA to {precision - 2} d.p."
            | UsActuarial precision -> $"US CFPB actuarial to {precision - 2} d.p."
            | UnitedStatesRule -> "United States rule"

    /// basic calculation to determine the APR
    let annualPercentageRate unitPeriodRate unitPeriodsPerYear = unitPeriodRate * unitPeriodsPerYear

    /// whether a transfer is an advance or a payment
    [<Struct>]
    type TransferType =
        /// outgoing transfer
        | Advance
        /// incoming transfer
        | Payment

    /// details of an advance or a payment
    [<Struct>]
    type Transfer = {
        /// advance or payment
        TransferType: TransferType
        /// the date of the transfer
        TransferDate: Date
        /// the amount of the transfer
        Value: int64<Cent>
    }

    /// an approximation of the unit-period equivalent of the APR, expressed as a decimal rate (not a percentage),
    /// used as the initial guess for the solver
    let roughUnitPeriodRate principal interestTotal paymentCount =
        if principal = 0m then
            0m
        else
            2m * interestTotal / (principal * (paymentCount + 1m))


    /// calculates the APR (note that as of 2025-05-08 the EU and UK calculation methods are aligned)
    ///
    /// $$\sum_{k=1}^{m} C_k (1 + X)^{-t_k} = \sum_{l=1}^{m'} D_l (1 + X)^{-s_l}$$
    ///
    /// which is equivalent to
    ///
    /// $$\sum_{k=1}^{m} \frac{C_k}{(1 + X)^{t_k}} = \sum_{l=1}^{m'} \frac{D_l}{(1 + X)^{s_l}}$$
    let internal calculateAprEuUk (startDate: Date) (principal: int64<Cent>) transfers =
        if principal = 0L<Cent> || Array.isEmpty transfers then
            Solution.Impossible
        else
            let payments = transfers |> Array.filter (fun t -> t.TransferType = Payment)
            let paymentTotal = payments |> Array.sumBy _.Value

            if principal = paymentTotal then
                Solution.Found(0m, 0, 0m)
            else
                let paymentCount = payments |> Array.length |> decimal
                let principalM = Cent.toDecimal principal
                let interestTotal = Cent.toDecimal paymentTotal - principalM
                let roughUnitPeriodRate = roughUnitPeriodRate principalM interestTotal paymentCount
                let advanceIntervals = [| principal, 0m |]

                let paymentIntervals =
                    payments
                    |> Array.map (fun t -> t.Value, decimal (t.TransferDate - startDate).Days / 365m)

                let calc transfers unitPeriodRate =
                    transfers
                    |> Array.sumBy (fun (amount, years) ->
                        try // rates near -1 with long-dated payments produce tiny divisors whose quotients overflow decimal, so treat these as zero (as in the US calculation)
                            let divisor = 1m + unitPeriodRate |> powm years

                            if Double.IsNaN divisor || divisor = 0. then
                                0m
                            else
                                amount |> Cent.toDecimal |> (fun a -> double a / divisor |> decimal)
                        with _ ->
                            0m
                    )

                let generator unitPeriodRate =
                    let aa = calc advanceIntervals unitPeriodRate
                    let pp = calc paymentIntervals unitPeriodRate
                    let difference = Decimal.Round(pp - aa, 8)
                    difference

                Array.solveNewtonRaphson generator 100u roughUnitPeriodRate 1e-6m

    /// APR as in EU Directive [2008/48/EC](https://eur-lex.europa.eu/eli/dir/2008/48/2023-12-30)
    module EuropeanUnion =
        /// calculates the APR
        ///
        /// $$\sum_{k=1}^{m} C_k (1 + X)^{-t_k} = \sum_{l=1}^{m'} D_l (1 + X)^{-s_l}$$
        ///
        /// which is equivalent to
        ///
        /// $$\sum_{k=1}^{m} \frac{C_k}{(1 + X)^{t_k}} = \sum_{l=1}^{m'} \frac{D_l}{(1 + X)^{s_l}}$$
        let calculateApr = calculateAprEuUk

    /// APR as in FCA [CONC App 1.2.6](https://www.handbook.fca.org.uk/handbook/CONC/App/1/2.html)
    module UnitedKingdom =
        /// calculates the APR
        ///
        /// $$\sum_{k=1}^{m} C_k (1 + X)^{-t_k} = \sum_{l=1}^{m'} D_l (1 + X)^{-s_l}$$
        ///
        /// which is equivalent to
        ///
        /// $$\sum_{k=1}^{m} \frac{C_k}{(1 + X)^{t_k}} = \sum_{l=1}^{m'} \frac{D_l}{(1 + X)^{s_l}}$$
        let calculateApr = calculateAprEuUk

    /// APR as in https://www.consumerfinance.gov/rules-policy/regulations/1026/j/
    module UsActuarial =

        /// (b)(5)(i) The number of days between 2 dates shall be the number of 24-hour intervals between any point in time on the first
        /// date to the same point in time on the second date.
        let daysBetween (date1: Date) (date2: Date) = (date2 - date1).Days

        /// (b)(5)(iv) If the unit-period is a day, [...] the number of full unit-periods and the remaining fractions of a unit-period
        /// shall be determined by dividing the number of days between the 2 given dates by the number of days per unit-period. If the
        /// unit-period is a day, the number of unit-periods per year shall be 365. [...]
        let dailyUnitPeriods termStart transfers =
            transfers
            |> Array.map (fun t ->
                t,
                {
                    // the number of unit-periods is the actual number of days from the start of the term to the transfer date,
                    // rather than an index-based count that would assume consecutive daily transfers
                    Quotient = daysBetween termStart t.TransferDate
                    Remainder = 0m
                }
            )

        /// (b)(5)(ii) If the unit-period is a month, the number of full unit-periods between 2 dates shall be the number of months
        /// measured back from the later date. The remaining fraction of a unit-period shall be the number of days measured forward from
        /// the earlier date to the beginning of the first full unit-period, divided by 30. If the unit-period is a month, there are
        /// 12 unit-periods per year.
        ///
        /// (b)(5)(iii) If the unit-period is [...] a multiple of a month not exceeding 11 months, the number of days between 2 dates
        /// shall be 30 times the number of full months measured back from the later date, plus the number of remaining days. The number
        /// of full unit-periods and the remaining fraction of a unit-period shall be determined by dividing such number of days [...] by
        /// the appropriate multiple of 30 in the case of a multimonthly unit-period. [...] If the number of unit-periods is a multiple
        /// of a month, the number of unit-periods per year shall be 12 divided by the number of months per unit-period.
        ///
        /// (b)(5)(v) If the unit-period is a year, the number of full unit-periods between 2 dates shall be the number of full years (each
        /// equal to 12 months) measured back from the later date. The remaining fraction of a unit-period shall be
        ///
        /// > (A) The remaining number of months divided by 12 if the remaining interval is equal to a whole number of months, or
        ///
        /// > (B) The remaining number of days divided by 365 if the remaining interval is not equal to a whole number of months.
        let monthlyUnitPeriods multiple termStart transfers =
            let multiple = Math.Max(1, multiple)
            let transferDates = transfers |> Array.map _.TransferDate
            let transferCount = transfers |> Array.length
            let scheduleLength = UnitPeriod.PaymentCount <| (transferCount + 1) * multiple

            let unitPeriod =
                transferDates
                |> UnitPeriod.detect UnitPeriod.Direction.Reverse (UnitPeriod.Month 1)

            let schedule =
                UnitPeriod.generatePaymentSchedule scheduleLength UnitPeriod.Direction.Reverse unitPeriod
                |> Array.filter (fun d -> d >= termStart)

            let scheduleCount = schedule |> Array.length
            let lastWholeMonthBackIndex = 0
            let lastWholeUnitPeriodBackIndex = (scheduleCount - 1) % multiple

            let offset =
                scheduleCount - 1 - (transferCount - 1) * multiple
                |> fun i -> Decimal.Floor(decimal i / decimal multiple) |> int

            [| 0 .. (transferCount - 1) |]
            |> Array.map (fun i ->
                let wholeUnitPeriods = i + offset

                let remainingMonthDays =
                    decimal (lastWholeUnitPeriodBackIndex - lastWholeMonthBackIndex) * 30m

                let remainingDays = decimal (schedule[lastWholeMonthBackIndex] - termStart).Days

                let remainder =
                    if multiple < 12 then
                        (remainingMonthDays + remainingDays) / (30m * decimal multiple)
                    elif remainingDays = 0m then
                        decimal (lastWholeUnitPeriodBackIndex - lastWholeMonthBackIndex) / 12m
                    else
                        decimal (schedule[lastWholeUnitPeriodBackIndex] - termStart).Days / 365m

                transfers[i],
                {
                    Quotient = wholeUnitPeriods
                    Remainder = remainder
                }
            )

        /// (b)(5)(iii) If the unit-period is a semimonth [...], the number of days between 2 dates shall be 30 times the number of full
        /// months measured back from the later date, plus the number of remaining days. The number of full unit-periods and the remaining
        /// fraction of a unit-period shall be determined by dividing such number of days by 15 in the case of a semimonthly unit-period
        /// [...]. If the unit-period is a semimonth, the number of unit-periods per year shall be 24. [...]
        let semiMonthlyUnitPeriods termStart transfers =
            let transferDates = transfers |> Array.map _.TransferDate
            let transferCount = transfers |> Array.length
            let scheduleLength = UnitPeriod.PaymentCount <| transferCount + 2

            let frequency =
                transferDates
                |> UnitPeriod.detect UnitPeriod.Direction.Reverse UnitPeriod.SemiMonth

            let schedule =
                UnitPeriod.generatePaymentSchedule scheduleLength UnitPeriod.Direction.Reverse frequency
                |> Array.filter (fun d -> d >= termStart)

            let scheduleCount = schedule |> Array.length
            let offset = scheduleCount - transferCount

            [| 0 .. (transferCount - 1) |]
            |> Array.map (fun i ->
                let lastWholeMonthBackIndex = Math.Max(0, (i + offset) % 2)
                let wholeUnitPeriods = i + offset - lastWholeMonthBackIndex

                let fractional =
                    decimal (schedule[lastWholeMonthBackIndex] - termStart).Days / 15m
                    |> fun d -> divRem d 1m

                transfers[i],
                {
                    Quotient = wholeUnitPeriods + fractional.Quotient
                    Remainder = fractional.Remainder
                }
            )

        /// (b)(5)(iv) If the unit-period is [...] a week, or a multiple of a week, the number of full unit-periods and the remaining
        /// fractions of a unit-period shall be determined by dividing the number of days between the 2 given dates by the number of days
        /// per unit-period. [...] If the unit-period is a week or a multiple of a week, the number of unit-periods per year shall be
        /// 52 divided by the number of weeks per unit-period.
        let weeklyUnitPeriods multiple termStart transfers =
            let multiple = Math.Max(1, multiple)
            let daysPerUnitPeriod = 7m * decimal multiple

            transfers
            |> Array.map (fun t ->
                // divide the actual number of days from the start of the term to the transfer date by the number of days
                // per unit-period, rather than assuming transfers fall on consecutive unit-period boundaries
                let dr =
                    decimal (daysBetween termStart t.TransferDate) / daysPerUnitPeriod
                    |> fun d -> divRem d 1m

                t,
                {
                    Quotient = dr.Quotient
                    Remainder = dr.Remainder
                }
            )

        /// (b)(5)(vi) In a single advance, single payment transaction in which the term is less than a year and is equal
        /// to a whole number of months, the number of unit-periods in the term shall be 1, and the number of
        /// unit-periods per year shall be 12 divided by the number of months in the term or 365 divided by the
        /// number of days in the term.
        ///
        /// (b)(5)(vii) In a single advance, single payment transaction in which the term is less than a year and is not
        /// equal to a whole number of months, the number of unit-periods in the term shall be 1, and the number of
        /// unit-periods per year shall be 365 divided by the number of days in the term.
        let singleUnitPeriod _ transfers =
            let transfer = transfers |> Array.exactlyOne
            [| transfer, { Quotient = 1; Remainder = 0m } |]

        /// map an array of transfers to an array of whole and fractional unit periods
        let mapUnitPeriods unitPeriod =
            match unitPeriod with
            | UnitPeriod.Day -> dailyUnitPeriods
            | UnitPeriod.Week multiple -> weeklyUnitPeriods multiple
            | UnitPeriod.SemiMonth -> semiMonthlyUnitPeriods
            | UnitPeriod.Month multiple -> monthlyUnitPeriods multiple

        /// (b)(8) General equation.
        let generalEquation consummationDate firstFinanceChargeEarnedDate advances payments =
            if Array.isEmpty advances || Array.isEmpty payments then
                Solution.Impossible
            else
                let advanceTotal = advances |> Array.sumBy (_.Value >> Cent.toDecimal)

                if advanceTotal = 0m then
                    Solution.Impossible
                else
                    let paymentTotal = payments |> Array.sumBy (_.Value >> Cent.toDecimal)

                    let advanceDates = advances |> Array.map _.TransferDate
                    let paymentDates = payments |> Array.map _.TransferDate

                    let term =
                        UnitPeriod.transactionTerm
                            consummationDate
                            firstFinanceChargeEarnedDate
                            (paymentDates |> Array.last)
                            (advanceDates |> Array.last)

                    if advanceTotal = paymentTotal then
                        Solution.Found(0m, 0, 0m)
                    elif term.Duration = 0<DurationDay> then
                        // all transfers fall on the term start date, so there is no time dimension over which to compute a rate
                        Solution.Impossible
                    else
                        let unitPeriodsPerYear, unitPeriodMap =
                            // (b)(5)(vi)/(vii): in a single advance, single payment transaction in which the term is less than
                            // a year, the unit-period is the term itself, so the number of unit-periods in the term is 1, and
                            // the number of unit-periods per year is 12 divided by the number of months in the term if the term
                            // is equal to a whole number of months, or 365 divided by the number of days in the term otherwise
                            if Array.length advances = 1 && Array.length payments = 1 && int term.Duration < 365 then
                                let unitPeriodsPerYear =
                                    match [| 1..11 |] |> Array.tryFind (fun m -> term.Start.AddMonths m = term.End) with
                                    | Some months -> 12m / decimal months
                                    | None -> 365m / decimal (int term.Duration)

                                unitPeriodsPerYear, singleUnitPeriod
                            else
                                let unitPeriod = UnitPeriod.nearest term advanceDates paymentDates
                                UnitPeriod.numberPerYear unitPeriod, mapUnitPeriods unitPeriod

                        let paymentCount = payments |> Array.length |> decimal
                        let interestTotal = paymentTotal - advanceTotal

                        let roughUnitPeriodRate =
                            roughUnitPeriodRate advanceTotal interestTotal paymentCount

                        let unitPeriodRate =
                            let eq = [| advances[0].Value, 0m, 0 |]

                            let ft =
                                unitPeriodMap term.Start payments
                                |> Array.map (fun (tr, dr) ->
                                    let f = dr.Remainder //The fraction of a unit-period in the time interval from the beginning of the term of the transaction to the jth payment.
                                    let t = dr.Quotient //The number of full unit-periods from the beginning of the term of the transaction to the jth payment.
                                    tr.Value, f, t
                                )

                            let calc transfers unitPeriodRate =
                                transfers
                                |> Array.sumBy (fun (amount, remainder, quotient) ->
                                    try // high quotients will yield oversize decimals but high divisors will yield a zero result anyway
                                        let divisor =
                                            (1m + remainder * unitPeriodRate) * (1m + unitPeriodRate |> powi quotient)

                                        if divisor = 0m then 0m else Cent.toDecimal amount / divisor
                                    with _ ->
                                        0m
                                )

                            let generator unitPeriodRate =
                                let aa = calc eq unitPeriodRate
                                let pp = calc ft unitPeriodRate
                                let difference = Decimal.Round(pp - aa, 10)
                                difference

                            Array.solveNewtonRaphson generator 100u roughUnitPeriodRate 1e-6m

                        match unitPeriodRate with
                        | Solution.Found(upr, iteration, tolerance) ->
                            Solution.Found(annualPercentageRate upr unitPeriodsPerYear, iteration, tolerance)
                        | s -> s

    /// calculates the APR to a given precision for a single-advance transaction where the consummation date, first finance-charge earned date and
    /// advance date are all the same
    let calculate method advanceValue advanceDate transfers =
        match method with
        | CalculationMethod.EuropeanUnion _ -> EuropeanUnion.calculateApr advanceDate advanceValue transfers
        | CalculationMethod.UnitedKingdom _ -> UnitedKingdom.calculateApr advanceDate advanceValue transfers
        | CalculationMethod.UsActuarial _ ->
            let advances = [|
                {
                    TransferType = Advance
                    TransferDate = advanceDate
                    Value = advanceValue
                }
            |]

            if Array.isEmpty transfers then
                [|
                    {
                        TransferType = Payment
                        TransferDate = advanceDate.AddYears 1
                        Value = 0L<Cent>
                    }
                |]
            else
                transfers
            |> UsActuarial.generalEquation advanceDate advanceDate advances
        | CalculationMethod.UnitedStatesRule -> failwith "Not yet implemented"

    /// converts an APR solution to a percentage, if possible
    let toPercent aprMethod aprSolution =
        let precision =
            match aprMethod with
            | CalculationMethod.EuropeanUnion precision
            | CalculationMethod.UnitedKingdom precision
            | CalculationMethod.UsActuarial precision -> precision
            | _ -> 0

        match aprSolution with
        | Solution.Found(apr, _, _) -> Decimal.Round(apr, precision) |> Percent.fromDecimal
        // the Newton-Raphson solver only reports the iteration limit when the residual is still outside the tolerance,
        // so the partial solution is unreliable and must not be reported as if it were a valid APR
        | _ -> Percent 0m

    /// calculates the APR rate for the specified unit-period as per UK regulation
    let ukUnitPeriodRate unitPeriod apr =
        let unitPeriodsPerYear = UnitPeriod.numberPerYear unitPeriod

        if unitPeriodsPerYear = 0m then
            failwith $"Invalid unit period {unitPeriod}: it has zero unit-periods per year"

        apr
        |> Percent.toDecimal
        |> fun m -> (((1m + m) |> powm (1m / unitPeriodsPerYear) |> decimal) - 1m) * 100m
        |> Percent
