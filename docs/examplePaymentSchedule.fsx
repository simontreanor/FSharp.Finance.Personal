(**
---
title: Payment schedule examples
category: Examples
categoryindex: 2
index: 4
description: Examples of payment schedules
keywords: payment schedule unit-period
---
*)

(**
# Payment Schedule Examples

## Basic example #1

The following example shows the scheduled for a car loan of £10,000 taken out on 7 February 2024 with 36 monthly repayments:

*)

#r "nuget:FSharp.Finance.Personal"

open System
open FSharp.Finance.Personal
open PaymentSchedule

let scheduleParameters =
    {
        AsOfDate = Date(2024, 02, 07)
        StartDate = Date(2024, 02, 07)
        Principal = 10000_00L<Cent>
        UnitPeriodConfig = UnitPeriod.Monthly(1, 2024, 3, 7)
        PaymentCount = 36
        FeesAndCharges = {
            Fees = [||]
            FeesSettlement = Fees.Settlement.DueInFull
            Charges = [||]
            LatePaymentGracePeriod = 0<DurationDay>
        }
        Interest = {
            Rate = Interest.Rate.Annual (Percent 6.9m)
            Cap = { Total = ValueNone; Daily = ValueNone }
            GracePeriod = 0<DurationDay>
            Holidays = [||]
            RateOnNegativeBalance = ValueNone
        }
        Calculation = {
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            RoundingOptions = { InterestRounding = Round MidpointRounding.AwayFromZero; PaymentRounding = Round MidpointRounding.AwayFromZero }
            FinalPaymentAdjustment = AdjustFinalPayment
        }
    }
let schedule = scheduleParameters |> calculate AroundZero
schedule

(*** include-it ***)

(**
It is possible to format the `Items` property as an HTML table:
*)

let html =
    schedule
    |> ValueOption.map (fun s -> s.Items |> Formatting.generateHtmlFromArray)
    |> ValueOption.defaultValue ""
html

(*** include-it-raw ***)
