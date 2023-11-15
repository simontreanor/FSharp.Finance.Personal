namespace FSharp.Finance

open System
open UnitPeriod

module Schedule =

    /// direction in which to generate the schedule: forward works forwards from a given date and reverse works backwards
    [<Struct>]
    type Direction =
        | Forward
        | Reverse

    /// generate a schedule based on a unit-period schedule
    let generate count direction unitPeriodSchedule =
        let adjustMonthEnd monthEndTrackingDay (dt: DateTime) =
            if dt.Day > 15 && monthEndTrackingDay > 28 then
                DateTime(dt.Year, dt.Month, Int32.Min(monthEndTrackingDay, DateTime.DaysInMonth(dt.Year, dt.Month)))
            else dt
        let generate =
            match unitPeriodSchedule |> constrain with
            | Single startDate ->
                Array.map (fun _ -> startDate)
            | Daily startDate ->
                Array.map (fun c -> startDate.AddDays (float c))
            | Weekly (multiple, startDate) ->
                Array.map (fun c -> startDate.AddDays (float(c * 7 * multiple)))
            | SemiMonthly (SemiMonthlyConfig (year, month, TrackingDay day1, TrackingDay day2)) ->
                let startDate = DateTime(year, month, Int32.Min(day1, DateTime.DaysInMonth(year, month)))
                let offset, monthEndTrackingDay = (if day1 > day2 then 1, day1 else 0, day2) |> fun (o, metd) -> (match direction with Forward -> o, metd | Reverse -> o - 1, metd)
                Array.collect(fun c -> [|
                    startDate.AddMonths c |> adjustMonthEnd monthEndTrackingDay
                    startDate.AddMonths (c + offset) |> fun dt -> DateTime(dt.Year, dt.Month, Int32.Min(day2, DateTime.DaysInMonth(dt.Year, dt.Month))) |> adjustMonthEnd monthEndTrackingDay
                |])
                >> Array.take count
            | Monthly (multiple, (MonthlyConfig (year, month, TrackingDay day))) ->
                let startDate = DateTime(year, month, Int32.Min(day, DateTime.DaysInMonth(year, month)))
                Array.map (fun c -> startDate.AddMonths (c * multiple) |> adjustMonthEnd day)
        match direction with
        | Forward -> [| 0 .. (count - 1) |] |> generate
        | Reverse -> [| 0 .. -1 .. -(count - 1) |] |> generate |> Array.sort

    /// for a given interval and array of dates, devise the unit-period schedule
    let detect direction interval (transferDates: DateTime array) =
        let transform = match direction with Forward -> id | Reverse -> Array.rev
        let transferDates = transform transferDates
        let firstTransferDate = transferDates |> Array.head
        match interval with
        | NoInterval _ -> Single firstTransferDate
        | Day -> Daily firstTransferDate
        | Week multiple -> Weekly (multiple, firstTransferDate)
        | SemiMonth ->
            transferDates
            |> Array.chunkBySize 2
            |> Array.transpose
            |> Array.map (Array.maxBy _.Day >> _.Day)
            |> fun days -> SemiMonthlyConfig(firstTransferDate.Year, firstTransferDate.Month, TrackingDay days[0], TrackingDay days[1])
            |> SemiMonthly
        | Month multiple ->
            transferDates
            |> Array.maxBy _.Day
            |> _.Day
            |> fun day -> multiple, MonthlyConfig(firstTransferDate.Year, firstTransferDate.Month, TrackingDay day)
            |> Monthly
