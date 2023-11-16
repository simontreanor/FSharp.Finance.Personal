namespace FSharp.Finance

open System

[<AutoOpen>]
module General =
    
    /// a transaction term is the length of a transaction, from the start date to the final payment
    [<Struct>]
    type TransactionTerm = {
        Start: DateTime
        End: DateTime
        TotalDays: int
    }

    /// calculate the transaction term based on specific events
    let transactionTerm (consummationDate: DateTime) firstFinanceChargeEarnedDate (lastPaymentDueDate: DateTime) lastAdvanceScheduledDate =
        let beginDateTime = if firstFinanceChargeEarnedDate > consummationDate then firstFinanceChargeEarnedDate else consummationDate
        let endDateTime = if lastAdvanceScheduledDate > lastPaymentDueDate then lastAdvanceScheduledDate else lastPaymentDueDate
        { Start = beginDateTime; End = endDateTime; TotalDays = (endDateTime.Date - beginDateTime.Date).TotalDays |> int }
