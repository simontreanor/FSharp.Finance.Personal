(**
---
title: Amortisation Examples
category: Examples
categoryindex: 2
index: 3
description: Examples of amortisation
keywords: amortisation amortization
---
*)

(**
# Amortisation Examples

## Basic example #1

The following example shows a small personal loan of £1,500 taken out on 26 November 2022 and repaid on time in 5 monthly instalments,
with a level payment of £456.88 and a final payment of £456.84:

*)

#r "nuget:FSharp.Finance.Personal"

open System
open FSharp.Finance.Personal
open Amortisation
open Calculation
open DateDay
open Scheduling

let scheduleParameters =
    {
        AsOfDate = Date(2023, 4, 1)
        StartDate = Date(2022, 11, 26)
        Principal = 1500_00L<Cent>
        ScheduleConfig = AutoGenerateSchedule {
            UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
            ScheduleLength = PaymentCount 5
        }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            ScheduledPaymentOption = AsScheduled
            CloseBalanceOption = LeaveOpenBalance
            PaymentRounding = RoundUp
            MinimumPayment = DeferOrWriteOff 50L<Cent>
            PaymentTimeout = 3<DurationDay>
        }
        FeeConfig = None
        ChargeConfig = None
        InterestConfig = {
            Method = Interest.Method.Simple
            StandardRate = Interest.Rate.Daily (Percent 0.8m)
            Cap = {
                TotalAmount = ValueSome <| Amount.Percentage (Percent 100m, Restriction.NoLimit)
                DailyAmount = ValueSome <| Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
            }
            InitialGracePeriod = 3<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
            Rounding = RoundDown
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
        }
    }

let actualPayments =
    Map [
        4<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        35<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        66<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        94<OffsetDay>, [| ActualPayment.quickConfirmed 456_88L<Cent> |]
        125<OffsetDay>, [| ActualPayment.quickConfirmed 456_84L<Cent> |]
    ]

let schedules =
    actualPayments
    |> Amortisation.generate scheduleParameters ValueNone false

schedules.AmortisationSchedule

(*** include-it ***)

(**
It is possible to format the `Items` property as an HTML table:
*)

let html = Schedule.toHtmlTable schedules.AmortisationSchedule

$"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)
