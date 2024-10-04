(**
---
title: Amortisation examples
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

open FSharp.Finance.Personal
open Calculation
open Currency
open DateDay
open Fee
open Charge
open PaymentSchedule
open Percentages

let scheduleParameters =
    {
        AsOfDate = Date(2023, 4, 1)
        StartDate = Date(2022, 11, 26)
        Principal = 1500_00L<Cent>
        PaymentSchedule = AutoGenerateSchedule {
            UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
            PaymentCount = 5
            MaxDuration = ValueNone
        }
        PaymentOptions = {
            ScheduledPaymentOption = AsScheduled
            CloseBalanceOption = LeaveOpenBalance
        }
        FeeConfig = Fee.Config.DefaultValue
        ChargeConfig = {
            ChargeTypes = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
            Rounding = ValueSome RoundDown
            ChargeHolidays = [||]
            ChargeGrouping = Charge.ChargeGrouping.OneChargeTypePerDay
            LatePaymentGracePeriod = 0<DurationDay>
        }
        Interest = {
            Method = Interest.Method.Simple
            StandardRate = Interest.Rate.Daily (Percent 0.8m)
            Cap = {
                Total = ValueSome (Amount.Percentage (Percent 100m, ValueNone, ValueSome RoundDown))
                Daily = ValueSome (Amount.Percentage (Percent 0.8m, ValueNone, ValueNone))
            }
            InitialGracePeriod = 3<DurationDay>
            PromotionalRates = [||]
            RateOnNegativeBalance = ValueNone
        }
        Calculation = {
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            RoundingOptions = RoundingOptions.recommended
            MinimumPayment = DeferOrWriteOff 50L<Cent>
            PaymentTimeout = 3<DurationDay>
        }
    }

let actualPayments =
    Map [
        4<OffsetDay>, [| ActualPayment.QuickConfirmed 456_88L<Cent> |]
        35<OffsetDay>, [| ActualPayment.QuickConfirmed 456_88L<Cent> |]
        66<OffsetDay>, [| ActualPayment.QuickConfirmed 456_88L<Cent> |]
        94<OffsetDay>, [| ActualPayment.QuickConfirmed 456_88L<Cent> |]
        125<OffsetDay>, [| ActualPayment.QuickConfirmed 456_84L<Cent> |]
    ]

let amortisationSchedule =
    actualPayments
    |> Amortisation.generate scheduleParameters IntendedPurpose.Statement ScheduleType.Original false

amortisationSchedule

(*** include-it ***)

(**
It is possible to format the `Items` property as an HTML table:
*)

let html =
    amortisationSchedule
    |> ValueOption.map (_.ScheduleItems >> generateHtmlFromMap None)
    |> ValueOption.defaultValue ""

$"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)
