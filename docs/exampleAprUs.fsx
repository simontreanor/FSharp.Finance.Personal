(**
---
title: US APR example
category: Examples
categoryindex: 2
index: 2
description: Example of US APR calculation
keywords: APR US
---
*)

(**
# US APR calculation

## Basic example

The following example shows a loan of $5,000.00 taken out on 10th January 1978 and repaid in 24 monthly instalments:

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Apr
open Currency
open DateDay
open UnitPeriod

let startDate = Date(1978, 1, 10)

let principal = 5000_00L<Cent>

let transfers =
    Monthly (1, 1978, 2, 10)
    |> generatePaymentSchedule 24 ValueNone Direction.Forward
    |> Array.map(fun d -> { TransferType = Payment; TransferDate = d; Value = 230_00L<Cent> })

let aprMethod = CalculationMethod.UsActuarial 4

let solution = Apr.calculate aprMethod principal startDate transfers

solution

(*** include-it ***)

(**
This result is of a `Array.Solution` type. `Found` means that it was able to find a solution.
The APR, expressed as a decimal, is returned as the first item of the tuple. The APR is more usefully shown as a percentage,
which can easily be done as follows:
*)

solution |> toPercent aprMethod

(*** include-it ***)

(**
### Notes

- The current implementation only supports single-advance transactions
- The solver tries to find a solution that stabilises at 10 decimal places, using no more than 100 iterations
- If it cannot find a solution within these confines, it will return `IterationLimitReached` along with a tuple giving
the partial solution as the first item (which may or may not be a good approximation of the answer)
- If the transfers list is empty, it will return `Solution.Impossible`
- The remaining parts of the solution tuples return information about the number of iterations used and details of any
tolerances configured. However, these are currently not customisable for US APR calculations so are not useful here.
*)
