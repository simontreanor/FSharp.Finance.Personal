namespace FSharp.Finance.Personal

open System

[<AutoOpen>]
module Date =

    /// the offset of a date from the start date, in days
    [<Measure>]
    type OffsetDay

    [<RequireQualifiedAccess>]
    module OffsetDay =
        /// convert an offset date to an offset day based on a given start date
        let fromDate (startDate: DateTime) (offsetDate: DateTime) = (offsetDate - startDate).Days * 1<OffsetDay>
        /// convert an offset day to an offset date based on a given start date
        let toDate (startDate: DateTime) (offsetDay: int<OffsetDay>) = startDate.AddDays(float offsetDay)


    /// a duration of a number of days
    [<Measure>] type DurationDays

    /// day of month, bug: specifying 29, 30, or 31 means the dates will track the specific day of the month where
    /// possible, otherwise the day will be the last day of the month; so 31 will track the month end; also note that it is
    /// possible to start with e.g. (2024, 02, 31) and this will yield 2024-02-29 29 2024-03-31 2024-04-30 etc.
    [<RequireQualifiedAccess>]
    module TrackingDay =
        /// create a date from a year, month, and tracking day
        let toDate y m td = DateTime(y, m, min (DateTime.DaysInMonth(y, m)) td)
