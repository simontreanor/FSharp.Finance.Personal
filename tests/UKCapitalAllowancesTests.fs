namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal
open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances.Types

module UKCapitalAllowancesTests =

    [<Fact>]
    let ``Default configuration has expected values`` () =
        Types.Default.AnnualInvestmentAllowanceLimit |> should equal 1_000_000_00L<Cent>
        Types.Default.MainPoolRate |> should equal 0.18m
        Types.Default.SpecialRatePoolRate |> should equal 0.06m
        Types.Default.MaxYears |> should equal 10
        Types.Default.SmallPoolThreshold |> should equal 1_000_00L<Cent>

    [<Fact>]
    let ``Small asset fully claimed via AIA in year 1`` () =
        let expenditure = {
            Amount = 5_000_00L<Cent>
            Pool = Types.Pool.Main
            Description = "Small equipment"
        }

        let schedule = (Calculations.scheduleDefault expenditure).Years

        // Should have at least one year
        schedule |> should not' (be Empty)

        // First year should claim full amount via AIA
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.AnnualInvestmentAllowance |> should equal 5_000_00L<Cent>
        year1.WritingDownAllowance |> should equal 0L<Cent>
        year1.TotalAllowances |> should equal 5_000_00L<Cent>
        year1.PoolValueEndOfYear |> should equal 0L<Cent>

    [<Fact>]
    let ``Large asset partially claimed via AIA then WDA`` () =
        let expenditure = {
            Amount = 50_000_00L<Cent>
            Pool = Types.Pool.Main
            Description = "Large equipment"
        }

        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 10_000_00L<Cent>
                MaxYears = 3
        }

        let schedule = (Calculations.generateSchedule customConfig expenditure).Years

        // Should have multiple years
        schedule.Length |> should be (greaterThan 1)

        // First year: £10k AIA, £7.2k WDA (18% of remaining £40k)
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.AnnualInvestmentAllowance |> should equal 10_000_00L<Cent>
        year1.WritingDownAllowance |> should equal 7_200_00L<Cent> // 18% of 40k
        year1.TotalAllowances |> should equal 17_200_00L<Cent>
        year1.PoolValueEndOfYear |> should equal 32_800_00L<Cent> // 40k - 7.2k

    [<Fact>]
    let ``Special rate pool uses 6% WDA rate`` () =
        let expenditure = {
            Amount = 30_000_00L<Cent>
            Pool = Types.Pool.SpecialRate
            Description = "Vehicle"
        }

        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 20_000_00L<Cent>
                MaxYears = 2
        }

        let schedule = (Calculations.generateSchedule customConfig expenditure).Years

        // First year: £20k AIA, £0.6k WDA (6% of remaining £10k)
        let year1 = schedule |> List.head
        year1.AnnualInvestmentAllowance |> should equal 20_000_00L<Cent>
        year1.WritingDownAllowance |> should equal 600_00L<Cent> // 6% of 10k
        year1.PoolValueEndOfYear |> should equal 9_400_00L<Cent>

    [<Fact>]
    let ``Example machinery schedule works`` () =
        let schedule = (Examples.exampleMachinerySchedule ()).Years

        schedule |> should not' (be Empty)

        let year1 = schedule |> List.head
        year1.AnnualInvestmentAllowance |> should equal 50_000_00L<Cent> // Full amount via AIA
        year1.WritingDownAllowance |> should equal 0_00L<Cent>
        year1.TotalAllowances |> should equal 50_000_00L<Cent>

    [<Fact>]
    let ``Example vehicle schedule works`` () =
        let schedule = (Examples.exampleVehicleSchedule ()).Years

        schedule |> should not' (be Empty)

        let year1 = schedule |> List.head
        year1.AnnualInvestmentAllowance |> should equal 30_000_00L<Cent> // Full amount via AIA
        year1.WritingDownAllowance |> should equal 0_00L<Cent>
        year1.TotalAllowances |> should equal 30_000_00L<Cent>

    [<Fact>]
    let ``Schedule continues until pool is written off via small pools allowance`` () =
        let expenditure = {
            Amount = 50_000_00L<Cent>
            Pool = Types.Pool.Main
            Description = "Equipment"
        }

        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 0_00L<Cent> // No AIA
                MaxYears = 50
        }

        let result = Calculations.generateSchedule customConfig expenditure
        let schedule = result.Years

        // Should continue for multiple years until the pool drops to the small pools threshold
        schedule.Length |> should be (greaterThan 15)
        schedule.Length |> should be (lessThan 50)

        // Last year writes off the remaining small pool in full - nothing left unclaimed
        let lastYear = schedule |> List.last
        lastYear.PoolValueEndOfYear |> should equal 0L<Cent>
        result.UnclaimedPool |> should equal 0L<Cent>

        // Total allowances over the schedule equal the original expenditure
        schedule |> List.sumBy (fun year -> year.TotalAllowances) |> should equal expenditure.Amount

    [<Fact>]
    let ``Pool at or below small pools threshold is written off in full`` () =
        let expenditure = {
            Amount = 1_000_00L<Cent> // exactly £1,000
            Pool = Types.Pool.Main
            Description = "Small pool"
        }

        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 0L<Cent>
                MaxYears = 10
        }

        let result = Calculations.generateSchedule customConfig expenditure
        let schedule = result.Years

        schedule.Length |> should equal 1
        let year1 = schedule |> List.head
        year1.WritingDownAllowance |> should equal 1_000_00L<Cent>
        year1.PoolValueEndOfYear |> should equal 0L<Cent>
        result.UnclaimedPool |> should equal 0L<Cent>

    [<Fact>]
    let ``Tiny residual pool is cleared instead of repeating zero WDA years`` () =
        let expenditure = {
            Amount = 0_03L<Cent>
            Pool = Types.Pool.Main
            Description = "Tiny asset"
        }

        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 0L<Cent>
                MaxYears = 10
        }

        let schedule = (Calculations.generateSchedule customConfig expenditure).Years
        let lastYear = schedule |> List.last

        schedule.Length |> should be (lessThanOrEqualTo 2)
        schedule |> List.exists (fun year -> year.WritingDownAllowance = 0L<Cent> && year.PoolValueEndOfYear > 0L<Cent>) |> should equal false
        lastYear.WritingDownAllowance |> should be (greaterThan 0L<Cent>)
        lastYear.PoolValueEndOfYear |> should equal 0L<Cent>

    [<Fact>]
    let ``Truncation at MaxYears reports the unclaimed pool`` () =
        // audited scenario: a large special-rate pool truncated at the default 10 years
        // used to abandon the residual with no flag
        let expenditure = {
            Amount = 200_000_00L<Cent>
            Pool = Types.Pool.SpecialRate
            Description = "Long-life asset"
        }

        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 0L<Cent>
                MaxYears = 10
        }

        let result = Calculations.generateSchedule customConfig expenditure
        let schedule = result.Years

        schedule.Length |> should equal 10

        // the unclaimed residual is visible and consistent with the schedule
        let lastYear = schedule |> List.last
        result.UnclaimedPool |> should equal lastYear.PoolValueEndOfYear
        result.UnclaimedPool |> should be (greaterThan 0L<Cent>)

        // claimed + unclaimed = original expenditure
        let totalClaimed = schedule |> List.sumBy (fun year -> year.TotalAllowances)
        totalClaimed + result.UnclaimedPool |> should equal expenditure.Amount
