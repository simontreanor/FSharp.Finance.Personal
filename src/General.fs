namespace FSharp.Finance

open System

[<AutoOpen>]
module General =

    [<Struct>]
    type TransactionTerm = {
        Start: DateTime
        End: DateTime
        TotalDays: int
    }

    let transactionTerm (consummationDate: DateTime) firstFinanceChargeEarnedDate (lastPaymentDueDate: DateTime) lastAdvanceScheduledDate =
        let beginDateTime = if firstFinanceChargeEarnedDate > consummationDate then firstFinanceChargeEarnedDate else consummationDate
        let endDateTime = if lastAdvanceScheduledDate > lastPaymentDueDate then lastAdvanceScheduledDate else lastPaymentDueDate
        { Start = beginDateTime; End = endDateTime; TotalDays = (endDateTime.Date - beginDateTime.Date).TotalDays |> int }
