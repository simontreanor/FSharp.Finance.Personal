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
open CustomerPayments
open DateDay
open FeesAndCharges
open PaymentSchedule
open Percentages

let scheduleParameters =
    {
        AsOfDate = Date(2023, 4, 1)
        StartDate = Date(2022, 11, 26)
        Principal = 1500_00L<Cent>
        PaymentSchedule = RegularSchedule (
            UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31),
            PaymentCount = 5,
            MaxDuration = ValueNone
        )
        FeesAndCharges = {
            Fees = [||]
            FeesAmortisation = Fees.FeeAmortisation.AmortiseProportionately
            FeesSettlementRefund = Fees.SettlementRefund.ProRata ValueNone
            Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
            ChargesHolidays = [||]
            ChargesGrouping = OneChargeTypePerDay
            LatePaymentGracePeriod = 0<DurationDay>
        }
        Interest = {
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

let actualPayments = [|
    { PaymentDay =  4<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
    { PaymentDay = 35<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
    { PaymentDay = 66<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
    { PaymentDay = 94<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_88L<Cent>) }
    { PaymentDay = 125<OffsetDay>; PaymentDetails = ActualPayment (ActualPaymentStatus.Confirmed 456_84L<Cent>) }
|]

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
    |> ValueOption.map (_.ScheduleItems >> Formatting.generateHtmlFromArray None)
    |> ValueOption.defaultValue ""

$"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)
