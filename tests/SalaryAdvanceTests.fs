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

        let mode3 = Custom [ 25000L<Cent>; 25000L<Cent> ]
        mode3.Html |> should equal "custom amounts over 2 payroll dates"

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

    /// Test custom repayment amounts happy path
    [<Fact>]
    let testCustomAmountsSchedule () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15); Date(2024, 2, 29) |]

        let config =
            SalaryAdvance.ScheduleConfig.create
                advanceDate
                50000L<Cent>
                (Custom [ 20000L<Cent>; 20000L<Cent>; 10000L<Cent> ])
                payrollDates

        let schedule = SalaryAdvance.createSchedule config

        schedule.Length |> should equal 3
        schedule.[0].PaymentDate |> should equal (Date(2024, 1, 31))
        schedule.[0].RepaymentAmount |> should equal 20000L<Cent>
        schedule.[0].RemainingBalance |> should equal 30000L<Cent>
        schedule.[1].RepaymentAmount |> should equal 20000L<Cent>
        schedule.[1].RemainingBalance |> should equal 10000L<Cent>
        schedule.[2].RepaymentAmount |> should equal 10000L<Cent>
        schedule.[2].RemainingBalance |> should equal 0L<Cent>

    /// Test custom repayment amounts validation failures
    [<Fact>]
    let testCustomAmountsValidation () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15) |]

        let baseConfig =
            SalaryAdvance.ScheduleConfig.create advanceDate 50000L<Cent> LumpOnFirstPayroll payrollDates

        // wrong count: one amount for two payroll dates
        let wrongCount =
            { baseConfig with RepaymentMode = Custom [ 50000L<Cent> ] }

        let wrongCountErrors = SalaryAdvance.ScheduleConfig.validate wrongCount
        wrongCountErrors |> should contain "Custom repayment amounts count (1) must match payroll dates count (2)"

        // wrong sum: amounts do not add up to the total repayable
        let wrongSum =
            { baseConfig with RepaymentMode = Custom [ 20000L<Cent>; 20000L<Cent> ] }

        let wrongSumErrors = SalaryAdvance.ScheduleConfig.validate wrongSum
        wrongSumErrors.Length |> should equal 1
        wrongSumErrors.[0].Contains("must sum to the total repayable") |> should equal true

        // negative amount
        let negativeAmount =
            { baseConfig with RepaymentMode = Custom [ 60000L<Cent>; -10000L<Cent> ] }

        let negativeErrors = SalaryAdvance.ScheduleConfig.validate negativeAmount
        negativeErrors |> should contain "Custom repayment amounts must all be positive"

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

        // percentage fees follow the library's Percent convention: Percent 2m = 2%
        let configWithPctFee = configWithFee
                              |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee (Percent 2m))

        let schedule2 = SalaryAdvance.createSchedule configWithPctFee
        schedule2.[0].FeeAmount |> should equal 1000L<Cent>

    /// Test that the fee rounding is configurable via the library's Rounding type
    [<Fact>]
    let testFeeRounding () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]

        // 2% of 33333 cents = 666.66 cents
        let config =
            SalaryAdvance.ScheduleConfig.create advanceDate 33333L<Cent> LumpOnFirstPayroll payrollDates
            |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee (Percent 2m))

        // default rounding: midpoint away from zero, so 666.66 -> 667
        SalaryAdvance.calculateFeeAmount config.FeeRounding config.AdvanceAmount config.Fee
        |> should equal 667L<Cent>

        // round down: 666.66 -> 666
        let roundDownConfig = config |> SalaryAdvance.ScheduleConfig.withFeeRounding RoundDown

        SalaryAdvance.calculateFeeAmount roundDownConfig.FeeRounding roundDownConfig.AdvanceAmount roundDownConfig.Fee
        |> should equal 666L<Cent>

        let schedule = SalaryAdvance.createSchedule roundDownConfig
        schedule.[0].FeeAmount |> should equal 666L<Cent>
        schedule.[0].RepaymentAmount |> should equal 33999L<Cent>

    /// Test fee treatments: netted from proceeds vs added on top
    [<Fact>]
    let testFeeTreatments () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15) |]

        let baseConfig =
            SalaryAdvance.ScheduleConfig.create advanceDate 50000L<Cent> EvenlyProrated payrollDates
            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 500L<Cent>)

        // added on top (default): full advance disbursed, advance + fee repaid
        let onTopSchedule = SalaryAdvance.createSchedule baseConfig
        let onTopCashflows = SalaryAdvance.exportCashflows baseConfig
        SalaryAdvance.netDisbursedAmount baseConfig |> should equal 50000L<Cent>
        SalaryAdvance.totalRepayable baseConfig |> should equal 50500L<Cent>
        onTopCashflows.[0].Amount |> should equal -50000L<Cent>

        onTopSchedule |> Array.sumBy (fun item -> item.RepaymentAmount) |> should equal 50500L<Cent>
        onTopSchedule |> Array.sumBy (fun item -> item.FeeAmount) |> should equal 500L<Cent>

        // netted from proceeds: advance less fee disbursed, advance alone repaid, no fee in the repayments
        let nettedConfig =
            baseConfig |> SalaryAdvance.ScheduleConfig.withFeeTreatment NettedFromProceeds

        let nettedSchedule = SalaryAdvance.createSchedule nettedConfig
        let nettedCashflows = SalaryAdvance.exportCashflows nettedConfig
        SalaryAdvance.netDisbursedAmount nettedConfig |> should equal 49500L<Cent>
        SalaryAdvance.totalRepayable nettedConfig |> should equal 50000L<Cent>
        nettedCashflows.[0].Amount |> should equal -49500L<Cent>

        nettedSchedule |> Array.sumBy (fun item -> item.RepaymentAmount) |> should equal 50000L<Cent>
        nettedSchedule |> Array.sumBy (fun item -> item.FeeAmount) |> should equal 0L<Cent>

        // invariant: principal (total repayable) equals the sum of scheduled repayments in both treatments
        for config in [ baseConfig; nettedConfig ] do
            let schedule = SalaryAdvance.createSchedule config

            schedule
            |> Array.sumBy (fun item -> item.RepaymentAmount)
            |> should equal (SalaryAdvance.totalRepayable config)

        // a fee at least as large as the advance cannot be netted from the proceeds
        let feeTooLarge =
            nettedConfig |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 50000L<Cent>)

        let feeErrors = SalaryAdvance.ScheduleConfig.validate feeTooLarge
        feeErrors |> should contain "Fee must be less than the advance amount when netted from proceeds"

    /// Test that the prorated fee allocation is derived from the payment allocation, so that the per-payment
    /// principal/fee decomposition is coherent
    [<Fact>]
    let testProratedFeeAllocationCoherence () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31); Date(2024, 2, 15); Date(2024, 2, 29) |]

        let config =
            SalaryAdvance.ScheduleConfig.create advanceDate 10000L<Cent> EvenlyProrated payrollDates
            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 200L<Cent>)

        let schedule = SalaryAdvance.createSchedule config

        // total repayable 10200 across three payrolls: payments [3400; 3400; 3400], fees allocated by payment
        // share as [66; 67; 67], so per-payment principal is [3334; 3333; 3333]
        schedule |> Array.map (fun item -> item.RepaymentAmount) |> should equal [| 3400L<Cent>; 3400L<Cent>; 3400L<Cent> |]
        schedule |> Array.map (fun item -> item.FeeAmount) |> should equal [| 66L<Cent>; 67L<Cent>; 67L<Cent> |]

        let principals = schedule |> Array.map (fun item -> item.RepaymentAmount - item.FeeAmount)
        principals |> Array.sum |> should equal 10000L<Cent>
        schedule |> Array.sumBy (fun item -> item.FeeAmount) |> should equal 200L<Cent>
        schedule |> Array.iter (fun item -> item.FeeAmount >= 0L<Cent> |> should equal true)

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
            validConfig |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee (Percent 100m))

        let feeErrors = SalaryAdvance.ScheduleConfig.validate invalidPercentageFee
        feeErrors |> should contain "Percentage fee must be at least 0% and less than 100%"

        // a zero percentage fee is accepted, consistent with a zero flat fee
        let zeroPercentageFee =
            validConfig |> SalaryAdvance.ScheduleConfig.withFee (PercentageFee (Percent 0m))

        (SalaryAdvance.ScheduleConfig.validate zeroPercentageFee).Length |> should equal 0

        let zeroFlatFee =
            validConfig |> SalaryAdvance.ScheduleConfig.withFee (FlatFee 0L<Cent>)

        (SalaryAdvance.ScheduleConfig.validate zeroFlatFee).Length |> should equal 0

    /// Test that amounts large enough to overflow int64 when summed are rejected
    [<Fact>]
    let testOverflowRejection () =
        let advanceDate = Date(2024, 1, 15)
        let payrollDates = [| Date(2024, 1, 31) |]

        let validConfig =
            SalaryAdvance.ScheduleConfig.create advanceDate 50000L<Cent> LumpOnFirstPayroll payrollDates

        let hugeAdvance =
            { validConfig with AdvanceAmount = SalaryAdvance.maxSupportedAmount + 1L<Cent> }

        let advanceErrors = SalaryAdvance.ScheduleConfig.validate hugeAdvance
        advanceErrors.Length |> should equal 1
        advanceErrors.[0].Contains("Advance amount must not exceed") |> should equal true

        let hugeFee =
            validConfig
            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee(SalaryAdvance.maxSupportedAmount + 1L<Cent>))

        let feeErrors = SalaryAdvance.ScheduleConfig.validate hugeFee
        feeErrors.Length |> should equal 1
        feeErrors.[0].Contains("Flat fee must not exceed") |> should equal true

        // amounts at the cap are accepted and do not overflow: advance + fee stays within int64
        let atCap =
            { validConfig with AdvanceAmount = SalaryAdvance.maxSupportedAmount }
            |> SalaryAdvance.ScheduleConfig.withFee (FlatFee SalaryAdvance.maxSupportedAmount)

        (SalaryAdvance.ScheduleConfig.validate atCap).Length |> should equal 0
        SalaryAdvance.totalRepayable atCap |> should equal (2L<Cent> * 922_337_203_685_477_580L)

    /// Test that a null payroll dates array yields a validation error rather than a NullReferenceException
    [<Fact>]
    let testNullPayrollDatesValidation () =
        let advanceDate = Date(2024, 1, 15)

        let config =
            SalaryAdvance.ScheduleConfig.create advanceDate 50000L<Cent> LumpOnFirstPayroll null

        let errors = SalaryAdvance.ScheduleConfig.validate config
        errors |> should contain "Payroll dates array must not be null"

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
        summary.NetDisbursedAmount |> should equal 50000L<Cent>
        summary.TotalFeeAmount |> should equal 500L<Cent>
        summary.TotalRepaymentAmount |> should equal 50500L<Cent>
        summary.TermInDays |> should equal 16
        summary.NumberOfPayments |> should equal 1
        // flat rate over the term, with no time dimension
        summary.EffectiveFeeRate |> should equal (Percent 1.0m)
        // simple annualization: 1% x 365 / 16 days = 22.8125%
        summary.AnnualizedEffectiveFeeRate |> should equal (Some(Percent 22.8125m))
