(**
---
title: PCP Car Finance Example
category: Examples
categoryindex: 2
index: 7
description: Worked example of a Personal Contract Purchase (PCP) car finance plan
keywords: PCP car finance personal contract purchase balloon payment amortisation
---
*)

(**
# PCP Car Finance Example

## Scenario

A customer purchases a car worth **£25,000** on **1 June 2024** using a Personal Contract
Purchase (PCP) agreement. They pay a **£3,000 deposit**, leaving a **£22,000** financed amount.
The agreement runs for **36 months** at **6.9% APR** (actuarial method) with a
**Guaranteed Future Value (GFV) / balloon payment of £10,000** at the end.

PCP is modelled by using a `CustomSchedule` where the first 35 payments cover interest and a
small amount of principal, and the final payment equals the GFV plus any remaining balance.

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Amortisation
open AppliedPayment
open Calculation
open DateDay
open Scheduling
open UnitPeriod

let startDate = Date(2024, 6, 1)

(**
### Step 1 – Calculate the level payment on the financed amount

First calculate the level payment that would amortise the full £22,000 over 36 months
at the agreed rate. We then adjust the final payment to be the balloon (GFV) instead.
*)

let pcpBasicParams: BasicParameters = {
    EvaluationDate = startDate
    StartDate = startDate
    Principal = 22000_00L<Cent>
    ScheduleConfig =
        AutoGenerateSchedule {
            UnitPeriodConfig = Monthly(1, 2024, 6, 30)
            ScheduleLength = PaymentCount 36
        }
    PaymentConfig = {
        LevelPaymentOption = LowerFinalPayment
        Rounding = RoundUp
    }
    FeeConfig = ValueNone
    InterestConfig = {
        Method = Interest.Method.Actuarial
        StandardRate = Interest.Rate.Annual(Percent 6.9m)
        Cap = Interest.Cap.zero
        Rounding = RoundDown
        AprMethod = Apr.CalculationMethod.UnitedKingdom 3
    }
}

let referenceSchedule = calculateBasicSchedule pcpBasicParams

(**
### Step 2 – Build the PCP schedule with a balloon final payment

The monthly payment is taken from the reference schedule; the 36th payment is replaced
by the £10,000 GFV balloon. In practice the balloon is calculated so that the outstanding
balance exactly equals the GFV; this example approximates that for illustration purposes.
*)

let balloonPayment = 10000_00L<Cent>

let monthlyPaymentDays =
    referenceSchedule.Items
    |> Array.filter (_.ScheduledPayment >> ScheduledPayment.isSome)
    |> Array.map (fun si -> si.Day)

let pcpPaymentMap =
    monthlyPaymentDays
    |> Array.mapi (fun i day ->
        let paymentValue =
            if i < Array.length monthlyPaymentDays - 1 then
                ScheduledPayment.total referenceSchedule.Items[i].ScheduledPayment
            else
                balloonPayment
        day, ScheduledPayment.quick (ValueSome paymentValue) ValueNone
    )
    |> Map.ofArray

let parameters: Parameters = {
    Basic = {
        EvaluationDate = startDate
        StartDate = startDate
        Principal = 22000_00L<Cent>
        ScheduleConfig = CustomSchedule pcpPaymentMap
        PaymentConfig = {
            LevelPaymentOption = LowerFinalPayment
            Rounding = RoundUp
        }
        FeeConfig = ValueNone
        InterestConfig = {
            Method = Interest.Method.Actuarial
            StandardRate = Interest.Rate.Annual(Percent 6.9m)
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
### Step 3 – Amortise all payments on time

Simulate the customer making all 36 payments on schedule (including the balloon):
*)

let actualPayments =
    pcpPaymentMap
    |> Map.map (fun _ sp ->
        [| ActualPayment.quickConfirmed (ScheduledPayment.total sp) |]
    )

let amortisationResult = actualPayments |> amortise parameters

let html = Schedule.toHtmlTable parameters amortisationResult.AmortisationSchedule

$"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)
