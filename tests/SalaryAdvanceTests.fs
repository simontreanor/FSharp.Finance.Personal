namespace FSharp.Finance.Personal.Tests

open System
open FSharp.Finance.Personal
open FSharp.Finance.Personal.SalaryAdvance
open FSharp.Finance.Personal.DateDay
open FSharp.Finance.Personal.Calculation

/// Tests for the SalaryAdvance module to validate requirements
module SalaryAdvanceTests =

    /// Test that the RepaymentMode discriminated union is properly defined
    let testRepaymentModeDefinition () =
        // Test LumpOnFirstPayroll
        let mode1 = LumpOnFirstPayroll
        assert (mode1.Html = "lump sum on first payroll")
        
        // Test EvenlyProrated  
        let mode2 = EvenlyProrated
        assert (mode2.Html = "evenly prorated")
        
        // Test Custom with int64
        let mode3 = Custom 30L
        assert (mode3.Html = "custom over 30 days")
        
        printfn "✓ RepaymentMode DU correctly defined with all required cases"

    /// Test schedule construction functionality
    let testScheduleConstruction () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15) |]
        let config = SalaryAdvance.ScheduleConfig.create 
                        advanceDate
                        50000L<Cent>  // $500.00
                        LumpOnFirstPayroll
                        payrollDates
        
        let schedule = SalaryAdvance.createSchedule config
        
        // Should have one payment on first payroll
        assert (schedule.Length = 1)
        assert (schedule.[0].PaymentDate = Date(2024, 1, 31))
        assert (schedule.[0].RepaymentAmount = 50000L<Cent>)
        assert (schedule.[0].RemainingBalance = 0L<Cent>)
        
        printfn "✓ Schedule construction works for LumpOnFirstPayroll"
        
        // Test evenly prorated
        let config2 = { config with RepaymentMode = EvenlyProrated }
        let schedule2 = SalaryAdvance.createSchedule config2
        
        assert (schedule2.Length = 2)
        assert (schedule2.[0].RepaymentAmount = 25000L<Cent>)
        assert (schedule2.[1].RepaymentAmount = 25000L<Cent>)
        
        printfn "✓ Schedule construction works for EvenlyProrated"

    /// Test fee handling functionality
    let testFeeHandling () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]
        
        // Test with flat fee
        let configWithFee = SalaryAdvance.ScheduleConfig.create 
                              advanceDate
                              50000L<Cent>
                              LumpOnFirstPayroll
                              payrollDates
                            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>)
        
        let schedule = SalaryAdvance.createSchedule configWithFee
        assert (schedule.[0].RepaymentAmount = 50500L<Cent>) // $500 + $5 fee
        assert (schedule.[0].FeeAmount = 500L<Cent>)
        
        printfn "✓ Fee handling works with FlatFee"
        
        // Test percentage fee
        let configWithPctFee = configWithFee 
                              |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee 2.0m)
        
        let schedule2 = SalaryAdvance.createSchedule configWithPctFee
        assert (schedule2.[0].FeeAmount = 1000L<Cent>) // 2% of $500 = $10
        
        printfn "✓ Fee handling works with PercentageFee"

    /// Test exportable cashflows functionality
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
        
        // Should have 2 cashflows: advance (positive) and repayment (negative)
        assert (cashflows.Length = 2)
        
        // First should be advance disbursement (positive)
        assert (cashflows.[0].Date = advanceDate)
        assert (cashflows.[0].Amount = 50000L<Cent>)
        assert (cashflows.[0].Description.Contains("advance disbursement"))
        
        // Second should be repayment (negative total amount)
        assert (cashflows.[1].Date = Date(2024, 1, 31))
        assert (cashflows.[1].Amount = -50500L<Cent>)
        assert (cashflows.[1].Description.Contains("Repayment"))
        
        printfn "✓ Exportable cashflows work correctly for analytical use"

    /// Test validation functionality
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
        assert (errors.Length = 0)
        
        // Invalid config (negative amount) should have errors
        let invalidConfig = { validConfig with AdvanceAmount = -1000L<Cent> }
        let errors2 = SalaryAdvance.ScheduleConfig.validate invalidConfig
        assert (errors2.Length > 0)
        
        printfn "✓ Validation functionality works correctly"

    /// Test summary calculations
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
        
        assert (summary.AdvanceAmount = 50000L<Cent>)
        assert (summary.TotalFeeAmount = 500L<Cent>)
        assert (summary.TotalRepaymentAmount = 50500L<Cent>)
        assert (summary.TermInDays = 16) // Jan 31 - Jan 15 = 16 days
        assert (summary.NumberOfPayments = 1)
        assert (summary.EffectiveFeeRate = 1.0m) // $5 fee on $500 = 1%
        
        printfn "✓ Summary calculations work correctly"

    /// Run all tests
    let runAllTests () =
        printfn "Running SalaryAdvance module tests..."
        testRepaymentModeDefinition ()
        testScheduleConstruction ()
        testFeeHandling ()
        testExportableCashflows ()
        testValidation ()
        testSummaryCalculations ()
        printfn "✅ All SalaryAdvance tests passed!"

// Export test runner for external use
module TestRunner = 
    let run () = SalaryAdvanceTests.runAllTests ()