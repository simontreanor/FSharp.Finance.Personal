namespace FSharp.Finance.Personal.Tests

open System
open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal

module AmortisationTests =

    open Amortisation

    let parametersWith interestCapitalisation output =
        let principal = Cent.fromDecimal 1200m
        let productFees = Percentage (189.47m<Percent>, ValueNone)
        let annualInterestRate = 9.95m<Percent>
        let startDate = DateTime(2023, 11, 15)
        let unitPeriodConfig = UnitPeriod.Weekly(2, startDate.AddDays(15.))
        let maxLoanLength = 180<Duration>
        let paymentCount = UnitPeriod.maxPaymentCount maxLoanLength startDate unitPeriodConfig
        {
            Principal = principal
            ProductFees = productFees
            AnnualInterestRate = annualInterestRate
            InterestCapitalisation = interestCapitalisation
            StartDate = startDate
            UnitPeriodConfig = unitPeriodConfig
            PaymentCount = paymentCount
            Output = output
        }

    [<Fact>]
    let ``Biweekly schedule with long first period (unit-periods only)`` () =
        let p = parametersWith OnPaymentDates Full
        let actual = createRegularScheduleInfo p
        let schedule = { Parameters = p; IntermediateResult = actual.Schedule.IntermediateResult; Items = actual.Schedule.Items }
        let expected = {
            Schedule = schedule
            AdvancesTotal = Cent.fromDecimal 1200m
            PaymentsTotal = Cent.fromDecimal 3554.65m
            PrincipalTotal = Cent.fromDecimal 1200m
            ProductFeesTotal = Cent.fromDecimal 2273.64m
            InterestTotal = Cent.fromDecimal 81.01m
            PenaltyChargesTotal = Cent.fromDecimal 0m   
            ProductFeesRefund = Cent.fromDecimal 0m   
            FinalPaymentDate = DateTime(2024, 04, 18)
            FinalPaymentDateCount = 11
            Apr = 623.706600m<Percent>
            EffectiveAnnualInterestRate = 5.491804m<Percent>
            EffectiveDailyInterestRate = 0.015046m<Percent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``Biweekly schedule with long first period (unit-periods with daily interest)`` () =
        let p = parametersWith EveryDay Summary
        let actual = createRegularScheduleInfo p
        let schedule = { Parameters = p; IntermediateResult = actual.Schedule.IntermediateResult; Items = actual.Schedule.Items }
        let expected = {
            Schedule = schedule
            AdvancesTotal = Cent.fromDecimal 1200m
            PaymentsTotal = Cent.fromDecimal 3554.81m
            PrincipalTotal = Cent.fromDecimal 1200m
            ProductFeesTotal = Cent.fromDecimal 2273.64m
            InterestTotal = Cent.fromDecimal 81.17m
            PenaltyChargesTotal = Cent.fromDecimal 0m   
            ProductFeesRefund = Cent.fromDecimal 0m   
            FinalPaymentDate = DateTime(2024, 04, 18)
            FinalPaymentDateCount = 11
            Apr = 623.645811m<Percent>
            EffectiveAnnualInterestRate = 5.502650m<Percent>
            EffectiveDailyInterestRate = 0.015076m<Percent>
        }
        actual |> should equal expected

    [<Fact>]
    let ``Biweekly schedule with long first period (interspersed days)`` () =
        let p = parametersWith EveryDay Full
        let actual = createRegularScheduleInfo p
        let schedule = { Parameters = p; IntermediateResult = actual.Schedule.IntermediateResult; Items = actual.Schedule.Items }
        let expected = {
            Schedule = schedule
            AdvancesTotal = Cent.fromDecimal 1200m
            PaymentsTotal = Cent.fromDecimal 3554.81m
            PrincipalTotal = Cent.fromDecimal 1200m
            ProductFeesTotal = Cent.fromDecimal 2273.64m
            InterestTotal = Cent.fromDecimal 81.17m
            PenaltyChargesTotal = Cent.fromDecimal 0m   
            ProductFeesRefund = Cent.fromDecimal 0m   
            FinalPaymentDate = DateTime(2024, 04, 18)
            FinalPaymentDateCount = 11
            Apr = 623.645811m<Percent>
            EffectiveAnnualInterestRate = 5.502650m<Percent>
            EffectiveDailyInterestRate = 0.015076m<Percent>
        }
        actual |> should equal expected
