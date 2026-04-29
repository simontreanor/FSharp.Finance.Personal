namespace FSharp.Finance.Personal.Tests

open System
open FsUnit.Xunit
open FSharp.Finance.Personal
open FSharp.Finance.Personal.SalaryAdvance
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Calculation
open Xunit

/// Tests for the SalaryAdvance module to validate requirements
module SalaryAdvanceTests =

    /// Test that the RepaymentMode discriminated union is properly defined
    [<Fact>]
    let testRepaymentModeDefinition () =
        let mode1 = RepaymentMode.LumpOnFirstPayroll
        mode1.Html |> should equal "lump sum on first payroll"

        let mode2 = EvenlyProrated
        mode2.Html |> should equal "evenly prorated"

        let mode3 = Custom 30L
        mode3.Html |> should equal "custom over 30 days"

    /// Test schedule construction functionality
    [<Fact>]
    let testScheduleConstruction () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15) |]
        let config = SalaryAdvance.ScheduleConfig.create 
                        advanceDate
                        50000L<Cent>  // $500.00
                        LumpOnFirstPayroll
                        payrollDates

        let schedule = SalaryAdvance.createSchedule config

        schedule.Length |> should equal 1
        schedule.[0].PaymentDate |> should equal (Date(2024, 1, 31))
        schedule.[0].RepaymentAmount |> should equal 50000L<Cent>
        schedule.[0].RemainingBalance |> should equal 0L<Cent>

        let config2 = { config with RepaymentMode = EvenlyProrated }
        let schedule2 = SalaryAdvance.createSchedule config2

        schedule2.Length |> should equal 2
        schedule2.[0].RepaymentAmount |> should equal 25000L<Cent>
        schedule2.[0].RemainingBalance |> should equal 25000L<Cent>
        schedule2.[1].RepaymentAmount |> should equal 25000L<Cent>
        schedule2.[1].RemainingBalance |> should equal 0L<Cent>

    [<Fact>]
    let testEvenlyProratedHandlesRemaindersAndFeeAllocation () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15); Date(2024, 2, 29) |]

        let config =
            SalaryAdvance.ScheduleConfig.create
                advanceDate
                10000L<Cent>
                EvenlyProrated
                payrollDates
            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 1L<Cent>)

        let schedule = SalaryAdvance.createSchedule config
        let totalRepayable = 10001L<Cent>
        let totalRepayments = schedule |> Array.sumBy (fun payment -> payment.RepaymentAmount)
        let totalFees = schedule |> Array.sumBy (fun payment -> payment.FeeAmount)

        schedule.Length |> should equal 3
        totalRepayments |> should equal totalRepayable
        totalFees |> should equal 1L<Cent>
        schedule.[0].RepaymentAmount |> should equal 3333L<Cent>
        schedule.[1].RepaymentAmount |> should equal 3333L<Cent>
        schedule.[2].RepaymentAmount |> should equal 3335L<Cent>
        schedule.[0].FeeAmount |> should equal 0L<Cent>
        schedule.[1].FeeAmount |> should equal 0L<Cent>
        schedule.[2].FeeAmount |> should equal 1L<Cent>
        schedule.[0].RemainingBalance |> should equal 6668L<Cent>
        schedule.[1].RemainingBalance |> should equal 3335L<Cent>
        schedule.[2].RemainingBalance |> should equal 0L<Cent>

    /// Test fee handling functionality
    [<Fact>]
    let testFeeHandling () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]

        let configWithFee = SalaryAdvance.ScheduleConfig.create 
                              advanceDate
                              50000L<Cent>
                              LumpOnFirstPayroll
                              payrollDates
                            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>)

        let schedule = SalaryAdvance.createSchedule configWithFee
        schedule.[0].RepaymentAmount |> should equal 50500L<Cent>
        schedule.[0].FeeAmount |> should equal 500L<Cent>

        let configWithPctFee = configWithFee 
                              |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee 0.02m)

        let schedule2 = SalaryAdvance.createSchedule configWithPctFee
        schedule2.[0].FeeAmount |> should equal 1000L<Cent>

    /// Test exportable cashflows functionality
    [<Fact>]
    let testExportableCashflows () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]
        let config = SalaryAdvance.ScheduleConfig.create 
                        advanceDate
                        50000L<Cent>
                        LumpOnFirstPayroll
                        payrollDates
                     |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>)

        let cashflows = SalaryAdvance.exportCashflows config

        cashflows.Length |> should equal 2
        cashflows.[0].Date |> should equal advanceDate
        cashflows.[0].Amount |> should equal -50000L<Cent>
        cashflows.[0].Description.Contains("advance disbursement") |> should equal true
        cashflows.[1].Date |> should equal (Date(2024, 1, 31))
        cashflows.[1].Amount |> should equal 50500L<Cent>
        cashflows.[1].Description.Contains("Repayment") |> should equal true

        let borrowerCashflows = SalaryAdvance.borrowerCashflows config
        borrowerCashflows.[0].Amount |> should equal 50000L<Cent>
        borrowerCashflows.[1].Amount |> should equal -50500L<Cent>

    /// Test validation functionality
    [<Fact>]
    let testValidation () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]
        
        // Valid config should have no errors
        let validConfig = SalaryAdvance.ScheduleConfig.create 
                            advanceDate
                            50000L<Cent>
                            LumpOnFirstPayroll
                            payrollDates

        let errors = SalaryAdvance.ScheduleConfig.validate validConfig
        errors.Length |> should equal 0

        let invalidConfig = { validConfig with AdvanceAmount = -1000L<Cent> }
        let errors2 = SalaryAdvance.ScheduleConfig.validate invalidConfig
        errors2.Length > 0 |> should equal true

        let outOfOrderPayrolls =
            { validConfig with PayrollDates = [| Date(2024, 2, 15); Date(2024, 1, 31) |] }

        let payrollErrors = SalaryAdvance.ScheduleConfig.validate outOfOrderPayrolls
        payrollErrors |> should contain "Payroll dates must be strictly increasing"

        let invalidPercentageFee =
            validConfig |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee 1.0m)

        let feeErrors = SalaryAdvance.ScheduleConfig.validate invalidPercentageFee
        feeErrors |> should contain "Percentage fee must be greater than 0 and less than 1"

    [<Fact>]
    let testInvalidConfigRaisesWhenBuildingSchedule () =
        let advanceDate = Date(2024, 1, 15)
        let config =
            SalaryAdvance.ScheduleConfig.create
                advanceDate
                50000L<Cent>
                LumpOnFirstPayroll
                [||]

        Assert.Throws<ArgumentException>(fun () -> SalaryAdvance.createSchedule config |> ignore)
        |> ignore

    /// Test summary calculations
    [<Fact>]
    let testSummaryCalculations () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]
        let config = SalaryAdvance.ScheduleConfig.create 
                        advanceDate
                        50000L<Cent>
                        LumpOnFirstPayroll
                        payrollDates
                     |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>)

        let summary = SalaryAdvance.calculateSummary config

        summary.AdvanceAmount |> should equal 50000L<Cent>
        summary.TotalFeeAmount |> should equal 500L<Cent>
        summary.TotalRepaymentAmount |> should equal 50500L<Cent>
        summary.TermInDays |> should equal 16
        summary.NumberOfPayments |> should equal 1
        summary.EffectiveFeeRate |> should equal 1.0m
