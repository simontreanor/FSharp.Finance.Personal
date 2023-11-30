namespace FSharp.Finance.Personal

open System

/// functions for settling outstanding payments
module Settlement =

    open IrregularPayment

    type Quote = {
        SettlementFigure: int<Cent>
        SettlementStatement: Apportionment array
    }

    let getQuote (settlementDate: DateTime) (sp: RegularPayment.ScheduleParameters) (actualPayments: Payment array) =
        let extraPaymentAmount = sp.Principal * 10
        let extraPayment = {
            Day = int (settlementDate.Date - sp.StartDate.Date).Days * 1<Day>
            ScheduledPayment = 0<Cent>
            ActualPayments = [| extraPaymentAmount |]
            NetEffect = 0<Cent>
            PaymentStatus = ValueNone
            PenaltyCharges = [| |]
        }
        voption {
            let! appliedPayments = applyPayments sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| extraPayment |] |])
            let finalApportionment = appliedPayments |> Array.filter(fun a -> a.Date <= settlementDate) |> Array.last
            let actualPaymentAmounts = finalApportionment.ActualPayments |> Array.filter(fun p -> p <> extraPaymentAmount)
            let actualPaymentsTotal = actualPaymentAmounts |> Array.sum
            let settlementFigure = finalApportionment.NetEffect + finalApportionment.PrincipalBalance - actualPaymentsTotal
            let settlementPayment = { extraPayment with ActualPayments = [| settlementFigure |] }
            let! settlementStatement = applyPayments sp (ValueSome settlementDate) (Array.concat [| actualPayments; [| settlementPayment |] |])
            return { SettlementFigure = settlementFigure; SettlementStatement = settlementStatement }
        }
