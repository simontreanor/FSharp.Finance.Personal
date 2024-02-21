namespace FSharp.Finance.Personal

/// an unambiguous way to represent regular date intervals and generate schedules based on them
/// 
/// note: unit-period definitions are based on US federal legislation but the definitions are universally applicable
module UnitPeriod =

    /// a transaction term is the length of a transaction (i.e. a financial product), from the start date to the final payment
    [<Struct>]
    type TransactionTerm = {
        /// the first date of the transaction
        Start: Date
        /// the last date of the transaction
        End: Date
        /// the length of the transaction in days
        Duration: int<DurationDay>
    }

    /// calculate the transaction term based on specific events
    let transactionTerm (consummationDate: Date) firstFinanceChargeEarnedDate (lastPaymentDueDate: Date) lastAdvanceScheduledDate =
        let beginDate = if firstFinanceChargeEarnedDate > consummationDate then firstFinanceChargeEarnedDate else consummationDate
        let endDate = if lastAdvanceScheduledDate > lastPaymentDueDate then lastAdvanceScheduledDate else lastPaymentDueDate
        { Start = beginDate; End = endDate; Duration = (endDate - beginDate).Days * 1<DurationDay> }

    /// interval between payments
    [<Struct>]
    type UnitPeriod =
        /// non-recurring
        | NoInterval of UnitPeriodDays:int<DurationDay> // cf. (b)(5)(vii)
        /// a day
        | Day
        /// a week or a multiple of weeks
        | Week of WeekMultiple:int
        /// half a month
        | SemiMonth
        /// a month or a multiple of months
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
            min term.Duration 365<DurationDay>
            |> NoInterval
        else
            let periodLengths =
                [| [| term.Start |]; advanceDates; paymentDates |] |> Array.concat
                |> Array.sort
                |> Array.distinct
                |> Array.windowed 2
                |> Array.map ((fun a -> (a[1] - a[0]).Days) >> normalise >> fst)
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
        | Single of Date:Date
        /// daily starting on the given date
        | Daily of StartDate:Date
        /// (multi-)weekly: every n weeks starting on the given date
        | Weekly of WeekMultiple:int * WeekStartDate:Date
        /// semi-monthly: twice a month starting on the date given by year, month and day 1, with the other day given by day 2
        | SemiMonthly of smYear:int * smMonth:int * Day1:int * Day2:int
        /// (multi-)monthly: every n months starting on the date given by year, month and day, which tracks month-end (see config)
        | Monthly of MonthMultiple:int * Year:int * Month:int * Day:int

    module Config =

        /// creates a semi-monthly config specifying the first day only, using month-end tracking where appropriate
        let defaultSemiMonthly (startDate: Date) =
            let day1 = startDate.Day
            let monthEndDay1 = Date.DaysInMonth(startDate.Year, startDate.Month)
            let trackingDay1 = if day1 = monthEndDay1 then 31 else day1
            let day2, monthOffset =
                if trackingDay1 < 15 then trackingDay1 + 15, 0
                elif trackingDay1 = 15 then 31, 0
                elif trackingDay1 = 31 then 15, 1
                else trackingDay1 - 15, 1
            let monthEndDay2 = startDate.AddMonths monthOffset |> fun d -> Date.DaysInMonth(d.Year, d.Month)
            let trackingDay2 = if day1 >= 15 && day2 = monthEndDay2 then 31 else day2
            SemiMonthly (startDate.Year, startDate.Month, trackingDay1, trackingDay2)

        let defaultMonthly multiple (startDate: Date) =
            let monthEndDay = Date.DaysInMonth(startDate.Year, startDate.Month)
            let trackingDay = if startDate.Day = monthEndDay then 31 else startDate.Day
            Monthly (multiple, startDate.Year, startDate.Month, trackingDay)

        /// create a unit-period config from a unit period (using month-end tracking for semi-monthly and monthly unit periods)
        let from startDate = function
        | NoInterval unitPeriodDays -> Single startDate
        | Day -> Daily startDate
        | Week weekMultiple -> Weekly (weekMultiple, startDate)
        | SemiMonth -> defaultSemiMonthly startDate
        | Month monthMultiple -> defaultMonthly monthMultiple startDate

        /// approximate length of unit period in days, used e.g. for generating rescheduling iterations
        let roughLength = function
        | Single _ -> 365m
        | Daily _ -> 1m
        | Weekly (multiple, _) -> 7m * decimal multiple
        | SemiMonthly _ -> 15m
        | Monthly (multiple, _, _, _) -> 30m * decimal multiple

        /// pretty-print the unit-period config, useful for debugging 
        let serialise = function
        | Single d -> $"""(Single {d.ToString()})"""
        | Daily sd -> $"""(Daily {sd.ToString()})"""
        | Weekly (multiple, wsd) -> $"""({multiple.ToString "00"}-Weekly ({wsd.ToString()}))"""
        | SemiMonthly (y, m, td1, td2) -> $"""(SemiMonthly ({y.ToString "0000"}-{m.ToString "00"}-({(int td1).ToString "00"}_{(int td2).ToString "00"}))"""
        | Monthly (multiple, y, m, d) -> $"""({multiple.ToString "00"}-Monthly ({y.ToString "0000"}-{m.ToString "00"}-{(int d).ToString "00"}))"""

        /// gets the start date based on a unit-period config
        let startDate = function
            | Single d -> d
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
    let maxPaymentCount (maxLoanLength: int<DurationDay>) (startDate: Date) (config: Config) =
        let offset y m td = ((TrackingDay.toDate y m td) - startDate).Days |> fun f -> int f * 1<DurationDay>
        match config with
        | Single d -> maxLoanLength - offset d.Year d.Month d.Day
        | Daily d -> maxLoanLength - offset d.Year d.Month d.Day
        | Weekly (multiple, d) -> (maxLoanLength - offset d.Year d.Month d.Day) / (multiple * 7)
        | SemiMonthly (y, m, d1, _) -> (maxLoanLength - offset y m (int d1)) / 15
        | Monthly (multiple, y, m, d) -> (maxLoanLength - offset y m (int d)) / (multiple * 30)
        |> int

    /// direction in which to generate the schedule: forward works forwards from a given date and reverse works backwards
    [<Struct;RequireQualifiedAccess>]
    type Direction =
        /// create a schedule starting on the given date
        | Forward
        /// create a schedule ending on the given date
        | Reverse

    /// generate a payment schedule based on a unit-period config
    let generatePaymentSchedule count direction unitPeriodConfig =
        let adjustMonthEnd (monthEndTrackingDay: int) (d: Date) =
            if d.Day > 15 && monthEndTrackingDay > 28 then
                TrackingDay.toDate d.Year d.Month monthEndTrackingDay
            else d
        let generate =
            match unitPeriodConfig |> Config.constrain with
            | Single startDate ->
                Array.map (fun _ -> startDate)
            | Daily startDate ->
                Array.map startDate.AddDays
            | Weekly (multiple, startDate) ->
                Array.map (fun c -> startDate.AddDays (c * 7 * multiple))
            | SemiMonthly (year, month, td1, td2) ->
                let startDate = TrackingDay.toDate year month td1
                let offset, monthEndTrackingDay = (if td1 > td2 then 1, td1 else 0, td2) |> fun (o, metd) -> (match direction with Direction.Forward -> o, metd | Direction.Reverse -> o - 1, metd)
                Array.collect(fun c -> [|
                    startDate.AddMonths c |> adjustMonthEnd monthEndTrackingDay
                    startDate.AddMonths (c + offset) |> fun d -> TrackingDay.toDate d.Year d.Month td2 |> adjustMonthEnd monthEndTrackingDay
                |])
                >> Array.take count
            | Monthly (multiple, year, month, td) ->
                let startDate = TrackingDay.toDate year month td
                Array.map (fun c -> startDate.AddMonths (c * multiple) |> adjustMonthEnd td)
        match direction with
        | Direction.Forward -> [| 0 .. (count - 1) |] |> generate
        | Direction.Reverse -> [| 0 .. -1 .. -(count - 1) |] |> generate |> Array.sort

    /// for a given interval and array of dates, devise the unit-period config
    let detect direction interval (transferDates: Date array) =
        if Array.isEmpty transferDates then failwith "No transfer dates given" else
        let transform = match direction with Direction.Forward -> id | Direction.Reverse -> Array.rev
        let transferDates = transform transferDates
        let firstTransferDate = transferDates |> Array.head
        match interval with
        | NoInterval _ -> Single firstTransferDate
        | Day -> Daily firstTransferDate
        | Week multiple -> Weekly (multiple, firstTransferDate)
        | SemiMonth ->
            if Array.length transferDates % 2 = 1 then // deal with odd numbers of transfer dates
                transferDates
                |> Array.vTryLastBut 1
                |> ValueOption.map(fun a -> [| transferDates; [| a |] |] |> Array.concat)
                |> ValueOption.defaultValue transferDates
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
