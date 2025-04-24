(**
---
title: Amortisation FAQ
category: Compliance
categoryindex: 4
index: 4
description: Detailed description of amortisation calculations
---

# Amortisation FAQ

<details>
<summary>How is the interest calculated?</summary>
<div>
#### Initial calculations

When setting up a schedule, interest is calculated based on a simple schedule that assumes that all payments will be made on time and in full.

* For simple-interest calculations, see [Simple-Interest Calculations](interestSimple.fsx)
* For add-on-interest calculations, see [Add-On-Interest Calculations](interestAddOn.fsx)

#### Running calculations

While a schedule is active (i.e. it has started and the principal balance is non-zero), the interest is calculated based on the scheduled and
actual payments made. The day of any scheduled or actual payment constitutes an event in the amortisation schedule, and simple interest is
calculated based on the principal (+ fees) balance and the number of days since the last event.

For full details, see [AmortisationCalculations](amortisation.fsx).
</div>
</details>
*)

(*** hide ***)
#r "nuget:FSharp.Finance.Personal"
open FSharp.Finance.Personal
open Calculation
open DateDay
open Scheduling
open UnitPeriod

let parameters = {
    AsOfDate = Date(2025, 4, 24)
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
            TotalAmount = ValueSome (Amount.Percentage (Percent 100m, Restriction.NoLimit))
            DailyAmount = ValueSome (Amount.Percentage (Percent 0.8m, Restriction.NoLimit))
        }
        InitialGracePeriod = 3<DurationDay>
        PromotionalRates = [||]
        RateOnNegativeBalance = Interest.Rate.Zero
        Rounding = RoundDown
        AprMethod = Apr.CalculationMethod.UnitedKingdom 3
    }
}

(**
<details>
<summary>What happens if a customer were to not make their repayment on time?</summary>
<div>
Let's take a look at the amortisation schedules to illustrate this. Let's define a loan of £1000 advanced on 24 April 2025, paid back over 4 months starting
one month after the advance date. The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount.
First we will look at the simple-interest method and then the add-on-interest method.

<br />
> Note: As a general principal, for payments that are not yet due it is assumed that they will be paid on time and in full. This is to provide for a more
> realistic projection of the schedule. 

#### Simple Interest

Here's the schedule prior to any actual payments being made (looking at it from day 0). The main thing to note is that the principal balance at the end of the
schedule is zero, meaning it is fully amortised.
*)

(*** hide ***)
Amortisation.generate parameters ValueNone false Map.empty
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
Now, let's assume that it's day 35 and no payments have been made, so the payment due on day 30 has been missed. The schedule would look like this:
*)

(*** hide ***)
Amortisation.generate { parameters with AsOfDate = Date(2025, 5, 29) } ValueNone false Map.empty
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that a new event at day 35 has been created, as we are observing the schedule on that day. This capitalises the interest that has accrued
so far so that we can see the principal balance on that day.

As the payment due on day 30 has been missed, the interest that has accrued is not paid off and is added to the interest balance. This means that any subsequent
payments would need to clear the interest balance before the principal balance is reduced, and this means that the principal balance remains higher for longer.
Looking at the principal balance at the the end of the schedule shows that this has not-insubstantial consequences, as there is still £693.40 outstanding.

#### Add-On Interest

Here's the schedule prior to any actual payments being made (looking at it from day 0). This time the interest is added up-front, with an initial interest
balance of £815.56. The interest is paid off before the principal, so more interest is accrued in total than under the simple-interest method. The principal
at the end of the schedule is zero, meaning it is fully amortised.
*)

(*** hide ***)
Amortisation.generate { parameters with InterestConfig.Method = Interest.Method.AddOn } ValueNone false Map.empty
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
Let's again assume that it's day 35 and no payments have been made, so the payment due on day 30 has been missed. The schedule would look like this:
*)

(*** hide ***)
Amortisation.generate { parameters with AsOfDate = Date(2025, 5, 27); InterestConfig.Method = Interest.Method.AddOn } ValueNone false Map.empty
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that a new event at day 35 has been created, as we are observing the schedule on that day. This capitalises the interest that has accrued
so far so that we can see the principal balance on that day.

As the payment due on day 30 has been missed, the interest balance stays higher for longer, meaning that the principal balance remains higher for longer.
This means that more interest is accrued in total than the initial interest balance. To correct for this, new interest of £134.30 is added to the final
schedule item. The principal balance at the end of the schedule is therefore £588.45.

> ##### Why is the final settlement amount lower for the add-on method?
> You may well wonder, given that the add-on-interest method results in a higher amount of interest accruing than simple-interest method, why the final
> settlement amount (i.e. the final principal balance) is lower for the add-on method.
> <br /><br />
> One interesting feature of the add-on method is that in the early days of the schedule, missed payments have no effect on the principal balance. Looking
> at the original add-on schedule, we see that the principal balance remains at £1000 until day 30. Any extra interest is only accrued if the principal
> balance remains outstanding at this level for longer than this. Looking at the schedule on day 35, we see that the principal balance remains at £1000
> until day 61, meaning that extra interest is accrued, but only for 31 days out of 60 compared to the simple-interest method.

> ##### Why is the new interest only added at the very end of the schedule?
> During the schedule, actual-payment amounts and timings may vary, some of which may well have an effect on the total interest accrued. If we were to make
> constant adjustments to the interest balance, this would become difficult to track. If subsequent payments were paid earlier than due this could even lead
> to a situation where interest was overpaid and a refund might be required. By making the interest adjustment at the end, we can avoid this situation.
</div>
</details>

<details>
<summary>How is the additional interest calculated?</summary>
<div>
#### Simple Interest
Given that the interest rate is usually fixed for the duration of the schedule, the interest accrued is purely a function of the principal balance and
how many days it is outstanding:

<div class="mermaid">
    graph LR
        A["$$\text{interest} = \text{principal} \times \text{daily interest rate} \times \text{days}$$"]
</div>

Therefore any variations will affect the interest accrued:

* A missed or late payment, will increase the number of days and therefore increase the total interest
* An underpayment will leave the principal at a higher than expected level and therefore increase the total interest
* An overpayment or early settlement will reduce both the principal and number of days outstanding and therefore decrease the overall interest.

#### Add-On Interest
Though the interest is calculated up-front and added to the schedule as an initial interest balance, adjustments will need to be made if the payment
schedule is not adhered to. At the end of the schedule, the total simple interest (calculated as stated in the Simple Interest section above) is
compared to the initial interest balance and a correction is made to the final schedule item if necessary.
</div>
</details>

<details>
<summary>Is the additional interest calculated on the outstanding loan principal?</summary>
<div>
Indirectly, yes. At the end of the schedule, the total simple interest (calculated as stated in the Simple Interest section above) is
compared to the initial interest balance and a correction is made to the final schedule item if necessary.
</div>
</details>

<details>
<summary>Where is the additional interest added?</summary>
<div>
For simple-interest, the interest is automatically adjusted during the schedule. For add-on interest, the adjustment is made at the end of the schedule.
</div>
</details>

<details>
<summary>When is the customer expected to pay off this additional interest?</summary>
<div>
If additional interest has accrued, the effect of this will be that the schedule is not fully amortised, i.e. the final principal balance is not zero.
How this is handled is outside the scope of the library, and depends on business rules. There are a number of ways, including:

* Adding the outstanding principal balance to the final payment amount
* Rescheduling the loan, e.g. by adding new scheduled payments to the end of the schedule
* Rolling over the loan, i.e. creating a new loan with any outstanding balances as the new principal balance
</div>
</details>

<details>
<summary>Is there a set maximum amount of days that are used as a “limit” for additional interest to be charged?</summary>
<div>
No, caps on interest charges are not based on the number of days, but on the interest caps set in the loan parameters.
Both daily and total caps can be set, and these are defined as either a simple amount or a percentage of the principal amount.
</div>
</details>

<details>
<summary>What happens when a customer settles earlier than the agreed term?</summary>
<div>
Let's take a look at the amortisation schedules to illustrate this. Let's define a loan of £1000 advanced on 24 April 2025, paid back over 4 months starting
one month after the advance date. The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount.
First we will look at the simple-interest method and then the add-on-interest method.

#### Simple Interest

Here's the schedule prior to any actual payments being made (looking at it from day 0).
*)

(*** hide ***)
Amortisation.generate parameters ValueNone false Map.empty
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
Now, let's assume that the first two payments have been made on time, and the customer decides to repay in full on day 70. The schedule would look like this:
*)

(*** hide ***)
let actualPayments =
    Map [
        30<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
        61<OffsetDay>, [| ActualPayment.quickConfirmed 417_72L<Cent> |]
    ]
Amortisation.generate { parameters with AsOfDate = Date(2025, 7, 3) } (ValueSome SettlementDay.SettlementOnAsOfDay) false actualPayments
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that a new event at day 70 has been created, as we are observing the schedule on that day, and have requested a settlement quotation on that day too.

The settlement quotation is £650.83, which is the sum required to pay off all outstanding balances, including any interest accrued up to that day. This fully amortises
the schedule, and the remaining two payments on days 91 and 122 are no longer required. The total of the two payments is £835.41, meaning that the customer has saved
£184.58 in interest.

#### Add-On Interest

Here's the schedule prior to any actual payments being made (looking at it from day 0). This time the interest is added up-front, with an initial interest
balance of £815.56.
*)

(*** hide ***)
Amortisation.generate { parameters with InterestConfig.Method = Interest.Method.AddOn } ValueNone false Map.empty
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
Let's assume again that the first two payments have been made on time, and the customer decides to repay in full on day 70. The schedule would look like this:
*)

(*** hide ***)
let actualPayments' =
    Map [
        30<OffsetDay>, [| ActualPayment.quickConfirmed 454_15L<Cent> |]
        61<OffsetDay>, [| ActualPayment.quickConfirmed 454_15L<Cent> |]
    ]
Amortisation.generate { parameters with AsOfDate = Date(2025, 7, 3); InterestConfig.Method = Interest.Method.AddOn } (ValueSome SettlementDay.SettlementOnAsOfDay) false actualPayments'
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
You can see that a new event at day 70 has been created, as we are observing the schedule on that day, and have requested a settlement quotation on that day too.

The settlement quotation is £643.71, which is the sum required to pay off all outstanding balances, including any interest accrued up to that day. As this is an early
settlement, and the interest for the full schedule was charged up-front, an interest rebate of £264.55 has been calculated. The principal balance of £908.26, minus the
interest rebate of £264.55, leaves a final settlement payment of £643.71 to pay. This would fully amortise the schedule, and the remaining two payments on days 91 and
122 are no longer required. In contrast to the simple-interest method, where you have to add up the unrequired payments and deduct the settlement figure to calculate the
saved interest, in the add-on-interest method the interest rebate is explicitly calculated.
</div>
</details>

<details>
<summary>Is overcharged interest refunded?</summary>
<div>
As shown above, if a customer settles early and has overpaid interest, this is rebated as part of the settlement quotation. It would be termed a _rebate_ rather than a _refund_,
as the settlement quotation is generally a positive amount and so is a net flow from the customer to the lender.

The exception to this is if the customer has somehow managed to overpay, i.e. to pay an amount greater than the settlement figure. In this case, the lender would need to
refund the customer the difference between the settlement figure and the amount paid. This is illustrated in the example below:
*)

(*** hide ***)
let actualPayments'' =
    Map [
        5<OffsetDay>, [| ActualPayment.quickConfirmed 1050_00L<Cent> |]
    ]
Amortisation.generate { parameters with AsOfDate = Date(2025, 4, 29); InterestConfig.Method = Interest.Method.AddOn } ValueNone false actualPayments''
|> _.AmortisationSchedule
|> Amortisation.Schedule.toHtmlTable
|> fun html -> $"""<div style="overflow-x: auto;">{html}</div>"""

(*** include-it-raw ***)

(**
Here, the customer has paid £1050.00 on day 5, which is greater than the settlement figure of £1039.90. The lender would need to refund the customer the difference of £10.10.
This is a rare case, as the lender would normally expect to receive the settlement figure and not more than this.
</div>
</details>

<details>
<summary>Can customers make partial payments? Can the customer make a payment of any amount towards the amount owed?</summary>
<div>
Yes, this is possible, though it may increase the total interest accrued. The schedule will automatically calculate the balances, but if the schedule is not fully amortised,
the customer will need to pay off the remaining balance at some point in the future. 
</div>
</details>

<details>
<summary>What is the minimum partial payment a customer can make?</summary>
<div>
There is no minimum partial payment, but payment processors may have a minimum amount that they will accept.
</div>
</details>

<details>
<summary>How does refinancing work?</summary>
<div>
There are two types of refinancing:

* Rescheduling, i.e. adding new scheduled payments to the end of the schedule
* Rolling over, i.e. creating a new loan with any outstanding balances as the new principal balance

Let's take a look at these two options in more detail.

#### Rescheduling

#### Rollover

</div>
</details>

<details>
<summary>What happens to the repayment amounts?</summary>
<div>
</div>
</details>

<details>
<summary>What happens to the loan term?</summary>
<div>
</div>
</details>

<details>
<summary>If forbearance is offered, how is this calculated?</summary>
<div>
</div>
</details>

<details>
<summary>Can interest be frozen for a period of time?</summary>
<div>
</div>
</details>

## Example Scenarios

### Example Scenario 1:

A customer has taken a £400 loan over 4 months with us but has paid off the loan in 2 months (60 days).

Do we calculate a “new interest” based on 60 days?

If so, what calculation is used for the “new interest”?

Do we refund the customer on overpaid interest?

For example: (old interest) - (new interest) = interest refund.

If a customer is due a refund:

How do we inform the customer they are due a refund?

Do the payments team manually pay this refund into the bank account that was registered at the time the loan was taken?

How long does it take to process the refund?

### Example Scenario 2:

A customer has taken a £400 over 4 month loan with us but has not paid the first scheduled repayment.

When does the accrued interest start?

Is it the due date of the scheduled repayment?

Do we allow up until 11:59pm for payment to be made on due date of the scheduled repayment?

Is it the day after the due date of the scheduled repayment?

How is the additional interest calculated?

Is the additional interest calculated on the outstanding loan principal? (e.g. additional interest is calculated on the outstanding loan principal. If at the time the customer misses a repayment and their repayments have not started to pay off the outstanding loan principal, then the customer will be charged interest on their original loan principal.)

Where is the additional interest added in terms of repayments? (e.g. additional interest is added onto the interest that is currently owed. As the Interest First Method is being used, this repayments on the loan will first go towards paying off the accrued interest and future interest before any principal amount is paid off.)

Is there a max amount of days that we allow for the interest to be added?

Will the additional interest stop accumulating at the next scheduled repayment date?

Is it a case that if the customer misses the first scheduled repayment and there are 30 days until the second scheduled repayment, then the interest on the first repayment will stop at the 30 days?

### Example Scenario 3:

A customer has taken a £400 over a 4 month loan with us but has not paid the first or second repayments.

Does the accrued interest from the first missed repayment stop on the due date of the second scheduled repayment date and then interest on the missed second repayment begins?

Does the accrued interest from the first missed repayment run alongside the accrued interest from the second missed repayment and continue to accumulate until a repayment is made?

How would the additional interest be charged for 2 missed repayments in a row?

What happens to the repayment schedule if 2+ repayments are missed?

### Example Scenario 4:

A customer has taken a loan for £250 over 4 months on 18/03/2025. The customer wants to make a partial payment towards their loan before the first scheduled repayment date (20/03/2025). They log in to the online customer portal and there are two options for them when it comes to making a partial repayment. Option 1: Next Payment (£102.81) or Total Sum (£253.99) will cover your total sum of £411.24.

How is the Total Sum calculated?

Why do we not offer the customer to make a payment amount of their choice with a minimum payment set in the online portal?

For example: another option the customer can select, “make payment (minimum £10)”:

Can the customer make a partial payment less than the next payment amount?

If so, how?

If so, what happens to the repayment schedule?

### Example Scenario 5:

A customer has taken a loan for £400 over 4 months and has made the first two payments on time. However, due to unforeseen circumstances, they can no longer afford the repayments of their loan. They have reached out to the customer service team to explain this.

Do we freeze interest for a set period of time that has been agreed between the customer service team and the customer?

Do we still accrue interest on the loan and run the risk of irresponsible lending with the loan becoming unaffordable for the customer?

What happens to the repayment schedule?

### Example Scenario 6:

A customer has taken a loan for £400 over 4 month and has made the first repayment on time but cannot afford the next/second scheduled payment. They have contacted customer service to delay the second scheduled repayment but are happy to keep the following month’s payments remaining the same?

How would the refinancing work for this?

What interest is the customer charged for this?

What happens to the repayment schedule?
*)