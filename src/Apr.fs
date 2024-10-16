namespace FSharp.Finance.Personal

open System

/// calculating the APR according to various country-specific regulations
module Apr =

    open ArrayExtension
    open Calculation
    open Currency
    open DateDay
    open Util

    /// the calculation method used to determine the APR
    [<RequireQualifiedAccess; Struct>]
    type CalculationMethod =
        /// calculates the APR according to UK FCA rules to the stated decimal precision (note that this is two places more than the percent precision)
        | UnitedKingdom of UkPrecision:int
        /// calculates the APR according to the US CFPB actuarial method to the stated decimal precision (note that this is two places more than the percent precision)
        | UsActuarial of UsPrecision:int
        /// calculates the APR according to the United States rule (not yet implemented)
        | UnitedStatesRule

    /// basic calculation to determine the APR
    let annualPercentageRate unitPeriodRate unitPeriodsPerYear =
        unitPeriodRate * unitPeriodsPerYear

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

    /// APR as in https://www.handbook.fca.org.uk/handbook/MCOB/10/?view=chapter
    module UnitedKingdom =

        /// calculates the APR
        let calculateApr (startDate: Date) (principal: int64<Cent>) (transfers: Transfer array) =
            if principal = 0L<Cent> || Array.isEmpty transfers then Solution.Impossible else
            let payments = transfers |> Array.filter(fun t -> t.TransferType = Payment)
            let paymentTotal = payments |> Array.sumBy _.Value
            if principal = paymentTotal then Solution.Found(0m, 0, 0) else
            let roughApr = 4.2m
            let ak = [| principal, 0m |]
            let a'k' =
                payments
                |> Array.map(fun t ->
                    t.Value, decimal (t.TransferDate - startDate).Days / 365m
                )
            let calc transfers unitPeriodRate =
                transfers
                |> Array.sumBy(fun (amount, years) ->
                    let divisor = ((1m + unitPeriodRate) |> powm years)
                    if divisor = 0. then 0m else amount |> Cent.toDecimal |> fun a -> double a / divisor |> decimal
                )
            let generator unitPeriodRate =
                let aa = calc ak unitPeriodRate
                let pp = calc a'k' unitPeriodRate
                let difference = Decimal.Round(pp - aa, 8)
                difference
            Array.solve generator 100 roughApr AroundZero ToleranceSteps.zero

    /// APR as in https://www.consumerfinance.gov/rules-policy/regulations/1026/j/
    module UsActuarial =

        /// (b)(5)(i) The number of days between 2 dates shall be the number of 24-hour intervals between any point in time on the first 
        /// date to the same point in time on the second date.
        let daysBetween (date1: Date) (date2: Date) =
            (date2 - date1).Days

        /// (b)(5)(iv) If the unit-period is a day, [...] the number of full unit-periods and the remaining fractions of a unit-period 
        /// shall be determined by dividing the number of days between the 2 given dates by the number of days per unit-period. If the 
        /// unit-period is a day, the number of unit-periods per year shall be 365. [...]
        let dailyUnitPeriods termStart transfers =
            let transferDates = transfers |> Array.map _.TransferDate
            let offset = daysBetween termStart (transferDates |> Array.head)
            transfers
            |> Array.mapi(fun i t -> t, { Quotient = i + offset; Remainder = 0m })

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
        let monthlyUnitPeriods (multiple: int) termStart transfers =
            let multiple = Math.Max(1, multiple)
            let transferDates = transfers |> Array.map _.TransferDate
            let transferCount = transfers |> Array.length
            let unitPeriod = transferDates |> UnitPeriod.detect UnitPeriod.Direction.Reverse (UnitPeriod.Month 1)
            let schedule = UnitPeriod.generatePaymentSchedule ((transferCount + 1) * multiple) Duration.Unlimited UnitPeriod.Direction.Reverse unitPeriod |> Array.filter(fun d -> d >= termStart)
            let scheduleCount = schedule |> Array.length
            let lastWholeMonthBackIndex = 0
            let lastWholeUnitPeriodBackIndex = (scheduleCount - 1) % multiple
            let offset = (scheduleCount - 1) - ((transferCount - 1) * multiple) |> fun i -> Decimal.Floor(decimal i / decimal multiple) |> int
            [| 0 .. (transferCount - 1) |]
            |> Array.map(fun i ->
                let wholeUnitPeriods = i + offset
                let remainingMonthDays = decimal (lastWholeUnitPeriodBackIndex - lastWholeMonthBackIndex) * 30m
                let remainingDays = decimal (schedule[lastWholeMonthBackIndex] - termStart).Days
                let remainder =
                    if multiple < 12 then
                        (remainingMonthDays + remainingDays) / (30m * decimal multiple)
                    elif remainingDays = 0m then
                        decimal (lastWholeUnitPeriodBackIndex - lastWholeMonthBackIndex) / 12m
                    else
                        decimal (schedule[lastWholeUnitPeriodBackIndex] - termStart).Days / 365m
                transfers[i], { Quotient = wholeUnitPeriods; Remainder = remainder }
            )

        /// (b)(5)(iii) If the unit-period is a semimonth [...], the number of days between 2 dates shall be 30 times the number of full 
        /// months measured back from the later date, plus the number of remaining days. The number of full unit-periods and the remaining 
        /// fraction of a unit-period shall be determined by dividing such number of days by 15 in the case of a semimonthly unit-period 
        /// [...]. If the unit-period is a semimonth, the number of unit-periods per year shall be 24. [...]
        let semiMonthlyUnitPeriods termStart transfers =
            let transferDates = transfers |> Array.map _.TransferDate
            let transferCount = transfers |> Array.length
            let frequency = transferDates |> UnitPeriod.detect UnitPeriod.Direction.Reverse UnitPeriod.SemiMonth
            let schedule = UnitPeriod.generatePaymentSchedule (transferCount + 2) Duration.Unlimited UnitPeriod.Direction.Reverse frequency |> Array.filter(fun d -> d >= termStart)
            let scheduleCount = schedule |> Array.length
            let offset = scheduleCount - transferCount
            [| 0 .. (transferCount - 1) |]
            |> Array.map(fun i ->
                let lastWholeMonthBackIndex = Math.Max(0, (i + offset) % 2)
                let wholeUnitPeriods = i + offset - lastWholeMonthBackIndex
                let fractional = decimal (schedule[lastWholeMonthBackIndex] - termStart).Days / 15m |> fun d -> divRem d 1m
                transfers[i], { Quotient = wholeUnitPeriods + fractional.Quotient; Remainder = fractional.Remainder }
            )

        /// (b)(5)(iv) If the unit-period is [...] a week, or a multiple of a week, the number of full unit-periods and the remaining 
        /// fractions of a unit-period shall be determined by dividing the number of days between the 2 given dates by the number of days 
        /// per unit-period. [...] If the unit-period is a week or a multiple of a week, the number of unit-periods per year shall be 
        /// 52 divided by the number of weeks per unit-period.
        let weeklyUnitPeriods (multiple: int) termStart transfers =
            let multiple = Math.Max(1, multiple)
            let transferDates = transfers |> Array.map _.TransferDate
            let dr  = decimal (daysBetween termStart (transferDates |> Array.head)) / (7m * decimal multiple) |> fun d -> divRem d 1m
            transfers
            |> Array.mapi(fun i t ->
                t, { Quotient = dr.Quotient + i; Remainder = dr.Remainder }
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
            | (UnitPeriod.NoInterval _) -> singleUnitPeriod
            | UnitPeriod.Day -> dailyUnitPeriods
            | (UnitPeriod.Week multiple) -> weeklyUnitPeriods multiple
            | UnitPeriod.SemiMonth -> semiMonthlyUnitPeriods
            | (UnitPeriod.Month multiple) -> monthlyUnitPeriods multiple

        /// (b)(8) General equation.
        let generalEquation consummationDate firstFinanceChargeEarnedDate advances payments =
            if Array.isEmpty advances || Array.isEmpty payments then Solution.Impossible else
            let advanceTotal = advances |> Array.sumBy (_.Value >> Cent.toDecimal)
            if advanceTotal = 0m then Solution.Impossible else
            let paymentTotal = payments |> Array.sumBy (_.Value >> Cent.toDecimal)
            if advanceTotal = paymentTotal then Solution.Found(0m, 0, 0) else
            let advanceDates = advances |> Array.map _.TransferDate
            let paymentDates = payments |> Array.map _.TransferDate
            let term = UnitPeriod.transactionTerm consummationDate firstFinanceChargeEarnedDate (paymentDates |> Array.last) (advanceDates |> Array.last)
            let unitPeriod = UnitPeriod.nearest term advanceDates paymentDates
            let unitPeriodsPerYear = UnitPeriod.numberPerYear unitPeriod
            let roughUnitPeriodRate =
                let paymentAverage = payments |> Array.averageBy (_.Value >> Cent.toDecimal)
                let paymentCount = payments |> Array.length |> decimal
                if advanceTotal = 0m || unitPeriodsPerYear = 0m then
                    0m
                else
                    ((paymentAverage / advanceTotal) * (paymentCount / 12m)) / unitPeriodsPerYear
            let unitPeriodRate =
                let eq = [| advances[0].Value, 0m, 0 |]
                let ft =
                    mapUnitPeriods unitPeriod term.Start payments
                    |> Array.map(fun (tr, dr) ->
                        let f = dr.Remainder //The fraction of a unit-period in the time interval from the beginning of the term of the transaction to the jth payment.
                        let t = dr.Quotient //The number of full unit-periods from the beginning of the term of the transaction to the jth payment.
                        tr.Value, f, t
                    )
                let calc transfers unitPeriodRate =
                    transfers
                    |> Array.sumBy(fun (amount, remainder, quotient) ->
                        try // high quotients will yield oversize decimals but high divisors will yield a zero result anyway
                            let divisor = (1m + (remainder * unitPeriodRate)) * ((1m + unitPeriodRate) |> powi quotient)
                            if divisor = 0m then 0m else Cent.toDecimal amount / divisor
                        with _ -> 0m
                    )
                let generator unitPeriodRate =
                    let aa = calc eq unitPeriodRate
                    let pp = calc ft unitPeriodRate
                    let difference = Decimal.Round(pp - aa, 10)
                    difference
                Array.solve generator 100 roughUnitPeriodRate AroundZero ToleranceSteps.zero
            match unitPeriodRate with
            | Solution.Found(upr, iteration, tolerance) ->
                Solution.Found (annualPercentageRate upr unitPeriodsPerYear, iteration, tolerance)
            | s -> s

    /// calculates the APR to a given precision for a single-advance transaction where the consummation date, first finance-charge earned date and
    /// advance date are all the same
    let calculate method advanceValue advanceDate transfers =
        match method with
        | CalculationMethod.UnitedKingdom _ ->
            UnitedKingdom.calculateApr advanceDate advanceValue transfers
        | CalculationMethod.UsActuarial _ ->
            let advances = [| { TransferType = Advance; TransferDate = advanceDate; Value = advanceValue } |]
            if Array.isEmpty transfers then [| { TransferType = Payment; TransferDate = advanceDate.AddYears 1; Value = 0L<Cent> } |]
            else transfers
            |> UsActuarial.generalEquation advanceDate advanceDate advances
        | CalculationMethod.UnitedStatesRule ->
            failwith "Not yet implemented"

    /// converts an APR solution to a percentage, if possible
    let toPercent aprMethod aprSolution =
        let precision = aprMethod |> function CalculationMethod.UnitedKingdom precision | CalculationMethod.UsActuarial precision -> precision | _ -> 0
        match aprSolution with
        | Solution.Found(apr, _, _)
        | Solution.IterationLimitReached(apr, _, _) ->
            Decimal.Round(apr, precision)
            |> Percent.fromDecimal
            |> ValueSome
        | _ -> ValueNone

    /// calculates the APR rate for the specified unit-period as per UK regulation
    let ukUnitPeriodRate unitPeriod apr =
        apr
        |> Percent.toDecimal
        |> fun m -> (((1m + m) |> powm (1m / UnitPeriod.numberPerYear unitPeriod) |> decimal) - 1m) * 100m
        |> Percent
