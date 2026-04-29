namespace FSharp.Finance.Personal.Tests

open Xunit
open FsUnit.Xunit

open FSharp.Finance.Personal.Calculation
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS
open FSharp.Finance.Personal.EquipmentFinance.Depreciation.US_MACRS.Types

module USMacrsTests =

    [<Fact>]
    let ``Asset class recovery periods are correct`` () =
        Calculations.getRecoveryPeriod Types.AssetClass.ThreeYear |> should equal 3
        Calculations.getRecoveryPeriod Types.AssetClass.FiveYear |> should equal 5
        Calculations.getRecoveryPeriod Types.AssetClass.SevenYear |> should equal 7
        Calculations.getRecoveryPeriod Types.AssetClass.TenYear |> should equal 10
        Calculations.getRecoveryPeriod Types.AssetClass.FifteenYear |> should equal 15
        Calculations.getRecoveryPeriod Types.AssetClass.TwentyYear |> should equal 20

    [<Fact>]
    let ``Five-year property percentages are available`` () =
        let percentages = Tables.getDepreciationPercentages Types.AssetClass.FiveYear
        
        percentages.Length |> should equal 6
        percentages.[0] |> should equal 20.00m
        percentages.[1] |> should equal 32.00m
        percentages.[2] |> should equal 19.20m
        percentages.[3] |> should equal 11.52m
        percentages.[4] |> should equal 11.52m
        percentages.[5] |> should equal 5.76m

    [<Fact>]
    let ``Seven-year property percentages are available`` () =
        let percentages = Tables.getDepreciationPercentages Types.AssetClass.SevenYear
        
        percentages.Length |> should equal 8
        percentages.[0] |> should equal 14.29m
        percentages.[1] |> should equal 24.49m

    [<Fact>]
    let ``Five-year asset depreciation schedule is correct`` () =
        let asset = {
            CostBasis = 10000_00L<Cent> // $10,000
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.FiveYear
            Convention = Types.Convention.HalfYear
        }
        
        let schedule = Calculations.generateSchedule asset
        
        schedule.Length |> should equal 6
        
        // Year 1: 20% of $10,000 = $2,000
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.DepreciationRate |> should equal 0.20m
        year1.DepreciationAmount |> should equal 2000_00L<Cent> // $2,000
        year1.AccumulatedDepreciation |> should equal 2000_00L<Cent>
        year1.BookValue |> should equal 8000_00L<Cent>
        
        // Year 2: 32% of $10,000 = $3,200
        let year2 = schedule.[1]
        year2.Year |> should equal 2
        year2.DepreciationRate |> should equal 0.32m
        year2.DepreciationAmount |> should equal 3200_00L<Cent> // $3,200
        year2.AccumulatedDepreciation |> should equal 5200_00L<Cent>
        year2.BookValue |> should equal 4800_00L<Cent>

    [<Fact>]
    let ``Three-year asset depreciation schedule is correct`` () =
        let asset = {
            CostBasis = 3000_00L<Cent> // $3,000
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.ThreeYear
            Convention = Types.Convention.HalfYear
        }
        
        let schedule = Calculations.generateSchedule asset
        
        schedule.Length |> should equal 4
        
        // Year 1: 33.33% of $3,000 ≈ $999.90
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.DepreciationAmount |> should (equalWithin 100L<Cent>) 999_90L<Cent>
        
        // Final year should have minimal book value
        let lastYear = schedule |> List.last
        lastYear.BookValue |> should be (lessThan 300_00L<Cent>) // Most should be depreciated

    [<Fact>]
    let ``Total depreciation equals original basis`` () =
        let asset = {
            CostBasis = 500000L<Cent> // $5,000
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.SevenYear
            Convention = Types.Convention.HalfYear
        }
        
        let schedule = Calculations.generateSchedule asset
        
        let totalDepreciation = 
            schedule 
            |> List.sumBy (fun year -> year.DepreciationAmount)
        
        // Total should equal original basis (within rounding tolerance)
        totalDepreciation |> should (equalWithin 50L<Cent>) asset.CostBasis

    [<Fact>]
    let ``MACRS schedule is capped at full cost basis`` () =
        let asset = {
            CostBasis = 100_00L<Cent>
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.ThreeYear
            Convention = Types.Convention.HalfYear
        }

        let schedule = Calculations.generateSchedule asset
        let lastYear = schedule |> List.last

        lastYear.AccumulatedDepreciation |> should equal asset.CostBasis
        lastYear.BookValue |> should equal 0L<Cent>

    [<Fact>]
    let ``Unsupported MACRS convention is rejected`` () =
        let asset = {
            CostBasis = 1000_00L<Cent>
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.FiveYear
            Convention = Types.Convention.MidQuarter
        }

        (fun () -> Calculations.generateSchedule asset |> ignore)
        |> should throw typeof<System.ArgumentException>

    [<Fact>]
    let ``Example computer schedule works`` () =
        let schedule = Examples.exampleComputerSchedule ()
        
        schedule |> should not' (be Empty)
        schedule.Length |> should equal 6
        
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.DepreciationAmount |> should equal 2000_00L<Cent> // 20% of $10,000

    [<Fact>]
    let ``Example furniture schedule works`` () =
        let schedule = Examples.exampleFurnitureSchedule ()
        
        schedule |> should not' (be Empty)
        schedule.Length |> should equal 8 // 7-year property has 8 years
        
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.DepreciationAmount |> should equal 714_50L<Cent> // 14.29% of $5,000

    [<Fact>]
    let ``Example equipment schedule works`` () =
        let schedule = Examples.exampleEquipmentSchedule ()
        
        schedule |> should not' (be Empty)
        schedule.Length |> should equal 8
        
        let year1 = schedule |> List.head
        year1.Year |> should equal 1
        year1.DepreciationAmount |> should equal 3572_50L<Cent> // 14.29% of $25,000

    [<Fact>]
    let ``Book value decreases each year`` () =
        let asset = {
            CostBasis = 15000_00L<Cent> // $15,000
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.FiveYear
            Convention = Types.Convention.HalfYear
        }
        
        let schedule = Calculations.generateSchedule asset
        
        // Book value should decrease each year
        let bookValues = schedule |> List.map (fun year -> year.BookValue)
        
        bookValues 
        |> List.pairwise
        |> List.iter (fun (prev, curr) -> curr |> should be (lessThan prev))

    [<Fact>]
    let ``Accumulated depreciation increases each year`` () =
        let asset = {
            CostBasis = 8000_00L<Cent> // $8,000
            PlacedInServiceDate = FSharp.Finance.Personal.DateDay.Date(2024, 1, 1)
            PropertyClass = Types.AssetClass.ThreeYear
            Convention = Types.Convention.HalfYear
        }
        
        let schedule = Calculations.generateSchedule asset
        
        // Accumulated depreciation should increase each year
        let accumulatedValues = schedule |> List.map (fun year -> year.AccumulatedDepreciation)
        
        accumulatedValues 
        |> List.pairwise
        |> List.iter (fun (prev, curr) -> curr |> should be (greaterThan prev))

    [<Fact>]
    let ``Asset classification works correctly`` () =
        Calculations.classifyAsset "computer equipment" |> should equal Types.AssetClass.FiveYear
        Calculations.classifyAsset "car" |> should equal Types.AssetClass.FiveYear
        Calculations.classifyAsset "office furniture" |> should equal Types.AssetClass.SevenYear
        Calculations.classifyAsset "manufacturing equipment" |> should equal Types.AssetClass.SevenYear
        Calculations.classifyAsset "building" |> should equal Types.AssetClass.FifteenYear
        Calculations.classifyAsset "general equipment" |> should equal Types.AssetClass.FiveYear // default
