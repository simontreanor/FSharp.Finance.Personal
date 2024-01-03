namespace FSharp.Finance.Personal

open System
open UnitPeriod

module Schedule =

    /// direction in which to generate the schedule: forward works forwards from a given date and reverse works backwards
    [<Struct>]
    type Direction =
        | Forward
        | Reverse

    /// generate a schedule based on a unit-period schedule
    let generate count direction unitPeriodConfig =
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
                let offset, monthEndTrackingDay = (if td1 > td2 then 1, td1 else 0, td2) |> fun (o, metd) -> (match direction with Forward -> o, metd | Reverse -> o - 1, metd)
                Array.collect(fun c -> [|
                    startDate.AddMonths c |> adjustMonthEnd monthEndTrackingDay
                    startDate.AddMonths (c + offset) |> fun dt -> TrackingDay.toDate dt.Year dt.Month td2 |> adjustMonthEnd monthEndTrackingDay
                |])
                >> Array.take count
            | Monthly (multiple, year, month, td) ->
                let startDate = TrackingDay.toDate year month td
                Array.map (fun c -> startDate.AddMonths (c * multiple) |> adjustMonthEnd td)
        match direction with
        | Forward -> [| 0 .. (count - 1) |] |> generate
        | Reverse -> [| 0 .. -1 .. -(count - 1) |] |> generate |> Array.sort

    /// for a given interval and array of dates, devise the unit-period schedule
    let detect direction interval (transferDates: DateTime array) =
        if Array.isEmpty transferDates then Single (DateTime.Today.AddYears 1) else
        let transform = match direction with Forward -> id | Reverse -> Array.rev
        let transferDates = transform transferDates
        let firstTransferDate = transferDates |> Array.head
        match interval with
        | NoInterval _ -> Single firstTransferDate
        | Day -> Daily firstTransferDate
        | Week multiple -> Weekly (multiple, firstTransferDate)
        | SemiMonth ->
            if Array.length transferDates % 2 = 1 then // deals with odd numbers of transfer dates
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
