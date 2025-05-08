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

The `cref:T:FSharp.Finance.Personal.Scheduling` module contains the functions that create the basic schedule. The basic schedule is a basic schedule that allows
us to calculate the interest accrued over the schedule as well as the level and final payments.

Let's start by defining the parameters. Let's define a loan of £1000 advanced on 22 April 2025, paid back over 4 months starting one month after the advance date.
The loan has a daily interest rate of 0.798% and a cap of 0.8% per day as well as a cap of 100% of the principal amount. Interest is calculated using the actuarial method. 
*)

(*** hide ***)
#r "nuget:FSharp.Finance.Personal"
open FSharp.Finance.Personal
open Calculation
open DateDay
open Scheduling
open UnitPeriod
(*** ***)
let bp : BasicParameters = {
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
Then we call the `cref:M:FSharp.Finance.Personal.Scheduling.calculateBasicSchedule` function to generate the schedule:
*)

let actuarialInterestSchedule = calculateBasicSchedule bp
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
        { bp with InterestConfig.Method = Interest.Method.AddOn }
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
1. **Estimate total interest**: estimate the total interest and level payment to improve the solver performance
1. **Solve for payment values**: use the bisection method to determine the level payments required
1. **Tweak final payment**: ensure the final payment fully amortises the schedule

Let's look at each of these items in more detail.

### Step 1: Generate payment days

Here we take the schedule config from the parameters and generate the payment days. In this example, the schedule is auto-generated, so the
`cref:M:FSharp.Finance.Personal.Scheduling.generatePaymentMap` function takes the start date of the schedule, the unit period and the first payment date, and
generates the payment days. It is also possible to specify the payment days manually or specify multiple ranges of dates at different intervals. However, the
auto-generated schedule is the most common use case, and ensures respect for varying month lengths and month-end tracking dates.
We'll also get extract a couple of values for later use.

*)

let paymentMap = generatePaymentMap bp.StartDate bp.ScheduleConfig
let paymentDays = paymentMap |> Map.keys |> Seq.toArray

let finalScheduledPaymentDay = Array.last paymentDays
let paymentCount = Array.length paymentDays

(*** hide ***)
$"""Payment days = {paymentDays |> Array.map string |> String.concat ", "}"""

(*** include-it-raw ***)

(**
### Step 2: Estimate the total interest

To make the iteration more efficient, we use an initial guess for the payment value, which is calculated based on the estimated total interest and the number
of payments.
*)

let estimatedInterestTotal =
    let dailyInterestRate =
        bp.InterestConfig.StandardRate
        |> Interest.Rate.daily
        |> Percent.toDecimal
    Cent.toDecimalCent bp.Principal
        * dailyInterestRate
        * decimal finalScheduledPaymentDay
        * Fraction.toDecimal (Fraction.Simple(2, 3))

let initialPaymentGuess =
    calculateLevelPayment
        paymentCount
        bp.PaymentConfig.Rounding
        bp.Principal
        0L<Cent>
        estimatedInterestTotal
    |> Cent.toDecimalCent
    |> decimal

(*** hide ***)
$"Estimated interest total = {estimatedInterestTotal / 100m :``N2``}"

(*** include-it-raw ***)

(**<br />*)

(*** hide ***)
$"Initial payment guess = {initialPaymentGuess / 100m:``N2``}"

(*** include-it-raw ***)

(**
### Step 3: Solve for payment values

Determining the payment values requires the use of a solver, because payment values determine how much principal is paid off each unit-period, and therefore how
much interest is accrued, which in turn affects the payment values. We use the bisection method (`cref:M:FSharp.Finance.Personal.Calculation.Array.solveBisection`)
for this. This method runs a generator function (`cref:M:FSharp.Finance.Personal.Scheduling.generatePaymentValue`) on the schedule, which calculates the final
principal balance for a given payment value. The bisection method then iteratively narrows down the level payment value until the final principal balance is close
to zero (usually just below zero, so the final payment can be slightly smaller).
*)

let generatePaymentValue (bp: BasicParameters) paymentDays firstItem roughPayment =
    let scheduledPayment =
        roughPayment
        |> Cent.round bp.PaymentConfig.Rounding
        |> fun rp -> ScheduledPayment.quick (ValueSome rp) ValueNone
    let schedule =
        paymentDays
        |> Array.fold(fun basicItem pd ->
            generateItem bp bp.InterestConfig.Method scheduledPayment basicItem pd
        ) firstItem
    let principalBalance = decimal schedule.PrincipalBalance
    principalBalance, ScheduledPayment.total schedule.ScheduledPayment |> Cent.toDecimal

let initialBasicItem = { BasicItem.initial with PrincipalBalance = bp.Principal }

let basicItems =
    let solution =
        Array.solveBisection
            (generatePaymentValue bp paymentDays initialBasicItem)
            100u
            initialPaymentGuess
            (LevelPaymentOption.toTargetTolerance bp.PaymentConfig.LevelPaymentOption)
            (ToleranceSteps.forPaymentValue paymentCount)
    match solution with
        | Solution.Found (paymentValue, _, _) ->
            let paymentMap' =
                paymentMap
                |> Map.map(fun _ sp ->
                    { sp with
                        Original =
                            sp.Original
                            |> ValueOption.map(fun _ -> paymentValue |> Cent.fromDecimal)
                    })
            paymentDays
            |> Array.scan(fun basicItem pd ->
                generateItem bp bp.InterestConfig.Method paymentMap'[pd] basicItem pd
            ) initialBasicItem
        | _ ->
            [||]

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = basicItems; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> BasicSchedule.toHtmlTable

(*** include-it-raw ***)

(**
### Step 4: Tweak final payment

The final payment is adjusted (`cref:M:FSharp.Finance.Personal.Scheduling.adjustFinalPayment`) to ensure that the final principal balance is zero.
*)

let items =
    basicItems
    |> adjustFinalPayment
        finalScheduledPaymentDay
        bp.ScheduleConfig.IsAutoGenerateSchedule

(*** hide ***)
{ EvaluationDay = 0<OffsetDay>; Items = items; Stats = (*☣*) Unchecked.defaultof<InitialStats> } |> BasicSchedule.toHtmlTable

(*** include-it-raw ***)

(**
As an extra step, the library calculates a number of statistics for the schedule, including the total interest accrued, the total fees and charges,
the total payments made, and the final principal balance. The full output for this schedule, including stats, is available in the Output section in
the page [Unit-Test Outputs](unitTestOutput.fsx), under Compliance. This particular example is defined as [ComplianceTest023](content/Compliance/ComplianceTest023.md).
*)
