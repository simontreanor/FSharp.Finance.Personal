namespace FSharp.Finance.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance

module AmortisationTests =

    open Amortisation

    [<Fact>]
    let ``Biweekly schedule with long first period (unit-periods only)`` () =
        let parameters =
            let principal = 1200m
            let productFees = Percentage (1.8947m, ValueNone)
            let annualInterestRate = 0.0995m
            let startDate = DateTime.Today
            let unitPeriodConfig = UnitPeriod.Weekly(2, DateTime.Today.AddDays(15.))
            let paymentCount = 11 // to-do: restore function to determine this based on max loan length?
            { Principal = principal; ProductFees = productFees; AnnualInterestRate = annualInterestRate; StartDate = startDate; UnitPeriodConfig = unitPeriodConfig; PaymentCount = paymentCount }
        let roughPayments =
            [| 0 .. 10 |]
            |> Array.map(fun i -> { Amount = 329.13m; Date = (DateTime(2023, 11, 30)).AddDays(float i * 14.); Day = 15 + (i * 14); Index = i; Interval = (if i = 0 then 15 else 14); PenaltyCharges = ValueNone })
        let intermediateResult = { Parameters = parameters; InterestTotal = 81.01m; PenaltyChargesTotal = 0m; RoughPayments = roughPayments }
        let items = [|
            { Date = DateTime(2023, 11, 15); Day =   0; Advance = 1200m; Payments = [|         |]; NewInterest =  0m   ; NewPenaltyCharges = 0m; PrincipalPortion =   0m   ; ProductFeesPortion =   0m   ; InterestPortion =  0m   ; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance = 1200m   ; ProductFeesBalance = 2273.64m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2023, 11, 30); Day =  15; Advance =    0m; Payments = [| 323.15m |]; NewInterest = 14.2m ; NewPenaltyCharges = 0m; PrincipalPortion = 106.73m; ProductFeesPortion = 202.22m; InterestPortion = 14.2m ; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance = 1093.27m; ProductFeesBalance = 2071.42m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2023, 12, 14); Day =  29; Advance =    0m; Payments = [| 323.15m |]; NewInterest = 12.08m; NewPenaltyCharges = 0m; PrincipalPortion = 107.46m; ProductFeesPortion = 203.61m; InterestPortion = 12.08m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  985.81m; ProductFeesBalance = 1867.81m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2023, 12, 28); Day =  43; Advance =    0m; Payments = [| 323.15m |]; NewInterest = 10.89m; NewPenaltyCharges = 0m; PrincipalPortion = 107.87m; ProductFeesPortion = 204.39m; InterestPortion = 10.89m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  877.94m; ProductFeesBalance = 1663.42m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 01, 11); Day =  57; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  9.7m ; NewPenaltyCharges = 0m; PrincipalPortion = 108.28m; ProductFeesPortion = 205.17m; InterestPortion =  9.7m ; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  769.66m; ProductFeesBalance = 1458.25m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 01, 25); Day =  71; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  8.5m ; NewPenaltyCharges = 0m; PrincipalPortion = 108.7m ; ProductFeesPortion = 205.95m; InterestPortion =  8.5m ; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  660.96m; ProductFeesBalance = 1252.3m ; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 02, 08); Day =  85; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  7.3m ; NewPenaltyCharges = 0m; PrincipalPortion = 109.11m; ProductFeesPortion = 206.74m; InterestPortion =  7.3m ; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  551.85m; ProductFeesBalance = 1045.56m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 02, 22); Day =  99; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  6.1m ; NewPenaltyCharges = 0m; PrincipalPortion = 109.53m; ProductFeesPortion = 207.52m; InterestPortion =  6.1m ; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  442.32m; ProductFeesBalance =  838.04m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 03, 07); Day = 113; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  4.89m; NewPenaltyCharges = 0m; PrincipalPortion = 109.95m; ProductFeesPortion = 208.31m; InterestPortion =  4.89m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  332.37m; ProductFeesBalance =  629.73m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 03, 21); Day = 127; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  3.67m; NewPenaltyCharges = 0m; PrincipalPortion = 110.37m; ProductFeesPortion = 209.11m; InterestPortion =  3.67m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  222m   ; ProductFeesBalance =  420.62m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 04, 04); Day = 141; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  2.45m; NewPenaltyCharges = 0m; PrincipalPortion = 110.79m; ProductFeesPortion = 209.91m; InterestPortion =  2.45m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =  111.21m; ProductFeesBalance =  210.71m; InterestBalance = 0m; PenaltyChargesBalance = 0m }
            { Date = DateTime(2024, 04, 18); Day = 155; Advance =    0m; Payments = [| 323.15m |]; NewInterest =  1.23m; NewPenaltyCharges = 0m; PrincipalPortion = 111.21m; ProductFeesPortion = 210.71m; InterestPortion =  1.23m; PenaltyChargesPortion = 0m; ProductFeesRefund = 0m; PrincipalBalance =    0m   ; ProductFeesBalance =    0m   ; InterestBalance = 0m; PenaltyChargesBalance = 0m }
        |]
        let schedule = { Parameters = parameters; IntermediateResult = intermediateResult; Items = items }
        let expected = {
            Schedule = schedule
            AdvancesTotal = 1200m
            PaymentsTotal = 3554.65m
            PrincipalTotal = 1200m
            ProductFeesTotal = 2273.64m
            InterestTotal = 81.01m
            PenaltyChargesTotal = 0m   
            ProductFeesRefund = 0m   
            FinalPaymentDate = DateTime(2024, 04, 18)
            FinalPaymentCount = 11
            Apr = 6.23706600m
            EffectiveAnnualInterestRate = 0.0549180352226965782106044008m
            EffectiveDailyInterestRate = 0.0001504603704731413101660395m
        }

        let actual = createRegularScheduleInfo UnitPeriodsOnly parameters

        actual |> should equal expected
