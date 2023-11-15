namespace FSharp.Finance

open System

module Amortisation =

    [<Struct>]
    type PenaltyCharge =
        | LatePayment of LatePayment:decimal
        | InsufficientFunds of InsufficientFunds:decimal

    /// a payment (a list of these is used to create an amortisation schedule)
    [<Struct>]
    type Payment = {
        Index: int
        Date: DateTime
        Day: int
        Amount: decimal
        Interval: int
        PenaltyCharges: PenaltyCharge array voption
    }

    [<Struct>]
    type PaymentInfo = {
        Payment: Payment
        PenaltyChargesTotal: decimal
    }

    let paymentInfo p = {
        Payment = p
        PenaltyChargesTotal = p.PenaltyCharges |> ValueOption.map (Array.sumBy(function LatePayment m | InsufficientFunds m -> m)) |> ValueOption.defaultValue 0m
    }
 
    /// detail of a loan repayment with apportionment of a repayment to principal, product fees, interest balances and penalty charges
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

    [<Struct>]
    type ScheduleItemInfo = {
        ScheduleItem: ScheduleItem
        PaymentsTotal: decimal
    }

    let scheduleItemInfo si= {
        ScheduleItem = si
        PaymentsTotal = si.Payments |> Array.sum
    }

    [<Struct>]
    type ProductFees =
        | Percentage of Percentage:decimal * Cap:decimal voption
        | Simple of Simple:decimal

    [<Struct>]
    type Parameters = {
        Principal: decimal
        ProductFees: ProductFees
        AnnualInterestRate: decimal
        StartDate: DateTime
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
    }

    [<Struct>]
    type ParametersInfo = {
        Parameters: Parameters
        ProductFeesTotal: decimal
        ProductFeesPercentage: decimal
        DailyInterestRate: decimal
        InitialBalance: decimal
    }

    let parametersInfo p = 
        let calculateProductFees principal productFees =
            match productFees with
            | Percentage (percentage, ValueSome cap) ->
                Decimal.Min(Decimal.Floor(principal * percentage * 100m) / 100m, cap)
            | Percentage (percentage, ValueNone) ->
                Decimal.Floor(principal * percentage * 100m) / 100m
            | Simple simple -> simple
        let productFeesTotal = calculateProductFees p.Principal p.ProductFees
        {
            Parameters = p
            ProductFeesTotal = productFeesTotal
            ProductFeesPercentage = productFeesTotal / p.Principal
            DailyInterestRate = p.AnnualInterestRate / 365m
            InitialBalance = p.Principal + productFeesTotal
        }

    [<Struct>]
    type IntermediateResult = {
        Parameters: Parameters
        RoughPayments: Payment array
        InterestTotal: decimal
        PenaltyChargesTotal: decimal
    }

    [<Struct>]
    type IntermediateResultInfo = {
        IntermediateResult: IntermediateResult
        LoanTermDays: int
        PaymentTotal: decimal
        SinglePayment: decimal
        FinalPayment: decimal
    }

    let intermediateResultInfo ir =
        let pi = parametersInfo ir.Parameters
        let paymentTotal = ir.Parameters.Principal + pi.ProductFeesTotal + ir.InterestTotal + ir.PenaltyChargesTotal
        let singlePayment = paymentTotal / decimal ir.Parameters.PaymentCount |> roundMoney
        {
            IntermediateResult = ir
            LoanTermDays = ir.RoughPayments |> Array.maxBy(fun p -> p.Day) |> fun p -> p.Day
            PaymentTotal = paymentTotal
            SinglePayment = singlePayment
            FinalPayment = pi.InitialBalance + ir.InterestTotal - (singlePayment *  decimal (ir.Parameters.PaymentCount - 1)) |> roundMoney
        }

    [<Struct>]
    type Schedule =
        {
            Parameters: Parameters
            IntermediateResult: IntermediateResult
            Items: ScheduleItem array
        }

    [<Struct>]
    type ScheduleInfo =
        {
            Schedule: Schedule
            AdvancesTotal: decimal
            PaymentsTotal: decimal
            PrincipalTotal: decimal
            ProductFeesTotal: decimal
            InterestTotal: decimal
            PenaltyChargesTotal: decimal
            ProductFeesRefund: decimal
            FinalPaymentDate: DateTime
            FinalPaymentCount: int
            Apr: decimal
            EffectiveAnnualInterestRate: decimal
            EffectiveDailyInterestRate: decimal
        }

    let scheduleInfo s =
        let iri = intermediateResultInfo s.IntermediateResult
        let paymentsTotal si = si |> scheduleItemInfo |> fun sii -> sii.PaymentsTotal
        let interestTotal = s.Items |> Array.sumBy(fun asi -> asi.InterestPortion)
        let principalTotal = s.Items |> Array.sumBy(fun asi -> asi.PrincipalPortion)
        let productFeesTotal = s.Items |> Array.sumBy(fun asi -> asi.ProductFeesPortion)
        let penaltyChargesTotal = s.Items |> Array.sumBy(fun asi -> asi.PenaltyChargesPortion)
        let productFeesRefund = s.Items |> Array.sumBy(fun asi -> asi.ProductFeesRefund)
        let apr =
            let advanceAmount = s.Parameters.Principal
            let advanceDate = s.Parameters.StartDate
            let payments =
                s.Items
                |> Array.filter(fun si -> paymentsTotal si > 0m)
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.Date = si.Date; Apr.Amount = paymentsTotal si })
            Apr.calculate Apr.UsActuarial 8 advanceAmount advanceDate payments
        {
            Schedule = s
            AdvancesTotal = s.Items |> Array.sumBy(fun asi -> asi.Advance)
            PaymentsTotal = s.Items |> Array.sumBy(fun si -> paymentsTotal si)
            PrincipalTotal = principalTotal
            ProductFeesTotal = productFeesTotal
            InterestTotal = interestTotal
            ProductFeesRefund = productFeesRefund
            PenaltyChargesTotal = penaltyChargesTotal
            Apr = apr
            EffectiveAnnualInterestRate = (interestTotal / (principalTotal + productFeesTotal - productFeesRefund)) * (365m / decimal iri.LoanTermDays)
            EffectiveDailyInterestRate = (interestTotal / (principalTotal + productFeesTotal - productFeesRefund)) * (1m / decimal iri.LoanTermDays)
            FinalPaymentDate = s.Items |> Array.maxBy(fun asi -> asi.Date) |> fun asi -> asi.Date
            FinalPaymentCount = s.Items |> Array.length |> (+) -1
        }

    /// calculate total interest due on a regular schedule
    let calculateInterestTotal (p: Parameters) (payments: Payment array) =
        let loanTermDays = payments |> Array.maxBy(fun r -> r.Day) |> fun r -> r.Day
        // note that inside this function, "principal" refers to (principal + product fees) together
        let rec calculate (p: Parameters) (payments: Payment array) day (principalBalance: decimal) interestBalance (accumulatedInterest: decimal) =
            let pi = parametersInfo p
            if day > loanTermDays then
                if Decimal.Abs principalBalance < 0.00001m then
                    accumulatedInterest |> roundMoney
                else
                    let payments' = payments |> Array.mapi(fun i r ->
                        { Index = i; Date = r.Date; Day = r.Day; Amount = r.Amount + (principalBalance / decimal p.PaymentCount); Interval = r.Interval; PenaltyCharges = r.PenaltyCharges }
                    )
                    calculate p payments' 1 (p.Principal + pi.ProductFeesTotal) 0m 0m
            else
                let interest = principalBalance * pi.DailyInterestRate
                let principalRepayment, interestRepayment =
                    payments
                    |> Array.tryPick(fun r -> if r.Day = day then Some r.Amount else None)
                    |> Option.map(fun r -> (interestBalance + interest) |> fun interestToPay -> Decimal.Max(0m, r - interestToPay), interestToPay)
                    |> Option.defaultValue (0m, 0m)
                calculate p payments (day + 1) (principalBalance - principalRepayment) (interestBalance + interest - interestRepayment) (accumulatedInterest + interest)
        calculate p payments 1 (p.Principal + (parametersInfo p).ProductFeesTotal) 0m 0m

    /// calculate amortisation schedule detailing how loan and its constituent elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (p: Parameters) (ir: IntermediateResult) (payments: Payment array) (startDate: DateTime voption) =
        let pi = parametersInfo p
        let iri = intermediateResultInfo ir
        let maxRepaymentDay = payments |> Array.maxBy(fun r -> r.Day) |> fun r -> r.Day
        let advance = {
            Date = (startDate |> ValueOption.defaultValue DateTime.Today)
            Day = 0
            Advance = p.Principal |> roundMoney
            Payments = [||]
            NewInterest = 0m |> roundMoney
            NewPenaltyCharges = 0m |> roundMoney
            PrincipalPortion = 0m |> roundMoney
            ProductFeesPortion = 0m |> roundMoney
            InterestPortion = 0m |> roundMoney
            PenaltyChargesPortion = 0m |> roundMoney
            ProductFeesRefund = 0m |> roundMoney
            PrincipalBalance = p.Principal |> roundMoney
            ProductFeesBalance = pi.ProductFeesTotal |> roundMoney
            InterestBalance = 0m |> roundMoney
            PenaltyChargesBalance = 0m |> roundMoney
        }
        payments
        |> Array.scan(fun asi payment ->
            let newPenaltyCharges = (paymentInfo payment).PenaltyChargesTotal
            let penaltyChargesPortion = newPenaltyCharges + asi.PenaltyChargesBalance

            let newInterest = (asi.PrincipalBalance + asi.ProductFeesBalance) * pi.DailyInterestRate * decimal payment.Interval |> roundMoney
            let interestPortion = newInterest + asi.InterestBalance
            
            let productFeesDue = Decimal.Min(pi.ProductFeesTotal, pi.ProductFeesTotal * decimal payment.Day / decimal iri.LoanTermDays |> roundMoney)
            let productFeesRemaining = pi.ProductFeesTotal - productFeesDue
            let settlementFigure = asi.PrincipalBalance + asi.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = (payment.Amount > settlementFigure && payment.Day <> iri.LoanTermDays) || (payment.Day = iri.LoanTermDays && payment.Day = maxRepaymentDay)

            let actualRepayment =
                if asi.PrincipalBalance = 0m then
                    0m
                elif isSettlement then
                    settlementFigure
                else
                    payment.Amount

            let principalPortion, cabFeePortion, productFeesRefund =
                if (penaltyChargesPortion > actualRepayment) || (penaltyChargesPortion + interestPortion > actualRepayment) then
                    0m, 0m, 0m
                else
                    if isSettlement then
                        let cabFeePayment = asi.ProductFeesBalance - productFeesRemaining |> roundMoney
                        actualRepayment - penaltyChargesPortion - interestPortion - cabFeePayment, cabFeePayment, productFeesRemaining
                    else
                        let principalPayment = (actualRepayment - penaltyChargesPortion - interestPortion) / (1m + pi.ProductFeesPercentage) |> roundMoney
                        principalPayment, actualRepayment - penaltyChargesPortion - interestPortion - principalPayment, 0m
                        
            let principalPortion = Decimal.Min(asi.PrincipalBalance, principalPortion)
            let productFeesPortion = Decimal.Min(asi.ProductFeesBalance, cabFeePortion)

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualRepayment then
                    penaltyChargesPortion - actualRepayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualRepayment then
                    0m, interestPortion - (actualRepayment - penaltyChargesPortion)
                else
                    0m, 0m

            {
                Date = startDate |> ValueOption.map(fun dt -> dt.AddDays (float payment.Day)) |> ValueOption.defaultValue payment.Date
                Day = payment.Day
                Advance = 0m |> roundMoney
                Payments = [| actualRepayment |> roundMoney |]
                NewInterest = newInterest |> roundMoney
                NewPenaltyCharges = newPenaltyCharges |> roundMoney
                PrincipalPortion = principalPortion |> roundMoney
                ProductFeesPortion = productFeesPortion |> roundMoney
                InterestPortion = interestPortion - carriedInterest |> roundMoney
                PenaltyChargesPortion = penaltyChargesPortion - carriedPenaltyCharges |> roundMoney
                ProductFeesRefund = productFeesRefund |> roundMoney
                PrincipalBalance = asi.PrincipalBalance - principalPortion |> roundMoney
                ProductFeesBalance = asi.ProductFeesBalance - productFeesPortion - productFeesRefund |> roundMoney
                InterestBalance = asi.InterestBalance + newInterest - interestPortion + carriedInterest |> roundMoney
                PenaltyChargesBalance = asi.PenaltyChargesBalance + newPenaltyCharges - penaltyChargesPortion + carriedPenaltyCharges |> roundMoney
            }
        ) advance

    [<Struct>]
    type Output =
        | UnitPeriodsOnly
        | IntersperseDays

    let createRegularScheduleInfo output p =
        let pi = parametersInfo p
        let paymentDates = Schedule.generate p.PaymentCount Schedule.Forward p.UnitPeriodConfig
        let termLength = ((paymentDates |> Array.last).Date - p.StartDate.Date).TotalDays

        let roughPayment = (p.Principal + pi.ProductFeesTotal) * (1m + (p.AnnualInterestRate * decimal termLength / 365m)) / decimal p.PaymentCount |> roundMoney
        let roughPayments =
            paymentDates
            |> Array.mapi(fun i dt -> {
                Index = i
                Date = dt
                Day = (dt.Date - p.StartDate.Date).TotalDays |> int
                Amount = roughPayment
                Interval = (dt.Date - (if i = 0 then p.StartDate else paymentDates[i - 1]).Date).TotalDays |> int
                PenaltyCharges = ValueNone
            })

        let zeroPayment i = { Index = i; Date = p.StartDate.AddDays(float i); Day = i; Amount = 0m; Interval = 1; PenaltyCharges = ValueNone }
        let maxRepaymentDay = roughPayments |> Array.maxBy(fun r -> r.Day) |> fun r -> r.Day
        let interspersedDays =
            match output with
            | UnitPeriodsOnly -> roughPayments
            | IntersperseDays ->
                [| 1 .. maxRepaymentDay |]
                |> Array.collect(fun i -> 
                    roughPayments
                    |> Array.filter(fun r -> r.Day = i)
                    |> fun pp ->
                        match pp with
                        | [||] -> [| zeroPayment i |]
                        | _ -> pp
                )

        let finalPaymentIndex = paymentDates |> Array.length |> (+) -1
        let intermediateResult = {
            Parameters = p
            RoughPayments = interspersedDays
            InterestTotal = calculateInterestTotal p interspersedDays
            PenaltyChargesTotal = 0m
        }
        let iri = intermediateResultInfo intermediateResult
        let payments =
            interspersedDays
            |> Array.mapi(fun i p -> { p with Amount = if finalPaymentIndex = i then iri.FinalPayment else iri.SinglePayment })
        {
            Parameters = p
            IntermediateResult = intermediateResult
            Items = calculateSchedule p intermediateResult payments (ValueSome p.StartDate)
        }
        |> scheduleInfo

// to-do: create version of schedule with daily rather than periodical items
