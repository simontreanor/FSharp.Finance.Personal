(**
---
title: Amortisation examples
category: Examples
categoryindex: 2
index: 3
description: Examples of amortisation
keywords: amortisation amortization
---
*)

(**
# Amortisation Examples

## Basic example #1

The following example shows a small personal loan of £1,500 taken out on 26 November 2022 and repaid on time in 5 monthly instalments,
with a level payment of £456.88 and a final payment of £456.84:

*)

// #r "nuget:FSharp.Finance.Personal"
#r @"..\src\bin\Debug\netstandard2.1\FSharp.Finance.Personal.dll"

open FSharp.Finance.Personal
open CustomerPayments
open PaymentSchedule

let scheduleParameters =
    {
        AsOfDate = Date(2023, 4, 1)
        StartDate = Date(2022, 11, 26)
        Principal = 1500_00L<Cent>
        PaymentSchedule = RegularSchedule (
	        UnitPeriodConfig = UnitPeriod.Monthly(1, 2022, 11, 31)
	        PaymentCount = 5
        )
        FeesAndCharges = {
            Fees = [||]
            FeesSettlement = Fees.Settlement.ProRataRefund
            Charges = [| Charge.LatePayment (Amount.Simple 10_00L<Cent>) |]
            ChargesHolidays = [||]
            LatePaymentGracePeriod = 0<DurationDay>
        }
        Interest = {
            Rate = Interest.Rate.Daily (Percent 0.8m)
            Cap = { Total = ValueSome <| Interest.TotalPercentageCap (Percent 100m); Daily = ValueSome <| Interest.DailyPercentageCap (Percent 0.8m) }
            InitialGracePeriod = 3<DurationDay>
            Holidays = [||]
            RateOnNegativeBalance = ValueNone
        }
        Calculation = {
            AprMethod = Apr.CalculationMethod.UnitedKingdom 3
            RoundingOptions = { InterestRounding = RoundDown; PaymentRounding = RoundUp }
            MinimumPayment = DeferOrWriteOff 50L<Cent>
            PaymentTimeout = 3<DurationDay>
        }
    }

let actualPayments = [|
    { PaymentDay =   4<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 456_88L<Cent>) }
    { PaymentDay =  35<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 456_88L<Cent>) }
    { PaymentDay =  66<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 456_88L<Cent>) }
    { PaymentDay =  94<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 456_88L<Cent>) }
    { PaymentDay = 125<OffsetDay>; PaymentDetails = ActualPayment (ActualPayment.Confirmed 456_84L<Cent>) }
|]

let amortisationSchedule =
    actualPayments
    |> Amortisation.generate scheduleParameters IntendedPurpose.Statement
amortisationSchedule

(*** include-it ***)

(**
It is possible to format the `Items` property as an HTML table:
*)

let html =
    amortisationSchedule
    |> ValueOption.map (_.ScheduleItems >> Formatting.generateHtmlFromArray)
    |> ValueOption.defaultValue ""
html

(*** include-it-raw ***)

