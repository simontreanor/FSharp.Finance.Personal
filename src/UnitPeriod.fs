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

        /// creates a semi monthly config specifying the first day only, using month-end tracking where appropriate
        let defaultSemiMonthly year month day1 =
            let day2 =
                if day1 < 15 then day1 + 15
                elif day1 = 15 then 31
                elif day1 = 31 then 15
                else day1 - 15
            SemiMonthly (year, month, day1, day2)

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

    /// direction in which to generate the schedule: forward works forwards from a given date and reverse works backwards
    [<Struct;RequireQualifiedAccess>]
    type Direction =
        | Forward
        | Reverse

    /// generate a payment schedule based on a unit-period config
    let generatePaymentSchedule count direction unitPeriodConfig =
        let adjustMonthEnd (monthEndTrackingDay: int) (dt: DateTime) =
            if dt.Day > 15 && monthEndTrackingDay > 28 then
                TrackingDay.toDate dt.Year dt.Month monthEndTrackingDay
            else dt
        let generate =
            match unitPeriodConfig |> Config.constrain with
            | Single startDate ->
                Array.map (fun _ -> startDate)
            | Daily startDate ->
                Array.map (float >> startDate.AddDays)
            | Weekly (multiple, startDate) ->
                Array.map (fun c -> startDate.AddDays (float(c * 7 * multiple)))
            | SemiMonthly (year, month, td1, td2) ->
                let startDate = TrackingDay.toDate year month td1
                let offset, monthEndTrackingDay = (if td1 > td2 then 1, td1 else 0, td2) |> fun (o, metd) -> (match direction with Direction.Forward -> o, metd | Direction.Reverse -> o - 1, metd)
                Array.collect(fun c -> [|
                    startDate.AddMonths c |> adjustMonthEnd monthEndTrackingDay
                    startDate.AddMonths (c + offset) |> fun dt -> TrackingDay.toDate dt.Year dt.Month td2 |> adjustMonthEnd monthEndTrackingDay
                |])
                >> Array.take count
            | Monthly (multiple, year, month, td) ->
                let startDate = TrackingDay.toDate year month td
                Array.map (fun c -> startDate.AddMonths (c * multiple) |> adjustMonthEnd td)
        match direction with
        | Direction.Forward -> [| 0 .. (count - 1) |] |> generate
        | Direction.Reverse -> [| 0 .. -1 .. -(count - 1) |] |> generate |> Array.sort

    /// for a given interval and array of dates, devise the unit-period config
    let detect direction interval (transferDates: DateTime array) =
        if Array.isEmpty transferDates then Single (DateTime.Today.AddYears 1) else
        let transform = match direction with Direction.Forward -> id | Direction.Reverse -> Array.rev
        let transferDates = transform transferDates
        let firstTransferDate = transferDates |> Array.head
        match interval with
        | NoInterval _ -> Single firstTransferDate
        | Day -> Daily firstTransferDate
        | Week multiple -> Weekly (multiple, firstTransferDate)
        | SemiMonth ->
            if Array.length transferDates % 2 = 1 then // deal with odd numbers of transfer dates
                transferDates |> Array.lastButOne |> fun a -> [| transferDates; [| a |] |] |> Array.concat
            else transferDates
            |> Array.chunkBySize 2
            |> Array.transpose
            |> Array.map (Array.maxBy _.Day >> _.Day)
            |> fun days -> SemiMonthly(firstTransferDate.Year, firstTransferDate.Month, days[0], days[1])
        | Month multiple ->
            transferDates
            |> Array.maxBy _.Day
            |> _.Day
            |> fun day -> Monthly(multiple, firstTransferDate.Year, firstTransferDate.Month, day)
