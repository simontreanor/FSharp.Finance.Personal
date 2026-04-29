(**
---
title: Mortgage Example
category: Examples
categoryindex: 2
index: 6
description: Worked example of a residential repayment mortgage
keywords: mortgage repayment amortisation interest
---
*)

(**
# Mortgage Example

## Scenario

A homeowner takes out a **£200,000** residential repayment mortgage on **1 March 2024**
over **25 years (300 months)** at a fixed annual rate of **4.5%** (actuarial method).
Monthly repayments begin on **1 April 2024**.

This example models the capital-and-interest (repayment) structure typical of UK residential mortgages.

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
        EvaluationDate = Date(2024, 3, 1)
        StartDate = Date(2024, 3, 1)
        Principal = 200000_00L<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2024, 3, 31)
                ScheduleLength = PaymentCount 300
            }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.Actuarial
            StandardRate = Interest.Rate.Annual(Percent 4.5m)
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

The schedule shows the level monthly payment and total interest payable over the full term:
*)

let schedule = calculateBasicSchedule parameters.Basic

schedule

(*** include-it ***)

(**
### Amortisation schedule (first 12 months)

To keep the output manageable, we simulate the first 12 monthly payments on time and
display the resulting amortisation schedule through to the evaluation date:
*)

let actualPayments =
    schedule.Items
    |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
    |> Array.truncate 12
    |> Array.map (fun si ->
        si.Day, [| ActualPayment.quickConfirmed (ScheduledPayment.total si.ScheduledPayment) |]
    )
    |> Map.ofArray

let amortisationResult = actualPayments |> amortise parameters

let html = Schedule.toHtmlTable parameters amortisationResult.AmortisationSchedule

$"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)
