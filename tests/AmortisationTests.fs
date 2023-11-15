namespace FSharp.Finance.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance
open System.ComponentModel.Design

module AmortisationTests =

    open Amortisation

    let parameters1 =
        let principal = 1200m
        let productFees = Percentage (1.8947m, ValueNone)
        let annualInterestRate = 0.0995m
        let startDate = DateTime.Today
        let unitPeriodConfig = UnitPeriod.Weekly(2, DateTime.Today.AddDays(15.))
        let paymentCount = 11 // to-do: restore function to determine this based on max loan length?
        { Principal = principal; ProductFees = productFees; AnnualInterestRate = annualInterestRate; StartDate = startDate; UnitPeriodConfig = unitPeriodConfig; PaymentCount = paymentCount }

    [<Fact>]
    let ``Biweekly schedule with long first period (unit-periods only)`` () =
        let actual = createRegularScheduleInfo UnitPeriodsOnly parameters1
        let schedule = { Parameters = parameters1; IntermediateResult = actual.Schedule.IntermediateResult; Items = actual.Schedule.Items }
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
            FinalPaymentDateCount = 11
            Apr = 6.23706600m
            EffectiveAnnualInterestRate = 0.0549180352226965782106044008m
            EffectiveDailyInterestRate = 0.0001504603704731413101660395m
        }
        actual |> should equal expected

    [<Fact>]
    let ``Biweekly schedule with long first period (unit-periods with daily interest)`` () =
        let actual = createRegularScheduleInfo UnitPeriodsWithInterestCalculatedDaily parameters1
        let schedule = { Parameters = parameters1; IntermediateResult = actual.Schedule.IntermediateResult; Items = actual.Schedule.Items }
        let expected = {
            Schedule = schedule
            AdvancesTotal = 1200m
            PaymentsTotal = 3554.81m
            PrincipalTotal = 1200m
            ProductFeesTotal = 2273.64m
            InterestTotal = 81.17m
            PenaltyChargesTotal = 0m   
            ProductFeesRefund = 0m   
            FinalPaymentDate = DateTime(2024, 04, 18)
            FinalPaymentDateCount = 11
            Apr = 6.23645811m
            EffectiveAnnualInterestRate = 0.0550265019013242964245742404m
            EffectiveDailyInterestRate = 0.0001507575394556830039029431m
        }
        actual |> should equal expected

    [<Fact>]
    let ``Biweekly schedule with long first period (interspersed days)`` () =
        let actual = createRegularScheduleInfo IntersperseDays parameters1
        let schedule = { Parameters = parameters1; IntermediateResult = actual.Schedule.IntermediateResult; Items = actual.Schedule.Items }
        let expected = {
            Schedule = schedule
            AdvancesTotal = 1200m
            PaymentsTotal = 3554.81m
            PrincipalTotal = 1200m
            ProductFeesTotal = 2273.64m
            InterestTotal = 81.17m
            PenaltyChargesTotal = 0m   
            ProductFeesRefund = 0m   
            FinalPaymentDate = DateTime(2024, 04, 18)
            FinalPaymentDateCount = 11
            Apr = 6.23645811m
            EffectiveAnnualInterestRate = 0.0550265019013242964245742404m
            EffectiveDailyInterestRate = 0.0001507575394556830039029431m
        }
        actual |> should equal expected
