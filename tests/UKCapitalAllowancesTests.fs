namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal.EquipmentFinance.Depreciation.UK_CapitalAllowances

module UKCapitalAllowancesTests =

    [<Fact>]
    let ``Default configuration has expected values`` () =
        Types.Default.AnnualInvestmentAllowanceLimit |> should equal 1_000_000m
        Types.Default.MainPoolRate |> should equal 0.18m
        Types.Default.SpecialRatePoolRate |> should equal 0.06m
        Types.Default.MaxYears |> should equal 10

    [<Fact>]
    let ``Rounding function works correctly`` () =
        Calculations.roundAwayFromZero 12.345m |> should equal 12.35m
        Calculations.roundAwayFromZero 12.344m |> should equal 12.34m
        Calculations.roundAwayFromZero 12.346m |> should equal 12.35m

    [<Fact>]
    let ``Small asset fully claimed via AIA in year 1`` () =
        let expenditure = {
            Amount = 5_000m
            Pool = Types.Pool.Main
            Description = "Small equipment"
        }
        
        let schedule = Calculations.scheduleDefault expenditure
        
        // Should have at least one year
        schedule |> should not' (be Empty)
        
        // First year should claim full amount via AIA
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.AnnualInvestmentAllowance |> should equal 5_000m
        year1.WritingDownAllowance |> should equal 0m
        year1.TotalAllowances |> should equal 5_000m
        year1.PoolValueEndOfYear |> should equal 0m

    [<Fact>]
    let ``Large asset partially claimed via AIA then WDA`` () =
        let expenditure = {
            Amount = 50_000m
            Pool = Types.Pool.Main
            Description = "Large equipment"
        }
        
        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 10_000m
                MaxYears = 3
        }
        
        let schedule = Calculations.generateSchedule customConfig expenditure
        
        // Should have multiple years
        schedule.Length |> should be (greaterThan 1)
        
        // First year: £10k AIA, £7.2k WDA (18% of remaining £40k)
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.AnnualInvestmentAllowance |> should equal 10_000m
        year1.WritingDownAllowance |> should equal 7_200m // 18% of 40k
        year1.TotalAllowances |> should equal 17_200m
        year1.PoolValueEndOfYear |> should equal 32_800m // 40k - 7.2k

    [<Fact>]
    let ``Special rate pool uses 6% WDA rate`` () =
        let expenditure = {
            Amount = 30_000m
            Pool = Types.Pool.SpecialRate
            Description = "Vehicle"
        }
        
        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 20_000m
                MaxYears = 2
        }
        
        let schedule = Calculations.generateSchedule customConfig expenditure
        
        // First year: £20k AIA, £0.6k WDA (6% of remaining £10k)
        let year1 = schedule |> List.head
        year1.AnnualInvestmentAllowance |> should equal 20_000m
        year1.WritingDownAllowance |> should equal 600m // 6% of 10k
        year1.PoolValueEndOfYear |> should equal 9_400m

    [<Fact>]
    let ``Example machinery schedule works`` () =
        let schedule = Examples.exampleMachinerySchedule ()
        
        schedule |> should not' (be Empty)
        
        let year1 = schedule |> List.head
        year1.AnnualInvestmentAllowance |> should equal 50_000m // Full amount via AIA
        year1.WritingDownAllowance |> should equal 0m
        year1.TotalAllowances |> should equal 50_000m

    [<Fact>]
    let ``Example vehicle schedule works`` () =
        let schedule = Examples.exampleVehicleSchedule ()
        
        schedule |> should not' (be Empty)
        
        let year1 = schedule |> List.head
        year1.AnnualInvestmentAllowance |> should equal 30_000m // Full amount via AIA
        year1.WritingDownAllowance |> should equal 0m
        year1.TotalAllowances |> should equal 30_000m

    [<Fact>]
    let ``Schedule continues until pool value is zero or max years reached`` () =
        let expenditure = {
            Amount = 1_000m
            Pool = Types.Pool.Main
            Description = "Small equipment"
        }
        
        let customConfig = {
            Types.Default with
                AnnualInvestmentAllowanceLimit = 0m // No AIA
                MaxYears = 20
        }
        
        let schedule = Calculations.generateSchedule customConfig expenditure
        
        // Should continue for multiple years until pool depleted
        schedule.Length |> should be (greaterThan 5)
        
        // Last year should have pool value of 0 or very small
        let lastYear = schedule |> List.last
        lastYear.PoolValueEndOfYear |> should be (lessThanOrEqualTo 1m)