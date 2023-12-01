namespace FSharp.Finance.Personal

open System

module UnitPeriod =

    /// a transaction term is the length of a transaction, from the start date to the final payment
    [<Struct>]
    type TransactionTerm = {
        Start: DateTime
        End: DateTime
        Duration: int<Duration>
    }

    /// calculate the transaction term based on specific events
    let transactionTerm (consummationDate: DateTime) firstFinanceChargeEarnedDate (lastPaymentDueDate: DateTime) lastAdvanceScheduledDate =
        let beginDateTime = if firstFinanceChargeEarnedDate > consummationDate then firstFinanceChargeEarnedDate else consummationDate
        let endDateTime = if lastAdvanceScheduledDate > lastPaymentDueDate then lastAdvanceScheduledDate else lastPaymentDueDate
        { Start = beginDateTime; End = endDateTime; Duration = (endDateTime.Date - beginDateTime.Date).Days * 1<Duration> }

    /// interval between payments
    [<Struct>]
    type UnitPeriod =
        | NoInterval of UnitPeriodDays:int<Duration> // cf. (b)(5)(vii)
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
            min term.Duration 365<Duration>
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

    /// settings for a semi-monthly payment config, where day 1 is in the first half of the month (1st to 15th) and day 2 in the second half (16th to 31st)
    [<Struct>]
    type SemiMonthlyConfig = SemiMonthlyConfig of Year:int * Month:int * Day1:int<TrackingDay> * Day2:int<TrackingDay>

    /// settings for a monthly payment config, where day is the day of the month to track
    [<Struct>]
    type MonthlyConfig = MonthlyConfig of Year:int * Month:int * Day:int<TrackingDay>

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
        | SemiMonthly of SemiMonthlyConfig
        /// (multi-)monthly: every n months starting on the date given by year, month and day, which tracks month-end (see config)
        | Monthly of MonthMultiple:int * MonthlyConfig

    module Config =

        let serialise = function
        | Single dt -> $"""Single+{dt.ToString "yyyy-MM-dd"}"""
        | Daily sd -> $"""Daily+{sd.ToString "yyyy-MM-dd"}"""
        | Weekly (wm, wsd) -> $"""{wm.ToString "00"}-Weekly+{wsd.ToString "yyyy-MM-dd"}"""
        | SemiMonthly (SemiMonthlyConfig (y, m, td1, td2)) -> $"""SemiMonthly+{y.ToString "0000"}-{m.ToString "00"}-({(int td1).ToString "00"}+{(int td2).ToString "00"})"""
        | Monthly (mm, MonthlyConfig(y, m, d)) -> $"""{mm.ToString "00"}-Monthly+{y.ToString "0000"}+{m.ToString "00"}+{(int d).ToString "00"}"""

    let configStartDate = function
        | Single dt -> dt
        | Daily sd -> sd
        | Weekly (_, wsd) -> wsd
        | SemiMonthly (SemiMonthlyConfig(y, m, d1, _)) -> TrackingDay.toDate y m (int d1)
        | Monthly (_, MonthlyConfig(y, m, d)) -> TrackingDay.toDate y m (int d)

    /// constrains the freqencies to valid values
    let constrain = function
        | Single _ | Daily _ | Weekly _ as f -> f
        | SemiMonthly (SemiMonthlyConfig (_, _, day1, day2)) as f
            when day1 >= 1<TrackingDay> && day1 <= 15<TrackingDay> && day2 >= 16<TrackingDay> && day2 <= 31<TrackingDay> &&
                ((day2 < 31<TrackingDay> && day2 - day1 = 15<TrackingDay>) || (day2 = 31<TrackingDay> && day1 = 15<TrackingDay>)) -> f
        | SemiMonthly (SemiMonthlyConfig (_, _, day1, day2)) as f
            when day2 >= 1<TrackingDay> && day2 <= 15<TrackingDay> && day1 >= 16<TrackingDay> && day1 <= 31<TrackingDay> &&
                ((day1 < 31<TrackingDay> && day1 - day2 = 15<TrackingDay>) || (day1 = 31<TrackingDay> && day2 = 15<TrackingDay>)) -> f
        | Monthly (_, MonthlyConfig (_, _, day)) as f when day >= 1<TrackingDay> && day <= 31<TrackingDay> -> f
        | f -> failwith $"Unit-period config `%O{f}` is out-of-bounds of constraints"

    /// generates a suggested number of payments to constrain the loan within a certain duration
    let maxPaymentCount (maxLoanLength: int<Duration>) (startDate: DateTime) (config: Config) =
        let offset y m td = ((TrackingDay.toDate y m td) - startDate).Days |> fun f -> int f * 1<Duration>
        match config with
        | Single dt -> maxLoanLength - offset dt.Year dt.Month dt.Day
        | Daily dt -> maxLoanLength - offset dt.Year dt.Month dt.Day
        | (Weekly (multiple, dt)) -> (maxLoanLength - offset dt.Year dt.Month dt.Day) / (multiple * 7)
        | SemiMonthly (SemiMonthlyConfig (y, m, d1, _)) -> (maxLoanLength - offset y m (int d1)) / 15
        | (Monthly (multiple, MonthlyConfig (y, m, d))) -> (maxLoanLength - offset y m (int d)) / (multiple * 30)
        |> int
