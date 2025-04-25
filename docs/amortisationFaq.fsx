(**
---
title: Amortisation FAQ
category: Compliance
categoryindex: 4
index: 4
description: Frequently asked questions about amortisation calculations
---

# Amortisation FAQ

## How is interest calculated?

### Initial calculations

When setting up a schedule, interest is calculated based on a simple schedule that assumes that all payments will be made on time and in full.

* For simple-interest calculations, see [Simple-Interest Calculations](interestSimple.fsx)
* For add-on-interest calculations, see [Add-On-Interest Calculations](interestAddOn.fsx)

### Running calculations

While a schedule is active (i.e. it has started and the principal balance is non-zero), the interest is calculated based on the scheduled and
actual payments made. The day of any scheduled or actual payment constitutes an event in the amortisation schedule, and simple interest is
calculated based on the principal (+ fees) balance and the number of days since the last event.

For full details, see [Amortisation Calculations](amortisation.fsx).

## Basis of calculations for the following questions

The basic schedule we will use here is a loan of £1000 advanced on 24 April 2025, paid back over 4 months starting one month after the advance date.
The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount.
The examples may use either the simple-interest method or the add-on-interest method.

*)

(*** hide ***)
#r "nuget:FSharp.Finance.Personal"
open FSharp.Finance.Personal
open Calculation
open DateDay
open Scheduling
open UnitPeriod

(**
<br />
<details>
<summary>Show/hide parameters</summary>
<div>
*)

let parameters = {
    EvaluationDate = Date(2025, 4, 24) // the date that we're evaluating the schedule
    StartDate = Date(2025, 4, 24)
    Principal = 1000_00L<Cent>
    ScheduleConfig = AutoGenerateSchedule {
        UnitPeriodConfig = Monthly(1, 2025, 5, 24)
        ScheduleLength = PaymentCount 4
    }
    PaymentConfig = {
        LevelPaymentOption = LowerFinalPayment
        ScheduledPaymentOption = AsScheduled
        Rounding = RoundUp
        Minimum = DeferOrWriteOff 50L<Cent>
        Timeout = 3<DurationDay>
    }
    FeeConfig = None
    ChargeConfig = None
    InterestConfig = {
        Method = Interest.Method.Simple
        StandardRate = Interest.Rate.Daily (Percent 0.798m)
        Cap = {
            TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
            DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
        }
        InitialGracePeriod = 3<DurationDay>
        PromotionalRates = [||]
        RateOnNegativeBalance = Interest.Rate.Zero
        Rounding = RoundDown
        AprMethod = Apr.CalculationMethod.UnitedKingdom 3
    }
}

(**
</div>
</details>

## What happens if a customer were to not make their repayment on time?

Let's take a look at the amortisation schedules to illustrate this. First we will look at the simple-interest method and then the add-on-interest method.

<br />
> Note: As a general principle, for payments that are not yet due it is assumed that they will be paid on time and in full. This is to provide for a more
> realistic projection of the schedule. 

### Simple Interest

Here's the schedule prior to any actual payments being made (looking at it from day 0). The main thing to note is that the principal balance at the end of the
schedule is zero, meaning it is fully amortised.

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation0 =
    Amortisation.generate
        parameters //the parameters defined above
        ValueNone // no settlement quotation requested
        false // don't clip unrequired payments from the end of the schedule
        Map.empty // no actual payments made

(**
</div>
</details>
*)

(*** hide ***)
amortisation0
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""
(*** include-it-raw ***)

(**
Now, let's assume that it's day 35 and no payments have been made, so the payment due on day 30 has been missed. The schedule would look like this:

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation1 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 5, 29) // evaluate the schedule on day 35
        }
        ValueNone // no settlement quotation requested
        false // don't clip unrequired payments from the end of the schedule
        Map.empty // no actual payments made

(**
</div>
</details>
*)

(*** hide ***)
amortisation1
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
<br />
> Note: You can see that a new event at day 35 has been created, as we are observing the schedule on that day. This capitalises the interest that has accrued
> so far so that we can see the principal balance on that day.

As the payment due on day 30 has been missed, the interest that has accrued is not paid off and is added to the interest balance. This means that any subsequent
payments would need to clear the interest balance before the principal balance is reduced, and this means that the principal balance remains higher for longer.
Looking at the principal balance at the end of the schedule shows that this has not-insubstantial consequences, as there is still £693.40 outstanding.

### Add-On Interest

Here's the schedule prior to any actual payments being made (looking at it from day 0). This time the interest is added up-front, with an initial interest
balance of £815.56. The interest is paid off before the principal, so more interest is accrued in total than under the simple-interest method. The principal
at the end of the schedule is zero, meaning it is fully amortised.

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation2 =
    Amortisation.generate
        { parameters with
            InterestConfig.Method = Interest.Method.AddOn // use the add-on interest method
        }
        ValueNone // no settlement quotation requested
        false // don't clip unrequired payments from the end of the schedule
        Map.empty // no actual payments made

(**
</div>
</details>
*)

(*** hide ***)
amortisation2
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
Let's again assume that it's day 35 and no payments have been made, so the payment due on day 30 has been missed. The schedule would look like this:

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation3 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 5, 27) // evaluate the schedule on day 35
            InterestConfig.Method = Interest.Method.AddOn // use the add-on interest method
        }
        ValueNone // no settlement quotation requested
        false // don't clip unrequired payments from the end of the schedule
        Map.empty // no actual payments made

(**
</div>
</details>
*)

(*** hide ***)
amortisation3
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
As the payment due on day 30 has been missed, the interest balance stays higher for longer, meaning that the principal balance remains higher for longer.
This means that more interest is accrued in total than the initial interest balance. To correct for this, new interest of £134.30 is added to the final
schedule item. The principal balance at the end of the schedule is therefore £588.45.

<br />
> **Why is the final settlement amount lower for the add-on method?**
>
> You may well wonder, given that the add-on-interest method results in a higher amount of interest accruing than simple-interest method, why the final
> settlement amount (i.e. the final principal balance) is lower for the add-on method.
> <br /><br />
> One interesting feature of the add-on method is that in the early days of the schedule, missed payments have no effect on the principal balance. Looking
> at the original add-on schedule, we see that the principal balance remains at £1000 until day 30. Any extra interest is only accrued if the principal
> balance remains outstanding at this level for longer than this. Looking at the schedule on day 35, we see that the principal balance remains at £1000
> until day 61, meaning that extra interest is accrued, but only for 31 days out of 60 compared to the simple-interest method.

<br />
> **Why is the new interest only added at the very end of the schedule?**
>
> During the schedule, actual-payment amounts and timings may vary, some of which may well have an effect on the total interest accrued. If we were to make
> constant adjustments to the interest balance, this would become difficult to track. If subsequent payments were paid earlier than due this could even lead
> to a situation where interest was overpaid and a refund might be required. By making the interest adjustment at the end, we can avoid this situation.

## How is the additional interest calculated?

### Simple Interest

Given that the interest rate is usually fixed for the duration of the schedule, the interest accrued is purely a function of the principal balance and
how many days it is outstanding:

<div class="mermaid">
    graph LR
        A["$$\text{interest} = \text{principal balance} \times \text{daily interest rate} \times \text{days}$$"]
</div>

Therefore any variations will affect the interest accrued:

* A missed or late payment will increase the number of days and therefore increase the total interest
* An underpayment will leave the principal at a higher than expected level and therefore increase the total interest
* An overpayment or early settlement will reduce both the principal and number of days outstanding and therefore decrease the overall interest.

### Add-On Interest

Though the interest is calculated up-front and added to the schedule as an initial interest balance, adjustments will need to be made if the payment
schedule is not adhered to. At the end of the schedule, the total simple interest (calculated as stated in the Simple Interest section above) is
compared to the initial interest balance and a correction is made to the final schedule item if necessary.

## Is the additional interest calculated on the outstanding loan principal?

Indirectly, yes. At the end of the schedule, the total simple interest (calculated as stated in the Simple Interest section above) is
compared to the initial interest balance and a correction is made to the final schedule item if necessary.

## Where is the additional interest added?

For simple-interest, the interest is automatically adjusted during the schedule. For add-on interest, the adjustment is made at the end of the schedule.

## When is the customer expected to pay off this additional interest?

If additional interest has accrued, the effect of this will be that the schedule is not fully amortised, i.e. the final principal balance is not zero.
Deciding how to handle this is outside the scope of the library, and depends on business rules. There are a number of ways, including:

* Adding the outstanding principal balance to the final payment amount
* Rescheduling the loan, e.g. by adding new scheduled payments to the end of the schedule
* Rolling over the loan, i.e. creating a new loan with any outstanding balances as the new principal balance

## Is there a set maximum amount of days that are used as a limit for additional interest to be charged?

No, caps on interest charges are not based on the number of days, but on the interest caps set in the loan parameters.
Both daily and total caps can be set, and these are defined as either a simple amount or a percentage of the principal amount.

## What happens when a customer settles earlier than the agreed term?

First we will look at the simple-interest method and then the add-on-interest method.

### Simple Interest

As a reminder, here's the schedule prior to any actual payments being made (looking at it from day 0).
*)

(*** hide ***)
amortisation0
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
Now, let's assume that the first two payments have been made on time, and the customer decides to repay in full on day 70. The schedule would look like this:

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation4 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 7, 3) // evaluate the schedule on day 70
        }
        SettlementDay.SettlementOnEvaluationDay // settlement quotation requested on day 70
        false // don't clip unrequired payments from the end of the schedule
        (Map [
            30<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
        ]) // actual payments made on days 30 and 61

(**
</div>
</details>
*)

(*** hide ***)
amortisation4
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
<br />
> Note: You can see that a new event at day 70 has been created, as we are observing the schedule on that day, and have requested a settlement quotation on that day too.

The settlement quotation is £650.83, which is the sum required to pay off all outstanding balances, including any interest accrued up to that day. This fully amortises
the schedule, and the remaining two payments on days 91 and 122 are no longer required. The total of the two payments is £835.41, meaning that the customer has saved
£184.58 in interest.

### Add-On Interest

As a reminder, here's the schedule prior to any actual payments being made (looking at it from day 0). This time the interest is added up-front, with an initial interest
balance of £815.56.
*)

(*** hide ***)
amortisation2
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
Let's assume again that the first two payments have been made on time, and the customer decides to repay in full on day 70. The schedule would look like this:

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation5 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 7, 3) // evaluate the schedule on day 70
            InterestConfig.Method = Interest.Method.AddOn // use the add-on interest method
        }
        SettlementDay.SettlementOnEvaluationDay // settlement quotation requested on day 70
        false // don't clip unrequired payments from the end of the schedule
        (Map [
            30<OffsetDay>, [| ActualPayment.quickConfirmed 454_15L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 454_15L<Cent> |]
        ]) // actual payments made on days 30 and 61

(**
</div>
</details>
*)

(*** hide ***)
amortisation5
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
The settlement quotation is £643.71, which is the sum required to pay off all outstanding balances, including any interest accrued up to that day. As this is an early
settlement, and the interest for the full schedule was charged up-front, an interest rebate of £264.55 has been calculated. The principal balance of £908.26, minus the
interest rebate of £264.55, leaves a final settlement payment of £643.71 to pay. This would fully amortise the schedule, and the remaining two payments on days 91 and
122 are no longer required. In contrast to the simple-interest method, where you have to add up the unrequired payments and deduct the settlement figure to calculate the
saved interest, in the add-on-interest method the interest rebate is explicitly calculated.

## Is overcharged interest refunded?

As shown above, if a customer settles early and has overpaid interest, this is rebated as part of the settlement quotation. It would be termed a _rebate_ rather than a _refund_,
as the settlement quotation is generally a positive amount and so is a net flow from the customer to the lender.

The exception to this is if the customer has somehow managed to overpay, i.e. to pay an amount greater than the settlement figure. In this case, the lender would need to
refund the customer the difference between the settlement figure and the amount paid. This is illustrated in the example below:

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation6 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 4, 29) // evaluate the schedule on day 5
            InterestConfig.Method = Interest.Method.AddOn // use the add-on interest method
        }
        ValueNone // no settlement quotation requested
        true // clip unrequired payments from the end of the schedule
        (Map [
            5<OffsetDay>, [| ActualPayment.quickConfirmed 1050_00L<Cent> |]
        ]) // single overpayment made on day 5

(**
</div>
</details>
*)

(*** hide ***)
amortisation6
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
Here, the customer has paid £1050.00 on day 5, which is greater than the settlement figure of £1039.90. The lender would need to refund the customer the difference of £10.10.
This is a rare case, as the lender would normally expect to receive the settlement figure and not more than this.

## Can customers make partial payments? Can the customer make a payment of any amount towards the amount owed?

Yes, this is possible, though it may increase the total interest accrued. The schedule will automatically calculate the balances, but if the schedule is not fully amortised,
the customer will need to pay off the remaining balance at some point in the future. 

## What is the minimum partial payment a customer can make?

There is no minimum partial payment, but payment processors may have a minimum amount that they will accept.

## How does refinancing work?

There are two types of refinancing:

* Rescheduling, i.e. adding new scheduled payments to the end of the schedule
* Rolling over, i.e. creating a new loan with any outstanding balances as the new principal balance

> Note: both of these options may be limited by regulatory requirements, particularly in relation to responsible lending. There may be limits on the number of times
> a loan can be refinanced, and the amount of interest that can be charged. This is outside the scope of this library.

Let's take a look at these two options in more detail.

For both of these scenarios, we will take the same example as above, where a customer has taken a loan of £1000 advanced on 24 April 2025, paid back over 4 months
starting one month after the advance date. The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount.
We'll use the add-on interest method, though the rescheduling/rollover functions work identically for both methods, just the interest amounts are different. The 
customer already paid the first two payments on time, but missed the remaining two payments. The customer has requested refinancing on day 152.
<details>
<summary>Show/hide code</summary>
<div>
*)

let refinanceExampleParameters =
    { parameters with
        EvaluationDate = Date(2025, 9, 23) // evaluate the schedule on day 152
        InterestConfig.Method = Interest.Method.AddOn // use the add-on interest method
    }
let actualPayments =
    Map [
        30<OffsetDay>, [| ActualPayment.quickConfirmed 454_15L<Cent> |]
        61<OffsetDay>, [| ActualPayment.quickConfirmed 454_15L<Cent> |]
    ] // actual payments made on days 30 and 61
let refinanceExampleSchedule =
    Amortisation.generate
        refinanceExampleParameters
        SettlementDay.SettlementOnEvaluationDay // settlement quotation requested on day 152
        false // don't clip unrequired payments from the end of the schedule
        actualPayments

(**
</div>
</details>

Here is the status of the schedule on day 152 prior to any refinancing, where a settlement quotation has been requested so the interest is capitalised:
*)

(*** hide ***)
refinanceExampleSchedule
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
### Rescheduling

Here, the customer has agreed to pay £50 per week from 1 October 2025.

<details>
<summary>Show/hide code</summary>
<div>
*)

(*** hide ***)
open Rescheduling
(*** ***)
let rescheduleParameters : RescheduleParameters = {
    FeeSettlementRebate = Fee.SettlementRebate.Zero // no fees, so irrelevant
    PaymentSchedule =
        FixedSchedules [|
            {
                UnitPeriodConfig = Weekly(1, Date(2025, 10, 1)) // weekly payments starting on 1 October 2025
                PaymentCount = 100 // more than enough payments to cover the schedule (this will be automatically curtailed)
                PaymentValue = 50_00L<Cent> // £50 per week
                ScheduleType = ScheduleType.Rescheduled 152<OffsetDay> // indicate that rescheduling was requested on day 152
            }
        |]
    RateOnNegativeBalance = Interest.Rate.Zero // no negative balance, so irrelevant
    PromotionalInterestRates = [||] // no promotional rates
    SettlementDay = ValueNone //no settlement requested, just generate a statement
}
let rescheduleSchedules = reschedule refinanceExampleParameters rescheduleParameters actualPayments

(**
</div>
</details>

The rescheduled amortisation is as follows:
*)

(*** hide ***)
rescheduleSchedules.NewSchedules.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
<br />
> There are some interesting points to note:
> 
> * The first rescheduled payments, from day 160 to day 237, are paying off the interest balance, and only then does the principal balance start to be paid off.
> * On day 202, the new interest drops significantly and falls to zero on all subsequent days. This is because the 100% interest cap has been reached.

### Rollover

Here, the customer has agreed to roll over the loan to a new 8-month loan, starting on 1 October 2025.

<details>
<summary>Show/hide code</summary>
<div>
*)

let originalFinalPaymentDay = // get the final payment day from the original schedule
    refinanceExampleSchedule.AmortisationSchedule.ScheduleItems
    |> Map.filter (fun _ si -> ScheduledPayment.isSome si.ScheduledPayment)
    |> Map.maxKeyValue
    |> fst
let rolloverParameters : RolloverParameters = {
    OriginalFinalPaymentDay = originalFinalPaymentDay
    PaymentSchedule =
        AutoGenerateSchedule {
            UnitPeriodConfig = Monthly(1, 2025, 10, 1) // monthly payments starting on 1 October 2025
            ScheduleLength = PaymentCount 8 // 8 payments
        }
    InterestConfig = refinanceExampleParameters.InterestConfig // use the same interest config as the original schedule
    PaymentConfig = refinanceExampleParameters.PaymentConfig // use the same payment config as the original schedule
    FeeHandling = Fee.FeeHandling.CarryOverAsIs // no fees, so irrelevant
}
let rolloverSchedules = rollOver refinanceExampleParameters rolloverParameters actualPayments

(**
</div>
</details>

The old loan would be closed as per the settlement quote above. The new rolled-over loan amortisation schedule is as follows:
*)

(*** hide ***)
rolloverSchedules.NewSchedules.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
<br />
> There are some interesting points to note:
> 
> * The settlement figure is £1091.70, which is greater than the original loan amount due to the capitalised interest.
> * By comparison, the settlement figure on day 152 when rescheduling (rather than rolling over) is only £908.64, because the interest is not capitalised.
> * On day 131 of the new loan, you can see that the simple interest hits the 100% total cap, and interest is no longer accrued from that point on.


### What happens to the payment amounts?

Payment amounts can be:

* Automatically calculated to amortise the loan over the new schedule, or
* Set to a fixed periodical amount, or
* Set to an arbitrary set of scheduled payments

### What happens to the loan term?

Depending on the payment schedule, the loan term will either be amortised over a set number of payments, or the number of payments will be determined by
how long it takes to amortise the schedule based on the payment amount.

## If forbearance is offered, how is this calculated?

The schedule can be partially or fully paid off by using write-off payments.

Let's take our simple-interest loan, where the customer has already made the first two payments on time.

### Single-payment write-off

<details>
<summary>Show/hide code</summary>
<div>
*)

let amortisation7 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 7, 3) // evaluate the schedule on day 70
        }
        ValueNone // no settlement quotation requested
        false // don't clip unrequired payments from the end of the schedule
        (Map [
            30<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickWriteOff 417_72L<Cent> |]
        ]) // actual payments made on days 30 and 61, and a single-payment write-off on day 91

(**
</div>
</details>
*)

(*** hide ***)
amortisation7
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that the single-payment write-off has no effect on the remainder of the schedule, and the remaining payment is still due on day 122.

### Full write-off

<details>
<summary>Show/hide code</summary>
<div>
*)

// first, run the amortisation with the existing actual payments to get the settlement figure
let amortisation8 =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 7, 3) // evaluate the schedule on day 70
        }
        (SettlementDay.SettlementOn 91<OffsetDay>) // settlement quotation requested on day 91
        false // don't clip unrequired payments from the end of the schedule
        (Map [
            30<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
        ]) // actual payments made on days 30 and 61, and a single-payment write-off on day 91
// get the generated settlement figure
let settlementFigure = amortisation8.AmortisationSchedule.FinalStats.SettlementFigure
// use the settlement figure as the full write-off amount
let fullWriteOffAmount = settlementFigure |> Option.map snd |> Option.defaultValue 0L<Cent>
// run the amortisation again with the full write-off payment
let amortisation8' =
    Amortisation.generate
        { parameters with
            EvaluationDate = Date(2025, 7, 3) // evaluate the schedule on day 70
        }
        (SettlementDay.SettlementOn 91<OffsetDay>) // settlement quotation requested on day 91
        false // don't clip unrequired payments from the end of the schedule
        (Map [
            30<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
            61<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
            91<OffsetDay>, [| ActualPayment.quickWriteOff fullWriteOffAmount |]
        ]) // actual payments made on days 30 and 61, and a full write-off on day 91

(**
</div>
</details>
*)

(*** hide ***)
amortisation8'
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that a settlement payment of £752.58 was determined on day 91, and this is used as the write-off amount. This amount is less than the remaining two
payments (£417.72 + £417.69 = £835.41) as this is effectively an early settlement and so less interest is accrued. The remaining payment on day 122 is no longer
required as the schedule is fully amortised.

## Can interest be frozen for a period of time?

Yes, the library supports promotional interest rates, essentially ranges of dates during which a different interest rate is applied. To freeze interest, these
can be used with the interest rate set to zero. This is illustrated in the example below:

<details>
<summary>Show/hide code</summary>
<div>
*)


// first, run the amortisation with the existing actual payments to get the settlement figure
let amortisation9 =
    Amortisation.generate
        { parameters with
            InterestConfig.PromotionalRates = [|
                { // promotional rate to freeze interest for a month
                    DateRange = { Start = Date(2025, 6, 25); End = Date(2025, 7, 24) }
                    Rate = Interest.Rate.Zero // zero interest rate
                }
            |]
        }
        ValueNone // no settlement quotation requested
        false // don't clip unrequired payments from the end of the schedule
        Map.empty // no actual payments made

(**
</div>
</details>
*)

(*** hide ***)
amortisation9
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div class="schedule">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that the interest accrued over the month up to the payment on day 91 is zero. The scheduled payments are lowered to account for this as we've set this
up from the start of the loan.

It would be equally possible to set this up after a schedule has started, but depending on the actual promotional rates applied, this might end up requiring a
rebate of existing interest paid or even a refund to the customer if it meant that existing payments exceeded the revised settlement figure. The library would
automatically calculate this, but it would be up to the lender to decide how to handle it.

## When does accrued interest start?

Accrued interest starts from the first day of the schedule, i.e. the advance date.

The parameters used in these examples have a 3-day initial grace period, meaning that if the customer pays back the principal amount within 3 days of the advance date,
no interest is charged. If they wanted to settle the schedule after this date, interest would be charged from the advance date.

If there were no initial grace period, a customer could pay back the principal amount on the advance date and no interest would be charged. If they wanted to settle
the schedule after this date, interest would be charged from the advance date. So if they wanted to settle the schedule on day 1, one day's interest would be charged.

## Do we allow up until 11:59pm for payment to be made on due date of the scheduled repayment?

The library does not have a concept of time, so all dates are treated as whole days. It is up to the lender to decide how to handle this, e.g. deciding on what day
to record any payment as having been made.

The parameters used in these examples have a 3-day payment timeout, allowing us to mark payments as pending for up to 3 days after the scheduled payment date. This
means that if a payment is made on day 30, it will be marked as pending until day 33. If the payment is not made by then, it will be marked as missed.

## Can a customer delay one payment and keep all subsequent payments the same?

This kind of scenario could be handled in a number of ways:

* The simplest way would be to book the actual payment as having been made on the due date. This would preserve the original schedule and interest totals.
* With the add-on interest method, if a payment is made before the next one is due, and while the principal balance is still at the initial amount, no extra interest would be accrued.
* Alternatives involving refinancing would require careful calculation to ensure the remaining payments are the same.
* Otherwise, if extra interest is charged to the customer, the loan would not be amortised and extra payment is unavoidable.
*)