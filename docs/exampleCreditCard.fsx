(**
---
title: Credit Card Example
category: Examples
categoryindex: 2
index: 8
description: Worked example of a credit card minimum-payment scenario
keywords: credit card revolving credit minimum payment interest amortisation
---
*)

(**
# Credit Card Example

## Scenario

A customer has a **£2,000** credit card balance as of **1 January 2024** at a representative
APR of **24.9%** per annum (actuarial method). The card requires a monthly minimum payment of
**£25 or 1% of the balance, whichever is greater**. For simplicity this example fixes the
payments at the minimum amount for a fixed period to illustrate how long it takes to
clear the balance and how much interest is paid.

We model this as a fixed repayment schedule where the customer pays **£50 per month** —
a modest amount above the minimum — and observe the amortisation over time.

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Amortisation
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

(**
### Parameters

A fixed monthly payment of £50 is applied until the balance is cleared.
The schedule is long enough to cover full repayment; `TrimEnd = true` stops
the schedule once the balance reaches zero.
*)

let parameters: Parameters = {
    Basic = {
        EvaluationDate = Date(2024, 1, 1)
        StartDate = Date(2024, 1, 1)
        Principal = 2000_00L<Cent>
        ScheduleConfig =
            FixedSchedules [|
                {
                    UnitPeriodConfig = Monthly(1, 2024, 1, 31)
                    PaymentCount = 60
                    PaymentValue = 50_00L<Cent>
                    ScheduleType = ScheduleType.Original
                }
            |]
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.Actuarial
            StandardRate = Interest.Rate.Annual(Percent 24.9m)
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
        TrimEnd = true
    }
}

(**
### Amortise the credit card balance

Simulate the customer making every £50 payment on time until the balance is cleared:
*)

let schedule = calculateBasicSchedule parameters.Basic

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

(**
### Key observations

- The total interest paid and the number of months to clear the balance are visible in the
  final stats of the schedule.
- Increasing the monthly payment significantly reduces the total interest cost and repayment term.
- With a high interest rate (24.9% APR) and a low fixed payment, a large proportion of early
  payments goes toward interest rather than reducing the principal — a behaviour clearly
  visible in the amortisation schedule above.
*)
