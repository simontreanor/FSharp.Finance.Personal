namespace FSharp.Finance.Personal

open System

// note: unit-period definitions are based on US federal legislation but the definitions are universally applicable
module UnitPeriod =

    /// a transaction term is the length of a transaction, from the start date to the final payment
    [<Struct>]
    type TransactionTerm = {
        Start: DateTime
        End: DateTime
        Duration: int<DurationDays>
    }

    /// calculate the transaction term based on specific events
    let transactionTerm (consummationDate: DateTime) firstFinanceChargeEarnedDate (lastPaymentDueDate: DateTime) lastAdvanceScheduledDate =
        let beginDate = if firstFinanceChargeEarnedDate > consummationDate then firstFinanceChargeEarnedDate else consummationDate
        let endDate = if lastAdvanceScheduledDate > lastPaymentDueDate then lastAdvanceScheduledDate else lastPaymentDueDate
        { Start = beginDate; End = endDate; Duration = (endDate.Date - beginDate.Date).Days * 1<DurationDays> }

    /// interval between payments
    [<Struct>]
    type UnitPeriod =
        | NoInterval of UnitPeriodDays:int<DurationDays> // cf. (b)(5)(vii)
        | Day
        | Week of WeekMultiple:int
        | SemiMonth
        | Month of MonthMultiple:int

    /// all unit-periods, excluding unlikely ones (opinionated!)
    let all =
        [|
            [| 1, Day |]
            // [| 1 .. 52 |] |> Array.map(fun i -> (i * 7), Week i)
            [| 1; 2; 4 |] |> Array.map(fun i -> (i * 7), Week i)
            [| 15, SemiMonth |]
            // [| 1 .. 12 |] |> Array.map(fun i -> (i * 30), Month i)
            [| 1; 2; 3; 6; 12 |] |> Array.map(fun i -> (i * 30), Month i)
        |]
        |> Array.concat
        |> Array.sortBy fst

    /// coerce a length to the nearest unit-period
    let normalise length =
        all
        |> Array.minBy(fun (days, _) -> abs (days - length))

    /// lengths that occur more than once
    let commonLengths (lengths: int array) =
        lengths
        |> Array.countBy id
        |> Array.filter(fun (_, count) -> count > 1)

    /// find the nearest unit-period according to the transaction term and transfer dates
    let nearest term advanceDates paymentDates =
        if (advanceDates |> Array.length) = 1 && (paymentDates |> Array.length) < 2 then
            min term.Duration 365<DurationDays>
            |> NoInterval
        else
            let periodLengths =
                [| [| term.Start |]; advanceDates; paymentDates |] |> Array.concat
                |> Array.sort
                |> Array.distinct
                |> Array.windowed 2
                |> Array.map ((fun a -> (a[1].Date - a[0].Date).Days) >> normalise >> fst)
            let commonPeriodLengths = periodLengths |> commonLengths
            if commonPeriodLengths |> Array.isEmpty then
                periodLengths
                |> Array.countBy id
                |> Array.sortBy snd
                |> Array.averageBy (snd >> decimal)
                |> roundMidpointTowardsZero
                |> int
            else
                commonPeriodLengths
                |> Array.sortBy snd
                |> Array.maxBy snd
                |> fst
            |> normalise
            |> snd

    /// number of unit-periods in a year
    let numberPerYear = function
        | (NoInterval unitPeriodDays) -> 365m / decimal unitPeriodDays
        | Day -> 365m
        | (Week multiple) -> 52m / decimal multiple
        | SemiMonth -> 24m
        | (Month multiple) -> 12m / decimal multiple

    /// unit period combined with a
    [<Struct>]
    type Config =
        /// single on the given date
        | Single of DateTime:DateTime
        /// daily starting on the given date
        | Daily of StartDate:DateTime
        /// (multi-)weekly: every n weeks starting on the given date
        | Weekly of WeekMultiple:int * WeekStartDate:DateTime
        /// semi-monthly: twice a month starting on the date given by year, month and day 1, with the other day given by day 2
        | SemiMonthly of smYear:int * smMonth:int * Day1:int * Day2:int
        /// (multi-)monthly: every n months starting on the date given by year, month and day, which tracks month-end (see config)
        | Monthly of MonthMultiple:int * Year:int * Month:int * Day:int

    module Config =
        /// pretty-print the unit-period config, useful for debugging 
        let serialise = function
        | Single dt -> $"""(Single {dt.ToString "yyyy-MM-dd"})"""
        | Daily sd -> $"""(Daily {sd.ToString "yyyy-MM-dd"})"""
        | Weekly (multiple, wsd) -> $"""({multiple.ToString "00"}-Weekly ({wsd.ToString "yyyy-MM-dd"}))"""
        | SemiMonthly (y, m, td1, td2) -> $"""(SemiMonthly ({y.ToString "0000"}-{m.ToString "00"}-({(int td1).ToString "00"}_{(int td2).ToString "00"}))"""
        | Monthly (multiple, y, m, d) -> $"""({multiple.ToString "00"}-Monthly ({y.ToString "0000"}-{m.ToString "00"}-{(int d).ToString "00"}))"""

        /// gets the start date based on a unit-period config
        let startDate = function
            | Single dt -> dt
            | Daily sd -> sd
            | Weekly (_, wsd) -> wsd
            | SemiMonthly (y, m, d1, _) -> TrackingDay.toDate y m d1
            | Monthly (_, y, m, d) -> TrackingDay.toDate y m d

        /// constrains the freqencies to valid values
        let constrain = function
            | Single _ | Daily _ | Weekly _ as f -> f
            | SemiMonthly (_, _, day1, day2) as f
                when day1 >= 1 && day1 <= 15 && day2 >= 16 && day2 <= 31 &&
                    ((day2 < 31 && day2 - day1 = 15) || (day2 = 31 && day1 = 15)) -> f
            | SemiMonthly (_, _, day1, day2) as f
                when day2 >= 1 && day2 <= 15 && day1 >= 16 && day1 <= 31 &&
                    ((day1 < 31 && day1 - day2 = 15) || (day1 = 31 && day2 = 15)) -> f
            | Monthly (_, _, _, day) as f when day >= 1 && day <= 31 -> f
            | f -> failwith $"Unit-period config `%O{f}` is out-of-bounds of constraints"

    /// generates a suggested number of payments to constrain the loan within a certain duration
    let maxPaymentCount (maxLoanLength: int<DurationDays>) (startDate: DateTime) (config: Config) =
        let offset y m td = ((TrackingDay.toDate y m td) - startDate).Days |> fun f -> int f * 1<DurationDays>
        match config with
        | Single dt -> maxLoanLength - offset dt.Year dt.Month dt.Day
        | Daily dt -> maxLoanLength - offset dt.Year dt.Month dt.Day
        | Weekly (multiple, dt) -> (maxLoanLength - offset dt.Year dt.Month dt.Day) / (multiple * 7)
        | SemiMonthly (y, m, d1, _) -> (maxLoanLength - offset y m (int d1)) / 15
        | Monthly (multiple, y, m, d) -> (maxLoanLength - offset y m (int d)) / (multiple * 30)
        |> int
