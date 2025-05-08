namespace FSharp.Finance.Personal

open System

/// a .NET Framework polyfill equivalent to the DateOnly structure in .NET Core
module DateDay =

    /// the date at the customer's location - ensure any time-zone conversion is performed before using this - as all calculations are date-only with no time component, summer time or other such time artefacts
    [<Struct; StructuredFormatDisplay("{Html}")>]
    type Date =
        val Year: int
        val Month: int
        val Day: int
        new (year: int, month: int, day: int) = { Year = year; Month = month; Day = day }
        with
            static member internal FromDateTime (dt: DateTime) = Date(dt.Year, dt.Month, dt.Day)
            member d.ToDateTime() = DateTime(d.Year, d.Month, d.Day)
            member d.AddDays (i: int) = DateTime(d.Year, d.Month, d.Day).AddDays(float i) |> Date.FromDateTime
            member d.AddMonths i = DateTime(d.Year, d.Month, d.Day).AddMonths i |> Date.FromDateTime
            member d.AddYears i = DateTime(d.Year, d.Month, d.Day).AddYears i |> Date.FromDateTime
            member d.Html = d.ToDateTime().ToString "yyyy-MM-dd"
            static member FromIsoString (ds: string) = Date(ds[0..3] |> Convert.ToInt32, ds[5..6] |> Convert.ToInt32, ds[8..9] |> Convert.ToInt32)
            static member (-) (d1: Date, d2: Date) =  d1.ToDateTime() - d2.ToDateTime()
            static member DaysInMonth(year, month) = DateTime.DaysInMonth(year, month)

    /// the offset of a date from the start date, in days
    [<Measure>]
    type OffsetDay

    /// functions for converting offset days to and from dates
    [<RequireQualifiedAccess>]
    module OffsetDay =
        /// convert an offset date to an offset day based on a given start date
        let fromDate (startDate: Date) (offsetDate: Date) = (offsetDate - startDate).Days * 1<OffsetDay>
        /// convert an offset day to an offset date based on a given start date
        let toDate (startDate: Date) (offsetDay: int<OffsetDay>) = startDate.AddDays(int offsetDay)


    /// a duration of a number of days
    [<Measure>] type DurationDay

    /// day of month, bug: specifying 29, 30, or 31 means the dates will track the specific day of the month where
    /// possible, otherwise the day will be the last day of the month; so 31 will track the month end; also note that it is
    /// possible to start with e.g. (2024, 02, 31) and this will yield 2024-02-29 29 2024-03-31 2024-04-30 etc.
    [<RequireQualifiedAccess>]
    module TrackingDay =
        /// create a date from a year, month, and tracking day
        let toDate y m td = Date(y, m, min (Date.DaysInMonth(y, m)) td)

#if DATEONLY
    /// wrapper for DateOnly support
    type DateOnly with
        member this.ToDate () =
            Date(this.Year, this.Month, this.Day)

    /// wrapper for DateOnly support
    module DateOnly =
        let fromDate (d:Date) =
            DateOnly(d.Year, d.Month, d.Day)
#endif
