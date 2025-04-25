(**
---
title: Add-On-Interest Calculations
category: Compliance
categoryindex: 4
index: 2
description: Detailed description of add-on interest calculations
---

# Add-On-Interest Calculations

## Definition

Add-on interest is a method of calculating interest where the total interest accrued over the entire schedule is added to the beginning of the schedule,
as an initial interest balance. Given that payments are applied in the order charges -> interest -> fees -> principal, this means that the interest
balance is reduced before the principal balance, and therefore the principal balance remains higher for longer.

## Relevant Code

The `cref:T:FSharp.Finance.Personal.Scheduling` module contains the functions that create the initial schedule. The initial schedule is a simple schedule that allows
us to calculate the interest accrued over the schedule as well as the level and final payments.

Let's start by defining the parameters. Let's define a loan of £1000 advanced on 22 April 2025, paid back over 4 months starting one month after the advance date.
The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount. Interest is calculated using the add-on method. 

<br />
<details>
<summary>Show/hide parameters</summary>
*)

(*** hide ***)
#r "nuget:FSharp.Finance.Personal"
open FSharp.Finance.Personal
open Calculation
open DateDay
open Scheduling
open UnitPeriod
(*** ***)
let parameters = {
    EvaluationDate = Date(2025, 4, 22)
    StartDate = Date(2025, 4, 22)
    Principal = 1000_00L<Cent>
    ScheduleConfig = AutoGenerateSchedule {
        UnitPeriodConfig = Monthly(1, 2025, 5, 22)
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
        Method = Interest.Method.AddOn
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
</details>

Then we call the `cref:M:FSharp.Finance.Personal.Scheduling.calculate` function to generate the schedule:
*)

let schedule = Scheduling.calculate parameters
(*** hide ***)
schedule |> SimpleSchedule.toHtmlTable

(*** include-it-raw ***)

(**
The schedule has calculated an initial interest balance of £816.56. We can see that the interest balance is paid off before the principal, meaning that the full principal
remains outstanding for two months. Given that interest is accrued on the principal balance only (no interest on interest), maintaining a higher principal balance for longer
means that the interest accrued is higher than it would be if the principal was paid off first.

<br />
<details>
<summary>Simple-interest comparison (click to expand)</summary>
To illustrate this, we can compare the add-on-interest schedule with a simple-interest schedule:
*)

let simpleInterestSchedule = Scheduling.calculate { parameters with InterestConfig.Method = Interest.Method.Simple }
(*** hide ***)
simpleInterestSchedule |> SimpleSchedule.toHtmlTable

(*** include-it-raw ***)

(**
Here, as there is no initial interest balance, the principal starts to be paid off immediately, and the total interest accrued is therefore lower. 
</details>

## Calculation Details

`cref:M:FSharp.Finance.Personal.Scheduling.calculate` is the function that generates the schedule. Here is a summary of the calculation steps:

1. **Generate payment days**: generate the payment days based on the unit-period (e.g. monthly) and the first payment date
2. **Estimate total interest**: estimate the total interest accruing over the entire schedule 
3. **Solve for payment values**: use the bisection method to determine the level payments required
4. **Iterate to maximise interest**: for the add-on interest method, iterate until the initial interest matches the total interest
5. **Tweak final payment**: ensure the final payment fully amortises the schedule

Let's look at each of these items in more detail.

### Step 1: Generate payment days

Here we take the schedule config from the parameters and generate the payment days. In this example, the schedule is auto-generated, so the
`cref:M:FSharp.Finance.Personal.Scheduling.generatePaymentMap` function takes the start date of the schedule, the unit period and the first payment date, and
generates the payment days. It is also possible to specify the payment days manually or specify multiple ranges of dates at different intervals. However, the
auto-generated schedule is the most common use case, and ensures respect for varying month lengths and month-end tracking dates.
*)

let paymentMap = generatePaymentMap parameters.StartDate parameters.ScheduleConfig
let paymentDays = paymentMap |> Map.keys |> Seq.toArray

(**Result:*)

(*** hide ***)
paymentDays |> Array.map string |> String.concat ", "

(*** include-it-raw ***)

(**
### Step 2: Estimate total interest

The `cref:M:FSharp.Finance.Personal.Scheduling.totalAddOnInterest` function calculates the total interest by taking

principal × dailyInterestRate × totalNumberOfDays

and capping this at 100% of the principal amount (as specified in the parameters under the interest `cref:T:FSharp.Finance.Personal.Interest.Cap`).

*)

let finalScheduledPaymentDay = paymentDays |> Array.tryLast |> Option.defaultValue 0<OffsetDay>
let initialInterestBalance = totalAddOnInterest parameters finalScheduledPaymentDay

(**Result:*)

(*** hide ***)
initialInterestBalance |> Cent.toDecimal |> fun m -> $"{m:N2}"

(*** include-it-raw ***)

(**
### Step 3: Solve for payment values

Determining the payment values requires the use of a solver, because payment values determine how much principal is paid off each unit-period, and therefore how
much interest is accrued, which in turn affects the payment values. We use the bisection method (`cref:M:FSharp.Finance.Personal.Calculation.Array.solveBisection`)
for this. This method runs a generator function (`cref:M:FSharp.Finance.Personal.Scheduling.generatePaymentValue`) on the schedule, which calculates the final
principal balance for a given payment value. The bisection method then iteratively narrows down the level payment value until the final principal balance is close
to zero (usually just below zero, so the final payment can be slightly smaller). To make the iteration more efficient, we use an initial guess for the payment value,
which is calculated based on the estimated total interest and the number of payments. (In this instance, the initial guess is actually the correct payment value,
as the schedule is simple, but for more complex schedules, several iterations may be required.)

<br />
<details>
<summary>Show/hide code</summary>
*)

// precalculations
let firstItem = { SimpleItem.initial with InterestBalance = initialInterestBalance; PrincipalBalance = parameters.Principal }
let paymentCount = Array.length paymentDays
let interest = Cent.toDecimalCent initialInterestBalance
let roughPayment =
    calculateLevelPayment paymentCount parameters.PaymentConfig.Rounding parameters.Principal 0L<Cent> interest
    |> Cent.toDecimalCent
    |> decimal
// the following calculations are part of `cref:M:FSharp.Finance.Personal.Scheduling.generatePaymentValue` but modified to show the intermediate steps
let scheduledPayment =
    roughPayment
    |> Cent.round parameters.PaymentConfig.Rounding
    |> fun rp -> ScheduledPayment.quick (ValueSome rp) ValueNone
let simpleItems =
    paymentDays
    |> Array.scan(fun simpleItem pd ->
        generateItem parameters parameters.InterestConfig.Method scheduledPayment simpleItem pd
    ) firstItem

(**
</details>
*)

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = simpleItems; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> SimpleSchedule.toHtmlTable

(*** include-it-raw ***)

(**
### Step 4: Iterate to equalise interest

Now the schedule days and payment values are known, we iterate through the schedule until the final principal balance is zero.

This step is required because the initial interest balance is non-zero, meaning that any payments are apportioned to interest first, meaning that
the principal balance is paid off at a difference pace than it would otherwise be; this, in turn, generates different interest, which leads to a
different initial interest balance, so the process must be repeated until the total interest and the initial interest are equalised.

<br />
<details>
<summary>Show/hide code</summary>
*)

let finalInterestTotal =
    simpleItems
    |> Array.last
    |> _.TotalSimpleInterest
let simpleItems' =
    ValueSome { Iteration = 0; InterestBalance = finalInterestTotal }
    |> Array.unfold (equaliseInterest parameters paymentDays firstItem paymentCount 0L<Cent> paymentMap)
    |> Array.last

(**
</details>
*)

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = simpleItems'; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> SimpleSchedule.toHtmlTable

(*** include-it-raw ***)

(**
### Step 5: Tweak final payment

The final payment is adjusted (`cref:M:FSharp.Finance.Personal.Scheduling.adjustFinalPayment`) to ensure that the final principal balance is zero.
*)

let items =
    simpleItems'
    |> adjustFinalPayment finalScheduledPaymentDay parameters.ScheduleConfig.IsAutoGenerateSchedule

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = items; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> SimpleSchedule.toHtmlTable

(*** include-it-raw ***)

(**
As an extra step, the library calculates a number of statistics for the schedule, including the total interest accrued, the total fees and charges,
the total payments made, and the final principal balance. The full output for this schedule, including stats, is available in the Output section in
the page [Unit-Test Outputs](unitTestOutput.fsx), under Compliance. This particular example is defined as [ComplianceTest022](content/Compliance/ComplianceTest022.md).
*)
