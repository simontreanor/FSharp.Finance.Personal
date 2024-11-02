(**
---
title: UK APR example
category: Examples
categoryindex: 2
index: 1
description: Example of UK APR calculation
keywords: APR UK
---
*)

(**
# UK APR calculation

## Basic example

The following example shows a loan of £500.00 taken out on 10th October 2012 and repaid in two monthly instalments:

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Apr
open Calculation
open DateDay

let startDate = Date(2012, 10, 10)

let principal = 500_00L<Cent>

let transfers = [|
    { TransferType = Payment; TransferDate = Date(2012, 11, 10); Value = 270_00L<Cent> }
    { TransferType = Payment; TransferDate = Date(2012, 12, 10); Value = 270_00L<Cent> }
|]

let aprMethod = CalculationMethod.UnitedKingdom 3

let solution = Apr.calculate aprMethod principal startDate transfers
solution

(*** include-it ***)

(**
This result is of an `Array.Solution` type. `Found` means that it was able to find a solution.
The APR, expressed as a decimal, is returned as the first item of the tuple. The APR is more usefully shown as a percentage,
which can easily be done as follows:
*)

solution |> toPercent aprMethod

(*** include-it ***)

(**
### Notes

- The current implementation only supports single-advance transactions
- The solver tries to find a solution that stabilises at 8 decimal places, using no more than 100 iterations
- If it cannot find a solution within these confines, it will return `IterationLimitReached` along with a tuple giving
the partial solution as the first item (which may or may not be a good approximation of the answer)
- If the transfers list is empty, it will return `Solution.Impossible`
- The remaining parts of the solution tuples return information about the number of iterations used and details of any
tolerances configured. However, these are currently not customisable for UK APR calculations so are not useful here.
*)
