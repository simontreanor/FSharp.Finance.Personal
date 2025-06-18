(**
---
title: Amortisation Examples
category: Examples
categoryindex: 2
index: 4
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

open FSharp.Finance.Personal
open Amortisation
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

let parameters: Parameters = {
    Basic = {
        EvaluationDate = Date(2023, 4, 1)
        StartDate = Date(2022, 11, 26)
        Principal = 1500_00L<Cent>
        ScheduleConfig =
            AutoGenerateSchedule {
                UnitPeriodConfig = Monthly(1, 2022, 11, 31)
                ScheduleLength = PaymentCount 5
            }
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.Actuarial
            StandardRate = Interest.Rate.Daily(Percent 0.8m)
            Cap = {
                TotalAmount = Amount.Percentage(Percent 100m, Restriction.NoLimit)
                DailyAmount = Amount.Percentage(Percent 0.8m, Restriction.NoLimit)
            }
            Rounding = RoundDown
            AprMethod = Apr.CalculationMethod.UnitedKingdom
            AprPrecision = 3
        }
    }
    Advanced = {
        PaymentConfig = {
            ScheduledPaymentOption = AsScheduled
            Minimum = DeferOrWriteOff 50L<Cent>
            Timeout = 3<OffsetDay>
        }
        FeeConfig = ValueNone
        ChargeConfig = None
        InterestConfig = {
            InitialGracePeriod = 3<OffsetDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = Interest.Rate.Zero
        }
        SettlementDay = SettlementDay.NoSettlement
        TrimEnd = false
    }
}

let actualPayments =
    Map [
        4<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
        35<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
        66<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
        94<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_88L<Cent> ]
        125<OffsetDay>, Map [ 0, ActualPayment.quickConfirmed 456_84L<Cent> ]
    ]

let schedules = actualPayments |> amortise parameters

schedules.AmortisationSchedule

(*** include-it ***)

(**
It is possible to format the `Items` property as an HTML table:
*)

let html = Schedule.toHtmlTable parameters schedules.AmortisationSchedule

$"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)
