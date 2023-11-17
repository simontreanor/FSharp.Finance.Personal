namespace FSharp.Finance

open System

module Amortisation =

    /// the type and amount of penalty charge
    [<Struct>]
    type PenaltyCharge =
        | LatePayment of LatePayment:int<Cent>
        | InsufficientFunds of InsufficientFunds:int<Cent>

    /// a payment (a list of these is used to create an amortisation schedule)
    [<Struct>]
    type Payment = {
        Index: int
        Date: DateTime
        Day: int<Day>
        Amount: decimal //int<Cent> //decimal may be needed to calculate interest correctly
        Interval: int<Duration>
        PenaltyCharges: PenaltyCharge array voption
    }

    /// extra information on a payment
    [<Struct>]
    type PaymentInfo = {
        Payment: Payment
        PenaltyChargesTotal: int<Cent>
    }

    /// calculate extra information on a payment
    let paymentInfo p = {
        Payment = p
        PenaltyChargesTotal = p.PenaltyCharges |> ValueOption.map (Array.sumBy(function LatePayment m | InsufficientFunds m -> m)) |> ValueOption.defaultValue 0<Cent>
    }
 
    /// detail of a repayment with apportionment of a repayment to principal, product fees, interest and penalty charges
    [<Struct>]
    type ScheduleItem =
        {
            Date: DateTime
            Day: int<Day>
            Advance: int<Cent>
            Payments: int<Cent> array //one or more payments can be made on the same day
            NewInterest: int<Cent>
            NewPenaltyCharges: int<Cent>
            PrincipalPortion: int<Cent>
            ProductFeesPortion: int<Cent>
            InterestPortion: int<Cent>
            PenaltyChargesPortion: int<Cent>
            ProductFeesRefund: int<Cent>
            PrincipalBalance: int<Cent>
            ProductFeesBalance: int<Cent>
            InterestBalance: int<Cent>
            PenaltyChargesBalance: int<Cent>
        }

    /// extra information on a schedule item
    [<Struct>]
    type ScheduleItemInfo = {
        ScheduleItem: ScheduleItem
        PaymentsTotal: int<Cent>
    }

    /// calculate extra information on a schedule item
    let scheduleItemInfo si = {
        ScheduleItem = si
        PaymentsTotal = si.Payments |> Array.sum
    }

    /// the type and amount of any product fees, taking into account any constraints
    [<Struct>]
    type ProductFees =
        | Percentage of Percentage:decimal<Percent> * Cap:int<Cent> voption
        | Simple of Simple:int<Cent>

    /// options on how often interest is capitalised
    [<Struct>]
    type InterestCapitalisation =
        /// interest is capitalised at the beginning of the term
        | AtTermStart
        /// interest is capitalised at every payment date
        | OnPaymentDates
        /// interest is capitalised daily
        | EveryDay

    /// options on how often interest is capitalised
    [<Struct>]
    type Output =
        /// produce full daily itemised amortisation schedule
        | Full
        /// produce amortisation schedule summarised at each unit-period
        | Summary

    /// parameters for calculating an amortisation schedule
    [<Struct>]
    type Parameters = {
        Principal: int<Cent>
        ProductFees: ProductFees
        AnnualInterestRate: decimal<Percent>
        InterestCapitalisation: InterestCapitalisation
        StartDate: DateTime
        UnitPeriodConfig: UnitPeriod.Config
        PaymentCount: int
        Output: Output
    }

    /// extra information on parameters
    [<Struct>]
    type ParametersInfo = {
        Parameters: Parameters
        ProductFeesTotal: int<Cent>
        ProductFeesPercentage: decimal<Percent>
        DailyInterestRate: decimal<Percent>
        InitialBalance: int<Cent>
    }

    /// calculate extra information on parameters
    let parametersInfo p = 
        let calculateProductFees (principal: int<Cent>) productFees =
            match productFees with
            | Percentage (percentage, ValueSome cap) ->
                Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
                |> fun cents -> Cent.min cents cap
            | Percentage (percentage, ValueNone) ->
                Decimal.Floor(decimal principal * decimal percentage) / 100m |> fun m -> (int m * 1<Cent>)
            | Simple simple -> simple
        let productFeesTotal = calculateProductFees p.Principal p.ProductFees
        {
            Parameters = p
            ProductFeesTotal = productFeesTotal
            ProductFeesPercentage = decimal productFeesTotal / decimal p.Principal * 100m<Percent>
            DailyInterestRate = p.AnnualInterestRate / 365m
            InitialBalance = p.Principal + productFeesTotal
        }

    /// intermediate result used in calculating an amortisation schedule
    [<Struct>]
    type IntermediateResult = {
        Parameters: Parameters
        RoughPayments: Payment array
        InterestTotal: int<Cent>
        PenaltyChargesTotal: int<Cent>
    }

    /// extra information on intermedate result
    [<Struct>]
    type IntermediateResultInfo = {
        IntermediateResult: IntermediateResult
        MaxPaymentDay: int<Day>
        PaymentTotal: int<Cent>
        LevelPayment: int<Cent>
        FinalPayment: int<Cent>
    }

    /// calculate extra information on the intermedate result
    let intermediateResultInfo ir =
        let pi = parametersInfo ir.Parameters
        let paymentTotal = ir.Parameters.Principal + pi.ProductFeesTotal + ir.InterestTotal + ir.PenaltyChargesTotal
        let singlePayment = decimal paymentTotal / decimal ir.Parameters.PaymentCount |> Cent.round
        {
            IntermediateResult = ir
            MaxPaymentDay = ir.RoughPayments |> Array.maxBy _.Day |> _.Day
            PaymentTotal = paymentTotal
            LevelPayment = singlePayment
            FinalPayment = pi.InitialBalance + ir.InterestTotal - (singlePayment * (ir.Parameters.PaymentCount - 1))
        }

    /// an amortisation schedule, itemising how principal, product fees, interest and penalty charges are paid off
    [<Struct>]
    type Schedule =
        {
            Parameters: Parameters
            IntermediateResult: IntermediateResult
            Items: ScheduleItem array
        }

    /// extra information on the amortisation schedule
    [<Struct>]
    type ScheduleInfo =
        {
            Schedule: Schedule
            AdvancesTotal: int<Cent>
            PaymentsTotal: int<Cent>
            PrincipalTotal: int<Cent>
            ProductFeesTotal: int<Cent>
            InterestTotal: int<Cent>
            PenaltyChargesTotal: int<Cent>
            ProductFeesRefund: int<Cent>
            FinalPaymentDate: DateTime
            FinalPaymentDateCount: int
            Apr: decimal<Percent>
            EffectiveAnnualInterestRate: decimal<Percent>
            EffectiveDailyInterestRate: decimal<Percent>
        }

    /// calculate extra information on the amortisation schedule
    let scheduleInfo s =
        let iri = intermediateResultInfo s.IntermediateResult
        let paymentsTotal si = si |> scheduleItemInfo |> _.PaymentsTotal
        let interestTotal = s.Items |> Array.sumBy _.InterestPortion
        let principalTotal = s.Items |> Array.sumBy _.PrincipalPortion
        let productFeesTotal = s.Items |> Array.sumBy _.ProductFeesPortion
        let penaltyChargesTotal = s.Items |> Array.sumBy _.PenaltyChargesPortion
        let productFeesRefund = s.Items |> Array.sumBy _.ProductFeesRefund
        let apr =
            let advanceAmount = s.Parameters.Principal
            let advanceDate = s.Parameters.StartDate
            let payments =
                s.Items
                |> Array.filter(fun si -> paymentsTotal si > 0<Cent>)
                |> Array.map(fun si -> { Apr.TransferType = Apr.Payment; Apr.Date = si.Date; Apr.Amount = paymentsTotal si })
            Apr.calculate Apr.UsActuarial 8 advanceAmount advanceDate payments
        {
            Schedule = s
            AdvancesTotal = s.Items |> Array.sumBy _.Advance
            PaymentsTotal = s.Items |> Array.sumBy paymentsTotal
            PrincipalTotal = principalTotal
            ProductFeesTotal = productFeesTotal
            InterestTotal = interestTotal
            ProductFeesRefund = productFeesRefund
            PenaltyChargesTotal = penaltyChargesTotal
            Apr = apr
            EffectiveAnnualInterestRate = (decimal interestTotal / decimal (principalTotal + productFeesTotal - productFeesRefund)) * (365m / decimal iri.MaxPaymentDay) |> Percent.fromDecimal |> Percent.round 6
            EffectiveDailyInterestRate = (decimal interestTotal / decimal (principalTotal + productFeesTotal - productFeesRefund)) * (1m / decimal iri.MaxPaymentDay) |> Percent.fromDecimal |> Percent.round 6
            FinalPaymentDate = s.Items |> Array.maxBy _.Date |> _.Date
            FinalPaymentDateCount = s.Items |> Array.filter(fun si -> si.Payments |> Array.isEmpty |> not) |> Array.length
        }

    /// calculate total interest due on a regular schedule
    [<TailCall>]
    let calculateInterestTotal (p: Parameters) maxPaymentDay (roughPayments: Payment array) =
        match p.InterestCapitalisation with
        | AtTermStart -> failwith "Not implemented yet"
        | _ ->
        // note that inside this function, "principal" refers to (principal + product fees) together
        let rec calculate (p: Parameters) (roughPayments: Payment array) (day: int<Day>) (principalBalance: decimal) (interestBalance: decimal) (accumulatedInterest: decimal) =
            let pi = parametersInfo p
            if day > maxPaymentDay then
                if abs principalBalance < 0.001m then
                    accumulatedInterest |> Cent.round
                else
                    let roughPayments' = roughPayments |> Array.mapi(fun i r ->
                        { Index = i; Date = r.Date; Day = r.Day; Amount = r.Amount + principalBalance / decimal p.PaymentCount; Interval = r.Interval; PenaltyCharges = r.PenaltyCharges }
                    )
                    calculate p roughPayments' 1<Day> (p.Principal + pi.ProductFeesTotal |> decimal) 0m 0m
            else
                let interest = decimal principalBalance * Percent.toDecimal pi.DailyInterestRate
                let principalRepayment, interestRepayment =
                    roughPayments
                    |> Array.tryPick(fun payment -> if payment.Day = day then Some payment.Amount else None)
                    |> Option.map(fun paymentAmount -> (interestBalance + interest) |> fun interestToPay -> max 0m (decimal paymentAmount - interestToPay), interestToPay)
                    |> Option.defaultValue (0m, 0m)
                calculate p roughPayments (day + 1<Day>) (principalBalance - principalRepayment) (interestBalance + interest - interestRepayment) (accumulatedInterest + interest)
        calculate p roughPayments 1<Day> (p.Principal + (parametersInfo p).ProductFeesTotal |> decimal) 0m 0m

    /// calculate amortisation schedule detailing how elements (principal, product fees, interest and penalty charges) are paid off over time
    let calculateSchedule (p: Parameters) (ir: IntermediateResult) (payments: Payment array) (startDate: DateTime voption) =
        let pi = parametersInfo p
        let iri = intermediateResultInfo ir
        let advance = {
            Date = (startDate |> ValueOption.defaultValue DateTime.Today)
            Day = 0<Day>
            Advance = p.Principal
            Payments = [||]
            NewInterest = 0<Cent>
            NewPenaltyCharges = 0<Cent>
            PrincipalPortion = 0<Cent>
            ProductFeesPortion = 0<Cent>
            InterestPortion = 0<Cent>
            PenaltyChargesPortion = 0<Cent>
            ProductFeesRefund = 0<Cent>
            PrincipalBalance = p.Principal
            ProductFeesBalance = pi.ProductFeesTotal
            InterestBalance = 0<Cent>
            PenaltyChargesBalance = 0<Cent>
        }
        payments
        |> Array.scan(fun asi payment ->
            let newPenaltyCharges = (paymentInfo payment).PenaltyChargesTotal
            let penaltyChargesPortion = newPenaltyCharges + asi.PenaltyChargesBalance

            let newInterest = decimal (asi.PrincipalBalance + asi.ProductFeesBalance) * Percent.toDecimal pi.DailyInterestRate * decimal payment.Interval |> Cent.round
            let interestPortion = newInterest + asi.InterestBalance
            
            let productFeesDue = Cent.min pi.ProductFeesTotal (decimal pi.ProductFeesTotal * decimal payment.Day / decimal iri.MaxPaymentDay |> Cent.round)
            let productFeesRemaining = pi.ProductFeesTotal - productFeesDue
            let settlementFigure = asi.PrincipalBalance + asi.ProductFeesBalance - productFeesRemaining + interestPortion + penaltyChargesPortion
            let isSettlement = (Cent.round payment.Amount > settlementFigure && payment.Day <> iri.MaxPaymentDay) || (payment.Day = iri.MaxPaymentDay && payment.Day = iri.MaxPaymentDay)

            let actualPayment =
                if asi.PrincipalBalance = 0<Cent> then
                    0<Cent>
                elif isSettlement then
                    settlementFigure
                else
                    Cent.round payment.Amount

            let principalPortion, cabFeePortion, productFeesRefund =
                if (penaltyChargesPortion > actualPayment) || (penaltyChargesPortion + interestPortion > actualPayment) then
                    0<Cent>, 0<Cent>, 0<Cent>
                else
                    if isSettlement then
                        let cabFeePayment = asi.ProductFeesBalance - productFeesRemaining
                        actualPayment - penaltyChargesPortion - interestPortion - cabFeePayment, cabFeePayment, productFeesRemaining
                    else
                        let principalPayment = decimal (actualPayment - penaltyChargesPortion - interestPortion) / (1m + Percent.toDecimal pi.ProductFeesPercentage) |> Cent.round
                        principalPayment, actualPayment - penaltyChargesPortion - interestPortion - principalPayment, 0<Cent>
                        
            let principalPortion = Cent.min asi.PrincipalBalance principalPortion
            let productFeesPortion = Cent.min asi.ProductFeesBalance cabFeePortion

            let carriedPenaltyCharges, carriedInterest =
                if penaltyChargesPortion > actualPayment then
                    penaltyChargesPortion - actualPayment, interestPortion
                elif penaltyChargesPortion + interestPortion > actualPayment then
                    0<Cent>, interestPortion - (actualPayment - penaltyChargesPortion)
                else
                    0<Cent>, 0<Cent>

            {
                Date = startDate |> ValueOption.map(fun dt -> dt.AddDays (float payment.Day)) |> ValueOption.defaultValue payment.Date
                Day = payment.Day
                Advance = 0<Cent>
                Payments = if actualPayment > 0<Cent> then [| actualPayment |] else [||]
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
        ) advance

    /// generates an amortisation schedule based on the output options and parameters
    let createRegularScheduleInfo p =
        let pi = parametersInfo p
        let paymentDates = Schedule.generate p.PaymentCount Schedule.Forward p.UnitPeriodConfig
        let termLength = ((paymentDates |> Array.last).Date - p.StartDate.Date).TotalDays

        let roughPayment = decimal (p.Principal + pi.ProductFeesTotal) * (1m + (Percent.toDecimal p.AnnualInterestRate * decimal termLength / 365m)) / decimal p.PaymentCount
        let roughPayments =
            paymentDates
            |> Array.mapi(fun i dt -> {
                Index = i
                Date = dt
                Day = (dt.Date - p.StartDate.Date).TotalDays |> fun f -> int f * 1<Day>
                Amount = roughPayment
                Interval = (dt.Date - (if i = 0 then p.StartDate else paymentDates[i - 1]).Date).TotalDays |> fun f -> int f * 1<Duration>
                PenaltyCharges = ValueNone
            })

        let zeroPayment i = { Index = i; Date = p.StartDate.AddDays(float i); Day = i * 1<Day>; Amount = 0m; Interval = 1<Duration>; PenaltyCharges = ValueNone }
        let maxPaymentDay = roughPayments |> Array.maxBy _.Day |> _.Day
        let roughPayments' =
            match p.InterestCapitalisation with
            | AtTermStart
            | OnPaymentDates -> roughPayments
            | EveryDay ->
                [| 1 .. int maxPaymentDay |]
                |> Array.map(fun i -> 
                    roughPayments
                    |> Array.tryFind (fun p -> p.Day = i * 1<Day>)
                    |> function
                        | Some p -> { p with Index = i; Interval = 1<Duration> }
                        | None -> zeroPayment i
                )

        let finalPaymentIndex = roughPayments' |> Array.length |> (+) -1
        let intermediateResult = {
            Parameters = p
            RoughPayments = roughPayments'
            InterestTotal = calculateInterestTotal p maxPaymentDay roughPayments'
            PenaltyChargesTotal = 0<Cent>
        }
        let iri = intermediateResultInfo intermediateResult
        let payments =
            roughPayments'
            |> Array.mapi(fun i p -> { p with Amount = if finalPaymentIndex = i then decimal iri.FinalPayment elif p.Amount > 0m then decimal iri.LevelPayment else 0m })

        let outputFun =
            match p.Output with
            | Full -> id
            | Summary ->
                Array.filter(fun si -> si.Advance > 0<Cent> || si.Payments |> Array.isEmpty |> not)
                >> Array.map(fun si -> { si with NewInterest = si.InterestPortion })

        {
            Parameters = p
            IntermediateResult = intermediateResult
            Items = calculateSchedule p intermediateResult payments (ValueSome p.StartDate) |> outputFun
        }
        |> scheduleInfo
