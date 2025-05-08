(**
---
title: Actuarial-Interest Calculations
category: Compliance
categoryindex: 4
index: 1
description: Detailed description of actuarial interest calculations
---

# Actuarial-Interest Calculations

## Definition

Actuarial interest is a method of calculating interest where the interest is accrued during the schedule. The initial interest balance is zero, and each time
a scheduled payment is due or an actual payment is made, the interest is calculated based on the outstanding principal balance and the number of days it has
been outstanding. Payments are applied in the order charges -> interest -> fees -> principal, but as interest is not front loaded (as in the
[add-on interest method](interestAddOn.fsx)), this means each payment pays off the interest in full and then the principal balance is reduced. This means that
the principal balance is lower than under the add-on interest method, and therefore the interest accrued is lower. Calculating the interest this way is the
simplest method of calculating interest.

## Relevant Code

The `cref:T:FSharp.Finance.Personal.Scheduling` module contains the functions that create the initial schedule. The initial schedule is a basic schedule that allows
us to calculate the interest accrued over the schedule as well as the level and final payments.

Let's start by defining the parameters. Let's define a loan of £1000 advanced on 22 April 2025, paid back over 4 months starting one month after the advance date.
The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount. Interest is calculated using the actuarial method. 

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
let parameters : BasicParameters = {
    EvaluationDate = Date(2025, 4, 22)
    StartDate = Date(2025, 4, 22)
    Principal = 1000_00L<Cent>
    ScheduleConfig = AutoGenerateSchedule {
        UnitPeriodConfig = Monthly(1, 2025, 5, 22)
        ScheduleLength = PaymentCount 4
    }
    PaymentConfig = {
        LevelPaymentOption = LowerFinalPayment
        Rounding = RoundUp
    }
    FeeConfig = ValueNone
    InterestConfig = {
        Method = Interest.Method.Actuarial
        StandardRate = Interest.Rate.Daily (Percent 0.798m)
        Cap = {
            TotalAmount = Amount.Percentage (Percent 100m, Restriction.NoLimit)
            DailyAmount = Amount.Percentage (Percent 0.8m, Restriction.NoLimit)
        }
        Rounding = RoundDown
        AprMethod = Apr.CalculationMethod.UnitedKingdom 3
    }
}

(**
</details>

Then we call the `cref:M:FSharp.Finance.Personal.Scheduling.calculateBasicSchedule` function to generate the schedule:
*)

let actuarialInterestSchedule = calculateBasicSchedule parameters
(*** hide ***)
actuarialInterestSchedule |> BasicSchedule.toHtmlTable

(*** include-it-raw ***)

(**
As there is no initial interest balance, the principal starts to be paid off immediately, and the total interest accrued is therefore lower. 

<br />
<details>
<summary>Add-on-interest comparison (click to expand)</summary>
To illustrate this, we can compare the actuarial-interest schedule with an add-on-interest schedule:
*)

let addOnInterestSchedule =
    calculateBasicSchedule
        { parameters with InterestConfig.Method = Interest.Method.AddOn }
(*** hide ***)
addOnInterestSchedule |> BasicSchedule.toHtmlTable

(*** include-it-raw ***)

(**
Here, the schedule has calculated an initial interest balance of £816.56. We can see that the interest balance is paid off before the principal, meaning that the full principal
remains outstanding for two months. Given that interest is accrued on the principal balance only (no interest on interest), maintaining a higher principal balance for longer
means that the interest accrued is higher than it would be if the principal was paid off first.
</details>

## Calculation Details

`cref:M:FSharp.Finance.Personal.Scheduling.calculateBasicSchedule` is the function that generates the schedule. Here is a summary of the calculation steps:

1. **Generate payment days**: generate the payment days based on the unit-period (e.g. monthly) and the first payment date
2. **Solve for payment values**: use the bisection method to determine the level payments required
3. **Tweak final payment**: ensure the final payment fully amortises the schedule

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
### Step 2: Solve for payment values

Determining the payment values requires the use of a solver, because payment values determine how much principal is paid off each unit-period, and therefore how
much interest is accrued, which in turn affects the payment values. We use the bisection method (`cref:M:FSharp.Finance.Personal.Calculation.Array.solveBisection`)
for this. This method runs a generator function (`cref:M:FSharp.Finance.Personal.Scheduling.generatePaymentValue`) on the schedule, which calculates the final
principal balance for a given payment value. The bisection method then iteratively narrows down the level payment value until the final principal balance is close
to zero (usually just below zero, so the final payment can be slightly smaller). To make the iteration more efficient, we use an initial guess for the payment value,
which is calculated based on the estimated total interest and the number of payments. (In this instance, the initial guess is actually the correct payment value,
as the schedule is basic, but for more complex schedules, several iterations may be required.)

<br />
<details>
<summary>Show/hide code</summary>
*)

// precalculations
let firstItem = { BasicItem.initial with PrincipalBalance = parameters.Principal }
let paymentCount = Array.length paymentDays
let paymentRounding = parameters.PaymentConfig.Rounding
let principal = parameters.Principal
let roughPayment =
    calculateLevelPayment paymentCount paymentRounding principal 0L<Cent> 0m<Cent>
    |> Cent.toDecimalCent
    |> decimal
// the following calculations are part of `Scheduling.generatePaymentValue`
// but modified to show the intermediate steps
let scheduledPayment =
    roughPayment
    |> Cent.round parameters.PaymentConfig.Rounding
    |> fun rp -> ScheduledPayment.quick (ValueSome rp) ValueNone
let interestMethod = parameters.InterestConfig.Method
let basicItems =
    paymentDays
    |> Array.scan(fun basicItem pd ->
        generateItem parameters interestMethod scheduledPayment basicItem pd
    ) firstItem

(**
</details>
*)

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = basicItems; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> BasicSchedule.toHtmlTable

(*** include-it-raw ***)

(**
### Step 3: Tweak final payment

The final payment is adjusted (`cref:M:FSharp.Finance.Personal.Scheduling.adjustFinalPayment`) to ensure that the final principal balance is zero.
*)

let finalScheduledPaymentDay =
    paymentDays
    |> Array.tryLast
    |> Option.defaultValue 0<OffsetDay>
let isAutoGenerateSchedule = parameters.ScheduleConfig.IsAutoGenerateSchedule
let items =
    basicItems
    |> adjustFinalPayment finalScheduledPaymentDay isAutoGenerateSchedule

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = items; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> BasicSchedule.toHtmlTable

(*** include-it-raw ***)

(**
As an extra step, the library calculates a number of statistics for the schedule, including the total interest accrued, the total fees and charges,
the total payments made, and the final principal balance. The full output for this schedule, including stats, is available in the Output section in
the page [Unit-Test Outputs](unitTestOutput.fsx), under Compliance. This particular example is defined as [ComplianceTest023](content/Compliance/ComplianceTest023.md).
*)
