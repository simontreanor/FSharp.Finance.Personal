namespace FSharp.Finance.Personal

module PaymentMap =

    open Currency
    open CustomerPayments
    open DateDay

    [<RequireQualifiedAccess; Struct>]
    type Payment = {
        Day: int<OffsetDay>
        Amount: int64<Cent>
    }

    [<Struct>]
    type PaymentMap = {
        PaymentDueId: int
        PaymentMadeId: int voption
        Amount: int64<Cent>
        VarianceDays: int<DurationDay>
        DueBalance: int64<Cent>
        PaidBalance: int64<Cent>
    }

    let create asOfDate startDate (scheduledPayments: Payment array) (actualPayments: Payment array) =

        let asOfDay = asOfDate |> OffsetDay.fromDate startDate

        let paymentsDue =
            scheduledPayments
            |> Array.filter(fun sp -> sp.Day <= asOfDay)
            |> Array.mapi(fun i sp -> i, sp)
            |> Map.ofArray

        let paymentsMade =
            actualPayments
            |> Array.filter(fun ap -> ap.Day <= asOfDay && ap.Amount <> 0L<Cent>)
            |> Array.mapi(fun i ap -> i, ap)
            |> Map.ofArray

        let count m = if Map.isEmpty m then 0 else Map.count m

        let paymentsDueCount = count paymentsDue
        let paymentsMadeCount = count paymentsMade

        let paymentMap =
            {| due = 0; made = 0; dbal = 0L<Cent>; mbal = 0L<Cent> |} // bal = negative: too little paid, positive: too much paid
            |> Array.unfold(fun a ->
                if paymentsDueCount = 0 then
                    None
                elif a.due = paymentsDueCount (* && a.made = paymentsMadeCount *) then
                    None
                else
                    let paymentDue = paymentsDue[a.due]

                    let paymentMadeDay, paymentMadeAmount =
                        if a.made >= paymentsMadeCount then
                            ValueNone, 0L<Cent>
                        else
                            paymentsMade[a.made] |> fun pm -> ValueSome pm.Day, pm.Amount

                    let pd = if a.dbal = 0L<Cent> then paymentDue.Amount else Cent.min a.dbal paymentDue.Amount
                    let pm = if a.mbal = 0L<Cent> then paymentMadeAmount else Cent.min a.mbal paymentMadeAmount

                    let dueBalance = if pm = 0L<Cent> then 0L<Cent> elif pd > pm then pd - pm else 0L<Cent>
                    let madeBalance = if pm > pd then pm - pd else 0L<Cent>

                    let nextDue = if dueBalance = 0L<Cent> || pm = 0L<Cent> then a.due + 1 else a.due
                    let nextMade = if madeBalance = 0L<Cent> then min paymentsMadeCount (a.made + 1) else a.made

                    let paymentMadeId, amount =
                        if paymentMadeAmount = 0L<Cent> || a.made = paymentsMadeCount then
                            ValueNone, pd
                        else
                            ValueSome a.made, Cent.min pd pm

                    let varianceDays = int ((paymentMadeDay |> ValueOption.defaultValue asOfDay) - paymentDue.Day) * 1<DurationDay>

                    Some ({
                        PaymentDueId = a.due
                        PaymentMadeId = paymentMadeId
                        Amount = amount
                        VarianceDays = varianceDays
                        DueBalance = dueBalance
                        PaidBalance = madeBalance
                    }, {| due = nextDue; made = nextMade; dbal = dueBalance; mbal = madeBalance |})
            )
            
        paymentMap
