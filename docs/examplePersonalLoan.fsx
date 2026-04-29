(**
---
title: Personal Loan Example
category: Examples
categoryindex: 2
index: 5
description: Worked example of a personal loan amortisation schedule
keywords: personal loan amortisation schedule interest
---
*)

(**
# Personal Loan Example

## Scenario

A customer borrows **£5,000** on **1 January 2024** and repays it over **24 monthly instalments**
at an annual interest rate of **9.9%** (actuarial method). Payments begin on **1 February 2024**.

This is a typical unsecured personal loan product.

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Amortisation
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

let parameters: Parameters = {
    Basic = {
        EvaluationDate = Date(2026, 1, 1)
        StartDate = Date(2024, 1, 1)
        Principal = 5000_00L<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2024, 1, 31)
                ScheduleLength = PaymentCount 24
            }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.Actuarial
            StandardRate = Interest.Rate.Annual(Percent 9.9m)
            Cap = Interest.Cap.zero
            Rounding = RoundDown
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }
    }
    Advanced = {
        PaymentConfig = {
            ScheduledPaymentOption = AsScheduled
            Minimum = DeferOrWriteOff 50L<Cent>
            Timeout = 3<DurationDay>
        }
        FeeConfig = ValueNone
        ChargeConfig = None
        InterestConfig = {
            InitialGracePeriod = 0<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
        }
        SettlementDay = SettlementDay.NoSettlement
        TrimEnd = false
    }
}

(**
### Generate the basic payment schedule

The schedule shows the level monthly payment amount and the total cost of credit:
*)

let schedule = calculateBasicSchedule parameters.Basic

schedule

(*** include-it ***)

(**
### Amortise with on-time payments

Simulate the customer making every payment exactly on schedule:
*)

let actualPayments =
    schedule.Items
    |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
    |> Array.map (fun si ->
        si.Day, [| ActualPayment.quickConfirmed (ScheduledPayment.total si.ScheduledPayment) |]
    )
    |> Map.ofArray

let amortisationResult = actualPayments |> amortise parameters

let html = Schedule.toHtmlTable parameters amortisationResult.AmortisationSchedule

$"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)
