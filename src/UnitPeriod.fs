namespace FSharp.Finance

open System

module UnitPeriod =

    [<Struct>]
    type UnitPeriod =
        | NoInterval of UnitPeriodDays:int // cf. (b)(5)(vii)
        | Day
        | Week of WeekMultiple:int
        | SemiMonth
        | Month of MonthMultiple:int

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
        |> Array.minBy(fun (days, _) -> Math.Abs(days - length))

    let commonLengths (lengths: int array) =
        lengths
        |> Array.countBy id
        |> Array.filter(fun (_, count) -> count > 1)

    /// find the nearest unit-period according to the transaction term and transfer dates
    let nearest term advanceDates paymentDates =
        if (advanceDates |> Array.length) = 1 && (paymentDates |> Array.length) = 1 then
            Math.Min(term.TotalDays, 365)
            |> NoInterval
        else
            let periodLengths =
                [| [| term.Start |]; advanceDates; paymentDates |] |> Array.concat
                |> Array.sort
                |> Array.distinct
                |> Array.windowed 2
                |> Array.map ((fun a -> (a[1].Date - a[0].Date).TotalDays) >> int >> normalise >> fst)
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

    /// day of month, bug: specifying 29, 30, or 31 means the dates will track the specific day of the month where
    /// possible, otherwise the day will be the last day of the month; so 31 will track the month end; also note that it is
    /// possible to start with e.g. (2024, 02, 31) and this will yield 2024-02-29 29 2024-03-31 2024-04-30 etc.
    [<Struct>]
    type TrackingDay = TrackingDay of int

    [<Struct>]
    type SemiMonthlyConfig = SemiMonthlyConfig of Year:int * Month:int * Day1:TrackingDay * Day2:TrackingDay

    [<Struct>]
    type MonthlyConfig = MonthlyConfig of Year:int * Month:int * Day:TrackingDay

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

    /// constrains the freqencies to valid values
    let constrain = function
        | Single _ | Daily _ | Weekly _ as f -> f
        | SemiMonthly (SemiMonthlyConfig (_, _, TrackingDay day1, TrackingDay day2)) as f
            when day1 >= 1 && day1 <= 15 && day2 >= 16 && day2 <= 31 && ((day2 < 31 && day2 - day1 = 15) || (day2 = 31 && day1 = 15)) -> f
        | SemiMonthly (SemiMonthlyConfig (_, _, TrackingDay day1, TrackingDay day2)) as f
            when day2 >= 1 && day2 <= 15 && day1 >= 16 && day1 <= 31 && ((day1 < 31 && day1 - day2 = 15) || (day1 = 31 && day2 = 15)) -> f
        | Monthly (_, MonthlyConfig (_, _, TrackingDay day)) as f when day >= 1 && day <= 31 -> f
        | f -> failwith $"Unit-period config `%O{f}` is out-of-bounds of constraints"
