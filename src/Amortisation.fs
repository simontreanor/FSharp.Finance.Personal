namespace FSharp.Finance

open System

module Amortisation =

    [<Struct>]
    type PenaltyCharge =
        | LatePayment of LatePayment:decimal
        | InsufficientFunds of InsufficientFunds:decimal

    /// a payment (a list of these is used to create an amortisation schedule)
    [<Struct>]
    type Payment =
        {
            Index: int
            Date: DateTime
            Day: int
            Amount: decimal
            Interval: int
            PenaltyCharges: PenaltyCharge array option
        }
        with member x.PenaltyChargesTotal = x.PenaltyCharges |> Option.map (Array.sumBy(function LatePayment m | InsufficientFunds m -> m)) |> Option.defaultValue 0m
 
    /// detail of a loan repayment with apportionment of a repayment to principal, fees, interest balances and charges
    [<Struct>]
    type ScheduleItem =
        {
            Date: DateTime
            Day: int
            Advance: decimal
            Payments: decimal array //one or more payments can be made on the same day
            NewInterest: decimal
            NewPenaltyCharges: decimal
            PrincipalPortion: decimal
            ProductFeesPortion: decimal
            InterestPortion: decimal
            PenaltyChargesPortion: decimal
            ProductFeesRefund: decimal
            PrincipalBalance: decimal
            ProductFeesBalance: decimal
            InterestBalance: decimal
            PenaltyChargesBalance: decimal
        }
        with member x.PaymentsTotal = x.Payments |> Array.sum

    [<Struct>]
    type ProductFees =
        | Percentage of Percentage:decimal * Cap:decimal option
        | Simple of Simple:decimal

    [<Struct>]
    type Parameters =
        {
            Principal: decimal
            ProductFees: ProductFees
            AnnualInterestRate: decimal
            StartDate: DateTime
            UnitPeriodConfig: UnitPeriod.Config
            PaymentCount: int
        }
        with
            static member calculateProductFees principal productFees =
                match productFees with
                | Percentage (percentage, Some cap) ->
                    Math.Min(Math.Floor(principal * percentage * 100m) / 100m, cap)
                | Percentage (percentage, None) ->
                    Math.Floor(principal * percentage * 100m) / 100m
                | Simple simple -> simple
            member x.ProductFeesTotal = Parameters.calculateProductFees x.Principal x.ProductFees
            member x.ProductFeesPercentage = x.ProductFeesTotal / x.Principal
            member x.DailyInterestRate = x.AnnualInterestRate / 365m
            member x.InitialBalance = x.Principal + x.ProductFeesTotal

    [<Struct>]
    type IntermediateResult =
        {
            Parameters: Parameters
            RoughPayments: Payment array
            InterestTotal: decimal
        }
        with
            member x.LoanTermDays = x.RoughPayments |> Array.maxBy(fun p -> p.Day) |> fun p -> p.Day
            member x.PaymentTotal = x.Parameters.Principal + x.Parameters.ProductFeesTotal + x.InterestTotal
            member x.SinglePayment = Math.Round(x.PaymentTotal / decimal x.Parameters.PaymentCount, 2)
            member x.FinalPayment = Math.Round(x.Parameters.InitialBalance + x.InterestTotal - (x.SinglePayment *  decimal (x.Parameters.PaymentCount - 1)), 2)

    [<Struct>]
    type Schedule =
        {
            Parameters: Parameters
            IntermediateResult: IntermediateResult
            Items: ScheduleItem array
        }
        with
            member x.Apr =
                let precision = 8
                let advanceAmount = x.Parameters.Principal + x.Parameters.ProductFeesTotal
                let advanceDate = x.Parameters.StartDate
                let payments =
                    x.Items
                    |> Array.filter(fun asi -> asi.PaymentsTotal > 0m)
                    |> Array.map(fun asi -> { Apr.TransferType = Apr.Payment; Apr.Date = asi.Date; Apr.Amount = asi.PaymentsTotal })
                Apr.calculate precision advanceAmount advanceDate payments
            member x.ProductFeesRefund = x.Items |> Array.sumBy(fun asi -> asi.ProductFeesRefund)
            member x.EffectiveAnnualInterestRate = (x.InterestTotal / (x.PrincipalTotal + x.ProductFeesTotal - x.ProductFeesRefund)) * (365m / decimal x.IntermediateResult.LoanTermDays)
            member x.EffectiveDailyInterestRate = (x.InterestTotal / (x.PrincipalTotal + x.ProductFeesTotal - x.ProductFeesRefund)) * (1m / decimal x.IntermediateResult.LoanTermDays)
            member x.FinalRepaymentDate = x.Items |> Array.maxBy(fun asi -> asi.Date) |> fun asi -> asi.Date
            member x.RepaymentCount = x.Items |> Array.length
            member x.ProductFeesTotal = x.Items |> Array.sumBy(fun asi -> asi.ProductFeesPortion)
            member x.InterestTotal = x.Items |> Array.sumBy(fun asi -> asi.InterestPortion)
            member x.PenaltyChargesTotal = x.Items |> Array.sumBy(fun asi -> asi.PenaltyChargesPortion)
            member x.PrincipalTotal = x.Items |> Array.sumBy(fun asi -> asi.PrincipalPortion)
            member x.AdvancesTotal = x.Items |> Array.sumBy(fun asi -> asi.Advance)
            member x.PaymentsTotal = x.Items |> Array.sumBy(fun asi -> asi.PaymentsTotal)

    /// calculate total interest due on a regular schedule
    let calculateInterestTotal (p: Parameters) (payments: Payment array) =
        let loanTermDays = payments |> Array.maxBy(fun r -> r.Day) |> fun r -> r.Day
        // note that inside this function, "principal" refers to (principal + product fees) together
        let rec calculate (p: Parameters) (payments: Payment array) day (principalBalance: decimal) interestBalance (accumulatedInterest: decimal) =
            if day > loanTermDays then
                if Math.Abs principalBalance < 0.00001m then
                    Math.Round(accumulatedInterest, 2)
                else
                    let payments' = payments |> Array.mapi(fun i r ->
                        { Index = i; Date = r.Date; Day = r.Day; Amount = r.Amount + (principalBalance / decimal p.PaymentCount); Interval = r.Interval; PenaltyCharges = r.PenaltyCharges }
                    )
                    calculate p payments' 1 (p.Principal + p.ProductFeesTotal) 0m 0m
            else
                let interest = principalBalance * p.DailyInterestRate
                let principalRepayment, interestRepayment =
                    payments
                    |> Array.tryPick(fun r -> if r.Day = day then Some r.Amount else None)
                    |> Option.map(fun r -> (interestBalance + interest) |> fun interestToPay -> Math.Max(0m, r - interestToPay), interestToPay)
                    |> Option.defaultValue (0m, 0m)
                calculate p payments (day + 1) (principalBalance - principalRepayment) (interestBalance + interest - interestRepayment) (accumulatedInterest + interest)
        calculate p payments 1 (p.Principal + p.ProductFeesTotal) 0m 0m

    /// calculate amortisation schedule detailing how loan and its constituent elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (parameters: Parameters) (ir: IntermediateResult) (payments: Payment array) (startDate: DateTime option) =
        let maxRepaymentDay = payments |> Array.maxBy(fun r -> r.Day) |> fun r -> r.Day
        payments
        |> Array.scan(fun asi p ->
            let newPenaltyCharges = p.PenaltyChargesTotal
            let penaltyChargesPortion = newPenaltyCharges + asi.PenaltyChargesBalance

            let newInterest = Math.Round((asi.PrincipalBalance + asi.ProductFeesBalance) * parameters.DailyInterestRate * decimal p.Interval, 2)
            let interestPortion = newInterest + asi.InterestBalance
            
            let productFeesDue = Math.Min(parameters.ProductFeesTotal, Math.Round(parameters.ProductFeesTotal * decimal p.Day / decimal ir.LoanTermDays, 2))
            let productFeesRemaining = parameters.ProductFeesTotal - productFeesDue
            let settlementFigure = asi.PrincipalBalance + asi.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = (p.Amount > settlementFigure && p.Day <> ir.LoanTermDays) || (p.Day = ir.LoanTermDays && p.Day = maxRepaymentDay)

            let actualRepayment =
                if asi.PrincipalBalance = 0m then
                    0m
                elif isSettlement then
                    settlementFigure
                else
                    p.Amount

            let principalPortion, cabFeePortion, productFeesRefund =
                if (penaltyChargesPortion > actualRepayment) || (penaltyChargesPortion + interestPortion > actualRepayment) then
                    0m, 0m, 0m
                else
                    if isSettlement then
                        let cabFeePayment = asi.ProductFeesBalance - productFeesRemaining
                        actualRepayment - penaltyChargesPortion - interestPortion - cabFeePayment, cabFeePayment, productFeesRemaining
                    else
                        let principalPayment = Math.Round((actualRepayment - penaltyChargesPortion - interestPortion) / (1m + parameters.ProductFeesPercentage), 2)
                        principalPayment, actualRepayment - penaltyChargesPortion - interestPortion - principalPayment, 0m
                        
            let principalPortion = Math.Min(asi.PrincipalBalance, principalPortion)
            let productFeesPortion = Math.Min(asi.ProductFeesBalance, cabFeePortion)

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualRepayment then
                    penaltyChargesPortion - actualRepayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualRepayment then
                    0m, interestPortion - (actualRepayment - penaltyChargesPortion)
                else
                    0m, 0m

            {
                Date = startDate |> Option.map(fun dt -> dt.AddDays (float p.Day)) |> Option.defaultValue p.Date
                Day = p.Day
                Advance = 0m
                Payments = [| actualRepayment |]
                NewInterest = newInterest
                NewPenaltyCharges = newPenaltyCharges
                PrincipalPortion = principalPortion
                ProductFeesPortion = productFeesPortion
                InterestPortion = interestPortion - carriedInterest
                PenaltyChargesPortion = penaltyChargesPortion - carriedPenaltyCharges
                ProductFeesRefund = productFeesRefund
                PrincipalBalance = asi.PrincipalBalance - principalPortion
                ProductFeesBalance = asi.ProductFeesBalance - productFeesPortion - productFeesRefund
                InterestBalance = asi.InterestBalance + newInterest - interestPortion + carriedInterest
                PenaltyChargesBalance = asi.PenaltyChargesBalance + newPenaltyCharges - penaltyChargesPortion + carriedPenaltyCharges
            }
        ) { Date = (startDate |> Option.defaultValue DateTime.Today); Day = 0; Advance = parameters.Principal + parameters.ProductFeesTotal; Payments = [||]; NewInterest = 0m; NewPenaltyCharges = 0m; PrincipalPortion = 0m; ProductFeesPortion = 0m; InterestPortion = 0m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance = parameters.Principal; ProductFeesBalance = parameters.ProductFeesTotal; InterestBalance = 0m; PenaltyChargesBalance = 0m }

    let createRegularSchedule parameters =
        let paymentDates = Schedule.generate parameters.PaymentCount Schedule.Forward parameters.UnitPeriodConfig
        let termLength = ((paymentDates |> Array.last).Date - parameters.StartDate.Date).TotalDays
        let roughPayment = Math.Round((parameters.Principal + parameters.ProductFeesTotal) * (1m + (parameters.AnnualInterestRate * decimal termLength / 365m)) / decimal parameters.PaymentCount, 2)
        let roughPayments =
            paymentDates
            |> Array.mapi(fun i dt -> {
                Index = i
                Date = dt
                Day = (dt.Date - parameters.StartDate.Date).TotalDays |> int
                Amount = roughPayment
                Interval = (dt.Date - (if i = 0 then parameters.StartDate else paymentDates[i - 1]).Date).TotalDays |> int
                PenaltyCharges = None
            })
        let finalPaymentIndex = paymentDates |> Array.length |> (+) -1
        let intermediateResult = {
            Parameters = parameters
            RoughPayments = roughPayments
            InterestTotal = calculateInterestTotal parameters roughPayments
        }
        let payments =
            roughPayments
            |> Array.mapi(fun i p -> { p with Amount = if finalPaymentIndex = i then intermediateResult.FinalPayment else intermediateResult.SinglePayment })
        {
            Parameters = parameters
            IntermediateResult = intermediateResult
            Items = calculateSchedule parameters intermediateResult payments (Some parameters.StartDate)
        }

    let create principal productFees annualInterestRate startDate unitPeriodConfig paymentCount =
        { Principal = principal; ProductFees = productFees; AnnualInterestRate = annualInterestRate; StartDate = startDate; UnitPeriodConfig = unitPeriodConfig; PaymentCount = paymentCount }
        |> createRegularSchedule
