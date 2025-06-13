(**
---
title: Payment schedule examples
category: Examples
categoryindex: 2
index: 5
description: Examples of payment schedules
keywords: payment schedule unit-period
---
*)

(**
# Payment Schedule Examples

## Basic Example #1

The following example shows the scheduled for a car loan of £10,000 taken out on 7 February 2024 with 36 monthly repayments:

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Calculation
open DateDay
open Scheduling
open UnitPeriod

let parameters = {
    EvaluationDate = Date(2024, 02, 07)
    StartDate = Date(2024, 02, 07)
    Principal = 10000_00uL<Cent>
    ScheduleConfig =
        AutoGenerateSchedule {
            UnitPeriodConfig = Monthly(1u, 2024, 3, 7)
            ScheduleLength = PaymentCount 36u
        }
    PaymentConfig = {
        LevelPaymentOption = LowerFinalPayment
        Rounding = RoundUp
    }
    FeeConfig = ValueNone
    InterestConfig = {
        Method = Interest.Method.Actuarial
        StandardRate = Interest.Rate.Annual <| Percent 6.9m
        Cap = Interest.Cap.zero
        Rounding = RoundDown
        AprMethod = Apr.CalculationMethod.UnitedKingdom
        AprPrecision = 3u
    }
}

let schedule = parameters |> calculateBasicSchedule

schedule

(*** include-it ***)

(**
It is possible to format the `Items` property as an HTML table:
*)

let html = schedule |> BasicSchedule.toHtmlTable

$"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)
