namespace FSharp.Finance.Personal

open System
open Payments

/// functions for rescheduling payments after an original schedule failed to amortise
module Rescheduling =

    [<Struct>]
    type PaymentPlanSchedule =
        | RegularPlan of PaymentPlanStart: DateTime * UnitPeriod: UnitPeriod.UnitPeriod * Amount: int64<Cent>
        | IrregularPlan of Payment array

    [<Struct>]
    type PaymentPlanParameters = {
        Schedule: PaymentPlanSchedule
        InterestRate: Interest.Rate voption
        LatePaymentGracePeriod: int<DurationDay> voption
        LatePaymentCharge: Amount voption
    }

    /// creates a payment plan for fully amortising an outstanding balance based on a payment amount, unit-period config and interest rate
    let createPaymentPlan (amount: int64<Cent>) unitPeriodConfig interestRate (outstandingBalance: int64<Cent>) originalStartDate =
        let count = // to-do: add a function that takes this count as a parameter
            let roughUnitPeriodLength = unitPeriodConfig |> UnitPeriod.Config.roughLength
            let initialCount = decimal outstandingBalance / decimal amount |> Math.Ceiling
            let estimatedYears = (roughUnitPeriodLength * initialCount) / 365m
            let annualInterestRate = interestRate |> Interest.Rate.annual |> Percent.toDecimal
            (1m + (annualInterestRate * estimatedYears)) * initialCount |> Math.Ceiling |> int
        unitPeriodConfig
        |> UnitPeriod.generatePaymentSchedule count UnitPeriod.Direction.Forward
        |> Array.map(fun d -> { PaymentDay = d |> OffsetDay.fromDate originalStartDate ; PaymentDetails = ScheduledPayment amount })

    /// 
    let disapplyInterest =
        ()