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

#r "nuget:FSharp.Finance.Personal"

open FSharp.Finance.Personal
open Apr

let startDate = Date(2012, 10, 10)
let principal = 500_00L<Cent>
let transfers = [|
    { TransferType = Payment; TransferDate = Date(2012, 11, 10); Amount = 270_00L<Cent> }
    { TransferType = Payment; TransferDate = Date(2012, 12, 10); Amount = 270_00L<Cent> }
|]

UnitedKingdom.calculateApr startDate principal transfers

(*** include-fsi-output ***)
