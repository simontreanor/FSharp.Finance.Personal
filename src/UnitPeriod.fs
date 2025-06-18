namespace FSharp.Finance.Personal

/// an unambiguous way to represent regular date intervals and generate schedules based on them
///
/// note: unit-period definitions are based on US federal legislation but the definitions are universally applicable
module UnitPeriod =

    open Calculation
    open DateDay

    /// a transaction term is the length of a transaction (i.e. a financial product), from the start date to the final payment
    [<Struct>]
    type TransactionTerm = {
        /// the first date of the transaction
        Start: Date
        /// the last date of the transaction
        End: Date
        /// the length of the transaction in days
        Duration: int<OffsetDay>
    }

    /// calculate the transaction term based on specific events
    let transactionTerm
        (consummationDate: Date)
        firstFinanceChargeEarnedDate
        (lastPaymentDueDate: Date)
        lastAdvanceScheduledDate
        =
        let beginDate =
            if firstFinanceChargeEarnedDate > consummationDate then
                firstFinanceChargeEarnedDate
            else
                consummationDate

        let endDate =
            if lastAdvanceScheduledDate > lastPaymentDueDate then
                lastAdvanceScheduledDate
            else
                lastPaymentDueDate

        {
            Start = beginDate
            End = endDate
            Duration = OffsetDay.fromDate beginDate endDate
        }

    /// interval between payments
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type UnitPeriod =
        /// a day
        | Day
        /// a week or a multiple of weeks
        | Week of WeekMultiple: int
        /// half a month
        | SemiMonth
        /// a month or a multiple of months
        | Month of MonthMultiple: int

        /// HTML formatting to display the unit period in a readable format
        member up.Html =
            match up with
            | Day -> "day"
            | Week multiple when multiple = 1 -> "week"
            | Week multiple -> $"{multiple}-week"
            | SemiMonth -> "semi-month"
            | Month multiple when multiple = 1 -> "month"
            | Month multiple -> $"{multiple}-month"

    /// all unit-periods, excluding unlikely ones (opinionated!)
    let all =
        [|
            [| 1, Day |]
            // [| 1 .. 52 |] |> Array.map(fun i -> i * 7, Week i)
            [| 1; 2; 4 |] |> Array.map (fun i -> i * 7, Week i)
            [| 15, SemiMonth |]
            // [| 1 .. 12 |] |> Array.map(fun i -> i * 30, Month i)
            [| 1; 2; 3; 6; 12 |] |> Array.map (fun i -> i * 30, Month i)
        |]
        |> Array.concat
        |> Array.sortBy fst

    /// coerce a length to the nearest unit-period
    let internal normalise length =
        all |> Array.minBy (fun (days, _) -> abs (days - length))

    /// lengths that occur more than once
    let commonLengths (lengths: int array) =
        lengths |> Array.countBy id |> Array.filter (fun (_, count) -> count > 1)

    /// find the nearest unit-period according to the transaction term and transfer dates
    let nearest term advanceDates paymentDates =
        let periodLengths =
            [| [| term.Start |]; advanceDates; paymentDates |]
            |> Array.concat
            |> Array.sort
            |> Array.distinct
            |> Array.windowed 2
            |> Array.map ((fun a -> (a[1] - a[0]).Days) >> normalise >> fst)

        let commonPeriodLengths =
            if Array.isEmpty periodLengths then
                [||]
            else
                commonLengths periodLengths

        if Array.isEmpty commonPeriodLengths then
            periodLengths
            |> Array.countBy id
            |> Array.sortBy snd
            |> Array.averageBy (snd >> decimal)
            |> roundMidpointTowardsZero
            |> int
        else
            commonPeriodLengths |> Array.sortBy snd |> Array.maxBy snd |> fst
        |> normalise
        |> snd

    /// number of unit-periods in a year
    let numberPerYear =
        function
        | Day -> 365m
        | Week multiple when multiple > 0 -> 52m / decimal multiple
        | SemiMonth -> 24m
        | Month multiple when multiple > 0 -> 12m / decimal multiple
        | _ -> 0m

    /// approximate length of unit period in days, used e.g. as the denominator in unit-period fractions
    let roughLength unitPeriod =
        match unitPeriod with
        | Day -> 1
        | Week multiple -> 7 * multiple
        | SemiMonth -> 15
        | Month multiple -> 30 * multiple

    /// unit period combined with a start date and multiple where appropriate
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type Config =
        /// daily starting on the given date
        | Daily of StartDate: Date
        /// (multi-)weekly: every n weeks starting on the given date
        | Weekly of WeekMultiple: int * WeekStartDate: Date
        /// semi-monthly: twice a month starting on the date given by year, month and day 1, with the other day given by day 2
        | SemiMonthly of smYear: int * smMonth: int * Day1: int * Day2: int
        /// (multi-)monthly: every n months starting on the date given by year, month and day, which tracks month-end (see config)
        | Monthly of MonthMultiple: int * Year: int * Month: int * Day: int

        /// HTML formatting to display the unit-period config in a readable format
        member upc.Html =
            let formatMonthEnd d =
                if d = 31 then "month-end" else d.ToString "00"

            match upc with
            | Daily sd -> $"daily from %A{sd}"
            | Weekly(multiple, wsd) when multiple = 1 -> $"weekly from %A{wsd}"
            | Weekly(multiple, wsd) -> $"{multiple}-weekly from %A{wsd}"
            | SemiMonthly(y, m, td1, td2) ->
                $"""semi-monthly from {y}-{m.ToString "00"} on {formatMonthEnd td1} and {formatMonthEnd td2}"""
            | Monthly(multiple, y, m, d) when multiple = 1 ->
                $"""monthly from {y}-{m.ToString "00"} on {formatMonthEnd d}"""
            | Monthly(multiple, y, m, d) -> $"""{multiple}-monthly from {y}-{m.ToString "00"} on {formatMonthEnd d}"""

    /// functions for creating and handling unit-period configs
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

            let monthEndDay2 =
                startDate.AddMonths monthOffset |> fun d -> Date.DaysInMonth(d.Year, d.Month)

            let trackingDay2 = if day1 >= 15 && day2 = monthEndDay2 then 31 else day2
            SemiMonthly(startDate.Year, startDate.Month, trackingDay1, trackingDay2)

        /// creates a monthly config, using month-end tracking where appropriate
        let defaultMonthly multiple (startDate: Date) =
            let monthEndDay = Date.DaysInMonth(startDate.Year, startDate.Month)
            let trackingDay = if startDate.Day = monthEndDay then 31 else startDate.Day
            Monthly(multiple, startDate.Year, startDate.Month, trackingDay)

        /// create a unit-period config from a unit period (using month-end tracking for semi-monthly and monthly unit periods)
        let from startDate unitPeriod =
            match unitPeriod with
            | Day -> Daily startDate
            | Week weekMultiple -> Weekly(weekMultiple, startDate)
            | SemiMonth -> defaultSemiMonthly startDate
            | Month monthMultiple -> defaultMonthly monthMultiple startDate

        /// get the underlying unit period from a unit-period config
        let unitPeriod unitPeriodConfig =
            match unitPeriodConfig with
            | Daily _ -> Day
            | Weekly(multiple, _) -> Week multiple
            | SemiMonthly _ -> SemiMonth
            | Monthly(multiple, _, _, _) -> Month multiple

        /// approximate length of unit period in days, used e.g. for generating rescheduling iterations
        let roughLength unitPeriodConfig =
            match unitPeriodConfig with
            | Daily _ -> 1m
            | Weekly(multiple, _) -> 7m * decimal multiple
            | SemiMonthly _ -> 15m
            | Monthly(multiple, _, _, _) -> 30m * decimal multiple

        /// gets the start date based on a unit-period config
        let startDate unitPeriodConfig =
            match unitPeriodConfig with
            | Daily sd -> sd
            | Weekly(_, wsd) -> wsd
            | SemiMonthly(y, m, d1, _) -> TrackingDay.toDate y m d1
            | Monthly(_, y, m, d) -> TrackingDay.toDate y m d

        /// if a month or day are out of range, constrain them to within range
        let internal fixDate year month day =
            let m =
                if month < 1 then 1
                elif month > 12 then 12
                else month

            let daysInMonth = Date.DaysInMonth(year, m)

            let d =
                if day < 1 then 1
                elif day > daysInMonth then daysInMonth
                else day

            Date(year, m, d)

        /// fixes an incorrect config by using a default configuration
        let internal fix unitPeriodConfig =
            match unitPeriodConfig with
            | Daily _
            | Weekly _ as c -> c
            | SemiMonthly(year, month, day1, _) -> fixDate year month day1 |> defaultSemiMonthly
            | Monthly(multiple, year, month, day) -> fixDate year month day |> defaultMonthly multiple

        /// constrains the freqencies to valid values
        let constrain unitPeriodConfig =
            match unitPeriodConfig with
            | Daily _
            | Weekly _ as c -> c
            | SemiMonthly(_, _, day1, day2) as c when
                day1 >= 1
                && day1 <= 15
                && day2 >= 16
                && day2 <= 31
                && (day2 < 31 && day2 - day1 = 15 || day2 = 31 && day1 = 15)
                ->
                c
            | SemiMonthly(_, _, day1, day2) as c when
                day2 >= 1
                && day2 <= 15
                && day1 >= 16
                && day1 <= 31
                && (day1 < 31 && day1 - day2 = 15 || day1 = 31 && day2 = 15)
                ->
                c
            | Monthly(_, _, _, day) as c when day >= 1 && day <= 31 -> c
            | invalidConfig -> fix invalidConfig

    /// defines the length of a payment schedule, either by the number of payments or by the maximum duration
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type ScheduleLength =
        | PaymentCount of Payments: int
        | MaxDuration of StartDate: Date * Days: int<OffsetDay>

        member sl.Html =
            match sl with
            | PaymentCount payments -> $"<i>payment count</i> {payments}"
            | MaxDuration(_, days) -> $"<i>max duration</i> {days}"

    /// direction in which to generate the schedule: forward works forwards from a given date and reverse works backwards
    [<Struct; RequireQualifiedAccess>]
    type Direction =
        /// create a schedule starting on the given date
        | Forward
        /// create a schedule ending on the given date
        | Reverse

    /// generate a payment schedule based on a unit-period config
    let generatePaymentSchedule scheduleLength direction unitPeriodConfig =
        let adjustMonthEnd monthEndTrackingDay (date: Date) =
            if date.Day > 15 && monthEndTrackingDay > 28 then
                TrackingDay.toDate date.Year date.Month monthEndTrackingDay
            else
                date

        let initCount = 0

        match unitPeriodConfig |> Config.constrain with
        | Daily firstPaymentDate ->
            match scheduleLength with
            | PaymentCount count ->
                match direction with
                | Direction.Forward -> Array.init count (fun i -> firstPaymentDate.AddDays i)
                | Direction.Reverse -> Array.init count (fun i -> firstPaymentDate.AddDays -i)
            | MaxDuration(startDate, duration) ->
                match direction with
                | Direction.Forward ->
                    Array.unfold
                        (fun count ->
                            let nextDate = firstPaymentDate.AddDays count

                            if nextDate <= startDate.AddDays +(int duration) then
                                Some(nextDate, count + 1)
                            else
                                None
                        )
                        initCount
                | Direction.Reverse ->
                    Array.unfold
                        (fun count ->
                            let nextDate = firstPaymentDate.AddDays -count

                            if nextDate <= startDate.AddDays -(int duration) then
                                Some(nextDate, count - 1)
                            else
                                None
                        )
                        initCount
        | Weekly(multiple, firstPaymentDate) ->
            match scheduleLength with
            | PaymentCount count ->
                match direction with
                | Direction.Forward -> Array.init count (fun i -> firstPaymentDate.AddDays(i * 7 * multiple))
                | Direction.Reverse -> Array.init count (fun i -> firstPaymentDate.AddDays -(i * 7 * multiple))
            | MaxDuration(startDate, duration) ->
                match direction with
                | Direction.Forward ->
                    Array.unfold
                        (fun i ->
                            let nextDate = firstPaymentDate.AddDays +(i * 7 * multiple)

                            if nextDate <= startDate.AddDays +(int duration) then
                                Some(nextDate, i + 1)
                            else
                                None
                        )
                        initCount
                | Direction.Reverse ->
                    Array.unfold
                        (fun i ->
                            let nextDate = firstPaymentDate.AddDays -(i * 7 * multiple)

                            if nextDate <= startDate.AddDays -(int duration) then
                                Some(nextDate, i - 1)
                            else
                                None
                        )
                        initCount
        | SemiMonthly(year, month, td1, td2) ->
            let firstPaymentDate = TrackingDay.toDate year month td1

            let offset, monthEndTrackingDay =
                if td1 > td2 then 1, td1 else 0, td2
                |> fun (o, metd) ->
                    match direction with
                    | Direction.Forward -> o, metd
                    | Direction.Reverse -> o - 1, metd

            match scheduleLength with
            | PaymentCount count ->
                match direction with
                | Direction.Forward -> [| 0 .. (count - 1) |]
                | Direction.Reverse -> [| 0 .. -1 .. -(count - 1) |]
                |> Array.collect (fun c -> [|
                    firstPaymentDate.AddMonths c |> adjustMonthEnd monthEndTrackingDay

                    firstPaymentDate.AddMonths(c + offset)
                    |> fun d -> TrackingDay.toDate d.Year d.Month td2
                    |> adjustMonthEnd monthEndTrackingDay
                |])
                |> Array.take count

            | MaxDuration(startDate, duration) ->
                Array.unfold
                    (fun count ->
                        let nextDate1 =
                            firstPaymentDate.AddMonths count |> adjustMonthEnd monthEndTrackingDay

                        let nextDate2 =
                            firstPaymentDate.AddMonths(count + offset)
                            |> fun d -> TrackingDay.toDate d.Year d.Month td2
                            |> adjustMonthEnd monthEndTrackingDay

                        match direction with
                        | Direction.Forward ->
                            let finalPaymentDate = startDate.AddDays(int duration)

                            let output = [|
                                if nextDate1 <= finalPaymentDate then
                                    yield nextDate1
                                if nextDate2 <= finalPaymentDate then
                                    yield nextDate2
                            |]

                            if output.Length > 0 then Some(output, count + 1) else None
                        | Direction.Reverse ->
                            let finalPaymentDate = startDate.AddDays -(int duration)

                            let output = [|
                                if nextDate1 >= finalPaymentDate then
                                    yield nextDate1
                                if nextDate2 >= finalPaymentDate then
                                    yield nextDate2
                            |]

                            if output.Length > 0 then Some(output, count - 1) else None
                    )
                    initCount
                |> Array.collect id
        | Monthly(multiple, year, month, trackingDay) ->
            let firstPaymentDate = TrackingDay.toDate year month trackingDay

            match scheduleLength with
            | PaymentCount c ->
                match direction with
                | Direction.Forward ->
                    Array.init c (fun i -> firstPaymentDate.AddMonths +(i * multiple) |> adjustMonthEnd trackingDay)
                | Direction.Reverse ->
                    Array.init c (fun i -> firstPaymentDate.AddMonths -(i * multiple) |> adjustMonthEnd trackingDay)
            | MaxDuration(startDate, duration) ->
                match direction with
                | Direction.Forward ->
                    Array.unfold
                        (fun count ->
                            let nextDate =
                                firstPaymentDate.AddMonths +(count * multiple) |> adjustMonthEnd trackingDay

                            if nextDate <= startDate.AddDays +(int duration) then
                                Some(nextDate, count + 1)
                            else
                                None
                        )
                        initCount
                | Direction.Reverse ->
                    Array.unfold
                        (fun count ->
                            let nextDate =
                                firstPaymentDate.AddMonths -(count * multiple) |> adjustMonthEnd trackingDay

                            if nextDate <= startDate.AddDays -(int duration) then
                                Some(nextDate, count - 1)
                            else
                                None
                        )
                        initCount
        |> Array.sort

    /// for a given interval and array of dates, devise the unit-period config
    let detect direction interval transferDates =
        if Array.isEmpty transferDates then
            failwith "No transfer dates given"
        else
            let transform =
                match direction with
                | Direction.Forward -> id
                | Direction.Reverse -> Array.rev

            let transferDates = transform transferDates
            let firstTransferDate = Array.head transferDates

            match interval with
            | Day -> Daily firstTransferDate
            | Week multiple -> Weekly(multiple, firstTransferDate)
            | SemiMonth ->
                if Array.length transferDates % 2 = 1 then // deal with odd numbers of transfer dates
                    transferDates
                    |> fun a ->
                        if Array.isEmpty a then
                            None
                        else
                            a |> Array.rev |> Array.tail |> Array.tryHead //last but one
                    |> Option.map (fun a -> [| transferDates; [| a |] |] |> Array.concat)
                    |> Option.defaultValue transferDates
                else
                    transferDates
                |> Array.chunkBySize 2
                |> Array.transpose
                |> Array.map (Array.maxBy _.Day >> _.Day)
                |> fun days -> SemiMonthly(firstTransferDate.Year, firstTransferDate.Month, days[0], days[1])
            | Month multiple ->
                transferDates
                |> Array.maxBy _.Day
                |> _.Day
                |> fun day -> Monthly(multiple, firstTransferDate.Year, firstTransferDate.Month, day)
