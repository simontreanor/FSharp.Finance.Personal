(**
---
title: UK APR examples
category: Examples
categoryindex: 2
index: 1
description: Examples of UK APR calculations
keywords: APR UK
---
*)

(**
# Basic example

The following example shows a loan of £500.00 taken out on 10th October 2012 and repaid in two monthly instalments:

*)

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Apr

let startDate = Date(2012, 10, 10)
let principal = 500_00L<Cent>
let transfers = [|
    { TransferType = Payment; TransferDate = Date(2012, 11, 10); Amount = 270_00L<Cent> }
    { TransferType = Payment; TransferDate = Date(2012, 12, 10); Amount = 270_00L<Cent> }
|]

let solution = UnitedKingdom.calculateApr startDate principal transfers
solution

(*** include-it ***)

(**
This result is of a `Array.Solution` type. `Found` means that it was able to find a solution.
The APR, expressed as a decimal, is returned as the first item of the tuple. The APR is more usefully shown as a percentage,
which can easily be done as follows:
*)

match solution with
| Solution.Found(apr, _, _) -> apr | _ -> 0m
|> Percent.fromDecimal

(*** include-it ***)
